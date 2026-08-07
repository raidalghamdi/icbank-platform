using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Logs a wizard-answer audit trail entry (<c>POST /qa-queries</c>). Closes DEFECT-LOG.md SEC-02:
/// the Node source allowed anyone to write to this audit table with no authentication at all --
/// an attacker could pollute the audit log itself; this port requires <c>media_monitoring:view</c>
/// and stamps the authenticated caller's id rather than trusting a client-supplied identity.
/// </summary>
/// <param name="ActorUserId">The id of the authenticated caller.</param>
/// <param name="Period">The requested period, free text.</param>
/// <param name="Audience">The requested audience tier, free text.</param>
/// <param name="Sources">The requested source list.</param>
/// <param name="FocusTopics">The requested focus topics, free text.</param>
/// <param name="Language">The requested output language, free text.</param>
/// <param name="Recipients">The intended recipients, free text.</param>
/// <param name="Mode">The wizard mode: generate or search.</param>
public sealed record LogWizardQaQueryCommand(
    int ActorUserId,
    string? Period,
    string? Audience,
    IReadOnlyList<string>? Sources,
    string? FocusTopics,
    string? Language,
    string? Recipients,
    string? Mode) : IRequest<Result<int>>;
