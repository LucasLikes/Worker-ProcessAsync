namespace Worker.ProcessSync.Config;

public sealed class CamundaSettings
{
    public const string Section = "Camunda";

    public string BaseUrl { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    /// <summary>Quantos processos buscar por página na API do Camunda.</summary>
    public int PageSize { get; init; } = 200;
}

public sealed class SyncSettings
{
    public const string Section = "Sync";

    /// <summary>Intervalo entre cada ciclo de DeltaSync em segundos.</summary>
    public int IntervalSeconds { get; init; } = 60;

    /// <summary>
    /// Janela de lookback no DeltaSync: relê processos atualizados nos últimos X minutos.
    /// Garante que não percamos updates que chegaram durante o ciclo anterior.
    /// </summary>
    public int LookbackMinutes { get; init; } = 5;
}

public sealed class DatabaseSettings
{
    public const string Section = "Database";

    public string ConnectionString { get; init; } = string.Empty;
    /// <summary>Schema PostgreSQL do tenant (ex.: "tenant_acme").</summary>
    public string Schema { get; init; } = "public";
}