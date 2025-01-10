using FormCMS.Auth.DTO;

namespace FormCMS.Auth.Services;

public interface IProfileService
{
    UserDto? GetInfo();
    Task ChangePassword(ProfileDto dto);
}