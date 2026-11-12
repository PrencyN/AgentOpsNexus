using System.ComponentModel.DataAnnotations;

namespace AgentOpsNexus.AgentApi.Contracts;

public sealed class CreateAgentRequest
{
    [Required]
    [MinLength(10)]
    [MaxLength(4000)]
    public required string Message { get; init; }

    [Required]
    [RegularExpression(
        "^(Development|Test|Staging|Production)$",
        ErrorMessage = "Environment must be Development, Test, Staging, or Production.")]
    public required string Environment { get; init; }

    [Required]
    [RegularExpression(
        "^(Low|Medium|High|Critical)$",
        ErrorMessage = "Severity must be Low, Medium, High, or Critical.")]
    public required string Severity { get; init; }

    public bool SimulateFailure { get; init; }
}