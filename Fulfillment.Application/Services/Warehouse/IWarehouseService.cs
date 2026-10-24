namespace Fulfillment.Application.Services.Warehouse;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IWarehouseService
{
    Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseDto>> ListActiveAsync(CancellationToken cancellationToken = default);
}
