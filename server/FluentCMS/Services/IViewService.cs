using Microsoft.Extensions.Primitives;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Services;

public interface IViewService
{
    Task<ViewResult> List(string viewName, Cursor cursor, Dictionary<string, StringValues> querystringDictionary,
        CancellationToken cancellationToken);

    Task<Record> One(string viewName, Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken);
    Task<Record[]> Many(string viewName, Dictionary<string, StringValues> querystringDictionary, CancellationToken cancellationToken);
}