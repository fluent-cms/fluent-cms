using System.Text.Json;

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
    Task<Record?> One(string entityName, string strId);
    Task<EntityList?> CrosstableList(string entityName, string strId, string field, bool exclude);
    Task<int?> CrosstableSave(string entityName, string strId, string field, JsonElement[] items);
    Task<int?> CrosstableDelete(string entityName, string strId, string attributeName, JsonElement[] elements);
}