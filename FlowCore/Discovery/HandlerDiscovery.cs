using System.Diagnostics.CodeAnalysis;
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
        var generated = TryLoadGeneratedRegistry();
        if (generated is not null)
            return generated;

        return DiscoverWithReflection(assemblies);
    }

    private static HandlerRegistry? TryLoadGeneratedRegistry()
    {
        try
        {
            var generatedType = Type.GetType(
                "FlowCore.Generated.GeneratedHandlerRegistry, FlowCore",
                throwOnError: false);

            if (generatedType is null)
                return null;

            var handlersProp = generatedType.GetProperty("Handlers",
                BindingFlags.Static | BindingFlags.Public);

            if (handlersProp?.GetValue(null) is IReadOnlyList<HandlerDescriptor> descriptors)
            {
                var registry = new HandlerRegistry();
                foreach (var descriptor in descriptors)
                {
                    registry.Register(descriptor);
                }
                return registry;
            }
        }
        catch
        {
        }

        return null;
    }

    [RequiresUnreferencedCode("Handler discovery uses reflection. Use Source Generators for AOT compatibility.")]
    [RequiresDynamicCode("Handler discovery uses reflection. Use Source Generators for AOT compatibility.")]
    private static HandlerRegistry DiscoverWithReflection(params Assembly[] assemblies)
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
