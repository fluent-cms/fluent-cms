using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicExpresso;

using FluentCMS.Utils.KateQueryExt;
using FluentCMS.Utils.ResultExt;
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
    string PrimaryKey = DefaultFields.Id,
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
    );

public static class EntityConstants
{
    public const int DefaultPageSize = 50;
}

public static class EntityHelper
{
    public static LoadedEntity ToLoadedEntity(this Entity entity)
    {
        var primaryKey = entity.Attributes.FindOneAttr(entity.PrimaryKey)!.ToLoaded(entity.TableName);
        var titleAttribute = entity.Attributes.FindOneAttr(entity.TitleAttribute)?.ToLoaded(entity.TableName) ?? primaryKey;
        var attributes = entity.Attributes.Select(x => x.ToLoaded(entity.TableName));
        var deletedAttribute = new LoadedAttribute(entity.TableName, DefaultFields.Deleted);
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
    
    public static Result<SqlKata.Query> OneQuery(this LoadedEntity e,ValidFilter[] filters, ValidSort[] sorts,IEnumerable<LoadedAttribute> attributes)
    {
        var query = e.Basic().Select(attributes.Select(x => x.AddTableModifier()));
        query.ApplyJoin([..filters.Select(x=>x.Vector),..sorts.Select(x=>x.Vector)]);
        var result = query.ApplyFilters(filters); 
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }
        query.ApplySorts(sorts);
        return query;
    }

    public static SqlKata.Query ByIdQuery(this LoadedEntity e,ValidValue id, IEnumerable<LoadedAttribute> attributes, IEnumerable<ValidFilter> filters)
    {
        var query = e.Basic().Where(e.PrimaryKey, id.Value)
            .Select(attributes.Select(x => x.AddTableModifier()));
        query.ApplyFilters(filters);
        return query;
    }

    public static SqlKata.Query ListQuery(this LoadedEntity e,ValidFilter[] filters, ValidSort[] sorts, 
        ValidPagination? pagination, ValidSpan? cursor, IEnumerable<LoadedAttribute> attributes)
    {
        var query = e.GetCommonListQuery(filters,sorts,pagination,cursor,attributes);
        query.ApplyJoin([..filters.Select(x=>x.Vector),..sorts.Select(x=>x.Vector)]);
        return query;
    }
    
    internal static SqlKata.Query GetCommonListQuery(this LoadedEntity e, 
        IEnumerable<ValidFilter> filters, 
        ValidSort[] sorts, 
        ValidPagination? pagination, 
        ValidSpan? cursor, 
        IEnumerable<LoadedAttribute> attributes)
    {
        var q = e.Basic().Select(attributes.Select(x => x.AddTableModifier()));
        q.ApplyFilters(filters);
        q.ApplySorts(cursor?.Span.IsForward()??false ? sorts.ReverseOrder() : sorts);
        q.ApplyCursor(cursor,sorts);
        if (pagination is not null)
        {
            q.ApplyPagination(pagination);
        }
        return q;
    }
    

    public static SqlKata.Query CountQuery(this LoadedEntity e,ValidFilter[] filters)
    {
        var query = e.GetCommonCountQuery(filters);
        query.ApplyJoin(filters.Select(x => x.Vector));
        return query;
    }

    internal static SqlKata.Query GetCommonCountQuery(this LoadedEntity e, IEnumerable<ValidFilter> filters)
    {
        var query = e.Basic();
        query.ApplyFilters(filters);
        return query;
    }


