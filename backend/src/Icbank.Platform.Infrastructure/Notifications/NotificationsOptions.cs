namespace Icbank.Platform.Infrastructure.Notifications;

/// <summary>Strongly-typed binding of the <c>Notifications</c> configuration section.</summary>
public sealed class NotificationsOptions
{
    /// <summary>Gets or sets the configuration section name.</summary>
    public const string SectionName = "Notifications";

    /// <summary>
    /// Gets or sets which backend implementation to register for <c>IReportEmailSender</c> and
    /// <c>IShorfahNotificationSender</c>: <see cref="NotificationsProvider.Null"/> (default -- the
    /// existing honest no-op implementations, no cloud dependency) or
    /// <see cref="NotificationsProvider.AzureCommunicationServices"/> (deployed environments;
    /// app-service.bicep sets this via the <c>Notifications__Provider</c> app setting).
    /// </summary>
    public NotificationsProvider Provider { get; set; } = NotificationsProvider.Null;
}
