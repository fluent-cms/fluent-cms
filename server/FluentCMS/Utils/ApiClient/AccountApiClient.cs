using FluentCMS.Utils.HttpClientExt;

namespace FluentCMS.Utils.ApiClient;

public class AccountApiClient(HttpClient client)
{
    public async Task EnsureLogin()
    {
        var loginData = new
        {
            email = "sadmin@cms.com",
            password = "Admin1!"
        };
        await client.PostObject("/api/register", loginData);
        (await client.PostAndSaveCookie("/api/login?useCookies=true", loginData)).EnsureSuccessStatusCode();
    } 
}