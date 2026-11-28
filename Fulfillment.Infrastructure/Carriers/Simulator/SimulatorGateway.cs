namespace Fulfillment.Infrastructure.Carriers.Simulator;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Enums;

public class SimulatorGateway : ICarrierGateway
{
    public const string CarrierCode = "SIMULATOR";
    public string Code => CarrierCode;

    public Task<CarrierFeeCalculationResult> CalculateFeeAsync(
        CarrierFeeCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Fixed baseline fee with modest weight scaling for simulated demo
        decimal baseFee = 25000m;
        if (request.TotalWeightInGrams > 1000)
        {
            var extraKg = (decimal)Math.Ceiling((request.TotalWeightInGrams - 1000) / 1000.0);
            baseFee += extraKg * 5000m;
        }

        var result = new CarrierFeeCalculationResult(
            TotalFee: baseFee,
            ExpectedDeliveryTime: DateTimeOffset.UtcNow.AddDays(2),
            ServiceCode: "SIM_STANDARD");

        return Task.FromResult(result);
    }

    public Task<CarrierCreateShipmentResult> CreateShipmentAsync(
        CarrierCreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Generate distinctive demo tracking number: SIM-yyMMddHHmmss-XXX
        var trackingCode = $"SIM-{DateTimeOffset.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        decimal fee = 25000m;

        var result = new CarrierCreateShipmentResult(
            TrackingCode: trackingCode,
            ShippingFee: fee,
            ExpectedDeliveryTime: DateTimeOffset.UtcNow.AddDays(2),
            InitialStatus: ShipmentStatus.ReadyToPick,
            LabelUrl: $"/api/v1/shipments/labels/simulator?tracking={trackingCode}&pageSize=A5");

        return Task.FromResult(result);
    }

    public Task<CarrierStatusResult> GetStatusAsync(
        string trackingCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingCode);

        var result = new CarrierStatusResult(
            TrackingCode: trackingCode,
            Status: ShipmentStatus.ReadyToPick,
            RawStatus: "ready_to_pick",
            Description: "Đơn hàng mô phỏng đã sẵn sàng bàn giao cho bưu tá.",
            UpdatedAt: DateTimeOffset.UtcNow);

        return Task.FromResult(result);
    }

    public Task<CarrierCancelResult> CancelAsync(
        string trackingCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingCode);

        var result = new CarrierCancelResult(
            TrackingCode: trackingCode,
            Success: true,
            Message: "Đã hủy vận đơn mô phỏng thành công.");

        return Task.FromResult(result);
    }

    public Task<string?> GetPrintTokenAsync(
        string trackingCode,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(trackingCode);
    }

    public Task<bool> UpdateOrderAsync(
        string trackingCode,
        string toName,
        string toPhone,
        string toAddress,
        string? toWardCode,
        int? toDistrictId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public string BuildPrintLabelUrl(string trackingCode, string? printToken, string pageSize = "A5")
    {
        return $"/api/v1/shipments/labels/simulator?tracking={trackingCode}&pageSize={pageSize}";
    }

    public ShipmentStatus MapStatus(string rawCarrierStatus)
    {
        if (string.IsNullOrWhiteSpace(rawCarrierStatus))
        {
            return ShipmentStatus.ReadyToPick;
        }

        var normalized = rawCarrierStatus.Trim().ToLowerInvariant();

        if (Enum.TryParse<ShipmentStatus>(rawCarrierStatus, true, out var parsed))
        {
            return parsed;
        }

        return normalized switch
        {
            "ready_to_pick" or "ready" => ShipmentStatus.ReadyToPick,
            "picking" => ShipmentStatus.Picking,
            "packed" or "storing" => ShipmentStatus.Packed,
            "picked" or "handed_over" => ShipmentStatus.HandedOver,
            "delivering" or "in_transit" or "transporting" => ShipmentStatus.InTransit,
            "delivered" or "success" => ShipmentStatus.Delivered,
            "delivery_fail" or "failed" => ShipmentStatus.DeliveryFailed,
            "returning" or "return" => ShipmentStatus.Returning,
            "returned" => ShipmentStatus.Returned,
            "cancel" or "cancelled" => ShipmentStatus.Cancelled,
            _ => ShipmentStatus.ReadyToPick
        };
    }
}
