namespace Fulfillment.Api.Tests.Infrastructure;

using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fulfillment.Application.Interfaces;
using Fulfillment.Infrastructure.Persistence;

public class FulfillmentWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("INTERNAL_SERVICE_SECRET", "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e"),
                new KeyValuePair<string, string?>("InternalSecurity:ServiceKey", "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e"),
                new KeyValuePair<string, string?>("Carriers:DefaultCarrierCode", "GHN"),
                new KeyValuePair<string, string?>("Swagger:Enabled", "false")
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var fulfillmentDbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFulfillmentDbContext));
            if (fulfillmentDbContextDescriptor != null)
            {
                services.Remove(fulfillmentDbContextDescriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            services.AddScoped<IFulfillmentDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }
}

