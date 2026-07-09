using FluentAssertions;
using FlowCore.Core.Interfaces;
using FlowCore.Messaging;
using Moq;
using Xunit;

namespace FlowCore.Tests;

public class RetryTests
{
    [Fact]
    public async Task ImmediateRetryPolicy_FirstAttempt_ShouldRetry()
    {
        var policy = new ImmediateRetryPolicy(maxAttempts: 3);
        var context = new RetryContext
        {
            Attempt = 1,
            Exception = new InvalidOperationException("fail"),
            EventType = "TestEvent"
        };

        var decision = await policy.EvaluateAsync(context);
        decision.Action.Should().Be(RetryAction.Retry);
    }

    [Fact]
    public async Task ImmediateRetryPolicy_LastAttempt_ShouldStop()
    {
        var policy = new ImmediateRetryPolicy(maxAttempts: 3);
        var context = new RetryContext
        {
            Attempt = 3,
            Exception = new InvalidOperationException("fail"),
            EventType = "TestEvent"
        };

        var decision = await policy.EvaluateAsync(context);
        decision.Action.Should().Be(RetryAction.Stop);
    }

    [Fact]
    public async Task ImmediateRetryPolicy_ExceedsMaxAttempts_ShouldStop()
    {
        var policy = new ImmediateRetryPolicy(maxAttempts: 3);
        var context = new RetryContext
        {
            Attempt = 5,
            Exception = new InvalidOperationException("fail"),
            EventType = "TestEvent"
        };

        var decision = await policy.EvaluateAsync(context);
        decision.Action.Should().Be(RetryAction.Stop);
    }

    [Fact]
    public async Task RetryHandler_ShouldRetry_WhenPolicySaysRetry()
    {
        var policy = new ImmediateRetryPolicy(maxAttempts: 3);
        var handler = new RetryHandler(policy);

        var context = new RetryContext
        {
            Attempt = 1,
            Exception = new InvalidOperationException("retry"),
            EventType = "TestEvent"
        };

        var shouldRetry = await handler.ShouldRetryAsync(context);
        shouldRetry.Should().BeTrue();
    }

    [Fact]
    public async Task RetryHandler_ShouldStop_WhenAttemptsExhausted()
    {
        var policy = new ImmediateRetryPolicy(maxAttempts: 3);
        var dlq = new Mock<IDeadLetterWriter>();
        var handler = new RetryHandler(policy, dlq.Object);

        var context = new RetryContext
        {
            Attempt = 3,
            Exception = new InvalidOperationException("exhausted"),
            EventType = "TestEvent",
            MessageId = Guid.NewGuid()
        };

        var shouldRetry = await handler.ShouldRetryAsync(context);
        shouldRetry.Should().BeFalse();

        dlq.Verify(x => x.WriteAsync(
            It.Is<DeadLetterContext>(c => c.RetryCount == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetryHandler_WithoutDLQ_ShouldNotThrow()
    {
        var policy = new ImmediateRetryPolicy(maxAttempts: 1);
        var handler = new RetryHandler(policy);

        var context = new RetryContext
        {
            Attempt = 1,
            Exception = new InvalidOperationException("no-dlq"),
            EventType = "TestEvent",
            MessageId = Guid.NewGuid()
        };

        var shouldRetry = await handler.ShouldRetryAsync(context);
        shouldRetry.Should().BeFalse();
    }

    [Fact]
    public async Task InMemoryDeadLetterWriter_ShouldStoreContext()
    {
        var writer = new InMemoryDeadLetterWriter();

        var context = new DeadLetterContext
        {
            Envelope = new MessageEnvelope
            {
                MessageId = Guid.NewGuid(),
                EventType = "TestEvent",
                Timestamp = DateTimeOffset.UtcNow
            },
            Exception = new InvalidOperationException("dlq-test"),
            RetryCount = 3,
            FailedAt = DateTimeOffset.UtcNow
        };

        await writer.WriteAsync(context);

        var allMessages = writer.GetMessages();
        allMessages.Should().ContainSingle();
        allMessages[0].RetryCount.Should().Be(3);
        allMessages[0].Exception!.Message.Should().Be("dlq-test");
    }

    [Fact]
    public async Task InMemoryDeadLetterWriter_ShouldStoreMultiple()
    {
        var writer = new InMemoryDeadLetterWriter();

        await writer.WriteAsync(new DeadLetterContext
        {
            Envelope = new MessageEnvelope { MessageId = Guid.NewGuid(), EventType = "Event1" },
            RetryCount = 1
        });

        await writer.WriteAsync(new DeadLetterContext
        {
            Envelope = new MessageEnvelope { MessageId = Guid.NewGuid(), EventType = "Event2" },
            RetryCount = 2
        });

        var allMessages = writer.GetMessages();
        allMessages.Should().HaveCount(2);
    }

    [Fact]
    public void RetryContext_DefaultValues_ShouldBeSet()
    {
        var context = new RetryContext();
        context.Attempt.Should().Be(0);
        context.MessageId.Should().BeEmpty();
    }

    [Fact]
    public async Task RetryHandler_WithDelayInPolicy_ShouldDelay()
    {
        var mockPolicy = new Mock<IRetryPolicy>();
        mockPolicy
            .Setup(x => x.EvaluateAsync(It.IsAny<RetryContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RetryDecision { Action = RetryAction.Retry, Delay = TimeSpan.FromMilliseconds(1) });

        var handler = new RetryHandler(mockPolicy.Object);
        var context = new RetryContext
        {
            Attempt = 1,
            Exception = new InvalidOperationException("delay-test"),
            EventType = "TestEvent"
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var shouldRetry = await handler.ShouldRetryAsync(context);
        sw.Stop();

        shouldRetry.Should().BeTrue();
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(1);
    }
}
