using System.Data;
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
    Task<Schema[]> All(string type, IEnumerable<string>? names, CancellationToken cancellationToken = default);
    Task<Schema[]> AllWithAction(string type, CancellationToken token = default);

    Task<Schema?> ByIdWithAction(int id, CancellationToken token = default);
    Task<Schema?> ById(int id, CancellationToken cancellationToken = default);

    Task<Result> NameNotTakenByOther(Schema schema, CancellationToken token);
    Task<Schema?> GetByNameDefault(string name, string type, CancellationToken token = default);
    Task<Schema?> GetByNamePrefixDefault(string name, string type, CancellationToken token = default);

    Task<Schema> SaveWithAction(Schema schema, CancellationToken ct=default, IDbTransaction? tx=null);
    Task<Schema> AddOrUpdateByNameWithAction(Schema schema, CancellationToken ct = default,IDbTransaction? tx=null );
    Task Delete(int id, CancellationToken token = default,IDbTransaction? tx =null);
    
    Task EnsureTopMenuBar(CancellationToken ct = default,IDbTransaction? tx = null);
    Task EnsureSchemaTable(CancellationToken token = default,IDbTransaction? trans = null);

    Task RemoveEntityInTopMenuBar(Entity entity, CancellationToken ct,IDbTransaction? tx= null);
    public Task EnsureEntityInTopMenuBar(Entity entity, CancellationToken ct,IDbTransaction? tx = null);
}