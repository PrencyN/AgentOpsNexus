using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentOpsNexus.AgentApi.Security;

public sealed class DevelopmentAuthenticationHandler
    : AuthenticationHandler<
        AuthenticationSchemeOptions>
{
    public const string SchemeName =
        "AgentOpsDevelopment";

    public DevelopmentAuthenticationHandler(
        IOptionsMonitor<
            AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(
            options,
            logger,
            encoder)
    {
    }

    protected override Task<
        AuthenticateResult>
        HandleAuthenticateAsync()
    {
        var claims =
            new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    "local-investigator"),

                new(
                    ClaimTypes.Name,
                    "Local Investigator"),

                new(
                    "sub",
                    "local-investigator"),

                new(
                    "scp",
                    AgentOpsScopes.AccessAsUser),

                new(
                    ClaimTypes.Role,
                    AgentOpsRoles.Investigator)
            };

        var identity =
            new ClaimsIdentity(
                claims,
                SchemeName,
                ClaimTypes.Name,
                ClaimTypes.Role);

        var principal =
            new ClaimsPrincipal(
                identity);

        var ticket =
            new AuthenticationTicket(
                principal,
                SchemeName);

        return Task.FromResult(
            AuthenticateResult.Success(
                ticket));
    }
}
