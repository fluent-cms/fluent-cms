namespace FluentCMS.Models.Queries;

public class Pagination
{
    public int First { get; set; }
    public int Rows { get; set; }
    public string Cursor { get; set; } = "";
}
