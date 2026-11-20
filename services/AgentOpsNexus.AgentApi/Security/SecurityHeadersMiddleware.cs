namespace AgentOpsNexus.AgentApi.Security;

/// <summary>
/// Adds defensive HTTP response headers to every API response.
///
/// The API returns JSON and Server-Sent Events. It does not render HTML,
/// execute scripts, or host frontend resources, so the Content Security
/// Policy can use a restrictive default-src policy.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        AddSecurityHeaders(
            context.Response.Headers);

        await _next(context);
    }

    private static void AddSecurityHeaders(
        IHeaderDictionary headers)
    {
        headers.TryAdd(
            "X-Content-Type-Options",
            "nosniff");

        headers.TryAdd(
            "Referrer-Policy",
            "no-referrer");

        headers.TryAdd(
            "X-Frame-Options",
            "DENY");

        headers.TryAdd(
            "Content-Security-Policy",
            "default-src 'none'; " +
            "base-uri 'none'; " +
            "form-action 'none'; " +
            "frame-ancestors 'none';");

        headers.TryAdd(
            "Permissions-Policy",
            "accelerometer=(), " +
            "ambient-light-sensor=(), " +
            "autoplay=(), " +
            "camera=(), " +
            "display-capture=(), " +
            "encrypted-media=(), " +
            "fullscreen=(), " +
            "geolocation=(), " +
            "gyroscope=(), " +
            "magnetometer=(), " +
            "microphone=(), " +
            "midi=(), " +
            "payment=(), " +
            "picture-in-picture=(), " +
            "publickey-credentials-get=(), " +
            "screen-wake-lock=(), " +
            "usb=(), " +
            "web-share=(), " +
            "xr-spatial-tracking=()");

        headers.TryAdd(
            "Cross-Origin-Opener-Policy",
            "same-origin");

        headers.TryAdd(
            "Cross-Origin-Resource-Policy",
            "same-site");

        /*
         * Prevent browsers and intermediary caches from retaining
         * incident descriptions or generated investigation responses.
         */
        headers.TryAdd(
            "Cache-Control",
            "no-store, no-cache, must-revalidate");

        headers.TryAdd(
            "Pragma",
            "no-cache");

        headers.TryAdd(
            "Expires",
            "0");

        /*
         * Avoid exposing ASP.NET Core implementation details when a
         * hosting environment adds this header.
         */
        headers.Remove(
            "X-Powered-By");
    }
}