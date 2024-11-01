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
        var attributes = CheckResult(await SelectionSetToNode(fields, entity, cancellationToken));
        return query.ToLoadedQuery(entity, attributes);
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
        CheckResult(await SelectionSetToNode(fields, entity, cancellationToken));
        CheckResult(await (query.Sorts ?? []).ToValidSorts(entity, entitySchemaService.ResolveAttributeVector));
        CheckResult(await (query.Filters ?? []).Resolve(entity, null, entitySchemaService.ResolveAttributeVector));
    }

    private async Task<Result<ImmutableArray<GraphAttribute>>> SelectionSetToNode(
        IEnumerable<GraphQLField> graphQlFields,
        LoadedEntity entity,
        CancellationToken cancellationToken)
    {

        List<GraphAttribute> attributes = new();
        foreach (var field in graphQlFields)
        {
            var (_, _, graphAttr, err) = await LoadAttribute(entity, field.Name.StringValue,cancellationToken);
            if (err is not null)
            {
                return Result.Fail(err);
            }

            (_, _, graphAttr, err) = await LoadSelection(graphAttr, field,cancellationToken);
            if (err is not null)
            {
                return Result.Fail(err);
            }
            
            (_, _, graphAttr, err) = await LoadSorts(graphAttr, field,cancellationToken);
            if (err is not null)
            {
                return Result.Fail(err);
            }

            attributes.Add(graphAttr);
        }

        return attributes.ToImmutableArray();
    }

    async Task<Result<GraphAttribute>> LoadSorts(GraphAttribute graphAttr, GraphQLField graphQlField, CancellationToken cancellationToken)
    {
        if (graphAttr.Type != DisplayType.Crosstable)
        {
            return graphAttr;
        }

        if (graphQlField.Arguments is null)
        {
            return graphAttr;
        }

        var sorts = new List<Sort>(); 
        foreach (var arg in graphQlField.Arguments)
        {
            if (arg.Name != SortConstant.SortKey) continue;
            if (arg.Value is not GraphQLListValue listValue || listValue.Values is null)
            {
                continue;
            }

            foreach (var item in listValue.Values)
            {
                if (item is not GraphQLObjectValue { Fields: not null } objectValue) continue;
                foreach (var field in objectValue.Fields)
                {
                    if (!(field.Value is GraphQLEnumValue enumValue && (enumValue.Name == SortOrder.Asc || enumValue.Name == SortOrder.Desc)))
                    {
                        return Result.Fail($"Fail to parse sort direction, it should be {SortOrder.Asc} or {SortOrder.Desc}");
                    }
                    sorts.Add(new Sort(field.Name.StringValue, enumValue.Name.StringValue));
                }
            }
        }

        var (_,_,validSort, errors) = await sorts.ToValidSorts(graphAttr.Crosstable!.TargetEntity, entitySchemaService.ResolveAttributeVector);
        if (errors is not null)
        {
            return Result.Fail(errors);
        }
        return graphAttr with{Sorts = [..validSort]};
    }
    async Task<Result<GraphAttribute>> LoadSelection(GraphAttribute graphAttr, GraphQLField graphQlField, CancellationToken cancellationToken)
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
                await SelectionSetToNode(graphQlField.SelectionSet.SubFields(), targetEntity, cancellationToken);
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
