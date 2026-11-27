namespace Fulfillment.UnitTests;

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fulfillment.Application.Interfaces;
using Fulfillment.Infrastructure.Services.Notification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class NotificationClientTests
{
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public HttpStatusCode StatusCodeToReturn { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(StatusCodeToReturn);
        }
    }

    [Fact]
    public async Task SendShipmentStatusEmailAsync_WhenValid_ShouldSendCorrectPayloadAndHeaders()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("http://localhost:8007/")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>(
                    "InternalSecurity:ServiceKey",
                    "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e")
            })
            .Build();

        var client = new NotificationClient(httpClient, NullLogger<NotificationClient>.Instance, config);

        // Act
        var result = await client.SendShipmentStatusEmailAsync(
            "buyer@example.com",
            "GHN_TRACK_999",
            "InTransit",
            CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        mockHandler.LastRequest.Should().NotBeNull();
        mockHandler.LastRequest!.RequestUri!.ToString().Should().Be("http://localhost:8007/api/v1/notifications/email/send");
        mockHandler.LastRequest.Headers.Contains("X-Internal-Service-Key").Should().BeTrue();
        mockHandler.LastRequest.Headers.GetValues("X-Internal-Service-Key").Should().ContainSingle()
            .Which.Should().Be("eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e");

        mockHandler.LastRequestBody.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(mockHandler.LastRequestBody!);
        var root = doc.RootElement;
        root.GetProperty("toEmail").GetString().Should().Be("buyer@example.com");
        root.GetProperty("subject").GetString().Should().Contain("GHN_TRACK_999");
        root.GetProperty("htmlBody").GetString().Should().Contain("InTransit");
    }

    [Fact]
    public async Task SendShipmentStatusEmailAsync_WhenEmptyEmail_ShouldReturnFalseWithoutRequest()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("http://localhost:8007/")
        };
        var config = new ConfigurationBuilder().Build();
        var client = new NotificationClient(httpClient, NullLogger<NotificationClient>.Instance, config);

        // Act
        var result = await client.SendShipmentStatusEmailAsync(
            "",
            "GHN_TRACK_999",
            "InTransit",
            CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        mockHandler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task SendDeliveryAlertAsync_WhenValid_ShouldSendCorrectInternalAlert()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("http://localhost:8007/")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>(
                    "InternalSecurity:ServiceKey",
                    "eaT0Ombups70WJzd85CBBpp8rAno55SxKn1qvYBGH9e")
            })
            .Build();

        var client = new NotificationClient(httpClient, NullLogger<NotificationClient>.Instance, config);
        var orderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();

        var request = new DeliveryAlertRequest
        {
            OrderId = orderId,
            BuyerUserId = buyerId,
            TrackingNumber = "GHN_TRACK_123",
            Status = "Delivered",
            Note = "Giao thành công cho bảo vệ",
            ShipperName = "Nguyễn Văn Shipper",
            ShipperPhone = "0901234567",
            PodUrl = "https://cdn.ghn.vn/pod/img.jpg",
            WarehouseName = "Kho Tân Bình",
            OccurredAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = await client.SendDeliveryAlertAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        mockHandler.LastRequest.Should().NotBeNull();
        mockHandler.LastRequest!.RequestUri!.ToString().Should().Be("http://localhost:8007/api/v1/notifications/in-app");
        mockHandler.LastRequest.Headers.Contains("X-Internal-Service-Key").Should().BeTrue();

        using var doc = JsonDocument.Parse(mockHandler.LastRequestBody!);
        var root = doc.RootElement;
        root.GetProperty("userId").GetGuid().Should().Be(buyerId);
        root.GetProperty("notificationType").GetString().Should().Be("OrderCompleted");
        root.GetProperty("title").GetString().Should().Contain("Delivered");
        root.GetProperty("body").GetString().Should().Contain("GHN_TRACK_123");
        using var metaDoc = JsonDocument.Parse(root.GetProperty("payload").GetString()!);
        metaDoc.RootElement.GetProperty("orderId").GetGuid().Should().Be(orderId);
        metaDoc.RootElement.GetProperty("trackingNumber").GetString().Should().Be("GHN_TRACK_123");
        metaDoc.RootElement.GetProperty("shipperName").GetString().Should().Be("Nguyễn Văn Shipper");
        metaDoc.RootElement.GetProperty("podUrl").GetString().Should().Be("https://cdn.ghn.vn/pod/img.jpg");
    }

    [Fact]
    public async Task SendDeliveryAlertAsync_WhenServiceError_ShouldReturnFalseGracefully()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler
        {
            StatusCodeToReturn = HttpStatusCode.InternalServerError
        };
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("http://localhost:8007/")
        };

        var config = new ConfigurationBuilder().Build();
        var client = new NotificationClient(httpClient, NullLogger<NotificationClient>.Instance, config);

        var request = new DeliveryAlertRequest
        {
            OrderId = Guid.NewGuid(),
            BuyerUserId = Guid.NewGuid(),
            TrackingNumber = "GHN_TRACK_123",
            Status = "Delivered"
        };

        // Act
        var result = await client.SendDeliveryAlertAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}
