using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Worker.ProcessSync.Config;
using Worker.ProcessSync.Domain;
using Worker.ProcessSync.Interfaces;

namespace Worker.ProcessSync.Infrastructure;

/// <summary>
/// Repositório PostgreSQL usando Dapper.
/// Schema é injetado dinamicamente via <see cref="DatabaseSettings.Schema"/>,
/// suportando multi-tenancy sem múltiplos DbContexts.
/// </summary>
public sealed class TicketRepository : ITicketRepository
{
    private readonly DatabaseSettings _db;
    private readonly ILogger<TicketRepository> _logger;

    // Nome qualificado da tabela; calculado uma vez no construtor.
    private readonly string _table;

    public TicketRepository(
        IOptions<DatabaseSettings> dbOpts,
        ILogger<TicketRepository> logger)
    {
        _db = dbOpts.Value;
        _logger = logger;
        // Ex.: "tenant_acme"."camunda_process_ticket"
        _table = $"\"{_db.Schema}\".\"camunda_process_ticket\"";
    }

    public async Task BulkUpsertAsync(IEnumerable<ProcessTicket> tickets, CancellationToken ct = default)
    {
        var list = tickets.ToList();
        if (list.Count == 0) return;

        // Uma única query parametrizada com unnest() — mais eficiente que N INSERTs.
        // O ON CONFLICT atualiza apenas se algo mudou (updated_at + campos de negócio).
        const string sql = @"
            INSERT INTO {0} (
                process_id, hash, trace_id, document, name,
                process_date, stage, status, journey,
                created_at, updated_at
            )
            SELECT
                unnest(@ProcessIds),  unnest(@Hashes),    unnest(@TraceIds),
                unnest(@Documents),   unnest(@Names),
                unnest(@ProcessDates),unnest(@Stages),    unnest(@Statuses),
                unnest(@Journeys),
                unnest(@CreatedAts),  unnest(@UpdatedAts)
            ON CONFLICT (process_id) DO UPDATE SET
                hash         = EXCLUDED.hash,
                trace_id     = EXCLUDED.trace_id,
                document     = EXCLUDED.document,
                name         = EXCLUDED.name,
                process_date = EXCLUDED.process_date,
                stage        = EXCLUDED.stage,
                status       = EXCLUDED.status,
                journey      = EXCLUDED.journey,
                updated_at   = EXCLUDED.updated_at
            WHERE
                {0}.status       IS DISTINCT FROM EXCLUDED.status
             OR {0}.stage        IS DISTINCT FROM EXCLUDED.stage
             OR {0}.hash         IS DISTINCT FROM EXCLUDED.hash
             OR {0}.updated_at   < EXCLUDED.updated_at;
        ";

        var parameters = new
        {
            ProcessIds = list.Select(t => t.ProcessId).ToArray(),
            Hashes = list.Select(t => t.Hash).ToArray(),
            TraceIds = list.Select(t => t.TraceId).ToArray(),
            Documents = list.Select(t => t.Document).ToArray(),
            Names = list.Select(t => t.Name).ToArray(),
            ProcessDates = list.Select(t => t.ProcessDate).ToArray(),
            Stages = list.Select(t => t.Stage).ToArray(),
            Statuses = list.Select(t => (short)t.Status).ToArray(),
            Journeys = list.Select(t => t.Journey).ToArray(),
            CreatedAts = list.Select(t => t.CreatedAt).ToArray(),
            UpdatedAts = list.Select(t => t.UpdatedAt).ToArray()
        };

        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);

        try
        {
            var affected = await conn.ExecuteAsync(
                string.Format(sql, _table),
                parameters);

            _logger.LogDebug("BulkUpsert: {Total} enviados, {Affected} linhas alteradas", list.Count, affected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no BulkUpsert ({Count} tickets)", list.Count);
            throw;
        }
    }

    public async Task<bool> IsEmptyAsync(CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        var count = await conn.ExecuteScalarAsync<long>(
            $"SELECT COUNT(1) FROM {_table} LIMIT 1");
        return count == 0;
    }

    public async Task<DateTime?> GetLatestUpdatedAtAsync(CancellationToken ct = default)
    {
        await using var conn = CreateConnection();
        await conn.OpenAsync(ct);
        return await conn.ExecuteScalarAsync<DateTime?>(
            $"SELECT MAX(updated_at) FROM {_table}");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private NpgsqlConnection CreateConnection() =>
        new(_db.ConnectionString);
}