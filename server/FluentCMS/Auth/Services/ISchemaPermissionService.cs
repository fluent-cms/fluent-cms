using FluentCMS.Cms.Models;
using FluentCMS.Utils.HookFactory;
using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Auth.Services;

public interface ISchemaPermissionService
{
    string[] GetAll();
    void GetOne(string schemaName);
    Task Delete(int schemaId);
    Task<Schema> Save(Schema schema); 
}