namespace Fulfillment.Api.Controllers.Internal;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Fulfillment.Application.Services.Warehouse;

[ApiController]
[Route("api/v1/internal/warehouses")]
public sealed class InternalWarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public InternalWarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpPost]
    public async Task<ActionResult<InternalWarehouseResponse>> CreateInternal(
        [FromBody] InternalCreateWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _warehouseService.CreateInternalAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("seller/{sellerId:guid}")]
    public async Task<ActionResult<WarehouseDto>> GetBySellerId(
        [FromRoute] Guid sellerId,
        CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.GetBySellerIdAsync(sellerId, cancellationToken);
        if (warehouse == null)
        {
            return NotFound(new { errorCode = "WAREHOUSE_NOT_FOUND", message = $"No active warehouse found for seller '{sellerId}'." });
        }
        return Ok(warehouse);
    }

    [HttpPut("seller/{sellerId:guid}")]
    public async Task<ActionResult<WarehouseDto>> UpdateBySellerId(
        [FromRoute] Guid sellerId,
        [FromBody] InternalUpdateSellerWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.UpdateBySellerIdAsync(sellerId, request, cancellationToken);
        if (warehouse == null)
        {
            return NotFound(new { errorCode = "WAREHOUSE_NOT_FOUND", message = $"No active warehouse found for seller '{sellerId}'." });
        }
        return Ok(warehouse);
    }

    [HttpDelete("seller/{sellerId:guid}")]
    public async Task<IActionResult> DeleteBySellerId(
        [FromRoute] Guid sellerId,
        CancellationToken cancellationToken)
    {
        await _warehouseService.DeleteBySellerIdAsync(sellerId, cancellationToken);
        return NoContent();
    }
}

