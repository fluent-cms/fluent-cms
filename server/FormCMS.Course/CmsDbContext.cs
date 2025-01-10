using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FormCMS.Course;


internal class CmsDbContext : IdentityDbContext<IdentityUser>
{
    public CmsDbContext(){}
    public CmsDbContext(DbContextOptions<CmsDbContext> options):base(options){}
}