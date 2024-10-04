using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.Blog.Data;
public class MyUser : IdentityUser
{
   
}
public class AppDbContext: IdentityDbContext <MyUser>
{
    public AppDbContext(){}
    public AppDbContext(DbContextOptions<AppDbContext> options):base(options){}
}