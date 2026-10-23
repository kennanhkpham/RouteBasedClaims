using InsuranceClaimApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<PrescriptionDetails> PrescriptionDetails => Set<PrescriptionDetails>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Claim>().ToTable("Claim");
        modelBuilder.Entity<Client>().ToTable("Client");
        modelBuilder.Entity<PrescriptionDetails>()
            .ToTable("PrescriptionDetails");

        modelBuilder.Entity<Claim>()
            .HasOne(c => c.PrescriptionDetail)
            .WithOne(p => p.Claim)
            .HasForeignKey<PrescriptionDetails>(p => p.PrescriptionClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}