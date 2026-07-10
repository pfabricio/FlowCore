namespace FlowCore.Core.Interfaces;

public interface IHandlerResolver
{
    object GetHandler(Type requestType, Type responseType);
}
