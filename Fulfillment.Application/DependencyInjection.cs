namespace Fulfillment.Application;

using Microsoft.Extensions.DependencyInjection;
using Fulfillment.Application.Services.Shipment;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IShipmentService, ShipmentService>();
        return services;
    }
}
