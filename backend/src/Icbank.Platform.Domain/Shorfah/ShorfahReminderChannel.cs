namespace Icbank.Platform.Domain.Shorfah;

/// <summary>Delivery channel for a reminder (DATA-MODEL.md section 5).</summary>
public enum ShorfahReminderChannel
{
    /// <summary>In-app notification only.</summary>
    InApp = 0,

    /// <summary>Email only.</summary>
    Email = 1,

    /// <summary>Both in-app and email.</summary>
    Both = 2,
}
