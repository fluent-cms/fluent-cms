using FluentCMS.Cms.Models;
using FluentResults;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;

public interface ISchemaService
{
    Task<Schema[]> GetAll(string type,CancellationToken cancellationToken);
    Task<Schema?> GetByIdDefault(int id, CancellationToken cancellationToken = default);
    Task<Schema?> GetByNameDefault(string name, string type="", CancellationToken cancellationToken = default);

    Task<Schema> Save(Schema schema, CancellationToken cancellationToken);
    Task Delete(int id, CancellationToken cancellationToken);
    
    Task<Result<Entity>> GetEntityByNameOrDefault(string name, bool loadRelated, CancellationToken cancellationToken = default);
    Task<Query> GetQueryByName(string name, CancellationToken cancellationToken);
    
    Task<Entity?> GetTableDefine(string tableName, CancellationToken cancellationToken);
    Task<Schema> SaveTableDefine(Schema schemaDto, CancellationToken cancellationToken);
    
    
    Task EnsureTopMenuBar(CancellationToken cancellationToken);
    Task EnsureSchemaTable(CancellationToken cancellationToken);

    Task<Schema> AddOrSaveSimpleEntity(string entity, string field, string? lookup, string? crossTable,
        CancellationToken cancellationToken = default);
    
    object CastToDatabaseType(Attribute attribute, string str);
}