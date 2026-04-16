using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Worker.ProcessSync.Config;
using Worker.ProcessSync.Interfaces;
using Worker.ProcessSync.Services;

namespace Worker.ProcessSync.Strategies;

/// <summary>
/// FullSync: percorre TODOS os processos do tenant página a página.
/// Executado apenas quando a tabela está vazia (primeira carga).
/// </summary>
public sealed class FullSyncStrategy : ITenantSyncStrategy
{
    private readonly ICamundaClient _camunda;
    private readonly ITicketRepository _repo;
    private readonly IStatusResolver _resolver;
    private readonly CamundaSettings _settings;
    private readonly ILogger<FullSyncStrategy> _logger;

    public FullSyncStrategy(
        ICamundaClient camunda,
        ITicketRepository repo,
        IStatusResolver resolver,
        IOptions<CamundaSettings> settings,
        ILogger<FullSyncStrategy> logger)
    {
        _camunda = camunda;
        _repo = repo;
        _resolver = resolver;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[FullSync] Iniciando carga completa para tenant {Tenant}", _settings.TenantId);

        int firstResult = 0;
        int total = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var processes = await _camunda.GetHistoryProcessesAsync(
                _settings.TenantId, firstResult, _settings.PageSize, ct);

            if (processes.Count == 0) break;

            var tickets = await BuildTicketsAsync(processes, ct);
            await _repo.BulkUpsertAsync(tickets, ct);

            total += processes.Count;
            firstResult += processes.Count;

            _logger.LogInformation("[FullSync] Processados {Total} até agora...", total);

            // Menos que uma página cheia = última página
            if (processes.Count < _settings.PageSize) break;
        }

        _logger.LogInformation("[FullSync] Concluído. Total: {Total} processos.", total);
        return total;
    }

    private async Task<List<Worker.ProcessSync.Domain.ProcessTicket>> BuildTicketsAsync(
        IReadOnlyList<Worker.ProcessSync.Models.CamundaHistoryProcess> processes,
        CancellationToken ct)
    {
        var tickets = new List<Worker.ProcessSync.Domain.ProcessTicket>(processes.Count);

        foreach (var process in processes)
        {
            ct.ThrowIfCancellationRequested();

            var activities = await _camunda.GetActivityHistoryAsync(
                process.Id, _settings.TenantId, ct);

            var status = _resolver.ResolveProcessStatus(process.State, activities);
            tickets.Add(TicketMapper.Map(process, status));
        }

        return tickets;
    }
}

/// <summary>
/// DeltaSync: busca apenas processos que mudaram desde a última execução.
/// Usa uma janela de lookback para garantir que não percamos eventos de borda.
/// </summary>
public sealed class DeltaSyncStrategy : ITenantSyncStrategy
{
    private readonly ICamundaClient _camunda;
    private readonly ITicketRepository _repo;
    private readonly IStatusResolver _resolver;
    private readonly CamundaSettings _camundaSettings;
    private readonly SyncSettings _syncSettings;
    private readonly ILogger<DeltaSyncStrategy> _logger;

    public DeltaSyncStrategy(
        ICamundaClient camunda,
        ITicketRepository repo,
        IStatusResolver resolver,
        IOptions<CamundaSettings> camundaOpts,
        IOptions<SyncSettings> syncOpts,
        ILogger<DeltaSyncStrategy> logger)
    {
        _camunda = camunda;
        _repo = repo;
        _resolver = resolver;
        _camundaSettings = camundaOpts.Value;
        _syncSettings = syncOpts.Value;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CancellationToken ct = default)
    {
        // Âncora: max(updated_at) da tabela - lookback configiurável para cobrir edge cases
        var latestInDb = await _repo.GetLatestUpdatedAtAsync(ct);
        var since = (latestInDb ?? DateTime.UtcNow.AddDays(-1))
                            .AddMinutes(-_syncSettings.LookbackMinutes);

        _logger.LogDebug("[DeltaSync] Buscando processos alterados desde {Since:O}", since);

        int firstResult = 0;
        int total = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var processes = await _camunda.GetProcessesUpdatedSinceAsync(
                _camundaSettings.TenantId, since, firstResult, _camundaSettings.PageSize, ct);

            if (processes.Count == 0) break;

            var tickets = new List<Worker.ProcessSync.Domain.ProcessTicket>(processes.Count);

            foreach (var process in processes)
            {
                var activities = await _camunda.GetActivityHistoryAsync(
                    process.Id, _camundaSettings.TenantId, ct);

                var status = _resolver.ResolveProcessStatus(process.State, activities);
                tickets.Add(TicketMapper.Map(process, status));
            }

            await _repo.BulkUpsertAsync(tickets, ct);

            total += processes.Count;
            firstResult += processes.Count;

            if (processes.Count < _camundaSettings.PageSize) break;
        }

        if (total > 0)
            _logger.LogInformation("[DeltaSync] {Total} processos atualizados.", total);

        return total;
    }
}