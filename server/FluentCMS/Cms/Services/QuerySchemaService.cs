using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.Cache;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQLParser;
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

    private async Task<LoadedQuery> GetByName(string name,CancellationToken cancellationToken)
    {
        StrNotEmpty(name).ValOrThrow("query name should not be empty");
        var item = NotNull(await schemaService.GetByNameDefault(name, SchemaType.Query, cancellationToken))
            .ValOrThrow($"can not find query by name {name}");
        var query = NotNull(item.Settings.Query)
            .ValOrThrow("invalid view format");
        var entityName = StrNotEmpty(query.EntityName)
            .ValOrThrow($"referencing entity was not set for {query.EntityName}");

        var entity = CheckResult(await entitySchemaService.GetLoadedEntity(entityName, cancellationToken));
        var attributes = CheckResult(await InitViewSelection(query,entity, cancellationToken));
        return query.ToLoadedQuery(entity, attributes);
    }

    public async Task<Schema> Save(Schema schema, CancellationToken cancellationToken)
    {
        await VerifyQuery(schema.Settings.Query, cancellationToken);
        var ret = await schemaService.Save(schema, cancellationToken);
        var query =await GetByName(schema.Name, cancellationToken);
        queryCache.Remove(query.Name);
        return ret;
    }

    private async Task<Result<LoadedAttribute[]>> InitViewSelection(Query query, LoadedEntity entity, CancellationToken cancellationToken)
    {
        var document = Parser.Parse(query.SelectionSet);
        List<LoadedAttribute> fieldNodes = new();

        foreach (var definition in document.Definitions)
        {
            if (definition is not GraphQLOperationDefinition operation)
                return Result.Fail("invalid selection definition, definition is not operation");
            var nodeResult = await SelectionSetToNode(operation.SelectionSet, entity, cancellationToken);
            if (nodeResult.IsFailed)
            {
                return Result.Fail(nodeResult.Errors);
            }

            fieldNodes.AddRange(nodeResult.Value);
        }

        return fieldNodes.ToArray();
    }


    private async Task<Result<LoadedAttribute[]>> SelectionSetToNode(GraphQLSelectionSet? selectionSet, LoadedEntity entity,
        CancellationToken cancellationToken)
    {
        if (selectionSet == null) return new Result<LoadedAttribute[]>();

        List<LoadedAttribute> attributes = new();
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not GraphQLField field) continue;
            var attrRes = await FieldToAttribute(field);
            if (attrRes.IsFailed)
            {
                return Result.Fail(attrRes.Errors);
            }
            attributes.Add(attrRes.Value);
        }
        return attributes.ToArray();

        async Task<Result<LoadedAttribute>> FieldToAttribute(GraphQLField field)
        {
            var fldName = field.Name.StringValue;
            var attribute = entity.Attributes.FindOneAttribute(fldName);
            if (attribute is null)
                return Result.Fail($"Verifying `SectionSet` fail, can not find {fldName} in {entity.Name}");
            
            LoadedEntity? lookup = default;
            Crosstable? crosstable = default;
            LoadedAttribute[] children = [];
            var  childRes = Result.Ok();
            
            switch (attribute.Type)
            {
                case DisplayType.Crosstable:
                    var targetEntity =
                        await entitySchemaService.GetLoadedEntity(attribute.GetLookupTarget(), cancellationToken);
                    if (targetEntity.IsFailed)
                    {
                        return Result.Fail(
                            $"not find entity by name {attribute.GetLookupTarget()} for crosstable {attribute.Fullname}");
                    }

                    crosstable = CrosstableHelper.Crosstable(entity, targetEntity.Value);
                    childRes = await LoadChildren(targetEntity.Value);
                    break;
                case DisplayType.Lookup:
                    var lookupRes = await entitySchemaService.GetLoadedEntity(attribute.GetLookupTarget(), cancellationToken);
                    if (lookupRes.IsFailed)
                    {
                        return Result.Fail(
                            $"not find entity by name {attribute.GetLookupTarget()} for lookup {attribute.Fullname}");
                    }

                    lookup = lookupRes.Value;
                    childRes = await LoadChildren(lookup);
                    break;
            }

            if (childRes.IsFailed)
            {
                return Result.Fail(childRes.Errors);
            }

            return attribute with { Lookup = lookup, Crosstable = crosstable, Children = children };

            async Task<Result> LoadChildren(LoadedEntity ett)
            {
                var res =
                    await SelectionSetToNode(field.SelectionSet, ett, cancellationToken);
                if (res.IsFailed)
                {
                    return Result.Fail(res.Errors);
                }
                children = res.Value;
                return Result.Ok();
            }
        }
    }

    private async Task VerifyQuery(Query? query, CancellationToken cancellationToken)
    {
        if (query is null)
        {
            throw new InvalidParamException("query is null");
        }

        var entityName = StrNotEmpty(query.EntityName)
            .ValOrThrow($"entity name of {query.EntityName} should not be empty");
        var entity =
            CheckResult(await entitySchemaService.GetLoadedEntity(entityName, cancellationToken));
        
        var attributes =CheckResult(await InitViewSelection(query, entity, cancellationToken));

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
}