using Icbank.Platform.Api.Auth;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Icbank.Platform.Api.Extensions;

/// <summary>
/// Registers the 72 generated <c>{pageSlug}:{verb}</c> authorization policies (18 pages × 4
/// verbs) plus the distinct <c>super-admin</c> policy, backed by exactly two requirement handlers
/// (DOTNET-CONVENTIONS.md §5.4: "one requirement handler, 72 generated policies, not 72
/// hand-written AddPolicy calls").
/// </summary>
public static class AuthorizationPolicyExtensions
{
    /// <summary>The policy name for the distinct super-admin capability (closes SEC-01).</summary>
    public const string SuperAdminPolicyName = "super-admin";

    /// <summary>The policy name for cron/service-to-service API key authentication (closes SEC-13).</summary>
    public const string CronApiKeyPolicyName = "cron-api-key";

    /// <summary>Adds the permission-based policies and their handlers.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddIcbankAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, SuperAdminAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, CronApiKeyAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(SuperAdminPolicyName, policy => policy.Requirements.Add(SuperAdminRequirement.Instance))
            .AddPolicy(CronApiKeyPolicyName, policy => policy.Requirements.Add(CronApiKeyRequirement.Instance));

        foreach (var pageSlug in PageSlugs.All)
        {
            foreach (PermissionVerb verb in Enum.GetValues<PermissionVerb>())
            {
                var policyName = PermissionRequirementFactory.BuildPolicyName(pageSlug, verb);
                services.AddAuthorizationBuilder()
                    .AddPolicy(policyName, policy => policy.Requirements.Add(new PermissionRequirement(pageSlug, verb)));
            }
        }

        return services;
    }
}
