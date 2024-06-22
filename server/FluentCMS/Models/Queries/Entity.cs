using FluentCMS.Utils.Dao;
using SqlKata;

namespace FluentCMS.Models.Queries;
using Record = IDictionary<string,object>;

public class Entity
{
    public Entity(){}
    public void SetAttributes(ColumnDefinition[] cols )
    {
        Attributes = cols.Select(x => new Attribute(x)).ToArray();
    }
    public string TableName { get; set; } = "";
    public string Title { get; set; } = "";
    public string PrimaryKey { get; set; } = "";
    public string TitleAttribute { get; set; } = "";
    
    public int DefaultPageSize { get; set; } = 20;
    //must be public to expose to json parser
    public Attribute[] Attributes { get; set; } = [];
    public Attribute[] ListLookups()
    {
        return Attributes.Where(x => x.InList && x.Type == DisplayType.lookup).ToArray();
    }
    public Attribute[] Lookups()
    {
        return Attributes.Where(x=>x.Type == DisplayType.lookup).ToArray();
    }

    public Attribute[] DetailLookups()
    {
        return Attributes.Where(x => x.InDetail && x.Type == DisplayType.lookup).ToArray();
    }
    public Attribute[] AttributesForLookup()
    {
        return Attributes.Where(x => x.Field == PrimaryKey || x.Field == TitleAttribute).ToArray();
    }

    public Attribute[] ListAttributes()
    {
        return Attributes.Where(x => x.InList).ToArray();
    }

    public Attribute[] DetailAttributes()
    {
        return Attributes.Where(x => x.InDetail).ToArray();
    }

    public Func<string,object>? GetDetailFieldParser(string field)
    {
        var attr = Attributes.FirstOrDefault(x => x.Field == field && x.InDetail);
        if (attr is null)
        {
            return null;
        }
        return attr.CastToDatabaseType;
    }

    public Attribute KeyAttribute()
    {
        return Attributes.First(x => x.Field == PrimaryKey);
    }

    public object[] Ids(Record[] items)
    {
        return items.Select((x => x[PrimaryKey])).ToArray();
    }
    public Query? One(string key)
    {
        var id = KeyAttribute().CastToDatabaseType(key);
        return Basic().Where(PrimaryKey, id).Select(Attributes.Filter(x=>x.InDetail).Select(c=>c.Field));
    }
    public Query List()
    {
        var lstFields = ListAttributes().Select(x=>x.Field);
        return Basic().Select(lstFields.ToArray());
    }
    public Query Many(object[]ids, Attribute[] attributes)
    {
        var lstFields = attributes.Select(x => x.Field);
        return Basic().Select(lstFields.ToArray()).WhereIn(PrimaryKey,ids);
    }

    public Query Insert(Record item)
    {
        return new Query(TableName).AsInsert(item, true);
    }

    public Query? Update(Record item)
    {
        return item.TryGetValue(PrimaryKey, out object val)
            ? new Query(TableName).Where(PrimaryKey, val).AsUpdate(item.Keys, item.Values)
            : null;
    }

    public Query? Delete(Record item)
    {
        return item.TryGetValue(PrimaryKey, out object key)?
         new Query(TableName).Where(PrimaryKey, key).AsDelete():null;
    }
    
    private Query Basic()
    {
        return new Query(TableName).Where("deleted", false);
    }
}