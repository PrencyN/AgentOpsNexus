namespace AgentOpsNexus.AgentApi.Security;

public interface ICurrentUserAccessor
{
    CurrentUser GetCurrentUser();
}