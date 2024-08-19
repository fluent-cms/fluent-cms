using Microsoft.AspNetCore.Identity;

namespace FluentCMS.Utils.IdentityExt;

public static class IdentityResultExt
{
    public static string ErrorMessage(this IdentityResult result) =>
        string.Join("\r\n", result.Errors.Select(e => e.Description));
}