using ClaimsService.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimsService.Data;

public class ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : DbContext(options)
{
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<PrescriptionDetails> PrescriptionDetails => Set<PrescriptionDetails>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Claim>().ToTable("Claim");
        modelBuilder.Entity<PrescriptionDetails>().ToTable("PrescriptionDetails");
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.Property(message => message.EventType).HasMaxLength(100).IsRequired();
            entity.Property(message => message.Payload).IsRequired();
            entity.Property(message => message.LastError).HasMaxLength(2048);
            entity.HasIndex(message => new { message.ProcessedAtUtc, message.Id });
        });

        modelBuilder.Entity<Claim>()
            .HasOne(claim => claim.PrescriptionDetail)
            .WithOne(prescription => prescription.Claim)
            .HasForeignKey<PrescriptionDetails>(prescription => prescription.PrescriptionClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
