using FluentCMS.Models;
using Microsoft.EntityFrameworkCore;
using Utils.DataDefinitionExecutor;
using Utils.QueryBuilder;
using Attribute = System.Attribute;

namespace FluentCMS.Services;
public partial class SchemaService
{

    private async Task PreSaveView(SchemaDto dto)
    {
        var view = dto.Settings?.View;
        if (view is null)
        {
            return;
        }
        var existing = await context.Schemas.FirstOrDefaultAsync(s => s.Name == view.EntityName );
        Val.NotNull(existing).ValOrThrow($"not found entity {view.EntityName}");
    }
    
    private async Task PostLoadEntity(SchemaDto dto)
    {
        var entity = dto.Settings?.Entity;
        if (entity is null)
        {
            return;
        }

        entity.Init();
        await LoadRelated(entity);
    }

    private async Task VerifyEntity(SchemaDto dto, ColumnDefinition[] cols, Entity entity)
    {
        var existing = await context.Schemas.FirstOrDefaultAsync(s => s.Name == dto.Name && s.Id != dto.Id);
        Val.CheckBool(existing is null).ThrowFalse($"the schema name {dto.Name} exists");

        Val.CheckBool(cols.Length > 0 && dto.Id is null).ThrowTrue($"the table name {entity.TableName} exists");
        foreach (var attribute in entity.GetAttributes(DisplayType.lookup, null, null))
        {
            await CheckLookup(attribute);
        }
    }

    private async Task EnsureEntityInTopMenuBar(Entity entity)
    {
        var menuBarSchema = await GetByIdOrName("top-menu-bar");
        var menuBar = menuBarSchema?.Settings?.Menu;
        if (menuBarSchema is not null && menuBar is not null)
        {
            var link = "/entities/" + entity.Name;
            var menuItem = menuBar.MenuItems.FirstOrDefault(me => me.Url == link);
            if (menuItem is null)
            {
                var label = entity.Title;
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

    private async Task<Entity?> GetEntityByName(string name, bool loadRelated)
    {
        var item = await context.Schemas.Where(x => x.Name == name && x.Type == SchemaType.Entity)
            .FirstOrDefaultAsync();
        if (item is null)
        {
            return null;
        }

        var dto = new SchemaDto(item);
        var entity = dto.Settings?.Entity;
        if (entity is null)
        {
            return null;
        }

        entity.Init();
        if (loadRelated)
        {
            await LoadRelated(entity);
        }

        return entity;
    }

    private async Task LoadRelated(Entity entity)
    {
        foreach (var attribute in entity.GetAttributes(DisplayType.lookup, null, null))
        {
            var lookupEntityName = Val.StrNotEmpty(attribute.GetLookupEntityName())
                .ValOrThrow($"lookup entity name for {entity.Name}.{attribute.Field} should not be empty");
            attribute.Lookup = Val.NotNull(await GetEntityByName(lookupEntityName, false))
                .ValOrThrow($"not find entity by name by name {entity.Name}");
        }

        foreach (var attribute in entity.GetAttributes(DisplayType.crosstable, null, null))
        {
            var targetEntityName = Val.StrNotEmpty(attribute.GetCrossEntityName())
                .ValOrThrow($"crosstable entity name for ${entity.Name}.{attribute.Field}");
            var targetEntity = Val.NotNull(await GetEntityByName(targetEntityName, false))
                .ValOrThrow($"not find entity by name by name {entity.Name}");
            attribute.Crosstable = new Crosstable(entity, targetEntity);
        }
    }

    private async Task CreateCrosstable(Entity entity, global::Utils.QueryBuilder.Attribute attribute)
    {
        var entityName = Val.StrNotEmpty(attribute.GetCrossEntityName())
            .ValOrThrow($"crosstable entity of {attribute.Field} was not set");
        var targetEntity = Val.NotNull(await GetEntityByName(entityName, false))
            .ValOrThrow($"not find entity by name {entityName}");
        var crossTable = new Crosstable(entity, targetEntity);
        var columns = await definitionExecutor.GetColumnDefinitions(crossTable.CrossEntity.TableName);
        if (columns.Length == 0)
        {
            await definitionExecutor.CreateTable(crossTable.CrossEntity.TableName, crossTable.GetColumnDefinitions());
        }
    }

    private async Task CheckLookup(global::Utils.QueryBuilder.Attribute attribute)
    {
        Val.CheckBool(attribute.DataType != DataType.Int).ThrowTrue("lookup datatype should be int");
        var entityName = Val.StrNotEmpty(attribute.GetLookupEntityName())
            .ValOrThrow($"lookup entity of {attribute.Field} was not set");
        Val.NotNull(await GetEntityByName(entityName, false))
            .ValOrThrow($"not find entity by name {entityName}");
    } 
    
}