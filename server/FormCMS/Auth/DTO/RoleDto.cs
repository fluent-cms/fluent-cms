namespace FormCMS.Auth.DTO;

public record RoleDto(
    string Name,
    string[] ReadWriteEntities,
    string[] RestrictedReadWriteEntities,
    string[] ReadonlyEntities,
    string[] RestrictedReadonlyEntities
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