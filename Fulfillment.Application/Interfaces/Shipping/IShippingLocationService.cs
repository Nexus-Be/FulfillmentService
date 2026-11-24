namespace Fulfillment.Application.Interfaces.Shipping;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Services.Shipping;

public interface IShippingLocationService
{
    Task<LocationList<ProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default);

    Task<LocationList<DistrictDto>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default);

    Task<LocationList<WardDto>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailableShippingServiceDto>> GetAvailableServicesAsync(int fromDistrictId, int toDistrictId, CancellationToken cancellationToken = default);

    Task<ShippingLocationValidationResponse> ValidateLocationAsync(ValidateShippingLocationRequest request, CancellationToken cancellationToken = default);
}
