namespace Fulfillment.Application;

using Microsoft.Extensions.DependencyInjection;
using Fulfillment.Application.Services.Inventory;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Warehouse;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IShipmentService, ShipmentService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        return services;
    }
}
