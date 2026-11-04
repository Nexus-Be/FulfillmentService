namespace Fulfillment.Api.Workers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Services.Inventory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class ReservationAutoExpireWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationAutoExpireWorker> _logger;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(1);

    public ReservationAutoExpireWorker(
        IServiceProvider serviceProvider,
        ILogger<ReservationAutoExpireWorker> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationAutoExpireWorker started. Scanning every {PeriodMinutes} minute(s).", _period.TotalMinutes);

        using var timer = new PeriodicTimer(_period);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryAppService>();

                var expiredCount = await inventoryService.AutoExpireReservationsAsync(stoppingToken);
                if (expiredCount > 0)
                {
                    _logger.LogInformation("ReservationAutoExpireWorker successfully expired {Count} overdue reservation(s).", expiredCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during reservation auto-expiration background job.");
            }
        }

        _logger.LogInformation("ReservationAutoExpireWorker stopped.");
    }
}

