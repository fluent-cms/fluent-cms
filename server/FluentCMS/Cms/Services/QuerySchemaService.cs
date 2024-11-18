using System.Collections.Immutable;
using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.Graph;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQLParser.AST;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class QuerySchemaService(
    ISchemaService schemaSvc,
    IEntitySchemaService entitySchemaSvc,
    ExpiringKeyValueCache<LoadedQuery> queryCache
) : IQuerySchemaService
{
    public async Task<LoadedQuery> GetByGraphFields(string entityName, IEnumerable<GraphQLField> fields,
        IEnumerable<IValueProvider>? args)
    {
        var entity = CheckResult(await entitySchemaSvc.GetLoadedEntity(entityName));
        var selection = CheckResult(await SelectionSetToNode("", entity, fields, default));
        var (sorts,filters) = CheckResult(await GetSortAndFilter(entity, args??[]));
        return new LoadedQuery("GraphQL" + entityName , entityName, entity.DefaultPageSize, selection,sorts , filters, entity);
    }

    public async Task<LoadedQuery> GetByNameAndCache(string name, CancellationToken token = default)
    {
        var query = await queryCache.GetOrSet(name, async () => await GetByName(name, token));
        return NotNull(query).ValOrThrow($"can not find query [{name}]");
    }

    private async Task<LoadedQuery> GetByName(string name, CancellationToken token)
    {
        StrNotEmpty(name).ValOrThrow("query name should not be empty");
        var item = NotNull(await schemaSvc.GetByNameDefault(name, SchemaType.Query, token))
            .ValOrThrow($"can not find query by name {name}");
        var query = NotNull(item.Settings.Query).ValOrThrow("invalid view format");
        var entity = CheckResult(await entitySchemaSvc.GetLoadedEntity(query.EntityName, token));
        var fields = CheckResult(GraphTypeHelper.GetRootGraphQlFields(query.SelectionSet));
        var selection = CheckResult(await SelectionSetToNode("", entity, fields, token));
        var sorts = CheckResult(await query.Sorts.ToValidSorts(entity, entitySchemaSvc));
        return query.ToLoadedQuery(entity, selection, sorts);
    }

    public async Task<Schema> Save(Schema schema, CancellationToken cancellationToken)
    {
        await VerifyQuery(schema.Settings.Query, cancellationToken);
        var ret = await schemaSvc.SaveWithAction(schema, cancellationToken);
        var query = await GetByName(schema.Name, cancellationToken);
        queryCache.Remove(query.Name);
        return ret;
    }

    private async Task VerifyQuery(Query? query, CancellationToken token)
    {
        if (query is null)
        {
            throw new InvalidParamException("query is null");
        }

        var entity = CheckResult(await entitySchemaSvc.GetLoadedEntity(query.EntityName, token));
        CheckResult(await query.Filters.Verify(entity, entitySchemaSvc, entitySchemaSvc));

        var fields = CheckResult(GraphTypeHelper.GetRootGraphQlFields(query.SelectionSet));
        var selection = CheckResult(await SelectionSetToNode("", entity, fields, token));
        var sorts = CheckResult(await query.Sorts.ToValidSorts(entity, entitySchemaSvc));
        //todo: subfields' can only order by local attribute for now.
        //maybe support order subfields' subfield later
        CheckSorts(selection, sorts, true);
    }

    private void CheckSorts(ImmutableArray<GraphAttribute> attributes, IEnumerable<ValidSort> sorts,
        bool allowRecursive)
    {
        foreach (var sort in sorts)
        {
            var find = allowRecursive
                ? attributes.RecursiveFind(sort.FieldName)
                : attributes.FindOneAttr(sort.FieldName);
            if (find is null)
            {
                throw new InvalidParamException($"can not find sort field {sort.FieldName} in selection");
            }
        }

        foreach (var attr in attributes)
        {
            CheckSorts(attr.Selection, attr.Sorts, false);
        }
    }

    private async Task<Result<(ImmutableArray<ValidSort>, ImmutableArray<Filter>)>> GetSortAndFilter(
        LoadedEntity entity, 
        IEnumerable<IValueProvider> args)
    {
        var sorts = new List<Sort>();
        var filters = new List<Filter>();
        foreach (var input in args)
        {
            if (input.Name() == SortConstant.SortKey)
            {
                var toSortResult = input.ToSort();
                if (toSortResult.Errors?.Count > 0 )
                {
                    return Result.Fail(toSortResult.Errors);
                }

                sorts = [..toSortResult.Value];
            }
            else
            {
                var toFilterResult = input.ToFilter();
                if (toFilterResult.Errors?.Count > 0 )
                {
                    return Result.Fail(toFilterResult.Errors);
                }
                filters.Add(toFilterResult.Value);
            }
        }

        if (sorts.Count == 0)
        {
            sorts.Add(new Sort(entity.PrimaryKey, SortOrder.Asc));
        }
        var (_, _, validSort, validSortErr) = await sorts.ToValidSorts(entity, entitySchemaSvc);
        if (validSortErr is not null)
        {
            return Result.Fail(validSortErr);
        }

        return ([..validSort], [..filters]);
    }

    private async Task<Result<ImmutableArray<GraphAttribute>>> SelectionSetToNode(
        string prefix,
        LoadedEntity entity,
        IEnumerable<GraphQLField> graphQlFields,
        CancellationToken token)
    {

        List<GraphAttribute> attributes = [];
        foreach (var field in graphQlFields)
        {
            var (_, _, graphAttr, err) = await LoadAttribute(entity, field.Name.StringValue, token);
            if (err is not null)
            {
                return Result.Fail(err);
            }

            graphAttr = graphAttr with { Prefix = prefix };

            (_, _, graphAttr, err) = await LoadSelection(graphAttr.FullPathName(prefix), graphAttr, field, token);
            if (err is not null)
            {
                return Result.Fail(err);
            }

            if (graphAttr.Type == DisplayType.Crosstable && field.Arguments is not null)
            {
                var target = graphAttr.Crosstable!.TargetEntity;
                var parseRes = await GetSortAndFilter(target, field.Arguments.Select(x => new GraphQlArgumentValueProvider(x)));
                if (parseRes.Errors is not null)
                {
                    return Result.Fail(parseRes.Errors);
                }

                var (s, f) = parseRes.Value;
                graphAttr = graphAttr with { Filters = f,Sorts =s };
            }

            attributes.Add(graphAttr);
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

        if (targetEntity is not null && field.SelectionSet is not null)
        {
            var (_, _, children, errors) =
                await SelectionSetToNode(prefix, targetEntity, field.SelectionSet.SubFields(), token);
            if (errors is not null)
            {
                return Result.Fail($"Fail to get subfield of {attr}, errors: {errors}");
            }

            attr = attr with { Selection = children };
        }

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

        if (find.Type is DisplayType.Crosstable or DisplayType.Lookup)
        {
            var (_, _, compoundAttr, loadRelatedErr) =
                await entitySchemaSvc.LoadOneCompoundAttribute(entity, find, [], token);
            if (loadRelatedErr is not null)
            {
                return Result.Fail(loadRelatedErr);
            }

            find = compoundAttr;
        }

        return find.ToGraph();
    }
}
