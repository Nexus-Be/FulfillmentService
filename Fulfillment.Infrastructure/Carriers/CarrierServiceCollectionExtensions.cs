namespace Fulfillment.Infrastructure.Carriers;

using System.Collections.Generic;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
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

        services.AddScoped<ICarrierResolver>(sp =>
        {
            var gateways = sp.GetRequiredService<IEnumerable<ICarrierGateway>>();
            var options = sp.GetRequiredService<IOptions<CarrierOptions>>().Value;
            return new CarrierResolver(gateways, options.DefaultCarrierCode);
        });

        return services;
    }
}
