using FluentCMS.Models.Queries;
using Microsoft.Extensions.Primitives;

namespace FluentCMS.Services;

public interface IViewService
{
    Task<ViewResult?> List(string viewName, Cursor cursor,
        Dictionary<string, StringValues> querystringDictionary);

    Task<Record?> One(string viewName, Dictionary<string, StringValues> querystringDictionary);
    Task<IDictionary<string, object>[]?> Many(string viewName, Dictionary<string, StringValues> querystringDictionary);
}