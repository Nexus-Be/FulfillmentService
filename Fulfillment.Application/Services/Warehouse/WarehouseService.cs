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
            .FirstOrDefaultAsync(
                w => w.IsActive && (w.SellerId == request.SellerId || (request.UserId != Guid.Empty && w.SellerId == request.UserId)),
                cancellationToken);

        if (existingWarehouse != null)
        {
            return new InternalWarehouseResponse(
                existingWarehouse.Id,
                request.SellerId,
                existingWarehouse.IsDisabled ? "INACTIVE" : "ACTIVE",
                existingWarehouse.CreatedAt);
        }

        var effectiveSellerId = request.UserId != Guid.Empty ? request.UserId : request.SellerId;
        var generatedCode = "WH_" + effectiveSellerId.ToString("N")[..8].ToUpperInvariant();
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
            SellerId = effectiveSellerId,
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

    public async Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse", id);

        if (!isAdmin && currentUserId != Guid.Empty && warehouse.SellerId != currentUserId)
        {
            throw new BusinessRuleValidationException("FORBIDDEN", "You do not have permission to update this warehouse.", 403);
        }

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

        // Hỗ trợ tra cứu chéo giữa SeedSellerUserId và SeedSellerProfileId nếu là tài khoản seed
        if (warehouse == null)
        {
            var seedUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var seedProfileId = Guid.Parse("33333333-3333-3333-3333-333333333334");

            if (sellerId == seedUserId)
            {
                warehouse = await _dbContext.Warehouses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.SellerId == seedProfileId && w.IsActive, cancellationToken);
            }
            else if (sellerId == seedProfileId)
            {
                warehouse = await _dbContext.Warehouses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.SellerId == seedUserId && w.IsActive, cancellationToken);
            }
        }

        return warehouse == null ? null : MapToDto(warehouse);
    }

    public async Task<WarehouseDto?> GetOrProvisionForSellerAsync(
        Guid userId,
        Guid? sellerProfileId = null,
        string? shopName = null,
        string? address = null,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.SellerId == userId && w.IsActive, cancellationToken);

        if (warehouse == null && sellerProfileId.HasValue && sellerProfileId.Value != Guid.Empty)
        {
            var profileWarehouse = await _dbContext.Warehouses
                .FirstOrDefaultAsync(w => w.SellerId == sellerProfileId.Value && w.IsActive, cancellationToken);

            if (profileWarehouse != null)
            {
                profileWarehouse.SellerId = userId;
                if (!string.IsNullOrWhiteSpace(shopName) && profileWarehouse.Name != shopName.Trim())
                {
                    profileWarehouse.Name = shopName.Trim();
                }
                if (!string.IsNullOrWhiteSpace(address) && profileWarehouse.Address != address.Trim())
                {
                    profileWarehouse.Address = address.Trim();
                }
                await _dbContext.SaveChangesAsync(cancellationToken);
                return MapToDto(profileWarehouse);
            }
        }

        if (warehouse == null)
        {
            var generatedCode = "WH_" + userId.ToString("N")[..8].ToUpperInvariant();
            var isCodeTaken = await _dbContext.Warehouses.AnyAsync(w => w.Code == generatedCode, cancellationToken);
            if (isCodeTaken)
            {
                generatedCode = "WH_" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            }

            var resolvedName = !string.IsNullOrWhiteSpace(shopName)
                ? shopName.Trim()
                : $"Kho Riêng Gian Hàng ({generatedCode})";

            var resolvedAddress = !string.IsNullOrWhiteSpace(address)
                ? address.Trim()
                : "Kho hàng riêng của Seller";

            warehouse = new Warehouse
            {
                Code = generatedCode,
                Name = resolvedName,
                Type = WarehouseType.SellerWarehouse.ToString(),
                SellerId = userId,
                Address = resolvedAddress,
                IsActive = true
            };

            _dbContext.Warehouses.Add(warehouse);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(warehouse);
    }

    public async Task<WarehouseDto?> UpdateBySellerIdAsync(
        Guid sellerId,
        InternalUpdateSellerWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.SellerId == sellerId && w.IsActive, cancellationToken);

        if (warehouse == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.ShopName))
        {
            warehouse.Name = request.ShopName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.PickupAddress))
        {
            warehouse.Address = request.PickupAddress.Trim();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(warehouse);
    }

    public async Task<bool> DeleteBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.SellerId == sellerId, cancellationToken);

        if (warehouse == null)
        {
            return false;
        }

        // Đánh dấu tắt hoặc xóa khỏi DB khi UserService rollback
        warehouse.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
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
                w.Type,
                w.Address,
                !w.IsActive,
                w.SellerId))

            .ToListAsync(cancellationToken);

        return new PagedData<WarehouseDto>(items, PaginationMetadata.Create(safePage, safePageSize, totalCount));
    }

    public async Task<WarehouseDto> SetActiveStatusAsync(Guid id, bool isActive, Guid currentUserId = default, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse", id);

        if (!isAdmin && currentUserId != Guid.Empty && warehouse.SellerId != currentUserId)
        {
            throw new BusinessRuleValidationException("FORBIDDEN", "You do not have permission to change status of this warehouse.", 403);
        }

        warehouse.IsActive = isActive;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(warehouse);
    }

    private static WarehouseDto MapToDto(Warehouse w) =>
        new WarehouseDto(
            w.Id,
            w.Code,
            w.Name,
            w.Type,
            w.Address,
            !w.IsActive,
            w.SellerId);

}
