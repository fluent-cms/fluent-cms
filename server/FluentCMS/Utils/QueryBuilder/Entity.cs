using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicExpresso;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentCMS.Utils.KateQueryExt;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;
public enum InListOrDetail
{
    InList,
    InDetail,
}
public record Entity(
    ImmutableArray<Attribute> Attributes,
    string Name = "",
    string TableName = "",
    string PrimaryKey ="",
    string Title ="",
    string TitleAttribute ="",
    int DefaultPageSize = EntityConstants.DefaultPageSize
);

public record LoadedEntity(
    ImmutableArray<LoadedAttribute> Attributes,
    LoadedAttribute PrimaryKeyAttribute,
    LoadedAttribute LoadedTitleAttribute,
    LoadedAttribute DeletedAttribute,
    string Name,
    string TableName,
    string PrimaryKey = DefaultFields.Id,
    string Title = "", //needed by admin panel
    string TitleAttribute = "",
    int DefaultPageSize = EntityConstants.DefaultPageSize
    ) ;

public static class EntityConstants
{
    public const int DefaultPageSize = 50;
}

public static class EntityHelper
{

    public static LoadedEntity ToLoadedEntity(this Entity entity, Func<string, string,object> cast)
    {
        var primaryKey = entity.Attributes.FindOneAttribute(entity.PrimaryKey)!.ToLoaded(entity.TableName);
        var titleAttribute = entity.Attributes.FindOneAttribute(entity.TitleAttribute)?.ToLoaded(entity.TableName) ?? primaryKey;
        var attributes = entity.Attributes.Select(x => x.ToLoaded(entity.TableName));
        var deletedAttribute = new LoadedAttribute([],entity.TableName, DefaultFields.Deleted);
        return new LoadedEntity(
            [..attributes],
            PrimaryKeyAttribute:primaryKey,
            LoadedTitleAttribute: titleAttribute,
            DeletedAttribute:deletedAttribute,
            entity.Name,
            entity.TableName,
            entity.PrimaryKey,
            entity.Title,
            entity.TitleAttribute,
            entity.DefaultPageSize
        );
    }
    
    public static Result<SqlKata.Query> OneQuery(this LoadedEntity e,ImmutableArray<ValidFilter> filters, IEnumerable<LoadedAttribute> attributes)
    {
        var query = e.Basic().Select(attributes.Select(x => x.GetFullName()));
        query.ApplyJoin(filters.Select(x=>x.Vector));
        var result = query.ApplyFilters(filters); //filters.Apply(this, query);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        return query;
    }

    public static SqlKata.Query ByIdQuery(this LoadedEntity e,object id, IEnumerable<LoadedAttribute> attributes, IEnumerable<ValidFilter> filters)
    {
        var query = e.Basic().Where(e.PrimaryKey, id)
            .Select(attributes.Select(x => x.GetFullName()));
        query.ApplyFilters(filters);
        return query;
    }

    public static Result<SqlKata.Query> ListQuery(this LoadedEntity e,ImmutableArray<ValidFilter> filters, ImmutableArray<ValidSort> sorts, 
        ValidPagination pagination, ValidCursor? cursor, IEnumerable<LoadedAttribute> attributes)
    {
        var query = e.Basic().Select(attributes.Select(x => x.GetFullName()));
        query.ApplyJoin([..filters.Select(x=>x.Vector),..sorts.Select(x=>x.Vector)]);
        var cursorResult = query.ApplyCursor(cursor, sorts);
        if (cursorResult.IsFailed) return Result.Fail(cursorResult.Errors);
        query.ApplyPagination(pagination);
        if (cursor?.Cursor.IsForward() == false && sorts != null)
        {
            query.ApplySorts(sorts.ReverseOrder());
        }
        else
        {
            query.ApplySorts(sorts);
        }
        
        query.ApplyFilters(filters);
        return query;
    }

    public static SqlKata.Query CountQuery(this LoadedEntity e,ImmutableArray<ValidFilter> filters)
    {
        var query = e.Basic();
        query.ApplyJoin(filters.Select(x=>x.Vector));
        query.ApplyFilters(filters);
        return query;
    }


    public static Result<SqlKata.Query> ManyQuery(this LoadedEntity e,ImmutableArray<object> ids, IEnumerable<LoadedAttribute> attributes)
    {
        if (!ids.Any())
        {
            Result.Fail("ids is empty");
        }

        var lstFields = attributes.Select(x => x.Field);
        return e.Basic().Select(lstFields.ToArray()).WhereIn(e.PrimaryKey, ids);
    }

