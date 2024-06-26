using System.Text.Json;
using FluentCMS.Models.Queries;
using Attribute = FluentCMS.Models.Queries.Attribute;

namespace FluentCMS.Services;
using Record = IDictionary<string,object>;



public interface IEntityService
{
    Task<ListResult?> List(string entityName,Pagination? pagination, Sorts? sorts,Filters? filters);
    Task<int?> Insert(string entityName, JsonElement item);
    Task<int?> Update(string entityName, JsonElement item);
    Task<int?> Delete(string entityName, JsonElement item);
    Task<Record?> One(string entityName, string strId);
    Task<ListResult?> CrosstableList(string entityName, string strId, string field, bool exclude);
    Task<int?> CrosstableSave(string entityName, string strId, string field, JsonElement[] items);
    Task<int?> CrosstableDelete(string entityName, string strId, string attributeName, JsonElement[] elements);

    Task AttachLookup(Attribute lookupAttribute, IDictionary<string, object>[] items,
        Func<Entity, Attribute[]> getFields);

    Task AttachCrosstable(Attribute crossTableAttribute, IDictionary<string, object>[] items,
        Func<Entity, Attribute[]> getFields);
}