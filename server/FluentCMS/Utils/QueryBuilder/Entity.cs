using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicExpresso;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.KateQueryExt;
using FluentResults;
using Microsoft.EntityFrameworkCore;

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

    public Result ValidateLocalAttributes(Record record)
    {
        var interpreter = new Interpreter();
        interpreter.Reference(typeof(Regex));
        var errs = new List<IError>();
        foreach (var localAttribute in Attributes.GetLocalAttributes().Where(x=>!string.IsNullOrWhiteSpace(x.Validation)))
        {
            var res = Validate(localAttribute);
            
            if (res.IsFailed)
            {
                errs.AddRange(res.Errors);
            }
        }
        return errs.Count == 0 ? Result.Ok():Result.Fail(errs);

        Result Validate(Attribute attribute)
        {
            record.TryGetValue(attribute.Field, out var value);
            var typeOfAttribute = attribute.DataType switch
            {
                DataType.Int => typeof(int),
                DataType.Datetime => typeof(DateTime),
                _=> typeof(string)
            };

            var err = string.IsNullOrWhiteSpace(attribute.ValidationMessage) ? null : attribute.ValidationMessage;
            try
            {
                if (!interpreter.Eval<bool>(attribute.Validation, new Parameter(attribute.Field, typeOfAttribute, value)))
                {
                    return Result.Fail(err ?? $"Validate fail for {attribute.Header}");
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(err ?? $"validate fail for {attribute.Header}, ex = {ex.Message}");
            }
        }
    }
    
    public Result<Record> Parse (JsonElement jsonElement, Func<Attribute, string, object> cast)
    {
        Dictionary<string, object> ret = new();
        foreach (var property in jsonElement.EnumerateObject())
        {
            var attribute = Attributes.FindOneAttribute(property.Name);
            if (attribute == null) continue;
            var res = attribute.Type switch
            {
                DisplayType.lookup => SubElement(property.Value, attribute.Lookup!.PrimaryKey).Bind(x=>Convert(x, attribute)),
                _ => Convert(property.Value, attribute)
            };
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }

            ret[property.Name] = res.Value;
        }
        return ret;
        
        Result<JsonElement?> SubElement(JsonElement element, string key)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out JsonElement subElement))
            {
                return subElement;
            }

            return Result.Ok<JsonElement?>(null!);
        }
        
        Result<object> Convert(JsonElement? element, Attribute attribute)
        {
            if (element is null)
            {
                return Result.Ok<object>(null!);
            }
            return element.Value.ValueKind switch
            {
                JsonValueKind.String => cast(attribute, element.Value.GetString()!),
                JsonValueKind.Number when element.Value.TryGetInt32(out var intValue) => intValue,
                JsonValueKind.Number when element.Value.TryGetInt64(out var longValue) => longValue,
                JsonValueKind.Number when element.Value.TryGetDouble(out var doubleValue) => doubleValue,
                JsonValueKind.Number => element.Value.GetDecimal(),
                JsonValueKind.True => Result.Ok<object>(true),
                JsonValueKind.False => Result.Ok<object>(false),
                JsonValueKind.Null => Result.Ok<object>(null!),
                JsonValueKind.Undefined => Result.Ok<object>(null!),
                _ => Result.Fail<object>($"value kind {element.Value.ValueKind} is not supported")
            };
        }
    }
}