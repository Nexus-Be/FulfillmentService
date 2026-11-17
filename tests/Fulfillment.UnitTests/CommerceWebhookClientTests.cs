namespace Fulfillment.UnitTests;

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Infrastructure.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class CommerceWebhookClientTests
{
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task NotifyDeliveryStatusAsync_ShouldSendCorrectHeaderAndPayload()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("http://localhost:8003/")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>(
                    "InternalSecurity:ServiceKey",
                    "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e")
            })
            .Build();

        var client = new CommerceWebhookClient(config, NullLogger<CommerceWebhookClient>.Instance, httpClient);
        var subOrderId = Guid.NewGuid();
        var deliveredAt = new DateTimeOffset(2026, 11, 16, 14, 30, 0, TimeSpan.Zero);

        var request = new CommerceDeliveryStatusRequest
        {
            SubOrderId = subOrderId,
            Status = "Delivered",
            CarrierCode = "GHN",
            TrackingCode = "GHN_TEST_123",
            Note = "Giao hàng thành công",
            DeliveredAt = deliveredAt,
            Location = "Bưu cục Tân Bình",
            PodUrl = "https://cdn.ghn.vn/pod/img123.jpg",
            ShipperName = "Nguyễn Văn Shipper",
            ShipperPhone = "0909999999"
        };

        // Act
        await client.NotifyDeliveryStatusAsync(request, CancellationToken.None);

        // Assert
        mockHandler.LastRequest.Should().NotBeNull();
        mockHandler.LastRequest!.RequestUri!.ToString().Should().Be($"http://localhost:8003/api/v1/sub-orders/{subOrderId}/delivery-status");
        mockHandler.LastRequest.Headers.Contains("X-Internal-Service-Key").Should().BeTrue();
        mockHandler.LastRequest.Headers.GetValues("X-Internal-Service-Key").Should().ContainSingle()
            .Which.Should().Be("eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e");

        mockHandler.LastRequestBody.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(mockHandler.LastRequestBody!);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("Delivered");
        root.GetProperty("carrierCode").GetString().Should().Be("GHN");
        root.GetProperty("trackingCode").GetString().Should().Be("GHN_TEST_123");
        root.GetProperty("note").GetString().Should().Be("Giao hàng thành công");
        DateTimeOffset.Parse(root.GetProperty("deliveredAt").GetString()!).Should().Be(deliveredAt);
        root.GetProperty("location").GetString().Should().Be("Bưu cục Tân Bình");
        root.GetProperty("podUrl").GetString().Should().Be("https://cdn.ghn.vn/pod/img123.jpg");
        root.GetProperty("shipperName").GetString().Should().Be("Nguyễn Văn Shipper");
        root.GetProperty("shipperPhone").GetString().Should().Be("0909999999");
    }

    [Fact]
    public async Task NotifyDeliveryStatusAsync_Overload_ShouldMapCorrectly()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("http://localhost:8003/")
        };

        var config = new ConfigurationBuilder().Build();
        var client = new CommerceWebhookClient(config, NullLogger<CommerceWebhookClient>.Instance, httpClient);
        var subOrderId = Guid.NewGuid();

        // Act
        await client.NotifyDeliveryStatusAsync(subOrderId, "InTransit", "GHN_SHIP_456", "Đang luân chuyển", cancellationToken: CancellationToken.None);

        // Assert
        mockHandler.LastRequest.Should().NotBeNull();
        using var doc = JsonDocument.Parse(mockHandler.LastRequestBody!);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("InTransit");
        root.GetProperty("carrierCode").GetString().Should().Be("GHN");
        root.GetProperty("trackingCode").GetString().Should().Be("GHN_SHIP_456");
        root.GetProperty("note").GetString().Should().Be("Đang luân chuyển");
        root.GetProperty("deliveredAt").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
