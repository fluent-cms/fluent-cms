using FluentCMS.Models;
using FluentResults;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Services;

public interface ISchemaService
{
    Task<Schema[]> GetAll(string type,CancellationToken cancellationToken);
    Task<Result<Entity>> GetEntityByNameOrDefault(string name, CancellationToken cancellationToken = default);
    Task<Schema> GetByIdOrName(string name, bool extend, CancellationToken cancellationToken);
    Task<Schema?> GetByIdOrNameDefault(string name, CancellationToken cancellationToken);
    Task<View> GetViewByName(string name, CancellationToken cancellationToken);
    Task<Entity?> GetTableDefine(string tableName, CancellationToken cancellationToken);
    Task<Schema> SaveTableDefine(Schema schemaDto, CancellationToken cancellationToken);
    Task<Schema> Save(Schema schema, CancellationToken cancellationToken);
    Task EnsureTopMenuBar(CancellationToken cancellationToken);
    Task EnsureSchemaTable(CancellationToken cancellationToken);
    Task<bool> Delete(int id, CancellationToken cancellationToken);
    Task<Schema> AddOrSaveEntity(Entity entity, CancellationToken cancellationToken);

    Task<Schema> AddOrSaveSimpleEntity(string entity, string field, string? lookup, string? crossTable,
        CancellationToken cancellationToken = default);
}