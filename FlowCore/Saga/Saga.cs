namespace FlowCore.Saga;

public abstract class Saga
{
    private readonly List<SagaStep> _steps = new();

    public string Name => GetType().Name;
    public IReadOnlyList<SagaStep> Steps => _steps.AsReadOnly();

    protected void AddStep<TEvent>(
        string name,
        Func<TEvent, CancellationToken, Task> execute,
        Func<TEvent, CancellationToken, Task>? compensate = null)
    {
        _steps.Add(new SagaStep(
            name,
            typeof(TEvent),
            (evt, ct) => execute((TEvent)evt, ct),
            compensate is not null ? (evt, ct) => compensate((TEvent)evt, ct) : null));
    }

    public abstract Task DefineStepsAsync();
}