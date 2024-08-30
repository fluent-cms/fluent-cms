using System.Text.Json;
using FluentCMS.Cms.Models;
using FluentCMS.Services;
using FluentResults;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;
using GraphQLParser;
using GraphQLParser.AST;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;
using Query = FluentCMS.Utils.QueryBuilder.Query;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;
public static class  SchemaType
{
    public const string Menu = "menu";
    public const string Entity = "entity";
    public const string Query = "query";
}

public static class SchemaName
{
    public const string TopMenuBar = "top-menu-bar";
}
public partial class SchemaService
{
    private const string TableName = "__schemas";
    private const string ColumnId = "id";
    private const string ColumnName = "name";
    private const string ColumnType = "type";
    private const string ColumnSettings = "settings";
    private const string ColumnDeleted = "deleted";
    private const string ColumnCreatedBy = "created_by";

    private static string[] Fields() => [ColumnId, ColumnName, ColumnType, ColumnSettings, ColumnCreatedBy];

    private static SqlKata.Query BaseQuery()
    {
        return new SqlKata.Query(TableName).Select(Fields()).Where(ColumnDeleted, false);
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
                        var res = await LoadCrosstable(entity,attribute, cancellationToken);
                        if (res.IsFailed)
                        {
                            return Result.Fail(res.Errors);
                        }
                    }
                    var crossTableChildren =
                        await SelectionSetToNode(field.SelectionSet, attribute.Crosstable!.TargetEntity, cancellationToken);
                    if (crossTableChildren.IsFailed)
                    {
                        return Result.Fail(crossTableChildren.Errors);
                    }

                    attribute.Children = crossTableChildren.Value;
                    break;
                case DisplayType.lookup:
                    if (attribute.Lookup is null)
                    {
                        var res = await LoadLookup(attribute, cancellationToken);
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

    private async Task VerifyIfSchemaIsView(Schema dto, CancellationToken cancellationToken)
    {
        var view = dto.Settings.Query;
        if (view is null) //not view, just ignore
        {
            return;
        }

        var entityName = StrNotEmpty(view.EntityName).
            ValOrThrow($"entity name of {view.EntityName} should not be empty");
        view.Entity = CheckResult(await GetEntityByNameOrDefault(entityName, true,cancellationToken));
        CheckResult(await InitViewSelection(view, cancellationToken));
        
        var listAttributes = view.Selection.GetLocalAttributes();
        foreach (var viewSort in view.Sorts)
        {
            var find = listAttributes.FirstOrDefault(x=>x.Field == viewSort.FieldName);
            NotNull(find).ValOrThrow($"sort field {viewSort.FieldName} should in list attributes");
        }

        var attr = view.Entity.Attributes.GetLocalAttributes();
        foreach (var viewFilter in view.Filters)
        {
            var find = attr.FirstOrDefault(x => x.Field == viewFilter.FieldName);
            NotNull(find).ValOrThrow($"filter field {viewFilter.FieldName} should in entity's attribute list");
        }

    }
    
    private async Task<Result> InitIfSchemaIsEntity(Schema dto, CancellationToken cancellationToken)
    {
        var entity = dto.Settings.Entity;
        if (entity is null)//not a entity, ignore
        {
            return Result.Ok();
        }
        entity.Init();
        return await LoadRelated(entity,cancellationToken);
    }

    private async Task VerifyEntity(Schema dto, ColumnDefinition[] cols, Entity entity, CancellationToken cancellationToken)
    {
        CheckResult(await NameNotTakenByOther(dto, cancellationToken));
        CheckResult(TableExistsWhenCreatingNewEntity());
        CheckResult(TitleAttributeExists());
        
        foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.lookup))
        {
            await CheckLookup(attribute,cancellationToken);
        }

        Result TitleAttributeExists()
        {
            var attribute = entity.DisplayTitleAttribute();
            return attribute is null ? Result.Fail($"`{entity.TitleAttribute}` was not in attributes list") : Result.Ok();
        }
        
