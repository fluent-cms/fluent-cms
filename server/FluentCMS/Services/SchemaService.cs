using FluentCMS.Data;
using FluentCMS.Models;
using Utils.Dao;
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

public class SchemaService(AppDbContext context, IDao dao) : ISchemaService
{
    public async Task<SchemaDto?> SaveTableDefine(SchemaDto dto)
    {
        var entity = dto.Settings?.Entity;
        ArgumentNullException.ThrowIfNull(entity);
        var cols = await dao.GetColumnDefinitions(entity.TableName);
        
        if (cols.Length > 0)//if table exists, alter table add columns
        {
            await dao.AddColumns(entity.TableName, entity.GetAddedColumnDefinitions(cols));
        }
        else 
        {
            entity.PrimaryKey = "id";
            entity.EnsureDefaultAttribute();
            await dao.CreateTable(entity.TableName, entity.GetColumnDefinitions());
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
        
        entity.LoadDefine(await dao.GetColumnDefinitions(entity.TableName));
        return dto;
    }

    private void InitEntity(Entity? entity)
    {
        if (entity is null)
        {
            return;
        }
        foreach (var entityAttribute in entity.Attributes)
        {
            entityAttribute.Parent = entity;
        }
    }   

    private async Task LoadRelated(Entity? entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        foreach (var attribute in entity.GetAttributes(DisplayType.lookup, null, null))
        {
            var lookupEntityName = attribute.GetLookupEntityName();
            if (string.IsNullOrWhiteSpace(lookupEntityName))
            {
                throw new Exception($"lookup entity name is not set for {entity.Name}");
            }
            attribute.Lookup = await _GetEntityByName(lookupEntityName, false);
        }

        foreach (var attribute in entity.GetAttributes(DisplayType.crosstable, null,null))
        {
            var joinEntityName = attribute.GetCrossJoinEntityName();
            if (string.IsNullOrWhiteSpace(joinEntityName))
            {
                throw new Exception($"Crosstable entity name is not set for {entity.Name}");
            }
            var crossEntity = await _GetEntityByName(joinEntityName,false);
            if (crossEntity is null)
            {
                continue;
            }

            var lookups = crossEntity.GetAttributes(DisplayType.lookup, null,null);
            var fromAttribute = lookups.FirstOrDefault(x => x.GetLookupEntityName() == entity.Name);
            var targetAttribute = lookups.FirstOrDefault(x => x.GetLookupEntityName() != entity.Name);

            if (fromAttribute == null || targetAttribute == null)
            {
                continue;
            }

            var targetEntity = await _GetEntityByName(targetAttribute.GetLookupEntityName(),false);
            if (targetEntity is null)
            {
                continue;
                
            }
            attribute.Crosstable = new Crosstable
            {
                FromAttribute = fromAttribute,
                TargetAttribute = targetAttribute,
                TargetEntity = targetEntity,
                CrossEntity = crossEntity,
            };
        }

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
        
        view.Entity =  await _GetEntityByName(view.EntityName, true);
        return view;
    }
 
    public async Task<Entity?> GetEntityByName(string name)
    {
        return await _GetEntityByName(name, true);
    }
    
    private async Task<Entity?> _GetEntityByName(string name, bool loadRelated)
    {
        var item = await context.Schemas.Where(x => x.Name == name && x.Type == SchemaType.Entity)
            .FirstOrDefaultAsync();
        var dto = new SchemaDto(item);
        if (dto.Type != SchemaType.Entity)
        {
            return null;
        }

        InitEntity(dto.Settings?.Entity );
        if (loadRelated)
        {
            await LoadRelated(dto.Settings?.Entity);
        }
        return dto.Settings?.Entity;
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

        item = await context.Schemas.FirstOrDefaultAsync(x => x.Name == name);
        var dto = new SchemaDisplayDto(item);
        if (dto.Settings?.Entity is null)
        {
            return dto;
        }

        InitEntity(dto.Settings?.Entity);
        await LoadRelated(dto.Settings?.Entity);
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
        var product = await context.Schemas.FindAsync(id);
        if (product == null)
        {
            return false;
        }

        context.Schemas.Remove(product);
        await context.SaveChangesAsync();
        return true;
    }
}
