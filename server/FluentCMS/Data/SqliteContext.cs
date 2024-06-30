using Microsoft.EntityFrameworkCore;

namespace FluentCMS.Data;

public class SqliteContext : AppDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //here
    }
    //for migration
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=cms.db");
}