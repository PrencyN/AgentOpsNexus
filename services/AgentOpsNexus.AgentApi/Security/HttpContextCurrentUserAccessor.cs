using System.Security.Claims;

namespace AgentOpsNexus.AgentApi.Security;

public sealed class HttpContextCurrentUserAccessor
    : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor
        _httpContextAccessor;

    public HttpContextCurrentUserAccessor(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor =
            httpContextAccessor;
    }

    public CurrentUser GetCurrentUser()
    {
        var httpContext =
            _httpContextAccessor.HttpContext
            ??
            throw new InvalidOperationException(
                "No active HTTP context is available.");

        var principal =
            httpContext.User;

        if (
            principal.Identity?.IsAuthenticated
            !=
            true)
        {
            throw new InvalidOperationException(
                "An authenticated user is required.");
        }

        var subjectId =
            FirstNonEmptyClaim(
                principal,
                "sub",
                ClaimTypes.NameIdentifier,
                "oid");

        if (string.IsNullOrWhiteSpace(
                subjectId))
        {
            throw new InvalidOperationException(
                "The authenticated principal does not " +
                "contain a stable subject identifier.");
        }

        var displayName =
            FirstNonEmptyClaim(
                principal,
                ClaimTypes.Name,
                "name",
                "preferred_username");

        if (string.IsNullOrWhiteSpace(
                displayName))
        {
            displayName =
                subjectId;
        }

        subjectId =
            subjectId.Trim();

        displayName =
            displayName.Trim();

        if (subjectId.Length > 200)
        {
            throw new InvalidOperationException(
                "The authenticated subject identifier " +
                "exceeds 200 characters.");
        }

        if (displayName.Length > 200)
        {
            displayName =
                displayName[..200];
        }

        return new CurrentUser(
            SubjectId:
                subjectId,

            DisplayName:
                displayName,

            IsAdministrator:
                principal.IsInRole(
                    AgentOpsRoles.Administrator));
    }

    private static string?
        FirstNonEmptyClaim(
            ClaimsPrincipal principal,
            params string[] claimTypes)
    {
        foreach (
            var claimType in claimTypes)
        {
            var value =
                principal.FindFirst(
                    claimType)
                ?.Value;

            if (!string.IsNullOrWhiteSpace(
                    value))
            {
                return value;
            }
        }

        return null;
    }
}