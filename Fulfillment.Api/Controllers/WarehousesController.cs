namespace Fulfillment.Api.Controllers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Services.Warehouse;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListActive(CancellationToken ct)
    {
        var warehouses = await _warehouseService.ListActiveAsync(ct);
        return Ok(warehouses);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var warehouse = await _warehouseService.GetByIdAsync(id, ct);
        if (warehouse == null)
        {
            return NotFound(new { message = "Warehouse not found with the specified ID." });
        }

        return Ok(warehouse);
    }
}
