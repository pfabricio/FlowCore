using System.Reflection;

namespace FlowCore.Messaging;

public static class EventTypeResolver
{
    private static readonly Dictionary<string, Type> Cache = new();
    private static readonly object Lock = new();

    public static void Warmup(IEnumerable<Assembly> assemblies)
    {
        lock (Lock)
        {
            foreach (var asm in assemblies)
            {
                foreach (var t in asm.GetExportedTypes())
                {
                    if (!Cache.ContainsKey(t.Name))
                        Cache[t.Name] = t;
                }
            }
        }
    }

    public static Type Resolve(string eventTypeName)
    {
        lock (Lock)
        {
            if (Cache.TryGetValue(eventTypeName, out var type))
                return type;

            type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetExportedTypes())
                .FirstOrDefault(t => t.Name == eventTypeName || t.FullName == eventTypeName);

            if (type is null)
                throw new InvalidOperationException($"Event type '{eventTypeName}' not found.");

            Cache[eventTypeName] = type;
            return type;
        }
    }

    public static void Clear()
    {
        lock (Lock)
        {
            Cache.Clear();
        }
    }
}