using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;
namespace FluentCMS.Utils.HookFactory;
public record EntityPreGetOneArgs(string Name, string RecordId, Record? OutRecord):BaseArgs (Name);
public record EntityPostGetOneArgs(string Name, string RecordId, Record Record):BaseArgs (Name);
public record EntityPreGetListArgs(string Name, LoadedEntity Entity,ImmutableArray<ValidFilter> RefFilters, ImmutableArray<ValidSort> RefSorts, ValidPagination RefPagination):BaseArgs(Name) ;
public record EntityPostGetListArgs(string Name, ListResult RefListResult):BaseArgs(Name) ;
public record EntityPreUpdateArgs(string Name, string RecordId, Record RefRecord):BaseArgs (Name);
public record EntityPostUpdateArgs(string Name, string RecordId, Record Record):BaseArgs (Name);
public record EntityPreAddArgs(string Name, Record RefRecord):BaseArgs (Name);
public record EntityPostAddArgs(string Name, string RecordId, Record Record):BaseArgs(Name) ;
public record EntityPreDelArgs(string Name, string RecordId, Record RefRecord):BaseArgs (Name);
public record EntityPostDelArgs(string Name, string RecordId, Record Record):BaseArgs (Name);