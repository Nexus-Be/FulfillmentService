namespace Fulfillment.Application.Services.Warehouse;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Common;
using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Exceptions;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Exceptions;

public class WarehouseService : IWarehouseService
{
    private readonly IFulfillmentDbContext _dbContext;
    private readonly IShippingLocationService _locations;

    public WarehouseService(IFulfillmentDbContext dbContext, IShippingLocationService locations)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    /// <summary>
    /// A warehouse may be kept without an address (a placeholder that cannot ship yet), but an address that is given must be the whole
    /// province, district and ward chain and must be confirmed by the GHN lists before it is saved. Without a trusted GHN source the call is
    /// 503 LOCATION_SOURCE_UNAVAILABLE and nothing is saved. Returns the confirmation, or null for a placeholder.
    /// </summary>
    private async Task<ShippingLocationValidationResponse?> EnsureVerifiedAddressAsync(int? provinceId, int? districtId, string? wardCode, CancellationToken cancellationToken)
    {
        var ward = wardCode?.Trim();
        if (!provinceId.HasValue && !districtId.HasValue && string.IsNullOrEmpty(ward))
        {
            return null;
        }

        if (!provinceId.HasValue || !districtId.HasValue || string.IsNullOrEmpty(ward))
        {
            throw new BusinessRuleValidationException("WAREHOUSE_ADDRESS_INCOMPLETE", "The province, district and ward of a warehouse address must be given together.", 400);
        }

        var result = await _locations.ValidateLocationAsync(new ValidateShippingLocationRequest(provinceId.Value, districtId.Value, ward), cancellationToken);
        if (!result.IsValid)
        {
            throw new BusinessRuleValidationException("VAL_INVALID_LOCATION", result.Message ?? "The province, district and ward do not match.", 400);
        }

        return result;
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request, Guid sellerId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var effectiveSellerId = sellerId != Guid.Empty ? sellerId : request.SellerId;
        if (effectiveSellerId.HasValue && effectiveSellerId.Value != Guid.Empty)
        {
            var alreadyExists = await _dbContext.Warehouses
                .AnyAsync(w => w.SellerId == effectiveSellerId.Value && w.IsActive, cancellationToken);
            if (alreadyExists)
            {
                throw new BusinessRuleValidationException("SELLER_ALREADY_HAS_WAREHOUSE", "A warehouse is already registered for this seller account.", 400);
            }
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var isCodeTaken = await _dbContext.Warehouses
            .AnyAsync(w => w.Code == normalizedCode, cancellationToken);

        if (isCodeTaken)
        {
            throw new BusinessRuleValidationException("WAREHOUSE_CODE_TAKEN", $"Warehouse code '{normalizedCode}' is already in use.", 400);
        }

        var verified = await EnsureVerifiedAddressAsync(request.ProvinceId, request.DistrictId, request.WardCode, cancellationToken);

        var warehouse = new Warehouse
        {
            Code = normalizedCode,
            Name = request.Name.Trim(),
            SellerId = effectiveSellerId,
            Address = request.Address?.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            ProvinceName = request.ProvinceName?.Trim() ?? verified?.ProvinceName,
            ProvinceId = request.ProvinceId,
            DistrictId = request.DistrictId,
            WardCode = request.WardCode?.Trim(),
            GhnShopId = request.GhnShopId,
            IsActive = true
        };

        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(warehouse);
    }

    public async Task<InternalWarehouseResponse> CreateInternalAsync(InternalCreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

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

        var verifiedAddress = await EnsureVerifiedAddressAsync(request.ProvinceId, request.DistrictId, request.WardCode, cancellationToken);

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
            SellerId = effectiveSellerId,
            Address = request.PickupAddress.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            ProvinceName = request.ProvinceName?.Trim() ?? verifiedAddress?.ProvinceName,
            ProvinceId = request.ProvinceId,
            DistrictId = request.DistrictId,
            WardCode = request.WardCode?.Trim(),
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
        ArgumentNullException.ThrowIfNull(request);

        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new EntityNotFoundException("WAREHOUSE_NOT_FOUND", "Warehouse", id);

        if (!isAdmin && currentUserId != Guid.Empty && warehouse.SellerId != currentUserId)
        {
            throw new BusinessRuleValidationException("FORBIDDEN", "You do not have permission to update this warehouse.", 403);
        }

        // A change of name or phone is never held up by the address lists; a change of the province, district or ward is checked as a whole chain.
        var addressChanged = request.ProvinceId.HasValue || request.DistrictId.HasValue || request.WardCode != null;
        ShippingLocationValidationResponse? verifiedChange = null;
        if (addressChanged)
        {
            verifiedChange = await EnsureVerifiedAddressAsync(
                request.ProvinceId ?? warehouse.ProvinceId,
                request.DistrictId ?? warehouse.DistrictId,
                request.WardCode ?? warehouse.WardCode,
                cancellationToken);
        }

        warehouse.Name = request.Name.Trim();
        if (request.Address != null) warehouse.Address = request.Address.Trim();
        if (request.ContactPhone != null) warehouse.ContactPhone = request.ContactPhone.Trim();
        if (request.ProvinceName != null) warehouse.ProvinceName = request.ProvinceName.Trim();
        else if (verifiedChange?.ProvinceName != null) warehouse.ProvinceName = verifiedChange.ProvinceName;
        if (request.ProvinceId.HasValue) warehouse.ProvinceId = request.ProvinceId.Value;
        if (request.DistrictId.HasValue) warehouse.DistrictId = request.DistrictId.Value;
        if (request.WardCode != null) warehouse.WardCode = request.WardCode.Trim();
        if (request.GhnShopId.HasValue) warehouse.GhnShopId = request.GhnShopId.Value;

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

        if (warehouse == null)
        {
            var seedUserId = Guid.Parse("cccccccc-3333-3333-3333-333333333333");
            var seedProfileId = Guid.Parse("cccccccc-3333-3333-3333-333333333334");

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

    public async Task<WarehouseDto?> GetForSellerAsync(
        Guid userId,
        Guid? sellerProfileId = null,
        string? shopName = null,
        string? address = null,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.SellerId == userId && w.IsActive, cancellationToken);
        if (warehouse is not null)
        {
            return MapToDto(warehouse);
        }

        // A warehouse registered under the seller profile ID (older data) is still the seller's own.
        if (sellerProfileId.HasValue && sellerProfileId.Value != Guid.Empty)
        {
            var legacy = await _dbContext.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.SellerId == sellerProfileId.Value && w.IsActive, cancellationToken);
            if (legacy is not null)
            {
                return MapToDto(legacy);
            }
        }

        return null;
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
        ArgumentNullException.ThrowIfNull(request);

        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.SellerId == sellerId && w.IsActive, cancellationToken);

        if (warehouse == null)
        {
            return null;
        }

        var addressChangedBySeller = request.ProvinceId.HasValue || request.DistrictId.HasValue || !string.IsNullOrWhiteSpace(request.WardCode);
        ShippingLocationValidationResponse? verifiedBySeller = null;
        if (addressChangedBySeller)
        {
            verifiedBySeller = await EnsureVerifiedAddressAsync(
                request.ProvinceId ?? warehouse.ProvinceId,
                request.DistrictId ?? warehouse.DistrictId,
                !string.IsNullOrWhiteSpace(request.WardCode) ? request.WardCode : warehouse.WardCode,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.ShopName))
        {
            warehouse.Name = request.ShopName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.PickupAddress))
        {
            warehouse.Address = request.PickupAddress.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.ContactPhone))
        {
            warehouse.ContactPhone = request.ContactPhone.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.ProvinceName))
        {
            warehouse.ProvinceName = request.ProvinceName.Trim();
        }
        else if (verifiedBySeller?.ProvinceName != null)
        {
            warehouse.ProvinceName = verifiedBySeller.ProvinceName;
        }

        if (request.ProvinceId.HasValue)
        {
            warehouse.ProvinceId = request.ProvinceId.Value;
        }

        if (request.DistrictId.HasValue)
        {
            warehouse.DistrictId = request.DistrictId.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.WardCode))
        {
            warehouse.WardCode = request.WardCode.Trim();
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
                w.Address,
                w.ContactPhone,
                w.ProvinceName,
                w.DistrictId,
                w.WardCode,
                w.GhnShopId,
                !w.IsActive,
                w.SellerId,
                w.ProvinceId))
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
            w.Address,
            w.ContactPhone,
            w.ProvinceName,
            w.DistrictId,
            w.WardCode,
            w.GhnShopId,
            !w.IsActive,
            w.SellerId,
            w.ProvinceId);
}
