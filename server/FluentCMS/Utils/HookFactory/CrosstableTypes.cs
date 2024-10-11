using FluentCMS.Utils.QueryBuilder;

namespace FluentCMS.Utils.HookFactory;

public record CrosstablePreAddArgs(string Name, string RecordId, LoadedAttribute Attribute, Record[] RefItems):BaseArgs(Name) ;
public record CrosstablePostAddArgs(string Name, string RecordId, LoadedAttribute Attribute, Record[] Items):BaseArgs (Name);
public record CrosstablePreDelArgs(string Name, string RecordId, LoadedAttribute Attribute, Record[] RefItems):BaseArgs (Name);
public record CrosstablePostDelArgs(string Name, string RecordId, LoadedAttribute Attribute, Record[] Items):BaseArgs (Name);
