namespace Icbank.Platform.Application.Auth;

/// <summary>Application-layer read-only view of the Azure AD SSO configuration, so handlers never need to reference ASP.NET Core's <c>IOptions&lt;T&gt;</c> directly.</summary>
public interface ISsoOptionsProvider
{
    /// <summary>Gets a value indicating whether Azure AD SSO is enabled.</summary>
    bool Enabled { get; }

    /// <summary>Gets the domain restriction, if any (BUSINESS-RULES.md §11.3).</summary>
    string? AllowedDomain { get; }

    /// <summary>Gets the configured allow-list of post-login redirect targets (closes SEC-11).</summary>
    IReadOnlyCollection<string> AllowedRedirectTargets { get; }
}
