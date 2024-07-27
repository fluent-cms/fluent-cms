using System.Text.Json;
using Microsoft.Extensions.Primitives;
using Utils.QueryBuilder;
using Attribute = Utils.QueryBuilder.Attribute;

namespace FluentCMS.Services;

public interface IEntityService
{
    Task<ListResult> List(string entityName,Pagination? pagination, Dictionary<string, StringValues> qs);
    Task<int> Insert(string entityName, JsonElement item);
    Task<int> Update(string entityName, JsonElement item);
    Task<int> Delete(string entityName, JsonElement item);
    Task<Record?> One(string entityName, string strId);
    Task<ListResult> CrosstableList(string entityName, string strId, string field, bool exclude);
    Task<int> CrosstableSave(string entityName, string strId, string field, JsonElement[] items);
    Task<int> CrosstableDelete(string entityName, string strId, string attributeName, JsonElement[] elements);

    Task AttachLookup(Attribute lookupAttribute, Record[] items, Func<Entity, Attribute[]> getFields);

    Task AttachCrosstable(Attribute crossTableAttribute, Record[] items, Func<Entity, Attribute[]> getFields);
}