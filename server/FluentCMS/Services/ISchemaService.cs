using FluentCMS.Models;
using FluentCMS.Models.Queries;

namespace FluentCMS.Services;

public interface ISchemaService
{
    Task<IEnumerable<SchemaDisplayDto>> GetAll();
    Task<Entity?> GetEntityByName(string? name);
    Task<SchemaDisplayDto?> GetByIdOrName(string name);
    Task<SchemaDisplayDto?> GetTableDefine(int id);
    Task<SchemaDto?> Save(SchemaDto schema);
    Task<bool> Delete(int id);
}