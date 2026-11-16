namespace Fulfillment.Infrastructure.AI;

public class AiOptions
{
    public const string SectionName = "AiSettings";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemma-4-31b-it";
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public int TimeoutSeconds { get; set; } = 30;
}

