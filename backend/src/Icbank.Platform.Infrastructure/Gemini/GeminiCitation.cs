namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// One grounding citation, built by pairing a <c>groundingSupports[].segment</c> text range with
/// the <c>groundingChunks[].web</c> entry(ies) it points at (real <c>v1beta generateContent</c>
/// response shape -- NOT the <c>url_citation</c> annotation shape used by some other Gemini API
/// surfaces). See BUSINESS-RULES.md §4 grounding safeguard.
/// </summary>
/// <param name="Url">
/// <c>groundingChunks[].web.uri</c> verbatim. This is a Google redirect
/// (<c>https://vertexaisearch.cloud.google.com/grounding-api-redirect/&lt;token&gt;</c>), not the
/// publisher's real URL -- Google does not expose the resolved link in this API response. Whether
/// these redirects remain valid for as long as this platform's 7-day report cache is unverified;
/// see GEMINI-ADAPTER-NOTES.md.
/// </param>
/// <param name="Title">
/// <c>groundingChunks[].web.title</c> verbatim -- typically the bare publisher domain (e.g.
/// <c>alriyadh.com</c>), not a full article headline. This is the only human-readable indication
/// of the real source and must be persisted alongside <see cref="Url"/> so the UI can still show
/// the publisher if the redirect later breaks.
/// </param>
/// <param name="StartIndex">The start character offset (<c>segment.startIndex</c>) in the response text the citation covers.</param>
/// <param name="EndIndex">The end character offset (<c>segment.endIndex</c>) in the response text the citation covers.</param>
public sealed record GeminiCitation(string Url, string? Title, int StartIndex, int EndIndex);
