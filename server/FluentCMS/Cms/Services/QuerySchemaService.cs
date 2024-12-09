using System.Collections.Immutable;
using FluentCMS.Cms.Models;
using FluentCMS.Modules;
using FluentCMS.Services;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.Graph;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using FluentResults;
using GraphQLParser.AST;
using Query = FluentCMS.Utils.QueryBuilder.Query;
using Schema = FluentCMS.Cms.Models.Schema;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class QuerySchemaService(
    ISchemaService schemaSvc,
    IEntitySchemaService entitySchemaSvc,
    ExpiringKeyValueCache<LoadedQuery> queryCache,
    CmsModule cmsModule
) : IQuerySchemaService
{
    public async Task<LoadedQuery> ByGraphQlRequest(GraphQlRequestDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Query.Name) 
            && queryCache.TryGetValue(dto.Query.Name, out var cachedQuery) 
            && cachedQuery != null 
            && cachedQuery.Source == dto.Query.Source)
        {
                return cachedQuery;
        }

        var query = dto.Query;
        var loadedQuery = await ToLoadedQuery(query, dto.Fields);
        if (string.IsNullOrWhiteSpace(query.Name)) return loadedQuery;

        query = query with
        {
            IdeUrl = $"{cmsModule.GraphPath}?query={Uri.EscapeDataString(query.Source)}&operationName={query.Name}"
        };

        await VerifyQuery(query);
        var schema = new Schema(query.Name, SchemaType.Query, new Settings(Query: query));
        await schemaSvc.AddOrUpdateByNameWithAction(schema, default);
        queryCache.Replace(query.Name, loadedQuery);

        return loadedQuery;
    }

    public async Task<LoadedQuery> ByNameAndCache(string name, CancellationToken token = default)
    {
        var query = await queryCache.GetOrSet(name, async () => await GetByName(name, token));
        return NotNull(query).ValOrThrow($"can not find query [{name}]");
    }

    public string CreateQueryUrl()
    {
        return NotNull(cmsModule).ValOrThrow("query module is not enabled").GraphPath;
    }
    
    public async Task Delete(Schema schema, CancellationToken token)
    {
        await schemaSvc.Delete(schema.Id, token);
        if (schema.Settings.Query is not null)
        {
            queryCache.Remove(schema.Settings.Query.Name);
        }
    }

    private async Task<LoadedQuery> GetByName(string name, CancellationToken token)
    {
        StrNotEmpty(name).ValOrThrow("query name should not be empty");
        var item = NotNull(await schemaSvc.GetByNameDefault(name, SchemaType.Query, token))
            .ValOrThrow($"can not find query by name {name}");
        var query = NotNull(item.Settings.Query).ValOrThrow("invalid view format");
        var fields = Ok(Converter.GetRootGraphQlFields(query.Source));
        return await ToLoadedQuery(query,fields,token);
    }

    private async Task<LoadedQuery> ToLoadedQuery(Query query, IEnumerable<GraphQLField> fields, CancellationToken token = default){
        var entity = Ok(await entitySchemaSvc.GetLoadedEntity(query.EntityName, token));
        var selection = Ok(await SelectionSetToNode("", entity, fields, token));
        var sorts = Ok(await SortHelper.ToValidSorts(query.Sorts,entity, entitySchemaSvc));
        var validFilter = Ok(await query.Filters.ToValid(entity, entitySchemaSvc, entitySchemaSvc));
        return query.ToLoadedQuery(entity, selection, sorts,validFilter);
    }

    private async Task VerifyQuery(Query? query, CancellationToken token = default) 
    {
        if (query is null)
        {
            throw new InvalidParamException("query is null");
        }

        var entity = Ok(await entitySchemaSvc.GetLoadedEntity(query.EntityName, token));
        Ok(await query.Filters.ToValid(entity,entitySchemaSvc,entitySchemaSvc));

        var fields = Ok(Converter.GetRootGraphQlFields(query.Source));
        Ok(await SelectionSetToNode("", entity, fields, token));
        Ok(await SortHelper.ToValidSorts(query.Sorts,entity, entitySchemaSvc));
    }

    private async Task<Result<ImmutableArray<GraphAttribute>>> SelectionSetToNode(
        string prefix,
        LoadedEntity entity,
        IEnumerable<GraphQLField> graphQlFields,
        CancellationToken token = default)
    {

        List<GraphAttribute> attributes = [];
        foreach (var field in graphQlFields)
        {
            if (!(await LoadAttribute(entity, field.Name.StringValue, token)).Try(out var graphAttr, out var err))
            {
                return Result.Fail(err);
            }

            graphAttr = graphAttr with { Prefix = prefix };

            if (!(await LoadSelection(graphAttr.FullPathName(prefix), graphAttr, field, token)).Try(out graphAttr,
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
        GraphQLField field, CancellationToken token)
    {
        var targetEntity = attr.Type switch
        {
            DisplayType.Crosstable => attr.Crosstable!.TargetEntity,
            DisplayType.Lookup => attr.Lookup,
            _ => null
        };

        if (targetEntity is null || field.SelectionSet is null) return attr;
        if (!(await SelectionSetToNode(prefix, targetEntity, field.SelectionSet.SubFields(), token))
            .Try(out var children, out var err))
        {
            return Result.Fail(err);
        }

        attr = attr with { Selection = children };

        return attr;
    }

    private async Task<Result<GraphAttribute>> LoadAttribute(LoadedEntity entity, string fldName,
        CancellationToken token)
    {
        var find = entity.Attributes.FindOneAttr(fldName);
        if (find is null)
        {
            return Result.Fail($"Parsing `SectionSet` fail, can not find {fldName} in {entity.Name}");
        }

        if (find.Type is not (DisplayType.Crosstable or DisplayType.Lookup)) return find.ToGraph();
        if (!(await entitySchemaSvc.LoadOneCompoundAttribute(entity, find, [], token))
            .Try(out var compoundAttr, out var err))
        {
            return Result.Fail(err);
        }

        find = compoundAttr;

        return find.ToGraph();
    }
}