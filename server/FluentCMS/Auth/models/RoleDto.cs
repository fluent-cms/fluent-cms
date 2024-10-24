using System.Collections.Immutable;

namespace FluentCMS.Auth.models;

public record RoleDto(
    string Name,
    ImmutableArray<string> ReadWriteEntities,
    ImmutableArray<string> RestrictedReadWriteEntities,
    ImmutableArray<string> ReadonlyEntities,
    ImmutableArray<string> RestrictedReadonlyEntities
);

public static class RoleConstants
{
    /// <summary>
    /// super admin:
    /// schema: any schema,
    /// data: any entity
    /// </summary>
    public const string Sa = "sa"; 
    
    /// <summary>
    /// admin
    /// schema : only entity and view, only his own schema
    /// </summary>
    public const string Admin = "admin";
    
}