    public static SqlKata.Query ManyQuery(this LoadedEntity e,IEnumerable<ValidValue> ids, IEnumerable<LoadedAttribute> attrs)
    {
        return e.Basic()
            .Select(attrs.Select(x=>x.AddTableModifier()))
            .WhereIn(e.PrimaryKey, ids.GetValues());
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
         => item.TryGetValue(e.PrimaryKey, out var key)
            ? Result.Ok(new SqlKata.Query(e.TableName).Where(e.PrimaryKey, key).AsUpdate([DefaultFields.Deleted], [true]))
            : Result.Fail($"Failed to get value with primary key [${e.PrimaryKey}]");

    public static SqlKata.Query Basic(this LoadedEntity e) =>
        new SqlKata.Query(e.TableName)
            .Where(e.DeletedAttribute.AddTableModifier(), false);

    public static Entity WithDefaultAttr(this Entity e)
    {
        var list = new List<Attribute>();
        if (e.Attributes.FindOneAttr(DefaultFields.Id) is null)
        {
            list.Add(new Attribute
            ( 
                Field : DefaultFields.Id, Header : "id",
                IsDefault : true, InDetail:true, InList:true,
                DataType : DataType.Int, 
                DisplayType : DisplayType.Number
            ));
        }

        list.AddRange(e.Attributes);

        if (e.Attributes.FindOneAttr(DefaultFields.CreatedAt) is null)
        {
            list.Add(new Attribute
            (
                Field : DefaultFields.CreatedAt, Header : "Created At", 
                InList : true, InDetail : false, IsDefault : true,
                DataType : DataType.Datetime
            ));
        }

        if (e.Attributes.FindOneAttr(DefaultFields.UpdatedAt) is null)
        {
            list.Add(new Attribute
            (
                Field : DefaultFields.UpdatedAt, Header : "Updated At", 
                InList : true, InDetail : false, IsDefault : true,
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
        var interpreter = new Interpreter().Reference(typeof(Regex));
        var result = Result.Ok();
        foreach (var localAttribute in e.Attributes.Where(x=>x.IsLocal() && !string.IsNullOrWhiteSpace(x.Validation)))
        {
            if (!Validate(localAttribute).Try(out var err))
            {
                result.WithErrors(err);
            }
        }
        return result;
        
        Result Validate(LoadedAttribute attribute)
        {
            record.TryGetValue(attribute.Field, out var value);
            var typeOfAttribute = attribute.DataType switch
            {
                DataType.Int => typeof(int),
                DataType.Datetime => typeof(DateTime),
                _=> typeof(string)
            };

            try
            {
                var res = interpreter.Eval(attribute.Validation,
                    new Parameter(attribute.Field, typeOfAttribute, value));
                return res switch
                {
                    true => Result.Ok(),
                    "" => Result.Ok(),

                    false => Result.Fail($"Validation failed for {attribute.Header}"),
                    string errMsg => Result.Fail(errMsg),
                    _ => Result.Fail($"Validation failed for {attribute.Header}, expression should return string or bool result"),
                };
            }
            catch (Exception ex)
            {
                return Result.Fail($"validate fail for {attribute.Header}, Validate Rule is not correct, ex = {ex.Message}");
            }
        }
    }

    public static Result<Record> Parse (this LoadedEntity e, JsonElement jsonElement, IAttributeValueResolver resolver)
    {
        Dictionary<string, object> ret = new();
        foreach (var property in jsonElement.EnumerateObject())
        {
            var attribute = e.Attributes.FindOneAttr(property.Name);
            if (attribute == null) continue;
            
            var res = attribute.DataType switch
            {
                DataType.Lookup => SubElement(property.Value, attribute.Lookup!.PrimaryKey).Bind(x=>Convert(x, attribute)),
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
        
        Result<object> Convert(JsonElement? element, LoadedAttribute attribute)
        {
            if (element is null)
            {
                return Result.Ok<object>(null!);
            }
            return element.Value.ValueKind switch
            {
                JsonValueKind.String when resolver.ResolveVal(attribute, element.Value.GetString()!,out var caseVal) => caseVal.Value, 
                JsonValueKind.Number when element.Value.TryGetInt32(out var intValue) => intValue,
                JsonValueKind.Number when element.Value.TryGetInt64(out var longValue) => longValue,
                JsonValueKind.Number when element.Value.TryGetDouble(out var doubleValue) => doubleValue,
                JsonValueKind.Number => element.Value.GetDecimal(),
                JsonValueKind.True => Result.Ok<object>(true),
                JsonValueKind.False => Result.Ok<object>(false),
                JsonValueKind.Null => Result.Ok<object>(null!),
                JsonValueKind.Undefined => Result.Ok<object>(null!),
                _ => Result.Fail<object>($"Fail to convert [{attribute.Field}], input valueKind is [{element.Value.ValueKind}]")
            };
        }
    }
}