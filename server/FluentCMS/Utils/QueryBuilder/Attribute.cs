using System.Globalization;
using System.Text.Json.Serialization;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public class Attribute
{

    public string DataType { get; set; }

    private string _field = "";
    public string Field
    {
        get => _field;
        set => _field = value.Trim();
    }
    public string Header { get; set; } = "";
    public bool InList { get; set; } 
    public bool InDetail { get; set; }
    public bool IsDefault { get; set; } //for admin panel

    public string Type { get; set; }

    public string Options { get; set; } = "";
    public string Validation { get; set; } = "";
    public string ValidationMessage { get; set; } = "";

    [JsonIgnore]
    public bool IsLocalAttribute => Type != DisplayType.Crosstable;
    [JsonIgnore]
    public Entity? Parent { get; set; }
    // not input in json designer, 
    public Crosstable? Crosstable { get; set; } 
    public Entity? Lookup { get; set; }

    [JsonIgnore] public Attribute[]? Children { get; set; } 
    public Attribute() {}

    public Attribute(ColumnDefinition col)
    {
        Field = col.ColumnName;
        Header = SnakeToTitle(col.ColumnName);
        InList = true;
        InDetail = true;
        Type = DisplayType.Text;
        DataType = col.DataType;
        return;
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

    public string FullName()
    {
        ArgumentNullException.ThrowIfNull(Parent);
        return Parent.TableName + "." + Field;
    }
    public Result<string> GetCrossEntityName()
    {
        return string.IsNullOrWhiteSpace(Options) ? Result.Fail($"not find corsstable for {FullName()}") : Options;
    }
 
    public Result<string> GetLookupEntityName()
    {
        return string.IsNullOrWhiteSpace(Options) ? Result.Fail($"not find lookup for {FullName()}") : Options;
    }
    
    public object[] GetValues(Record[] records)
    {
        return records.Where(x=>x.ContainsKey(Field)).Select(x => x[Field]).Distinct().Where(x => x != null).ToArray();
    }
}

public static class AttributeNodeHelper
{
    public static Attribute? FindOneAttribute(this Attribute[]?  arr, string name)
    {
        return arr?.FirstOrDefault(x => x.Field == name);
    }
    public static Attribute[] GetLocalAttributes(this Attribute[]? arr)
    {
        return arr?.Where(x => x.IsLocalAttribute).ToArray()??[];
    }
    public static Attribute[] GetLocalAttributes(this Attribute[]? arr, InListOrDetail listOrDetail)
    {
        return arr?.Where(x =>
                x.Type != DisplayType.Crosstable &&
                (listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail))
            .ToArray()??[];
    }

    public static Attribute[] GetLocalAttributes(this Attribute[]? arr, string[] attributes)
    {
        return arr?.Where(x => x.Type != DisplayType.Crosstable && attributes.Contains(x.Field)).ToArray()??[];
    }

    public static Attribute[] GetAttributesByType(this Attribute[]? arr, string displayType)
    {
        return arr?.Where(x => x.Type == displayType).ToArray()??[];
    }

    public static Attribute[] GetAttributesByType(this Attribute[]? arr, string type, InListOrDetail listOrDetail)
    {
        return arr?.Where(x => x.Type == type && (listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail))
            .ToArray()??[];
    }
}