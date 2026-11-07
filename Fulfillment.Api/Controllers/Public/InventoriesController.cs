namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<InventoryItemResponse>> InitializeInventory(
        [FromBody] DeclareInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.DeclareInventoryAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Adjusts inventory balance for a product SKU (Intake or Adjust).</summary>
    [HttpPost("adjust")]
    public async Task<ActionResult<AdjustInventoryResponse>> AdjustInventory(
        [FromBody] AdjustInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.AdjustInventoryAsync(request, cancellationToken);
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
    public async Task<ActionResult<IReadOnlyList<InventoryLedgerResponse>>> GetLedgersBySkuId(
        [FromRoute] Guid skuId,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.GetLedgersBySkuIdAsync(skuId, cancellationToken);
        return Ok(result);
    }
}
