using FluentAssertions;
using FlowCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlowCore.Tests;

public class HealthCheckTests
{
    [Fact]
    public void HealthCheckResult_Healthy_ShouldSetStatus()
    {
        var result = HealthCheckResult.Healthy("test", "all good");

        result.Name.Should().Be("test");
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("all good");
        result.Exception.Should().BeNull();
        result.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void HealthCheckResult_Degraded_ShouldSetStatus()
    {
        var ex = new InvalidOperationException("degraded");
        var result = HealthCheckResult.Degraded("db", "slow response", ex);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Exception.Should().Be(ex);
    }

    [Fact]
    public void HealthCheckResult_Unhealthy_ShouldSetStatus()
    {
        var ex = new InvalidOperationException("down");
        var result = HealthCheckResult.Unhealthy("db", "connection lost", ex);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().Be(ex);
    }

    [Fact]
    public void HealthCheckRegistry_ShouldReturnRegisteredChecks()
    {
        var check1 = new FakeHealthCheck();
        var check2 = new FakeHealthCheck();
        var registry = new HealthCheckRegistry([check1, check2]);

        registry.HealthChecks.Should().HaveCount(2);
    }

    [Fact]
    public void AddHealthCheck_ShouldRegisterInDi()
    {
        var services = new ServiceCollection();
        var builder = new FlowCoreBuilder(services);

        builder.AddHealthCheck<FakeHealthCheck>();

        var provider = services.BuildServiceProvider();
        var checks = provider.GetServices<IHealthCheck>();
        checks.Should().Contain(c => c is FakeHealthCheck);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnResult()
    {
        var check = new FakeHealthCheck();

        var result = await check.CheckAsync();

        result.Status.Should().Be(HealthStatus.Healthy);
    }
}

public class FakeHealthCheck : IHealthCheck
{
    public ValueTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        => new(HealthCheckResult.Healthy("fake"));
}
