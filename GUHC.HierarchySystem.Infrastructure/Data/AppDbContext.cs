using GUHC.HierarchySystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace GUHC.HierarchySystem.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.HasOne(e => e.ParentAccount)
                .WithMany(e => e.ChildAccounts)
                .HasForeignKey(e => e.ParentAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ParentAccountId);
        });
    }
}
