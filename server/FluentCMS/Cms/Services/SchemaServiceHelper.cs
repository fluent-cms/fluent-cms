using System.Text.Json;
using FluentCMS.Models;
using FluentCMS.Services;
using FluentResults;
using SqlKata;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Services;
using static InvalidParamExceptionFactory;
public static class  SchemaType
{
    public const string Menu = "menu";
    public const string Entity = "entity";
    public const string View = "view";
}

public static class SchemaName
{
    public const string TopMenuBar = "top-menu-bar";
}
public partial class SchemaService
{
    private const string SchemaTableName = "__schemas";
    private const string SchemaColumnId = "id";
    private const string SchemaColumnName = "name";
    private const string SchemaColumnType = "type";
    private const string SchemaColumnSettings = "settings";
    private const string SchemaColumnDeleted = "deleted";
    private const string SchemaColumnCreatedBy = "created_by";

    private static string[] Fields() => [SchemaColumnId, SchemaColumnName, SchemaColumnType, SchemaColumnSettings, SchemaColumnCreatedBy];

    private static Query BaseQuery()
    {
        return new Query(SchemaTableName).Select(Fields()).Where(SchemaColumnDeleted, false);
    }
    private async Task VerifyIfSchemaIsView(Schema dto, CancellationToken cancellationToken)
    {
        var view = dto.Settings.View;
        if (view is null) //not view, just ignore
        {
            return;
        }

        var entityName = StrNotEmpty(view.EntityName).
            ValOrThrow($"entity name of {view.EntityName} should not be empty");
        view.Entity = CheckResult(await GetEntityByNameOrDefault(entityName, true,cancellationToken));
        
        foreach (var viewAttributeName in view.AttributeNames??[])
        {
            NotNull(view.Entity.FindOneAttribute(viewAttributeName))
                .ValOrThrow($"not find attribute {viewAttributeName} of enity {entityName}");
        }

        var listAttributes = CheckResult(view.LocalAttributes(InListOrDetail.InList));
        foreach (var viewSort in view.Sorts??[])
        {
            var find = listAttributes.FirstOrDefault(x=>x.Field == viewSort.FieldName);
            NotNull(find).ValOrThrow($"sort field {viewSort.FieldName} should in list attributes");
        }

        var attr = view.Entity.LocalAttributes();
        foreach (var viewFilter in view.Filters??[])
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
        var query = BaseQuery().Where(SchemaColumnName, dto.Name).WhereNot(SchemaColumnId, dto.Id);
        var existing = await kateQueryExecutor.Count(query,cancellationToken);
        True(existing ==0).ThrowNotTrue($"the schema name {dto.Name} exists");
        False(cols.Length > 0 && dto.Id ==0 ).
            ThrowNotFalse($"the table name {entity.TableName} exists");
        
        foreach (var attribute in entity.GetAttributesByType(DisplayType.lookup))
        {
            await CheckLookup(attribute,cancellationToken);
        }
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
        var query = BaseQuery().Where(SchemaColumnName, name).Where(SchemaColumnType, SchemaType.Entity);
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

    private async Task<Result> LoadRelated(Entity entity, CancellationToken cancellationToken)
    {
        foreach (var attribute in entity.GetAttributesByType(DisplayType.lookup))
        {
            var lookupEntityName = attribute.GetLookupEntityName();
            if (lookupEntityName.IsFailed)
            {
                return Result.Fail(lookupEntityName.Errors);
            }

            var lookup = await GetEntityByNameOrDefault(lookupEntityName.Value, false,cancellationToken);
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

            var targetEntity = await GetEntityByNameOrDefault(targetEntityName.Value, false,cancellationToken);
            if (targetEntity.IsFailed)
            {
                return Result.Fail($"not find entity by name {entity.Name} for crosstable {attribute.FullName()}");
            }

            attribute.Crosstable = new Crosstable(entity, targetEntity.Value);
        }
        return Result.Ok();
    }

    private async Task CreateCrosstable(Entity entity, Utils.QueryBuilder.Attribute attribute,CancellationToken cancellationToken)
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

    private async Task CheckLookup(Utils.QueryBuilder.Attribute attribute, CancellationToken cancellationToken)
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
                {SchemaColumnName, dto.Name},
                {SchemaColumnType, dto.Type},
                {SchemaColumnSettings, JsonSerializer.Serialize(dto.Settings)},
                {SchemaColumnCreatedBy, dto.CreatedBy}
            };
            var query = new Query(SchemaTableName).AsInsert(record,true);
            dto.Id = await kateQueryExecutor.Exec(query,cancellationToken);
        }
        else
        {
            var query = new Query(SchemaTableName)
                .Where(SchemaColumnId, dto.Id)
                .AsUpdate(
                    [SchemaColumnName, SchemaColumnType, SchemaColumnSettings],
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
            Name = (string)record[SchemaColumnName],
            Type = (string)record[SchemaColumnType],
            Settings = JsonSerializer.Deserialize<Settings>((string)record[SchemaColumnSettings])!,
            CreatedBy = (string)record[SchemaColumnCreatedBy],
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
    
    private async Task<Schema> InternalSave(Schema dto, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery().Where(SchemaColumnName, dto.Name)
            .WhereNot(SchemaColumnId, dto.Id);
        var existing = await kateQueryExecutor.Count(query, cancellationToken);
        True(existing == 0).ThrowNotTrue($"the schema name {dto.Name} exists");
        await VerifyIfSchemaIsView(dto, cancellationToken);
        await SaveSchema(dto, cancellationToken);
        return dto;
    }
}