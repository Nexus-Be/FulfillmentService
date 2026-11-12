namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Fulfillment.Api.Extensions;
using Fulfillment.Api.Filters;
using Fulfillment.Application.Services.Inventory;

[ApiController]
[Route("api/v1/inventories")]
public sealed class InventoriesController : ControllerBase
{
    private readonly IInventoryAppService _inventoryAppService;

    public InventoriesController(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    /// <summary>Initializes warehouse inventory for a product SKU.</summary>
    [HttpPost("initialize")]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> InitializeInventory(
        [FromBody] DeclareInventoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var result = await _inventoryAppService.DeclareInventoryAsync(request, userId, this.IsAdmin(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Adjusts inventory balance for a product SKU (Intake or Adjust).</summary>
    [HttpPost("adjust")]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> AdjustInventory(
        [FromBody] AdjustInventoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var result = await _inventoryAppService.AdjustInventoryAsync(request, userId, this.IsAdmin(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Adjusts inventory balances for multiple product SKUs in one atomic batch (Intake or Adjust).</summary>
    [HttpPost("batch-adjust")]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> BatchAdjustInventory(
        [FromBody] BatchAdjustInventoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var result = await _inventoryAppService.BatchAdjustInventoryAsync(request, userId, this.IsAdmin(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Retrieves inventory balance by SKU ID.</summary>
    [HttpGet("{skuId:guid}")]
    public async Task<ActionResult<InventoryItemResponse>> GetBySkuId(
        [FromRoute] Guid skuId,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.GetBySkuIdAsync(skuId, cancellationToken);
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>Retrieves inventory balances for a list of SKU IDs.</summary>
    [HttpPost("by-skus")]
    public async Task<ActionResult<IReadOnlyList<InventoryItemResponse>>> GetByListSku(
        [FromBody] IReadOnlyList<Guid> skuIds,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.GetByListSkuAsync(skuIds, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retrieves inventory transaction ledgers for a product SKU.</summary>
    [HttpGet("{skuId:guid}/ledgers")]
    [RequireRole("seller", "admin", "staff")]
    public async Task<ActionResult<IReadOnlyList<InventoryLedgerResponse>>> GetLedgersBySkuId(
        [FromRoute] Guid skuId,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.GetLedgersBySkuIdAsync(skuId, cancellationToken);
        return Ok(result);
    }
}
