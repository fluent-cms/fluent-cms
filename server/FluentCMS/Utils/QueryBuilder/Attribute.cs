using System.Collections.Immutable;
using System.Globalization;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public record Attribute(
    string Field,
    string Header = "",
    string DataType = DataType.String,
    string DisplayType = DisplayType.Text,
    bool InList = true,
    bool InDetail = true,
    bool IsDefault = false,
    string Options = "",
    string Validation = ""
);

public record LoadedAttribute(
    string TableName,
    string Field,

    string Header = "",
    string DataType = DataType.String,
    string DisplayType = DisplayType.Text,

    bool InList = true,
    bool InDetail = true,
    bool IsDefault = false,

    string Options = "", 
    string Validation = "",
    
    Junction? Junction = null,
    Lookup? Lookup = null,
    Collection ? Collection = null
) : Attribute(
    Field: Field,
    Header: Header,
    DisplayType: DisplayType,
    DataType: DataType,
    InList: InList,
    InDetail: InDetail,
    IsDefault:IsDefault,
    Validation:Validation,
    Options: Options
);

public sealed record GraphAttribute(
    ImmutableArray<GraphAttribute> Selection,
    ImmutableArray<ValidSort> Sorts,
    ImmutableArray<ValidFilter> Filters,
    
    string Prefix,
    string TableName,
    string Field,

    string Header = "",
    string DataType = DataType.String,
    string DisplayType = DisplayType.Text,

    bool InList = true,
    bool InDetail = true,
    bool IsDefault = false,

    string Options = "", 
    string Validation = "",
    
    Lookup? Lookup = null,
    Junction? Junction = null,
    Collection? Collection = null,
    
    Pagination? Pagination = null
    
) : LoadedAttribute(
    TableName:TableName,
    Field:Field,

    Header :Header,
    DataType : DataType,
    DisplayType : DisplayType,

    InList : InList,
    InDetail : InDetail,
    IsDefault : IsDefault,

    Options :Options, 
    Validation : Validation,
    
    Junction:Junction,
    Lookup :Lookup,
    Collection:Collection
);

public record QueryArrayArgs(ValidFilter[] Filters, ValidSort[] Sorts,ValidPagination? Pagination,ValidSpan? Span);
public record EntityLinkDesc(
    LoadedAttribute SourceAttribute,
    LoadedEntity TargetEntity,
    LoadedAttribute TargetAttribute,
    bool IsArray,
    Func<GraphAttribute[] , ValidValue[] , QueryArrayArgs? , SqlKata.Query> GetQuery);

public static class AttributeHelper
{

    public static object GetValueOrLookup(this LoadedAttribute attribute, Record rec)
        => attribute.DataType switch
        {
            DataType.Lookup when rec[attribute.Field] is Record sub => sub
                [attribute.Lookup!.TargetEntity.PrimaryKey],
            _ => rec[attribute.Field]
        };

    public static Result<EntityLinkDesc> GetEntityLinkDesc(
        this LoadedAttribute attribute
    ) => attribute.DataType switch
    {
        DataType.Lookup when attribute.Lookup is { } lookup =>
            new EntityLinkDesc(
                SourceAttribute:attribute, 
                TargetEntity:lookup.TargetEntity,
                TargetAttribute:lookup.TargetEntity.PrimaryKeyAttribute, 
                IsArray:false, 
                GetQuery:(fields, ids, _) => lookup.TargetEntity.ByIdsQuery(fields,ids)
                ),
        DataType.Junction when attribute.Junction is { } junction =>
            new EntityLinkDesc(
                SourceAttribute: junction.SourceEntity.PrimaryKeyAttribute,
                TargetEntity: junction.TargetEntity,
                TargetAttribute:junction.SourceAttribute,
                IsArray:true, 
                GetQuery:(fields,ids, args) => junction.GetRelatedItems(args!.Filters,args.Sorts,args.Pagination,args.Span,fields,ids)),
        DataType.Collection when attribute.Collection is { } collection =>
            new EntityLinkDesc(
                SourceAttribute:collection.SourceEntity.PrimaryKeyAttribute,
                TargetEntity:collection.TargetEntity, 
                TargetAttribute:collection.LinkAttribute,
                IsArray:true, 
                GetQuery:(fields,ids,args) => collection.List(args!.Filters,args.Sorts,args.Pagination,args.Span, fields,ids)
                ),
        _ => Result.Fail($"Cannot get entity link desc for attribute [{attribute.Field}]")
    };

    public static bool TryResolveTarget(this Attribute attribute, out string entityName, out bool isCollection)
    {
        entityName = "";
        isCollection = attribute.DataType is DataType.Collection or DataType.Junction;
        return attribute.DataType switch
        {
            DataType.Lookup => attribute.GetLookupTarget(out entityName),
            DataType.Junction => attribute.GetJunctionTarget(out entityName),
            DataType.Collection => attribute.GetCollectionTarget(out entityName, out _),
            _=> false
        };
    }
    public static LoadedAttribute ToLoaded(this Attribute a, string tableName)
    {
        return new LoadedAttribute(
            TableName: tableName,
            Field: a.Field,
            Header: a.Header,
            DataType: a.DataType,
            DisplayType: a.DisplayType,
            InList: a.InList,
            InDetail: a.InDetail,
            IsDefault: a.IsDefault,
            Options: a.Options,
            Validation: a.Validation
        );
    }

