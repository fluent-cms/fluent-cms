using System.Text.Json;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Cms.Services;

public interface IEntityService
{
    Task<ListResult?> List(string name,Pagination pagination, StrArgs args, CancellationToken token);
    Task<Record> Insert(string name, JsonElement item, CancellationToken token = default);
    Task<Record> Update(string name, JsonElement item, CancellationToken token);
    Task<Record> Delete(string name, JsonElement item, CancellationToken token);
    Task<Record> One(string entityName, string strId, CancellationToken token);
    Task<Record> OneByAttributes(string entityName, string strId, string[]attributes, CancellationToken token =default);
    
    Task<ListResult> JunctionList(string name, string id, string attr, bool exclude, StrArgs args, Pagination pagination, CancellationToken token);
    Task<int> JunctionAdd(string name, string id, string attr, JsonElement[] elements, CancellationToken token);
    Task<int> JunctionDelete(string name, string id, string attr, JsonElement[] elements, CancellationToken token);
}