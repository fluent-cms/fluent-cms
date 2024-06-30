using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FluentCMS.Utils.Dao;
using FluentCMS.Utils.Naming;

namespace FluentCMS.Models.Queries;

public class Attribute
{

    [JsonConverter(typeof(JsonStringEnumConverter))]

    public DatabaseType DataType { get; set; }
    public string Field { get; set; } = "";
    public string Header { get; set; } = "";
    public bool InList { get; set; } = false;
    public bool InDetail { get; set; } = false;
    public bool IsDefault { get; set; } = false;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DisplayType Type { get; set; }

    public string[] Options { get; set; } = [];

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
        Header = Naming.SnakeToTitle(col.ColumnName);
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
        return Options.First();
    }
 
    public string GetLookupEntityName()
    {
        var ret = Options.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(ret);
        return ret;
    }
    
    public object CastToDatabaseType(string str)
    {
        return DataType switch
        {
            DatabaseType.Int => int.Parse(str),
            DatabaseType.Date => DateTime.Parse(str),
            _ => str,
        };
    }

    public object[] GetValues(Record[] records)
    {
        return records.Where(x=>x.ContainsKey(Field)).Select(x => x[Field]).Distinct().Where(x => x != null).ToArray();
    }
}