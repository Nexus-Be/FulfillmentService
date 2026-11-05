namespace Fulfillment.Infrastructure.Carriers.Ghn;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Fulfillment.Application.Exceptions;
using Fulfillment.Application.Interfaces.Shipping;
using Fulfillment.Application.Services.Shipping;
using Fulfillment.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class GhnGateway : ICarrierGateway
{
    public const string CarrierCode = "GHN";
    public string Code => CarrierCode;

    private readonly HttpClient _httpClient;
    private readonly GhnOptions _options;
    private readonly ILogger<GhnGateway> _logger;

    public GhnGateway(
        IOptions<GhnOptions> options,
        ILogger<GhnGateway> logger,
        HttpClient? httpClient = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? new HttpClient();

        if (_httpClient.BaseAddress == null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }
        if (_options.TimeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }

        ConfigureHttpClientHeaders();
    }

    private void ConfigureHttpClientHeaders()
    {
        if (!string.IsNullOrWhiteSpace(_options.Token) && !_httpClient.DefaultRequestHeaders.Contains("Token"))
        {
            _httpClient.DefaultRequestHeaders.Add("Token", _options.Token);
        }

        if (!string.IsNullOrWhiteSpace(_options.ShopId) && !_httpClient.DefaultRequestHeaders.Contains("ShopId"))
        {
            _httpClient.DefaultRequestHeaders.Add("ShopId", _options.ShopId);
        }
    }

    public async Task<CarrierFeeCalculationResult> CalculateFeeAsync(
        CarrierFeeCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.FromAddress?.DistrictId == null)
        {
            throw new ArgumentException("FromDistrictId is required for calculating GHN shipping fee.", nameof(request));
        }

        if (request.ToAddress?.DistrictId == null || string.IsNullOrWhiteSpace(request.ToAddress.WardCode))
        {
            throw new ArgumentException("ToDistrictId and ToWardCode are required for calculating GHN shipping fee.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogWarning("GHN API token is not configured in environment. Triggering fallback calculation.");
            throw new CarrierIntegrationException("GHN API token is not configured.", CarrierCode, "MISSING_TOKEN");
        }

        var serviceTypeId = request.TotalWeightInGrams >= 20000 ? 5 : 2;

        var ghnRequest = new GhnFeeCalculationRequest
        {
            FromDistrictId = request.FromAddress.DistrictId.Value,
            ToDistrictId = request.ToAddress.DistrictId.Value,
            ToWardCode = request.ToAddress.WardCode,
            ServiceTypeId = serviceTypeId,
            Weight = request.TotalWeightInGrams > 0 ? request.TotalWeightInGrams : 500,
            Length = request.LengthCm ?? 20,
            Width = request.WidthCm ?? 15,
            Height = request.HeightCm ?? 10
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/shiip/public-api/v2/shipping-order/fee",
                ghnRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("GHN CalculateFee failed with status {StatusCode}: {ErrorText}", response.StatusCode, errorText);
                throw new CarrierIntegrationException(
                    $"GHN CalculateFee returned HTTP {response.StatusCode}.",
                    CarrierCode,
                    response.StatusCode.ToString());
            }

            var ghnResponse = await response.Content.ReadFromJsonAsync<GhnApiResponse<GhnFeeData>>(cancellationToken: cancellationToken);

            if (ghnResponse == null || ghnResponse.Code != 200 || ghnResponse.Data == null)
            {
                var errMessage = ghnResponse?.Message ?? ghnResponse?.CodeMessage ?? "Unknown error from GHN.";
                _logger.LogWarning("GHN fee error code {Code}: {Message}", ghnResponse?.Code, errMessage);
                throw new CarrierIntegrationException(errMessage, CarrierCode, ghnResponse?.CodeMessage ?? ghnResponse?.Code.ToString());
            }

            return new CarrierFeeCalculationResult(
                TotalFee: ghnResponse.Data.Total,
                ExpectedDeliveryTime: DateTimeOffset.UtcNow.AddDays(2),
                ServiceCode: serviceTypeId == 5 ? "GHN_HEAVY" : "GHN_STANDARD");
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "GHN CalculateFee timed out.");
            throw new CarrierTimeoutException("GHN fee calculation timed out.", CarrierCode, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GHN CalculateFee network request failed.");
            throw new CarrierIntegrationException("GHN communication error.", CarrierCode, "HTTP_ERROR", ex);
        }
    }

    public async Task<CarrierCreateShipmentResult> CreateShipmentAsync(
        CarrierCreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogWarning("GHN API token is not configured in environment. Using simulated sandbox registration for testing.");
            var simTracking = $"GHN{Random.Shared.Next(1000000, 9999999)}";
            return new CarrierCreateShipmentResult(
                TrackingCode: simTracking,
                ShippingFee: 32000.00m,
                ExpectedDeliveryTime: DateTimeOffset.UtcNow.AddDays(2),
                InitialStatus: ShipmentStatus.ReadyToPick,
                LabelUrl: $"https://fulfillment.nexus.internal/labels/{simTracking}.pdf");
        }

        var serviceTypeId = request.TotalWeightInGrams >= 20000 ? 5 : 2;

        var items = request.Items != null && request.Items.Count > 0
            ? request.Items.Select(i => new GhnItemDto
            {
                Name = i.Name ?? "Product",
                Code = i.SkuId,
                Quantity = i.Quantity,
                Weight = i.WeightInGrams,
                Length = i.LengthCm ?? 10,
                Width = i.WidthCm ?? 10,
                Height = i.HeightCm ?? 10
            }).ToList()
            : new List<GhnItemDto>
            {
                new()
                {
                    Name = "Package Item",
                    Quantity = 1,
                    Weight = request.TotalWeightInGrams,
                    Length = request.LengthCm ?? 20,
                    Width = request.WidthCm ?? 15,
                    Height = request.HeightCm ?? 10
                }
            };

        var ghnRequest = new GhnCreateOrderRequest
        {
            PaymentTypeId = 1, // 1: Shop tra phi van chuyen
            RequiredNote = "CHOXEMHANGKHONGTHU",
            Note = request.Note,
            ClientOrderCode = request.InternalOrderCode,
            ToName = request.ToAddress.FullName,
            ToPhone = request.ToAddress.Phone,
            ToAddress = request.ToAddress.AddressLine,
            ToWardName = request.ToAddress.WardName,
            ToWardCode = request.ToAddress.WardCode,
            ToDistrictId = request.ToAddress.DistrictId,
            Weight = request.TotalWeightInGrams > 0 ? request.TotalWeightInGrams : 500,
            Length = request.LengthCm ?? 20,
            Width = request.WidthCm ?? 15,
            Height = request.HeightCm ?? 10,
            ServiceTypeId = serviceTypeId,
            CodAmount = (int)request.CodAmount,
            Items = items
        };

        if (request.FromAddress != null && !string.IsNullOrWhiteSpace(request.FromAddress.FullName))
        {
            ghnRequest.FromName = request.FromAddress.FullName;
            ghnRequest.FromPhone = request.FromAddress.Phone;
            ghnRequest.FromAddress = request.FromAddress.AddressLine;
            ghnRequest.FromWardName = request.FromAddress.WardName;
            ghnRequest.FromDistrictId = request.FromAddress.DistrictId;
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/shiip/public-api/v2/shipping-order/create",
                ghnRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("GHN CreateOrder failed with status {StatusCode}: {ErrorText}", response.StatusCode, errorText);
                throw new CarrierIntegrationException(
                    $"GHN CreateOrder returned HTTP {response.StatusCode}.",
                    CarrierCode,
                    response.StatusCode.ToString());
            }

            var ghnResponse = await response.Content.ReadFromJsonAsync<GhnApiResponse<GhnCreateOrderData>>(cancellationToken: cancellationToken);

            if (ghnResponse == null || ghnResponse.Code != 200 || ghnResponse.Data == null)
            {
                var errMessage = ghnResponse?.Message ?? ghnResponse?.CodeMessage ?? "Unknown error creating GHN order.";
                _logger.LogWarning("GHN create order error code {Code}: {Message}", ghnResponse?.Code, errMessage);
                throw new CarrierIntegrationException(errMessage, CarrierCode, ghnResponse?.CodeMessage ?? ghnResponse?.Code.ToString());
            }

            DateTimeOffset? expectedDelivery = null;
            if (DateTimeOffset.TryParse(ghnResponse.Data.ExpectedDeliveryTime, out var dt))
            {
                expectedDelivery = dt;
            }

            return new CarrierCreateShipmentResult(
                TrackingCode: ghnResponse.Data.OrderCode,
                ShippingFee: ghnResponse.Data.TotalFee,
                ExpectedDeliveryTime: expectedDelivery ?? DateTimeOffset.UtcNow.AddDays(2),
                InitialStatus: ShipmentStatus.ReadyToPick,
                LabelUrl: $"https://fulfillment.nexus.internal/labels/{ghnResponse.Data.OrderCode}.pdf");
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "GHN CreateOrder timed out.");
            throw new CarrierTimeoutException("GHN order creation timed out.", CarrierCode, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GHN CreateOrder network request failed.");
            throw new CarrierIntegrationException("GHN communication error.", CarrierCode, "HTTP_ERROR", ex);
        }
    }

    public async Task<CarrierStatusResult> GetStatusAsync(
        string trackingCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingCode);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/shiip/public-api/v2/shipping-order/detail",
                new { order_code = trackingCode },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("GHN GetStatus failed: {ErrorText}", errorText);
                throw new CarrierIntegrationException(
                    $"GHN GetStatus returned HTTP {response.StatusCode}.",
                    CarrierCode,
                    response.StatusCode.ToString());
            }

            var ghnResponse = await response.Content.ReadFromJsonAsync<GhnApiResponse<GhnOrderDetailData>>(cancellationToken: cancellationToken);

            if (ghnResponse == null || ghnResponse.Code != 200 || ghnResponse.Data == null)
            {
                var errMessage = ghnResponse?.Message ?? ghnResponse?.CodeMessage ?? "Order not found in GHN.";
                throw new CarrierIntegrationException(errMessage, CarrierCode, ghnResponse?.CodeMessage);
            }

            var mappedStatus = MapStatus(ghnResponse.Data.Status);
            DateTimeOffset? updatedAt = null;
            if (DateTimeOffset.TryParse(ghnResponse.Data.UpdatedDate, out var dt))
            {
                updatedAt = dt;
            }

            return new CarrierStatusResult(
                TrackingCode: ghnResponse.Data.OrderCode,
                Status: mappedStatus,
                RawStatus: ghnResponse.Data.Status,
                Description: $"Status: {ghnResponse.Data.Status}",
                UpdatedAt: updatedAt ?? DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CarrierTimeoutException("GHN tracking query timed out.", CarrierCode, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new CarrierIntegrationException("GHN tracking query communication error.", CarrierCode, "HTTP_ERROR", ex);
        }
    }

    public async Task<CarrierCancelResult> CancelAsync(
        string trackingCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingCode);

        try
        {
            var request = new GhnCancelOrderRequest
            {
                OrderCodes = new List<string> { trackingCode }
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/shiip/public-api/v2/switch-status/cancel",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("GHN Cancel failed: {ErrorText}", errorText);
                return new CarrierCancelResult(trackingCode, false, errorText);
            }

            var ghnResponse = await response.Content.ReadFromJsonAsync<GhnApiResponse<object>>(cancellationToken: cancellationToken);

            var isSuccess = ghnResponse != null && ghnResponse.Code == 200;
            return new CarrierCancelResult(
                trackingCode,
                isSuccess,
                ghnResponse?.Message ?? (isSuccess ? "Cancelled successfully." : "Failed to cancel order on GHN."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN Cancel request error for {TrackingCode}", trackingCode);
            return new CarrierCancelResult(trackingCode, false, ex.Message);
        }
    }

    public ShipmentStatus MapStatus(string rawCarrierStatus)
    {
        return GhnStatusMapper.Map(rawCarrierStatus);
    }
}
