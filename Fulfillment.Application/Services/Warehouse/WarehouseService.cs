namespace Fulfillment.Application.Services.Warehouse;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Interfaces;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Exceptions;

public sealed class WarehouseService : IWarehouseService
{
    private readonly IFulfillmentDbContext _dbContext;

    public WarehouseService(IFulfillmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto, CancellationToken cancellationToken = default)
    {
        var warehouse = new Warehouse
        {
            Code = dto.Code,
            Name = dto.Name,
            Address = dto.Address,
            SellerId = dto.SellerId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(warehouse);
    }

    public async Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseDto dto, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new EntityNotFoundException("Warehouse", id);

        warehouse.Name = dto.Name;
        warehouse.Address = dto.Address;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(warehouse);
    }

    public async Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        return warehouse == null ? null : MapToDto(warehouse);
    }

    public async Task<WarehouseDto> SetActiveStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new EntityNotFoundException("Warehouse", id);

        warehouse.IsActive = isActive;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(warehouse);
    }

    public async Task<IReadOnlyList<WarehouseDto>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        var warehouses = await _dbContext.Warehouses
            .AsNoTracking()
            .Where(w => w.SellerId == sellerId)
            .OrderBy(w => w.Name)
            .Select(w => MapToDto(w))
            .ToListAsync(cancellationToken);

        return warehouses;
    }

    public async Task<IReadOnlyList<WarehouseDto>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        var warehouses = await _dbContext.Warehouses
            .AsNoTracking()
            .Where(w => w.IsActive)
            .OrderBy(w => w.Name)
            .Select(w => MapToDto(w))
            .ToListAsync(cancellationToken);

        return warehouses;
    }

    private static WarehouseDto MapToDto(Warehouse w) =>
        new WarehouseDto(
            w.Id,
            w.Code,
            w.Name,
            w.Address,
            w.IsActive,
            w.SellerId,
            w.CreatedAt);
}
