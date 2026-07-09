namespace FlowCore.Testing;

public sealed class FakeClock
{
    private DateTimeOffset _current;

    public DateTimeOffset UtcNow => _current;

    public FakeClock(DateTimeOffset? initialTime = null)
    {
        _current = initialTime ?? DateTimeOffset.UtcNow;
    }

    public void Advance(TimeSpan duration)
    {
        _current = _current.Add(duration);
    }

    public void Set(DateTimeOffset value)
    {
        _current = value;
    }
}
