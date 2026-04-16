using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using Worker.ProcessSync.Config;
using Worker.ProcessSync.HealthChecks;
using Worker.ProcessSync.Infrastructure;
using Worker.ProcessSync.Interfaces;
using Worker.ProcessSync.Services;
using Worker.ProcessSync.Strategies;
using Worker.ProcessSync.Workers;

namespace Worker.ProcessSync.Config;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddWorkerServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Configuration;

        // ── Options ────────────────────────────────────────────────────────────
        services
            .AddOptions<CamundaSettings>()
            .Bind(config.GetSection(CamundaSettings.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<SyncSettings>()
            .Bind(config.GetSection(SyncSettings.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<DatabaseSettings>()
            .Bind(config.GetSection(DatabaseSettings.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ── HttpClient com Polly ───────────────────────────────────────────────
        // Retry: 3 tentativas com jitter exponencial (não sobrecarrega o Camunda)
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)));

        // Circuit Breaker: abre após 5 falhas consecutivas por 30s
        var circuitBreaker = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 5,
                                 durationOfBreak: TimeSpan.FromSeconds(30));

        services
            .AddHttpClient<ICamundaClient, CamundaClient>(client =>
            {
                var baseUrl = config[$"{CamundaSettings.Section}:BaseUrl"]?.TrimEnd('/') + "/engine-rest/";
                client.BaseAddress = new Uri(baseUrl ?? "http://localhost:8080/engine-rest/");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(circuitBreaker);

        // ── Domain Services ────────────────────────────────────────────────────
        services.AddScoped<IStatusResolver, StatusResolver>();
        services.AddScoped<ITicketRepository, TicketRepository>();

        // Estratégias como Scoped (cada ciclo pega uma instância limpa)
        services.AddScoped<FullSyncStrategy>();
        services.AddScoped<DeltaSyncStrategy>();
        services.AddScoped<ProcessSyncService>();

        // ── HealthChecks ───────────────────────────────────────────────────────
        services
            .AddHealthChecks()
            .AddCheck<CamundaHealthCheck>("camunda", tags: ["ready"])
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

        // ── Worker ────────────────────────────────────────────────────────────
        services.AddHostedService<ProcessSyncWorker>();

        return builder;
    }
}