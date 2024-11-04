using System.Collections.Immutable;
using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Cms.Services;

public interface IQueryService
{
    Task<Record[]> List(string queryName, Cursor cursor, Pagination pagination,
        Dictionary<string, StringValues> paginationArgs, Dictionary<string, StringValues> filterArgs,
        CancellationToken cancellationToken);
    Task<Record[]> Many(string queryName,  Dictionary<string, StringValues> paginationArgs, Dictionary<string, StringValues> filterArgs, CancellationToken cancellationToken);
    Task<Record> One(string queryName, Dictionary<string, StringValues> paginationArgs, Dictionary<string, StringValues> filterArgs, CancellationToken cancellationToken);
    Task<Record> Partial(string queryName, string attrPath, Cursor cursor, int limit, CancellationToken cancellationToken);
}