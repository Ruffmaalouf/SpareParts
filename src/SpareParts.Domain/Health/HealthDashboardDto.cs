namespace SpareParts.Domain.Health;

public sealed record HealthDashboardDto
{
    public string Status { get; init; } = "ok";
    public DateTime Utc { get; init; }
    public string Service { get; init; } = string.Empty;
    public IReadOnlyList<string> Capabilities { get; init; } = [];
    public IReadOnlyList<SplitApiHealthDto> SplitApis { get; init; } = [];
    public DatabaseHealthDto Database { get; init; } = new();
    public MigrationsHealthDto Migrations { get; init; } = new();
    public ClientConfigHealthDto ClientConfig { get; init; } = new();
    public NotificationHealthDto Notifications { get; init; } = new();
}

public sealed record SplitApiHealthDto
{
    public string Service { get; init; } = string.Empty;
    public IReadOnlyList<string> Capabilities { get; init; } = [];
    public string HealthEndpoint { get; init; } = "/api/health";
    public string Status { get; init; } = "expected";
}

public sealed record DatabaseHealthDto
{
    public string Status { get; init; } = "unknown";
    public bool CanConnect { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string? Error { get; init; }
}

public sealed record MigrationsHealthDto
{
    public string Status { get; init; } = "configured";
    public int Count { get; init; }
    public IReadOnlyList<string> Names { get; init; } = [];
}

public sealed record ClientConfigHealthDto
{
    public string Status { get; init; } = "configured";
    public string WebDefaultApiBaseUrl { get; init; } = "http://localhost:5000";
    public string MobileAndroidDefaultApiBaseUrl { get; init; } = "http://10.0.2.2:5000";
    public string MobileIosDefaultApiBaseUrl { get; init; } = "http://localhost:5000";
    public string MobileEnvironmentVariable { get; init; } = "EXPO_PUBLIC_API_BASE_URL";
    public IReadOnlyList<string> CorsAllowedOrigins { get; init; } = [];
}

public sealed record NotificationHealthDto
{
    public string Status { get; init; } = "configured";
    public string HubPath { get; init; } = "/hubs/notifications";
    public bool SignalRRegistered { get; init; }
    public string DeliveryProvider { get; init; } = "Disabled";
    public bool WebhookConfigured { get; init; }
    public int TimeoutSeconds { get; init; }
}
