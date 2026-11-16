namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

    /// <summary>Retrieves paginated and filtered inventory transaction ledgers with audit summary.</summary>
    [HttpGet("ledgers")]
    [RequireRole("seller", "admin", "staff")]
    public async Task<IActionResult> GetLedgers(
        [FromQuery] InventoryLedgerFilterRequest request,
        CancellationToken cancellationToken)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var result = await _inventoryAppService.GetLedgersAsync(request, userId, this.IsAdmin(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Exports filtered inventory transaction ledgers to an Excel file (.xlsx).</summary>
    [HttpGet("ledgers/export")]
    [RequireRole("seller", "admin", "staff")]
    public async Task<IActionResult> ExportLedgers(
        [FromQuery] InventoryLedgerFilterRequest request,
        CancellationToken cancellationToken)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var fileBytes = await _inventoryAppService.ExportLedgersExcelAsync(request, userId, this.IsAdmin(), cancellationToken);
        var fileName = $"so_cai_kho_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>Retrieves audit transaction ledgers linked to a specific business reference (Order, Auction, Import).</summary>
    [HttpGet("ledgers/reference/{referenceType}/{referenceId}")]
    [RequireRole("seller", "admin", "staff")]
    public async Task<IActionResult> GetLedgersByReference(
        [FromRoute] string referenceType,
        [FromRoute] string referenceId,
        CancellationToken cancellationToken)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var result = await _inventoryAppService.GetLedgersByReferenceAsync(referenceType, referenceId, userId, this.IsAdmin(), cancellationToken);
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

    /// <summary>Analyzes an uploaded Excel/CSV file with AI schema inference and returns a deterministic preview.</summary>
    [HttpPost("import-preview")]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> ImportPreview(
        IFormFile file,
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        using var stream = file.OpenReadStream();
        var preview = await _inventoryAppService.PreviewImportAsync(stream, file.FileName, warehouseId, userId, this.IsAdmin(), cancellationToken);
        return Ok(preview);
    }

    /// <summary>Confirms and commits smart inventory adjustments atomically into database.</summary>
    [HttpPost("batch-commit-import")]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> BatchCommitImport(
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

    /// <summary>Updates low stock alert threshold and toggle for a specific SKU.</summary>
    [HttpPut("{skuId:guid}/threshold")]
    [RequireRole("seller", "admin")]
    public async Task<IActionResult> UpdateThreshold(
        [FromRoute] Guid skuId,
        [FromBody] UpdateStockThresholdRequest request,
        CancellationToken cancellationToken)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var result = await _inventoryAppService.UpdateThresholdAsync(skuId, request, userId, this.IsAdmin(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Retrieves all SKU items currently below their low stock threshold.</summary>
    [HttpGet("alerts/low-stock")]
    [RequireRole("seller", "admin", "staff")]
    public async Task<IActionResult> GetLowStockAlerts(
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var result = await _inventoryAppService.GetLowStockAlertsAsync(warehouseId, userId, this.IsAdmin(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Retrieves inventory items with no movement/activity for the given number of days.</summary>
    [HttpGet("alerts/dead-stock")]
    [RequireRole("seller", "admin", "staff")]
    public async Task<IActionResult> GetDeadStockAlerts(
        [FromQuery] Guid? warehouseId,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var result = await _inventoryAppService.GetDeadStockAlertsAsync(warehouseId, days, userId, this.IsAdmin(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Retrieves aggregated inventory analytics and filtered items with sorting.</summary>
    [HttpGet("analytics")]
    [RequireRole("seller", "admin", "staff")]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] InventoryAnalyticsFilterRequest request,
        CancellationToken cancellationToken)
    {
        if (!this.TryGetUserId(out var userId, out var errorResult))
        {
            return errorResult;
        }

        var result = await _inventoryAppService.GetAnalyticsAsync(request, userId, this.IsAdmin(), cancellationToken);
        return Ok(result);
    }
}
