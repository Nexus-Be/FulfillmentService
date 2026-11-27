namespace Fulfillment.Application.Services.Shipment;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Exceptions;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Entities;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;

public sealed class ShipmentService : IShipmentService
{
    private readonly IFulfillmentDbContext _dbContext;
    private readonly ICarrierResolver _carrierResolver;
    private readonly IConcurrencyLockService? _lockService;
    private readonly Fulfillment.Application.Services.Shipping.ShippingEstimateOptions _estimateOptions;
    private readonly DeliveryNotificationOutbox? _outbox;
    private readonly INotificationClient? _notificationClient;
    private readonly ILogger<ShipmentService>? _logger;

    public ShipmentService(
        IFulfillmentDbContext dbContext,
        ICarrierResolver carrierResolver,
        IConcurrencyLockService? lockService = null,
        Fulfillment.Application.Services.Shipping.ShippingEstimateOptions? estimateOptions = null,
        DeliveryNotificationOutbox? outbox = null,
        INotificationClient? notificationClient = null,
        ILogger<ShipmentService>? logger = null,
        ShippingBehaviorOptions? behavior = null)
    {
        _behavior = behavior ?? new ShippingBehaviorOptions();
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _carrierResolver = carrierResolver ?? throw new ArgumentNullException(nameof(carrierResolver));
        _lockService = lockService;
        _estimateOptions = estimateOptions ?? new Fulfillment.Application.Services.Shipping.ShippingEstimateOptions();
        _outbox = outbox;
        _notificationClient = notificationClient;
        _logger = logger;
    }

    public async Task<ShipmentDto> GetByTrackingNumberAsync(
        string trackingNumber,
        Guid currentUserId = default,
        bool isAdminOrStaff = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var trimmed = trackingNumber.Trim();
        var trimmedLower = trimmed.ToLowerInvariant();
        var isGuid = Guid.TryParse(trimmed, out var lookupGuid);

        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Events)
            .Include(s => s.Warehouse)
            .FirstOrDefaultAsync(s =>
                (s.TrackingNumber != null && s.TrackingNumber.ToLower() == trimmedLower) ||
                (s.GhnOrderCode != null && s.GhnOrderCode.ToLower() == trimmedLower) ||
                (s.SubOrderNo != null && s.SubOrderNo.ToLower() == trimmedLower) ||
                (s.ClientOrderCode != null && s.ClientOrderCode.ToLower() == trimmedLower) ||
                (isGuid && (s.OrderId == lookupGuid || s.Id == lookupGuid)),
                cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment", trackingNumber);

        ValidateShipmentAccess(shipment, currentUserId, isAdminOrStaff);

        return MapToDto(shipment);
    }

    public async Task<ShipmentDto> GetByOrderIdAsync(
        Guid orderId,
        Guid currentUserId = default,
        bool isAdminOrStaff = false,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Events)
            .Include(s => s.Warehouse)
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment", orderId);

        ValidateShipmentAccess(shipment, currentUserId, isAdminOrStaff);

