using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AgentOpsNexus.AgentApi.Observability;

public sealed class AgentOpsMetrics
{
    public const string MeterName =
        "AgentOpsNexus.AgentApi";

    private readonly Counter<long>
        _investigationCounter;

    private readonly Counter<long>
        _investigationFailureCounter;

    private readonly Histogram<double>
        _investigationDuration;

    private readonly Counter<long>
        _toolInvocationCounter;

    private readonly Counter<long>
        _toolFailureCounter;

    private readonly Histogram<double>
        _toolDuration;

    private readonly Histogram<long>
        _evidenceCount;

    public AgentOpsMetrics(
        IMeterFactory meterFactory)
    {
        var meter =
            meterFactory.Create(MeterName);

        _investigationCounter =
            meter.CreateCounter<long>(
                "agentops.investigations",
                unit: "{investigation}",
                description:
                    "Number of agent investigations.");

        _investigationFailureCounter =
            meter.CreateCounter<long>(
                "agentops.investigation.failures",
                unit: "{failure}",
                description:
                    "Number of failed agent investigations.");

        _investigationDuration =
            meter.CreateHistogram<double>(
                "agentops.investigation.duration",
                unit: "ms",
                description:
                    "Agent investigation duration.");

        _toolInvocationCounter =
            meter.CreateCounter<long>(
                "agentops.tool.invocations",
                unit: "{invocation}",
                description:
                    "Number of agent tool invocations.");

        _toolFailureCounter =
            meter.CreateCounter<long>(
                "agentops.tool.failures",
                unit: "{failure}",
                description:
                    "Number of failed agent tool invocations.");

        _toolDuration =
            meter.CreateHistogram<double>(
                "agentops.tool.duration",
                unit: "ms",
                description:
                    "Agent tool invocation duration.");

        _evidenceCount =
            meter.CreateHistogram<long>(
                "agentops.tool.evidence.count",
                unit: "{item}",
                description:
                    "Number of evidence records returned by a tool.");
    }

    public void RecordInvestigation(
        string provider,
        string status,
        double durationMilliseconds)
    {
        var tags =
            new TagList
            {
                {
                    "gen_ai.provider.name",
                    provider
                },
                {
                    "agentops.status",
                    status
                }
            };

        _investigationCounter.Add(
            1,
            tags);

        _investigationDuration.Record(
            durationMilliseconds,
            tags);

        if (!string.Equals(
                status,
                "completed",
                StringComparison.OrdinalIgnoreCase))
        {
            _investigationFailureCounter.Add(
                1,
                tags);
        }
    }

    public void RecordToolInvocation(
        string toolName,
        string status,
        double durationMilliseconds,
        int evidenceCount)
    {
        var tags =
            new TagList
            {
                {
                    "agentops.tool.name",
                    toolName
                },
                {
                    "agentops.status",
                    status
                }
            };

        _toolInvocationCounter.Add(
            1,
            tags);

        _toolDuration.Record(
            durationMilliseconds,
            tags);

        _evidenceCount.Record(
            evidenceCount,
            tags);

        if (!string.Equals(
                status,
                "completed",
                StringComparison.OrdinalIgnoreCase))
        {
            _toolFailureCounter.Add(
                1,
                tags);
        }
    }
}