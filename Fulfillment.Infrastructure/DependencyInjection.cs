namespace Fulfillment.Infrastructure;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.AI;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Infrastructure.AI;
using Fulfillment.Infrastructure.Carriers;
using Fulfillment.Infrastructure.Excel;
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

        // AI Schema Inference & Excel Parser
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.PostConfigure<AiOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                options.ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                              ?? Environment.GetEnvironmentVariable("AI_API_KEY")
                              ?? string.Empty;
            }
        });
        services.AddHttpClient<IAiSchemaInferenceService, GeminiSchemaInferenceService>();
        services.AddScoped<IInventoryExcelParser, InventoryExcelParser>();

        // Identity propagation delegating handler for outgoing inter-service HTTP requests
        services.AddTransient<Services.Common.IdentityPropagationHandler>();

        return services;
    }
}
