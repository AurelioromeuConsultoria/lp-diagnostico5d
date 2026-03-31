namespace Diagnostico5D.API.Configuration;

public class EvolutionApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public string CodigoPaisPadrao { get; set; } = "55";
    public int DelayMs { get; set; } = 0;
}
