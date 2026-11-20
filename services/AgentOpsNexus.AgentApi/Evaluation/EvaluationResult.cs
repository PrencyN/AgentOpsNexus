namespace AgentOpsNexus.AgentApi.Evaluation;

/// <summary>
/// Represents the deterministic evaluation of an AI-generated incident
/// response against the evidence and safety requirements of AgentOps
/// Nexus.
/// </summary>
public sealed record EvaluationResult
{
    public EvaluationResult(
        bool passed,
        int score,
        IReadOnlyCollection<EvaluationFinding> findings,
        IReadOnlyCollection<string> citedEvidenceIds,
        IReadOnlyCollection<string> unknownEvidenceIds)
    {
        if (score is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(score),
                "Evaluation score must be between 0 and 100.");
        }

        Passed = passed;
        Score = score;

        Findings =
            findings
            ??
            throw new ArgumentNullException(
                nameof(findings));

        CitedEvidenceIds =
            citedEvidenceIds
            ??
            throw new ArgumentNullException(
                nameof(citedEvidenceIds));

        UnknownEvidenceIds =
            unknownEvidenceIds
            ??
            throw new ArgumentNullException(
                nameof(unknownEvidenceIds));
    }

    public bool Passed { get; }

    public int Score { get; }

    public IReadOnlyCollection<EvaluationFinding>
        Findings { get; }

    public IReadOnlyCollection<string>
        CitedEvidenceIds { get; }

    public IReadOnlyCollection<string>
        UnknownEvidenceIds { get; }

    public bool HasErrors =>
        Findings.Any(
            finding =>
                finding.Severity ==
                EvaluationSeverity.Error);

    public bool HasWarnings =>
        Findings.Any(
            finding =>
                finding.Severity ==
                EvaluationSeverity.Warning);
}

public sealed record EvaluationFinding(
    string Code,
    EvaluationSeverity Severity,
    string Message);

public enum EvaluationSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2
}