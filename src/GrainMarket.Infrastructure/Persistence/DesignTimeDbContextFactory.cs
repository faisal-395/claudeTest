using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GrainMarket.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` run against this project directly, without spinning up
/// the full Api host. The connection string here is only used at design time to generate
/// migrations; the real one comes from GrainMarket.Api's appsettings.json at runtime.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=grainmarket;Username=postgres;Password=postgres");
        return new AppDbContext(optionsBuilder.Options);
    }
}
