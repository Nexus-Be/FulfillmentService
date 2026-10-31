namespace Fulfillment.Application.Services.Inventory;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IInventoryService
{
    Task<InventoryItemResponse> InitializeInventoryAsync(InitializeInventoryRequest request, CancellationToken cancellationToken = default);
    Task<InventoryItemResponse> GetBySkuIdAsync(Guid skuId, CancellationToken cancellationToken = default);
}
