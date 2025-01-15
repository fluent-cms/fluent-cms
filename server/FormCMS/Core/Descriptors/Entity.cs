using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DynamicExpresso;

using FormCMS.CoreKit.RelationDbQuery;
using FormCMS.Utils.ResultExt;
using FluentResults;

namespace FormCMS.Core.Descriptors;
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
        var primaryKey = entity.Attributes.FirstOrDefault(x=>x.Field ==entity.PrimaryKey)!.ToLoaded(entity.TableName);
        var titleAttribute = entity.Attributes.FirstOrDefault(x=>x.Field == entity.TitleAttribute)?.ToLoaded(entity.TableName) ?? primaryKey;
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

    public static SqlKata.Query ByIdsQuery(this LoadedEntity e,IEnumerable<LoadedAttribute> attributes,IEnumerable<ValidValue> ids)
    {
        var query = e.Basic().WhereIn(e.PrimaryKey, ids.GetValues())
            .Select(attributes.Select(x => x.AddTableModifier()));
        return query;
    }

    public static SqlKata.Query AllQuery(this LoadedEntity e, IEnumerable<LoadedAttribute> attributes)
        => e.Basic().Select(attributes.Select(x => x.Field));
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
        ValidSpan? span, 
        IEnumerable<LoadedAttribute> attributes)
    {
        var q = e.Basic().Select(attributes.Select(x => x.AddTableModifier()));
        q.ApplyFilters(filters);
        q.ApplySorts(SpanHelper.IsForward(span?.Span)?sorts: sorts.ReverseOrder());
        q.ApplyCursor(span,sorts);
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
        if (e.Attributes.FirstOrDefault(x=>x.Field ==DefaultFields.Id) is null)
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

        if (e.Attributes.FirstOrDefault(x=>x.Field == DefaultFields.CreatedAt) is null)
        {
            list.Add(new Attribute
            (
                Field : DefaultFields.CreatedAt, Header : "Created At", 
                InList : true, InDetail : false, IsDefault : true,
                DataType : DataType.Datetime, DisplayType:DisplayType.Datetime
            ));
        }

        if (e.Attributes.FirstOrDefault(x=>x.Field ==DefaultFields.UpdatedAt) is null)
        {
            list.Add(new Attribute
            (
                Field : DefaultFields.UpdatedAt, Header : "Updated At", 
                InList : true, InDetail : false, IsDefault : true,
                DataType : DataType.Datetime, DisplayType:DisplayType.Datetime
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

    public static Result<Record> Parse (this LoadedEntity entity, JsonElement element, IAttributeValueResolver resolver)
    {
        Dictionary<string, object> ret = new();
        foreach (var attribute in entity.Attributes.Where(x=>x.IsLocal()))
        {
            
            if (!element.TryGetProperty(attribute.Field, out var value)) continue;
            var res = attribute.ParseJsonElement(value, resolver);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
            ret[attribute.Field] = res.Value; 
           
        }
        return ret;
        
       
    }
}