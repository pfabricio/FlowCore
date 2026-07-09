using FluentAssertions;
using FlowCore.Diagnostics;
using Xunit;

namespace FlowCore.Tests;

public class MetricsContextTests
{
    [Fact]
    public void MetricsContext_ShouldRecordEntries()
    {
        var context = new MetricsContext();

        context.Record(new MetricEntry("test", MetricType.Counter, 1));

        context.Entries.Should().HaveCount(1);
        context.Entries.Should().Contain(e => e.Name == "test");
    }

    [Fact]
    public void MetricsContext_ShouldRecordCounter()
    {
        var context = new MetricsContext();

        context.RecordCounter("requests", 5);

        var entry = context.Entries.Should().ContainSingle().Subject;
        entry.Name.Should().Be("requests");
        entry.Type.Should().Be(MetricType.Counter);
        entry.Value.Should().Be(5);
    }

    [Fact]
    public void MetricsContext_ShouldRecordDuration()
    {
        var context = new MetricsContext();

        context.RecordDuration("operation", TimeSpan.FromMilliseconds(150));

        var entry = context.Entries.Should().ContainSingle().Subject;
        entry.Name.Should().Be("operation");
        entry.Type.Should().Be(MetricType.Timer);
        entry.Unit.Should().Be("ms");
        entry.Value.Should().Be(150);
    }

    [Fact]
    public void MetricsContext_ShouldRecordGauge()
    {
        var context = new MetricsContext();

        context.RecordGauge("connections", 42);

        var entry = context.Entries.Should().ContainSingle().Subject;
        entry.Name.Should().Be("connections");
        entry.Type.Should().Be(MetricType.Gauge);
        entry.Value.Should().Be(42);
    }

    [Fact]
    public void MetricsContext_ShouldAccumulateMultipleEntries()
    {
        var context = new MetricsContext();

        context.RecordCounter("a");
        context.RecordCounter("b", 2);
        context.RecordCounter("a");

        context.Entries.Should().HaveCount(3);
    }

    [Fact]
    public void NullMetricsContext_ShouldNeverThrow()
    {
        var instance = NullMetricsContext.Instance;

        instance.Record(new MetricEntry("x", MetricType.Counter, 1));
        instance.Entries.Should().BeEmpty();
    }

    [Fact]
    public void MetricEntry_ShouldUseUtcNowByDefault()
    {
        var before = DateTimeOffset.UtcNow;
        var entry = new MetricEntry("test", MetricType.Counter, 1);
        var after = DateTimeOffset.UtcNow;

        entry.Timestamp.Should().BeOnOrAfter(before);
        entry.Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void MetricEntry_ShouldAcceptCustomTimestampAndTags()
    {
        var ts = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var tags = new Dictionary<string, string> { { "env", "test" } };

        var entry = new MetricEntry("test", MetricType.Counter, 1, "count", ts, tags);

        entry.Timestamp.Should().Be(ts);
        entry.Tags.Should().ContainKey("env").WhoseValue.Should().Be("test");
    }
}


