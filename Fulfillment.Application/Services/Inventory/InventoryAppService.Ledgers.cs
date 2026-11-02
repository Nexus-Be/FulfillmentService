namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Exceptions;

public sealed partial class InventoryAppService
{
    public async Task<InventoryLedgerResponse> RecordLedgerEntryAsync(RecordLedgerRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(i => i.SkuId == request.SkuId, cancellationToken)
            ?? throw new EntityNotFoundException("InventoryItem for SKU", request.SkuId);

        var ledger = InventoryLedger.Create(
            item.Id,
            request.MovementType,
            request.Quantity,
            referenceType: request.ReferenceType,
            referenceId: request.ReferenceId,
            note: request.Note,
            reservationId: request.ReservationId);

        _dbContext.InventoryLedgers.Add(ledger);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new InventoryLedgerResponse(
            ledger.Id,
            request.SkuId,
            ledger.MovementType,
            ledger.Quantity,
            ledger.ReservationId,
            ledger.ReferenceType,
            ledger.ReferenceId,
            ledger.Note,
            ledger.CreatedAt);
    }

    public async Task<IReadOnlyList<InventoryLedgerResponse>> GetLedgersBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.SkuId == skuId, cancellationToken)
            ?? throw new EntityNotFoundException("InventoryItem for SKU", skuId);

        var ledgers = await _dbContext.InventoryLedgers
            .AsNoTracking()
            .Where(l => l.InventoryItemId == item.Id)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return ledgers.Select(l => new InventoryLedgerResponse(
            l.Id,
            skuId,
            l.MovementType,
            l.Quantity,
            l.ReservationId,
            l.ReferenceType,
            l.ReferenceId,
            l.Note,
            l.CreatedAt)).ToList();
    }
}
