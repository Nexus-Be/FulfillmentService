namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Contract for physical inventory management, stock adjustments, audit ledgers, and reservation lifecycles.
/// </summary>
public interface IInventoryService
{
    // Items
    Task<InventoryItemResponse> DeclareInventoryAsync(DeclareInventoryRequest request, CancellationToken cancellationToken = default);
    Task<AdjustInventoryResponse> AdjustInventoryAsync(AdjustInventoryRequest request, CancellationToken cancellationToken = default);
    Task<InventoryItemResponse?> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItemResponse>> GetByListSkuAsync(IReadOnlyList<Guid> skuIds, CancellationToken cancellationToken = default);

    // Ledgers
    Task<InventoryLedgerResponse> RecordLedgerEntryAsync(RecordLedgerRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryLedgerResponse>> GetLedgersBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default);

    // Reservations
    Task<InventoryReservationResponse> CreateReservationAsync(CreateReservationRequest request, CancellationToken cancellationToken = default);
    Task<InventoryReservationResponse> CommitReservationAsync(string referenceId, CancellationToken cancellationToken = default);
    Task<InventoryReservationResponse> ReleaseReservationAsync(string referenceId, string? reason = null, CancellationToken cancellationToken = default);
    Task<int> AutoExpireReservationsAsync(CancellationToken cancellationToken = default);
}

public interface IInventoryAppService : IInventoryService
{
}
