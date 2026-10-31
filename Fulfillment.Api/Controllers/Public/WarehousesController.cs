namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Fulfillment.Api.Extensions;
using Fulfillment.Api.Filters;
using Fulfillment.Application.Services.Warehouse;

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
    public async Task<IActionResult> ListActive(CancellationToken ct = default)
    {
        var warehouses = await _warehouseService.ListActiveAsync(ct);
        return Ok(warehouses);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var warehouse = await _warehouseService.GetByIdAsync(id, ct);
        if (warehouse == null)
        {
            return this.NotFoundError("WAREHOUSE_NOT_FOUND", $"Warehouse not found with ID '{id}'.");
        }

        return Ok(warehouse);
    }

    [HttpGet("seller/{sellerId:guid}")]
    public async Task<IActionResult> GetBySellerId([FromRoute] Guid sellerId, CancellationToken ct)
    {
        var warehouses = await _warehouseService.GetBySellerIdAsync(sellerId, ct);
        return Ok(warehouses);
    }

    [HttpPost]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseDto dto, CancellationToken ct)
    {
        var warehouse = await _warehouseService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, warehouse);
    }

    [HttpPut("{id:guid}")]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateWarehouseDto dto, CancellationToken ct)
    {
        var warehouse = await _warehouseService.UpdateAsync(id, dto, ct);
        return Ok(warehouse);
    }

    [HttpPatch("{id:guid}/status")]
    [RequireRole("admin")]
    public async Task<IActionResult> SetActiveStatus([FromRoute] Guid id, [FromQuery] bool isActive, CancellationToken ct)
    {
        var warehouse = await _warehouseService.SetActiveStatusAsync(id, isActive, ct);
        return Ok(warehouse);
    }
}
