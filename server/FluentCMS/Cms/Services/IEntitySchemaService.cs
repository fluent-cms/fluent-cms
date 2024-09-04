using FluentCMS.Cms.Models;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;

public interface IEntitySchemaService
{
    Task<Result<Entity>> GetByNameDefault(string name, bool loadRelated, CancellationToken cancellationToken = default);
    Task<Entity?> GetTableDefine(string tableName, CancellationToken cancellationToken);
    Task<Schema> SaveTableDefine(Schema schemaDto, CancellationToken cancellationToken);
    Task<Schema> AddOrSaveSimpleEntity(string entity, string field, string? lookup, string? crossTable,
        CancellationToken cancellationToken = default);

    Task<Result> LoadLookup(Attribute attribute, CancellationToken cancellationToken);
    Task<Result> LoadCrosstable(Entity sourceEntity, Attribute attribute, CancellationToken cancellationToken);
}