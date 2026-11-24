using AgentOpsNexus.AgentApi.Security;

namespace AgentOpsNexus.AgentApi.IntegrationTests.Infrastructure;

public static class TestIdentity
{
    public const string AuthenticationScheme =
        "AgentOpsIntegrationTest";

    public const string SubjectHeader =
        "X-Test-Subject";

    public const string NameHeader =
        "X-Test-Name";

    public const string RolesHeader =
        "X-Test-Roles";

    public const string ScopeHeader =
        "X-Test-Scope";

    public const string DefaultSubject =
        "integration-investigator";

    public const string DefaultName =
        "Integration Investigator";

    public const string DefaultScope =
        AgentOpsScopes.AccessAsUser;

    public const string DefaultRoles =
        AgentOpsRoles.Investigator;
}