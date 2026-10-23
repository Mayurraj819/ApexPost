namespace ApexPost.Web.Configuration;

public sealed class WebSecurityOptions
{
    public bool MaintenanceMode { get; init; }
    public int HstsMaxAgeDays { get; init; } = 180;
    public string[] AllowedCorsOrigins { get; init; } = [];
    public string[] IntegrationApiKeys { get; init; } = [];
    public string[] IntegrationAllowedIpAddresses { get; init; } = [];
    public int IntegrationRateLimitPermitLimit { get; init; } = 60;
    public int IntegrationRateLimitWindowSeconds { get; init; } = 60;
    public int LoginRateLimitPermitLimit { get; init; } = 5;
    public int LoginRateLimitWindowSeconds { get; init; } = 60;
}
