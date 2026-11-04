namespace Fulfillment.Infrastructure.Carriers.Ahamove;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Enums;

public class AhamoveGateway : ICarrierGateway
{
    public const string CarrierCode = "AHAMOVE";
    public string Code => CarrierCode;

    // TODO: Implement Ahamove carrier gateway API integration
    public Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Ahamove carrier integration is not supported yet.");
    }

    public Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Ahamove carrier integration is not supported yet.");
    }

    public Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Ahamove carrier integration is not supported yet.");
    }

    public Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Ahamove carrier integration is not supported yet.");
    }

    public ShipmentStatus MapStatus(string rawCarrierStatus)
    {
        throw new NotSupportedException("Ahamove carrier integration is not supported yet.");
    }
}

