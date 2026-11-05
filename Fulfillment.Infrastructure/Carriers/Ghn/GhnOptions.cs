namespace Fulfillment.Infrastructure.Carriers.Ghn;

public class GhnOptions
{
    public const string SectionName = "Carriers:GHN";

    public string Environment { get; set; } = "Staging";

    public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn";

    public string Token { get; set; } = string.Empty;

    public string ShopId { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;
}
