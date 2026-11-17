using System.Diagnostics;

namespace AgentOpsNexus.AgentApi.Observability;

public static class AgentOpsActivitySource
{
    public const string SourceName =
        "AgentOpsNexus.AgentApi";

    public static readonly ActivitySource Source =
        new(SourceName);

    public static Activity? StartAgentInvestigation(
        Guid requestId,
        string provider,
        string environment,
        string severity,
        int attemptNumber)
    {
        var activity =
            Source.StartActivity(
                "agent.investigate",
                ActivityKind.Internal);

        activity?.SetTag(
            "agentops.request_id",
            requestId);

        activity?.SetTag(
            "gen_ai.provider.name",
            provider);

        activity?.SetTag(
            "agentops.environment",
            environment);

        activity?.SetTag(
            "agentops.severity",
            severity);

        activity?.SetTag(
            "agentops.attempt_number",
            attemptNumber);

        return activity;
    }

    public static Activity? StartToolInvocation(
        string toolName,
        string environment)
    {
        var activity =
            Source.StartActivity(
                $"tool.{toolName}",
                ActivityKind.Internal);

        activity?.SetTag(
            "agentops.tool.name",
            toolName);

        activity?.SetTag(
            "agentops.environment",
            environment);

        return activity;
    }

    public static void MarkSucceeded(
        Activity? activity)
    {
        activity?.SetStatus(
            ActivityStatusCode.Ok);
    }

    public static void MarkFailed(
        Activity? activity,
        Exception exception)
    {
        activity?.SetStatus(
            ActivityStatusCode.Error,
            exception.GetType().Name);

        activity?.SetTag(
            "error.type",
            exception.GetType().FullName);
    }
}