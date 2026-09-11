using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MedMatch.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can create a DbContext without running
/// the API host (which requires environment variables). Migrations are generated
/// offline; no live database connection is needed.
/// </summary>
public sealed class MedMatchDbContextFactory : IDesignTimeDbContextFactory<MedMatchDbContext>
{
    public MedMatchDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MedMatchDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=medmatch;Username=medmatch;Password=ChangeMe!")
            .Options;
        return new MedMatchDbContext(options);
    }
}
