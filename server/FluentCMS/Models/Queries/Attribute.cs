using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FluentCMS.Utils.Dao;
using FluentCMS.Utils.Naming;

namespace FluentCMS.Models.Queries;
using Record = IDictionary<string,object>;

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
    
    public string[]?Options { get; set; }

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
        return Parent.TableName + "." + Field;
    }
    public string? GetCrossJoinEntityName()
    {
        return Options?.FirstOrDefault();
    }
 
    public string? GetLookupEntityName()
    {
        return Options?.FirstOrDefault();
    }
    
    public object CastToDatabaseType(string str)
    {
        switch (DataType)
        {
            case DatabaseType.Int :
               return int.Parse(str);
            case DatabaseType.Date:
            case DatabaseType.Datetime:
                return DateTime.Parse(str);
        }
        return str;
    }

    public object[] GetValues(Record[] records)
    {
        return records.Where(x=>x.ContainsKey(Field)).Select(x => x[Field]).Distinct().Where(x => x != null).ToArray();
    }
}