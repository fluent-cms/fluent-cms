using FluentCMS.Cms.Models;
using FluentResults;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

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
    Task<Schema[]> All(string type,  IEnumerable<string>? names,CancellationToken cancellationToken);
    Task<Schema[]> AllWithAction(string type,CancellationToken cancellationToken);
    
    Task<Schema?> ByIdWithAction(int id, CancellationToken cancellationToken = default);
    Task<Schema?> ById(int id, CancellationToken cancellationToken = default);
    
    public Task EnsureEntityInTopMenuBar(Entity entity, CancellationToken cancellationToken);
    Task<Result> NameNotTakenByOther(Schema schema, CancellationToken cancellationToken);
    Task<Schema?> GetByNameDefault(string name, string type, CancellationToken cancellationToken = default);
    Task<Schema?> GetByNamePrefixDefault(string name, string type, CancellationToken cancellationToken = default);
    Task<Schema> SaveWithAction(Schema schema, CancellationToken cancellationToken);
    Task Delete(int id, CancellationToken cancellationToken);
    Task EnsureTopMenuBar(CancellationToken cancellationToken);
    Task EnsureSchemaTable(CancellationToken cancellationToken);
}