using FluentAssertions;
using FlowCore.Core.Interfaces;
using FlowCore.Messaging;
using FlowCore.Scheduling;
using FlowCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace FlowCore.Tests;

public class SchedulingTests
{
    private static async Task<List<ScheduledMessage>> GetDueMessagesAsync(IScheduledMessageStore store, DateTimeOffset utcNow)
    {
        var result = new List<ScheduledMessage>();
        await foreach (var msg in store.GetDueMessagesAsync(utcNow))
        {
            result.Add(msg);
        }
        return result;
    }

    [Fact]
    public async Task ScheduleAsync_ShouldSaveMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        var serializer = new SystemTextJsonSerializer();
        var scheduler = new MessageScheduler(store, serializer);

        var executeAt = DateTimeOffset.UtcNow.AddHours(1);
        await scheduler.ScheduleAsync(new TestEvent("scheduled"), executeAt);

        var due = await GetDueMessagesAsync(store, DateTimeOffset.UtcNow.AddHours(2));
        due.Should().ContainSingle();
        due[0].EventType.Should().Be(nameof(TestEvent));
    }

    [Fact]
    public async Task ScheduleAsync_WithPastDate_ShouldBeDueImmediately()
    {
        var store = new InMemoryScheduledMessageStore();
        var serializer = new SystemTextJsonSerializer();
        var scheduler = new MessageScheduler(store, serializer);

        var executeAt = DateTimeOffset.UtcNow.AddHours(-1);
        await scheduler.ScheduleAsync(new TestEvent("overdue"), executeAt);

        var due = await GetDueMessagesAsync(store, DateTimeOffset.UtcNow);
        due.Should().ContainSingle();
    }

    [Fact]
    public async Task ScheduleAfterAsync_ShouldScheduleWithDelay()
    {
        var store = new InMemoryScheduledMessageStore();
        var serializer = new SystemTextJsonSerializer();
        var scheduler = new MessageScheduler(store, serializer);

        await scheduler.ScheduleAfterAsync(new TestEvent("delayed"), TimeSpan.FromMinutes(30));

        var due = await GetDueMessagesAsync(store, DateTimeOffset.UtcNow.AddMinutes(31));
        due.Should().ContainSingle();
    }

    [Fact]
    public async Task ScheduleAfterAsync_ShouldNotBeDueBeforeDelay()
    {
        var store = new InMemoryScheduledMessageStore();
        var serializer = new SystemTextJsonSerializer();
        var scheduler = new MessageScheduler(store, serializer);

        await scheduler.ScheduleAfterAsync(new TestEvent("not-yet"), TimeSpan.FromMinutes(60));

        var due = await GetDueMessagesAsync(store, DateTimeOffset.UtcNow);
        due.Should().BeEmpty();
    }

    [Fact]
    public async Task ScheduleAsync_MessageShouldSerializePayload()
    {
        var store = new InMemoryScheduledMessageStore();
        var serializer = new SystemTextJsonSerializer();
        var scheduler = new MessageScheduler(store, serializer);

        await scheduler.ScheduleAsync(new TestEvent("payload-test"), DateTimeOffset.UtcNow.AddDays(1));

        var due = await GetDueMessagesAsync(store, DateTimeOffset.UtcNow.AddDays(2));
        due.Should().ContainSingle();
        due[0].Payload.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MarkAsPublishedAsync_ShouldRemoveFromDue()
    {
        var store = new InMemoryScheduledMessageStore();
        var serializer = new SystemTextJsonSerializer();
        var scheduler = new MessageScheduler(store, serializer);

        await scheduler.ScheduleAsync(new TestEvent("publish"), DateTimeOffset.UtcNow.AddMinutes(5));

        var due = await GetDueMessagesAsync(store, DateTimeOffset.UtcNow.AddMinutes(10));
        var msg = due.First();
        await store.MarkAsPublishedAsync(msg.Id);

        var afterDue = await GetDueMessagesAsync(store, DateTimeOffset.UtcNow.AddMinutes(15));
        afterDue.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldRemoveFromDue()
    {
        var store = new InMemoryScheduledMessageStore();
        var serializer = new SystemTextJsonSerializer();
        var scheduler = new MessageScheduler(store, serializer);

        await scheduler.ScheduleAsync(new TestEvent("fail"), DateTimeOffset.UtcNow.AddMinutes(5));

        var due = await GetDueMessagesAsync(store, DateTimeOffset.UtcNow.AddMinutes(10));
        var msg = due.First();
        await store.MarkAsFailedAsync(msg.Id);

        var afterDue = await GetDueMessagesAsync(store, DateTimeOffset.UtcNow.AddMinutes(15));
        afterDue.Should().BeEmpty();
    }

    [Fact]
    public async Task SchedulerWorker_ShouldNotThrow_WhenNoDueMessages()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IScheduledMessageStore, InMemoryScheduledMessageStore>();
        services.AddSingleton<IEventBus>(_ => new Mock<IEventBus>().Object);
        services.AddSingleton<IMessageSerializer, SystemTextJsonSerializer>();
        var provider = services.BuildServiceProvider();

        using var cts = new CancellationTokenSource(500);
        var worker = new SchedulerWorker(provider);

        await worker.StartAsync(cts.Token);
        await worker.StopAsync(default);

        // If we reach here without exception, the test passes
        Assert.True(true);
    }
}
