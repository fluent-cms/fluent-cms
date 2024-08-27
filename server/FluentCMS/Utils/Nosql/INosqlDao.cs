using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Utils.Nosql;

public interface INosqlDao
{
    Task Upsert(string collectionName, string id, Record item);
    Task Delete(string collectionName, string id);
    Task BatchInsert(string collectionName, IEnumerable<Record> items);
    Task<Result<Record[]>> Query(string collectionName, Filters filters, Sorts? sorts = null, Cursor? cursor = null, Pagination? pagination = null);
}