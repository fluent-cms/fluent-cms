using FluentCMS.Data;
using FluentCMS.Models;
using Utils.DataDefinitionExecutor;
using Utils.QueryBuilder;
using Attribute = Utils.QueryBuilder.Attribute;

namespace FluentCMS.Services;

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class  SchemaType
{
    public const string Menu = "menu";
    public const string Entity = "entity";
    public const string View = "view";
}

public class SchemaService(AppDbContext context, IDefinitionExecutor definitionExecutor) : ISchemaService
{
    public async Task<SchemaDto> SaveTableDefine(SchemaDto dto)
    {
        var entity = Val.NotNull( "entity",dto.Settings?.Entity);
        foreach (var attribute in entity.GetAttributes(DisplayType.crosstable, null, null))
        {
            await CreateCrosstable(entity, attribute);
        }
        var cols = await definitionExecutor.GetColumnDefinitions(entity.TableName);
        if (cols.Length > 0) //if table exists, alter table add columns
        {
            await definitionExecutor.AlterTableAddColumns(entity.TableName, entity.GetAddedColumnDefinitions(cols));
        }
        else
        {
            entity.PrimaryKey = "id";
            entity.EnsureDefaultAttribute();
            await definitionExecutor.CreateTable(entity.TableName, entity.GetColumnDefinitions());
            //no need to expose deleted field to frontend 
            entity.RemoveDeleted();
        }
        return await Save(dto);
    }

    
    public async Task<Entity?> GetTableDefine(string tableName)
    {
        Val.NotEmpty(tableName,"table name");
        var entity = new Entity { TableName = tableName };
        entity.LoadDefine(await definitionExecutor.GetColumnDefinitions(tableName));
        return entity;
    }


    public async Task<IEnumerable<SchemaDisplayDto>> GetAll(string type)
    {
        var schemas = await context.Schemas.
            Where(x=> string.IsNullOrWhiteSpace(type) || x.Type == type).
            ToListAsync();
        return schemas.Select(x => new SchemaDisplayDto(x));
    }

    public async Task<View?> GetViewByName(string name)
    {
        Val.NotEmpty(name, "view name");
        var item =  await context.Schemas
            .Where(x => x.Name == name && x.Type == SchemaType.View)
            .FirstOrDefaultAsync();
        item = Val.NotNull($"view of {name}", item);
        var dto = new SchemaDto(item);
        var view = Val.NotNull($"view of {name}",dto.Settings?.View);
        var entityName = Val.NotNull($"entity name of {name}", view.EntityName);
        view.Entity = await GetEntityByName(entityName, true);
        return view;
    }

    public async Task<Entity?> GetEntityByName(string name)
    {
        return await GetEntityByName(name, true);
    }

    public async Task<SchemaDisplayDto?> GetByIdOrName(string name)
    {
        bool isInteger = int.TryParse(name, out int id);
        Schema? item;
        if (isInteger)
        {
            item = await context.Schemas.FindAsync(id);
            return new SchemaDisplayDto(item);
        }

        //by name is call from admin panel, need init entity for frontend
        item = await context.Schemas.FirstOrDefaultAsync(x => x.Name == name);
        var dto = new SchemaDisplayDto(item);
        var entity = dto.Settings?.Entity;
        if (entity is not null)
        {
            entity.Init();
            await LoadRelated(entity);
        }

        return dto;
    }

    public async Task<SchemaDto> Save(SchemaDto dto)
    {
        if (dto.Id is null)
        {
            var item = dto.ToModel();
            context.Schemas.Add(item);
            await context.SaveChangesAsync();
            dto.Id = item.Id;
        }
        else
        {
            var item = Val.NotNull($"schema of {dto.Id} ",context.Schemas.FirstOrDefault(i => i.Id == dto.Id));
            dto.Attach(item);
            await context.SaveChangesAsync();
        }

        if (dto.Type == SchemaType.Entity)
        {
            //add to top-menu-bar
            var menuBarSchema = await GetByIdOrName("top-menu-bar");
            var menuBar = menuBarSchema?.Settings?.Menu;
            if (menuBarSchema is not null && menuBar is not null)
            {
                var link = "/entities/" + dto.Name;
                var menuItem = menuBar.MenuItems.FirstOrDefault(me => me.Url == link);
                if (menuItem is null)
                {
                    var label = dto.Settings?.Entity?.Title ??"";
                    menuBar.MenuItems = [..menuBar.MenuItems, new MenuItem
                    {
                        Url = link,
                        Label = label
                    }];
                }
                await Save(menuBarSchema);
            }
        }
        return dto;
    }
    

    public async Task<bool> Delete(int id)
    {
        var item = await context.Schemas.FindAsync(id);
        ArgumentNullException.ThrowIfNull(item);
        context.Schemas.Remove(item);
        await context.SaveChangesAsync();
        return true;
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
            var errorVariable = $"lookup entity name for ${entity.Name}.{attribute.Field}";
            var lookupEntityName = Val.NotEmpty(errorVariable,attribute.GetLookupEntityName());
            attribute.Lookup = Val.NotNull(errorVariable,await GetEntityByName(lookupEntityName, false));
        }

        foreach (var attribute in entity.GetAttributes(DisplayType.crosstable, null, null))
        {
            var errorVariable = $"crosstable entity name for ${entity.Name}.{attribute.Field}";
            var targetEntityName = Val.NotEmpty(errorVariable,attribute.GetCrossEntityName());
            var targetEntity =Val.NotNull(errorVariable, await GetEntityByName(targetEntityName, false));
            attribute.Crosstable = new Crosstable(entity, targetEntity);
        }
    }
    
    private async Task CreateCrosstable(Entity entity, Attribute attribute)
    {
        var entityName = Val.NotEmpty($"crosstable of {attribute.Field}",
            attribute.GetCrossEntityName());
        var targetEntity = Val.NotNull($"target entity {attribute.Field}",
            await GetEntityByName(entityName, false));
        var crossTable = new Crosstable(entity, targetEntity);
        var columns = await definitionExecutor.GetColumnDefinitions(crossTable.CrossEntity.TableName);
        if (columns.Length == 0)
        {
            await definitionExecutor.CreateTable(crossTable.CrossEntity.TableName, crossTable.GetColumnDefinitions());
        }
    }
}
