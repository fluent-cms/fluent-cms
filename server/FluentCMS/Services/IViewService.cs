using FluentCMS.Models.Queries;

namespace FluentCMS.Services;

public interface IViewService
{
    Task<RecordList?> List(string viewName, Pagination? pagination);
}