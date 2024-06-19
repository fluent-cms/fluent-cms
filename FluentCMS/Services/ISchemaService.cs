using FluentCMS.Models;

namespace FluentCMS.Services;

public interface ISchemaService
{
    Task<IEnumerable<SchemaDisplayDto>> GetAll();
    Task<SchemaDisplayDto?> GetById(int id);
    Task<SchemaDisplayDto?> GetTableDefine(int id);
    Task<SchemaDto?> Save(SchemaDto schema);
    Task<bool> Delete(int id);
}