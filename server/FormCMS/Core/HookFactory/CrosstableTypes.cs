using FormCMS.Core.Descriptors;

namespace FormCMS.Core.HookFactory;

public record JunctionPreAddArgs(string Name, string RecordId, LoadedAttribute Attribute, Record[] RefItems):BaseArgs(Name) ;
public record JunctionPostAddArgs(string Name, string RecordId, LoadedAttribute Attribute, Record[] Items):BaseArgs (Name);
public record JunctionPreDelArgs(string Name, string RecordId, LoadedAttribute Attribute, Record[] RefItems):BaseArgs (Name);
public record JunctionPostDelArgs(string Name, string RecordId, LoadedAttribute Attribute, Record[] Items):BaseArgs (Name);
