using System.Collections.Immutable;
using System.Globalization;
using FluentCMS.Utils.DataDefinitionExecutor;

namespace FluentCMS.Utils.QueryBuilder;

public abstract record BaseAttribute(string Field, string Type, bool InList, bool InDetail, string Option);

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
) : BaseAttribute(
    Field: Field,
    Type: Type,
    InList: InList,
    InDetail: InDetail,
    Option: Options
);

public record ValidAttribute(
    string Fullname,
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

) : BaseAttribute(
    Field: Field,
    Type:Type,
    InList:InList,
    InDetail:InDetail,
    Option: Options
);

public record LoadedAttribute(
    string Fullname,
    string Field,
    Crosstable? Crosstable = default,
    LoadedEntity? Lookup = default,
    LoadedAttribute[]? Children =default,
    
    string Header = "",
    string DataType = DataType.String,
    string Type = DisplayType.Text,
    
    bool InList = true,
    bool InDetail = true,
    bool IsDefault = false,
    
    string Options = "", //frontend need this ,can not delete
    string Validation = "",
    string ValidationMessage = ""
) : BaseAttribute(
    Field: Field,
    Type:Type,
    InList:InList,
    InDetail:InDetail,
    Option: Options

);

public static class AttributeHelper
{
    public static string GetLookupTarget(this BaseAttribute a) => a.Option;
    public static string GetCrosstableTarget(this BaseAttribute a) => a.Option;
    private static bool IsLocalAttribute(this BaseAttribute a) => a.Type != DisplayType.Crosstable;

    public static LoadedAttribute ToLoaded(this ValidAttribute a, LoadedEntity? lookup = default, Crosstable? crosstable = default, LoadedAttribute[]? children  =default)
    {
        return new LoadedAttribute(
            Fullname: a.Fullname,
            Field: a.Field,
            Crosstable: crosstable,
            Lookup: lookup,
            Children: children,
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

    public static ValidAttribute ToValid(this Attribute a, string tableName)
    {
        var fullname = $"{tableName}.{a.Field}";

        // Create and return a ParsedAttr object
        return new ValidAttribute(
            Fullname: fullname,
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

    public static Attribute ToAttribute(this ColumnDefinition col)
    {
        return new Attribute(
            Field:col.ColumnName,
            Header : SnakeToTitle(col.ColumnName),
            DataType : col.DataType
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
   
    public static ImmutableArray<object> GetUniqValues<T>(this T a, Record[] records)
    where T :BaseAttribute
    {
        return [..records.Where(x => x.ContainsKey(a.Field)).Select(x => x[a.Field]).Distinct().Where(x => x != null)];
    }

    public static T? FindOneAttribute<T>(this IEnumerable<T>?  arr, string name)
    where T :BaseAttribute
    {
        return arr?.FirstOrDefault(x => x.Field == name);
    }
    public static ImmutableArray<T> GetLocalAttributes<T>(this IEnumerable<T>? arr)
    where T :BaseAttribute
    {
        return arr?.Where(x => x.IsLocalAttribute()).ToImmutableArray()??[];
    }
    public static ImmutableArray<T> GetLocalAttributes<T>(this IEnumerable<T>? arr, InListOrDetail listOrDetail)
    where T : BaseAttribute
    {
        return arr?.Where(x =>
                x.Type != DisplayType.Crosstable &&
                (listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail))
            .ToImmutableArray()??[];
    }

    public static ImmutableArray<T> GetLocalAttributes<T>(this IEnumerable<T>? arr, string[] attributes)
    where T : BaseAttribute
    {
        return arr?.Where(x => x.Type != DisplayType.Crosstable && attributes.Contains(x.Field)).ToImmutableArray()??[];
    }

    public static ImmutableArray<T> GetAttributesByType<T>(this IEnumerable<T>? arr, string displayType)
    where T : BaseAttribute
    {
        return arr?.Where(x => x.Type == displayType).ToImmutableArray()??[];
    }

    public static ImmutableArray<T> GetAttributesByType<T>(this IEnumerable<T>? arr, string type, InListOrDetail listOrDetail)
    where T : BaseAttribute
    {
        return arr?.Where(x => x.Type == type && (listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail))
            .ToImmutableArray()??[];
    }
    
    
}