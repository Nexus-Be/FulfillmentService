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
    private readonly IInventoryService _inventoryService;

    public InventoriesController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>Initializes warehouse inventory for a product SKU.</summary>
    [HttpPost("initialize")]
    public async Task<ActionResult<InventoryItemResponse>> InitializeInventory(
        [FromBody] InitializeInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.InitializeInventoryAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retrieves inventory balance by SKU ID.</summary>
    [HttpGet("{skuId:guid}")]
    public async Task<ActionResult<InventoryItemResponse>> GetBySkuId(
        [FromRoute] Guid skuId,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetBySkuIdAsync(skuId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retrieves inventory balances for a list of SKU IDs.</summary>
    [HttpPost("by-skus")]
    public async Task<ActionResult<IReadOnlyList<InventoryItemResponse>>> GetByListSku(
        [FromBody] IReadOnlyList<Guid> skuIds,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetByListSkuAsync(skuIds, cancellationToken);
        return Ok(result);
    }
}
