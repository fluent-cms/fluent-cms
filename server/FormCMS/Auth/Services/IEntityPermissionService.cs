using System.Collections.Immutable;
using FormCMS.Core.Descriptors;

namespace FormCMS.Auth.Services;

public interface IEntityPermissionService
{
    ImmutableArray<ValidFilter> List(string entityName, LoadedEntity entity, ImmutableArray<ValidFilter> filters);
    Task GetOne(string entityName, string recordId);
    Task Change(string entityName, string recordId);
    void Create(string entityName);
    void AssignCreatedBy(Record record);
}