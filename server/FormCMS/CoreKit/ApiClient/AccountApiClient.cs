using FormCMS.Utils.HttpClientExt;
using FluentResults;
using FormCMS.Core.Descriptors;

namespace FormCMS.CoreKit.ApiClient;

public class AccountApiClient(HttpClient client)
{
    public async Task<Result> EnsureLogin()
    {
        var loginData = new
        {
            email = "sadmin@cms.com",
            password = "Admin1!"
        };
        await client.PostResult("/api/register", loginData,JsonOptions.IgnoreCase);
        return await client.PostAndSaveCookie("/api/login?useCookies=true", loginData,JsonOptions.IgnoreCase);
    } 
}