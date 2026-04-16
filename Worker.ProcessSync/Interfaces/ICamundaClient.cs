using Worker.ProcessSync.Domain;
using Worker.ProcessSync.Enums;
using Worker.ProcessSync.Models;

namespace Worker.ProcessSync.Interfaces;

/// <summary>
/// Abstração do cliente REST do Camunda.
/// Permite mock fácil em testes e troca de implementação sem impacto no domínio.
/// </summary>
public interface ICamundaClient
{
    /// <summary>
    /// Retorna todos os processos do histórico para o tenant, paginado.
    /// </summary>
    Task<IReadOnlyList<CamundaHistoryProcess>> GetHistoryProcessesAsync(
        string tenantId,
        int firstResult,
        int maxResults,
        CancellationToken ct = default);

    /// <summary>
    /// Retorna histórico de atividades de um processo específico.
    /// </summary>
    Task<IReadOnlyList<CamundaHistoryActivity>> GetActivityHistoryAsync(
        string processInstanceId,
        string tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Retorna processos atualizados/iniciados após <paramref name="since"/>.
    /// Usado no DeltaSync para evitar re-processar tudo.
    /// </summary>
    Task<IReadOnlyList<CamundaHistoryProcess>> GetProcessesUpdatedSinceAsync(
        string tenantId,
        DateTime since,
        int firstResult,
        int maxResults,
        CancellationToken ct = default);

    /// <summary>
    /// Verifica se o Camunda está acessível (usado no HealthCheck).
    /// </summary>
    Task<bool> PingAsync(CancellationToken ct = default);
}
