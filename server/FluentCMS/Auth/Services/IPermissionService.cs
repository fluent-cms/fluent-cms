using FluentCMS.Models;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Auth.Services;

public interface IPermissionService
{
    void CheckEntityReadPermission(RecordMeta meta, Filters filters);
    Task CheckEntityAccessPermission(RecordMeta meta);
    void AssignCreatedBy(Record record);
    Task HandleSchema(Schema schema);
}