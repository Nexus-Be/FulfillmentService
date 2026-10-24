namespace Fulfillment.Application.Services.Warehouse;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Interfaces;

public sealed class WarehouseService : IWarehouseService
{
    private readonly IFulfillmentDbContext _dbContext;

    public WarehouseService(IFulfillmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (warehouse == null) return null;

        return new WarehouseDto(
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            warehouse.Type.ToString(),
            warehouse.Address,
            warehouse.IsDisabled);
    }

    public async Task<IReadOnlyList<WarehouseDto>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        var warehouses = await _dbContext.Warehouses
            .AsNoTracking()
            .Where(w => !w.IsDisabled)
            .OrderBy(w => w.Name)
            .Select(w => new WarehouseDto(
                w.Id,
                w.Code,
                w.Name,
                w.Type.ToString(),
                w.Address,
                w.IsDisabled))
            .ToListAsync(cancellationToken);

        return warehouses;
    }
}
