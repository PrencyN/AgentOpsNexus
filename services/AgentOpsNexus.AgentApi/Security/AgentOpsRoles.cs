namespace AgentOpsNexus.AgentApi.Security;

public static class AgentOpsRoles
{
    public const string Investigator =
        "Incident.Investigator";

    public const string Reader =
        "Incident.Reader";

    public const string Approver =
        "Incident.Approver";

    public const string Administrator =
        "AgentOps.Admin";

    public static readonly string[]
        RequestReaders =
        [
            Investigator,
            Reader,
            Approver,
            Administrator
        ];

    public static readonly string[]
        RequestCreators =
        [
            Investigator,
            Administrator
        ];

    public static readonly string[]
        RequestApprovers =
        [
            Approver,
            Administrator
        ];
}
