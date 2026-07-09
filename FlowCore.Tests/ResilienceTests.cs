using FluentAssertions;
using FlowCore.Resilience;
using Xunit;

namespace FlowCore.Tests;

public class ResilienceTests
{
    [Fact]
    public async Task TimeoutPolicy_ShouldCompleteNormally()
    {
        var policy = new TimeoutPolicy(TimeSpan.FromSeconds(5));

        var result = await policy.ExecuteAsync(async ct =>
        {
            await Task.Delay(1, ct);
            return "ok";
        });

        result.Should().Be("ok");
    }

    [Fact]
    public async Task TimeoutPolicy_ShouldThrowOnTimeout()
    {
        var policy = new TimeoutPolicy(TimeSpan.FromMilliseconds(10));

        var act = async () => await policy.ExecuteAsync<string>(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return "never";
        });

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task CircuitBreakerPolicy_ShouldStartClosed()
    {
        var policy = new CircuitBreakerPolicy(3, TimeSpan.FromSeconds(30));

        policy.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task CircuitBreakerPolicy_ShouldOpenAfterFailures()
    {
        var policy = new CircuitBreakerPolicy(2, TimeSpan.FromSeconds(30));

        for (var i = 0; i < 2; i++)
        {
            var act = async () => await policy.ExecuteAsync<string>(_ => throw new InvalidOperationException("fail"));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        policy.State.Should().Be(CircuitState.Open);
    }

    [Fact]
    public async Task CircuitBreakerPolicy_ShouldRejectWhenOpen()
    {
        var policy = new CircuitBreakerPolicy(1, TimeSpan.FromSeconds(30));

        var act1 = async () => await policy.ExecuteAsync<string>(_ => throw new InvalidOperationException("fail"));
        await act1.Should().ThrowAsync<InvalidOperationException>();

        var act2 = async () => await policy.ExecuteAsync(_ => new ValueTask<string>("ok"));
        await act2.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CircuitBreakerPolicy_ShouldResetOnSuccess()
    {
        var policy = new CircuitBreakerPolicy(2, TimeSpan.FromSeconds(30));

        var act = async () => await policy.ExecuteAsync<string>(_ => throw new InvalidOperationException("fail"));
        await act.Should().ThrowAsync<InvalidOperationException>();

        var result = await policy.ExecuteAsync(_ => new ValueTask<string>("ok"));
        result.Should().Be("ok");
        policy.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task BulkheadPolicy_ShouldLimitConcurrency()
    {
        var policy = new BulkheadPolicy(1);

        policy.MaxConcurrency.Should().Be(1);
        policy.AvailableSlots.Should().Be(1);

        var result = await policy.ExecuteAsync(_ => new ValueTask<string>("ok"));

        result.Should().Be("ok");
    }

    [Fact]
    public void BulkheadPolicy_ShouldThrowOnInvalidMaxConcurrency()
    {
        var act = () => new BulkheadPolicy(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task BulkheadPolicy_ShouldWaitForSlot()
    {
        var policy = new BulkheadPolicy(1);

        var task1 = policy.ExecuteAsync(async ct =>
        {
            await Task.Delay(200, ct);
            return "first";
        });

        await Task.Delay(50);

        var task2 = policy.ExecuteAsync(_ => new ValueTask<string>("second"));

        var result = await task2;
        result.Should().Be("second");
    }

    [Fact]
    public async Task FallbackPolicy_ShouldExecuteFallbackOnFailure()
    {
        var fallbackCalled = false;
        var policy = new FallbackPolicy(() => fallbackCalled = true);

        var act = async () => await policy.ExecuteAsync<string>(_ => throw new InvalidOperationException("fail"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        fallbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task FallbackPolicy_ShouldNotCallFallbackOnSuccess()
    {
        var fallbackCalled = false;
        var policy = new FallbackPolicy(() => fallbackCalled = true);

        var result = await policy.ExecuteAsync(_ => new ValueTask<string>("ok"));

        result.Should().Be("ok");
        fallbackCalled.Should().BeFalse();
    }

    [Fact]
    public async Task FallbackPolicy_WithWhenPredicate_ShouldOnlyMatchFilteredExceptions()
    {
        var fallbackCalled = false;
        var policy = new FallbackPolicy(
            _ => { fallbackCalled = true; return default; },
            ex => ex is InvalidOperationException);

        var act = async () => await policy.ExecuteAsync<string>(_ => throw new ArgumentException("no match"));

        await act.Should().ThrowAsync<ArgumentException>();
        fallbackCalled.Should().BeFalse();
    }

    [Fact]
    public async Task RateLimiterPolicy_ShouldAllowRequestsUnderLimit()
    {
        var policy = new RateLimiterPolicy(5, TimeSpan.FromSeconds(1));

        for (var i = 0; i < 5; i++)
        {
            var result = await policy.ExecuteAsync(_ => new ValueTask<string>($"r{i}"));
            result.Should().Be($"r{i}");
        }
    }

    [Fact]
    public void RateLimiterPolicy_ShouldThrowOnInvalidMaxRequests()
    {
        var act = () => new RateLimiterPolicy(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task PolicyComposer_ShouldChainPolicies()
    {
        var fallbackCalled = false;
        var timeout = new TimeoutPolicy(TimeSpan.FromSeconds(5));
        var circuitBreaker = new CircuitBreakerPolicy(1, TimeSpan.FromSeconds(30));
        var fallback = new FallbackPolicy(() => fallbackCalled = true);
        var composer = new PolicyComposer(timeout, circuitBreaker, fallback);

        var act = async () => await composer.ExecuteAsync<string>(_ => throw new InvalidOperationException("fail"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        fallbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task PolicyComposer_WithNoPolicies_ShouldExecuteDirectly()
    {
        var composer = new PolicyComposer();

        var result = await composer.ExecuteAsync(_ => new ValueTask<string>("direct"));

        result.Should().Be("direct");
    }
}
