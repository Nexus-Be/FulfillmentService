namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IInventoryService
{
    Task<InventoryItemResponse> InitializeInventoryAsync(InitializeInventoryRequest request, CancellationToken cancellationToken = default);
    Task<InventoryItemResponse> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItemResponse>> GetByListSkuAsync(IReadOnlyList<Guid> skuIds, CancellationToken cancellationToken = default);
}
