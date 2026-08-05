using System.Text.Json;

namespace Icbank.Platform.Api.Controllers;

/// <summary>The strict-schema daily-report upsert request body (<c>POST /daily-report</c>).</summary>
public sealed class DailyReportUpsertRequest
{
    /// <summary>Gets or sets the ISO (<c>yyyy-MM-dd</c>) report date.</summary>
    public string ReportDate { get; set; } = string.Empty;

    /// <summary>Gets or sets the freeform report payload.</summary>
    public JsonElement ReportData { get; set; }
}
