namespace Fulfillment.Application.Services.Warehouse;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Common;
using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Interfaces;
using Fulfillment.Domain.Exceptions;

public sealed class WarehouseService : IWarehouseService
{
    private readonly IFulfillmentDbContext _dbContext;

    public WarehouseService(IFulfillmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WarehouseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse", id);

        return new WarehouseDto(
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            warehouse.Type.ToString(),
            warehouse.Address,
            warehouse.IsDisabled);
    }

    public async Task<PagedData<WarehouseDto>> ListActiveAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (safePage, safePageSize) = PaginationConstants.Normalize(page, pageSize);

        var query = _dbContext.Warehouses
            .AsNoTracking()
            .Where(w => !w.IsDisabled);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(w => w.Name)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(w => new WarehouseDto(
                w.Id,
                w.Code,
                w.Name,
                w.Type.ToString(),
                w.Address,
                w.IsDisabled))
            .ToListAsync(cancellationToken);

        return new PagedData<WarehouseDto>(items, PaginationMetadata.Create(safePage, safePageSize, totalCount));
    }
}
