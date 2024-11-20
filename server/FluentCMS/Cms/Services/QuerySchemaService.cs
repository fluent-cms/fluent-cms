using System.Collections.Immutable;
using FluentCMS.Services;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.Graph;
using FluentCMS.Utils.QueryBuilder;
using FluentCMS.Utils.ResultExt;
using FluentResults;
using FluentResults.Extensions;
using GraphQLParser.AST;
using Query = FluentCMS.Utils.QueryBuilder.Query;
using Schema = FluentCMS.Cms.Models.Schema;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class QuerySchemaService(
    ISchemaService schemaSvc,
    IEntitySchemaService entitySchemaSvc,
    ExpiringKeyValueCache<LoadedQuery> queryCache
) : IQuerySchemaService
{
    public async Task<LoadedQuery> ByGraphQlRequest<T>(string entityName, IEnumerable<GraphQLField> fields, T[] args)
    where T : IObjectProvider,IPairProvider,IValueProvider
    {
        var entity = CheckResult(await entitySchemaSvc.GetLoadedEntity(entityName));
        var selection = CheckResult(await SelectionSetToNode("", entity, fields, default));
        var (sorts, filters)= CheckResult(GetSortAndFilter(args));
        foreach (var input in args)
        {
            CheckResult(input.Name() switch
            {
                FilterConstants.FilterExprKey => input.ToFilterExpr().BindAction(f=>filters = [..filters,..f]),
                _ => Result.Ok()
            });
        }
        var validSorts = CheckResult(await sorts.ToValidSorts(entity, entitySchemaSvc));
        return new LoadedQuery("GraphQL" + entityName , 
            entityName, 
            entity.DefaultPageSize, 
            selection,
            [..validSorts] , 
            [..filters], 
            entity);
    }

    public async Task<LoadedQuery> ByNameAndCache(string name, CancellationToken token = default)
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
        var fields = CheckResult(GraphParser.GetRootGraphQlFields(query.SelectionSet));
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

        var fields = CheckResult(GraphParser.GetRootGraphQlFields(query.SelectionSet));
        var selection = CheckResult(await SelectionSetToNode("", entity, fields, token));
        var sorts = CheckResult(await query.Sorts.ToValidSorts(entity, entitySchemaSvc));
        //todo: subfields' can only order by local attribute for now.
        //maybe support order subfields' subfield later
        CheckResult(sorts.Verify(selection, true));
    }

    private Task<Result<(ValidSort[], Filter[])>> GetSortAndFilterExpr<T>(LoadedEntity entity, IEnumerable<T> args)
    where T: IObjectProvider
    {
        throw new Exception("");
    }

    private Result<(Sort[], Filter[])> GetSortAndFilter<T>(IEnumerable<T> args)
        where T : IValueProvider, IPairProvider
    {
        var sorts = new List<Sort>();
        var filters = new List<Filter>();
        foreach (var input in args)
        {
            var name = input.Name();
            var res = name switch
            {
                SortConstant.SortKey => input.ToSorts().BindAction(s => sorts.AddRange(s)),
                FilterConstants.FilterExprKey => Result.Ok(),
                _ => input.ToFilter().BindAction(f => filters.Add(f)),
            };
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
        }
        return (sorts.ToArray(), filters.ToArray());
    }

    private async Task<Result<ValidSort[]>> ToValidSorts(LoadedEntity entity, IEnumerable<Sort> sorts)
    {
        var list = sorts.ToList();
        if (list.Count == 0)
        {
            list.Add(new Sort(entity.PrimaryKey, SortOrder.Asc));
        }
        return await list.ToArray().ToValidSorts(entity, entitySchemaSvc);
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
            var (ok, _, graphAttr, err) = await LoadAttribute(entity, field.Name.StringValue, token);
            if (!ok)
            {
                return Result.Fail(err);
            }

            graphAttr = graphAttr with { Prefix = prefix };

            (ok, _, graphAttr, err) = await LoadSelection(graphAttr.FullPathName(prefix), graphAttr, field, token);
            if (!ok)
            {
                return Result.Fail(err);
            }

            if (graphAttr.Type == DisplayType.Crosstable && field.Arguments is not null)
            {
                var target = graphAttr.Crosstable!.TargetEntity;
                var inputs = field.Arguments.Select(x => new GraphQlArgumentValueProvider(x));
                var (parseOk, _, (sorts, filters), parseErr) = GetSortAndFilter(inputs);
                if (!parseOk)
                {
                    return Result.Fail(parseErr);
                }

                var (validOk, _, validSorts, validErr) = await sorts.ToValidSorts(target, entitySchemaSvc);
                if (!validOk)
                {
                    return Result.Fail(validErr);
                }
                graphAttr = graphAttr with { Filters = [..filters],Sorts =[..validSorts] };
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
            var (_, failed, children, errors) =
                await SelectionSetToNode(prefix, targetEntity, field.SelectionSet.SubFields(), token);
            if (failed)
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
            var (_, failed, compoundAttr, err) =
                await entitySchemaSvc.LoadOneCompoundAttribute(entity, find, [], token);
            if (failed)
            {
                return Result.Fail(err);
            }

            find = compoundAttr;
        }

        return find.ToGraph();
    }
}
