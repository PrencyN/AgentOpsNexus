using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

namespace AgentOpsNexus.AgentApi.Security;

public static class
    AuthenticationServiceExtensions
{
    public static IServiceCollection
        AddAgentOpsAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
    {
        var authenticationEnabled =
            configuration.GetValue<bool>(
                $"{AuthenticationOptions.SectionName}" +
                ":Enabled");

        services
            .AddOptions<
                AuthenticationOptions>()
            .Bind(
                configuration.GetSection(
                    AuthenticationOptions
                        .SectionName));

        if (authenticationEnabled)
        {
            services
                .AddAuthentication(
                    JwtBearerDefaults
                        .AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(
                    configuration,
                    "AzureAd");
        }
        else
        {
            if (
                !environment.IsDevelopment() &&
                !environment.IsEnvironment(
                    "Testing"))
            {
                throw new InvalidOperationException(
                    "Entra authentication may only " +
                    "be disabled in Development or " +
                    "Testing environments.");
            }

            services
                .AddAuthentication(
                    options =>
                    {
                        options
                            .DefaultAuthenticateScheme =
                            DevelopmentAuthenticationHandler
                                .SchemeName;

                        options
                            .DefaultChallengeScheme =
                            DevelopmentAuthenticationHandler
                                .SchemeName;

                        options
                            .DefaultForbidScheme =
                            DevelopmentAuthenticationHandler
                                .SchemeName;
                    })
                .AddScheme<
                    AuthenticationSchemeOptions,
                    DevelopmentAuthenticationHandler>(
                        DevelopmentAuthenticationHandler
                            .SchemeName,
                        _ =>
                        {
                        });
        }

        services.AddAuthorization(
            options =>
            {
                options.AddPolicy(
                    AgentOpsPolicies.ReadRequests,
                    policy =>
                    {
                        policy
                            .RequireAuthenticatedUser();

                        policy.RequireRole(
                            AgentOpsRoles
                                .RequestReaders);
                    });

                options.AddPolicy(
                    AgentOpsPolicies.CreateRequests,
                    policy =>
                    {
                        policy
                            .RequireAuthenticatedUser();

                        policy.RequireRole(
                            AgentOpsRoles
                                .RequestCreators);
                    });

                options.AddPolicy(
                    AgentOpsPolicies.ApproveActions,
                    policy =>
                    {
                        policy
                            .RequireAuthenticatedUser();

                        policy.RequireRole(
                            AgentOpsRoles
                                .RequestApprovers);
                    });

                options.AddPolicy(
                    AgentOpsPolicies.Administration,
                    policy =>
                    {
                        policy
                            .RequireAuthenticatedUser();

                        policy.RequireRole(
                            AgentOpsRoles
                                .Administrator);
                    });
            });

        return services;
    }
}