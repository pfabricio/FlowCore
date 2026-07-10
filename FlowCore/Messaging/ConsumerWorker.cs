using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FlowCore.Core.Interfaces;
using FlowCore.Diagnostics;
using FlowCore.Execution;

namespace FlowCore.Messaging;

public abstract class ConsumerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageSerializer _serializer;

    protected ConsumerWorker(IServiceProvider serviceProvider, IMessageSerializer serializer)
    {
        _serviceProvider = serviceProvider;
        _serializer = serializer;
    }

    protected abstract Task ExecuteConsumeAsync(CancellationToken stoppingToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ExecuteConsumeAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            var logger = _serviceProvider.GetService<ILogger<ConsumerWorker>>();
            logger?.LogError(ex, "Consumer worker {WorkerType} failed unexpectedly", GetType().Name);
            throw;
        }
    }

    protected async Task ProcessEnvelopeAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(envelope.EventType) || envelope.Payload.Length == 0)
            return;

        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetService<IInboxStore>();
        var activityFactory = scope.ServiceProvider.GetRequiredService<IActivityFactory>();

        if (store is not null && await store.ExistsAsync(envelope.MessageId, cancellationToken))
        {
            GetCurrentDiagnostics()?.Write(new DiagnosticEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Severity = DiagnosticSeverity.Debug,
                Category = DiagnosticsConstants.EventBus,
                Event = $"consume.{envelope.EventType}.duplicated",
                Message = $"Message {envelope.MessageId} already processed, skipped"
            });
            return;
        }

        var activity = activityFactory.Start(
            $"consume.{envelope.EventType}",
            ActivityKindType.Consumer,
            envelope.CorrelationId.ToString());

        var diagnostics = GetCurrentDiagnostics();
        diagnostics?.Write(new DiagnosticEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Severity = DiagnosticSeverity.Information,
            Category = DiagnosticsConstants.EventBus,
            Event = $"consume.{envelope.EventType}.started",
            Message = $"Consuming {envelope.EventType}",
            Metadata = new()
            {
                ["message_id"] = envelope.MessageId.ToString(),
                ["correlation_id"] = envelope.CorrelationId.ToString()
            }
        });

        try
        {
            activity?.SetTag("event.type", envelope.EventType);
            activity?.SetTag("message.id", envelope.MessageId.ToString());
            activity?.SetTag("correlation.id", envelope.CorrelationId.ToString());

            using var innerScope = _serviceProvider.CreateScope();
            var eventBus = innerScope.ServiceProvider.GetRequiredService<IEventBus>();
            var serializer = innerScope.ServiceProvider.GetRequiredService<IMessageSerializer>();

            var eventType = EventTypeResolver.Resolve(envelope.EventType);
            var eventObj = serializer.Deserialize(eventType, envelope.Payload);

            await eventBus.PublishAsync((IEvent)eventObj, cancellationToken);

            if (store is not null)
            {
                await store.StoreAsync(new InboxMessage
                {
                    MessageId = envelope.MessageId,
                    EventType = envelope.EventType,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    Consumer = GetType().Name,
                    CorrelationId = envelope.CorrelationId
                }, cancellationToken);
            }

            diagnostics?.Write(new DiagnosticEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Severity = DiagnosticSeverity.Information,
                Category = DiagnosticsConstants.EventBus,
                Event = $"consume.{envelope.EventType}.success",
                Message = $"Consumed {envelope.EventType}"
            });
        }
        catch (Exception ex)
        {
            activity?.SetException(ex);

            diagnostics?.Write(new DiagnosticEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticsConstants.EventBus,
                Event = $"consume.{envelope.EventType}.failure",
                Message = $"Failed to consume {envelope.EventType}",
                Exception = ex,
                Metadata = new() { ["message_id"] = envelope.MessageId.ToString() }
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