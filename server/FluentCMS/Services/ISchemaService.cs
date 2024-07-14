using FluentCMS.Models;
using Utils.QueryBuilder;

namespace FluentCMS.Services;

public interface ISchemaService
{
    Task<IEnumerable<SchemaDisplayDto>> GetAll(string type);
    Task<Entity?> GetEntityByName(string name);
    Task<View> GetViewByName(string name);
    Task<SchemaDisplayDto?> GetByIdOrName(string name);
    Task<Entity?> GetTableDefine(string tableName);
    Task<SchemaDto> SaveTableDefine(SchemaDto schemaDto);
    Task<SchemaDto> Save(SchemaDto schema);
    Task<bool> Delete(int id);
}