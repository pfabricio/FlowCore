using FluentAssertions;
using FlowCore.Core.Interfaces;
using FlowCore.Testing;
using FlowCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlowCore.Tests;

public class TestingInfrastructureTests
{
    [Fact]
    public void FakeEventBus_ShouldRecordPublishedEvents()
    {
        var bus = new FakeEventBus();

        bus.PublishAsync(new TestEvent("data1"));

        bus.Published.Should().HaveCount(1);
    }

    [Fact]
    public void FakeEventBus_ShouldFilterByType()
    {
        var bus = new FakeEventBus();

        bus.PublishAsync(new TestEvent("data1"));
        bus.PublishAsync(new TestEvent("data2"));

        var events = bus.PublishedOfType<TestEvent>();
        events.Should().HaveCount(2);
    }

    [Fact]
    public void FakeEventBus_Clear_ShouldRemoveAllEvents()
    {
        var bus = new FakeEventBus();

        bus.PublishAsync(new TestEvent("data"));
        bus.Clear();

        bus.Published.Should().BeEmpty();
    }

    [Fact]
    public void FakeClock_ShouldReturnInitialTime()
    {
        var initial = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(initial);

        clock.UtcNow.Should().Be(initial);
    }

    [Fact]
    public void FakeClock_Advance_ShouldMoveTime()
    {
        var clock = new FakeClock();

        var before = clock.UtcNow;
        clock.Advance(TimeSpan.FromHours(2));
        var after = clock.UtcNow;

        (after - before).Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void FakeClock_Set_ShouldSetExactTime()
    {
        var clock = new FakeClock();
        var target = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);

        clock.Set(target);

        clock.UtcNow.Should().Be(target);
    }

    [Fact]
    public void CreateTestBuilder_ShouldRegisterFakeEventBus()
    {
        var services = new ServiceCollection();

        var builder = services.CreateTestBuilder();

        var provider = builder.Build();

        var fakeBus = provider.GetFakeEventBus();
        fakeBus.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateTestBuilder_WithFakeBus_ShouldCapturePublishedEvents()
    {
        var services = new ServiceCollection();
        services.AddScoped<IEventHandler<TestEvent>, TestEventHandler>();
        var builder = services.CreateTestBuilder();
        var provider = builder.Build();

        var mediator = provider.GetRequiredService<IFlowMediator>();
        await mediator.PublishAsync(new TestEvent("captured"));

        var fakeBus = provider.GetFakeEventBus();
        fakeBus.PublishedOfType<TestEvent>().Should().HaveCount(1);
    }
}
