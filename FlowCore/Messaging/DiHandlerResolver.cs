using System.Diagnostics.CodeAnalysis;
using FlowCore.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FlowCore.Messaging;

[RequiresDynamicCode("Handler resolution uses MakeGenericType. Use Source Generators for AOT compatibility.")]
[RequiresUnreferencedCode("Handler resolution uses MakeGenericType. Use Source Generators for AOT compatibility.")]
internal sealed class DiHandlerResolver : IHandlerResolver
{
    private readonly IServiceProvider _serviceProvider;

    public DiHandlerResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object GetHandler(Type requestType, Type responseType)
    {
        if (typeof(IQuery<>).MakeGenericType(responseType).IsAssignableFrom(requestType)
            || requestType.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)))
        {
            var handlerType = typeof(IQueryHandler<,>).MakeGenericType(requestType, responseType);
            return _serviceProvider.GetRequiredService(handlerType);
        }

        var commandHandlerType = typeof(ICommandHandler<,>).MakeGenericType(requestType, responseType);
        return _serviceProvider.GetRequiredService(commandHandlerType);
    }
}
