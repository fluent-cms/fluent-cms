namespace FluentCMS.Utils.HookFactory;
using Attribute = FluentCMS.Utils.QueryBuilder.Attribute;

public record CrosstablePreAddArgs(string Name, string RecordId, Attribute Attribute, Record[] RefItems):BaseArgs(Name) ;
public record CrosstablePostAddArgs(string Name, string RecordId, Attribute Attribute, Record[] Items):BaseArgs (Name);
public record CrosstablePreDelArgs(string Name, string RecordId, Attribute Attribute, Record[] RefItems):BaseArgs (Name);
public record CrosstablePostDelArgs(string Name, string RecordId, Attribute Attribute, Record[] Items):BaseArgs (Name);
