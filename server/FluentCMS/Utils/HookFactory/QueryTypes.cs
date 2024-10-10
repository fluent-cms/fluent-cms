using FluentCMS.Utils.QueryBuilder;
namespace FluentCMS.Utils.HookFactory;
public record QueryPreGetListArgs(string Name, string EntityName, ValidFilter[] Filters, Sort[] Sorts , Cursor Cursor , Pagination Pagination, Record[]? OutRecords = default):BaseArgs(Name) ;
public record QueryPreGetManyArgs(string Name,string EntityName,ValidFilter[] Filters, Record[]? OutRecords = default):BaseArgs(Name) ;
public record QueryPreGetOneArgs(string Name,string EntityName, ValidFilter[] Filters, Record? OutRecord = default):BaseArgs(Name) ;