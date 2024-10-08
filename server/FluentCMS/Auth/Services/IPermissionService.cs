using FluentCMS.Cms.Models;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Auth.Services;

public interface IPermissionService
{
    void CheckEntityReadPermission(EntityMeta meta, Filters filters);
    Task CheckEntityAccessPermission(EntityMeta meta);
    void AssignCreatedBy(Record record);
    Task HandleDeleteSchema(SchemaMeta schemaMeta);
    Task HandleSaveSchema(Schema schema);
}