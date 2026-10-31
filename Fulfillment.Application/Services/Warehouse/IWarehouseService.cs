namespace Fulfillment.Application.Services.Warehouse;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Common.Models;

public interface IWarehouseService
{
    Task<WarehouseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedData<WarehouseDto>> ListActiveAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
