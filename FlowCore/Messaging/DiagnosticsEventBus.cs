using System.Diagnostics;
using FlowCore.Core.Interfaces;
using FlowCore.Diagnostics;
using FlowCore.Execution;

namespace FlowCore.Messaging;

internal sealed class DiagnosticsEventBus : IEventBus
{
    private readonly IEventBus _inner;
    private readonly IActivityFactory _activityFactory;
    private readonly IMetricRecorder _metrics;

    public DiagnosticsEventBus(IEventBus inner, IActivityFactory activityFactory, IMetricRecorder metrics)
    {
        _inner = inner;
        _activityFactory = activityFactory;
        _metrics = metrics;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var eventName = typeof(TEvent).Name;
        var sw = Stopwatch.StartNew();

        var activity = _activityFactory.Start($"publish.{eventName}", ActivityKindType.Producer);
        var diagnostics = GetCurrentDiagnostics();

        diagnostics?.Write(new DiagnosticEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Severity = DiagnosticSeverity.Information,
            Category = DiagnosticsConstants.EventBus,
            Event = $"publish.{eventName}.started",
            Message = $"Publishing {eventName}"
        });

        try
        {
            activity?.SetTag("event.type", eventName);
            activity?.SetTag("event.fullname", typeof(TEvent).FullName);

            await _inner.PublishAsync(@event, cancellationToken);

            sw.Stop();
            _metrics.RecordCounter($"publish.{eventName}.success");
            _metrics.RecordDuration($"publish.{eventName}.duration", sw.Elapsed);

            diagnostics?.Write(new DiagnosticEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Severity = DiagnosticSeverity.Information,
                Category = DiagnosticsConstants.EventBus,
                Event = $"publish.{eventName}.success",
                Message = $"Published {eventName} in {sw.ElapsedMilliseconds}ms",
                Metadata = new() { ["duration_ms"] = sw.ElapsedMilliseconds }
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            activity?.SetException(ex);
            _metrics.RecordCounter($"publish.{eventName}.failure");
            _metrics.RecordDuration($"publish.{eventName}.duration", sw.Elapsed);

            diagnostics?.Write(new DiagnosticEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticsConstants.EventBus,
                Event = $"publish.{eventName}.failure",
                Message = $"Failed to publish {eventName}",
                Exception = ex,
                Metadata = new() { ["duration_ms"] = sw.ElapsedMilliseconds }
            });
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    public async Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        var eventName = @event.GetType().Name;
        var sw = Stopwatch.StartNew();

        var activity = _activityFactory.Start($"publish.{eventName}", ActivityKindType.Producer);
        var diagnostics = GetCurrentDiagnostics();

        diagnostics?.Write(new DiagnosticEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Severity = DiagnosticSeverity.Information,
            Category = DiagnosticsConstants.EventBus,
            Event = $"publish.{eventName}.started",
            Message = $"Publishing {eventName}"
        });

        try
        {
            activity?.SetTag("event.type", eventName);
            activity?.SetTag("event.fullname", @event.GetType().FullName);

            await _inner.PublishAsync(@event, cancellationToken);

            sw.Stop();
            _metrics.RecordCounter($"publish.{eventName}.success");
            _metrics.RecordDuration($"publish.{eventName}.duration", sw.Elapsed);

            diagnostics?.Write(new DiagnosticEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Severity = DiagnosticSeverity.Information,
                Category = DiagnosticsConstants.EventBus,
                Event = $"publish.{eventName}.success",
                Message = $"Published {eventName} in {sw.ElapsedMilliseconds}ms",
                Metadata = new() { ["duration_ms"] = sw.ElapsedMilliseconds }
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            activity?.SetException(ex);
            _metrics.RecordCounter($"publish.{eventName}.failure");
            _metrics.RecordDuration($"publish.{eventName}.duration", sw.Elapsed);

            diagnostics?.Write(new DiagnosticEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticsConstants.EventBus,
                Event = $"publish.{eventName}.failure",
                Message = $"Failed to publish {eventName}",
                Exception = ex,
                Metadata = new() { ["duration_ms"] = sw.ElapsedMilliseconds }
            });
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    private static IDiagnosticsContext? GetCurrentDiagnostics()
    {
        return ExecutionScope.Current?.Diagnostics;
    }
}