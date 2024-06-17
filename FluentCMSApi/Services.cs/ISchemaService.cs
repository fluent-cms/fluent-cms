using FluentCMSApi.models;

namespace FluentCMSApi.Services.cs;

public interface ISchemaService
{
    Task<IEnumerable<Schema>> GetAll();
    Task<Schema> GetById(int id);
    Task<Schema> Add(Schema schema);
    Task<Schema> Update(Schema schema);
    Task<bool> Delete(int id);
}