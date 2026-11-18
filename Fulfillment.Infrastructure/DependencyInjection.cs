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
using Fulfillment.Infrastructure.Shipping;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddHttpClient<IShippingLocationService, GhnLocationService>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5438;Database=fulfillment_db;Username=postgres;Password=MatkhaucuaTui123@";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IFulfillmentDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddCarrierServices(configuration);

        // Options pattern for CommerceService integration
        services.Configure<CommerceOptions>(options =>
        {
            var baseUrl = configuration["Services:CommerceService:BaseUrl"]
                ?? configuration["COMMERCE_SERVICE_URL"]
                ?? Environment.GetEnvironmentVariable("COMMERCE_SERVICE_URL");
            if (!string.IsNullOrWhiteSpace(baseUrl))
                options.BaseUrl = baseUrl;

            var path = configuration["Services:CommerceService:DeliveryStatusPath"]
                ?? configuration["COMMERCE_DELIVERY_STATUS_PATH"]
                ?? Environment.GetEnvironmentVariable("COMMERCE_DELIVERY_STATUS_PATH");
            if (!string.IsNullOrWhiteSpace(path))
                options.DeliveryStatusPath = path;

            var serviceKey = Environment.GetEnvironmentVariable("INTERNAL_SERVICE_SECRET")
                ?? Environment.GetEnvironmentVariable("INTERNAL_SERVICE_KEY")
                ?? configuration["INTERNAL_SERVICE_SECRET"]
                ?? configuration["INTERNAL_SERVICE_KEY"]
                ?? configuration["InternalSecurity:ServiceKey"];
            if (!string.IsNullOrWhiteSpace(serviceKey))
                options.ServiceKey = serviceKey;
        });

        // Delivery updates go to Commerce's webhook, which refuses a call without the internal service key.
        // The address comes from configuration: inside Docker "localhost" is Fulfillment itself, never Commerce.
        services.AddHttpClient<ICommerceWebhookClient, CommerceWebhookClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CommerceOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 10);

            if (!string.IsNullOrWhiteSpace(options.ServiceKey))
                client.DefaultRequestHeaders.Add("X-Internal-Service-Key", options.ServiceKey);
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
