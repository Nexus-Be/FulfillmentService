namespace Fulfillment.Api.Workers;

using Fulfillment.Application.Services.Shipment;

/// <summary>Keeps retrying the delivery updates Commerce has not acknowledged yet (see <see cref="DeliveryNotificationOutbox"/>).</summary>
public sealed class CommerceDeliveryNotificationWorker : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommerceDeliveryNotificationWorker> _logger;

    public CommerceDeliveryNotificationWorker(IServiceProvider serviceProvider, ILogger<CommerceDeliveryNotificationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CommerceDeliveryNotificationWorker started. Retrying unacknowledged updates every {Seconds} seconds.", Period.TotalSeconds);
        using var timer = new PeriodicTimer(Period);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outbox = scope.ServiceProvider.GetRequiredService<DeliveryNotificationOutbox>();
                var attempted = await outbox.ProcessDueAsync(BatchSize, stoppingToken);
                if (attempted > 0)
                    _logger.LogInformation("CommerceDeliveryNotificationWorker attempted {Count} delivery update(s).", attempted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // One bad cycle (database down, for example) must not stop the worker.
                _logger.LogError(ex, "Delivery notification cycle failed. It will run again in {Seconds} seconds.", Period.TotalSeconds);
            }
        }
    }
}
