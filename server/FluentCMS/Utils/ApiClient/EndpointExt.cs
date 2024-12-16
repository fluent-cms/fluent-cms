namespace FluentCMS.Utils.ApiClient;

internal static class EndpointExt
{
    public static string ToEntityApi(this string s) => $"/api/entities{s}";
    public static string ToSchemaApi(this string s) => $"/api/schemas{s}";
    public static string ToQueryApi(this string s) => $"/api/queries{s}";
    
}