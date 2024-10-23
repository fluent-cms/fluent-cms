using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Utils.Nosql;

public interface INosqlDao
{
    Task Upsert(string collectionName, string id, Record item);
    Task Delete(string collectionName, string id);
    Task BatchInsert(string collectionName, IEnumerable<Record> items);
    Task<Result<Record[]>> Query(string collectionName, IEnumerable<ValidFilter> filters,ValidPagination pagination , ImmutableArray<Sort>? sorts = null, ValidCursor? cursor = null );
}