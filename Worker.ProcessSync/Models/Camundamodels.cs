using System.Text.Json.Serialization;

namespace Worker.ProcessSync.Models;

/// <summary>Representa um processo no histórico do Camunda.</summary>
public sealed class CamundaHistoryProcess
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("processDefinitionKey")]
    public string ProcessDefinitionKey { get; init; } = string.Empty;

    [JsonPropertyName("processDefinitionName")]
    public string? ProcessDefinitionName { get; init; }

    [JsonPropertyName("startTime")]
    public DateTime? StartTime { get; init; }

    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; init; }

    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("tenantId")]
    public string? TenantId { get; init; }

    // Variáveis adicionais extraídas de extensionProperties ou variáveis do processo
    [JsonPropertyName("variables")]
    public Dictionary<string, object?> Variables { get; init; } = new();
}

/// <summary>Representa uma atividade no histórico do Camunda.</summary>
public sealed class CamundaHistoryActivity
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("activityId")]
    public string ActivityId { get; init; } = string.Empty;

    [JsonPropertyName("activityName")]
    public string? ActivityName { get; init; }

    [JsonPropertyName("activityType")]
    public string ActivityType { get; init; } = string.Empty;

    [JsonPropertyName("processInstanceId")]
    public string ProcessInstanceId { get; init; } = string.Empty;

    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; init; }

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; init; }

    [JsonPropertyName("extensionProperties")]
    public Dictionary<string, string> ExtensionProperties { get; init; } = new();
}

/// <summary>Variável de processo do Camunda (para resolver status Aprovado/Reprovado).</summary>
public sealed class CamundaProcessVariable
{
    [JsonPropertyName("value")]
    public object? Value { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }
}