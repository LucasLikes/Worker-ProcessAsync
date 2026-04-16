using System;
using System.Collections.Generic;
using System.Text;
using Worker.ProcessSync.Interfaces;

namespace Worker.ProcessSync.Services;

public class ProcessSyncService : IProcessSyncService
{
    private readonly ICamundaClient _camundaClient;
    private readonly IProcessSyncService __repo;

    public ProcessSyncService(ICamundaClient camundaClient, IProcessSyncService repo)
    {
        _camundaClient = camundaClient;
        __repo = repo;
    }

    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var processes = await _camunda.GetProcessesAsync(cancellationToken);

        foreach (var process in processes)
        {
            var existing = await __repo.GetByCamundaId(process.Id);

            if (existing == null)
                await __repo.Add(process);
            else
                await __repo.Update(process);
        }
    }
}
