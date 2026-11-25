namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Exceptions;

/// <summary>
/// A stand-in for the GHN lists in tests: every chain is confirmed, unless it is told otherwise. It counts the checks it was asked for.
/// </summary>
public sealed class AcceptingLocations : IShippingLocationService
{
    public bool SourceDown { get; set; }
    public bool Reject { get; set; }
    public int Checks { get; private set; }
    public List<(int Province, int District, string Ward)> Asked { get; } = new();

    public Task<LocationList<ProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new LocationList<ProvinceDto>(new[] { new ProvinceDto(202, "Ho Chi Minh") }, LocationSources.Ghn, DateTimeOffset.UtcNow));

    public Task<LocationList<DistrictDto>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new LocationList<DistrictDto>(Array.Empty<DistrictDto>(), LocationSources.Ghn, DateTimeOffset.UtcNow));

    public Task<LocationList<WardDto>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new LocationList<WardDto>(Array.Empty<WardDto>(), LocationSources.Ghn, DateTimeOffset.UtcNow));

    public Task<IReadOnlyList<AvailableShippingServiceDto>> GetAvailableServicesAsync(int fromDistrictId, int toDistrictId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AvailableShippingServiceDto>>(Array.Empty<AvailableShippingServiceDto>());

    public Task<ShippingLocationValidationResponse> ValidateLocationAsync(ValidateShippingLocationRequest request, CancellationToken cancellationToken = default)
    {
        Checks++;
        Asked.Add((request.ProvinceId, request.DistrictId, request.WardCode));
        if (SourceDown)
        {
            throw new BusinessRuleValidationException("LOCATION_SOURCE_UNAVAILABLE", "Shipping locations are temporarily unavailable.", 503);
        }

        return Task.FromResult(Reject
            ? new ShippingLocationValidationResponse(false, request.ProvinceId, null, request.DistrictId, null, request.WardCode, null, "The location hierarchy is not valid.")
            : new ShippingLocationValidationResponse(true, request.ProvinceId, "Ho Chi Minh", request.DistrictId, "Quan", request.WardCode, "Phuong"));
    }
}
