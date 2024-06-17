using FluentCMSApi.models;
using Microsoft.EntityFrameworkCore;

namespace FluentCMSApi.Data;

public class PgContext : AppDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //here
    }
    //for migration
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseNpgsql("Host=localhost;Database=fluent-cms;Username=postgres;Password=mysecretpassword");
}