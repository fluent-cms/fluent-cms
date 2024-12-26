using System.Collections.Immutable;
using FluentCMS.Cms.Models;
using FluentCMS.Types;
using FluentCMS.Utils.Cache;
using FluentCMS.Graph;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using FluentResults;
using GraphQLParser.AST;
using Query = FluentCMS.Utils.QueryBuilder.Query;
using Schema = FluentCMS.Cms.Models.Schema;

namespace FluentCMS.Cms.Services;

public sealed class QuerySchemaService(
    ISchemaService schemaSvc,
    IEntitySchemaService entitySchemaSvc,
    KeyValueCache<LoadedQuery> queryCache,
    CmsOptions cmsOptions
) : IQuerySchemaService
{
    public async Task<LoadedQuery> ByGraphQlRequest(Query query, GraphQLField[] fields)
    {
        if (string.IsNullOrWhiteSpace(query.Name))
        {
            return await ToLoadedQuery(query,fields);
        }

        var loadedQuery = await queryCache.GetOrSet(query.Name, SaveToDbAndCache);
        if (loadedQuery.Source != query.Source)
        {
            await queryCache.Remove(query.Name);
            loadedQuery = await queryCache.GetOrSet(query.Name, SaveToDbAndCache);
        }
        
        return loadedQuery;

        async ValueTask<LoadedQuery> SaveToDbAndCache(CancellationToken ct)
        {
            await SaveQuery(query,ct);
            return await ToLoadedQuery(query, fields, ct);
        }
    }

    public async Task SaveQuery(Query query, CancellationToken ct = default)
    {
        query = query with
        {
            IdeUrl =
            $"{cmsOptions.GraphQlPath}?query={Uri.EscapeDataString(query.Source)}&operationName={query.Name}"
        };
        await VerifyQuery(query, ct);
        var schema = new Schema(query.Name, SchemaType.Query, new Settings(Query: query));
        await schemaSvc.AddOrUpdateByNameWithAction(schema, ct);

    }

    public async Task<LoadedQuery> ByNameAndCache(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ResultException("query name should not be empty");
        var query = await queryCache.GetOrSet(name, async (token) =>
        {
            var schema = await schemaSvc.GetByNameDefault(name, SchemaType.Query, token)??
                throw new ResultException($"can not find query by name {name}");
            var query = schema.Settings.Query ??
                throw new ResultException("invalid query format");
            var fields = Converter.GetRootGraphQlFields(query.Source).Ok();
            return await ToLoadedQuery(query, fields, token);
        }, ct);
        return query ?? throw new ResultException($"can not find query [{name}]");
    }

    public string GraphQlClientUrl()
    {
        return cmsOptions.GraphQlPath;
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
        var validFilter = (await query.Filters.ToValidFilters(entity, entitySchemaSvc, entitySchemaSvc)).Ok();
        return query.ToLoadedQuery(entity, selection, sorts,validFilter);
    }

    private async Task VerifyQuery(Query? query, CancellationToken ct = default) 
    {
        if (query is null)
        {
            throw new ResultException("query is null");
        }

        var entity = (await entitySchemaSvc.GetLoadedEntity(query.EntityName, ct)).Ok();
        (await query.Filters.ToValidFilters(entity,entitySchemaSvc,entitySchemaSvc)).Ok();

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

            if (graphAttr.DataType == DataType.Junction)
            {
                var inputs = field.Arguments?.Select(x => new GraphQlArgumentDataProvider(x)) ?? [];
                if (!QueryHelper.ParseSimpleArguments(inputs).Try(out var res, out var parseErr))
                {
                    return Result.Fail(parseErr);
                }

                var (sorts, filters, pagination) = res;
                var target = graphAttr.Junction!.TargetEntity;
                
                if (!(await SortHelper.ToValidSorts(sorts,target, entitySchemaSvc)).Try(out var validSorts,out   err))
                {
                    return Result.Fail(err);
                }

                if (!(await filters.ToValidFilters(target, entitySchemaSvc, entitySchemaSvc)).Try(out var validFilters,
                        out err))
                {
                    return Result.Fail(err);
                }
                
                graphAttr = graphAttr with { Pagination = pagination, Filters = [..validFilters], Sorts = [..validSorts] };
            }

            attributes.Add(graphAttr);
        }

        if (attributes.FindOneAttr(entity.PrimaryKey) is null)
        {
            return Result.Fail($"Primary Key [{entity.PrimaryKey}] not found in [{prefix}]");
        }
        
        return attributes.ToImmutableArray();
    }

    private async Task<Result<GraphAttribute>> LoadSelection(string prefix, GraphAttribute attr,
        GraphQLField field, CancellationToken ct)
    {
        var targetEntity = attr.DataType switch
        {
            DataType.Junction => attr.Junction!.TargetEntity,
            DataType.Lookup => attr.Lookup,
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

        if (find.DataType is not (DataType.Junction or DataType.Lookup)) return find.ToGraph();
        if (!(await entitySchemaSvc.LoadCompoundAttribute(entity, find, [], ct))
            .Try(out var compoundAttr, out var err))
        {
            return Result.Fail(err);
        }

        find = compoundAttr;

        return find.ToGraph();
    }
}