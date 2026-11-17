namespace Fulfillment.Infrastructure;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.AI;
using Fulfillment.Application.Interfaces.Excel;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Infrastructure.AI;
using Fulfillment.Infrastructure.Carriers;
using Fulfillment.Infrastructure.Excel;
using Fulfillment.Infrastructure.HttpClients;
using Fulfillment.Infrastructure.Persistence;
using Fulfillment.Infrastructure.Services.Notification;

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

        // Delivery updates go to Commerce's webhook, which refuses a call without the internal service key.
        // The address comes from configuration: inside Docker "localhost" is Fulfillment itself, never Commerce.
        services.AddHttpClient<ICommerceWebhookClient, CommerceWebhookClient>(client =>
        {
            var baseUrl = configuration["Services:CommerceService:BaseUrl"]
                ?? configuration["COMMERCE_SERVICE_URL"]
                ?? Environment.GetEnvironmentVariable("COMMERCE_SERVICE_URL")
                ?? "http://localhost:8003";
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);

            var serviceKey = Environment.GetEnvironmentVariable("INTERNAL_SERVICE_SECRET")
                ?? configuration["INTERNAL_SERVICE_SECRET"]
                ?? configuration["InternalSecurity:ServiceKey"]
                ?? "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e";
            if (!string.IsNullOrWhiteSpace(serviceKey))
                client.DefaultRequestHeaders.Add("X-Internal-Service-Key", serviceKey);
        });

        var notificationBaseUrl = Environment.GetEnvironmentVariable("NOTIFICATION_SERVICE_URL")
            ?? configuration["NOTIFICATION_SERVICE_URL"]
            ?? configuration["Services:NotificationService:BaseUrl"]
            ?? configuration["Services:Notification:BaseUrl"]
            ?? "http://localhost:8007/";

        services.AddHttpClient<INotificationClient, NotificationClient>(client =>
        {
            client.BaseAddress = new Uri(notificationBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

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

            options.ApiKey = options.ApiKey?.Trim('"', '\'', ' ') ?? string.Empty;

            var envModel = Environment.GetEnvironmentVariable("GEMINI_MODEL")
                        ?? Environment.GetEnvironmentVariable("AI_MODEL");
            if (!string.IsNullOrWhiteSpace(envModel))
            {
                options.Model = envModel.Trim('"', '\'', ' ');
            }
        });
        services.AddHttpClient<IAiSchemaInferenceService, GeminiSchemaInferenceService>();
        services.AddScoped<IInventoryExcelParser, InventoryExcelParser>();
        services.AddScoped<IInventoryExcelExporter, InventoryExcelExporter>();
        services.AddSingleton<IConcurrencyLockService, Services.KeyedConcurrencyLockService>();

        // Identity propagation delegating handler for outgoing inter-service HTTP requests
        services.AddTransient<Services.Common.IdentityPropagationHandler>();

        return services;
    }
}