        return MapToDto(shipment);
    }

    public async Task<ShipmentDto> GetByIdAsync(
        Guid shipmentId,
        Guid currentUserId = default,
        bool isAdminOrStaff = false,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Events)
            .Include(s => s.Warehouse)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment", shipmentId);

        ValidateShipmentAccess(shipment, currentUserId, isAdminOrStaff);

        return MapToDto(shipment);
    }

    private static void ValidateShipmentAccess(Domain.Entities.Shipment shipment, Guid currentUserId, bool isAdminOrStaff)
    {
        if (isAdminOrStaff || currentUserId == Guid.Empty)
        {
            return;
        }

        var isOwner = (shipment.Warehouse != null && shipment.Warehouse.SellerId == currentUserId) ||
                      shipment.CreatedBy == currentUserId ||
                      (shipment.BuyerId.HasValue && shipment.BuyerId.Value == currentUserId);

        if (!isOwner)
        {
            throw new BusinessRuleValidationException("FORBIDDEN", "You do not have permission to view this shipment.", 403);
        }
    }

    private static bool IsAddressIncomplete(Domain.Entities.Warehouse warehouse) =>
        !warehouse.ProvinceId.HasValue || !warehouse.DistrictId.HasValue || string.IsNullOrWhiteSpace(warehouse.WardCode);

    public async Task<ShippingQuoteResponse> CalculateShippingFeeAsync(ShopShippingQuoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The origin is the seller's own active warehouse. Another seller's warehouse is never used: a shop without one is told so.
        // A caller that names the origin district itself (an estimate) does not need a warehouse.
        var warehouse = await _dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.SellerId == request.SellerId && w.IsActive, cancellationToken);
        if (warehouse is null && request.FromDistrictId <= 0)
        {
            throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Active warehouse of seller", request.SellerId);
        }

        int? fromDistrictId = request.FromDistrictId > 0 ? request.FromDistrictId : warehouse?.DistrictId;
        if (!fromDistrictId.HasValue || fromDistrictId.Value <= 0)
        {
            throw new BusinessRuleValidationException("INVALID_FROM_DISTRICT", "FromDistrictId is required for calculating shipping fee.", 400);
        }

        // The seller's own warehouse is the origin only if its whole address was confirmed against the GHN lists when it was saved (the province id is
        // set only by such a save). A caller that names the origin district itself is making an estimate and does not need the warehouse.
        if (warehouse is not null && request.FromDistrictId <= 0 && IsAddressIncomplete(warehouse))
        {
            throw new BusinessRuleValidationException("WAREHOUSE_ADDRESS_INCOMPLETE", "The warehouse address is not complete or has not been confirmed yet.", 400);
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
        catch (CarrierIntegrationException ex) when (_estimateOptions.Enabled && ex.ErrorCode == "MISSING_TOKEN")
        {
            // Opt-in only (see ShippingEstimateOptions): the carrier has no credentials here, so give a clearly marked estimate.
            return new ShippingQuoteResponse(
                Fee: _estimateOptions.Estimate(request.WeightInGrams > 0 ? request.WeightInGrams : 500),
                IsEstimatedFallback: true,
                ServiceName: "ESTIMATED");
        }
        catch (CarrierTimeoutException)
        {
            throw new BusinessRuleValidationException("SHIPPING_CARRIER_TIMEOUT", "The shipping carrier did not answer in time.", 503);
        }
        catch (CarrierNotFoundException)
        {
            throw new BusinessRuleValidationException("SHIPPING_CARRIER_NOT_CONFIGURED", "The shipping carrier is not configured.", 503);
        }
        catch (CarrierIntegrationException ex)
        {
            throw MapQuoteFailure(ex);
        }
    }

    /// <summary>
    /// A quote that failed because of the carrier (no credentials, refused credentials, carrier down, network) is not the buyer's address:
    /// it is reported as 503 with a code of its own, so the caller does not tell the buyer to fix an address that is fine. Only a carrier
    /// answer about the data itself (bad district or ward, bad weight) stays the 400 <c>SHIPPING_QUOTE_FAILED</c>.
    /// </summary>
    internal static BusinessRuleValidationException MapQuoteFailure(CarrierIntegrationException ex)
    {
        if (ex.ErrorCode is "MISSING_TOKEN")
        {
            return new BusinessRuleValidationException("SHIPPING_CARRIER_NOT_CONFIGURED", "The shipping carrier is not configured.", 503);
        }

        if (ex.ErrorCode is "HTTP_ERROR")
        {
            return new BusinessRuleValidationException("SHIPPING_CARRIER_UNAVAILABLE", "The shipping carrier is temporarily unavailable.", 503);
        }

        // For an HTTP answer of the carrier the error code is the name of the status ("Unauthorized", "BadGateway", ...).
        if (Enum.TryParse<System.Net.HttpStatusCode>(ex.ErrorCode, out var status))
        {
            if (status is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                return new BusinessRuleValidationException("SHIPPING_CARRIER_NOT_CONFIGURED", "The shipping carrier refused the configured credentials.", 503);
            }

            if ((int)status >= 500 || status is System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.TooManyRequests)
            {
                return new BusinessRuleValidationException("SHIPPING_CARRIER_UNAVAILABLE", "The shipping carrier is temporarily unavailable.", 503);
            }
        }

        return new BusinessRuleValidationException("SHIPPING_QUOTE_FAILED", $"Failed to calculate shipping fee: {ex.Message}", 400);
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

        // The sender is the seller's own active warehouse. Falling back to any other warehouse would send the parcel pickup and the
        // shipping fee to another seller's address, so a seller without one gets WAREHOUSE_NOT_FOUND and no shipment is created.
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.SellerId == request.SellerId && w.IsActive, cancellationToken)
            ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Active warehouse of seller", request.SellerId);

        if (IsAddressIncomplete(warehouse))
        {
            throw new BusinessRuleValidationException("WAREHOUSE_ADDRESS_INCOMPLETE", "The warehouse address is not complete or has not been confirmed yet, so no shipment can be created.", 400);
        }

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
                Items: carrierItems,
                ShopId: warehouse.GhnShopId,
                PickShiftId: request.PickShiftId),
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
            SubOrderNo = string.IsNullOrWhiteSpace(request.SubOrderNo) ? null : request.SubOrderNo.Trim(),
            BuyerId = request.BuyerId,
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
        using var _ = _lockService != null
            ? await _lockService.AcquireLockAsync($"shipment:order:{orderId}", cancellationToken: cancellationToken)
            : null;

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

        // Only shipments that failed delivery, are on their way back or are marked returned by the carrier can be confirmed for inventory restoration
        if (shipment.Status is not (ShipmentStatus.DeliveryFailed or ShipmentStatus.Returning or ShipmentStatus.Returned))
        {
            throw new BusinessRuleValidationException("SHIPMENT_NOT_RETURNING", "Shipment is not in a returnable state.", 409);
        }

        // Idempotency check via InventoryLedgers: if return was already confirmed and stock restored, return success without duplicate adjustment
        var referenceIds = new[] { shipment.OrderId.ToString(), orderId.ToString() };
        var alreadyReturned = await _dbContext.InventoryLedgers
            .AnyAsync(l => l.ReferenceType == "ORDER_RETURN" && l.ReferenceId != null && referenceIds.Contains(l.ReferenceId), cancellationToken);

        if (alreadyReturned)
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
                    referenceId: shipment.OrderId.ToString(),
                    note: $"Restored stock from returned order '{shipment.OrderId}'.");

                _dbContext.InventoryLedgers.Add(ledger);
            }
        }

        // Giữ nguyên trạng thái shipment cho carrier cập nhật, chỉ thêm event và cập nhật UpdatedAt
        shipment.UpdatedAt = DateTimeOffset.UtcNow;

        shipment.Events.Add(new ShipmentEvent
        {
            ShipmentId = shipment.Id,
            CarrierStatus = shipment.Status == ShipmentStatus.Returned ? "returned" : shipment.Status == ShipmentStatus.Returning ? "returning" : "delivery_fail",
            Status = shipment.Status == ShipmentStatus.Returned ? ShipmentEventStatus.Returned : shipment.Status == ShipmentStatus.Returning ? ShipmentEventStatus.Returning : ShipmentEventStatus.DeliveryFailed,
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

    public async Task<PagedData<ShipmentDto>> ListShipmentsAsync(
        ShipmentFilterRequest request,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = _dbContext.Shipments
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Events)
            .Include(s => s.Warehouse)
            .AsQueryable();

        // Enforce role isolation: sellers see shipments from their warehouse, buyers see their purchases
        if (!isAdmin)
        {
            query = query.Where(s =>
                (s.Warehouse != null && s.Warehouse.SellerId == currentUserId) ||
                s.CreatedBy == currentUserId ||
                (s.BuyerId != null && s.BuyerId == currentUserId));
        }

        if (request.WarehouseId.HasValue)
        {
            query = query.Where(s => s.WarehouseId == request.WarehouseId.Value);
        }

        if (request.OrderId.HasValue)
        {
            query = query.Where(s => s.OrderId == request.OrderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TrackingNumber))
        {
            var trackingLower = request.TrackingNumber.Trim().ToLowerInvariant();
            query = query.Where(s =>
                (s.TrackingNumber != null && s.TrackingNumber.ToLower().Contains(trackingLower)) ||
                (s.GhnOrderCode != null && s.GhnOrderCode.ToLower().Contains(trackingLower)));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.Trim().ToLowerInvariant();
            var isSearchGuid = Guid.TryParse(request.Search.Trim(), out var searchGuid);
            query = query.Where(s =>
                (s.TrackingNumber != null && s.TrackingNumber.ToLower().Contains(searchLower)) ||
                (s.GhnOrderCode != null && s.GhnOrderCode.ToLower().Contains(searchLower)) ||
                (s.SubOrderNo != null && s.SubOrderNo.ToLower().Contains(searchLower)) ||
                (s.ClientOrderCode != null && s.ClientOrderCode.ToLower().Contains(searchLower)) ||
                s.ReceiverName.ToLower().Contains(searchLower) ||
                s.ReceiverPhone.ToLower().Contains(searchLower) ||
                (isSearchGuid && (s.OrderId == searchGuid || s.Id == searchGuid)));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var tokens = request.Status
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var parsedStatuses = new List<ShipmentStatus>(tokens.Length);
            foreach (var token in tokens)
            {
                if (!int.TryParse(token, out _) &&
                    Enum.TryParse<ShipmentStatus>(token, true, out var parsedStatus) &&
                    Enum.IsDefined(typeof(ShipmentStatus), parsedStatus))
                {
                    parsedStatuses.Add(parsedStatus);
                }
                else
                {
                    throw new BusinessRuleValidationException("VAL_INVALID_STATUS", $"Invalid shipment status '{token}'.", 400);
                }
            }

            if (parsedStatuses.Count == 1)
            {
                var singleStatus = parsedStatuses[0];
                query = query.Where(s => s.Status == singleStatus);
            }
            else if (parsedStatuses.Count > 1)
            {
                query = query.Where(s => parsedStatuses.Contains(s.Status));
            }
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(s => s.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(s => s.CreatedAt <= request.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        int safePage = Math.Max(1, request.Page);
        int safePageSize = Math.Clamp(request.PageSize, 1, 100);

        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        var sortOrder = request.SortOrder?.Trim().ToLowerInvariant();
        var isAscending = sortOrder is "asc" or "oldest";

        var orderedQuery = (sortBy, isAscending) switch
        {
            ("updatedat" or "updated_at", true) => query.OrderBy(s => s.UpdatedAt).ThenBy(s => s.Id),
            ("updatedat" or "updated_at", false) => query.OrderByDescending(s => s.UpdatedAt).ThenByDescending(s => s.Id),
            (_, true) => query.OrderBy(s => s.CreatedAt).ThenBy(s => s.Id),
            _ => query.OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.Id)
        };

        var shipments = await orderedQuery
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var dtos = shipments.Select(MapToDto).ToList();
        return new PagedData<ShipmentDto>(dtos, PaginationMetadata.Create(safePage, safePageSize, totalCount));
    }

    public async Task<CancelShipmentResponse> CancelShipmentAsync(
        Guid shipmentId,
        string? reason,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        using var _ = _lockService != null
            ? await _lockService.AcquireLockAsync($"shipment:id:{shipmentId}", cancellationToken: cancellationToken)
            : null;

        var shipment = await _dbContext.Shipments
            .Include(s => s.Warehouse)
            .Include(s => s.Carrier)
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment", shipmentId);

        var isOwner = isAdmin ||
                      (currentUserId != Guid.Empty &&
                       ((shipment.Warehouse != null && shipment.Warehouse.SellerId == currentUserId) || shipment.CreatedBy == currentUserId));

        if (!isOwner)
        {
            throw new BusinessRuleValidationException("FORBIDDEN", "You do not have permission to cancel this shipment.", 403);
        }

        return await CancelShipmentCoreAsync(shipment, reason, cancellationToken);
    }

    public async Task<CancelShipmentResponse> CancelShipmentByOrderIdAsync(
        Guid orderId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        using var _ = _lockService != null
            ? await _lockService.AcquireLockAsync($"shipment:order:{orderId}", cancellationToken: cancellationToken)
            : null;

        var shipment = await _dbContext.Shipments
            .Include(s => s.Warehouse)
            .Include(s => s.Carrier)
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment for Order", orderId);

        return await CancelShipmentCoreAsync(shipment, reason, cancellationToken);
    }

    private async Task<CancelShipmentResponse> CancelShipmentCoreAsync(
        Domain.Entities.Shipment shipment,
        string? reason,
        CancellationToken cancellationToken)
    {
        var trackingCode = shipment.GhnOrderCode ?? shipment.TrackingNumber;

        if (shipment.Status == ShipmentStatus.Cancelled)
        {
            return new CancelShipmentResponse(
                shipment.Id,
                trackingCode,
                true,
                "Shipment has already been cancelled.");
        }

        if (shipment.Status == ShipmentStatus.Delivered || shipment.Status == ShipmentStatus.Returned)
        {
            throw new BusinessRuleValidationException(
                "SHIPMENT_CANNOT_BE_CANCELLED",
                $"Shipment in status '{shipment.Status}' cannot be cancelled.",
                400);
        }

        if (!string.IsNullOrWhiteSpace(trackingCode))
        {
            var gateway = _carrierResolver.Get(shipment.Carrier?.Code);
            var carrierResult = await gateway.CancelAsync(trackingCode, cancellationToken);
            if (!carrierResult.Success)
            {
                throw new BusinessRuleValidationException(
                    "CARRIER_CANCELLATION_FAILED",
                    carrierResult.Message ?? "Failed to cancel shipment with the shipping carrier.",
                    400);
            }
        }

        shipment.Status = ShipmentStatus.Cancelled;
        shipment.UpdatedAt = DateTimeOffset.UtcNow;

        shipment.Events.Add(new ShipmentEvent
        {
            ShipmentId = shipment.Id,
            CarrierStatus = "cancelled",
            Status = ShipmentEventStatus.Cancelled,
            Description = string.IsNullOrWhiteSpace(reason) ? "Shipment cancelled by seller/admin." : $"Shipment cancelled: {reason.Trim()}",
            OccurredAt = DateTimeOffset.UtcNow,
            Reason = reason?.Trim(),
            WarehouseName = shipment.Warehouse?.Name
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CancelShipmentResponse(
            shipment.Id,
            trackingCode,
            true,
            "Shipment cancelled successfully.");
    }

    public async Task<ShipmentDto> UpdateRecipientAddressAsync(
        Guid orderId,
        UpdateRecipientAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var _ = _lockService != null
            ? await _lockService.AcquireLockAsync($"shipment:order:{orderId}", cancellationToken: cancellationToken)
            : null;

        var shipment = await _dbContext.Shipments
            .Include(s => s.Items)
            .Include(s => s.Events)
            .Include(s => s.Warehouse)
            .Include(s => s.Carrier)
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment for Order", orderId);

        if (shipment.Status is not (ShipmentStatus.ReadyToPick or ShipmentStatus.Packed))
        {
            throw new BusinessRuleValidationException(
                "SHIPMENT_ALREADY_PICKED_UP",
                "Cannot change recipient address because the shipment is already being processed or picked up by carrier.",
                409);
        }

        var trackingCode = shipment.GhnOrderCode ?? shipment.TrackingNumber;
        if (!string.IsNullOrWhiteSpace(trackingCode))
        {
            var gateway = _carrierResolver.Get(shipment.Carrier?.Code);
            await gateway.UpdateOrderAsync(
                trackingCode,
                request.RecipientName,
                request.RecipientPhone,
                request.ShippingAddress,
                request.ToWardCode,
                request.ToDistrictId,
                cancellationToken);
        }

        shipment.ReceiverName = request.RecipientName;
        shipment.ReceiverPhone = request.RecipientPhone;
        shipment.ReceiverAddress = request.ShippingAddress;
        if (!string.IsNullOrWhiteSpace(request.ToProvinceName)) shipment.ToProvinceName = request.ToProvinceName;
        if (request.ToDistrictId.HasValue) shipment.ToDistrictId = request.ToDistrictId;
        if (!string.IsNullOrWhiteSpace(request.ToWardCode)) shipment.ToWardCode = request.ToWardCode;
        shipment.UpdatedAt = DateTimeOffset.UtcNow;

        shipment.Events.Add(new ShipmentEvent
        {
            ShipmentId = shipment.Id,
            CarrierStatus = "address_updated",
            Status = (ShipmentEventStatus)shipment.Status,
            Description = $"Recipient address updated to: {request.ShippingAddress}, {request.RecipientName} ({request.RecipientPhone}).",
            OccurredAt = DateTimeOffset.UtcNow,
            WarehouseName = shipment.Warehouse?.Name
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(shipment);
    }

    public async Task<ShipmentDto> SyncStatusAsync(
        Guid shipmentId,
        Guid currentUserId = default,
        bool isAdminOrStaff = false,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .Include(s => s.Items)
            .Include(s => s.Events)
            .Include(s => s.Warehouse)
            .Include(s => s.Carrier)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment", shipmentId);

        ValidateShipmentAccess(shipment, currentUserId, isAdminOrStaff);

        return await SyncStatusCoreAsync(shipment, cancellationToken);
    }

    public async Task<ShipmentDto> SyncStatusByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .Include(s => s.Items)
            .Include(s => s.Events)
            .Include(s => s.Warehouse)
            .Include(s => s.Carrier)
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment for Order", orderId);

        return await SyncStatusCoreAsync(shipment, cancellationToken);
    }

    private async Task<ShipmentDto> SyncStatusCoreAsync(
        Domain.Entities.Shipment shipment,
        CancellationToken cancellationToken)
    {
        var trackingCode = shipment.GhnOrderCode ?? shipment.TrackingNumber;
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return MapToDto(shipment);
        }

        var gateway = _carrierResolver.Get(shipment.Carrier?.Code);
        var statusResult = await gateway.GetStatusAsync(trackingCode, cancellationToken);

        if (ShipmentStatusTransitions.IsForward(shipment.Status, statusResult.Status))
        {
            shipment.Status = statusResult.Status;
            shipment.UpdatedAt = DateTimeOffset.UtcNow;
            if (statusResult.Status == ShipmentStatus.Delivered)
            {
                shipment.DeliveredAt = statusResult.UpdatedAt;
            }

            shipment.Events.Add(new ShipmentEvent
            {
                ShipmentId = shipment.Id,
                CarrierStatus = statusResult.RawStatus,
                Status = (ShipmentEventStatus)statusResult.Status,
                Description = $"Status synchronized from carrier: {statusResult.Description}",
                OccurredAt = statusResult.UpdatedAt ?? DateTimeOffset.UtcNow,
                WarehouseName = shipment.Warehouse?.Name
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(shipment);
    }

    public async Task<PrintShippingLabelResponse> GetPrintLabelAsync(
        Guid shipmentId,
        string pageSize,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _dbContext.Shipments
            .Include(s => s.Warehouse)
            .Include(s => s.Carrier)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment", shipmentId);

        var isOwner = isAdmin ||
                      (currentUserId != Guid.Empty &&
                       ((shipment.Warehouse != null && shipment.Warehouse.SellerId == currentUserId) || shipment.CreatedBy == currentUserId));

        if (!isOwner)
        {
            throw new BusinessRuleValidationException("FORBIDDEN", "You do not have permission to print label for this shipment.", 403);
        }

        var trackingCode = shipment.GhnOrderCode ?? shipment.TrackingNumber;
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            throw new BusinessRuleValidationException(
                "SHIPMENT_TRACKING_NOT_FOUND",
                "Shipment does not have a valid carrier tracking number.",
                400);
        }

        var normalizedPageSize = pageSize?.Trim().ToUpperInvariant() switch
        {
            "80X80" or "80*80" => "80x80",
            "52X70" or "52*70" => "52x70",
            _ => "A5"
        };

        var gateway = _carrierResolver.Get(shipment.Carrier?.Code);
        // GHN print tokens expire after ~30 minutes, so fetch a fresh token
        var printToken = await gateway.GetPrintTokenAsync(trackingCode, cancellationToken);
        if (!string.IsNullOrWhiteSpace(printToken))
        {
            shipment.PrintToken = printToken;
            shipment.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var effectiveToken = printToken ?? shipment.PrintToken;
        var printUrl = gateway.BuildPrintLabelUrl(trackingCode, effectiveToken, normalizedPageSize);

        return new PrintShippingLabelResponse(
            shipment.Id,
            trackingCode,
            effectiveToken,
            printUrl,
            normalizedPageSize);
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

        var lastFailEvent = events?.LastOrDefault(e => !string.IsNullOrEmpty(e.Reason) || !string.IsNullOrEmpty(e.ReasonCode));

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
            s.BuyerId,
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
            events,
            lastFailEvent?.Reason,
            lastFailEvent?.ReasonCode,
            s.CreatedAt,
            s.UpdatedAt,
            s.SubOrderNo);
    }

    private readonly ShippingBehaviorOptions _behavior;

    private bool ShouldNotifyCommerce(ShipmentStatus status) =>
        Array.IndexOf(NotifyStatuses, status) >= 0 || (status == ShipmentStatus.Returning && _behavior.EmitReturningStatus);

    private static readonly ShipmentStatus[] NotifyStatuses =
    {
        ShipmentStatus.HandedOver,
        ShipmentStatus.InTransit,
        ShipmentStatus.Delivered,
        ShipmentStatus.DeliveryFailed,
        ShipmentStatus.Returned,
        ShipmentStatus.Cancelled
    };

    private static string MapCommerceStatus(ShipmentStatus status) => status switch
    {
        ShipmentStatus.DeliveryFailed => "DeliveryAttemptFailed",
        _ => status.ToString()
    };

    private static ShipmentEventStatus MapToEventStatus(ShipmentStatus status) => status switch
    {
        ShipmentStatus.ReadyToPick or ShipmentStatus.Packed => ShipmentEventStatus.Created,
        ShipmentStatus.Picking => ShipmentEventStatus.PickedUp,
        ShipmentStatus.InTransit or ShipmentStatus.HandedOver => ShipmentEventStatus.InTransit,
        ShipmentStatus.Delivered => ShipmentEventStatus.Delivered,
        ShipmentStatus.DeliveryFailed => ShipmentEventStatus.DeliveryFailed,
        ShipmentStatus.Returned => ShipmentEventStatus.Returned,
        ShipmentStatus.Returning => ShipmentEventStatus.Returning,
        _ => ShipmentEventStatus.InTransit
    };

    private static ShipmentStatus ParseTargetStatus(string status)
    {
        if (Enum.TryParse<ShipmentStatus>(status, ignoreCase: true, out var parsed))
            return parsed;

        return status.Trim().ToLowerInvariant() switch
        {
            "ready_to_pick" => ShipmentStatus.ReadyToPick,
            "picking" or "money_collect_picking" => ShipmentStatus.Picking,
            "storing" or "packed" => ShipmentStatus.Packed,
            "picked" => ShipmentStatus.HandedOver,
            "delivering" or "money_collect_delivering" or "transporting" or "sorting" => ShipmentStatus.InTransit,
            "delivered" => ShipmentStatus.Delivered,
            "delivery_fail" => ShipmentStatus.DeliveryFailed,
            "waiting_to_return" or "return" or "returning" or "return_transporting" or "return_sorting" => ShipmentStatus.Returning,
            "returned" or "return_to_origin" => ShipmentStatus.Returned,
            "cancel" or "cancelled" => ShipmentStatus.Cancelled,
            _ => throw new BusinessRuleValidationException("VAL_INVALID_STATUS", $"Status '{status}' is not recognized.", 400)
        };
    }

    public async Task<SimulateShipperStatusResponse> SimulateShipperStatusAsync(
        SimulateShipperStatusRequest request,
        string? trackingNumber = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var effectiveTracking = !string.IsNullOrWhiteSpace(trackingNumber)
            ? trackingNumber
            : request.TrackingNumber;

        if (string.IsNullOrWhiteSpace(effectiveTracking))
        {
            throw new BusinessRuleValidationException("VAL_INVALID_INPUT", "Tracking number or order identifier is required.", 400);
        }

        var trimmed = effectiveTracking.Trim();
        var trimmedLower = trimmed.ToLowerInvariant();
        var isGuid = Guid.TryParse(trimmed, out var orderGuid);

        var shipment = await _dbContext.Shipments
            .Include(s => s.Events)
            .Include(s => s.Warehouse)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trimmed ||
                                      (s.TrackingNumber != null && s.TrackingNumber.ToLower() == trimmedLower) ||
                                      s.GhnOrderCode == trimmed ||
                                      (s.GhnOrderCode != null && s.GhnOrderCode.ToLower() == trimmedLower) ||
                                      (s.SubOrderNo != null && s.SubOrderNo.ToLower() == trimmedLower) ||
                                      s.ClientOrderCode == trimmed ||
                                      (isGuid && (s.OrderId == orderGuid || s.Id == orderGuid)),
                                 cancellationToken)
            ?? throw new EntityNotFoundException("SHIPMENT_NOT_FOUND", "Shipment", effectiveTracking);

        var targetStatus = ParseTargetStatus(request.Status);
        var previousStatus = shipment.Status;

        if (!request.Force && !ShipmentStatusTransitions.IsForward(previousStatus, targetStatus))
        {
            throw new BusinessRuleValidationException(
                "INVALID_STATUS_TRANSITION",
                $"Cannot transition shipment from '{previousStatus}' to '{targetStatus}'. Life-cycle transitions must move forward.",
                400);
        }

        shipment.Status = targetStatus;
        shipment.UpdatedAt = DateTimeOffset.UtcNow;
        if (targetStatus == ShipmentStatus.Delivered)
        {
            shipment.DeliveredAt = DateTimeOffset.UtcNow;
        }

        var rawJson = JsonSerializer.Serialize(request);

        // Record ShipmentEvent
        var shipmentEvent = new ShipmentEvent
        {
            ShipmentId = shipment.Id,
            CarrierStatus = request.Status.ToLowerInvariant(),
            Status = MapToEventStatus(targetStatus),
            Description = request.Description ?? $"Simulated shipper update to {targetStatus}.",
            ExternalEventId = "simulated_shipper",
            OccurredAt = DateTimeOffset.UtcNow,
            RawPayload = rawJson,
            ReasonCode = request.ReasonCode,
            Reason = request.Reason,
            ShipperName = request.ShipperName ?? "Simulated Shipper",
            ShipperPhone = request.ShipperPhone ?? "0901234567",
            PodUrl = request.PodUrl,
            WarehouseName = shipment.Warehouse?.Name
        };
        _dbContext.ShipmentEvents.Add(shipmentEvent);

        // Create WebhookAuditLog for auditing & outbox
        var auditLog = new WebhookAuditLog
        {
            OrderCode = shipment.TrackingNumber ?? effectiveTracking,
            ClientOrderCode = shipment.OrderId.ToString(),
            EventType = "simulated_shipper_status",
            Status = request.Status,
            RawJson = rawJson,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsProcessed = true,
            ProcessMessage = $"Simulated shipper update from '{previousStatus}' to '{targetStatus}'."
        };

        string? commerceStatusNotified = null;
        var notify = ShouldNotifyCommerce(targetStatus);
        if (notify)
        {
            commerceStatusNotified = MapCommerceStatus(targetStatus);
            auditLog.NotifyOrderId = shipment.OrderId;
            auditLog.NotifyStatus = commerceStatusNotified;
            auditLog.NotifyTrackingNumber = shipment.TrackingNumber ?? effectiveTracking;
            auditLog.NotifyDescription = request.Description ?? $"Simulated status update: {targetStatus}";
            auditLog.CommerceNotifyAttempts = 0;
            auditLog.CommerceNotifyNextRetryAt = DateTimeOffset.UtcNow.Add(DeliveryNotificationOutbox.FirstRetryDelay);
        }

        _dbContext.WebhookAuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (notify && _outbox != null)
        {
            try
            {
                await _outbox.TrySendAsync(auditLog, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to dispatch outbox notification to CommerceService for Order {OrderId}", shipment.OrderId);
            }

            if (_notificationClient != null)
            {
                try
                {
                    var alertRequest = new DeliveryAlertRequest
                    {
                        OrderId = shipment.OrderId,
                        ShipmentId = shipment.Id,
                        ShopId = shipment.Warehouse?.SellerId ?? shipment.WarehouseId,
                        BuyerUserId = shipment.BuyerId ?? Guid.Empty,
                        SellerUserId = shipment.Warehouse?.SellerId ?? shipment.CreatedBy,
                        TrackingNumber = shipment.TrackingNumber ?? effectiveTracking,
                        Status = commerceStatusNotified!,
                        Note = request.Description ?? request.Reason,
                        ShipperName = request.ShipperName,
                        ShipperPhone = request.ShipperPhone,
                        PodUrl = request.PodUrl,
                        WarehouseName = shipment.Warehouse?.Name,
                        OccurredAt = DateTimeOffset.UtcNow
                    };
                    await _notificationClient.SendDeliveryAlertAsync(alertRequest, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to dispatch delivery alert to NotificationService for Order {OrderId}", shipment.OrderId);
                }
            }
        }

        return new SimulateShipperStatusResponse(
            TrackingNumber: shipment.TrackingNumber ?? effectiveTracking,
            OrderId: shipment.OrderId,
            PreviousStatus: previousStatus.ToString(),
            CurrentStatus: targetStatus.ToString(),
            CommerceStatusNotified: commerceStatusNotified,
            IsSuccess: true,
            Message: $"Shipment status successfully transitioned from '{previousStatus}' to '{targetStatus}'.");
    }
}