    public static GraphAttribute ToGraph(this LoadedAttribute a)
    {
        return new GraphAttribute(
            Prefix: "",
            Selection: [],
            Filters: [],
            Sorts: [],
            Pagination:new Pagination(),
            Lookup: a.Lookup,
            Junction: a.Junction,
            Collection: a.Collection,
            TableName: a.TableName,
            Field: a.Field,
            Header: a.Header,
            DataType: a.DataType,
            DisplayType: a.DisplayType,
            InList: a.InList,
            InDetail: a.InDetail,
            IsDefault: a.IsDefault,
            Options: a.Options,
            Validation: a.Validation
        );
    }


    public static string AddTableModifier(this LoadedAttribute attribute, string tableAlias = "")
    {
        if (tableAlias == "")
        {
            tableAlias = attribute.TableName;
        }

        return $"{tableAlias}.{attribute.Field}";
    }

    public static string FullPathName(this Attribute a, string prefix)
    {
        return prefix == "" ? a.Field : prefix + "." + a.Field;
    }

    public static bool GetLookupTarget(this Attribute a, out string val)
    {
        val = a.Options;
        return !string.IsNullOrWhiteSpace(val);
    }

    public static bool GetCollectionTarget(this Attribute a, out string entityName, out string linkingAttribute)
    {
        (entityName, linkingAttribute) = ("", "");
        var parts = a.Options.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        (entityName, linkingAttribute) = (parts[0], parts[1]);
        return true;
    }
    

    public static bool GetDropdownOptions(this Attribute a, out string[] arr)
    {
        arr = a.Options.Split(',');
        return arr.Length > 0;
    }

    public static bool GetJunctionTarget(this Attribute a, out string val)
    {
       val = a.Options;
       return !string.IsNullOrWhiteSpace(val);
    }

    public static bool IsCompound(this Attribute a)
    {
        return a.DataType is DataType.Lookup or DataType.Junction or DataType.Collection;
    }

    public static bool IsLocal(this Attribute a)
    {
        return a.DataType  != DataType.Junction && a.DataType  != DataType.Collection;
    }

    public static Attribute ToAttribute(string name, string colType)
    {
        return new Attribute(
            Field: name,
            Header: SnakeToTitle(name),
            DataType: colType 
        );

        string SnakeToTitle(string snakeStr)
        {
            var components = snakeStr.Split('_');
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i].Length > 0)
                {
                    components[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(components[i]);
                }
            }
            return string.Join(" ", components);
        }
    }


    public static ValidValue[] GetUniq<T>(this T a, IEnumerable<Record> records)
        where T : Attribute 
    {
        var ret = new List<ValidValue>();
        foreach (var record in records)
        {
            if (record.TryGetValue(a.Field, out var value) && value != null)
            {
                ret.Add(value.ToValidValue());
            }
        }

        return ret.ToArray();
    }

    public static T? FindOneAttr<T>(this IEnumerable<T>? arr, string name)
        where T : Attribute
    {
        return arr?.FirstOrDefault(x => x.Field == name);
    }

    public static T[] GetLocalAttrs<T>(this IEnumerable<T>? arr)
        where T : Attribute
    {
        return arr?.Where(x=>x.IsLocal()).ToArray() ?? [];
    }

    public static T[] GetLocalAttrs<T>(this IEnumerable<T>? arr, string primaryKey, InListOrDetail listOrDetail)
        where T : Attribute
    {
        return arr?.Where(x =>
            x.Field == primaryKey
            || x.IsLocal() && (listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail)
        ).ToArray() ?? [];
    }

    public static T[] GetLocalAttrs<T>(this IEnumerable<T>? arr, string[] attributes)
        where T : Attribute
    {
        return arr?.Where(x => x.IsLocal() && attributes.Contains(x.Field)).ToArray() ?? [];
    }

    public static T[] GetAttrByType<T>(this IEnumerable<T>? arr, string dataType)
        where T : Attribute
    {
        return arr?.Where(x => x.DataType == dataType).ToArray() ?? [];
    }

    public static T[] GetAttrByType<T>(this IEnumerable<T>? arr, string dataType,
        InListOrDetail listOrDetail)
        where T : Attribute
    {
        return arr?.Where(x => x.DataType == dataType && (listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail))
            .ToArray() ?? [];
    }

    public static GraphAttribute? RecursiveFind(this IEnumerable<GraphAttribute> attributes, string name)
    {
        var parts = name.Split('.');
        var attrs = attributes;
        foreach (var part in parts[..^1])
        {
            var find = attrs.FindOneAttr(part);
            if (find == null)
            {
                return null;
            }

            attrs = find.Selection;
        }

        return attrs.FindOneAttr(parts.Last());
    }
}
