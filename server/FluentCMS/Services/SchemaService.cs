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

public partial class SchemaService(AppDbContext context, IDefinitionExecutor definitionExecutor) : ISchemaService
{
    public async Task<IEnumerable<SchemaDisplayDto>> GetAll(string type)
    {
        var schemas = await context.Schemas.Where(x => string.IsNullOrWhiteSpace(type) || x.Type == type).ToListAsync();
        return schemas.Select(x => new SchemaDisplayDto(x));
    }

    public async Task<SchemaDisplayDto?> GetByIdOrName(string name)
    {
        if ( int.TryParse(name, out var id))
        {
            var item = Val.NotNull(await context.Schemas.FindAsync(id)).ValOrThrow($"not find schema by id {id}");
            return new SchemaDisplayDto(item);
        }
        else
        {
            var item = await context.Schemas.FirstOrDefaultAsync(x => x.Name == name);
            item = Val.NotNull(item).ValOrThrow($"Schema [{name}] does not exist");
            var dto = new SchemaDisplayDto(item);
            //get by name is called from Admin Panel, need load related settings
            await PostLoadEntity(dto);
            return dto;
        }
    }

    public async Task<SchemaDto> Save(SchemaDto dto)
    {
        var existing = await context.Schemas.FirstOrDefaultAsync(s => s.Name == dto.Name && s.Id != dto.Id);
        Val.CheckBool(existing is null).ThrowFalse($"the schema name {dto.Name} exists");

        await PreSaveView(dto);
        if (dto.Id is null)
        {
            var item = dto.ToModel();
            context.Schemas.Add(item);
            await context.SaveChangesAsync();
            dto.Id = item.Id;
        }
        else
        {
            var item = Val.NotNull(context.Schemas.FirstOrDefault(i => i.Id == dto.Id))
                .ValOrThrow($"not find schema by id {dto.Id}");
            dto.Attach(item);
            await context.SaveChangesAsync();
        }
        return dto;
    }

    public async Task<View> GetViewByName(string name)
    {
        Val.StrNotEmpty(name).ValOrThrow("view name should not be empty");
        var item = await context.Schemas
            .Where(x => x.Name == name && x.Type == SchemaType.View)
            .FirstOrDefaultAsync();
        item = Val.NotNull(item).ValOrThrow($"didn't find {name}");
        var dto = new SchemaDto(item);
        var view = Val.NotNull(dto.Settings?.View)
            .ValOrThrow("invalid view format");
        var entityName = Val.StrNotEmpty(view.EntityName)
            .ValOrThrow($"referencing entity was not set for {view}");
        view.Entity = await GetEntityByName(entityName, true);
        return view;
    }

    public async Task<Entity?> GetEntityByName(string name)
    {
        return await GetEntityByName(name, true);
    }

    public async Task<SchemaDto> SaveTableDefine(SchemaDto dto)
    {
        var entity = Val.NotNull(dto.Settings?.Entity).ValOrThrow("invalid payload");
        var cols = await definitionExecutor.GetColumnDefinitions(entity.TableName);
        await VerifyEntity(dto, cols, entity);
        
        foreach (var attribute in entity.GetAttributesByType(DisplayType.crosstable))
        {
            await CreateCrosstable(entity, attribute);
        }

        if (cols.Length > 0) //if table exists, alter table add columns
        {
            await definitionExecutor.AlterTableAddColumns(entity.TableName, entity.AddedColumnDefinitions(cols));
        }
        else
        {
            entity.EnsureDefaultAttribute();
            await definitionExecutor.CreateTable(entity.TableName, entity.ColumnDefinitions());
            //no need to expose deleted field to frontend 
            entity.RemoveDeleted();
        }

        await EnsureEntityInTopMenuBar(entity);
        return await Save(dto);
    }


    public async Task<Entity?> GetTableDefine(string tableName)
    {
        Val.StrNotEmpty(tableName).ValOrThrow("Table name should not be empty");
        var entity = new Entity { TableName = tableName };
        entity.LoadAttributesByColDefines(await definitionExecutor.GetColumnDefinitions(tableName));
        return entity;
    }

    public async Task<bool> Delete(int id)
    {
        var item = await context.Schemas.FindAsync(id);
        ArgumentNullException.ThrowIfNull(item);
        context.Schemas.Remove(item);
        await context.SaveChangesAsync();
        return true;
    }


}
