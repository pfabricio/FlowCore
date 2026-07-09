using System.Reflection;
using FlowCore.Core;
using FlowCore.Core.Interfaces;

namespace FlowCore.Discovery;

internal sealed class HandlerDiscovery
{
    private static readonly Type CommandHandlerType = typeof(ICommandHandler<,>);
    private static readonly Type QueryHandlerType = typeof(IQueryHandler<,>);
    private static readonly Type EventHandlerType = typeof(IEventHandler<>);

    public HandlerRegistry Discover(params Assembly[] assemblies)
    {
        var registry = new HandlerRegistry();
        var types = assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .ToList();

        foreach (var type in types)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType)
                {
                    var openGeneric = iface.GetGenericTypeDefinition();

                    if (openGeneric == CommandHandlerType)
                    {
                        var args = iface.GetGenericArguments();
                        registry.Register(new HandlerDescriptor
                        {
                            HandlerType = type,
                            RequestType = args[0],
                            ResponseType = args[1] == typeof(Unit) ? null : args[1],
                            Kind = HandlerKind.Command
                        });
                    }
                    else if (openGeneric == QueryHandlerType)
                    {
                        var args = iface.GetGenericArguments();
                        registry.Register(new HandlerDescriptor
                        {
                            HandlerType = type,
                            RequestType = args[0],
                            ResponseType = args[1],
                            Kind = HandlerKind.Query
                        });
                    }
                    else if (openGeneric == EventHandlerType)
                    {
                        var eventType = iface.GetGenericArguments()[0];
                        registry.Register(new HandlerDescriptor
                        {
                            HandlerType = type,
                            RequestType = eventType,
                            Kind = HandlerKind.Event
                        });
                    }
                }
            }
        }

        Validate(registry);
        return registry;
    }

    private static void Validate(HandlerRegistry registry)
    {
        var duplicates = registry.Handlers
            .GroupBy(h => h.RequestType)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Count > 0)
        {
            var msgs = duplicates.Select(g =>
                $"Multiple handlers found for {g.Key.Name}: {string.Join(", ", g.Select(h => h.HandlerType.Name))}");
            throw new InvalidOperationException(
                $"Handler discovery validation failed:\n{string.Join("\n", msgs)}");
        }
    }
}
