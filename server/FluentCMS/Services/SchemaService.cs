using FluentCMS.Data;
using FluentCMS.Models;
using Utils.DataDefinitionExecutor;
using Utils.QueryBuilder;

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
    public async Task<SchemaDto?> SaveTableDefine(SchemaDto dto)
    {
        var entity = dto.Settings?.Entity;
        ArgumentNullException.ThrowIfNull(entity);
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

    public async Task<SchemaDisplayDto?> GetTableDefine(int id)
    {
        var schema = await context.Schemas.FindAsync(id);
        ArgumentNullException.ThrowIfNull(schema);

        var dto = new SchemaDisplayDto(schema);
        var entity = dto.Settings?.Entity;
        ArgumentNullException.ThrowIfNull(entity);

        entity.LoadDefine(await definitionExecutor.GetColumnDefinitions(entity.TableName));
        return dto;
    }


    public async Task<IEnumerable<SchemaDisplayDto>> GetAll()
    {
        var schemas = await context.Schemas.ToListAsync();
        return schemas.Select(x => new SchemaDisplayDto(x));
    }

    public async Task<View?> GetViewByName(string name)
    {
        var item = await context.Schemas.Where(x => x.Name == name && x.Type == SchemaType.View)
            .FirstOrDefaultAsync();
        ArgumentNullException.ThrowIfNull(item);
        var dto = new SchemaDto(item);
        var view = dto.Settings?.View;
        ArgumentNullException.ThrowIfNull(view);
        if (string.IsNullOrWhiteSpace(view.EntityName))
        {
            throw new Exception($"Entity Name not set for view {item.Name}");
        }

        view.Entity = await GetEntityByName(view.EntityName, true);
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
            return dto;
        }
        else
        {
            var item = context.Schemas.FirstOrDefault(i => i.Id == dto.Id);
            ArgumentNullException.ThrowIfNull(item);
            dto.Attach(item);
            await context.SaveChangesAsync();
            return dto;
        }
    }

    public async Task<bool> Delete(int id)
    {
        var item = await context.Schemas.FindAsync(id);
        ArgumentNullException.ThrowIfNull(item);
        context.Schemas.Remove(item);
        await context.SaveChangesAsync();
        return true;
    }

    private async Task<Entity> GetEntityByName(string name, bool loadRelated)
    {
        var item = await context.Schemas.Where(x => x.Name == name && x.Type == SchemaType.Entity)
            .FirstOrDefaultAsync();

        ArgumentNullException.ThrowIfNull(item);
        var dto = new SchemaDto(item);

        var entity = dto.Settings?.Entity;
        ArgumentNullException.ThrowIfNull(entity);
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
            var lookupEntityName = attribute.GetLookupEntityName();
            if (string.IsNullOrWhiteSpace(lookupEntityName))
            {
                throw new Exception($"lookup entity name is not set for {entity.Name}");
            }

            attribute.Lookup = await GetEntityByName(lookupEntityName, false);
        }

        foreach (var attribute in entity.GetAttributes(DisplayType.crosstable, null, null))
        {
            var joinEntityName = attribute.GetCrossJoinEntityName();
            if (string.IsNullOrWhiteSpace(joinEntityName))
            {
                throw new Exception($"Crosstable entity name is not set for {entity.Name}");
            }

            var crossEntity = await GetEntityByName(joinEntityName, false);
            var lookups = crossEntity.GetAttributes(DisplayType.lookup, null, null);
            var fromAttribute = lookups.FirstOrDefault(x => x.GetLookupEntityName() == entity.Name);
            var targetAttribute = lookups.FirstOrDefault(x => x.GetLookupEntityName() != entity.Name);

            if (fromAttribute == null || targetAttribute == null)
            {
                throw new Exception($"Load Crosstable Fail, not find from attribute or target attribute");
            }

            var targetEntity = await GetEntityByName(targetAttribute.GetLookupEntityName(), false);
            attribute.Crosstable = new Crosstable
            {
                FromAttribute = fromAttribute,
                TargetAttribute = targetAttribute,
                TargetEntity = targetEntity,
                CrossEntity = crossEntity,
            };
        }

    }
}
