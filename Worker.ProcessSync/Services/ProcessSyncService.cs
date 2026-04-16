using Microsoft.Extensions.Logging;
using Worker.ProcessSync.Interfaces;
using Worker.ProcessSync.Strategies;

namespace Worker.ProcessSync.Services;

/// <summary>
/// Orquestra a sincronização: decide entre FullSync (tabela vazia) e DeltaSync (recorrente).
/// Segue o Open/Closed Principle — para adicionar uma nova estratégia basta implementar
/// <see cref="ITenantSyncStrategy"/> e registrá-la no container.
/// </summary>
public sealed class ProcessSyncService
{
    private readonly ITicketRepository _repo;
    private readonly FullSyncStrategy _fullSync;
    private readonly DeltaSyncStrategy _deltaSync;
    private readonly ILogger<ProcessSyncService> _logger;

    public ProcessSyncService(
        ITicketRepository repo,
        FullSyncStrategy fullSync,
        DeltaSyncStrategy deltaSync,
        ILogger<ProcessSyncService> logger)
    {
        _repo = repo;
        _fullSync = fullSync;
        _deltaSync = deltaSync;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken ct)
    {
        // Decide a estratégia de forma transparente para o worker
        bool isEmpty = await _repo.IsEmptyAsync(ct);
        ITenantSyncStrategy strategy = isEmpty ? _fullSync : _deltaSync;

        var strategyName = isEmpty ? "FullSync" : "DeltaSync";

        try
        {
            _logger.LogInformation("Iniciando {Strategy}...", strategyName);
            var count = await strategy.ExecuteAsync(ct);
            _logger.LogInformation("{Strategy} concluído: {Count} ticket(s) processado(s).", strategyName, count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("{Strategy} cancelado pelo CancellationToken.", strategyName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante {Strategy}.", strategyName);
            // Não re-lança: o worker continuará no próximo ciclo
        }
    }
}