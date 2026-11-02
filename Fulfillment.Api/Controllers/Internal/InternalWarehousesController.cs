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

    [HttpPut("{sellerId:guid}")]
    public async Task<ActionResult<WarehouseDto>> UpdateInternal(
        [FromRoute] Guid sellerId,
        [FromBody] InternalUpdateWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _warehouseService.UpdateBySellerIdAsync(sellerId, request, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{sellerId:guid}")]
    public async Task<IActionResult> DeleteInternal(
        [FromRoute] Guid sellerId,
        CancellationToken cancellationToken)
    {
        await _warehouseService.DeleteBySellerIdAsync(sellerId, cancellationToken);
        return NoContent();
    }
}

