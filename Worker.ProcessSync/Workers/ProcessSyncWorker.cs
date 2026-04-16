using System;
using System.Collections.Generic;
using System.Text;
using Worker.ProcessSync.Interfaces;

namespace Worker.ProcessSync.Workers;

public class ProcessSyncWorker : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<ProcessSyncWorker> _logger;

    public ProcessSyncWorker(IServiceProvider provider, ILogger<ProcessSyncWorker> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _provider.CreateScope();

                var service = scope.ServiceProvider
                    .GetRequiredService<IProcessSyncService>();

                await service.SyncAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na execução");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}