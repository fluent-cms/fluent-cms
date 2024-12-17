using FluentCMS.Utils.HttpClientExt;
using FluentResults;

namespace FluentCMS.Utils.ApiClient;

public class AccountApiClient(HttpClient client)
{
    public async Task<Result> EnsureLogin()
    {
        var loginData = new
        {
            email = "sadmin@cms.com",
            password = "Admin1!"
        };
        await client.PostResult("/api/register", loginData);
        return await client.PostAndSaveCookie("/api/login?useCookies=true", loginData);
    } 
}