namespace Fulfillment.Infrastructure.Carriers.Ghtk;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Enums;

public class GhtkGateway : ICarrierGateway
{
    public const string CarrierCode = "GHTK";
    public string Code => CarrierCode;

    // TODO: Implement GHTK carrier gateway API integration
    public Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GHTK carrier integration is not supported yet.");
    }

    public Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GHTK carrier integration is not supported yet.");
    }

    public Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GHTK carrier integration is not supported yet.");
    }

    public Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GHTK carrier integration is not supported yet.");
    }

    public ShipmentStatus MapStatus(string rawCarrierStatus)
    {
        throw new NotSupportedException("GHTK carrier integration is not supported yet.");
    }
}

