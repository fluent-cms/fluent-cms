using System.Text.Json.Serialization;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentResults;

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


    private string _name = "";
    private string _tableName = "";
    private string _primaryKey = "";
    private string _titleAttribute = "";

    public string Name
    {
        get => _name;
        set => _name = value.Replace(" ", string.Empty).ToLower(); // make sure it is url friendly
    }

    public string TableName
    {
        get => _tableName;
        set => _tableName = value.Trim();
    }

    public string Title { get; set; } = "";

    public string PrimaryKey
    {
        get => string.IsNullOrWhiteSpace(_primaryKey) ? "id" : _primaryKey;
        set => _primaryKey = value.Trim();
    }

    public string TitleAttribute
    {
        get => _titleAttribute;
        set => _titleAttribute = value.Trim();
    }

    public int DefaultPageSize { get; set; } = 20;

    public Attribute[] Attributes { get; set; } = [];

    public Attribute PrimaryKeyAttribute() => Attributes.First(x => x.Field == PrimaryKey);
    public Attribute? DisplayTitleAttribute() => Attributes.FirstOrDefault(x => x.Field == TitleAttribute);
    public Attribute DeleteAttribute() => new Attribute { Parent = this, Field = "deleted"};

    public void Init()
    {
        foreach (var attribute in Attributes)
        {
            attribute.Parent = this;
        }
    }

    public Result<SqlKata.Query> OneQuery(Filters? filters, Attribute[] attributes)
    {
        var query = Basic().Select(attributes.Select(x => x.FullName()));
        var result = query.ApplyFilters(filters); //filters.Apply(this, query);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        return query;
    }

    public SqlKata.Query ByIdQuery(object id, Attribute[] attributes, Filters? filters)
    {
        var query = Basic().Where(PrimaryKey, id)
            .Select(attributes.Select(x => x.FullName()));
        query.ApplyFilters(filters);
        return query;
    }

    public Result<SqlKata.Query> ListQuery(Filters? filters, Sorts? sorts, Pagination? pagination, Cursor? cursor,
        Attribute[] attributes, Func<Attribute, string, object> cast)
    {
        var query = Basic().Select(attributes.Select(x => x.FullName()));

        var cursorResult = query.ApplyCursor(cursor, sorts);
        if (cursorResult.IsFailed) return Result.Fail(cursorResult.Errors);

        query.ApplyPagination(pagination);
        query.ApplySorts(sorts);
        query.ApplyFilters(filters);
        return query;
    }

    public SqlKata.Query CountQuery(Filters? filters)
    {
        var query = Basic();
        query.ApplyFilters(filters);
        return query;
    }


    public Result<SqlKata.Query> ManyQuery(object[] ids, Attribute[] attributes)
    {
        if (ids.Length == 0)
        {
            Result.Fail("ids is empty");
        }

        var lstFields = attributes.Select(x => x.Field);
        return Basic().Select(lstFields.ToArray()).WhereIn(PrimaryKey, ids);
    }

    public SqlKata.Query Insert(Record item)
    {
        //omit auto generated value
        if (PrimaryKeyAttribute().IsDefault)
        {
            item.Remove(PrimaryKey);
        }

        return new SqlKata.Query(TableName).AsInsert(item, true);
    }

    public Result<SqlKata.Query> UpdateQuery(Record item)
    {
        //to prevent SqlServer 'Cannot update identity column' error 
        if (!item.Remove(PrimaryKey, out var val))
        {
            return Result.Fail($"Failed to get value with primary key [${PrimaryKey}]");
        }

        var ret = new SqlKata.Query(TableName).Where(PrimaryKey, val).AsUpdate(item.Keys, item.Values);
        item[PrimaryKey] = val;
        return ret;
    }

    public Result<SqlKata.Query> DeleteQuery(Record item)
    {
        return item.TryGetValue(PrimaryKey, out var key)
            ? Result.Ok(new SqlKata.Query(TableName).Where(PrimaryKey, key).AsUpdate([DeletedFieldName], [true]))
            : Result.Fail($"Failed to get value with primary key [${PrimaryKey}]");
    }

    public SqlKata.Query Basic() => new SqlKata.Query(TableName).Where(TableName + $".{DeletedFieldName}", false);
      public void LoadAttributesByColDefines(ColumnDefinition[] cols)
    {
        Attributes = cols.Select(x => new Attribute(x)).ToArray();
    }

    public ColumnDefinition[] AddedColumnDefinitions(ColumnDefinition[] columnDefinitions)
    {
        var set = columnDefinitions.Select(x => x.ColumnName.ToLower()).ToHashSet();
        var items = Attributes.GetLocalAttributes().Where(x => !set.Contains(x.Field.ToLower().Trim()));
        return items.Select(x => new ColumnDefinition { ColumnName = x.Field, DataType = x.DataType }).ToArray();
    }

    public ColumnDefinition[] ColumnDefinitions()
    {
        return Attributes.GetLocalAttributes()
            .Select(x => new ColumnDefinition { ColumnName = x.Field, DataType = x.DataType })
            .ToArray();
    }

    public void EnsureDefaultAttribute()
    {
        var list = new List<Attribute>();
        if (Attributes.FindOneAttribute(DefaultPrimaryKeyFieldName) is null)
        {
            list.Add(new Attribute
            {
                Field = DefaultPrimaryKeyFieldName, Header = "id",
                InList = true, InDetail = true, IsDefault = true,
                DataType = DataType.Int, Type = DisplayType.text
            });
        }

        list.AddRange(Attributes);

        if (Attributes.FindOneAttribute(CreatedAtField) is null)
        {
            list.Add(new Attribute
            {
                Field = CreatedAtField, Header = "Created At", InList = true, InDetail = true, IsDefault = true,
                DataType = DataType.Datetime
            });
        }

        if (Attributes.FindOneAttribute(UpdatedAtField) is null)
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
        if (Attributes.FindOneAttribute(DeletedFieldName) == null)
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
}