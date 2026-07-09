namespace FlowCore.Diagnostics;

public interface IActivityScope : IDisposable
{
    void SetTag(string key, object? value);
    void SetException(Exception exception);
}