using System.Collections.Immutable;
using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.GraphQlExt;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQLParser.AST;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class QuerySchemaService(
    ISchemaService schemaService,
    IEntitySchemaService entitySchemaService,
    KeyValueCache<LoadedQuery> queryCache
) : IQuerySchemaService
{
    public async Task<LoadedQuery> GetByNameAndCache(string name, CancellationToken cancellationToken = default)
    {
        var query = await queryCache.GetOrSet(name, async () => await GetByName(name, cancellationToken));
        return NotNull(query).ValOrThrow($"can not find query [{name}]");
    }

    private async Task<LoadedQuery> GetByName(string name, CancellationToken cancellationToken)
    {
        StrNotEmpty(name).ValOrThrow("query name should not be empty");
        var item = NotNull(await schemaService.GetByNameDefault(name, SchemaType.Query, cancellationToken))
            .ValOrThrow($"can not find query by name {name}");
        var query = NotNull(item.Settings.Query).ValOrThrow("invalid view format");
        var entity = CheckResult(await entitySchemaService.GetLoadedEntity(query.EntityName, cancellationToken));
        var fields = CheckResult(GraphQlExt.GetRootGraphQlFields(query.SelectionSet));
        var attributes = CheckResult(await SelectionSetToNode("", entity, fields, cancellationToken));
        var sorts = CheckResult(await (query.Sorts??[]).ToValidSorts(entity, entitySchemaService.ResolveAttributeVector));
        return query.ToLoadedQuery(entity, attributes, sorts );
    }

    public async Task<Schema> Save(Schema schema, CancellationToken cancellationToken)
    {
        await VerifyQuery(schema.Settings.Query, cancellationToken);
        var ret = await schemaService.Save(schema, cancellationToken);
        var query = await GetByName(schema.Name, cancellationToken);
        queryCache.Remove(query.Name);
        return ret;
    }

    private async Task VerifyQuery(Query? query, CancellationToken cancellationToken)
    {
        if (query is null)
        {
            throw new InvalidParamException("query is null");
        }

        var entity = CheckResult(await entitySchemaService.GetLoadedEntity(query.EntityName, cancellationToken));
        var fields = CheckResult(GraphQlExt.GetRootGraphQlFields(query.SelectionSet));
        CheckResult(await SelectionSetToNode("", entity, fields, cancellationToken));
        CheckResult(await (query.Sorts ?? []).ToValidSorts(entity, entitySchemaService.ResolveAttributeVector));
        CheckResult(await (query.Filters ?? []).ToValid(entity, null, entitySchemaService.ResolveAttributeVector));
    }

    private async Task<Result<ImmutableArray<GraphAttribute>>> SelectionSetToNode(
        string prefix,
        LoadedEntity entity,
        IEnumerable<GraphQLField> graphQlFields,
        CancellationToken cancellationToken)
    {

        List<GraphAttribute> attributes = new();
        foreach (var field in graphQlFields)
        {
            var (_, _, graphAttr, err) = await LoadAttribute(entity, field.Name.StringValue, cancellationToken);
            graphAttr = graphAttr with { Prefix = prefix };
            if (err is not null)
            {
                return Result.Fail(err);
            }

            (_, _, graphAttr, err) = await LoadSelection(graphAttr.FullPathName(prefix) , graphAttr, field, cancellationToken);
            if (err is not null)
            {
                return Result.Fail(err);
            }

            if (graphAttr.Type == DisplayType.Crosstable && field.Arguments is not null)
            {

                (_, _, graphAttr, err) = LoadSorts(graphAttr, field.Arguments, graphAttr.Crosstable!.TargetEntity.PrimaryKey);
                if (err is not null)
                {
                    return Result.Fail(err);
                }

                (_, _, graphAttr, err) =  LoadFilters(graphAttr, field.Arguments);
                if (err is not null)
                {
                    return Result.Fail(err);
                }
            }
            attributes.Add(graphAttr);
        }

        return attributes.ToImmutableArray();
    }

    private static Result<Filter> ObjectToFilter(string fieldName, GraphQLObjectValue argObjVal)
    {

        //name: {omitFail:true, gt:2, lt:5, operator: and}
        //name: {omitFail:false, eq:3, eq:4, operator: or}
        var omitFail = false;
        var logicalOperator = LogicalOperators.And;
        var constraints = new List<Constraint>();
        var (_, _, pairs, err) = argObjVal.ToPairs();
        if (err is not null)
        {
            return Result.Fail(err);
        }

        foreach (var (key, val) in pairs)
        {
            switch (key)
            {
                case FilterConstants.LogicalOperatorKey:
                    if (val is not string strVal)
                    {
                        return Result.Fail("invalid filter logical operator");
                    }

                    logicalOperator = strVal;
                    break;
                case FilterConstants.OmitFailKey:
                    if (val is not bool boolVal)
                    {
                        return Result.Fail("invalid filter omit fail setting");
                    }

                    omitFail = boolVal;
                    break;
                default:
                    constraints.Add(new Constraint(key, val.ToString()!));
                    break;
            }
        }
        return new Filter(fieldName, logicalOperator,[..constraints],omitFail);
    }

    private static Result<GraphAttribute> LoadFilters(GraphAttribute graphAttr, GraphQLArguments arguments)
    {
        var filters = new List<Filter>();
        foreach (var arg in arguments)
        {
            if (arg.Name == SortConstant.SortKey)
            {
                continue;
            }

            var fieldName = arg.Name.StringValue;

            switch (arg.Value)
            {
                case GraphQLEnumValue argStrVal:
                    //name: 3
                    var constraint = new Constraint(Matches.EqualsTo, argStrVal.Name.StringValue);
                    filters.Add(new Filter(fieldName, LogicalOperators.And, [constraint], false));
                    break;
                case GraphQLObjectValue argObjVal:
                    var (_, _, filter, errors) = ObjectToFilter(fieldName, argObjVal);
                    if (errors is not null)
                    {
                        return Result.Fail([new Error($"Failed to resolve filter for {fieldName}"),..errors]);
                    }
                    filters.Add(filter);
                    break;
                default:
                    return Result.Fail("invalid filter");
            }
        }

        return graphAttr with { Filters = [..filters] };
    }

    //sort: id or sort: {id:desc, name:asc}
    private static Result<GraphAttribute> LoadSorts(GraphAttribute graphAttr, GraphQLArguments arguments, string primaryKey)
    {

        var sorts = new List<Sort>();
        foreach (var arg in arguments)
        {
            if (arg.Name != SortConstant.SortKey) continue;
            switch (arg.Value)
            {
                case GraphQLEnumValue str:
                    sorts.Add(new Sort(str.Name.StringValue, SortOrder.Asc));
                    break;
                case GraphQLObjectValue obj:
                    //{id:desc, name:asc}
                    var (_, _, pairs, error) = obj.ToPairs();
                    if (error is not null)
                    {
                        return Result.Fail(error);
                    }
                    foreach (var (k,v) in pairs)
                    {
                        sorts.Add(new Sort(k, v.ToString()!));
                    }
                    break;
            }
        }

        if (sorts.Count == 0)
        {
            //sort by primary key  default
            sorts.Add(new Sort(primaryKey, SortOrder.Asc));
        }

        return graphAttr with { Sorts = [..sorts] };
    }

    private async Task<Result<GraphAttribute>> LoadSelection(string prefix, GraphAttribute graphAttr, GraphQLField graphQlField, CancellationToken cancellationToken)
    {
        var targetEntity = graphAttr.Type switch
        {
            DisplayType.Crosstable => graphAttr.Crosstable!.TargetEntity,
            DisplayType.Lookup => graphAttr.Lookup,
            _ => null
        };

        if (targetEntity is not null && graphQlField.SelectionSet is not null)
        {
            var (_, _, children, errors) =
                await SelectionSetToNode(prefix, targetEntity,graphQlField.SelectionSet.SubFields(), cancellationToken);
            if (errors is not null)
            {
                return Result.Fail($"Fail to get subfield of {graphAttr}, errors: {errors}");
            }

            graphAttr = graphAttr with { Selection = children };
        }

        return graphAttr;
    }

    private async Task<Result<GraphAttribute>> LoadAttribute(LoadedEntity entity, string fldName, CancellationToken cancellationToken)
    {
        var find = entity.Attributes.FindOneAttribute(fldName);
        if (find is null)
        {
            return Result.Fail($"Verifying `SectionSet` fail, can not find {fldName} in {entity.Name}");
        }

        var (_, _, loadedAttr, loadRelatedErr) = await entitySchemaService.LoadOneRelated(entity, find, cancellationToken);
        if (loadRelatedErr is not null)
        {
            return Result.Fail(loadRelatedErr);
        }

        return loadedAttr.ToGraph();
    }

}
