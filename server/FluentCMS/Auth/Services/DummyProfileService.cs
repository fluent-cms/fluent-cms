using FluentCMS.Services;

namespace FluentCMS.Auth.Services;

public class DummyProfileService: IProfileService
{
    public UserDto GetInfo()
    {
        return new UserDto
        {
            Email = "sadmin@cms.com",
            Roles = [Roles.Sa],
        };
    }
    
    public async Task ChangePassword(ProfileDto dto)
    {
        throw new InvalidParamException("Not implemented yet");
    } 
}