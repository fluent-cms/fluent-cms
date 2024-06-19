using FluentCMS.Data;
using FluentCMS.Models;
using FluentCMS.Models.Queries;
using FluentCMS.Utils;

namespace FluentCMS.Services;

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        entity.DataKey = await dao.GetPrimaryKeyColumn(entity.TableName);
        entity.SetAttributes(await dao.GetColumnDefinitions(entity.TableName));
        return dto;
    }

    public async Task<IEnumerable<SchemaDisplayDto>> GetAll()
    {
         var schemas = await context.Schemas.ToListAsync();
         return schemas.Select(x => new SchemaDisplayDto(x));
    }

    public async Task<SchemaDisplayDto?> GetById(int id)
    {
        var item = await context.Schemas.FindAsync(id);
        return item is null ? null: new SchemaDisplayDto(item);
    }

    public async Task<Schema> Add(SchemaDto dto)
    {
        var item = dto.ToEntity();
        context.Schemas.Add(item);
        await context.SaveChangesAsync();
        return item;
    }


    public async Task<SchemaDto?> Save(SchemaDto dto)
    {
        if (dto.Id is null)
        {
            var item = dto.ToEntity();
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
