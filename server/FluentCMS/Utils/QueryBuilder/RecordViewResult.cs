namespace FluentCMS.Utils.QueryBuilder;

public sealed record QueryResult<T>(T[]? Items, string First, string Last);