namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="FinalMediaReportsController.SendEmailAsync"/>.</summary>
/// <param name="Recipients">The recipient email addresses.</param>
/// <param name="Subject">The optional email subject override.</param>
public sealed record SendFinalMediaReportEmailRequest(IReadOnlyList<string> Recipients, string? Subject);
