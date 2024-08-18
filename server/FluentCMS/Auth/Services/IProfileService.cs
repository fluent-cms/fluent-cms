namespace FluentCMS.Auth.Services;

public interface IProfileService
{
    Task ChangePassword(ProfileDto dto);
}