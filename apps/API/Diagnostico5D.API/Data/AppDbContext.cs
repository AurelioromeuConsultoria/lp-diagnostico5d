using Diagnostico5D.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Diagnostico5D.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Whatsapp).HasMaxLength(20);
            entity.Property(e => e.Status).HasDefaultValue("parcial");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now','localtime')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now','localtime')");
        });
    }
}
