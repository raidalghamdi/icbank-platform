namespace Icbank.Platform.Infrastructure.Notifications;

/// <summary>Strongly-typed binding of the <c>Notifications:AzureCommunicationServices</c> configuration section.</summary>
public sealed class AzureCommunicationServicesOptions
{
    /// <summary>Gets or sets the configuration section name.</summary>
    public const string SectionName = "Notifications:AzureCommunicationServices";

    /// <summary>Gets or sets the Communication Services resource endpoint (e.g. <c>https://icbank-prod-acs.communication.azure.com</c>). Not a secret.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the verified sender address configured on the Communication Services Email domain.</summary>
    public string SenderAddress { get; set; } = string.Empty;
}
