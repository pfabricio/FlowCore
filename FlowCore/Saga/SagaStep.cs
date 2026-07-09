namespace FlowCore.Saga;

public class SagaStep<TEvent>
{
    public string Name { get; }
    public Func<TEvent, CancellationToken, Task> Execute { get; }
    public Func<TEvent, CancellationToken, Task>? Compensate { get; }

    public SagaStep(string name, Func<TEvent, CancellationToken, Task> execute, Func<TEvent, CancellationToken, Task>? compensate = null)
    {
        Name = name;
        Execute = execute;
        Compensate = compensate;
    }
}

public class SagaStep
{
    public string Name { get; }
    public Type EventType { get; }
    public Func<object, CancellationToken, Task> Execute { get; }
    public Func<object, CancellationToken, Task>? Compensate { get; }

    public SagaStep(string name, Type eventType, Func<object, CancellationToken, Task> execute, Func<object, CancellationToken, Task>? compensate = null)
    {
        Name = name;
        EventType = eventType;
        Execute = execute;
        Compensate = compensate;
    }
}