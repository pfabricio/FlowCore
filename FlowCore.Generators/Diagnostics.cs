using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace FlowCore.Generators;

[Generator]
public sealed class HandlerDiagnostics : IIncrementalGenerator
{
    private const string CommandHandlerType = "FlowCore.Core.Interfaces.ICommandHandler`2";
    private const string QueryHandlerType = "FlowCore.Core.Interfaces.IQueryHandler`2";
    private const string EventHandlerType = "FlowCore.Core.Interfaces.IEventHandler`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var handlerInfos = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 },
                transform: static (ctx, _) => GetHandlerTypeInfo(ctx))
            .Where(static info => info is not null)
            .Collect();

        context.RegisterSourceOutput(handlerInfos, EmitDiagnostics);
    }

    private static HandlerTypeInfo? GetHandlerTypeInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var typeSymbol = semanticModel.GetDeclaredSymbol(classDecl);

        if (typeSymbol is null) return null;

        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (!iface.IsGenericType) continue;

            var openGeneric = iface.ConstructUnboundGenericType();
            var openGenericName = openGeneric.ToDisplayString();

            if (openGenericName is CommandHandlerType or QueryHandlerType or EventHandlerType)
            {
                return new HandlerTypeInfo(
                    typeSymbol.Name,
                    typeSymbol.ContainingType?.Name ?? typeSymbol.ContainingNamespace?.Name ?? "?",
                    iface.TypeArguments[0].Name,
                    openGenericName);
            }
        }

        return null;
    }

    private static void EmitDiagnostics(SourceProductionContext context, ImmutableArray<HandlerTypeInfo?> handlerInfos)
    {
        var list = handlerInfos.Where(h => h is not null).Cast<HandlerTypeInfo>().ToList();

        // Detect duplicate Command/Query handlers
        var duplicates = list
            .GroupBy(h => new { h.RequestName, h.InterfaceName })
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var dup in duplicates)
        {
            foreach (var h in dup)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "FC0001",
                            "Duplicate Handler",
                            $"Duplicate handler for {dup.Key.RequestName}: {h.TypeName}",
                            "FlowCore",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Location.None));
            }
        }

        // Check for Events without handlers
        var eventHandlers = new HashSet<string>(list
            .Where(h => h.InterfaceName == EventHandlerType)
            .Select(h => h.RequestName)
            .Distinct());

        var commandsWithEvents = list
            .Where(h => h.InterfaceName == CommandHandlerType)
            .Select(h => h.RequestName)
            .ToList();

        foreach (var cmd in commandsWithEvents)
        {
            // Convention: Command "CreateUser" may emit "UserCreated"
            var eventName = cmd.Replace("Create", "").Replace("Delete", "").Replace("Update", "") + "Event";
            if (eventName.Length > "Event".Length && !eventHandlers.Contains(eventName))
            {
                // Warning, not error - events are optional
            }
        }
    }

    private sealed record HandlerTypeInfo(string TypeName, string ContainerName, string RequestName, string InterfaceName);
}
