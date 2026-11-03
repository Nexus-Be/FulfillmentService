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
}

