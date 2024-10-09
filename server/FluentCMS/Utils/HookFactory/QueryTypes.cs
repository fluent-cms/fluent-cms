using FluentCMS.Utils.QueryBuilder;
namespace FluentCMS.Utils.HookFactory;
public record QueryPreGetListArgs(string Name, string EntityName, Filters Filters, Sorts Sorts , Cursor Cursor , Pagination Pagination, Record[]? OutRecords = default):BaseArgs(Name) ;
public record QueryPreGetManyArgs(string Name,string EntityName,Filters Filters, Record[]? OutRecords = default):BaseArgs(Name) ;
public record QueryPreGetOneArgs(string Name,string EntityName, Filters Filters, Record? OutRecord = default):BaseArgs(Name) ;