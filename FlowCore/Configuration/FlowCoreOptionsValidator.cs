using Microsoft.Extensions.Options;

namespace FlowCore.Configuration;

public class FlowCoreOptionsValidator : IValidateOptions<FlowCoreOptions>
{
    public ValidateOptionsResult Validate(string? name, FlowCoreOptions options)
    {
        if (options.MaxRetryAttempts < 0)
            return ValidateOptionsResult.Fail("MaxRetryAttempts must be >= 0");

        if (options.DefaultCacheExpiration <= TimeSpan.Zero)
            return ValidateOptionsResult.Fail("DefaultCacheExpiration must be positive");

        return ValidateOptionsResult.Success;
    }
}
