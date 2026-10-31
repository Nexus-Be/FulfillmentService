namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;

public sealed partial class InventoryAppService
{
    public async Task<InventoryItemResponse> DeclareInventoryAsync(DeclareInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.SkuId == request.SkuId && i.WarehouseId == request.WarehouseId, cancellationToken);

        if (existing != null)
        {
            throw new BusinessRuleValidationException($"Inventory for SKU '{request.SkuId}' already declared in warehouse '{request.WarehouseId}'.");
        }

        var item = new InventoryItem
        {
            SkuId = request.SkuId,
            WarehouseId = request.WarehouseId,
            TotalQuantity = request.Quantity,
            ReservedQuantity = 0,
            UnavailableQuantity = 0,
            AvailableQuantity = request.Quantity,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.InventoryItems.Add(item);

        if (request.Quantity > 0)
        {
            var ledger = new InventoryLedger
            {
                InventoryItemId = item.Id,
                MovementType = InventoryMovementType.Initial.ToString().ToUpperInvariant(),
                Quantity = request.Quantity,
                ReferenceType = "MANUAL",
                Note = request.Note ?? "Initial inventory declaration.",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.InventoryLedgers.Add(ledger);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(item);
    }

    public async Task<AdjustInventoryResponse> AdjustInventoryAsync(AdjustInventoryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Type == InventoryAdjustmentType.Intake && request.Quantity <= 0)
        {
            throw new BusinessRuleValidationException("Quantity for INTAKE must be greater than zero.");
        }

        if (request.Type == InventoryAdjustmentType.Adjust && request.Quantity == 0)
        {
            throw new BusinessRuleValidationException("Quantity for ADJUST cannot be zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BusinessRuleValidationException("Reason for adjustment is required.");
        }

        var item = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.SkuId == request.SkuId, cancellationToken)
            ?? throw new EntityNotFoundException("InventoryItem for SKU", request.SkuId);

        var newTotal = item.TotalQuantity + request.Quantity;

        if (newTotal < 0)
        {
            throw new BusinessRuleValidationException("Total quantity cannot be negative.");
        }

        if (newTotal < item.ReservedQuantity)
        {
            throw new BusinessRuleValidationException("Total quantity cannot be less than reserved quantity.");
        }

        item.TotalQuantity = newTotal;
        item.AvailableQuantity = item.TotalQuantity - item.ReservedQuantity - item.UnavailableQuantity;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        var movementType = request.Type switch
        {
            InventoryAdjustmentType.Intake => InventoryMovementType.Intake.ToString().ToUpperInvariant(),
            InventoryAdjustmentType.Adjust => InventoryMovementType.Adjust.ToString().ToUpperInvariant(),
            _ => throw new BusinessRuleValidationException("Unsupported adjustment type.")
        };

        var ledger = new InventoryLedger
        {
            InventoryItemId = item.Id,
            MovementType = movementType,
            Quantity = request.Quantity,
            ReferenceType = "MANUAL",
            Note = request.Reason,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.InventoryLedgers.Add(ledger);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdjustInventoryResponse(
            item.SkuId,
            item.TotalQuantity,
            item.ReservedQuantity,
            item.AvailableQuantity.Value,
            ledger.Id);
    }

    public async Task<InventoryItemResponse?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.SkuId == skuId, cancellationToken);

        return item == null ? null : MapToResponse(item);
    }

    public async Task<IReadOnlyList<InventoryItemResponse>> GetByListSkuAsync(IReadOnlyList<Guid> skuIds, CancellationToken cancellationToken = default)
    {
        if (skuIds == null || skuIds.Count == 0)
        {
            return Array.Empty<InventoryItemResponse>();
        }

        var distinctSkuIds = skuIds.Distinct().ToList();

        var items = await _dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => distinctSkuIds.Contains(i.SkuId))
            .ToListAsync(cancellationToken);

        return items.Select(MapToResponse).ToList();
    }

    private static InventoryItemResponse MapToResponse(InventoryItem item) =>
        new InventoryItemResponse(
            item.Id,
            item.SkuId,
            item.WarehouseId,
            item.TotalQuantity,
            item.ReservedQuantity,
            item.UnavailableQuantity,
            item.AvailableQuantity ?? (item.TotalQuantity - item.ReservedQuantity - item.UnavailableQuantity),
            item.UpdatedAt);
}
