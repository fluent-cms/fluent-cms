using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using GraphQLParser;
using GraphQLParser.AST;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;

public sealed class QuerySchemaService(
    ISchemaService schemaService,
    IEntitySchemaService entitySchemaService
) : IQuerySchemaService
{
    public async Task<Query> GetByName(string name, CancellationToken cancellationToken = default)
    {
        StrNotEmpty(name).ValOrThrow("query name should not be empty");
        var item = NotNull(await schemaService.GetByNameDefault(name, SchemaType.Query, cancellationToken))
            .ValOrThrow($"can not find query by name {name}");
        var view = NotNull(item.Settings.Query)
            .ValOrThrow("invalid view format");
        var entityName = StrNotEmpty(view.EntityName)
            .ValOrThrow($"referencing entity was not set for {view}");

        view.Entity =
            CheckResult(await entitySchemaService.GetByNameDefault(entityName, true, cancellationToken));
        CheckResult(await InitViewSelection(view, cancellationToken));
        return view;
    }

    public async Task<Schema> Save(Schema schema, CancellationToken cancellationToken)
    {
        await VerifyQuery(schema.Settings.Query, cancellationToken);
        return await schemaService.Save(schema, cancellationToken);
    }

    private async Task<Result> InitViewSelection(Query query, CancellationToken cancellationToken)
    {
        GraphQLDocument document = Parser.Parse(query.SelectionSet);
        List<Attribute> fieldNodes = new();

        foreach (var definition in document.Definitions)
        {
            if (definition is not GraphQLOperationDefinition operation)
                return Result.Fail("invalid selection definition, definition is not operation");
            var nodeResult = await SelectionSetToNode(operation.SelectionSet, query.Entity!, cancellationToken);
            if (nodeResult.IsFailed)
            {
                return Result.Fail(nodeResult.Errors);
            }

            fieldNodes.AddRange(nodeResult.Value);
        }

        query.Selection = fieldNodes.ToArray();
        return Result.Ok();
    }


    private async Task<Result<Attribute[]>> SelectionSetToNode(GraphQLSelectionSet? selectionSet, Entity entity,
        CancellationToken cancellationToken)
    {
        if (selectionSet == null) return new Result<Attribute[]>();

        List<Attribute> attributes = new();
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not GraphQLField field) continue;
            var fldName = field.Name.StringValue;
            var attribute = entity.Attributes.FindOneAttribute(fldName);
            if (attribute is null) return Result.Fail($"can not find {fldName} in {entity.Name}");
            switch (attribute.Type)
            {
                case DisplayType.crosstable:
                    if (attribute.Crosstable is null)
                    {
                        var res = await entitySchemaService.LoadCrosstable(entity, attribute, cancellationToken);
                        if (res.IsFailed)
                        {
                            return Result.Fail(res.Errors);
                        }
                    }

                    var crossTableChildren =
                        await SelectionSetToNode(field.SelectionSet, attribute.Crosstable!.TargetEntity,
                            cancellationToken);
                    if (crossTableChildren.IsFailed)
                    {
                        return Result.Fail(crossTableChildren.Errors);
                    }

                    attribute.Children = crossTableChildren.Value;
                    break;
                case DisplayType.lookup:
                    if (attribute.Lookup is null)
                    {
                        var res = await entitySchemaService.LoadLookup(attribute, cancellationToken);
                        if (res.IsFailed)
                        {
                            return Result.Fail(res.Errors);
                        }
                    }

                    var children =
                        await SelectionSetToNode(field.SelectionSet, attribute.Lookup!, cancellationToken);
                    if (children.IsFailed)
                    {
                        return Result.Fail(children.Errors);
                    }

                    attribute.Children = children.Value;
                    break;
            }

            attributes.Add(attribute);
        }

        return attributes.ToArray();
    }

    private async Task VerifyQuery(Query? query, CancellationToken cancellationToken)
    {
        if (query is null)
        {
            throw new InvalidParamException("query is null");
        }

        var entityName = StrNotEmpty(query.EntityName)
            .ValOrThrow($"entity name of {query.EntityName} should not be empty");
        query.Entity =
            CheckResult(await entitySchemaService.GetByNameDefault(entityName, true, cancellationToken));
        CheckResult(await InitViewSelection(query, cancellationToken));

        var listAttributes = query.Selection.GetLocalAttributes();
        foreach (var viewSort in query.Sorts)
        {
            var find = listAttributes.FirstOrDefault(x => x.Field == viewSort.FieldName);
            NotNull(find).ValOrThrow($"sort field {viewSort.FieldName} should in list attributes");
        }

        var attr = query.Entity.Attributes.GetLocalAttributes();
        foreach (var viewFilter in query.Filters)
        {
            var find = attr.FirstOrDefault(x => x.Field == viewFilter.FieldName);
            NotNull(find).ValOrThrow($"filter field {viewFilter.FieldName} should in entity's attribute list");
        }
    }
}