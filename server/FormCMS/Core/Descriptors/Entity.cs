using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicExpresso;

using FormCMS.CoreKit.RelationDbQuery;
using FormCMS.Utils.ResultExt;
using FluentResults;
using FormCMS.Utils.EnumExt;
using Npgsql.Internal.Postgres;

namespace FormCMS.Core.Descriptors;

public enum PublicationStatus
{
    Draft,
    Published,
    Unpublished,
    Scheduled
}

public record Entity(
    ImmutableArray<Attribute> Attributes,
    string Name = "",
    string DisplayName ="",
    string TableName = "",
    
    string PrimaryKey = "",
    string LabelAttributeName ="",
    
    int DefaultPageSize = EntityConstants.DefaultPageSize,
    PublicationStatus DefaultPublicationStatus = PublicationStatus.Published
);

public record LoadedEntity(
    ImmutableArray<LoadedAttribute> Attributes,
    LoadedAttribute PrimaryKeyAttribute,
    LoadedAttribute LabelAttribute,
    LoadedAttribute DeletedAttribute,
    LoadedAttribute PublicationStatusAttribute,
    string Name,
    string DisplayName , //needed by admin panel
    string TableName,
    
    string PrimaryKey,
    string LabelAttributeName, 
    int DefaultPageSize,
    PublicationStatus DefaultPublicationStatus 
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
        var labelAttribute = entity.Attributes.FirstOrDefault(x=>x.Field == entity.LabelAttributeName)?.ToLoaded(entity.TableName) ?? primaryKey;
        var attributes = entity.Attributes.Select(x => x.ToLoaded(entity.TableName));
        var deletedAttribute = new LoadedAttribute(entity.TableName, DefaultAttributeNames.Deleted.ToCamelCase());
        var publicationStatusAttribute = new LoadedAttribute(entity.TableName, DefaultAttributeNames.PublicationStatus.ToCamelCase());
        return new LoadedEntity(
            [..attributes],
            PrimaryKeyAttribute:primaryKey,
            LabelAttribute: labelAttribute,
            DeletedAttribute:deletedAttribute,
            Name:entity.Name,
            TableName: entity.TableName,
            PrimaryKey:entity.PrimaryKey,
            DisplayName:entity.DisplayName,
            LabelAttributeName:entity.LabelAttributeName,
            DefaultPageSize:entity.DefaultPageSize,
            DefaultPublicationStatus:entity.DefaultPublicationStatus,
            PublicationStatusAttribute:publicationStatusAttribute
        );
    }
    
    public static Result<SqlKata.Query> OneQuery(this LoadedEntity e,ValidFilter[] filters, ValidSort[] sorts,IEnumerable<LoadedAttribute> attributes,bool onlyPublished)
    {
        var query = e.Basic().Select(attributes.Select(x => x.AddTableModifier()));
        if (onlyPublished)
        {
            query.Where(e.PublicationStatusAttribute.AddTableModifier(), PublicationStatus.Published.ToCamelCase());   
        }
        query.ApplyJoin([..filters.Select(x=>x.Vector),..sorts.Select(x=>x.Vector)],onlyPublished);
        var result = query.ApplyFilters(filters); 
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }
        query.ApplySorts(sorts);
        return query;
    }

    public static SqlKata.Query ByIdsQuery(this LoadedEntity e,IEnumerable<string> fields,IEnumerable<ValidValue> ids,bool onlyPublished)
    {
        var query = e.Basic().WhereIn(e.PrimaryKey, ids.GetValues())
            .Select(fields);
        if (onlyPublished)
        {
            query.Where(e.PublicationStatusAttribute.AddTableModifier(), PublicationStatus.Published.ToCamelCase());
        }
        return query;
    }

    public static SqlKata.Query AllQuery(this LoadedEntity e, IEnumerable<LoadedAttribute> attributes)
        => e.Basic().Select(attributes.Select(x => x.Field));
    public static SqlKata.Query ListQuery(this LoadedEntity e,ValidFilter[] filters, ValidSort[] sorts, 
        ValidPagination? pagination, ValidSpan? cursor, IEnumerable<LoadedAttribute> attributes,bool onlyPublished)
    {
        var query = e.GetCommonListQuery(filters,sorts,pagination,cursor,attributes,onlyPublished);
        query.ApplyJoin([..filters.Select(x=>x.Vector),..sorts.Select(x=>x.Vector)],onlyPublished);
        return query;
    }
    
    internal static SqlKata.Query GetCommonListQuery(this LoadedEntity e, 
        IEnumerable<ValidFilter> filters, 
        ValidSort[] sorts, 
        ValidPagination? pagination, 
        ValidSpan? span, 
        IEnumerable<LoadedAttribute> attributes,
        bool onlyPublished)
    {
        var q = e.Basic().Select(attributes.Select(x => x.AddTableModifier()));
        if (onlyPublished)
        {
            q.Where(e.PublicationStatusAttribute.AddTableModifier(), PublicationStatus.Published.ToCamelCase());
        }
        
        q.ApplyFilters(filters);
        q.ApplySorts(SpanHelper.IsForward(span?.Span)?sorts: sorts.ReverseOrder());
        q.ApplyCursor(span,sorts);
        if (pagination is not null)
        {
            q.ApplyPagination(pagination);
        }
        return q;
    }
    

    public static SqlKata.Query CountQuery(this LoadedEntity e,ValidFilter[] filters,bool onlyPublished)
    {
        var query = e.GetCommonCountQuery(filters);
        //filter might contain lookup's target's attribute, 
        query.ApplyJoin(filters.Select(x => x.Vector),onlyPublished);
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

    public static Result<SqlKata.Query> SavePublicationStatus(this LoadedEntity e, object id, Record record)
    {
        if (record.TryGetValue(DefaultAttributeNames.PublicationStatus.ToCamelCase(), out var statusObject) 
            && statusObject is string statusString 
            && Enum.TryParse(statusString,true, out PublicationStatus status)
            
            && record.TryGetValue(DefaultAttributeNames.PublishedAt.ToCamelCase() , out var publishedAtObject)
            && publishedAtObject is string publishedAtString
            && DateTime.TryParse(publishedAtString, out var publishedAt))
        {
            
            return new SqlKata.Query(e.TableName)
             .Where(e.PrimaryKey, id)
             .AsUpdate(
                 [
                     DefaultAttributeNames.PublicationStatus.ToCamelCase(),
                     DefaultAttributeNames.PublishedAt.ToCamelCase()
                 ],
                 [status.ToCamelCase(), publishedAt]);
        }
        return Result.Fail("Can not save publication status, invalid input");
   }
    
    public static SqlKata.Query PublishAll(this Entity e)
    => new SqlKata.Query(e.TableName)
        .Where(DefaultAttributeNames.PublicationStatus.ToCamelCase(),PublicationStatus.Scheduled.ToCamelCase())
        .WhereDate(DefaultAttributeNames.PublishedAt.ToCamelCase(), "<", DateTime.Now)
        .AsUpdate([DefaultAttributeNames.PublicationStatus.ToCamelCase()], [PublicationStatus.Published.ToCamelCase()]) ;
    

    public static SqlKata.Query Unpublish(this LoadedEntity e, object id)
       => new SqlKata.Query(e.TableName)
            .Where(e.PrimaryKey, id)
            .AsUpdate([DefaultAttributeNames.PublicationStatus.ToCamelCase()], [PublicationStatus.Unpublished.ToCamelCase()]);

    public static SqlKata.Query Publish(this LoadedEntity e, object id, Record item)
    {
        var updateItem = new Dictionary<string, object>
        {
            [DefaultAttributeNames.PublicationStatus.ToCamelCase()] = PublicationStatus.Published.ToCamelCase()
        };

        if (item.TryGetValue(DefaultAttributeNames.PublicationStatus.ToCamelCase(), out var val) 
            && val is string s 
            && s == PublicationStatus.Unpublished.ToCamelCase())
        {
            //for unpublished item, no need to set published at
        }
        else
        {
            updateItem[DefaultAttributeNames.PublishedAt.ToCamelCase()] = DateTime.Now;
        }

        return new SqlKata.Query(e.TableName).Where(e.PrimaryKey, id).AsUpdate(updateItem.Keys, updateItem.Values);
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
            ? Result.Ok(new SqlKata.Query(e.TableName).Where(e.PrimaryKey, key)
                .AsUpdate([DefaultAttributeNames.Deleted.ToCamelCase()], [true]))
            : Result.Fail($"Failed to get value with primary key [${e.PrimaryKey}]");

    public static SqlKata.Query Basic(this LoadedEntity e)
    {
        var query = new SqlKata.Query(e.TableName)
            .Where(e.DeletedAttribute.AddTableModifier(), false);
        return query;
    }


    public static Result ValidateTitleAttributes(this LoadedEntity e, Record record)
    {
        if (record.TryGetValue(e.LabelAttributeName, out var value) && value is not null)
        {
            return Result.Ok();
        }
        return Result.Fail($"Validation fail for {e.LabelAttributeName}");
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