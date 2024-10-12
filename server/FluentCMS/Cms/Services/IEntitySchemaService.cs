using FluentCMS.Cms.Models;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Cms.Services;

public interface IEntitySchemaService
{
    
    Task<Result<LoadedEntity>> GetLoadedEntity(string name, CancellationToken cancellationToken = default);
    
    Task<Entity?> GetTableDefine(string tableName, CancellationToken cancellationToken);
    Task<Schema> SaveTableDefine(Schema schemaDto, CancellationToken cancellationToken);
    Task<Schema> AddOrSaveSimpleEntity(string entity, string field, string? lookup, string? crossTable,
        CancellationToken cancellationToken = default);
}