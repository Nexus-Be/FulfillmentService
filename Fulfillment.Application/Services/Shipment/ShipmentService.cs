namespace Fulfillment.Application.Services.Shipment;

using System;
using System.Collections.Generic;
using System.Linq;
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

    public ShipmentService(
        IFulfillmentDbContext dbContext,
        ICarrierResolver carrierResolver)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _carrierResolver = carrierResolver ?? throw new ArgumentNullException(nameof(carrierResolver));
    }

    public async Task<ShipmentDto> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber || s.GhnOrderCode == trackingNumber, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment", trackingNumber);

        return MapToDto(shipment);
    }

    public async Task<ShipmentDto> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment", orderId);

        return MapToDto(shipment);
    }

    public async Task<ShippingQuoteResponse> CalculateShippingFeeAsync(ShopShippingQuoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Dynamically resolve warehouse details for the seller, or fallback to default active warehouse
        var warehouse = await _dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.SellerId == request.SellerId && w.IsActive, cancellationToken)
            ?? await _dbContext.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.IsActive, cancellationToken);

        int? fromDistrictId = request.FromDistrictId > 0 ? request.FromDistrictId : warehouse?.DistrictId;
        if (!fromDistrictId.HasValue || fromDistrictId.Value <= 0)
        {
            throw new BusinessRuleValidationException("INVALID_FROM_DISTRICT", "FromDistrictId is required for calculating shipping fee.", 400);
        }

        var fromAddress = new ShippingAddress(
            FullName: warehouse?.Name ?? "Seller Warehouse",
            Phone: warehouse?.ContactPhone ?? string.Empty,
            AddressLine: warehouse?.Address ?? "Warehouse Address",
            WardCode: warehouse?.WardCode,
            DistrictId: fromDistrictId);

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
                    TotalWeightInGrams: request.WeightInGrams > 0 ? request.WeightInGrams : 500),
                cancellationToken);

            return new ShippingQuoteResponse(
                Fee: result.TotalFee,
                IsEstimatedFallback: false,
                ServiceName: result.ServiceCode ?? "GHN_STANDARD");
        }
        catch (CarrierIntegrationException ex)
        {
            throw new BusinessRuleValidationException("SHIPPING_QUOTE_FAILED", $"Failed to calculate shipping fee: {ex.Message}", 400);
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
                existing.TrackingNumber ?? existing.GhnOrderCode ?? string.Empty,
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

        // Construct sender warehouse address (Fix Bug 1: take real warehouse ContactPhone and DistrictId)
        var fromAddress = new ShippingAddress(
            FullName: warehouse.Name,
            Phone: !string.IsNullOrWhiteSpace(warehouse.ContactPhone) ? warehouse.ContactPhone : "0900000000",
            AddressLine: warehouse.Address ?? "Warehouse Storage, Ho Chi Minh, Vietnam",
            WardCode: warehouse.WardCode,
            DistrictId: warehouse.DistrictId);

        var toAddress = new ShippingAddress(
            FullName: request.RecipientName,
            Phone: request.RecipientPhone,
            AddressLine: request.ShippingAddress,
            WardCode: request.ToWardCode,
            DistrictId: request.ToDistrictId);

        var carrierItems = request.Items != null && request.Items.Count > 0
            ? request.Items.Select(i => new ShippingParcelItem(
                SkuId: i.SkuId.ToString(),
                Name: !string.IsNullOrWhiteSpace(i.Name) ? i.Name : "Product",
                Quantity: i.Quantity,
                WeightInGrams: i.WeightInGrams ?? 100,
                LengthCm: i.LengthCm ?? 10,
                WidthCm: i.WidthCm ?? 10,
                HeightCm: i.HeightCm ?? 10)).ToList()
            : null;

        var carrierResult = await carrier.CreateShipmentAsync(
            new CarrierCreateShipmentRequest(
                InternalOrderCode: request.SubOrderId.ToString(),
                FromAddress: fromAddress,
                ToAddress: toAddress,
                TotalWeightInGrams: request.WeightInGrams > 0 ? request.WeightInGrams : 500,
                LengthCm: request.LengthCm > 0 ? request.LengthCm : 20,
                WidthCm: request.WidthCm > 0 ? request.WidthCm : 15,
                HeightCm: request.HeightCm > 0 ? request.HeightCm : 10,
                CodAmount: request.CodAmount,
                Note: request.Note,
                Items: carrierItems),
            cancellationToken);

        var requiredNoteEnum = GHNRequiredNote.CHOXEMHANGKHONGTHU;
        if (!string.IsNullOrWhiteSpace(request.RequiredNote) &&
            Enum.TryParse<GHNRequiredNote>(request.RequiredNote, true, out var parsedNote))
        {
            requiredNoteEnum = parsedNote;
        }

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
            ToProvinceName = request.ToProvinceName,
            ToDistrictId = request.ToDistrictId,
            ToWardCode = request.ToWardCode,
            Weight = request.WeightInGrams > 0 ? request.WeightInGrams : 500,
            Length = request.LengthCm > 0 ? request.LengthCm : 20,
            Width = request.WidthCm > 0 ? request.WidthCm : 15,
            Height = request.HeightCm > 0 ? request.HeightCm : 10,
            CodAmount = request.CodAmount,
            InsuranceValue = request.InsuranceValue,
            PaymentTypeId = 1,
            RequiredNote = requiredNoteEnum,
            Note = request.Note,
            ClientOrderCode = request.SubOrderId.ToString(),
            GhnOrderCode = carrierResult.TrackingCode,
            PrintToken = carrierResult.TrackingCode,
            EstimatedDeliveryAt = carrierResult.ExpectedDeliveryTime,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            CreatedBy = request.SellerId,
            UpdatedBy = request.SellerId
        };

        if (request.Items != null && request.Items.Count > 0)
        {
            foreach (var item in request.Items)
            {
                shipment.Items.Add(new ShipmentItem
                {
                    ShipmentId = shipment.Id,
                    SkuId = item.SkuId,
                    Quantity = item.Quantity,
                    Name = !string.IsNullOrWhiteSpace(item.Name) ? item.Name : "Product",
                    Price = item.Price,
                    Weight = item.WeightInGrams ?? 100,
                    Length = item.LengthCm ?? 10,
                    Width = item.WidthCm ?? 10,
                    Height = item.HeightCm ?? 10
                });
            }
        }

        shipment.Events.Add(new ShipmentEvent
        {
            ShipmentId = shipment.Id,
            CarrierStatus = "ready_to_pick",
            Status = ShipmentEventStatus.Created,
            Description = "Shipment created and awaiting courier pickup.",
            OccurredAt = DateTimeOffset.UtcNow,
            WarehouseName = warehouse.Name
        });

        _dbContext.Shipments.Add(shipment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateShipmentResponse(
            shipment.Id,
            shipment.TrackingNumber ?? carrierResult.TrackingCode,
            carrierResult.LabelUrl ?? $"https://fulfillment.nexus.internal/labels/{shipment.Id}.pdf",
            shipment.Status.ToString());
    }

    public async Task<ConfirmReturnResponse> ConfirmReturnAsync(Guid orderId, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .Include(s => s.Items)
            .Include(s => s.Warehouse)
            .FirstOrDefaultAsync(s => s.OrderId == orderId || s.Id == orderId, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment for Order", orderId);

        var isOwner = isAdmin ||
                      (currentUserId != Guid.Empty &&
                       (shipment.Warehouse?.SellerId == currentUserId || shipment.CreatedBy == currentUserId));

        if (!isOwner)
        {
            throw new BusinessRuleValidationException("FORBIDDEN", "You do not have permission to confirm return for this shipment.", 403);
        }

        // Idempotency check: if return was already confirmed, return success without duplicate stock adjustment
        if (shipment.Status == ShipmentStatus.Returned)
        {
            return new ConfirmReturnResponse(
                Success: true,
                Code: "ALREADY_RETURNED",
                Message: "Return has already been confirmed for this shipment.",
                Data: new ConfirmReturnData(orderId, 0));
        }

        // Hoàn tồn kho cho các item thuộc shipment này
        int totalRestored = 0;
        foreach (var item in shipment.Items)
        {
            var invItem = await _dbContext.InventoryItems
                .FirstOrDefaultAsync(i => i.SkuId == item.SkuId && i.WarehouseId == shipment.WarehouseId, cancellationToken);

            if (invItem != null)
            {
                invItem.Adjust(item.Quantity);
                totalRestored += item.Quantity;

                var ledger = Domain.Entities.InventoryLedger.Create(
                    invItem.Id,
                    "RETURN",
                    item.Quantity,
                    referenceType: "ORDER_RETURN",
                    referenceId: orderId.ToString(),
                    note: $"Restored stock from returned order '{orderId}'.");

                _dbContext.InventoryLedgers.Add(ledger);
            }
        }

        // Cập nhật trạng thái shipment sang Returned
        shipment.Status = ShipmentStatus.Returned;
        shipment.UpdatedAt = DateTimeOffset.UtcNow;

        shipment.Events.Add(new ShipmentEvent
        {
            ShipmentId = shipment.Id,
            CarrierStatus = "returned",
            Status = ShipmentEventStatus.Returned,
            Description = "Seller confirmed return delivery. Stock restored.",
            OccurredAt = DateTimeOffset.UtcNow,
            WarehouseName = shipment.Warehouse?.Name
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ConfirmReturnResponse(
            Success: true,
            Code: "STOCK_RESTORED",
            Message: "Return confirmed successfully. Product stock has been restored to available inventory.",
            Data: new ConfirmReturnData(orderId, totalRestored));
    }

    private static ShipmentDto MapToDto(Domain.Entities.Shipment s)
    {
        var items = s.Items?.Select(i => new ShipmentItemDto(
            i.SkuId,
            i.Quantity,
            i.Name,
            i.Price,
            i.Weight,
            i.Length,
            i.Width,
            i.Height)).ToList();

        var events = s.Events?.Select(e => new ShipmentEventDto(
            e.Id,
            e.CarrierStatus,
            e.Status.ToString(),
            e.Description,
            e.OccurredAt,
            e.ReasonCode,
            e.Reason,
            e.ShipperName,
            e.ShipperPhone,
            e.PodUrl,
            e.WarehouseName)).OrderBy(e => e.OccurredAt).ToList();

        return new ShipmentDto(
            s.Id,
            s.OrderId,
            s.WarehouseId,
            s.CarrierId,
            s.TrackingNumber,
            s.Status.ToString(),
            s.ReceiverName,
            s.ReceiverPhone,
            s.ReceiverAddress,
            s.ToProvinceName,
            s.ToDistrictId,
            s.ToWardCode,
            s.Weight,
            s.Length,
            s.Width,
            s.Height,
            s.ShippingFee,
            s.CodAmount,
            s.InsuranceValue,
            s.PrintToken,
            s.GhnOrderCode,
            s.EstimatedDeliveryAt,
            s.DeliveredAt,
            items,
            events);
    }
}
