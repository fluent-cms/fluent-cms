namespace FluentCMSApi.models;

public abstract class ModelBase
{
    public int Id { get; set; } = 0;
    public bool Deleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now.ToUniversalTime();
    public DateTime UpdatedAt { get; set; } = DateTime.Now.ToUniversalTime();
}