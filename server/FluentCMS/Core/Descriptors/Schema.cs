namespace FluentCMS.Core.Descriptors;
public static class  SchemaType
{
    public const string Menu = "menu";
    public const string Entity = "entity";
    public const string Query = "query";
    public const string Page = "page";
}


public sealed record Settings(Entity? Entity = null, Query? Query =null, Menu? Menu =null, Page? Page = null);
public record Schema(string Name, string Type, Settings Settings, int Id = 0, string CreatedBy ="");