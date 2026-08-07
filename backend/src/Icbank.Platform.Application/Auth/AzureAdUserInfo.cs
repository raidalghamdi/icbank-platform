namespace Icbank.Platform.Application.Auth;

/// <summary>Result of exchanging an Azure AD authorization code for tokens and claims.</summary>
/// <param name="AzureObjectId">The user's stable Azure AD object id.</param>
/// <param name="Email">The user's email/UPN as returned by Azure AD.</param>
/// <param name="Name">The user's display name as returned by Azure AD.</param>
public sealed record AzureAdUserInfo(string AzureObjectId, string Email, string Name);
