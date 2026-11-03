namespace Fulfillment.Api.Controllers.Internal;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Fulfillment.Application.Services.Inventory;

[ApiController]
[Route("api/v1/internal/inventories/reservations")]
public sealed class InternalInventoryReservationsController : ControllerBase
{
    private readonly IInventoryAppService _inventoryAppService;

    public InternalInventoryReservationsController(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateReservation(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.CreateReservationAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{referenceId}/commit")]
    public async Task<IActionResult> CommitReservation(
        [FromRoute] string referenceId,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.CommitReservationAsync(referenceId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{referenceId}/release")]
    public async Task<IActionResult> ReleaseReservation(
        [FromRoute] string referenceId,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryAppService.ReleaseReservationAsync(referenceId, reason, cancellationToken);
        return Ok(result);
    }

    [HttpPost("auto-expire")]
    public async Task<IActionResult> AutoExpireReservations(CancellationToken cancellationToken)
    {
        var expiredCount = await _inventoryAppService.AutoExpireReservationsAsync(cancellationToken);
        return Ok(new { expiredCount });
    }
}

