using FluentCMS.Models;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Auth.Services;

public interface IPermissionService
{
    Task CheckEntity(RecordMeta meta);
    void AssignCreatedBy(Record record);
    Task HandleSchema(Schema schema);
}