namespace Fulfillment.Infrastructure.Carriers;

using System;
using System.Collections.Generic;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Infrastructure.Carriers.Ahamove;
using Fulfillment.Infrastructure.Carriers.Ghn;
using Fulfillment.Infrastructure.Carriers.Ghtk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class CarrierServiceCollectionExtensions
{
    public static IServiceCollection AddCarrierServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CarrierOptions>()
            .Configure(o =>
            {
                o.DefaultCarrierCode = configuration[$"{CarrierOptions.SectionName}:DefaultCarrierCode"] ?? string.Empty;
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.DefaultCarrierCode), "Configuration 'Carriers:DefaultCarrierCode' is missing or empty.")
            .ValidateOnStart();

        services.AddOptions<GhnOptions>()
            .Configure(o =>
            {
                var section = configuration.GetSection(GhnOptions.SectionName);
                o.Environment = section["Environment"] ?? "Staging";
                o.BaseUrl = section["BaseUrl"] ?? "https://dev-online-gateway.ghn.vn";
                o.Token = section["Token"] ?? string.Empty;
                o.ShopId = section["ShopId"] ?? string.Empty;
                if (int.TryParse(section["TimeoutSeconds"], out var timeout) && timeout > 0)
                {
                    o.TimeoutSeconds = timeout;
                }
            })
            .Validate(o =>
            {
                if (string.Equals(o.Environment, "Staging", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out var uri) ||
                        !string.Equals(uri.Host, "dev-online-gateway.ghn.vn", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                return true;
            }, "Security guardrail violation: Staging GHN BaseUrl host must strictly be 'dev-online-gateway.ghn.vn'. Production URLs are forbidden.")
            .ValidateOnStart();

        services.AddScoped<GhnGateway>();
        services.AddScoped<ICarrierGateway>(sp => sp.GetRequiredService<GhnGateway>());
        services.AddScoped<ICarrierGateway, GhtkGateway>();
        services.AddScoped<ICarrierGateway, AhamoveGateway>();

        services.AddScoped<ICarrierResolver>(sp =>
        {
            var gateways = sp.GetRequiredService<IEnumerable<ICarrierGateway>>();
            var options = sp.GetRequiredService<IOptions<CarrierOptions>>().Value;
            return new CarrierResolver(gateways, options.DefaultCarrierCode);
        });

        return services;
    }
}
