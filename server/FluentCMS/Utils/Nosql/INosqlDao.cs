using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Utils.Nosql;

public interface INosqlDao
{
    Task Insert(string collectionName, IEnumerable<Record> items);
    Task<Result<Record[]>> Query(string collectionName, Filters filters, Sorts sorts, Cursor cursor);
}