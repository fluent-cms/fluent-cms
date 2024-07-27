using System.Text.Json;
using FluentCMS.Models;
using FluentResults;
using SqlKata;
using Utils.DataDefinitionExecutor;
using Utils.QueryBuilder;

namespace FluentCMS.Services;
public partial class SchemaService
{
    private async Task VerifyIfSchemaIsView(Schema dto)
    {
        var view = dto.Settings.View;
        if (view is null) //not view, just ignore
        {
            return;
        }

        var entityName = Val.StrNotEmpty(view.EntityName).
            ValOrThrow($"entity name of {view.EntityName} should not be empty");
        view.Entity = Val.CheckResult(await GetEntityByNameOrDefault(entityName, true));
        
        foreach (var viewAttributeName in view.AttributeNames??[])
        {
            Val.NotNull(view.Entity.FindOneAttribute(viewAttributeName))
                .ValOrThrow($"not find attribute {viewAttributeName} of enity {entityName}");
        }

        var listAttributes = Val.CheckResult(view.LocalAttributes(InListOrDetail.InList));
        foreach (var viewSort in view.Sorts??[])
        {
            var find = listAttributes.FirstOrDefault(x=>x.Field == viewSort.FieldName);
            Val.NotNull(find).ValOrThrow($"sort field {viewSort.FieldName} should in list attributes");
        }

        var attr = view.Entity.LocalAttributes();
        foreach (var viewFilter in view.Filters??[])
        {
            var find = attr.FirstOrDefault(x => x.Field == viewFilter.FieldName);
            Val.NotNull(find).ValOrThrow($"filter field {viewFilter.FieldName} should in entity's attribute list");
        }
    }
    
    private async Task<Result> InitIfSchemaIsEntity(Schema dto)
    {
        var entity = dto.Settings.Entity;
        if (entity is null)//not a entity, ignore
        {
            return Result.Ok();
        }
        entity.Init();
        return await LoadRelated(entity);
    }

    private async Task VerifyEntity(Schema dto, ColumnDefinition[] cols, Entity entity)
    {
        var query = BaseQuery().Where(SchemaColumnName, dto.Name).WhereNot(SchemaColumnId, dto.Id);
        var existing = await kateQueryExecutor.Count(query);
        Val.CheckBool(existing ==0).ThrowFalse($"the schema name {dto.Name} exists");

        Val.CheckBool(cols.Length > 0 && dto.Id ==0 ).ThrowTrue($"the table name {entity.TableName} exists");
        foreach (var attribute in entity.GetAttributesByType(DisplayType.lookup))
        {
            await CheckLookup(attribute);
        }
    }

    private async Task EnsureEntityInTopMenuBar(Entity entity)
    {
        var menuBarSchema = await GetByIdOrName(SchemaName.TopMenuBar, false);
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
                        Url = link,
                        Label = entity.Title
                    }
                ];
            }
            await Save(menuBarSchema);
        }
    }

    private async Task<Result<Entity>> GetEntityByNameOrDefault(string name, bool loadRelated)
    {
        var query = BaseQuery().Where(SchemaColumnName, name).Where(SchemaColumnType, SchemaType.Entity);
        var item = ParseSchema(await kateQueryExecutor.One(query));

        var entity = item?.Settings.Entity;
        if (entity is null)
        {
            return Result.Fail($"Not find entity ${name}");
        }

        entity.Init();
        if (!loadRelated)
        {
            return entity;
        }

        var result = await LoadRelated(entity);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        return entity;
    }

    private async Task<Result> LoadRelated(Entity entity)
    {
        foreach (var attribute in entity.GetAttributesByType(DisplayType.lookup))
        {
            var lookupEntityName = attribute.GetLookupEntityName();
            if (lookupEntityName.IsFailed)
            {
                return Result.Fail(lookupEntityName.Errors);
            }

            var lookup = await GetEntityByNameOrDefault(lookupEntityName.Value, false);
            if (lookup.IsFailed)
            {
                return Result.Fail($"not find entity by name {lookupEntityName} for lookup {attribute.FullName()}");
            }

            attribute.Lookup = lookup.Value;
        }

        foreach (var attribute in entity.GetAttributesByType(DisplayType.crosstable))
        {
            var targetEntityName = attribute.GetCrossEntityName();
            if (targetEntityName.IsFailed)
            {
                return Result.Fail(targetEntityName.Errors);
            }

            var targetEntity = await GetEntityByNameOrDefault(targetEntityName.Value, false);
            if (targetEntity.IsFailed)
            {
                return Result.Fail($"not find entity by name {entity.Name} for crosstable {attribute.FullName()}");
            }

            attribute.Crosstable = new Crosstable(entity, targetEntity.Value);
        }
        return Result.Ok();
    }

    private async Task CreateCrosstable(Entity entity, global::Utils.QueryBuilder.Attribute attribute)
    {
        var entityName = Val.CheckResult(attribute.GetCrossEntityName());
        var targetEntity = Val.CheckResult(await GetEntityByNameOrDefault(entityName, false));
        var crossTable = new Crosstable(entity, targetEntity);
        var columns = await definitionExecutor.GetColumnDefinitions(crossTable.CrossEntity.TableName);
        if (columns.Length == 0)
        {
            await definitionExecutor.CreateTable(crossTable.CrossEntity.TableName, crossTable.GetColumnDefinitions());
        }
    }

    private async Task CheckLookup(global::Utils.QueryBuilder.Attribute attribute)
    {
        Val.CheckBool(attribute.DataType != DataType.Int)
            .ThrowTrue("lookup datatype should be int");
        var entityName = Val.CheckResult(attribute.GetLookupEntityName());
        Val.NotNull(await GetEntityByNameOrDefault(entityName, false))
            .ValOrThrow($"not find entity by name {entityName}");
    }

    private async Task SaveSchema(Schema dto)
    {
        if (dto.Id == 0)
        {
            var record = new Dictionary<string, object>
            { 
                {SchemaColumnName, dto.Name},
                {SchemaColumnType, dto.Type},
                {SchemaColumnSettings, JsonSerializer.Serialize(dto.Settings)},
            };
            var query = new Query(SchemaTableName).AsInsert(record,true);
            dto.Id = await kateQueryExecutor.Exec(query);
        }
        else
        {
            var query = new Query(SchemaTableName)
                .Where(SchemaColumnId, dto.Id)
                .AsUpdate(
                    [SchemaColumnName, SchemaColumnType, SchemaColumnSettings],
                    [dto.Name, dto.Type, JsonSerializer.Serialize(dto.Settings)]
                );
            await kateQueryExecutor.Exec(query);
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
            Name = (string)record[SchemaColumnName],
            Type = (string)record[SchemaColumnType],
            Settings = JsonSerializer.Deserialize<Settings>((string)record[SchemaColumnSettings])!,
            Id = record[SchemaColumnId] switch
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
}