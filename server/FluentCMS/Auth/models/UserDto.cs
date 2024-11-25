using System.Collections.Immutable;

namespace FluentCMS.Auth.models;

public sealed record UserDto(
    string Id ,
    string Email ,
    ImmutableArray<string> Roles ,
    ImmutableArray<string> ReadWriteEntities ,
    ImmutableArray<string> RestrictedReadWriteEntities ,
    ImmutableArray<string> ReadonlyEntities ,
    ImmutableArray<string> RestrictedReadonlyEntities ,
    ImmutableArray<string> AllowedMenus 
);


public static class UserConstants
{
    public const string MenuSchemaBuilder = "menu_schema_builder";
    public const string MenuUsers = "menu_users";
    public const string MenuRoles = "menu_roles";
}