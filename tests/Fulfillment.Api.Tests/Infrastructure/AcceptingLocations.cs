namespace Fulfillment.Api.Tests.Infrastructure;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;

/// <summary>Stands in for the GHN lists in API tests that are not about the lists: every province, district and ward chain is confirmed.</summary>
public sealed class AcceptingLocations : IShippingLocationService
{
    public Task<LocationList<ProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new LocationList<ProvinceDto>(Array.Empty<ProvinceDto>(), LocationSources.Ghn, DateTimeOffset.UtcNow));

    public Task<LocationList<DistrictDto>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new LocationList<DistrictDto>(Array.Empty<DistrictDto>(), LocationSources.Ghn, DateTimeOffset.UtcNow));

    public Task<LocationList<WardDto>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new LocationList<WardDto>(Array.Empty<WardDto>(), LocationSources.Ghn, DateTimeOffset.UtcNow));

    public Task<IReadOnlyList<AvailableShippingServiceDto>> GetAvailableServicesAsync(int fromDistrictId, int toDistrictId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AvailableShippingServiceDto>>(Array.Empty<AvailableShippingServiceDto>());

    public Task<ShippingLocationValidationResponse> ValidateLocationAsync(ValidateShippingLocationRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ShippingLocationValidationResponse(true, request.ProvinceId, "Province", request.DistrictId, "District", request.WardCode, "Ward"));
}
