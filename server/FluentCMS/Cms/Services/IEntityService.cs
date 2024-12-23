using System.Text.Json;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;

public interface IEntityService
{
    Task<ListResponse?> List(string name,Pagination pagination, StrArgs args, CancellationToken token);
    Task<Record> Insert(string name, JsonElement item, CancellationToken token = default);
    Task BatchInsert(string name,IEnumerable<string> cols, IEnumerable<IEnumerable<object>> records);
    
    Task<Record> Update(string name, JsonElement item, CancellationToken token);
    Task<Record> Delete(string name, JsonElement item, CancellationToken token);
    Task<Record> Single(string entityName, string strId, CancellationToken ct = default);
    Task<Record> OneByAttributes(string entityName, string strId, string[]attributes, CancellationToken token =default);
    
    Task<ListResponse> JunctionList(string name, string id, string attr, bool exclude, StrArgs args, Pagination pagination, CancellationToken token);
    Task<int> JunctionAdd(string name, string id, string attr, JsonElement[] elements, CancellationToken token = default);
    Task<int> JunctionDelete(string name, string id, string attr, JsonElement[] elements, CancellationToken token);
}