namespace Icbank.Platform.Api.Controllers;

/// <summary>A single requested delivery channel in <see cref="SendWeekendReportRequest"/>.</summary>
public sealed class SendWeekendReportChannelRequest
{
    /// <summary>Gets or sets the channel type: <c>email</c>, <c>sms</c>, or <c>whatsapp</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the delivery target.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional sub-kind, e.g. <c>work</c> for email.</summary>
    public string? Kind { get; set; }
}
