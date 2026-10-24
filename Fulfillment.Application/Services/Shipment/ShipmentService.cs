namespace Fulfillment.Application.Services.Shipment;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Interfaces;

public sealed class ShipmentService : IShipmentService
{
    private readonly IFulfillmentDbContext _dbContext;

    public ShipmentService(IFulfillmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShipmentDto?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, cancellationToken);

        if (shipment == null) return null;

        return new ShipmentDto(
            shipment.Id,
            shipment.OrderId,
            shipment.WarehouseId,
            shipment.CarrierId,
            shipment.TrackingNumber,
            shipment.Status.ToString(),
            shipment.ReceiverName,
            shipment.ReceiverPhone,
            shipment.ReceiverAddress);
    }

    public async Task<ShipmentDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);

        if (shipment == null) return null;

        return new ShipmentDto(
            shipment.Id,
            shipment.OrderId,
            shipment.WarehouseId,
            shipment.CarrierId,
            shipment.TrackingNumber,
            shipment.Status.ToString(),
            shipment.ReceiverName,
            shipment.ReceiverPhone,
            shipment.ReceiverAddress);
    }
}
