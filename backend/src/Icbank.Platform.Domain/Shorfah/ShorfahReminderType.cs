namespace Icbank.Platform.Domain.Shorfah;

/// <summary>Kind of a reminder notification (DATA-MODEL.md section 5).</summary>
public enum ShorfahReminderType
{
    /// <summary>The initial assignment reminder.</summary>
    Initial = 0,

    /// <summary>A reminder sent after the SLA deadline passed.</summary>
    Overdue = 1,

    /// <summary>A reminder sent shortly before the SLA deadline.</summary>
    PreDue = 2,
}
