using AgentOpsNexus.AgentApi.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AgentOpsNexus.AgentApi.UnitTests;

public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task
        InvokeAsync_AddsRequiredSecurityHeaders()
    {
        RequestDelegate next =
            context =>
            {
                context.Response.StatusCode = 200;
                return Task.CompletedTask;
            };

        var middleware =
            new SecurityHeadersMiddleware(next);

        var context =
            new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.Headers[
                "X-Content-Type-Options"]
            .ToString()
            .Should()
            .Be("nosniff");

        context.Response.Headers[
                "Referrer-Policy"]
            .ToString()
            .Should()
            .Be("no-referrer");

        context.Response.Headers[
                "X-Frame-Options"]
            .ToString()
            .Should()
            .Be("DENY");

        context.Response.Headers[
                "Content-Security-Policy"]
            .ToString()
            .Should()
            .Contain(
                "default-src 'none'");

        context.Response.Headers[
                "Content-Security-Policy"]
            .ToString()
            .Should()
            .Contain(
                "frame-ancestors 'none'");

        context.Response.Headers[
                "Permissions-Policy"]
            .ToString()
            .Should()
            .Contain("camera=()");

        context.Response.Headers[
                "Permissions-Policy"]
            .ToString()
            .Should()
            .Contain("microphone=()");

        context.Response.Headers[
                "Cache-Control"]
            .ToString()
            .Should()
            .Contain("no-store");
    }

    [Fact]
    public async Task
        InvokeAsync_InvokesNextMiddleware()
    {
        var nextCalled = false;

        RequestDelegate next =
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

        var middleware =
            new SecurityHeadersMiddleware(next);

        var context =
            new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task
        InvokeAsync_DoesNotOverwriteExistingHeader()
    {
        RequestDelegate next =
            _ => Task.CompletedTask;

        var middleware =
            new SecurityHeadersMiddleware(next);

        var context =
            new DefaultHttpContext();

        context.Response.Headers[
            "Referrer-Policy"] =
            "same-origin";

        await middleware.InvokeAsync(context);

        context.Response.Headers[
                "Referrer-Policy"]
            .ToString()
            .Should()
            .Be("same-origin");
    }
}