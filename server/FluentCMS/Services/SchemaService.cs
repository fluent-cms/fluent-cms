using FluentCMS.Data;
using FluentCMS.Models;
using FluentCMS.Models.Queries;
using FluentCMS.Utils.Dao;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace FluentCMS.Services;

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

public class EntityTypes
{
    public static string Entity = "entity";
    public static string Menu = "menu";
}

public class SchemaService(AppDbContext context, IDao dao) : ISchemaService
{
    public async Task<SchemaDisplayDto?> GetTableDefine(int id)
    {
        var schema = await context.Schemas.FindAsync(id);
        if (schema is null)
        {
            return null;
        }

        var dto = new SchemaDisplayDto(schema);
        var entity = dto.Settings?.Entity;
        if (entity is null)
        {
            return null;
        }
        entity.SetAttributes(await dao.GetColumnDefinitions(entity.TableName));
        return dto;
    }

    private void InitEntity(Entity? entity, string entityName)
    {
        if (entity is null)
        {
            return;
        }

        entity.EntityName = entityName;
        foreach (var entityAttribute in entity.Attributes)
        {
            entityAttribute.Parent = entity;
        }

    }   

    private async Task LoadRelated(Entity? entity)
    {
        if (entity is null)
        {
            return;
        }

       
        foreach (var attribute in entity.GetAttributes(DisplayType.lookup, null))
        {
            var lookupEntityName = attribute.GetLookupEntityName();
            if (lookupEntityName is null)
            {
                continue;
            }
            attribute.Lookup = await _GetEntityByName(lookupEntityName, false);
        }

        foreach (var attribute in entity.GetAttributes(DisplayType.crosstable, null))
        {
            var crossEntity = await _GetEntityByName(attribute.GetCrossJoinEntityName(),false);
            if (crossEntity is null)
            {
                continue;
            }

            var lookups = crossEntity.GetAttributes(DisplayType.lookup, null);
            var fromAttribute = lookups.FirstOrDefault(x => x.GetLookupEntityName() == entity.EntityName);
            var targetAttribute = lookups.FirstOrDefault(x => x.GetLookupEntityName() != entity.EntityName);

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

    public async Task<Entity?> GetEntityByName(string? name)
    {
        return await _GetEntityByName(name, true);
    }
    
    public async Task<Entity?> _GetEntityByName(string? name, bool loadRelated)
    {
        if (name is null)
        {
            return null;
        }
        var item = await context.Schemas.Where(x => x.Name == name).FirstOrDefaultAsync();
        if (item is null)
        {
            return null;
        }

        var dto = new SchemaDto(item);
        if (item.Type != EntityTypes.Entity)
        {
            return null;
        }

        InitEntity(dto.Settings?.Entity, name);
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
        }
        else
        {
            item = await context.Schemas.Where(x => x.Name == name).FirstOrDefaultAsync();
        }

        if (item is null)
        {
            return null;
        }
        var dto =  new SchemaDisplayDto(item);
        InitEntity(dto.Settings?.Entity, name);
        await LoadRelated(dto.Settings?.Entity);
        return dto;
    }

    public async Task<SchemaDto?> Save(SchemaDto dto)
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
            if (item == null)
            {
                return null;
            }
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
