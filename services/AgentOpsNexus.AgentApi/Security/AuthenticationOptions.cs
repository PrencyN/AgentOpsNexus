namespace AgentOpsNexus.AgentApi.Security;

public sealed class AuthenticationOptions
{
    public const string SectionName =
        "Authentication";

    public bool Enabled
    {
        get;
        init;
    }
}