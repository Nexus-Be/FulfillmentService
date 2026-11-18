namespace Fulfillment.Api.Controllers.Internal;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Shipping;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/internal/shipping")]
public sealed class InternalShippingController : ControllerBase
{
    private readonly IShipmentService _shipmentService;
    private readonly IShippingLocationService _locationService;

    public InternalShippingController(
        IShipmentService shipmentService,
        IShippingLocationService locationService)
    {
        _shipmentService = shipmentService ?? throw new ArgumentNullException(nameof(shipmentService));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
    }

    [HttpPost("quote")]
    public async Task<ActionResult<ShippingQuoteResponse>> Quote(
        [FromBody] ShopShippingQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _shipmentService.CalculateShippingFeeAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Validates hierarchical consistency of province, district, and ward against GHN master data.
    /// Used by UserService when saving buyer addresses and by WarehouseService when creating seller warehouses.
    /// </summary>
    [HttpPost("locations/validate")]
    public async Task<ActionResult<ShippingLocationValidationResponse>> ValidateLocation(
        [FromBody] ValidateShippingLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _locationService.ValidateLocationAsync(request, cancellationToken);
        if (!result.IsValid)
        {
            return BadRequest(new
            {
                errorCode = "VAL_INVALID_LOCATION",
                message = result.Message ?? "Specified province, district, and ward hierarchy is invalid.",
                data = result
            });
        }

        return Ok(result);
    }

    [HttpGet("locations/provinces")]
    public async Task<ActionResult<IReadOnlyList<ProvinceDto>>> GetProvinces(CancellationToken cancellationToken)
    {
        var provinces = await _locationService.GetProvincesAsync(cancellationToken);
        return Ok(provinces);
    }

    [HttpGet("locations/districts")]
    public async Task<ActionResult<IReadOnlyList<DistrictDto>>> GetDistricts(
        [FromQuery] int provinceId,
        CancellationToken cancellationToken)
    {
        if (provinceId <= 0)
        {
            return BadRequest(new { errorCode = "VAL_INVALID_INPUT", message = "Province ID must be greater than zero." });
        }

        var districts = await _locationService.GetDistrictsAsync(provinceId, cancellationToken);
        return Ok(districts);
    }

    [HttpGet("locations/wards")]
    public async Task<ActionResult<IReadOnlyList<WardDto>>> GetWards(
        [FromQuery] int districtId,
        CancellationToken cancellationToken)
    {
        if (districtId <= 0)
        {
            return BadRequest(new { errorCode = "VAL_INVALID_INPUT", message = "District ID must be greater than zero." });
        }

        var wards = await _locationService.GetWardsAsync(districtId, cancellationToken);
        return Ok(wards);
    }

    [HttpGet("services")]
    [HttpPost("services")]
    public async Task<ActionResult<IReadOnlyList<AvailableShippingServiceDto>>> GetServices(
        [FromQuery] int fromDistrictId,
        [FromQuery] int toDistrictId,
        CancellationToken cancellationToken)
    {
        var services = await _locationService.GetAvailableServicesAsync(fromDistrictId, toDistrictId, cancellationToken);
        return Ok(services);
    }
}
