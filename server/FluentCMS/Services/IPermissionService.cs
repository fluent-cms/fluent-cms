using FluentCMS.Models;
using FluentCMS.Utils.QueryBuilder;
using FluentResults;

namespace FluentCMS.Services;

public interface IPermissionService
{
    Task CheckEntityPermission(RecordMeta meta);
    void AssignCreatedBy(Record record);
    Task CheckSchemaPermission(Schema schema);
    void EnsureCreatedByField(Schema schema);
}