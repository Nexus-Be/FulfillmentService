namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Fulfillment.Api.Extensions;
using Fulfillment.Api.Filters;
using Fulfillment.Application.Common.Models;
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WarehouseDto>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var warehouse = await _warehouseService.GetByIdAsync(id, ct);
        return Ok(warehouse);
    }

    [HttpGet("seller/{sellerId:guid}")]
    public async Task<IActionResult> GetBySellerId([FromRoute] Guid sellerId, CancellationToken ct)
    {
        var warehouse = await _warehouseService.GetBySellerIdAsync(sellerId, ct);
        if (warehouse == null)
        {
            return this.NotFoundError("WAREHOUSE_NOT_FOUND", $"No active warehouse found for seller '{sellerId}'.");
        }

        return Ok(warehouse);
    }

    [HttpGet("me")]
    [RequireRole("seller")]
    public async Task<IActionResult> GetMyWarehouse(CancellationToken ct)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var warehouse = await _warehouseService.GetBySellerIdAsync(userId, ct);
        if (warehouse == null)
        {
            return this.NotFoundError("WAREHOUSE_NOT_FOUND", "No active warehouse registered for your seller account.");
        }

        return Ok(warehouse);
    }

    [HttpPost]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateWarehouseRequest request,
        CancellationToken ct)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var warehouse = await _warehouseService.CreateAsync(request, userId, ct);
        return StatusCode(StatusCodes.Status201Created, warehouse);
    }

    [HttpPut("{id:guid}")]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateWarehouseRequest request,
        CancellationToken ct)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var warehouse = await _warehouseService.UpdateAsync(id, request, userId, this.IsAdmin(), ct);
        return Ok(warehouse);
    }

    [HttpPatch("{id:guid}/status")]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> SetActiveStatus(
        [FromRoute] Guid id,
        [FromQuery] bool isActive,
        CancellationToken ct)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var warehouse = await _warehouseService.SetActiveStatusAsync(id, isActive, userId, this.IsAdmin(), ct);
        return Ok(warehouse);
    }
}
