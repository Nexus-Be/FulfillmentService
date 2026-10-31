namespace Fulfillment.Api.Controllers.Internal;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
}
