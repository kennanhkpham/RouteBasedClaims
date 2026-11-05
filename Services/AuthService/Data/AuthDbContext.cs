using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Account");
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Username).HasMaxLength(50).IsRequired();
            entity.Property(account => account.NormalizedUsername).HasMaxLength(50).IsRequired();
            entity.Property(account => account.PasswordHash).IsRequired();
            entity.Property(account => account.IsAdmin).IsRequired();
            entity.Property(account => account.Department).HasMaxLength(100).IsRequired();
            entity.Property(account => account.CreatedAt).IsRequired();
            entity.HasIndex(account => account.NormalizedUsername).IsUnique();
        });
    }
}
