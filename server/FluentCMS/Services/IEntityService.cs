using FluentCMS.Models.Queries;

namespace FluentCMS.Services;

public struct EntityList
{
    public dynamic[] Items { get; set; }
    public int TotalRecords { get; set; }
    public string Cursor { get; set; }
    public string HasMore { get; set; }
}

public interface IEntityService
{
    Task<EntityList?> GetAll(string entityName);
    Task<int?> Insert(string entityName, Record item);
    Task<int?> Update(string entityName, Record item);
    Task<int?> Delete(string entityName, Record item);
    Task<object?> GetOne(string entityName, string id);
}