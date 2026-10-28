using InsuranceClaimApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<PrescriptionDetails> PrescriptionDetails => Set<PrescriptionDetails>();
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Claim>().ToTable("Claim");
        modelBuilder.Entity<Client>().ToTable("Client");
        modelBuilder.Entity<PrescriptionDetails>()
            .ToTable("PrescriptionDetails");
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Account");
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Username).HasMaxLength(50).IsRequired();
            entity.Property(account => account.NormalizedUsername).HasMaxLength(50).IsRequired();
            entity.Property(account => account.PasswordHash).IsRequired();
            entity.Property(account => account.IsAdmin).IsRequired();
            entity.Property(account => account.CreatedAt).IsRequired();
            entity.HasIndex(account => account.NormalizedUsername).IsUnique();
        });

        modelBuilder.Entity<Claim>()
            .HasOne(c => c.PrescriptionDetail)
            .WithOne(p => p.Claim)
            .HasForeignKey<PrescriptionDetails>(p => p.PrescriptionClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}