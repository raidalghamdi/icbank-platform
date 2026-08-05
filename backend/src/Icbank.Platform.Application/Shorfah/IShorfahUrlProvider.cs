namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Port for the two absolute base URLs the Node source hardcoded directly into
/// <c>shorfah.ts</c> (BUSINESS-RULES.md §1.7: <c>https://icbank-platform-internal-comms.vercel.app</c>
/// and <c>https://workspaceapi-server-production-9087.up.railway.app</c>). This port makes both
/// configuration (<c>Shorfah:FrontendBaseUrl</c> / <c>Shorfah:ApiBaseUrl</c>) instead of literals,
/// per the task's explicit instruction that these "must become configuration ... not literals".
/// </summary>
public interface IShorfahUrlProvider
{
    /// <summary>Gets the absolute base URL of the internal-comms frontend, used for in-app deep links.</summary>
    string FrontendBaseUrl { get; }

    /// <summary>Gets the absolute base URL of this API, used for PDF/download links.</summary>
    string ApiBaseUrl { get; }
}
