using FluentCMS.Utils.ApiClient;
using FluentResults;

namespace FluentCMS.Utils.Test;


public static class QuerySourceExtensions
{
    public static Task<Result<T>> GraphQlQuery<T>(this string query, QueryApiClient client)
        => client.SendGraphQuery<T>(query);
    
    public static Task<Result> GraphQlQuery(this string query, QueryApiClient client)
        => client.SendGraphQuery(query);
}