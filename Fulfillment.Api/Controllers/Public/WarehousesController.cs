namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Services.Warehouse;
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

    /// <summary>Lists active warehouses with pagination.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedData<WarehouseDto>>> ListActive(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var warehouses = await _warehouseService.ListActiveAsync(page, pageSize, ct);
        return Ok(warehouses);
    }

    /// <summary>Gets warehouse details by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WarehouseDto>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var warehouse = await _warehouseService.GetByIdAsync(id, ct);
        return Ok(warehouse);
    }
}
