using FluentCMS.Cms.Models;
using FluentCMS.Utils.QueryBuilder;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

namespace FluentCMS.Utils.HookFactory;

//data exists in hook trigger and allow hook function to change
public sealed class HookParameter
{
    public Filters? Filters { get; init; } //before query entity or before view
    public Sorts? Sorts { get; init; } //before query entity or before view
    public Pagination? Pagination { get; init; } //before query entity
    public ListResult? ListResult { get; init; } // after query
    
    public Cursor? Cursor { get; init; } //before view
    public Record? Record { get; init; } //for create, update, delete entity
    public Attribute? Attribute { get; init; } //for cross table
    public IList<Record>? Records { get; init; } //for cross table
    public Schema? Schema { get; init; } //save, delete schema
}

//data not exist in hook trigger but returns by hook functions 
//not put them in HookParameter because it's easy and more efficient for hook function  hookReturn.Records = ***
public class HookReturn
{
    public Record Record { get; set; } = new Dictionary<string, object>();
    public Record[] Records { get; set; } = [];
}

//metas are immutable, changes in hook function are ignored
public record EntityMeta(string EntityName, string RecordId ="");

public record SchemaMeta(string SchemaType, int SchemaId)
{
    public const string TypeAll = "";
    public const int NoId = 0;
}

public record QueryMeta(string QueryName, string EntityName);