using FluentCMS.Models;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Auth.Services;

public interface IPermissionService
{
    void CheckEntityReadPermission(EntityMeta meta, Filters filters);
    Task CheckEntityAccessPermission(EntityMeta meta);
    void AssignCreatedBy(Record record);
    Task HandleSchema(Schema schema);
}