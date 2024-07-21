using FluentCMS.Models;
using Microsoft.EntityFrameworkCore;
using Utils.DataDefinitionExecutor;
using Utils.QueryBuilder;
using Attribute = System.Attribute;

namespace FluentCMS.Services;
public partial class SchemaService
{

    private const string TopMenuBar = "top-menu-bar";
    private async Task PreSaveView(SchemaDto dto)
    {
        var view = dto.Settings?.View;
        if (view is null) //not view, just ignore
        {
            return;
        }

        var entityName = Val.StrNotEmpty(view.EntityName).
            ValOrThrow($"entity name of {view.EntityName} should not be empty");
        view.Entity = Val.NotNull(await GetEntityByName(entityName, true)).
            ValOrThrow($"not find entity {entityName}");
        
        foreach (var viewAttributeName in view.AttributeNames??[])
        {
            Val.NotNull(view.Entity.FindOneAttribute(viewAttributeName))
                .ValOrThrow($"not find attribute {viewAttributeName} of enity {entityName}");
        }

        var listAttributes = view.LocalAttributes(InListOrDetail.InList);
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
    
    private async Task PostLoadEntity(SchemaDto dto)
    {
        var entity = dto.Settings?.Entity;
        if (entity is null)//not a engity, ignore
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
        foreach (var attribute in entity.GetAttributesByType(DisplayType.lookup))
        {
            await CheckLookup(attribute);
        }
    }

    private async Task _AddTopMenuBar()
    {
        var item = new MenuItem
        {
            Label = "Schema Builder",
            Url = "/schema-ui/list.html",
            Icon = "pi-cog",
            IsHref = true
        };
        var menuBarSchema = new SchemaDto
        {
            Name = TopMenuBar,
            Type = SchemaType.Menu,
        };
        menuBarSchema.Settings = new Settings
        {
            Menu = new Menu
            {
                Name = TopMenuBar,
                MenuItems = [item]
            }
        };
        await Save(menuBarSchema);
    }

    private async Task EnsureEntityInTopMenuBar(Entity entity)
    {
        var menuBarSchema = await GetByIdOrName(TopMenuBar);
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
        foreach (var attribute in entity.GetAttributesByType(DisplayType.lookup))
        {
            var lookupEntityName = Val.StrNotEmpty(attribute.GetLookupEntityName())
                .ValOrThrow($"lookup entity name for {attribute.FullName()} should not be empty");
            attribute.Lookup = Val.NotNull(await GetEntityByName(lookupEntityName, false))
                .ValOrThrow($"not find entity by name {lookupEntityName} for lookup {attribute.FullName()}");
        }

        foreach (var attribute in entity.GetAttributesByType(DisplayType.crosstable))
        {
            var targetEntityName = Val.StrNotEmpty(attribute.GetCrossEntityName())
                .ValOrThrow($"crosstable entity name for ${attribute.FullName()}");
            var targetEntity = Val.NotNull(await GetEntityByName(targetEntityName, false))
                .ValOrThrow($"not find entity by name {entity.Name} for crosstable {attribute.FullName()}");
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