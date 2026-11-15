namespace Fulfillment.Infrastructure.AI;

public class AiOptions
{
    public const string SectionName = "AiSettings";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-flash";
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public int TimeoutSeconds { get; set; } = 30;
}

