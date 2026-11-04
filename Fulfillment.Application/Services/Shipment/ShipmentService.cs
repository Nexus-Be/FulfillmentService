namespace Fulfillment.Application.Services.Shipment;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Exceptions;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;

public sealed class ShipmentService : IShipmentService
{
    private readonly IFulfillmentDbContext _dbContext;
    private readonly ICarrierResolver _carrierResolver;

    public ShipmentService(IFulfillmentDbContext dbContext, ICarrierResolver carrierResolver)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _carrierResolver = carrierResolver ?? throw new ArgumentNullException(nameof(carrierResolver));
    }

    public async Task<ShipmentDto> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment", trackingNumber);

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

    public async Task<ShipmentDto> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment", orderId);

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

    public async Task<ShippingQuoteResponse> CalculateShippingFeeAsync(ShopShippingQuoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fromAddress = new ShippingAddress(
            FullName: "Seller Warehouse",
            Phone: "0900000000",
            AddressLine: "Warehouse Address",
            DistrictId: request.FromDistrictId);

        var toAddress = new ShippingAddress(
            FullName: "Recipient",
            Phone: "0900000000",
            AddressLine: "Recipient Address",
            WardCode: request.ToWardCode,
            DistrictId: request.ToDistrictId);

        var carrier = _carrierResolver.Get();

        try
        {
            var result = await carrier.CalculateFeeAsync(
                new CarrierFeeCalculationRequest(
                    FromAddress: fromAddress,
                    ToAddress: toAddress,
                    TotalWeightInGrams: request.WeightInGrams),
                cancellationToken);

            return new ShippingQuoteResponse(
                Fee: result.TotalFee,
                IsEstimatedFallback: false,
                ServiceName: result.ServiceCode ?? "GHN_STANDARD");
        }
        catch (CarrierTimeoutException)
        {
            return new ShippingQuoteResponse(
                Fee: 35000.00m,
                IsEstimatedFallback: true,
                ServiceName: "GHN_STANDARD_FALLBACK");
        }
    }

    public async Task<CreateShipmentResponse> CreateShipmentAsync(CreateShipmentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrderId == request.SubOrderId || s.OrderId == request.OrderId, cancellationToken);

        if (existing != null)
        {
            return new CreateShipmentResponse(
                existing.Id,
                existing.TrackingNumber ?? string.Empty,
                $"https://fulfillment.nexus.internal/labels/{existing.Id}.pdf",
                existing.Status);
        }

        var warehouse = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.IsActive, cancellationToken);
        var warehouseId = warehouse?.Id ?? Guid.Parse("00000000-0000-0000-0000-000000000001");

        var carrierEntity = await _dbContext.Carriers.FirstOrDefaultAsync(c => c.IsActive, cancellationToken);
        var carrierId = carrierEntity?.Id ?? Guid.Parse("00000000-0000-0000-0000-000000000002");

        var carrier = _carrierResolver.Get();
        var fromAddress = new ShippingAddress(
            FullName: warehouse?.Name ?? "Main Warehouse",
            Phone: "0901234567",
            AddressLine: warehouse?.Address ?? "Warehouse WH_001, Hanoi, Vietnam");

        var toAddress = new ShippingAddress(
            FullName: request.RecipientName,
            Phone: request.RecipientPhone,
            AddressLine: request.ShippingAddress,
            WardCode: request.ToWardCode,
            DistrictId: request.ToDistrictId);

        var carrierResult = await carrier.CreateShipmentAsync(
            new CarrierCreateShipmentRequest(
                InternalOrderCode: request.SubOrderId.ToString(),
                FromAddress: fromAddress,
                ToAddress: toAddress,
                TotalWeightInGrams: request.WeightInGrams),
            cancellationToken);

        var shipment = new Domain.Entities.Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = request.SubOrderId,
            WarehouseId = warehouseId,
            CarrierId = carrierId,
            TrackingNumber = carrierResult.TrackingCode,
            Status = ShipmentStatus.ReadyToPick.ToString(),
            ShippingFee = carrierResult.ShippingFee,
            ReceiverName = request.RecipientName,
            ReceiverPhone = request.RecipientPhone,
            ReceiverAddress = request.ShippingAddress,
            EstimatedDeliveryAt = carrierResult.ExpectedDeliveryTime,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Shipments.Add(shipment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateShipmentResponse(
            shipment.Id,
            shipment.TrackingNumber ?? carrierResult.TrackingCode,
            carrierResult.LabelUrl ?? $"https://fulfillment.nexus.internal/labels/{shipment.Id}.pdf",
            shipment.Status);
    }
}
