namespace FlowCore.Execution;

public interface IExecutionItems
{
    void Set<T>(string key, T value);
    bool TryGet<T>(string key, out T? value);
    bool Remove(string key);
}
