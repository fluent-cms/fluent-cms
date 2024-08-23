using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebApiExamples;
public class AppDbContext: IdentityDbContext <IdentityUser>
{
    public AppDbContext(){}
    public AppDbContext(DbContextOptions<AppDbContext> options):base(options){}
}