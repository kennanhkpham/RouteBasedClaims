using ClientsService.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientsService.Data;

public sealed class ClientsDbContext(DbContextOptions<ClientsDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("Client");
            entity.Property(client => client.PolicyNumberKey)
                .HasMaxLength(64)
                .IsRequired();
            entity.HasIndex(client => client.PolicyNumberKey)
                .IsUnique();
        });
    }
}
