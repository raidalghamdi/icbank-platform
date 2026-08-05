namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /weekend/send</c>.</summary>
public sealed class SendWeekendReportRequest
{
    /// <summary>Gets or sets the requested delivery channels.</summary>
    public List<SendWeekendReportChannelRequest> Channels { get; set; } = new();

    /// <summary>Gets or sets the requested SMS/WhatsApp provider name.</summary>
    public string? Provider { get; set; }

    /// <summary>Gets or sets the reporting period label.</summary>
    public string? Period { get; set; }
}
