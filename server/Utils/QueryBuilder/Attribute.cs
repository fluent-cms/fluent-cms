using System.Globalization;
using System.Text.Json.Serialization;
using Utils.DataDefinitionExecutor;

namespace Utils.QueryBuilder;

public class Attribute
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DataType DataType { get; set; }

    public string Field { get; set; } = "";
    
    public string Header { get; set; } = "";
    public bool InList { get; set; } = false;
    public bool InDetail { get; set; } = false;
    public bool IsDefault { get; set; } = false; //frontend not show readonly attributes

    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DisplayType Type { get; set; }

    public string Options { get; set; } ="";

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
    public string? GetCrossJoinEntityName()
    {
        return Options;
    }
 
    public string GetLookupEntityName()
    {
        return Options;
    }
    
    public object CastToDatabaseType(string str)
    {
        return DataType switch
        {
            DataType.Int => int.Parse(str),
            DataType.Datetime => DateTime.Parse(str),
            _ => str,
        };
    }

    public object[] GetValues(Record[] records)
    {
        return records.Where(x=>x.ContainsKey(Field)).Select(x => x[Field]).Distinct().Where(x => x != null).ToArray();
    }
    private  string SnakeToTitle(string snakeStr)
    {
        // Split the snake_case string by underscores
        string[] components = snakeStr.Split('_');
        // Capitalize the first letter of each component and join them with spaces
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].Length > 0)
            {
                components[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(components[i]);
            }
        }
        return string.Join(" ", components);
    }
}