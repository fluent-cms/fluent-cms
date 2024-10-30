using System.Collections.Immutable;
using FluentCMS.Utils.QueryBuilder;
namespace FluentCMS.Utils.HookFactory;

public record QueryPreGetListArgs(
    string Name,
    string EntityName,
    ImmutableArray<ValidFilter> Filters,
    ImmutableArray<ValidSort> Sorts,
    ValidCursor Cursor,
    ValidPagination Pagination,
    Record[]? OutRecords = default) : BaseArgs(Name);

public record QueryPreGetManyArgs(
    string Name,
    string EntityName,
    ImmutableArray<ValidFilter> Filters,
    ValidPagination Pagination,
    Record[]? OutRecords = default) : BaseArgs(Name);
public record QueryPreGetOneArgs(string Name,string EntityName, ImmutableArray<ValidFilter> Filters, Record? OutRecord = default):BaseArgs(Name) ;