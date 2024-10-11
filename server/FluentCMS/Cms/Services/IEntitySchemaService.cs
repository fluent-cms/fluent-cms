using FluentCMS.Cms.Models;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;

public interface IEntitySchemaService
{
    
    Task<Result<LoadedEntity>> GetLoadedEntity(string name, CancellationToken cancellationToken = default);
    Task<Result<ValidEntity>> GetValidEntity(string name,  CancellationToken cancellationToken = default);
    Task<Result<Entity>> GetEntity(string name, CancellationToken cancellationToken = default);
    
    Task<Entity?> GetTableDefine(string tableName, CancellationToken cancellationToken);
    Task<Schema> SaveTableDefine(Schema schemaDto, CancellationToken cancellationToken);
    Task<Schema> AddOrSaveSimpleEntity(string entity, string field, string? lookup, string? crossTable,
        CancellationToken cancellationToken = default);
}