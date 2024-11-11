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
    Task<Schema> AddOrUpdate(Entity entity, CancellationToken token);
    Task<Result<LoadedAttribute>> LoadOneRelated(LoadedEntity entity, LoadedAttribute attr, CancellationToken token);
}