using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Worker.ProcessSync.Config;
using Worker.ProcessSync.Interfaces;
using Worker.ProcessSync.Models;

namespace Worker.ProcessSync.Infrastructure;

public sealed class CamundaClient : ICamundaClient
{
    private readonly HttpClient _http;
    private readonly CamundaSettings _settings;
    private readonly ILogger<CamundaClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CamundaClient(
        HttpClient http,
        IOptions<CamundaSettings> settings,
        ILogger<CamundaClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CamundaHistoryProcess>> GetHistoryProcessesAsync(
        string tenantId, int firstResult, int maxResults, CancellationToken ct = default)
    {
        var url = $"history/process-instance" +
                  $"?tenantIdIn={Uri.EscapeDataString(tenantId)}" +
                  $"&firstResult={firstResult}" +
                  $"&maxResults={maxResults}" +
                  $"&sortBy=startTime&sortOrder=desc";

        return await GetListAsync<CamundaHistoryProcess>(url, ct);
    }

    public async Task<IReadOnlyList<CamundaHistoryProcess>> GetProcessesUpdatedSinceAsync(
        string tenantId, DateTime since, int firstResult, int maxResults, CancellationToken ct = default)
    {
        // Camunda aceita startedAfter / finishedAfter — usamos startedAfter para capturar novos
        // e também buscamos os que terminaram recentemente via finishedAfter.
        // Uma única query com OR não existe na API; fazemos duas e fazemos distinct por Id.
        var isoSince = Uri.EscapeDataString(since.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"));

        var urlStarted = $"history/process-instance" +
                         $"?tenantIdIn={Uri.EscapeDataString(tenantId)}" +
                         $"&startedAfter={isoSince}" +
                         $"&firstResult={firstResult}&maxResults={maxResults}";

        var urlFinished = $"history/process-instance" +
                          $"?tenantIdIn={Uri.EscapeDataString(tenantId)}" +
                          $"&finishedAfter={isoSince}" +
                          $"&firstResult={firstResult}&maxResults={maxResults}";

        var started = await GetListAsync<CamundaHistoryProcess>(urlStarted, ct);
        var finished = await GetListAsync<CamundaHistoryProcess>(urlFinished, ct);

        // Distinct por Id — processos que iniciaram e terminaram no mesmo janela aparecem nas duas listas
        return started
            .Concat(finished)
            .DistinctBy(p => p.Id)
            .ToList();
    }

    public async Task<IReadOnlyList<CamundaHistoryActivity>> GetActivityHistoryAsync(
        string processInstanceId, string tenantId, CancellationToken ct = default)
    {
        var url = $"history/activity-instance" +
                  $"?processInstanceId={Uri.EscapeDataString(processInstanceId)}" +
                  $"&tenantIdIn={Uri.EscapeDataString(tenantId)}" +
                  $"&sortBy=startTime&sortOrder=asc";

        return await GetListAsync<CamundaHistoryActivity>(url, ct);
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.GetAsync("engine", ct);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Camunda ping falhou");
            return false;
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string url, CancellationToken ct)
    {
        using var resp = await _http.GetAsync(url, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Camunda {Url} retornou {Status}: {Body}", url, (int)resp.StatusCode, body);
            resp.EnsureSuccessStatusCode(); // lança para o Polly retentar
        }

        return await resp.Content.ReadFromJsonAsync<List<T>>(JsonOpts, ct)
               ?? new List<T>();
    }
}