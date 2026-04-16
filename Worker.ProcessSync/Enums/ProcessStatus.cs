namespace Worker.ProcessSync.Enums;

public enum PseudoStatus
{
    NaoIniciado = 0,
    EmAndamento = 1,
    Expirado = 2,
    Abandonado = 3,
    EmAnalise = 4,
    Executado = 5,
    Aprovado = 6,
    Reprovado = 7,
    Desconhecido = 8
}

public enum CamundaProcessState
{
    ACTIVE,
    COMPLETED,
    EXTERNALLY_TERMINATED
}

public enum ActivityType
{
    terminateEndEvent,
    userTask
}

public enum ActivityIdStatus
{
    APROVADO,
    REPROVADO,
    globalSLAExpired,
    coletaSLAExpired,
    sala_de_espera,
    iniciar_coleta
}