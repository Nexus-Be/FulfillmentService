namespace Fulfillment.UnitTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Application.Exceptions;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipment;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;
using Fulfillment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ShipmentServiceTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class FakeCarrierGateway : ICarrierGateway
    {
        public string Code => "GHN";

        public Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CarrierFeeCalculationResult(
                TotalFee: 28000m,
                ExpectedDeliveryTime: DateTimeOffset.UtcNow.AddDays(2),
                ServiceCode: "GHN_STANDARD"));
        }

        public Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CarrierCreateShipmentResult(
                TrackingCode: "GHN_TEST_123456",
                ShippingFee: 28000m,
                ExpectedDeliveryTime: DateTimeOffset.UtcNow.AddDays(2),
                InitialStatus: ShipmentStatus.ReadyToPick,
                LabelUrl: "https://labels.ghn.vn/GHN_TEST_123456.pdf"));
        }

        public Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CarrierStatusResult(
                TrackingCode: trackingCode,
                Status: ShipmentStatus.InTransit,
                RawStatus: "delivering",
                Description: "In delivery",
                UpdatedAt: DateTimeOffset.UtcNow));
        }

        public Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CarrierCancelResult(trackingCode, true, "Success"));
        }

        public Task<string?> GetPrintTokenAsync(string trackingCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>("mock_ghn_print_token");
        }

        public ShipmentStatus MapStatus(string rawCarrierStatus) => ShipmentStatus.ReadyToPick;
    }

    private sealed class FakeCarrierResolver : ICarrierResolver
    {
        private readonly ICarrierGateway _carrier;
        public FakeCarrierResolver(ICarrierGateway carrier) => _carrier = carrier;
        public ICarrierGateway Get(string? carrierCode = null) => _carrier;
    }

    [Fact]
    public async Task CalculateShippingFeeAsync_ShouldDynamicallyResolveSellerWarehouse_AndCalculateFee()
    {
        // Arrange
        using var db = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            Code = "WH_SELLER_01",
            Name = "Seller Dedicated Warehouse",
            Address = "123 Nguyen Hue, Q1, HCM",
            IsActive = true
        };
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();

        var fakeCarrier = new FakeCarrierGateway();
        var resolver = new FakeCarrierResolver(fakeCarrier);
        var service = new ShipmentService(db, resolver);

        var request = new ShopShippingQuoteRequest(
            SellerId: sellerId,
            FromDistrictId: 1442,
            ToDistrictId: 1450,
            ToWardCode: "20804",
            WeightInGrams: 500,
            Items: new List<ShippingQuoteItem>
            {
                new(Guid.NewGuid(), 1, 500)
            });

        // Act
        var result = await service.CalculateShippingFeeAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Fee.Should().Be(28000m);
        result.IsEstimatedFallback.Should().BeFalse();
        result.ServiceName.Should().Be("GHN_STANDARD");
    }

    private sealed class TokenlessCarrierGateway : ICarrierGateway
    {
        public string Code => "GHN";

        public Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default) =>
            throw new CarrierIntegrationException("GHN API token is not configured.", "GHN", "MISSING_TOKEN");

        public Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string?> GetPrintTokenAsync(string trackingCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ShipmentStatus MapStatus(string rawCarrierStatus) => ShipmentStatus.ReadyToPick;
    }

    private static ShopShippingQuoteRequest QuoteRequest(Guid sellerId, int weight) =>
        new(sellerId, FromDistrictId: 1442, ToDistrictId: 1450, ToWardCode: "20804", WeightInGrams: weight);

    [Fact]
    public async Task CalculateShippingFeeAsync_WithoutCarrierToken_FailsByDefault()
    {
        using var db = CreateDbContext();
        var service = new ShipmentService(db, new FakeCarrierResolver(new TokenlessCarrierGateway()));

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => service.CalculateShippingFeeAsync(QuoteRequest(Guid.NewGuid(), 500)));

        // Not the buyer's address: the carrier has no credentials here, so it is a 503 of its own.
        ex.ErrorCode.Should().Be("SHIPPING_CARRIER_NOT_CONFIGURED");
        ex.StatusCode.Should().Be(503);
    }

    private sealed class FailingCarrierGateway(Exception failure) : ICarrierGateway
    {
        public string Code => "GHN";

        public Task<CarrierFeeCalculationResult> CalculateFeeAsync(CarrierFeeCalculationRequest request, CancellationToken cancellationToken = default) =>
            throw failure;

        public Task<CarrierCreateShipmentResult> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CarrierStatusResult> GetStatusAsync(string trackingCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CarrierCancelResult> CancelAsync(string trackingCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string?> GetPrintTokenAsync(string trackingCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ShipmentStatus MapStatus(string rawCarrierStatus) => ShipmentStatus.ReadyToPick;
    }

    [Theory]
    [InlineData("integration", "MISSING_TOKEN", "SHIPPING_CARRIER_NOT_CONFIGURED", 503)]
    [InlineData("integration", "Unauthorized", "SHIPPING_CARRIER_NOT_CONFIGURED", 503)]   // the carrier refused the configured token
    [InlineData("integration", "Forbidden", "SHIPPING_CARRIER_NOT_CONFIGURED", 503)]
    [InlineData("integration", "HTTP_ERROR", "SHIPPING_CARRIER_UNAVAILABLE", 503)]        // network failure
    [InlineData("integration", "InternalServerError", "SHIPPING_CARRIER_UNAVAILABLE", 503)]
    [InlineData("integration", "BadGateway", "SHIPPING_CARRIER_UNAVAILABLE", 503)]
    [InlineData("integration", "ServiceUnavailable", "SHIPPING_CARRIER_UNAVAILABLE", 503)]
    [InlineData("integration", "TooManyRequests", "SHIPPING_CARRIER_UNAVAILABLE", 503)]
    [InlineData("integration", "500", "SHIPPING_CARRIER_UNAVAILABLE", 503)]               // numeric code answered by the carrier
    [InlineData("timeout", "", "SHIPPING_CARRIER_TIMEOUT", 503)]
    [InlineData("notfound", "", "SHIPPING_CARRIER_NOT_CONFIGURED", 503)]
    [InlineData("integration", "BadRequest", "SHIPPING_QUOTE_FAILED", 400)]               // the carrier objects to the data itself
    [InlineData("integration", "WARD_NOT_FOUND", "SHIPPING_QUOTE_FAILED", 400)]
    public async Task CalculateShippingFeeAsync_MapsEachCarrierFailureToItsOwnCode(string kind, string carrierCode, string expectedCode, int expectedStatus)
    {
        using var db = CreateDbContext();
        Exception failure = kind switch
        {
            "timeout" => new CarrierTimeoutException("GHN fee calculation timed out.", "GHN"),
            "notfound" => new CarrierNotFoundException("GHN"),
            _ => new CarrierIntegrationException("carrier said no", "GHN", carrierCode)
        };
        var service = new ShipmentService(db, new FakeCarrierResolver(new FailingCarrierGateway(failure)));

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() => service.CalculateShippingFeeAsync(QuoteRequest(Guid.NewGuid(), 500)));

        ex.ErrorCode.Should().Be(expectedCode);
        ex.StatusCode.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task CalculateShippingFeeAsync_WhenTheCarrierTimesOut_IsNotLeftToTheGenericHandler()
    {
        using var db = CreateDbContext();
        var service = new ShipmentService(db, new FakeCarrierResolver(new FailingCarrierGateway(new CarrierTimeoutException("slow", "GHN"))));

        var act = () => service.CalculateShippingFeeAsync(QuoteRequest(Guid.NewGuid(), 500));

        // It used to escape as a CarrierTimeoutException (504 from the middleware) because only CarrierIntegrationException was caught.
        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Theory]
    [InlineData(500, 30000)]
    [InlineData(750, 35000)]
    [InlineData(1500, 40000)]
    public async Task CalculateShippingFeeAsync_WithoutCarrierToken_GivesMarkedEstimateOnlyWhenEnabled(int weight, int expectedFee)
    {
        using var db = CreateDbContext();
        var service = new ShipmentService(
            db, new FakeCarrierResolver(new TokenlessCarrierGateway()), estimateOptions: new ShippingEstimateOptions(Enabled: true));

        var result = await service.CalculateShippingFeeAsync(QuoteRequest(Guid.NewGuid(), weight));

        result.Fee.Should().Be(expectedFee);
        result.IsEstimatedFallback.Should().BeTrue();
        result.ServiceName.Should().Be("ESTIMATED");
    }

    [Fact]
    public async Task CreateShipmentAsync_ShouldDynamicallyResolveWarehouseAndCarrier_AndSaveShipment()
    {
        // Arrange
        using var db = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            Code = "WH_SELLER_99",
            Name = "Shop 99 Warehouse",
            Address = "District 1, HCM",
            IsActive = true
        };
        var carrier = new Carrier
        {
            Id = Guid.NewGuid(),
            Code = "GHN",
            Name = "Giao Hang Nhanh",
            IsActive = true
        };
        db.Warehouses.Add(warehouse);
        db.Carriers.Add(carrier);
        await db.SaveChangesAsync();

        var fakeCarrier = new FakeCarrierGateway();
        var resolver = new FakeCarrierResolver(fakeCarrier);
        var service = new ShipmentService(db, resolver);

        var subOrderId = Guid.NewGuid();
        var request = new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            SubOrderId: subOrderId,
            SellerId: sellerId,
            RecipientName: "Tran Van B",
            RecipientPhone: "0912345678",
            ShippingAddress: "456 Le Loi, Q1",
            ToDistrictId: 1442,
            ToWardCode: "20101",
            WeightInGrams: 500);

        // Act
        var response = await service.CreateShipmentAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.TrackingNumber.Should().Be("GHN_TEST_123456");
        response.Status.Should().Be(ShipmentStatus.ReadyToPick.ToString());
        response.LabelUrl.Should().Be("https://labels.ghn.vn/GHN_TEST_123456.pdf");

        // Verify database persistence
        var savedShipment = await db.Shipments.FirstOrDefaultAsync(s => s.OrderId == subOrderId);
        savedShipment.Should().NotBeNull();
        savedShipment!.WarehouseId.Should().Be(warehouse.Id);
        savedShipment.CarrierId.Should().Be(carrier.Id);
        savedShipment.TrackingNumber.Should().Be("GHN_TEST_123456");
    }

    [Fact]
    public async Task CreateShipmentAsync_WhenCalledAgainForSameSubOrder_ReturnsExistingShipmentIdempotently()
    {
        // Arrange
        using var db = CreateDbContext();
        var sellerId = Guid.NewGuid();
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            SellerId = sellerId,
            Code = "WH_SELLER_02",
            Name = "Warehouse",
            IsActive = true
        };
        var carrier = new Carrier
        {
            Id = Guid.NewGuid(),
            Code = "GHN",
            Name = "GHN",
            IsActive = true
        };
        db.Warehouses.Add(warehouse);
        db.Carriers.Add(carrier);
        await db.SaveChangesAsync();

        var fakeCarrier = new FakeCarrierGateway();
        var resolver = new FakeCarrierResolver(fakeCarrier);
        var service = new ShipmentService(db, resolver);

        var subOrderId = Guid.NewGuid();
        var request = new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            SubOrderId: subOrderId,
            SellerId: sellerId,
            RecipientName: "Tran Van B",
            RecipientPhone: "0912345678",
            ShippingAddress: "456 Le Loi, Q1",
            ToDistrictId: 1442,
            ToWardCode: "20101",
            WeightInGrams: 500);

        // First call
        var firstResponse = await service.CreateShipmentAsync(request);

        // Second call with same subOrderId
        var secondResponse = await service.CreateShipmentAsync(request);

        // Assert
        secondResponse.Should().NotBeNull();
        secondResponse.ShipmentId.Should().Be(firstResponse.ShipmentId);
        secondResponse.TrackingNumber.Should().Be(firstResponse.TrackingNumber);

        // Only one shipment in database
        var count = await db.Shipments.CountAsync(s => s.OrderId == subOrderId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task CreateShipmentAsync_WhenNoWarehouseExists_ThrowsEntityNotFoundException()
    {
        // Arrange
        using var db = CreateDbContext();
        var fakeCarrier = new FakeCarrierGateway();
        var resolver = new FakeCarrierResolver(fakeCarrier);
        var service = new ShipmentService(db, resolver);

        var request = new CreateShipmentRequest(
            OrderId: Guid.NewGuid(),
            SubOrderId: Guid.NewGuid(),
            SellerId: Guid.NewGuid(),
            RecipientName: "Tran Van B",
            RecipientPhone: "0912345678",
            ShippingAddress: "456 Le Loi, Q1",
            ToDistrictId: 1442,
            ToWardCode: "20101",
            WeightInGrams: 500);

        // Act & Assert
        var act = async () => await service.CreateShipmentAsync(request);
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}

