using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Worker.ProcessSync.Config;
using Worker.ProcessSync.Services;

namespace Worker.ProcessSync.Workers;

/// <summary>
/// Worker principal. Responsabilidade única: cadenciar o ciclo de sincronização.
/// Toda a lógica de negócio fica no <see cref="ProcessSyncService"/>.
///
/// Fluxo:
/// 1. Aguarda HealthChecks estarem saudáveis antes de iniciar
/// 2. Executa SyncAsync (que decide Full vs Delta internamente)
/// 3. Aguarda o intervalo configurado
/// 4. Repete
/// </summary>
public sealed class ProcessSyncWorker : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly HealthCheckService _health;
    private readonly SyncSettings _settings;
    private readonly ILogger<ProcessSyncWorker> _logger;

    public ProcessSyncWorker(
        IServiceProvider provider,
        HealthCheckService health,
        IOptions<SyncSettings> syncOpts,
        ILogger<ProcessSyncWorker> logger)
    {
        _provider = provider;
        _health = health;
        _settings = syncOpts.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado. Intervalo: {Interval}s", _settings.IntervalSeconds);

        // Aguarda os serviços dependentes ficarem saudáveis antes de começar
        await WaitForHealthAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCycleAsync(stoppingToken);

            await Task.Delay(
                TimeSpan.FromSeconds(_settings.IntervalSeconds),
                stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        try
        {
            // Scoped: garante que repositório e cliente não acumulem estado entre ciclos
            await using var scope = _provider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ProcessSyncService>();
            await service.SyncAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Shutdown gracioso — não é erro
        }
        catch (Exception ex)
        {
            // Loga e continua: um ciclo com falha não derruba o worker
            _logger.LogError(ex, "Erro no ciclo de sincronização.");
        }
    }

    /// <summary>
    /// Aguarda até que todos os HealthChecks críticos estejam Healthy ou Degraded.
    /// Faz retry com backoff simples para não sobrecarregar no startup.
    /// </summary>
    private async Task WaitForHealthAsync(CancellationToken ct)
    {
        var delay = TimeSpan.FromSeconds(5);

        while (!ct.IsCancellationRequested)
        {
            var result = await _health.CheckHealthAsync(ct);

            if (result.Status != HealthStatus.Unhealthy)
            {
                _logger.LogInformation("HealthChecks OK. Iniciando sincronização.");
                return;
            }

            _logger.LogWarning(
                "HealthChecks não saudáveis: {Details}. Retry em {Delay}s...",
                string.Join(", ", result.Entries.Where(e => e.Value.Status == HealthStatus.Unhealthy).Select(e => e.Key)),
                delay.TotalSeconds);

            await Task.Delay(delay, ct);
            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60)); // backoff até 60s
        }
    }
}