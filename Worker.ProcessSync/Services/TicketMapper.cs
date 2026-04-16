using Worker.ProcessSync.Domain;
using Worker.ProcessSync.Enums;
using Worker.ProcessSync.Models;

namespace Worker.ProcessSync.Services;

/// <summary>
/// Centraliza o mapeamento de <see cref="CamundaHistoryProcess"/> + <see cref="PseudoStatus"/>
/// para <see cref="ProcessTicket"/>.
///
/// Convenções de extração de variáveis do processo:
/// - "document"  → CPF/CNPJ do titular
/// - "hash"      → token público do fluxo
/// - "traceId"   → correlação com outros sistemas
/// - "name"      → nome do titular
/// - "stage"     → etapa atual dentro do fluxo
/// </summary>
public static class TicketMapper
{
    public static ProcessTicket Map(
        CamundaHistoryProcess process,
        PseudoStatus status)
    {
        return ProcessTicket.From(
            processId: process.Id,
            hash: GetVar(process, "hash"),
            traceId: GetVar(process, "traceId"),
            document: GetVar(process, "document"),
            name: GetVar(process, "name"),
            processDate: process.StartTime ?? DateTime.UtcNow,
            stage: GetVar(process, "stage"),
            status: status,
            journey: process.ProcessDefinitionKey
        );
    }

    private static string? GetVar(CamundaHistoryProcess process, string key)
    {
        if (process.Variables.TryGetValue(key, out var val))
            return val?.ToString();
        return null;
    }
}