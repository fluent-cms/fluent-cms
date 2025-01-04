using FluentCMS.Core.Descriptors;
using FluentResults;

namespace FluentCMS.CoreKit.DocDbQuery;

public interface IDocumentDbQuery
{
    Task<Result<Record[]>> Query(string collection, IEnumerable<ValidFilter> filters, ValidSort[] sorts, ValidPagination pagination , ValidSpan? span = null );
}