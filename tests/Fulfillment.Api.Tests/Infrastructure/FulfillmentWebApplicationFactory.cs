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
    public const string WebhookSecret = "test-webhook-secret";

    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString("N");

    /// <summary>Stands in for CommerceService so webhook tests neither need it nor wait on a network call.</summary>
    public RecordingCommerceWebhookClient Commerce { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("INTERNAL_SERVICE_SECRET", "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e"),
                new KeyValuePair<string, string?>("InternalSecurity:ServiceKey", "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e"),
                new KeyValuePair<string, string?>("Carriers:DefaultCarrierCode", "GHN"),
                new KeyValuePair<string, string?>("Carriers:GHN:WebhookSecret", WebhookSecret),
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

            // The GHN lists are not available in tests; tests about the lists install their own stand-in.
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(Fulfillment.Application.Interfaces.Shipping.IShippingLocationService)).ToList())
                services.Remove(descriptor);
            services.AddSingleton<Fulfillment.Application.Interfaces.Shipping.IShippingLocationService, AcceptingLocations>();

            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(Fulfillment.Application.Interfaces.Shipping.ICommerceWebhookClient)).ToList())
                services.Remove(descriptor);
            services.AddSingleton<Fulfillment.Application.Interfaces.Shipping.ICommerceWebhookClient>(Commerce);

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }
}

public sealed class RecordingCommerceWebhookClient : Fulfillment.Application.Interfaces.Shipping.ICommerceWebhookClient
{
    private readonly List<(Guid OrderId, string Status, string? Tracking)> _sent = new();

    /// <summary>When false, Commerce "is down": nothing is acknowledged.</summary>
    public bool Acknowledges { get; set; } = true;

    public IReadOnlyList<(Guid OrderId, string Status, string? Tracking)> Sent => _sent;

    public Task<bool> NotifyDeliveryStatusAsync(
        Guid subOrderId, string status, string? trackingNumber = null, string? description = null,
        DateTimeOffset? occurredAt = null, CancellationToken cancellationToken = default)
    {
        lock (_sent) _sent.Add((subOrderId, status, trackingNumber));
        return Task.FromResult(Acknowledges);
    }
}

/// <summary>The same app with no webhook secret configured: the webhook must stay closed.</summary>
public sealed class UnconfiguredWebhookFactory : FulfillmentWebApplicationFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("Carriers:GHN:WebhookSecret", "") }));
    }
}
