using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClaimsService.Data;

public sealed class ClaimsDbContextFactory : IDesignTimeDbContextFactory<ClaimsDbContext>
{
    public ClaimsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ClaimsDb")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__ClaimsDb before running EF Core design-time commands.");

        var options = new DbContextOptionsBuilder<ClaimsDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ClaimsDbContext(options);
    }
}
