using System.Collections.Immutable;
using System.Globalization;
using FluentCMS.Utils.DataDefinitionExecutor;

namespace FluentCMS.Utils.QueryBuilder;

public record Attribute(
    string Field,
    string Header = "",
    string DataType = DataType.String,
    string Type = DisplayType.Text,
    bool InList = true,
    bool InDetail = true,
    bool IsDefault = false,
    string Options = "",
    string Validation = "",
    string ValidationMessage = ""
);

public record LoadedAttribute(
    string TableName,
    string Field,

    string Header = "",
    string DataType = DataType.String,
    string Type = DisplayType.Text,

    bool InList = true,
    bool InDetail = true,
    bool IsDefault = false,

    string Options = "", 
    string Validation = "",
    string ValidationMessage = "",
    
    Crosstable? Crosstable = default,
    LoadedEntity? Lookup = default
) : Attribute(
    Field: Field,
    Header: Header,
    Type: Type,
    DataType: DataType,
    InList: InList,
    InDetail: InDetail,
    IsDefault:IsDefault,
    Validation:Validation,
    ValidationMessage:ValidationMessage,
    Options: Options
);
public record GraphAttribute(
    ImmutableArray<GraphAttribute> Selection,
    ImmutableArray<ValidSort> Sorts,
    
    // filter need to resolve at the runtime
    ImmutableArray<Filter> Filters,
    
    string Prefix,
    string TableName,
    string Field,

    string Header = "",
    string DataType = DataType.String,
    string Type = DisplayType.Text,

    bool InList = true,
    bool InDetail = true,
    bool IsDefault = false,

    string Options = "", 
    string Validation = "",
    string ValidationMessage = "",
    
    LoadedEntity? Lookup = default,
        
    Crosstable? Crosstable = default,
    Pagination? Pagination = default
    
) : LoadedAttribute(
    TableName:TableName,
    Field:Field,

    Header :Header,
    DataType : DataType,
    Type : Type,

    InList : InList,
    InDetail : InDetail,
    IsDefault : IsDefault,

    Options :Options, 
    Validation : Validation,
    ValidationMessage : ValidationMessage,
    
    Crosstable:Crosstable,
    Lookup :Lookup
);

public static class AttributeHelper
{

    public static LoadedAttribute ToLoaded(this Attribute a, string tableName)
    {
        return new LoadedAttribute(
            TableName: tableName,
            Field: a.Field,
            Header: a.Header,
            DataType: a.DataType,
            Type: a.Type,
            InList: a.InList,
            InDetail: a.InDetail,
            IsDefault: a.IsDefault,
            Options: a.Options,
            Validation: a.Validation,
            ValidationMessage: a.ValidationMessage
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
            Crosstable: a.Crosstable,
            TableName: a.TableName,
            Field: a.Field,
            Header: a.Header,
            DataType: a.DataType,
            Type: a.Type,
            InList: a.InList,
            InDetail: a.InDetail,
            IsDefault: a.IsDefault,
            Options: a.Options,
            Validation: a.Validation,
            ValidationMessage: a.ValidationMessage
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
    public static bool GetSelectItems(this Attribute a, out string[] arr)
    {
        arr = a.Options.Split(',');
        return arr.Length > 0;
    }

    public static bool GetCrosstableTarget(this Attribute a, out string val)
    {
       val = a.Options;
       return !string.IsNullOrWhiteSpace(val);
    }

    public static bool IsCompound(this Attribute a)
    {
        return a.Type is DisplayType.Lookup or DisplayType.Crosstable;
    }
    
    public static Attribute ToAttribute(this ColumnDefinition col)
    {
        return new Attribute(
            Field: col.ColumnName,
            Header: SnakeToTitle(col.ColumnName),
            DataType: col.DataType
        );

        string SnakeToTitle(string snakeStr)
        {
            // Split the snake_case string by underscores
            var components = snakeStr.Split('_');
            // Capitalize the first letter of each component and join them with spaces
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


    public static ImmutableArray<object> GetUniq<T>(this T a, Record[] records)
        where T : Attribute
    {
        return [..records.Where(x => x.ContainsKey(a.Field)).Select(x => x[a.Field]).Distinct().Where(x => x != null)];
    }

    public static T? FindOneAttr<T>(this IEnumerable<T>? arr, string name)
        where T : Attribute
    {
        return arr?.FirstOrDefault(x => x.Field == name);
    }

    public static T[] GetLocalAttrs<T>(this IEnumerable<T>? arr)
        where T : Attribute
    {
        return arr?.Where(x => x.Type != DisplayType.Crosstable).ToArray() ?? [];
    }

    public static T[] GetLocalAttrs<T>(this IEnumerable<T>? arr, InListOrDetail listOrDetail)
        where T : Attribute
    {
        return arr?.Where(x =>
                x.Type != DisplayType.Crosstable &&
                (listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail))
            .ToArray() ?? [];
    }

    public static T[] GetLocalAttrs<T>(this IEnumerable<T>? arr, string[] attributes)
        where T : Attribute
    {
        return arr?.Where(x => x.Type != DisplayType.Crosstable && attributes.Contains(x.Field)).ToArray() ??
               [];
    }

    public static T[] GetAttrByType<T>(this IEnumerable<T>? arr, string displayType)
        where T : Attribute
    {
        return arr?.Where(x => x.Type == displayType).ToArray() ?? [];
    }

    public static T[] GetAttrByType<T>(this IEnumerable<T>? arr, string type,
        InListOrDetail listOrDetail)
        where T : Attribute
    {
        return arr?.Where(x => x.Type == type && (listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail))
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
