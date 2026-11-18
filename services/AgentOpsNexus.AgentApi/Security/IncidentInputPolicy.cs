using System.Globalization;

namespace AgentOpsNexus.AgentApi.Security;

/// <summary>
/// Performs deterministic validation and normalization of user-provided
/// incident descriptions before the descriptions are persisted or sent
/// to an AI model.
///
/// Prompt-injection phrases are not automatically rejected. User text is
/// treated as untrusted incident content and model instructions remain
/// controlled by the application.
/// </summary>
public sealed class IncidentInputPolicy
{
    public const int MinimumLength = 10;

    public const int MaximumLength = 4000;

    private const int MaximumConsecutiveNewLines = 8;

    public IncidentInputPolicyResult Evaluate(
        string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return IncidentInputPolicyResult.Rejected(
                code: "INCIDENT_DESCRIPTION_REQUIRED",
                message:
                    "Incident description is required.");
        }

        var normalized =
            NormalizeLineEndings(input).Trim();

        if (normalized.Length < MinimumLength)
        {
            return IncidentInputPolicyResult.Rejected(
                code:
                    "INCIDENT_DESCRIPTION_TOO_SHORT",
                message:
                    $"Incident description must contain at least " +
                    $"{MinimumLength} characters.");
        }

        if (normalized.Length > MaximumLength)
        {
            return IncidentInputPolicyResult.Rejected(
                code:
                    "INCIDENT_DESCRIPTION_TOO_LONG",
                message:
                    $"Incident description cannot exceed " +
                    $"{MaximumLength} characters.");
        }

        if (normalized.Contains(
                '\0',
                StringComparison.Ordinal))
        {
            return IncidentInputPolicyResult.Rejected(
                code:
                    "INCIDENT_DESCRIPTION_NULL_BYTE",
                message:
                    "Incident description cannot contain null bytes.");
        }

        var invalidCharacter =
            FindInvalidControlCharacter(
                normalized);

        if (invalidCharacter is not null)
        {
            return IncidentInputPolicyResult.Rejected(
                code:
                    "INCIDENT_DESCRIPTION_CONTROL_CHARACTER",
                message:
                    "Incident description contains an unsupported " +
                    $"control character at position " +
                    $"{invalidCharacter.Value.Position}.");
        }

        if (HasExcessiveConsecutiveNewLines(
                normalized))
        {
            return IncidentInputPolicyResult.Rejected(
                code:
                    "INCIDENT_DESCRIPTION_EXCESSIVE_WHITESPACE",
                message:
                    "Incident description contains too many " +
                    "consecutive blank lines.");
        }

        return IncidentInputPolicyResult.Accepted(
            normalized);
    }

    private static string NormalizeLineEndings(
        string input)
    {
        return input
            .Replace(
                "\r\n",
                "\n",
                StringComparison.Ordinal)
            .Replace(
                "\r",
                "\n",
                StringComparison.Ordinal);
    }

    private static InvalidCharacter?
        FindInvalidControlCharacter(
            string value)
    {
        for (var index = 0;
             index < value.Length;
             index++)
        {
            var character =
                value[index];

            /*
             * Permit newline and horizontal tab because incident
             * descriptions may contain structured notes.
             */
            if (character is '\n' or '\t')
            {
                continue;
            }

            var category =
                CharUnicodeInfo
                    .GetUnicodeCategory(
                        character);

            if (category is
                UnicodeCategory.Control or
                UnicodeCategory.Surrogate or
                UnicodeCategory.PrivateUse or
                UnicodeCategory.Format)
            {
                return new InvalidCharacter(
                    index,
                    category);
            }
        }

        return null;
    }

    private static bool
        HasExcessiveConsecutiveNewLines(
            string value)
    {
        var consecutiveNewLines = 0;

        foreach (var character in value)
        {
            if (character == '\n')
            {
                consecutiveNewLines++;

                if (consecutiveNewLines >
                    MaximumConsecutiveNewLines)
                {
                    return true;
                }
            }
            else
            {
                consecutiveNewLines = 0;
            }
        }

        return false;
    }

    private readonly record struct
        InvalidCharacter(
            int Position,
            UnicodeCategory Category);
}

/// <summary>
/// Result returned by <see cref="IncidentInputPolicy"/>.
/// </summary>
public sealed record IncidentInputPolicyResult
{
    private IncidentInputPolicyResult(
        bool isAccepted,
        string? normalizedInput,
        string? errorCode,
        string? errorMessage)
    {
        IsAccepted = isAccepted;
        NormalizedInput = normalizedInput;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsAccepted { get; }

    public string? NormalizedInput { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public static IncidentInputPolicyResult Accepted(
        string normalizedInput)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            normalizedInput);

        return new IncidentInputPolicyResult(
            isAccepted: true,
            normalizedInput: normalizedInput,
            errorCode: null,
            errorMessage: null);
    }

    public static IncidentInputPolicyResult Rejected(
        string code,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            code);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            message);

        return new IncidentInputPolicyResult(
            isAccepted: false,
            normalizedInput: null,
            errorCode: code,
            errorMessage: message);
    }
}