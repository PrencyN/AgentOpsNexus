using System.ComponentModel.DataAnnotations;

namespace AgentOpsNexus.AgentApi.Configuration;

public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    [Required]
    public string Provider { get; init; } = "Mock";

    public string ProjectEndpoint { get; init; } = string.Empty;

    public string ModelDeployment { get; init; } = string.Empty;

    public string TenantId { get; init; } = string.Empty;

    [Range(10, 300)]
    public int TimeoutSeconds { get; init; } = 180;
}