namespace FlowCore.Configuration;

public class FlowCoreOptions
{
    public bool EnableDiagnostics { get; set; }
    public bool EnableRetryPolicies { get; set; } = true;
    public bool RegisterDefaultBehaviors { get; set; } = true;
    public TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxRetryAttempts { get; set; } = 3;
}
