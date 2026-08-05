namespace Icbank.Platform.Infrastructure.Notifications;

/// <summary>The selectable notification-sending backend for report/Shorfah email.</summary>
public enum NotificationsProvider
{
    /// <summary>The existing honest no-op implementations. Default; no cloud dependency.</summary>
    Null = 0,

    /// <summary>Azure Communication Services Email.</summary>
    AzureCommunicationServices = 1,
}
