namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Application.Exceptions;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Enums;
using Xunit;

public class CarrierResolverTests
{
    private class FakeCarrierGateway : ICarrierGateway
    {
        public string Code { get; }

        public FakeCarrierGateway(string code)
        {
            Code = code;
        }

        public Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new CarrierFeeCalculationResult(30000));

        public Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new CarrierCreateShipmentResult("TRACK123", 30000, DateTimeOffset.UtcNow.AddDays(2), ShipmentStatus.ReadyToPick));

        public Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default)
            => Task.FromResult(new CarrierStatusResult(trackingCode, ShipmentStatus.InTransit, "in_transit"));

        public Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default)
            => Task.FromResult(new CarrierCancelResult(trackingCode, true));

        public ShipmentStatus MapStatus(string rawCarrierStatus) => ShipmentStatus.ReadyToPick;
    }

    [Fact]
    public void Get_WithoutCarrierCode_ShouldReturnDefaultCarrier()
    {
        var ghn = new FakeCarrierGateway("GHN");
        var ghtk = new FakeCarrierGateway("GHTK");
        var resolver = new CarrierResolver(new[] { ghn, ghtk }, "GHN");

        var result = resolver.Get();

        result.Should().NotBeNull();
        result.Code.Should().Be("GHN");
    }

    [Fact]
    public void Get_WithSpecificCode_ShouldReturnMatchingCarrier_CaseInsensitive()
    {
        var ghn = new FakeCarrierGateway("GHN");
        var ghtk = new FakeCarrierGateway("GHTK");
        var resolver = new CarrierResolver(new[] { ghn, ghtk }, "GHN");

        var result = resolver.Get("ghtk");

        result.Should().NotBeNull();
        result.Code.Should().Be("GHTK");
    }

    [Fact]
    public void Get_WithUnsupportedCode_ShouldThrowCarrierNotFoundException()
    {
        var ghn = new FakeCarrierGateway("GHN");
        var resolver = new CarrierResolver(new[] { ghn }, "GHN");

        Action act = () => resolver.Get("UNKNOWN_CARRIER");

        act.Should().Throw<CarrierNotFoundException>()
            .WithMessage("*UNKNOWN_CARRIER*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespaceDefaultCarrierCode_ShouldThrowArgumentException(string? invalidDefaultCode)
    {
        var ghn = new FakeCarrierGateway("GHN");

        Action act = () => new CarrierResolver(new[] { ghn }, invalidDefaultCode!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("defaultCarrierCode");
    }
}
