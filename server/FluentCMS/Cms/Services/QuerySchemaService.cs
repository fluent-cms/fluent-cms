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
        var attributes = CheckResult(await SelectionSetToNode(fields, entity, cancellationToken));

        var listAttributes = attributes.GetLocalAttributes();
        foreach (var viewSort in query.Sorts)
        {
            var find = listAttributes.FirstOrDefault(x => x.Field == viewSort.FieldName);
            NotNull(find).ValOrThrow($"sort field {viewSort.FieldName} should in list attributes");
        }

        var attr = attributes.GetLocalAttributes();
        foreach (var viewFilter in query.Filters)
        {
            var find = attr.FirstOrDefault(x => x.Field == viewFilter.FieldName);
            NotNull(find).ValOrThrow($"filter field {viewFilter.FieldName} should in entity's attribute list");
        }
    }

    private async Task<Result<ImmutableArray<LoadedAttribute>>> SelectionSetToNode(
        IEnumerable<GraphQLField> graphQlFields,
        LoadedEntity entity,
        CancellationToken ct)
    {

        List<LoadedAttribute> attributes = new();
        foreach (var field in graphQlFields)
        {
            var fldName = field.Name.StringValue;
            var attribute = entity.Attributes.FindOneAttribute(fldName);
            if (attribute is null)
                return Result.Fail($"Verifying `SectionSet` fail, can not find {fldName} in {entity.Name}");

            var res =
                await entitySchemaService.LoadOneRelated(entity, attribute, ct); 
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }

            attribute = res.Value;
            var targetEntity =  attribute.Type switch
            {
                DisplayType.Crosstable => attribute.Crosstable!.TargetEntity,
                DisplayType.Lookup => attribute.Lookup,
                _ => null
            };
            
            if (targetEntity is not null && field.SelectionSet is not null)
            {
                var children = await SelectionSetToNode(field.SelectionSet.SubFields(), targetEntity, ct);
                if (children.IsFailed)
                {
                    return Result.Fail($"Fail to get subfield of {attribute.GetFullName()}");
                }

                attribute = attribute with { Children = children.Value };
            }
            attributes.Add(attribute);
        }

        return attributes.ToImmutableArray();
    }
}
