using Azure.Communication.Email;
using Azure.Storage.Blobs;
using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Infrastructure;
using Icbank.Platform.Infrastructure.MediaMonitoring;
using Icbank.Platform.Infrastructure.Notifications;
using Icbank.Platform.Infrastructure.Shorfah;
using Icbank.Platform.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Icbank.Platform.UnitTests.Infrastructure;

/// <summary>
/// Verifies that <c>DependencyInjection.AddInfrastructure</c> resolves the correct concrete type
/// for each <c>ObjectStorage:Provider</c> and <c>Notifications:Provider</c> value -- including
/// each port's default -- so a misconfigured or drifted provider switch is caught by a fast unit
/// test rather than discovered against a real (or missing) cloud dependency.
/// </summary>
public sealed class DependencyInjectionTests
{
    private const string DefaultConnectionString = "Server=(localdb)\\mssqllocaldb;Database=icbank-di-test;";

    [Fact]
    public void AddInfrastructure_ObjectStorageProviderUnset_ResolvesFileSystemImplementationsByDefault()
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>());

        provider.GetRequiredService<IObjectStorageReader>().Should().BeOfType<FileSystemObjectStorageReader>();
        provider.GetRequiredService<IObjectUploadUrlIssuer>().Should().BeOfType<FileSystemObjectUploadUrlIssuer>();
        provider.GetRequiredService<IObjectStorageWriter>().Should().BeOfType<FileSystemObjectStorageWriter>();
        provider.GetRequiredService<IObjectStorageDeleter>().Should().BeOfType<FileSystemObjectStorageDeleter>();
    }

    [Fact]
    public void AddInfrastructure_ObjectStorageProviderFileSystem_ResolvesFileSystemImplementations()
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ObjectStorage:Provider"] = "FileSystem",
        });

        provider.GetRequiredService<IObjectStorageReader>().Should().BeOfType<FileSystemObjectStorageReader>();
        provider.GetRequiredService<IObjectUploadUrlIssuer>().Should().BeOfType<FileSystemObjectUploadUrlIssuer>();
        provider.GetRequiredService<IObjectStorageWriter>().Should().BeOfType<FileSystemObjectStorageWriter>();
        provider.GetRequiredService<IObjectStorageDeleter>().Should().BeOfType<FileSystemObjectStorageDeleter>();
    }

    [Fact]
    public void AddInfrastructure_ObjectStorageProviderAzureBlob_ResolvesAzureBlobImplementations()
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ObjectStorage:Provider"] = "AzureBlob",
            ["ObjectStorage:AzureBlob:ServiceUri"] = "https://icbank-di-test.blob.core.windows.net",
        });

        provider.GetRequiredService<IObjectStorageReader>().Should().BeOfType<AzureBlobObjectStorageReader>();
        provider.GetRequiredService<IObjectUploadUrlIssuer>().Should().BeOfType<AzureBlobObjectUploadUrlIssuer>();
        provider.GetRequiredService<IObjectStorageWriter>().Should().BeOfType<AzureBlobObjectStorageWriter>();
        provider.GetRequiredService<IObjectStorageDeleter>().Should().BeOfType<AzureBlobObjectStorageDeleter>();
        provider.GetRequiredService<BlobServiceClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_ObjectStorageProviderAzureBlobWithoutServiceUri_ThrowsClearError()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = DefaultConnectionString,
            ["ObjectStorage:Provider"] = "AzureBlob",
        });

        Action act = () => services.AddInfrastructure(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ObjectStorage:AzureBlob:ServiceUri*");
    }

    [Fact]
    public void AddInfrastructure_NotificationsProviderUnset_ResolvesNullImplementationsByDefault()
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>());

        provider.GetRequiredService<IReportEmailSender>().Should().BeOfType<NullReportEmailSender>();
        provider.GetRequiredService<IShorfahNotificationSender>().Should().BeOfType<NullShorfahNotificationSender>();
    }

    [Fact]
    public void AddInfrastructure_NotificationsProviderNull_ResolvesNullImplementations()
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Notifications:Provider"] = "Null",
        });

        provider.GetRequiredService<IReportEmailSender>().Should().BeOfType<NullReportEmailSender>();
        provider.GetRequiredService<IShorfahNotificationSender>().Should().BeOfType<NullShorfahNotificationSender>();
    }

    [Fact]
    public void AddInfrastructure_NotificationsProviderAzureCommunicationServices_ResolvesAcsImplementations()
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Notifications:Provider"] = "AzureCommunicationServices",
            ["Notifications:AzureCommunicationServices:Endpoint"] = "https://icbank-di-test.communication.azure.com",
        });

        provider.GetRequiredService<IReportEmailSender>().Should().BeOfType<AzureCommunicationServicesReportEmailSender>();
        provider.GetRequiredService<IShorfahNotificationSender>().Should().BeOfType<AzureCommunicationServicesShorfahNotificationSender>();
        provider.GetRequiredService<EmailClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_NotificationsProviderAzureCommunicationServicesWithoutEndpoint_ThrowsClearError()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = DefaultConnectionString,
            ["Notifications:Provider"] = "AzureCommunicationServices",
        });

        Action act = () => services.AddInfrastructure(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Notifications:AzureCommunicationServices:Endpoint*");
    }

    private static IConfiguration BuildConfiguration(IReadOnlyDictionary<string, string?> overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = DefaultConnectionString,
        };
        foreach (KeyValuePair<string, string?> pair in overrides)
        {
            values[pair.Key] = pair.Value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values!).Build();
    }

    private static ServiceProvider BuildProvider(IReadOnlyDictionary<string, string?> overrides)
    {
        var services = new ServiceCollection();
        IConfiguration configuration = BuildConfiguration(overrides);
        services.AddInfrastructure(configuration);

        // Why: BuildServiceProvider without ValidateOnBuild -- this test resolves specific ports
        // to assert their concrete type, it does not attempt to construct the full object graph
        // (e.g. AppDbContext, which would require an actual reachable SQL Server).
        return services.BuildServiceProvider();
    }
}
