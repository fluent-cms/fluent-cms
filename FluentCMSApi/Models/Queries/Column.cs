using System.ComponentModel.DataAnnotations;

namespace FluentCMSApi.models;

public class Column
{
    public string Field { get; set; } = "";
    public string Header { get; set; } = "";
    public bool InList { get; set; } = false;
    public bool InDetail { get; set; } = false;
    public string Type { get; set; } = "";
    public string[] Options { get; set; } = [];
    public int EntityId { get; set; } = 0;
    public virtual Entity Entity { get; set; } = null!;
}