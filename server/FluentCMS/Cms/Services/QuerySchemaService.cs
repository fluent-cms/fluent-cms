using System.Collections.Immutable;
using System.Text.Json;
using FluentCMS.Cms.Models;
using FluentCMS.Builders;
using FluentCMS.Exceptions;
using FluentCMS.Utils.Cache;
using FluentCMS.Graph;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using FluentResults;
using GraphQLParser.AST;
using Query = FluentCMS.Utils.QueryBuilder.Query;
using ResultExt = FluentCMS.Exceptions.ResultExt;
using Schema = FluentCMS.Cms.Models.Schema;

namespace FluentCMS.Cms.Services;

public sealed class QuerySchemaService(
    ISchemaService schemaSvc,
    IEntitySchemaService entitySchemaSvc,
    KeyValueCache<LoadedQuery> queryCache,
    CmsBuilder cms
) : IQuerySchemaService
{
    public async Task<LoadedQuery> ByGraphQlRequest(GraphQlRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Query.Name))
        {
            return await ToLoadedQuery(dto.Query,dto.Fields);
        }

        var loadedQuery = await queryCache.GetOrSet(dto.Query.Name, SaveToDbAndCache);
        if (loadedQuery.Source != dto.Query.Source)
        {
            await queryCache.Remove(dto.Query.Name);
            loadedQuery = await queryCache.GetOrSet(dto.Query.Name, SaveToDbAndCache);
        }
        
        return loadedQuery;

        async ValueTask<LoadedQuery> SaveToDbAndCache(CancellationToken ct)
        {
            var query = dto.Query with
            {
                IdeUrl =
                $"{cms.Options.GraphQlPath}?query={Uri.EscapeDataString(dto.Query.Source)}&operationName={dto.Query.Name}"
            };
            await VerifyQuery(query, ct);
            var schema = new Schema(query.Name, SchemaType.Query, new Settings(Query: query));
            await schemaSvc.AddOrUpdateByNameWithAction(schema, default);
            return await ToLoadedQuery(query, dto.Fields, ct);
        }
    }

    public async Task<LoadedQuery> ByNameAndCache(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ServiceException("query name should not be empty");
        var query = await queryCache.GetOrSet(name, async (token) =>
        {
            var schema = await schemaSvc.GetByNameDefault(name, SchemaType.Query, token)??
                throw new ServiceException($"can not find query by name {name}");
            var query = schema.Settings.Query ??
                throw new ServiceException("invalid query format");
            var fields = Converter.GetRootGraphQlFields(query.Source).Ok();
            return await ToLoadedQuery(query, fields, token);
        }, ct);
        return query ?? throw new ServiceException($"can not find query [{name}]");
    }

    public string CreateQueryUrl()
    {
        return cms.Options.GraphQlPath;
    }
    
    public async Task Delete(Schema schema, CancellationToken ct)
    {
        await schemaSvc.Delete(schema.Id, ct);
        if (schema.Settings.Query is not null)
        {
            await queryCache.Remove(schema.Settings.Query.Name,ct);
        }
    }

    private async Task<LoadedQuery> ToLoadedQuery(Query query, IEnumerable<GraphQLField> fields, CancellationToken ct = default){
        var entity = (await entitySchemaSvc.GetLoadedEntity(query.EntityName, ct)).Ok();
        var selection = (await SelectionSetToNode("", entity, fields, ct)).Ok();
        var sorts = (await SortHelper.ToValidSorts(query.Sorts,entity, entitySchemaSvc)).Ok();
        var validFilter = (await query.Filters.ToValid(entity, entitySchemaSvc, entitySchemaSvc)).Ok();
        return query.ToLoadedQuery(entity, selection, sorts,validFilter);
    }

    private async Task VerifyQuery(Query? query, CancellationToken ct = default) 
    {
        if (query is null)
        {
            throw new ServiceException("query is null");
        }

        var entity = (await entitySchemaSvc.GetLoadedEntity(query.EntityName, ct)).Ok();
        (await query.Filters.ToValid(entity,entitySchemaSvc,entitySchemaSvc)).Ok();

        var fields = Converter.GetRootGraphQlFields(query.Source).Ok();
        (await SelectionSetToNode("", entity, fields, ct)).Ok();
        (await SortHelper.ToValidSorts(query.Sorts,entity, entitySchemaSvc)).Ok();
    }

    private async Task<Result<ImmutableArray<GraphAttribute>>> SelectionSetToNode(
        string prefix,
        LoadedEntity entity,
        IEnumerable<GraphQLField> graphQlFields,
        CancellationToken ct = default)
    {

        List<GraphAttribute> attributes = [];
        foreach (var field in graphQlFields)
        {
            if (!(await LoadAttribute(entity, field.Name.StringValue, ct)).Try(out var graphAttr, out var err))
            {
                return Result.Fail(err);
            }

            graphAttr = graphAttr with { Prefix = prefix };

            if (!(await LoadSelection(graphAttr.FullPathName(prefix), graphAttr, field, ct)).Try(out graphAttr,
                    out err))
            {
                return Result.Fail(err);
            }

            if (graphAttr.Type == DisplayType.Crosstable)
            {
                var inputs = field.Arguments?.Select(x => new GraphQlArgumentDataProvider(x)) ?? [];
                if (!QueryHelper.ParseSimpleArguments(inputs).Try(out var res, out var parseErr))
                {
                    return Result.Fail(parseErr);
                }

                var (sorts, filters, pagination) = res;
                var target = graphAttr.Crosstable!.TargetEntity;
                
                if (!(await SortHelper.ToValidSorts(sorts,target, entitySchemaSvc)).Try(out var validSorts,out   err))
                {
                    return Result.Fail(err);
                }

                if (!(await filters.ToValid(target, entitySchemaSvc, entitySchemaSvc)).Try(out var validFilters,
                        out err))
                {
                    return Result.Fail(err);
                }
                
                graphAttr = graphAttr with { Pagination = pagination, Filters = [..validFilters], Sorts = [..validSorts] };
            }

            attributes.Add(graphAttr);
        }

        if (attributes.FindOneAttr(entity.PrimaryKey) == default)
        {
            return Result.Fail($"Primary Key [{entity.PrimaryKey}] not found in [{prefix}]");
        }
        
        return attributes.ToImmutableArray();
    }

    private async Task<Result<GraphAttribute>> LoadSelection(string prefix, GraphAttribute attr,
        GraphQLField field, CancellationToken ct)
    {
        var targetEntity = attr.Type switch
        {
            DisplayType.Crosstable => attr.Crosstable!.TargetEntity,
            DisplayType.Lookup => attr.Lookup,
            _ => null
        };

        if (targetEntity is null || field.SelectionSet is null) return attr;
        if (!(await SelectionSetToNode(prefix, targetEntity, field.SelectionSet.SubFields(), ct))
            .Try(out var children, out var err))
        {
            return Result.Fail(err);
        }

        attr = attr with { Selection = children };

        return attr;
    }

    private async Task<Result<GraphAttribute>> LoadAttribute(LoadedEntity entity, string fldName,
        CancellationToken ct)
    {
        var find = entity.Attributes.FindOneAttr(fldName);
        if (find is null)
        {
            return Result.Fail($"Parsing `SectionSet` fail, can not find {fldName} in {entity.Name}");
        }

        if (find.Type is not (DisplayType.Crosstable or DisplayType.Lookup)) return find.ToGraph();
        if (!(await entitySchemaSvc.LoadOneCompoundAttribute(entity, find, [], ct))
            .Try(out var compoundAttr, out var err))
        {
            return Result.Fail(err);
        }

        find = compoundAttr;

        return find.ToGraph();
    }
}