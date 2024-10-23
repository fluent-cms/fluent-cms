using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Auth.Services;

public interface IEntityPermissionService
{
    ImmutableArray<ValidFilter> List(string entityName, ImmutableArray<ValidFilter> filters);
    Task GetOne(string entityName, string recordId);
    Task Change(string entityName, string recordId);
    void Create(string entityName);
    void AssignCreatedBy(Record record);
}