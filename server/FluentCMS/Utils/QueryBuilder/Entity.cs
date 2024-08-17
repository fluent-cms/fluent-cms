using FluentCMS.Utils.DataDefinitionExecutor;
using FluentResults;
using SqlKata;

namespace FluentCMS.Utils.QueryBuilder;
public enum InListOrDetail
{
    InList,
    InDetail,
}
public sealed class Entity
{
    private const string DefaultPrimaryKeyFieldName = "id";
    private const string DeletedFieldName = "deleted";
    private const string CreatedAtField = "created_at";
    private const string UpdatedAtField = "updated_at";
   
    
    private string _name ="";
    private string _tableName ="";
    private string _primaryKey ="";
    private string _titleAttribute="";
    public string Name {
        get => _name;
        set => _name = value.Replace(" ", string.Empty).ToLower(); // make sure it is url friendly
    }
    public string TableName
    {
        get =>  _tableName;
        set => _tableName = value.Trim();
    }
    public string Title { get; set; } = "";

    public string PrimaryKey
    {
        get => string.IsNullOrWhiteSpace(_primaryKey) ? "id": _primaryKey;
        set =>_primaryKey =value.Trim(); 
    }

    public string TitleAttribute
    {
        get => _titleAttribute;
        set => _titleAttribute = value.Trim();
    }
    
    public int DefaultPageSize { get; set; } = 20;
    //must be public to expose to json parser
    public Attribute[] Attributes { get; set; } = [];

    public void Init()
    {
        foreach (var attribute in Attributes)
        {
            attribute.Parent = this;
        }
    }
    public void LoadAttributesByColDefines(ColumnDefinition[] cols )
    {
        Attributes = cols.Select(x => new Attribute(x)).ToArray();
    }
    
    public ColumnDefinition[] AddedColumnDefinitions(ColumnDefinition[] columnDefinitions)
    {
        var set = columnDefinitions.Select(x => x.ColumnName.ToLower()).ToHashSet();
        var attr = LocalAttributes();
        var items = attr.Where(x=>!set.Contains(x.Field.ToLower().Trim()));
        return items.Select(x => new ColumnDefinition { ColumnName = x.Field, DataType = x.DataType}).ToArray();
    }

    public ColumnDefinition[] ColumnDefinitions()
    {
        return LocalAttributes().Select(x => new ColumnDefinition { ColumnName = x.Field, DataType = x.DataType })
            .ToArray();
    }
    public void EnsureDefaultAttribute()
    {
        var list = new List<Attribute>();
        if (FindOneAttribute(DefaultPrimaryKeyFieldName) is null)
        {
            list.Add(new Attribute
            {
                Field = DefaultPrimaryKeyFieldName, Header = "id",
                InList = true, InDetail = true, IsDefault = true, 
                DataType = DataType.Int, Type = DisplayType.text
            });
        }
        
        list.AddRange(Attributes);

        if (FindOneAttribute(CreatedAtField) is null)
        {
            list.Add(new Attribute
            {
                Field = CreatedAtField, Header = "Created At", InList = true, InDetail = true, IsDefault = true,
                DataType = DataType.Datetime
            });
        }

        if (FindOneAttribute(UpdatedAtField) is null)
        {
            list.Add(new Attribute
            {
                Field = UpdatedAtField, Header = "Updated At", InList = true, InDetail = true, IsDefault = true,
                DataType = DataType.Datetime
            });
        }

        Attributes = list.ToArray();
    }

    public void EnsureDeleted()
    {
        if (FindOneAttribute(DeletedFieldName) == null)
        {
            Attributes = Attributes.Append(new Attribute
                {
                    Field = DeletedFieldName, InList = true, InDetail = true, IsDefault = true, DataType = DataType.Int
                }
            ).ToArray();
        }
    }

    public void RemoveDeleted()
    {
        Attributes = Attributes.Where(x => x.Field != DeletedFieldName).ToArray();
    }

    public Attribute[] ReferencedAttributes()
    {
        return Attributes.Where(x => x.Field == PrimaryKey || x.Field == TitleAttribute).ToArray();
    }

