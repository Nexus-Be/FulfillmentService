namespace Fulfillment.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Infrastructure.Carriers;
using Fulfillment.Infrastructure.HttpClients;
using Fulfillment.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5438;Database=fulfillment_db;Username=postgres;Password=MatkhaucuaTui123@";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IFulfillmentDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddCarrierServices(configuration);

        services.AddScoped<ICommerceWebhookClient, CommerceWebhookClient>();

        // Identity propagation delegating handler for outgoing inter-service HTTP requests
        services.AddTransient<Services.Common.IdentityPropagationHandler>();

        return services;
    }
}
