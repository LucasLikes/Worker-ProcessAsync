using Worker.ProcessSync.Enums;

namespace Worker.ProcessSync.Domain;

/// <summary>
/// Representa uma linha da tabela <c>camunda_process_ticket</c>.
/// Esta é a projeção desnormalizada pensada para performance da grid de filtros.
/// </summary>
public sealed class ProcessTicket
{
    public long Id { get; init; }
    public string ProcessId { get; init; } = string.Empty;
    public string? Hash { get; init; }
    public string? TraceId { get; init; }
    public string? Document { get; init; }
    public string? Name { get; init; }
    public DateTime ProcessDate { get; init; }
    public string? Stage { get; init; }
    public PseudoStatus Status { get; init; }
    public string? Journey { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Factory: constrói um ticket a partir dos dados crus do Camunda + status resolvido.
    /// Centraliza a lógica de mapeamento, mantendo o domínio livre de dependências externas.
    /// </summary>
    public static ProcessTicket From(
        string processId,
        string? hash,
        string? traceId,
        string? document,
        string? name,
        DateTime processDate,
        string? stage,
        PseudoStatus status,
        string? journey)
    {
        var now = DateTime.UtcNow;
        return new ProcessTicket
        {
            ProcessId = processId,
            Hash = hash,
            TraceId = traceId,
            Document = document,
            Name = name,
            ProcessDate = processDate,
            Stage = stage,
            Status = status,
            Journey = journey,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}