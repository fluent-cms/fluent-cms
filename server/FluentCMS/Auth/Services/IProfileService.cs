using FluentCMS.Auth.DTO;

namespace FluentCMS.Auth.Services;

public interface IProfileService
{
    UserDto? GetInfo();
    Task ChangePassword(ProfileDto dto);
}