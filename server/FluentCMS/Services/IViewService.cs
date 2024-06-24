namespace FluentCMS.Services;

public interface IViewService
{
    Task<RecordList?> List(string viewName);
}