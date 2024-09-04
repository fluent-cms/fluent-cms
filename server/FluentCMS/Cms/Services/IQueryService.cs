using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Services;

public interface IQueryService
{
    Task<RecordViewResult> List(string queryName, Cursor cursor, Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken);
    Task<Record> One(string queryName, Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken);
    Task<Record[]> Many(string queryName, Pagination? pagination, Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken);
}