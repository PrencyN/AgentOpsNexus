using System.Runtime.CompilerServices;
using AgentOpsNexus.AgentApi.Contracts;
using AgentOpsNexus.AgentApi.Domain;

namespace AgentOpsNexus.AgentApi.Agents;

public sealed class MockIncidentAgent
    : IIncidentAgent
{
    public async IAsyncEnumerable<
        AgentStreamEvent>
        InvestigateAsync(
            AgentRequestRecord request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken)
    {
        yield return CreateEvent(
            type:
                "agent.started",
            message:
                "Investigation started for the " +
                $"{request.Environment} environment.");

        await Task.Delay(
            millisecondsDelay: 600,
            cancellationToken);

        yield return CreateEvent(
            type:
                "agent.progress",
            message:
                "Classifying the incident and " +
                "identifying potentially affected services.");

        await Task.Delay(
            millisecondsDelay: 600,
            cancellationToken);

        if (request.SimulateFailure)
        {
            throw new HttpRequestException(
                "The simulated downstream AI " +
                "service is unavailable.");
        }

        yield return CreateEvent(
            type:
                "agent.progress",
            message:
                "Retrieving bounded deployment evidence.");

        await Task.Delay(
            millisecondsDelay: 400,
            cancellationToken);

        yield return CreateEvent(
            type:
                "agent.message.delta",
            message:
                """
                ## Summary

                """);

        yield return CreateEvent(
            type:
                "agent.message.delta",
            message:
                "The incident has been classified as " +
                $"{request.Severity.ToLowerInvariant()} " +
                "severity in the " +
                $"{request.Environment} environment.\n\n");

        await Task.Delay(
            millisecondsDelay: 400,
            cancellationToken);

        yield return CreateEvent(
            type:
                "agent.message.delta",
            message:
                """
                ## Known Facts

                - Checkout failures increased after a recent PaymentService deployment.
                - The current provider is the deterministic Mock agent used for automated testing and offline demonstrations.

                """);

        await Task.Delay(
            millisecondsDelay: 400,
            cancellationToken);

        yield return CreateEvent(
            type:
                "agent.message.delta",
            message:
                """
                ## Deployment Evidence

                Mock deployment evidence shows that PaymentService version 2.4.1 was deployed successfully [DEP-EV-001].

                A previous PaymentService deployment is available for comparison [DEP-EV-002].

                """);

        await Task.Delay(
            millisecondsDelay: 400,
            cancellationToken);

        yield return CreateEvent(
            type:
                "agent.message.delta",
            message:
                """
                ## Initial Assessment

                The deployment timing supports an investigation hypothesis. Correlation does not prove causation.

                """);

        await Task.Delay(
            millisecondsDelay: 400,
            cancellationToken);

        yield return CreateEvent(
            type:
                "agent.message.delta",
            message:
                """
                ## Possible Hypotheses

                1. PaymentService version 2.4.1 introduced a behavioral regression.
                2. A configuration difference became active during deployment.
                3. A downstream payment provider became unstable at approximately the same time.
                4. The deployment and incident are correlated but not causally related.

                """);

        await Task.Delay(
            millisecondsDelay: 400,
            cancellationToken);

        yield return CreateEvent(
            type:
                "agent.message.delta",
            message:
                """
                ## Evidence Still Required

                - Representative application error logs
                - Distributed traces for failed requests
                - Error-rate and latency metrics
                - Configuration comparisons
                - Downstream dependency status
                - Reproduction steps
                - Confirmation from the actual deployment platform

                """);

        await Task.Delay(
            millisecondsDelay: 400,
            cancellationToken);

        yield return CreateEvent(
            type:
                "agent.message.delta",
            message:
                """
                ## Recommended Next Steps

                1. Compare error rates and latency before and after the deployment.
                2. Correlate failed request identifiers with deployment timestamps.
                3. Review configuration changes associated with PaymentService version 2.4.1.
                4. Verify downstream payment-provider health.
                5. Compare the current release with the previous successful deployment.

                """);

        await Task.Delay(
            millisecondsDelay: 400,
            cancellationToken);

        yield return CreateEvent(
            type:
                "agent.message.delta",
            message:
                """
                ## Risk and Approval Notes

                A rollback is a consequential action and requires explicit human approval.

                No rollback, restart, deployment, configuration change, database modification, or stakeholder notification has been executed.

                """);

        await Task.Delay(
            millisecondsDelay: 400,
            cancellationToken);

        yield return CreateEvent(
            type:
                "agent.message.delta",
            message:
                """
                ## Limitations

                The deployment records are deterministic mock evidence.

                The application has not examined real logs, metrics, traces, operational databases, source code, or runbooks.

                Production decisions require verification from the actual deployment platform, telemetry systems, and responsible service owners.
                """);

        await Task.Delay(
            millisecondsDelay: 300,
            cancellationToken);

        yield return CreateEvent(
            type:
                "agent.completed",
            message:
                "Investigation completed.");
    }

    private static AgentStreamEvent CreateEvent(
        string type,
        string message)
    {
        return new AgentStreamEvent(
            type,
            message,
            DateTimeOffset.UtcNow);
    }
}