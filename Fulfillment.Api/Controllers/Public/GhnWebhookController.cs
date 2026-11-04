namespace Fulfillment.Api.Controllers.Public;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Domain.Enums;
using Fulfillment.Infrastructure.Carriers.Ghn;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("webhooks/ghn")]
[Route("api/v1/webhooks/ghn")]
public sealed class GhnWebhookController : ControllerBase
{
    private readonly IFulfillmentDbContext _dbContext;
    private readonly ICommerceWebhookClient _commerceWebhookClient;
    private readonly ILogger<GhnWebhookController> _logger;

    public GhnWebhookController(
        IFulfillmentDbContext dbContext,
        ICommerceWebhookClient commerceWebhookClient,
        ILogger<GhnWebhookController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _commerceWebhookClient = commerceWebhookClient ?? throw new ArgumentNullException(nameof(commerceWebhookClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<IActionResult> HandleGhnCallback(
        [FromBody] GhnWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.OrderCode))
        {
            return BadRequest(new { message = "Invalid webhook payload." });
        }

        _logger.LogInformation("Received GHN Webhook for OrderCode: {OrderCode}, Status: {Status}", payload.OrderCode, payload.Status);

        var shipment = await _dbContext.Shipments
            .FirstOrDefaultAsync(s => s.TrackingNumber == payload.OrderCode ||
                (payload.ClientOrderCode != null && s.OrderId.ToString() == payload.ClientOrderCode),
                cancellationToken);

        if (shipment == null)
        {
            _logger.LogWarning("Shipment with tracking {OrderCode} not found in database.", payload.OrderCode);
            return Ok(new { message = "Shipment not found, acknowledged." });
        }

        var newStatus = GhnStatusMapper.Map(payload.Status);
        shipment.Status = newStatus.ToString();
        shipment.UpdatedAt = DateTimeOffset.UtcNow;

        if (newStatus == ShipmentStatus.Delivered)
        {
            shipment.DeliveredAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Notify CommerceService if one of the terminal/critical statuses
        var notifyStatuses = new[]
        {
            ShipmentStatus.HandedOver,
            ShipmentStatus.InTransit,
            ShipmentStatus.Delivered,
            ShipmentStatus.DeliveryFailed,
            ShipmentStatus.Returned,
            ShipmentStatus.Cancelled
        };

        if (Array.IndexOf(notifyStatuses, newStatus) >= 0)
        {
            await _commerceWebhookClient.NotifyDeliveryStatusAsync(
                shipment.OrderId,
                newStatus.ToString(),
                shipment.TrackingNumber,
                payload.Description,
                cancellationToken);
        }

        return Ok(new { message = "Webhook processed successfully.", status = newStatus.ToString() });
    }
}

