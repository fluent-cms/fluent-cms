using System.Text.Json;

namespace FluentCMS.Services;
using Record = IDictionary<string,object>;



public interface IEntityService
{
    Task<RecordList?> List(string entityName);
    Task<int?> Insert(string entityName, JsonElement item);
    Task<int?> Update(string entityName, JsonElement item);
    Task<int?> Delete(string entityName, JsonElement item);
    Task<Record?> One(string entityName, string strId);
    Task<RecordList?> CrosstableList(string entityName, string strId, string field, bool exclude);
    Task<int?> CrosstableSave(string entityName, string strId, string field, JsonElement[] items);
    Task<int?> CrosstableDelete(string entityName, string strId, string attributeName, JsonElement[] elements);
}