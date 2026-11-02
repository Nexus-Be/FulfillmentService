namespace Fulfillment.Api.Controllers.Internal;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Fulfillment.Api.Extensions;
using Fulfillment.Application.Services.Inventory;

[ApiController]
[Route("api/v1/internal/inventories")]
public sealed class InternalInventoriesController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InternalInventoriesController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeInventory(
        [FromBody] DeclareInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.DeclareInventoryAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("declare")]
    public async Task<IActionResult> DeclareInventory(
        [FromBody] DeclareInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.DeclareInventoryAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustInventory(
        [FromBody] AdjustInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.AdjustInventoryAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{skuId:guid}")]
    public async Task<IActionResult> GetBySkuId(
        [FromRoute] Guid skuId,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetBySkuIdAsync(skuId, cancellationToken);
        if (result == null)
        {
            return this.NotFoundError("INVENTORY_NOT_FOUND", $"Inventory not found for SKU '{skuId}'.");
        }

        return Ok(result);
    }

    [HttpPost("batch-get")]
    public async Task<IActionResult> GetByListSku(
        [FromBody] IReadOnlyList<Guid> skuIds,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetByListSkuAsync(skuIds, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{skuId:guid}/ledgers")]
    public async Task<IActionResult> GetLedgersBySkuId(
        [FromRoute] Guid skuId,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetLedgersBySkuIdAsync(skuId, cancellationToken);
        return Ok(result);
    }
}
