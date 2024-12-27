using FluentCMS.Cms.Models;
using FluentResults;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Services;
public static class  SchemaType
{
    public const string Menu = "menu";
    public const string Entity = "entity";
    public const string Query = "query";
    public const string Page = "page";
}

public static class SchemaName
{
    public const string TopMenuBar = "top-menu-bar";
}

public interface ISchemaService
{
    Task<Schema[]> All(string type, IEnumerable<string>? names, CancellationToken ct = default);
    Task<Schema[]> AllWithAction(string type, CancellationToken ct = default);

    Task<Schema?> ByIdWithAction(int id, CancellationToken ct = default);
    Task<Schema?> ById(int id, CancellationToken ct = default);

    Task<Result> NameNotTakenByOther(Schema schema, CancellationToken ct);
    Task<Schema?> GetByNameDefault(string name, string type, CancellationToken ct = default);
    Task<Schema?> GetByNamePrefixDefault(string name, string type, CancellationToken ct = default);

    Task<Schema> SaveWithAction(Schema schema, CancellationToken ct=default);
    Task<Schema> Save(Schema dto,CancellationToken ct  = default);
    
    Task<Schema> AddOrUpdateByNameWithAction(Schema schema, CancellationToken ct = default);
    Task Delete(int id, CancellationToken ct = default);
    
    Task EnsureTopMenuBar(CancellationToken ct = default);
    Task EnsureSchemaTable(CancellationToken ct = default);

    Task RemoveEntityInTopMenuBar(Entity entity, CancellationToken ct = default);
    public Task EnsureEntityInTopMenuBar(Entity entity, CancellationToken ct = default);
}