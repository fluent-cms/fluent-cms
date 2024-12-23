using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Utils.DocumentDb;

public interface IDocumentDbDao
{
    Task Upsert(string collection, string primaryKey, Record record);
    Task Upsert(string collection, string primaryKey, object primaryKeyValue,object document);
    Task Delete(string collection, string id);
    Task BatchInsert(string collection, IEnumerable<Record> records);
    Task<Result<Record[]>> Query(string collection, IEnumerable<ValidFilter> filters, ValidSort[] sorts, ValidPagination pagination , ValidSpan? span = null );
}