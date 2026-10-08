using InsuranceClaimApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<PrescriptionDetails> PrescriptionDetails => Set<PrescriptionDetails>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Define 1-to-1 relationship between Claim and PrescriptionDetail
        modelBuilder.Entity<Claim>()
            .HasOne(c => c.PrescriptionDetail)
            .WithOne(p => p.Claim)
            .HasForeignKey<PrescriptionDetails>(p => p.PrescriptionClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}