    public string Fullname(string fieldName)
    {
        return TableName + "." + fieldName;
    }

    public Attribute[] LocalAttributes()
    {
        return Attributes.Where(x=>x.Type != DisplayType.crosstable).ToArray();
    }
    
    public Attribute[] LocalAttributes(InListOrDetail listOrDetail)
    {
        return Attributes.Where(x =>
                x.Type != DisplayType.crosstable && 
                (listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail))
            .ToArray();
    }

    public Attribute[] LocalAttributes(string[] attributes)
    {
        return Attributes
            .Where(x => x.Type != DisplayType.crosstable && attributes.Contains(x.Field)).ToArray();
    }

    public Attribute[] GetAttributesByType(DisplayType type)
    {
        return Attributes.Where(x => x.Type == type).ToArray();
    }

    public Attribute[] GetAttributesByType(DisplayType type, string[] attributes)
    {
        return Attributes.Where(x => x.Type == type && attributes.Contains(x.Field)).ToArray();
    }
     public Attribute[] GetAttributesByType(DisplayType type, InListOrDetail listOrDetail)
     {
         return Attributes.Where(x => x.Type == type && (listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail))
             .ToArray();
     }
   

    public Attribute PrimaryKeyAttribute()
    {
        return Attributes.First(x => x.Field == PrimaryKey);
    }
    
    public Attribute? FindOneAttribute(string name)
    {
        return Attributes.FirstOrDefault(x => x.Field == name);
    }
    public Result<Query> OneQuery(Filters? filters, Attribute[]attributes)
    {
        var query = Basic().Select(attributes.Select(x=>x.FullName()));
        if (filters is not null)
        {
            var result = filters.Apply(this, query);
            if (result.IsFailed)
            {
                return Result.Fail(result.Errors);
            }
        }
        return query;
    }
    
    public Query ByIdQuery(object id, Attribute[]attributes)
    {
        return Basic().Where(PrimaryKey, id)
            .Select(attributes.Select(x=>x.FullName()));
    }

    public Query ListQuery(Filters? filters, Sorts? sorts, Pagination? pagination, Cursor? cursor,
        Attribute[] attributes, Func<Attribute, string, object> cast)
    {
        var query = Basic().Select(attributes.Select(x => x.FullName()));
        pagination?.Apply(query);
        cursor?.Apply(this, query, sorts, cast);
        sorts?.Apply(this, query);
        filters?.Apply(this, query);
        return query;
    }

    public Query CountQuery(Filters? filters)
    {
        var query = Basic();
        filters?.Apply(this, query);
        return query;
    }


    public Result<Query> ManyQuery(object[]ids, Attribute[] attributes)
    {
        if (ids.Length == 0)
        {
            Result.Fail("ids is empty");
        }
        var lstFields = attributes.Select(x => x.Field);
        return Basic().Select(lstFields.ToArray()).WhereIn(PrimaryKey,ids);
    }

    public Query Insert(Record item)
    {
        //omit auto generated value
        if (PrimaryKeyAttribute().IsDefault)
        {
            item.Remove(PrimaryKey);
        }
        return new Query(TableName).AsInsert(item, true);
    }

    public Result<Query> UpdateQuery(Record item)
    {
        //to prevent SqlServer 'Cannot update identity column' error 
        if (!item.Remove(PrimaryKey, out var val))
        {
            return Result.Fail($"Failed to get value with primary key [${PrimaryKey}]");
        }
        var ret = new Query(TableName).Where(PrimaryKey, val).AsUpdate(item.Keys, item.Values);
        item[PrimaryKey] = val;
        return ret;
    }

    public Result<Query> DeleteQuery(Record item)
    {
        return item.TryGetValue(PrimaryKey, out var key)
            ? Result.Ok(new Query(TableName).Where(PrimaryKey, key).AsUpdate([DeletedFieldName], [true]))
            : Result.Fail($"Failed to get value with primary key [${PrimaryKey}]");
    }
    
    public Query Basic() =>  new Query(TableName).Where(TableName + $".{DeletedFieldName}", false);
}