using System.Security.Claims;
namespace FormCMS.Utils.IdentityExt;

public static class HttpContextExt
{
    public static bool HasClaims(this HttpContext? context, string claimType, string value)
    {
        var userClaims = context?.User;
        if (userClaims?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return userClaims.Claims.FirstOrDefault(x => x.Value == value && x.Type == claimType) != null;
    }


    public static bool HasRole(this HttpContext? context,string role)
    {
        return context?.User.IsInRole(role) == true;
    }

    public static bool GetUserId(this HttpContext? context, out string userId)
    {
        userId = context?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        return !string.IsNullOrWhiteSpace(userId);
    }
}