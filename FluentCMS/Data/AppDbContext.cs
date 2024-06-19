using FluentCMS.Models;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.Data;

public class AppDbContext: DbContext
{
    public AppDbContext(){}
    public AppDbContext(DbContextOptions<AppDbContext> options):base(options){}
    public DbSet<Schema> Schemas { get; set; } = null!;
}
