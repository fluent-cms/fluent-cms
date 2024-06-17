using FluentCMSApi.models;
using FluentCMSApi.Data;

namespace FluentCMSApi.Services.cs;

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

public class SchemaService (AppDbContext context) : ISchemaService
{
    public async Task<IEnumerable<Schema>> GetAll()
    {
        return await context.Schemas.ToListAsync();
    }

    public async Task<Schema> GetById(int id)
    {
        return await context.Schemas.FindAsync(id);
    }

    public async Task<Schema> Add(Schema item)
    {
        context.Schemas.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public async Task<Schema> Update(Schema item)
    {
        item.UpdatedAt = DateTime.Now.ToUniversalTime();
        context.Entry(item).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return item;
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