        Result TableExistsWhenCreatingNewEntity()
        {
            var creatingNewEntity = dto.Id == 0;
            var tableExists = cols.Length > 0;
            return creatingNewEntity && tableExists ? Result.Fail($"Fail to add new entity, the table {entity.TableName} aleady exists") : Result.Ok();
        } 
    }

    private async Task<Result> NameNotTakenByOther(Schema schema, CancellationToken cancellationToken)
    {
        var query = BaseQuery().Where(ColumnName, schema.Name).WhereNot(ColumnId, schema.Id);
        var count = await kateQueryExecutor.Count(query, cancellationToken);
        return count == 0? Result.Ok(): Result.Fail($"the schema name {schema.Name} was taken by other schema");
    }

    private async Task EnsureEntityInTopMenuBar(Entity entity, CancellationToken cancellationToken)
    {
        var menuBarSchema = await GetByNameVerify(SchemaName.TopMenuBar, false,cancellationToken);
        var menuBar = menuBarSchema.Settings.Menu;
        if (menuBar is not null)
        {
            var link = "/entities/" + entity.Name;
            var menuItem = menuBar.MenuItems.FirstOrDefault(me => me.Url == link);
            if (menuItem is null)
            {
                menuBar.MenuItems =
                [
                    ..menuBar.MenuItems, new MenuItem
                    {
                        Icon = "pi-bolt",
                        Url = link,
                        Label = entity.Title
                    }
                ];
            }
            await InternalSave(menuBarSchema,cancellationToken);
        }
    }

    private async Task<Result<Entity>> GetEntityByNameOrDefault(string name, bool loadRelated, CancellationToken cancellationToken)
    {
        var query = BaseQuery().Where(ColumnName, name).Where(ColumnType, SchemaType.Entity);
        var item = ParseSchema(await kateQueryExecutor.One(query,cancellationToken));

        var entity = item?.Settings.Entity;
        if (entity is null)
        {
            return Result.Fail($"Not find entity {name}");
        }

        entity.Init();
        if (!loadRelated)
        {
            return entity;
        }

        var result = await LoadRelated(entity,cancellationToken);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        return entity;
    }

    private async Task<Result> LoadLookup(Attribute attribute,CancellationToken cancellationToken)
    {
        var lookupEntityName = attribute.GetLookupEntityName();
        if (lookupEntityName.IsFailed)
        {
            return Result.Fail(lookupEntityName.Errors);
        }

        var lookup = await GetEntityByNameOrDefault(lookupEntityName.Value, false, cancellationToken);
        if (lookup.IsFailed)
        {
            return Result.Fail($"not find entity by name {lookupEntityName} for lookup {attribute.FullName()}");
        }
        attribute.Lookup = lookup.Value;
        return Result.Ok();
    }

    private async Task<Result> LoadCrosstable(Entity sourceEntity, Attribute attribute, CancellationToken cancellationToken)
    {
        var targetEntityName = attribute.GetCrossEntityName();
        if (targetEntityName.IsFailed)
        {
            return Result.Fail(targetEntityName.Errors);
        }

        var targetEntity = await GetEntityByNameOrDefault(targetEntityName.Value, false, cancellationToken);
        if (targetEntity.IsFailed)
        {
            return Result.Fail($"not find entity by name {targetEntityName} for crosstable {attribute.FullName()}");
        }
        attribute.Crosstable = new Crosstable(sourceEntity, targetEntity.Value);
        return Result.Ok();

    }

    private async Task<Result> LoadRelated(Entity entity, CancellationToken cancellationToken)
    {
        foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.lookup))
        {
            var res = await LoadLookup(attribute, cancellationToken);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
        }

        foreach (var attribute in entity.Attributes.GetAttributesByType(DisplayType.crosstable))
        {
            var res = await LoadCrosstable(entity,attribute, cancellationToken);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
        }
        return Result.Ok();
    }

    private async Task CreateCrosstable(Entity entity, Attribute attribute,CancellationToken cancellationToken)
    {
        var entityName = CheckResult(attribute.GetCrossEntityName());
        var targetEntity = CheckResult(await GetEntityByNameOrDefault(entityName, false,cancellationToken));
        var crossTable = new Crosstable(entity, targetEntity);
        var columns = await definitionExecutor.GetColumnDefinitions(crossTable.CrossEntity.TableName, cancellationToken);
        if (columns.Length == 0)
        {
            await definitionExecutor.CreateTable(crossTable.CrossEntity.TableName, crossTable.GetColumnDefinitions(),cancellationToken);
        }
    }

    private async Task CheckLookup(Attribute attribute, CancellationToken cancellationToken)
    {
        True(attribute.DataType == DataType.Int).ThrowNotTrue("lookup datatype should be int");
        var entityName = CheckResult(attribute.GetLookupEntityName());
        NotNull(await GetEntityByNameOrDefault(entityName, false,cancellationToken))
            .ValOrThrow($"not find entity by name {entityName}");
    }

    private async Task SaveSchema(Schema dto, CancellationToken cancellationToken)
    {
        if (dto.Id == 0)
        {
            var record = new Dictionary<string, object>
            { 
                {ColumnName, dto.Name},
                {ColumnType, dto.Type},
                {ColumnSettings, JsonSerializer.Serialize(dto.Settings)},
                {ColumnCreatedBy, dto.CreatedBy}
            };
            var query = new SqlKata.Query(TableName).AsInsert(record,true);
            dto.Id = await kateQueryExecutor.Exec(query,cancellationToken);
        }
        else
        {
            var query = new SqlKata.Query(TableName)
                .Where(ColumnId, dto.Id)
                .AsUpdate(
                    [ColumnName, ColumnType, ColumnSettings],
                    [dto.Name, dto.Type, JsonSerializer.Serialize(dto.Settings)]
                );
            await kateQueryExecutor.Exec(query,cancellationToken);
        }
    }

    private static Schema? ParseSchema(Record? record)
    {
        if (record is null)
        {
            return null;
        }
        record = record.ToDictionary(pair => pair.Key.ToLower(), pair => pair.Value);
        return  new Schema
        {
            Name = (string)record[ColumnName],
            Type = (string)record[ColumnType],
            Settings = JsonSerializer.Deserialize<Settings>((string)record[ColumnSettings])!,
            CreatedBy = (string)record[ColumnCreatedBy],
            Id = record[ColumnId] switch
            {
                int val => val,
                long val => (int)val,
                _ => 0
            }
        };
    }

    private static Schema[] ParseSchema(IEnumerable<Record> records)
    {
        return records.Select(x => ParseSchema(x)!).ToArray();
    }
    
    private async Task<Schema> InternalSave(Schema dto, CancellationToken cancellationToken = default)
    {
        CheckResult(await NameNotTakenByOther(dto, cancellationToken));
        await VerifyIfSchemaIsView(dto, cancellationToken);
        await SaveSchema(dto, cancellationToken);
        return dto;
    }
}