using FluentCMS.Utils.Dao;
using FluentCMS.Utils.Naming;

namespace FluentCMS.Models.Queries;

public class Attribute
{
    public Attribute(){}

    public Attribute(ColumnDefinition col)
    {
        Field = col.ColumnName;
        Header = Naming.SnakeToTitle(col.ColumnName);
        InList = col.DataType != "text";
        InDetail = true;
        Type = "text";

    }
    public string Field { get; set; } = "";
    public string Header { get; set; } = "";
    public bool InList { get; set; } = false;
    public bool InDetail { get; set; } = false;
    public string Type { get; set; } = "";
}