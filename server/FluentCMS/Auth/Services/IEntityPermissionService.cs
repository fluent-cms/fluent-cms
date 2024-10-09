using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Auth.Services;

public interface IEntityPermissionService
{
    void List(string entityName, Filters filters);
    Task GetOne(string entityName, string recordId);
    Task Change(string entityName, string recordId);
    void Create(string entityName);
    void AssignCreatedBy(Record record);
}