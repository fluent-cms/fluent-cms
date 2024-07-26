namespace FluentCMS.IntegrationTests;

public class CookieHolder
{
    private string[] _cookie =[];

    internal HttpResponseMessage GetCookie(HttpResponseMessage responseMessage)
    {
        _cookie = responseMessage.Headers.GetValues("Set-Cookie").ToArray();
        return responseMessage;
    }

    internal HttpRequestMessage SetCookie(HttpRequestMessage httpRequestMessage)
    {
        httpRequestMessage.Headers.Add("Cookie",_cookie);
        return httpRequestMessage;
    }
}