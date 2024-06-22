using FluentCMS.Data;
using FluentCMS.Models;
using FluentCMS.Models.Queries;
using FluentCMS.Utils.Dao;

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

        entity.PrimaryKey = await dao.GetPrimaryKeyColumn(entity.TableName);
        entity.SetAttributes(await dao.GetColumnDefinitions(entity.TableName));
        return dto;
    }

    public async Task<IEnumerable<SchemaDisplayDto>> GetAll()
    {
         var schemas = await context.Schemas.ToListAsync();
         return schemas.Select(x => new SchemaDisplayDto(x));
    }
    
    public async Task<Entity?> GetEntityByName(string? name)
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
        return item.Type==EntityTypes.Entity ? dto.Settings?.Entity: null;
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
        //for lookups, attach lookup entity to make frontend's life easier
        if (dto.Settings?.Entity is not null)
        {
            foreach (var attribute in dto.Settings.Entity.Lookups())
            {
                var entityName = attribute.GetLookupEntityName();
                if (entityName is null)
                {
                    continue;
                }

                attribute.LookupEntity = await GetEntityByName(entityName);
            }
        }
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
