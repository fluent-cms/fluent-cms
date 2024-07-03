using FluentCMS.Models;
using Utils.QueryBuilder;

namespace FluentCMS.Services;

public interface ISchemaService
{
    Task<IEnumerable<SchemaDisplayDto>> GetAll();
    Task<Entity?> GetEntityByName(string name);
    Task<View?> GetViewByName(string name);
    Task<SchemaDisplayDto?> GetByIdOrName(string name);
    Task<SchemaDisplayDto?> GetTableDefine(int id);
    Task<SchemaDisplayDto?> SaveTableDefine(SchemaDto schemaDto);
    
    Task<SchemaDto?> Save(SchemaDto schema);
    Task<bool> Delete(int id);
}