    public static SqlKata.Query Insert(this LoadedEntity e, Record item)
    {
        //omit auto generated value
        if (e.PrimaryKeyAttribute.IsDefault)
        {
            item.Remove(e.PrimaryKey);
        }

        return new SqlKata.Query(e.TableName).AsInsert(item, true);
    }

    public static Result<SqlKata.Query> UpdateQuery(this LoadedEntity e, Record item)
    {
        //to prevent SqlServer 'Cannot update identity column' error 
        if (!item.Remove(e.PrimaryKey, out var val))
        {
            return Result.Fail($"Failed to get value with primary key [${e.PrimaryKey}]");
        }

        var ret = new SqlKata.Query(e.TableName).Where(e.PrimaryKey, val).AsUpdate(item.Keys, item.Values);
        item[e.PrimaryKey] = val;
        return ret;
    }

    public static Result<SqlKata.Query> DeleteQuery(this LoadedEntity e,Record item)
    {
        return item.TryGetValue(e.PrimaryKey, out var key)
            ? Result.Ok(new SqlKata.Query(e.TableName).Where(e.PrimaryKey, key).AsUpdate([DefaultFields.Deleted], [true]))
            : Result.Fail($"Failed to get value with primary key [${e.PrimaryKey}]");
    }

    public static SqlKata.Query Basic(this LoadedEntity e) =>
        new SqlKata.Query(e.TableName).Where(e.TableName + $".{DefaultFields.Deleted}", false);
     

    public static ColumnDefinition[] AddedColumnDefinitions(this Entity e, ColumnDefinition[] columnDefinitions)
    {
        var set = columnDefinitions.Select(x => x.ColumnName.ToLower()).ToHashSet();
        var items = e.Attributes.GetLocalAttributes().Where(x => !set.Contains(x.Field.ToLower().Trim()));
        return items.Select(x => new ColumnDefinition(x.Field, x.DataType) ).ToArray();
    }

    public static ColumnDefinition[] ColumnDefinitions(this Entity e)
    {
        return e.Attributes.GetLocalAttributes()
            .Select(x => new ColumnDefinition (ColumnName : x.Field, DataType : x.DataType ))
            .ToArray();
    }

    public static Entity WithDefaultAttr(this Entity e)
    {
        var list = new List<Attribute>();
        if (e.Attributes.FindOneAttribute(DefaultFields.Id) is null)
        {
            list.Add(new Attribute
           ( 
                Field : DefaultFields.Id, Header : "id",
                IsDefault : true,
                DataType : DataType.Int, Type : DisplayType.Number
            ));
        }

        list.AddRange(e.Attributes);

        if (e.Attributes.FindOneAttribute(DefaultFields.CreatedAt) is null)
        {
            list.Add(new Attribute
            (
                Field : DefaultFields.CreatedAt, Header : "Created At", 
                InList : true, InDetail : true, IsDefault : true,
                DataType : DataType.Datetime
            ));
        }

        if (e.Attributes.FindOneAttribute(DefaultFields.UpdatedAt) is null)
        {
            list.Add(new Attribute
            (
                Field : DefaultFields.UpdatedAt, Header : "Updated At", 
                InList : true, InDetail : true, IsDefault : true,
                DataType : DataType.Datetime
            ));
        }

        return e with { Attributes = [..list] };
    }

    public static Result ValidateTitleAttributes(this LoadedEntity e, Record record)
    {
        if (record.TryGetValue(e.TitleAttribute, out var value) && value is not null)
        {
            return Result.Ok();
        }
        return Result.Fail($"Validation fail for {e.TitleAttribute}");
    }
    
    public static Result ValidateLocalAttributes(this LoadedEntity e,Record record)
    {
        var interpreter = new Interpreter();
        interpreter.Reference(typeof(Regex));
        var errs = new List<IError>();
        foreach (var localAttribute in e.Attributes.GetLocalAttributes().Where(x=>!string.IsNullOrWhiteSpace(x.Validation)))
        {
            var res = Validate(localAttribute);
            
            if (res.IsFailed)
            {
                errs.AddRange(res.Errors);
            }
        }
        return errs.Count == 0 ? Result.Ok():Result.Fail(errs);

        Result Validate(LoadedAttribute attribute)
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

    public static Result<Record> Parse (this LoadedEntity e, JsonElement jsonElement)
    {
        Dictionary<string, object> ret = new();
        foreach (var property in jsonElement.EnumerateObject())
        {
            var attribute = e.Attributes.FindOneAttribute(property.Name);
            if (attribute == null) continue;
            var res = attribute.Type switch
            {
                DisplayType.Lookup => SubElement(property.Value, attribute.Lookup!.PrimaryKey).Bind(x=>Convert(x, attribute)),
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
                JsonValueKind.String => attribute.Cast(element.Value.GetString()!),
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