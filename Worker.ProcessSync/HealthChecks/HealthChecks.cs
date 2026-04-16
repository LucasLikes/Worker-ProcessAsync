using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using Worker.ProcessSync.Config;
using Worker.ProcessSync.Interfaces;

namespace Worker.ProcessSync.HealthChecks;

/// <summary>
/// Verifica se a API REST do Camunda está acessível.
/// </summary>
public sealed class CamundaHealthCheck : IHealthCheck
{
    private readonly ICamundaClient _client;

    public CamundaHealthCheck(ICamundaClient client) => _client = client;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var ok = await _client.PingAsync(ct);
            return ok
                ? HealthCheckResult.Healthy("Camunda acessível.")
                : HealthCheckResult.Unhealthy("Camunda inacessível.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Erro ao conectar no Camunda.", ex);
        }
    }
}

/// <summary>
/// Verifica se o PostgreSQL está acessível e o schema do tenant existe.
/// </summary>
public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly DatabaseSettings _settings;

    public PostgresHealthCheck(IOptions<DatabaseSettings> opts) =>
        _settings = opts.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_settings.ConnectionString);
            await conn.OpenAsync(ct);

            // Verifica se a tabela do tenant existe (não basta conectar)
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = @schema
                      AND table_name   = 'camunda_process_ticket'
                )";
            cmd.Parameters.AddWithValue("schema", _settings.Schema);

            var exists = (bool)(await cmd.ExecuteScalarAsync(ct) ?? false);

            return exists
                ? HealthCheckResult.Healthy($"PostgreSQL OK. Schema '{_settings.Schema}' encontrado.")
                : HealthCheckResult.Degraded($"Tabela camunda_process_ticket não encontrada no schema '{_settings.Schema}'.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Erro ao conectar no PostgreSQL.", ex);
        }
    }
}