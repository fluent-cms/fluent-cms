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
    ISchemaService schemaSvc,
    IEntitySchemaService entitySchemaSvc,
    KeyValueCache<LoadedQuery> queryCache
) : IQuerySchemaService
{
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
        var fields = CheckResult(GraphQlExt.GetRootGraphQlFields(query.SelectionSet));
        var selection = CheckResult(await SelectionSetToNode("", entity, fields, token));
        var sorts = CheckResult(await query.Sorts.ToValidSorts(entity, entitySchemaSvc));
        return query.ToLoadedQuery(entity, selection, sorts );
    }

    public async Task<Schema> Save(Schema schema, CancellationToken cancellationToken)
    {
        await VerifyQuery(schema.Settings.Query, cancellationToken);
        var ret = await schemaSvc.Save(schema, cancellationToken);
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
        CheckResult(await query.Filters.Verify(entity, entitySchemaSvc,entitySchemaSvc));
        
        var fields = CheckResult(GraphQlExt.GetRootGraphQlFields(query.SelectionSet));
        var selection = CheckResult(await SelectionSetToNode("", entity, fields, token));
        var sorts = CheckResult(await query.Sorts.ToValidSorts(entity, entitySchemaSvc));
        //todo: subfields' can only order by local attribute for now.
        //maybe support order subfields' subfield later
        CheckSorts(selection,sorts,true);
    }

    private void CheckSorts(ImmutableArray<GraphAttribute> attributes, IEnumerable<ValidSort> sorts, bool allowRecursive)
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
            CheckSorts(attr.Selection, attr.Sorts,false);
        }
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

            (_, _, graphAttr, err) = await LoadSelection(graphAttr.FullPathName(prefix) , graphAttr, field, token);
            if (err is not null)
            {
                return Result.Fail(err);
            }

            if (graphAttr.Type == DisplayType.Crosstable)
            {
                var target = graphAttr.Crosstable!.TargetEntity;

                (_, _, graphAttr, err) = await LoadSorts(target,graphAttr, field.Arguments, target.PrimaryKey);
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

    private static Result<GraphAttribute> LoadFilters(GraphAttribute graphAttr, GraphQLArguments? arguments)
    {
        var filters = new List<Filter>();
        if (arguments is null) return graphAttr;

        foreach (var arg in arguments)
        {
            if (arg.Name == SortConstant.SortKey)
            {
                continue;
            }

            var fieldName = arg.Name.StringValue;

            switch (arg.Value)
            {
                case GraphQLStringValue stringValue:
                    AddFilter(fieldName, stringValue.Value.ToString());
                    break;
                case GraphQLIntValue intValue:
                    AddFilter(fieldName, intValue.Value.ToString());
                    break;

                case GraphQLEnumValue enumValue:
                    AddFilter(fieldName, enumValue.Name.StringValue);
                    break;
                case GraphQLObjectValue argObjVal:
                    var (_, _, filter, errors) = ObjectToFilter(fieldName, argObjVal);
                    if (errors is not null)
                    {
                        return Result.Fail([new Error($"Failed to resolve filter for {fieldName}"), ..errors]);
                    }

                    filters.Add(filter);
                    break;
                default:
                    return Result.Fail($"invalid value type for {fieldName}");
            }
        }

        return graphAttr with { Filters = [..filters] };

        void AddFilter(string fieldName, string val)
        {
            var constraint = new Constraint(Matches.EqualsTo, val);
            filters.Add(new Filter(fieldName, LogicalOperators.And, [constraint], false));
        }
    }

    //sort: id or sort: {id:desc, name:asc}
    private async Task<Result<GraphAttribute>> LoadSorts(LoadedEntity entity,GraphAttribute graphAttr, GraphQLArguments? arguments,
        string primaryKey)
    {

        var sorts = new List<Sort>();
        if (arguments is not null)
        {
            foreach (var arg in arguments)
            {
                if (arg.Name != SortConstant.SortKey) continue;
                switch (arg.Value)
                {
                    case GraphQLStringValue stringValue:
                        sorts.Add(new Sort(stringValue.Value.ToString(), SortOrder.Asc));
                        break;
                    case GraphQLEnumValue enumValue:
                        sorts.Add(new Sort(enumValue.Name.StringValue, SortOrder.Asc));
                        break;
                    case GraphQLObjectValue obj:
                        //{id:desc, name:asc}
                        var (_, _, pairs, error) = obj.ToPairs();
                        if (error is not null)
                        {
                            return Result.Fail(error);
                        }
                        foreach (var (k, v) in pairs)
                        {
                            sorts.Add(new Sort(k, v.ToString()!));
                        }
                        break;
                    default:
                        return Result.Fail("invalid value type for sorts");
                }
            }
        }

        if (sorts.Count == 0)
        {
            //sort by primary key default
            sorts.Add(new Sort(primaryKey, SortOrder.Asc));
        }

        var validSorts = CheckResult(await sorts.ToValidSorts(entity, entitySchemaSvc)); 
        return graphAttr with { Sorts = validSorts};
    }

    private async Task<Result<GraphAttribute>> LoadAttribute(LoadedEntity entity, string fldName, CancellationToken cancellationToken)
    {
        var find = entity.Attributes.FindOneAttr(fldName);
        if (find is null)
        {
            return Result.Fail($"Parsing `SectionSet` fail, can not find {fldName} in {entity.Name}");
        }

        var (_, _, loadedAttr, loadRelatedErr) = await entitySchemaSvc.LoadOneRelated(entity, find, cancellationToken);
        if (loadRelatedErr is not null)
        {
            return Result.Fail(loadRelatedErr);
        }

        return loadedAttr.ToGraph();
    }
}
