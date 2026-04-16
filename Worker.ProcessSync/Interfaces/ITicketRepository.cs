/// <summary>
/// Repositório responsável por persistir e consultar tickets na tabela do tenant.
/// </summary>
public interface ITicketRepository
{
    /// <summary>
    /// Upsert em lote: INSERT ... ON CONFLICT (process_id) DO UPDATE.
    /// Atualiza apenas se o status, stage ou dados relevantes mudaram.
    /// </summary>
    Task BulkUpsertAsync(IEnumerable<ProcessTicket> tickets, CancellationToken ct = default);

    /// <summary>Retorna true se a tabela do tenant está vazia (usado para decidir FullSync).</summary>
    Task<bool> IsEmptyAsync(CancellationToken ct = default);

    /// <summary>Retorna o updated_at mais recente gravado (usado como âncora do DeltaSync).</summary>
    Task<DateTime?> GetLatestUpdatedAtAsync(CancellationToken ct = default);
}