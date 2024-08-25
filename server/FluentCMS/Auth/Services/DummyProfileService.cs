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
            AllowedMenus = [UserDto.MenuSchemaBuilder]
        };
    }
    
    public Task ChangePassword(ProfileDto dto)
    {
        throw new InvalidParamException("Not implemented yet");
    } 
}