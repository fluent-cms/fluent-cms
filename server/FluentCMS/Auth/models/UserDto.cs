using System.Collections.Immutable;

namespace FluentCMS.Auth.models;

public sealed record UserDto(
    string Email ,
    ImmutableArray<string> Roles = default,
    ImmutableArray<string> ReadWriteEntities = default,
    ImmutableArray<string> RestrictedReadWriteEntities = default,
    ImmutableArray<string> ReadonlyEntities =default ,
    ImmutableArray<string> RestrictedReadonlyEntities =default,
    ImmutableArray<string> AllowedMenus =default,
    string Id = ""
);


public static class UserConstants
{
    public const string MenuSchemaBuilder = "menu_schema_builder";
    public const string MenuUsers = "menu_users";
    public const string MenuRoles = "menu_roles";
}