namespace Fulfillment.UnitTests;

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Enums;
using Fulfillment.Infrastructure.Carriers.Simulator;
using Xunit;

public class SimulatorGatewayTests
{
    [Fact]
    public async Task SimulatorGateway_CalculateFee_ReturnsExpectedFixedFee()
    {
        var gateway = new SimulatorGateway();
        var from = new ShippingAddress("Sender", "0900000000", "Warehouse");
        var to = new ShippingAddress("Receiver", "0901111111", "Buyer Address");
        var request = new CarrierFeeCalculationRequest(from, to, 500);

        var result = await gateway.CalculateFeeAsync(request);

        result.Should().NotBeNull();
        result.TotalFee.Should().Be(25000m);
        result.ServiceCode.Should().Be("SIM_STANDARD");
    }

    [Fact]
    public async Task SimulatorGateway_CreateShipment_GeneratesSimTrackingCode()
    {
        var gateway = new SimulatorGateway();
        var from = new ShippingAddress("Sender", "0900000000", "Warehouse");
        var to = new ShippingAddress("Receiver", "0901111111", "Buyer Address");
        var request = new CarrierCreateShipmentRequest("ORDER-123", from, to, 500);

        var result = await gateway.CreateShipmentAsync(request);

        result.Should().NotBeNull();
        result.TrackingCode.Should().StartWith("SIM-");
        result.InitialStatus.Should().Be(ShipmentStatus.ReadyToPick);
        result.ShippingFee.Should().Be(25000m);
    }

    [Fact]
    public void SimulatorGateway_MapStatus_MapsKnownStatusesCorrectly()
    {
        var gateway = new SimulatorGateway();

        gateway.MapStatus("delivered").Should().Be(ShipmentStatus.Delivered);
        gateway.MapStatus("in_transit").Should().Be(ShipmentStatus.InTransit);
        gateway.MapStatus("picking").Should().Be(ShipmentStatus.Picking);
        gateway.MapStatus("ReadyToPick").Should().Be(ShipmentStatus.ReadyToPick);
    }
}
