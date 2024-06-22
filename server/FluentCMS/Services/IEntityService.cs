using System.Text.Json;
using FluentCMS.Utils.Dao;

namespace FluentCMS.Services;
using Record = IDictionary<string,object>;

public struct EntityList
{
    public Record[]? Items { get; set; }
    public int TotalRecords { get; set; }
    public string Cursor { get; set; }
    public string HasMore { get; set; }
}

public interface IEntityService
{
    Task<EntityList?> List(string entityName);
    Task<int?> Insert(string entityName, JsonElement item);
    Task<int?> Update(string entityName, JsonElement item);
    Task<int?> Delete(string entityName, JsonElement item);
    Task<object?> One(string entityName, string id);
}