using System.Collections.Immutable;
using FluentCMS.Cms.Models;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Cms.Services;

public interface IEntitySchemaService: IEntityVectorResolver, IAttributeValueResolver
{
    Task<Result<LoadedEntity>> GetLoadedEntity(string name, CancellationToken token = default);
    Task<Entity?> GetTableDefine(string name, CancellationToken token);
    Task<Schema> SaveTableDefine(Schema schemaDto, CancellationToken token);
    Task<Schema> AddOrUpdateByName(Entity entity, CancellationToken token);
    Task<Result<LoadedAttribute>> LoadOneCompoundAttribute(LoadedEntity entity, LoadedAttribute attr,HashSet<string> visitedCrosstable, CancellationToken token);
    Task ReplaceCache();
    bool TryGetCachedSchema(out ImmutableArray<Entity> entities);
    Task Delete(Schema schema, CancellationToken token);
    Task<Schema> Save(Schema schema, CancellationToken token);
}