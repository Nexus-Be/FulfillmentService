namespace Fulfillment.Application.Interfaces.Shipping;

using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Enums;

public interface ICarrierGateway
{
    string Code { get; }

    Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default);

    Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default);

    Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default);

    Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default);

    Task<string?> GetPrintTokenAsync(string trackingCode, CancellationToken cancellationToken = default);

    Task<bool> UpdateOrderAsync(
        string trackingCode,
        string toName,
        string toPhone,
        string toAddress,
        string? toWardCode,
        int? toDistrictId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    string BuildPrintLabelUrl(string trackingCode, string? printToken, string pageSize = "A5")
        => $"https://dev-online-gateway.ghn.vn/a5/public-api/print{pageSize}?token={printToken ?? trackingCode}";

    ShipmentStatus MapStatus(string rawCarrierStatus);
}

