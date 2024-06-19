namespace FluentCMS.Services;

public struct EntityList
{
    public dynamic[] Items { get; set; }
    public int TotalRecords { get; set; }
    public string Cursor { get; set; }
    public string HasMore { get; set; }
}

public interface IEntityService
{
    Task<EntityList?> GetAll(string entityName);
}