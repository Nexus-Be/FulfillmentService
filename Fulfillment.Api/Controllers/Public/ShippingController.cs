namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Api.Extensions;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/shipping")]
public sealed class ShippingController : ControllerBase
{
    private readonly IShippingLocationService _locationService;
    private readonly IShipmentService _shipmentService;

    public ShippingController(
        IShippingLocationService locationService,
        IShipmentService shipmentService)
    {
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _shipmentService = shipmentService ?? throw new ArgumentNullException(nameof(shipmentService));
    }

    /// <summary>
    /// Retrieves all administrative provinces for shipping address configuration (cached 24h).
    /// </summary>
    [HttpGet("locations/provinces")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ProvinceDto>>> GetProvinces(CancellationToken cancellationToken)
    {
        var provinces = await _locationService.GetProvincesAsync(cancellationToken);
        return Ok(provinces);
    }

    /// <summary>
    /// Retrieves administrative districts within a province (cached 24h).
    /// </summary>
    [HttpGet("locations/districts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDistricts(
        [FromQuery] int provinceId,
        CancellationToken cancellationToken)
    {
        if (provinceId <= 0)
        {
            return this.BadRequestError("VAL_INVALID_INPUT", "Province ID must be greater than zero.");
        }

        var districts = await _locationService.GetDistrictsAsync(provinceId, cancellationToken);
        return Ok(districts);
    }

    /// <summary>
    /// Retrieves administrative wards within a district (cached 24h).
    /// </summary>
    [HttpGet("locations/wards")]
    [AllowAnonymous]
    public async Task<IActionResult> GetWards(
        [FromQuery] int districtId,
        CancellationToken cancellationToken)
    {
        if (districtId <= 0)
        {
            return this.BadRequestError("VAL_INVALID_INPUT", "District ID must be greater than zero.");
        }

        var wards = await _locationService.GetWardsAsync(districtId, cancellationToken);
        return Ok(wards);
    }

    /// <summary>
    /// Retrieves available shipping service packages between two districts.
    /// </summary>
    [HttpGet("services")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<AvailableShippingServiceDto>>> GetServices(
        [FromQuery] int fromDistrictId,
        [FromQuery] int toDistrictId,
        CancellationToken cancellationToken)
    {
        var services = await _locationService.GetAvailableServicesAsync(fromDistrictId, toDistrictId, cancellationToken);
        return Ok(services);
    }

    /// <summary>
    /// Calculates estimated shipping fee for cart/PDP preview without creating draft orders.
    /// </summary>
    [HttpPost("quote")]
    [AllowAnonymous]
    public async Task<ActionResult<ShippingQuoteResponse>> Quote(
        [FromBody] ShopShippingQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _shipmentService.CalculateShippingFeeAsync(request, cancellationToken);
        return Ok(result);
    }
}
