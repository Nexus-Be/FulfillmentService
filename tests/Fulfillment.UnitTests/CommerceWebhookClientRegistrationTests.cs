namespace Fulfillment.UnitTests;

using System.Net;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>Delivery updates never reached Commerce: the client called localhost and sent no service key.</summary>
public sealed class CommerceWebhookClientRegistrationTests
{
    [Fact]
    public async Task DeliveryStatus_GoesToTheConfiguredCommerceAddress_WithTheServiceKey()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused",
            ["Services:CommerceService:BaseUrl"] = "http://commerce-test:8003",
            ["InternalSecurity:ServiceKey"] = "test-service-key"
        }).Build();
        var handler = new RecordingHandler();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructureServices(configuration);
        services.AddHttpClient(nameof(ICommerceWebhookClient)).ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ICommerceWebhookClient>();
        var subOrderId = Guid.NewGuid();
        await client.NotifyDeliveryStatusAsync(subOrderId, "Delivered", "GHN123");

        Assert.Equal($"http://commerce-test:8003/api/v1/sub-orders/{subOrderId}/delivery-status", handler.Url);
        Assert.Equal("test-service-key", handler.ServiceKey);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? Url { get; private set; }
        public string? ServiceKey { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri!.ToString();
            ServiceKey = request.Headers.TryGetValues("X-Internal-Service-Key", out var values) ? values.FirstOrDefault() : null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
