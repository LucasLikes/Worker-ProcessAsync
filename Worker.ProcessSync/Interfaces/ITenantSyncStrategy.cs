
/// <summary>
/// Estratégia de sincronização.
/// Segue o padrão Strategy para alternar entre FullSync e DeltaSync sem if/else no worker.
/// </summary>
public interface ITenantSyncStrategy
{
    /// <summary>Executa a estratégia e retorna a quantidade de tickets processados.</summary>
    Task<int> ExecuteAsync(CancellationToken ct = default);
}