namespace FlowCore.Core;

/// <summary>
/// Tipo de retorno para commands que não retornam valor.
/// Use como retorno de ICommand para indicar que não há resultado.
/// </summary>
/// <example>
/// <code>
/// public record DeactivateUserCommand(Guid UserId) : ICommand&lt;Unit&gt;;
/// 
/// public class Handler : ICommandHandler&lt;DeactivateUserCommand, Unit&gt;
/// {
///     public async Task&lt;Unit&gt; HandleAsync(DeactivateUserCommand command, CancellationToken ct)
///     {
///         // ... lógica ...
///         return Unit.Value;
///     }
/// }
/// </code>
/// </example>
public struct Unit
{
    /// <summary>
    /// Instância singleton de Unit.
    /// </summary>
    public static readonly Unit Value = new Unit();
}
