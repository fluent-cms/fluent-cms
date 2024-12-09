using System.Collections.Immutable;
using FluentCMS.Cms.Models;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Cms.Services;

public interface IEntitySchemaService: IEntityVectorResolver, IAttributeValueResolver
{
    Task<Result<LoadedEntity>> GetLoadedEntity(string name, CancellationToken token = default);
    Task<Entity?> GetTableDefine(string name, CancellationToken token);
    Task<Schema> SaveTableDefine(Schema schemaDto, CancellationToken ct);
    Task<Schema> AddOrUpdateByName(Entity entity, CancellationToken ct);
    Task<Result<LoadedAttribute>> LoadOneCompoundAttribute(LoadedEntity entity, LoadedAttribute attr,HashSet<string> visitedCrosstable, CancellationToken token);
    ValueTask<ImmutableArray<Entity>> GetOrCreate(CancellationToken ct = default);
    Task Delete(Schema schema, CancellationToken ct);
    Task<Schema> Save(Schema schema, CancellationToken ct);
}