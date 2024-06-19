using FluentCMS.Utils.Dao;
using SqlKata;
using SqlKata.Execution;

namespace FluentCMS.Models.Queries;

public class Entity
{
    public Entity(){}
    public void SetAttributes(ColumnDefinition[] cols )
    {
        Columns = cols.Select(x => new Attribute(x)).ToArray();
    }
    public string TableName { get; set; } = "";
    public string Title { get; set; } = "";
    public string DataKey { get; set; } = ""; 
    public int DefaultPageSize { get; set; } = 20;
    public Attribute[] Columns { get; set; } = [];


    private Query Basic()
    {
        return new Query(TableName).Where("deleted", false);
    }
    public Query All()
    {
        return Basic().Select(Columns.Select(c=>c.Field));
    }
}