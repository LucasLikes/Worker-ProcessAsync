using Worker.ProcessSync.Enums;
using Worker.ProcessSync.Interfaces;
using Worker.ProcessSync.Models;

namespace Worker.ProcessSync.Services;

/// <summary>
/// Portado e simplificado do projeto CentralDeProcessos.
/// Resolve o PseudoStatus de um processo com base no seu estado Camunda e nas atividades finalizadas.
/// </summary>
public sealed class StatusResolver : IStatusResolver
{
    public PseudoStatus ResolveProcessStatus(
        string processState,
        IEnumerable<CamundaHistoryActivity> activities)
    {
        try
        {
            var acts = activities.ToList(); // materializa uma única vez

            if (processState == CamundaProcessState.ACTIVE.ToString())
            {
                // Ainda na sala de espera / coleta não iniciada
                if (acts.Any(a => a.ActivityId == ActivityIdStatus.iniciar_coleta.ToString() && a.EndTime == null))
                    return PseudoStatus.NaoIniciado;

                return PseudoStatus.EmAndamento;
            }

            if (processState == CamundaProcessState.COMPLETED.ToString())
            {
                // Aprovado
                if (acts.Any(IsTerminateEvent(ActivityIdStatus.APROVADO)))
                    return PseudoStatus.Aprovado;

                // Reprovado
                if (acts.Any(IsTerminateEvent(ActivityIdStatus.REPROVADO)))
                    return PseudoStatus.Reprovado;

                // SLA global expirado
                if (acts.Any(IsTerminateEvent(ActivityIdStatus.globalSLAExpired)))
                    return PseudoStatus.Expirado;

                // SLA de coleta expirado após coleta iniciada = abandonado
                if (acts.Any(a => a.ActivityId == ActivityIdStatus.iniciar_coleta.ToString())
                    && acts.Any(IsTerminateEvent(ActivityIdStatus.coletaSLAExpired)))
                    return PseudoStatus.Abandonado;
            }

            if (processState == CamundaProcessState.EXTERNALLY_TERMINATED.ToString())
                return PseudoStatus.Expirado;

            // COMPLETED sem evento terminal reconhecido
            if (acts.Any(IsTerminateEvent(ActivityIdStatus.APROVADO)))
                return PseudoStatus.Aprovado;

            if (acts.Any(IsTerminateEvent(ActivityIdStatus.REPROVADO)))
                return PseudoStatus.Reprovado;

            return PseudoStatus.Desconhecido;
        }
        catch
        {
            return PseudoStatus.Desconhecido;
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static Func<CamundaHistoryActivity, bool> IsTerminateEvent(ActivityIdStatus id) =>
        a => a.ActivityType == ActivityType.terminateEndEvent.ToString()
          && a.ActivityId == id.ToString();
}