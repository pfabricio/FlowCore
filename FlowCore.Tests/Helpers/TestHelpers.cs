using FlowCore.Core;
using FlowCore.Core.Interfaces;
using Moq;
using Xunit;

namespace FlowCore.Tests.Helpers;

// Test commands
public record TestCommand(string Name) : ICommand<string>;
public record TestCommandNoReturn(string Name) : ICommand<Unit>;
public record FailingCommand : ICommand<string>;

// Test queries
public record TestQuery(string Id) : IQuery<string>;
public record TestCachableQuery(string Id) : IQuery<string>, ICachableQuery<string>
{
    public string CacheKey => $"test:{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}

// Test events
public record TestEvent(string Data) : IEvent;

// Test handlers
public class TestCommandHandler : ICommandHandler<TestCommand, string>
{
    public Task<string> HandleAsync(TestCommand command, CancellationToken ct = default)
    {
        return Task.FromResult($"Hello {command.Name}");
    }
}

public class TestCommandNoReturnHandler : ICommandHandler<TestCommandNoReturn, Unit>
{
    public Task<Unit> HandleAsync(TestCommandNoReturn command, CancellationToken ct = default)
    {
        return Task.FromResult(Unit.Value);
    }
}

public class FailingCommandHandler : ICommandHandler<FailingCommand, string>
{
    public Task<string> HandleAsync(FailingCommand command, CancellationToken ct = default)
    {
        throw new InvalidOperationException("Handler failed");
    }
}

public class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public Task<string> HandleAsync(TestQuery query, CancellationToken ct = default)
    {
        return Task.FromResult($"Result for {query.Id}");
    }
}

public class TestEventHandler : IEventHandler<TestEvent>
{
    public List<string> ReceivedEvents { get; } = new();

    public Task HandleAsync(TestEvent @event, CancellationToken ct = default)
    {
        ReceivedEvents.Add(@event.Data);
        return Task.CompletedTask;
    }
}

// Test behaviors
public class TestBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    public List<string> ExecutedBehaviors { get; } = new();
    public string BehaviorName { get; }

    public TestBehavior(string name)
    {
        BehaviorName = name;
    }

    public async Task<TResult> Handle(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        ExecutedBehaviors.Add(BehaviorName);
        return await next();
    }
}
