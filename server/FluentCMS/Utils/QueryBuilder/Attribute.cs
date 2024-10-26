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
    string Option = "",
    string Validation = "",
    string ValidationMessage = ""
); 

public sealed record LoadedAttribute(
    ImmutableArray<LoadedAttribute> Children ,
    string TableName,
    string Field,

    string Header = "",
    string DataType = DataType.String,
    string Type = DisplayType.Text,

    bool InList = true,
    bool InDetail = true,
    bool IsDefault = false,

    string Option = "", 
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
    Option: Option
);


public static class AttributeHelper
{
    public static string GetFullName(this LoadedAttribute attribute)
    {
        return $"{attribute.TableName}.{attribute.Field}";
    }

    public static LoadedAttribute ToLoaded(this Attribute a, string tableName)
    {
        return new LoadedAttribute(
            TableName: tableName,
            Field: a.Field,
            Children: [],
            Header: a.Header,
            DataType: a.DataType,
            Type: a.Type,
            InList: a.InList,
            InDetail: a.InDetail,
            IsDefault: a.IsDefault,
            Option: a.Option,
            Validation: a.Validation,
            ValidationMessage: a.ValidationMessage
        );
    }

    public static string GetLookupTarget(this Attribute a) => a.Option;
    public static string GetCrosstableTarget(this Attribute a) => a.Option;
    private static bool IsLocalAttribute(this Attribute a) => a.Type != DisplayType.Crosstable;

    

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
    where T :Attribute
    {
        return [..records.Where(x => x.ContainsKey(a.Field)).Select(x => x[a.Field]).Distinct().Where(x => x != null)];
    }

    public static T? FindOneAttribute<T>(this IEnumerable<T>?  arr, string name)
    where T :Attribute
    {
        return arr?.FirstOrDefault(x => x.Field == name);
    }
    public static ImmutableArray<T> GetLocalAttributes<T>(this IEnumerable<T>? arr)
    where T :Attribute
    {
        return arr?.Where(x => x.IsLocalAttribute()).ToImmutableArray()??[];
    }
    public static ImmutableArray<T> GetLocalAttributes<T>(this IEnumerable<T>? arr, InListOrDetail listOrDetail)
    where T : Attribute
    {
        return arr?.Where(x =>
                x.Type != DisplayType.Crosstable &&
                (listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail))
            .ToImmutableArray()??[];
    }

    public static ImmutableArray<T> GetLocalAttributes<T>(this IEnumerable<T>? arr, string[] attributes)
    where T : Attribute
    {
        return arr?.Where(x => x.Type != DisplayType.Crosstable && attributes.Contains(x.Field)).ToImmutableArray()??[];
    }

    public static ImmutableArray<T> GetAttributesByType<T>(this IEnumerable<T>? arr, string displayType)
    where T : Attribute
    {
        return arr?.Where(x => x.Type == displayType).ToImmutableArray()??[];
    }

    public static ImmutableArray<T> GetAttributesByType<T>(this IEnumerable<T>? arr, string type, InListOrDetail listOrDetail)
    where T : Attribute
    {
        return arr?.Where(x => x.Type == type && (listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail))
            .ToImmutableArray()??[];
    }
}