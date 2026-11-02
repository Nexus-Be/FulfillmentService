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
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;

public sealed class WarehouseService : IWarehouseService
{
    private readonly IFulfillmentDbContext _dbContext;

    public WarehouseService(IFulfillmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request, Guid sellerId, CancellationToken cancellationToken = default)
    {
        if (sellerId != Guid.Empty)
        {
            var hasActiveWarehouse = await _dbContext.Warehouses
                .AnyAsync(w => w.SellerId == sellerId && w.IsActive, cancellationToken);

            if (hasActiveWarehouse)
            {
                throw new BusinessRuleValidationException("A warehouse has already been registered for this seller. Each seller is allowed only one warehouse.");
            }
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var isCodeTaken = await _dbContext.Warehouses
            .AnyAsync(w => w.Code == normalizedCode, cancellationToken);

        if (isCodeTaken)
        {
            throw new BusinessRuleValidationException($"Warehouse code '{normalizedCode}' is already in use.");
        }

        var warehouseType = Enum.TryParse<WarehouseType>(request.Type, ignoreCase: true, out var parsedType)
            ? parsedType
            : WarehouseType.SellerWarehouse;

        var warehouse = new Warehouse
        {
            Code = normalizedCode,
            Name = request.Name.Trim(),
            Type = warehouseType.ToString(),
            SellerId = sellerId != Guid.Empty ? sellerId : request.SellerId,
            Address = request.Address?.Trim(),
            IsActive = true
        };

        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(warehouse);
    }

    public async Task<InternalWarehouseResponse> CreateInternalAsync(InternalCreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var existingWarehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.SellerId == request.SellerId && w.IsActive, cancellationToken);

        if (existingWarehouse != null)
        {
            return new InternalWarehouseResponse(
                existingWarehouse.Id,
                request.SellerId,
                existingWarehouse.IsDisabled ? "INACTIVE" : "ACTIVE",
                existingWarehouse.CreatedAt);
        }

        var generatedCode = "WH_" + request.SellerId.ToString("N")[..8].ToUpperInvariant();
        var isCodeTaken = await _dbContext.Warehouses
            .AnyAsync(w => w.Code == generatedCode, cancellationToken);

        if (isCodeTaken)
        {
            generatedCode = "WH_" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        }

        var warehouse = new Warehouse
        {
            Code = generatedCode,
            Name = request.ShopName.Trim(),
            Type = WarehouseType.SellerWarehouse.ToString(),
            SellerId = request.SellerId,
            Address = request.PickupAddress.Trim(),
            IsActive = true
        };

        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new InternalWarehouseResponse(
            warehouse.Id,
            request.SellerId,
            "ACTIVE",
            warehouse.CreatedAt);
    }

    public async Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse", id);

        warehouse.Name = request.Name.Trim();
        warehouse.Address = request.Address?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(warehouse);
    }

    public async Task<WarehouseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse", id);

        return MapToDto(warehouse);
    }

    public async Task<WarehouseDto?> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.SellerId == sellerId && w.IsActive, cancellationToken);

        return warehouse == null ? null : MapToDto(warehouse);
    }

    public async Task<PagedData<WarehouseDto>> ListActiveAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (safePage, safePageSize) = PaginationConstants.Normalize(page, pageSize);

        var query = _dbContext.Warehouses
            .AsNoTracking()
            .Where(w => w.IsActive);

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
                !w.IsActive,
                w.SellerId))

            .ToListAsync(cancellationToken);

        return new PagedData<WarehouseDto>(items, PaginationMetadata.Create(safePage, safePageSize, totalCount));
    }

    public async Task<WarehouseDto> SetActiveStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse", id);

        warehouse.IsActive = isActive;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(warehouse);
    }

    private static WarehouseDto MapToDto(Warehouse w) =>
        new WarehouseDto(
            w.Id,
            w.Code,
            w.Name,
            w.Type.ToString(),
            w.Address,
            !w.IsActive,
            w.SellerId);

}
