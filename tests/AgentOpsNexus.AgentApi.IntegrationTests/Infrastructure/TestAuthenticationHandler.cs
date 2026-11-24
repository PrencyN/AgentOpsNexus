using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentOpsNexus.AgentApi.IntegrationTests.Infrastructure;

public sealed class TestAuthenticationHandler
    : AuthenticationHandler<
        AuthenticationSchemeOptions>
{
    public const string AnonymousSubject =
        "anonymous";

    public TestAuthenticationHandler(
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
        var subject =
            GetHeader(
                TestIdentity.SubjectHeader,
                TestIdentity.DefaultSubject);

        if (string.Equals(
                subject,
                AnonymousSubject,
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        var name =
            GetHeader(
                TestIdentity.NameHeader,
                TestIdentity.DefaultName);

        var scope =
            GetHeader(
                TestIdentity.ScopeHeader,
                TestIdentity.DefaultScope);

        var roles =
            GetHeader(
                    TestIdentity.RolesHeader,
                    TestIdentity.DefaultRoles)
                .Split(
                    ',',
                    StringSplitOptions
                        .RemoveEmptyEntries |
                    StringSplitOptions
                        .TrimEntries);

        var claims =
            new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    subject),

                new(
                    ClaimTypes.Name,
                    name),

                new(
                    "sub",
                    subject),

                new(
                    "scp",
                    scope)
            };

        claims.AddRange(
            roles.Select(
                role =>
                    new Claim(
                        ClaimTypes.Role,
                        role)));

        var identity =
            new ClaimsIdentity(
                claims,
                TestIdentity.AuthenticationScheme,
                ClaimTypes.Name,
                ClaimTypes.Role);

        var principal =
            new ClaimsPrincipal(
                identity);

        var ticket =
            new AuthenticationTicket(
                principal,
                TestIdentity.AuthenticationScheme);

        return Task.FromResult(
            AuthenticateResult.Success(
                ticket));
    }

    private string GetHeader(
        string name,
        string fallback)
    {
        var value =
            Request.Headers[name]
                .FirstOrDefault();

        return string.IsNullOrWhiteSpace(
            value)
            ? fallback
            : value.Trim();
    }
}