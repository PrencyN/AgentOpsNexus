namespace AgentOpsNexus.AgentApi.Security;

public sealed record CurrentUser(
    string SubjectId,
    string DisplayName,
    bool IsAdministrator);