using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClientsService.Data;

public sealed class ClientsDbContextFactory : IDesignTimeDbContextFactory<ClientsDbContext>
{
    public ClientsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ClientsDb")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__ClientsDb before running EF Core design-time commands.");

        var options = new DbContextOptionsBuilder<ClientsDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ClientsDbContext(options);
    }
}
