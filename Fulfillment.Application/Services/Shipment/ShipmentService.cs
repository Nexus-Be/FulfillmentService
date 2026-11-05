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

        // Dynamically resolve warehouse details for the seller, or fallback to default system warehouse
        var warehouse = await _dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.SellerId == request.SellerId && w.IsActive, cancellationToken)
            ?? await _dbContext.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.IsActive, cancellationToken);

        var fromAddress = new ShippingAddress(
            FullName: warehouse?.Name ?? "Seller Warehouse",
            Phone: string.Empty,
            AddressLine: warehouse?.Address ?? "Warehouse Address",
            DistrictId: request.FromDistrictId);

        var toAddress = new ShippingAddress(
            FullName: "Estimated Recipient",
            Phone: string.Empty,
            AddressLine: "Recipient Shipping Address",
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
                ServiceName: result.ServiceCode ?? $"{carrier.Code}_STANDARD");
        }
        catch (CarrierException)
        {
            return new ShippingQuoteResponse(
                Fee: 35000.00m,
                IsEstimatedFallback: true,
                ServiceName: $"{carrier.Code}_STANDARD_FALLBACK");
        }
    }

    public async Task<CreateShipmentResponse> CreateShipmentAsync(CreateShipmentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Idempotency check: strictly verify if shipment already created for this SubOrder
        var existing = await _dbContext.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrderId == request.SubOrderId, cancellationToken);

        if (existing != null)
        {
            return new CreateShipmentResponse(
                existing.Id,
                existing.TrackingNumber ?? string.Empty,
                $"https://fulfillment.nexus.internal/labels/{existing.Id}.pdf",
                existing.Status.ToString());
        }

        // Dynamically resolve warehouse for the seller
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.SellerId == request.SellerId && w.IsActive, cancellationToken)
            ?? await _dbContext.Warehouses
                .FirstOrDefaultAsync(w => w.IsActive, cancellationToken)
            ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse for seller", request.SellerId);

        // Dynamically resolve active carrier entity matching the resolver carrier code
        var carrier = _carrierResolver.Get();
        var carrierEntity = await _dbContext.Carriers
            .FirstOrDefaultAsync(c => c.Code == carrier.Code && c.IsActive, cancellationToken)
            ?? await _dbContext.Carriers
                .FirstOrDefaultAsync(c => c.IsActive, cancellationToken)
            ?? throw new EntityNotFoundException("CARRIER_NOT_FOUND", "Carrier", carrier.Code);

        var fromAddress = new ShippingAddress(
            FullName: warehouse.Name,
            Phone: request.RecipientPhone,
            AddressLine: warehouse.Address ?? "Warehouse Storage, Ho Chi Minh, Vietnam",
            DistrictId: request.ToDistrictId > 0 ? null : null);

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
            WarehouseId = warehouse.Id,
            CarrierId = carrierEntity.Id,
            ServiceType = ShippingServiceType.Standard,
            TrackingNumber = carrierResult.TrackingCode,
            Status = ShipmentStatus.ReadyToPick,
            ShippingFee = carrierResult.ShippingFee,
            ReceiverName = request.RecipientName,
            ReceiverPhone = request.RecipientPhone,
            ReceiverAddress = request.ShippingAddress,
            EstimatedDeliveryAt = carrierResult.ExpectedDeliveryTime,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            CreatedBy = request.SellerId,
            UpdatedBy = request.SellerId
        };

        _dbContext.Shipments.Add(shipment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateShipmentResponse(
            shipment.Id,
            shipment.TrackingNumber ?? carrierResult.TrackingCode,
            carrierResult.LabelUrl ?? $"https://fulfillment.nexus.internal/labels/{shipment.Id}.pdf",
            shipment.Status.ToString());
    }
}
