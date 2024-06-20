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

    public Query? One(string key)
    {
         //todo: need to judge pk data type
         return !int.TryParse(key, out int id) ? null:
         Basic().Where(DataKey, id).Select(Columns.Filter(x=>x.InDetail).Select(c=>c.Field));
    }
    public Query All()
    {
        return Basic().Select(Columns.Filter(x=>x.InList).Select(c=>c.Field));
    }

    public Query Insert(Record item)
    {
        return new Query(TableName).AsInsert(item, true);
    }

    public Query? Update(Record item)
    {
        return item.TryGetValue(DataKey, out object val)
            ? new Query(TableName).Where(DataKey, val).AsUpdate(item.Keys, item.Values)
            : null;
    }

    public Query? Delete(Record item)
    {
        return item.TryGetValue(DataKey, out object key)?
         new Query(TableName).Where(DataKey, key).AsDelete():null;
    }
}