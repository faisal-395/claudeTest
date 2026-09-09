using GrainMarket.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GrainMarket.Api.Tests;

/// <summary>Swaps the real Postgres-backed AppDbContext for a uniquely-named InMemory instance per
/// factory, so integration tests never need a live database.</summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // AddDbContext registers configuration via the multi-bound IDbContextOptionsConfiguration<T>
            // service, not just DbContextOptions<T> — removing only the latter leaves Npgsql's
            // configuration in place, so a second AddDbContext call (below) ends up applying both
            // providers to the same DbContextOptions and EF Core throws. Strip every descriptor
            // AddInfrastructure's AddDbContext<AppDbContext> registered before re-adding it.
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(AppDbContext) ||
                (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>))
            ).ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
