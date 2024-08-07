using System.Globalization;
using System.Text.Json.Serialization;
using FluentCMS.Utils.DataDefinitionExecutor;
using FluentResults;

namespace FluentCMS.Utils.QueryBuilder;

public class Attribute
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DataType DataType { get; set; }

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

    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DisplayType Type { get; set; }

    public string Options { get; set; } = "";

    [JsonIgnore]
    public Entity? Parent { get; set; }
    // not input in json designer, 
    public Crosstable? Crosstable { get; set; } 
    public Entity? Lookup { get; set; }
    public Attribute()
    {
    }

    public Attribute(ColumnDefinition col)
    {
        Field = col.ColumnName;
        Header = SnakeToTitle(col.ColumnName);
        InList = true;
        InDetail = true;
        Type = DisplayType.text;
        DataType = col.DataType;
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
    private static string SnakeToTitle(string snakeStr)
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