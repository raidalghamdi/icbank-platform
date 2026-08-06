using Azure.Communication.Email;
using Azure.Storage.Blobs;
using FluentAssertions;
using Icbank.Platform.Application.Dashboard;
using Icbank.Platform.Application.Designs.Composer;
using Icbank.Platform.Application.Designs.IconEvent;
using Icbank.Platform.Application.InternationalDays;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Application.Weekend;
using Icbank.Platform.Infrastructure;
using Icbank.Platform.Infrastructure.Dashboard;
using Icbank.Platform.Infrastructure.Designs;
using Icbank.Platform.Infrastructure.Gemini;
using Icbank.Platform.Infrastructure.InternationalDays;
using Icbank.Platform.Infrastructure.MediaMonitoring;
using Icbank.Platform.Infrastructure.Notifications;
using Icbank.Platform.Infrastructure.Shorfah;
using Icbank.Platform.Infrastructure.Storage;
using Icbank.Platform.Infrastructure.Weekend;
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

    [Fact]
    public void AddInfrastructure_GeminiApiKeyUnset_ResolvesTemplateImplementationsForAllTwelvePorts()
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>());

        provider.GetRequiredService<IExecutiveSummaryGenerator>().Should().BeOfType<TemplateExecutiveSummaryGenerator>();
        provider.GetRequiredService<IWeekendContentGenerator>().Should().BeOfType<TemplateWeekendContentGenerator>();
        provider.GetRequiredService<IWeekStartMessageGenerator>().Should().BeOfType<TemplateWeekStartMessageGenerator>();
        provider.GetRequiredService<IInternationalDaySearchProvider>().Should().BeOfType<TemplateInternationalDaySearchProvider>();
        provider.GetRequiredService<IMediaReportNarrativeGenerator>().Should().BeOfType<TemplateMediaReportNarrativeGenerator>();
        provider.GetRequiredService<IPromptExecutionEngine>().Should().BeOfType<TemplatePromptExecutionEngine>();
        provider.GetRequiredService<IFinalReportSectionGenerator>().Should().BeOfType<TemplateFinalReportSectionGenerator>();
        provider.GetRequiredService<IExecutiveSummaryRegenerator>().Should().BeOfType<TemplateExecutiveSummaryRegenerator>();
        provider.GetRequiredService<IReportArchiveQaEngine>().Should().BeOfType<TemplateReportArchiveQaEngine>();
        provider.GetRequiredService<IShorfahSectionContentGenerator>().Should().BeOfType<TemplateShorfahSectionContentGenerator>();
        provider.GetRequiredService<IIconEventDesignExtractor>().Should().BeOfType<TemplateIconEventDesignExtractor>();
        provider.GetRequiredService<IBackgroundImageGenerator>().Should().BeOfType<TemplateBackgroundImageGenerator>();

        // Why: this port is HTML-to-PNG rasterization, not an AI call -- it must stay
        // Template-backed regardless of whether a Gemini key is configured.
        provider.GetRequiredService<IIconEventImageRenderer>().Should().BeOfType<TemplateIconEventImageRenderer>();
    }

    [Theory]
    [InlineData("GEMINI_API_KEY")]
    [InlineData("GOOGLE_AI_API_KEY")]
    [InlineData("AI_INTEGRATIONS_GEMINI_API_KEY")]
    public void AddInfrastructure_GeminiApiKeyConfigured_ResolvesGeminiImplementationsForTwelvePorts(string keyName)
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>
        {
            [keyName] = "test-key-value",
        });

        provider.GetRequiredService<IExecutiveSummaryGenerator>().Should().BeOfType<GeminiExecutiveSummaryGenerator>();
        provider.GetRequiredService<IWeekendContentGenerator>().Should().BeOfType<GeminiWeekendContentGenerator>();
        provider.GetRequiredService<IWeekStartMessageGenerator>().Should().BeOfType<GeminiWeekStartMessageGenerator>();
        provider.GetRequiredService<IInternationalDaySearchProvider>().Should().BeOfType<GeminiInternationalDaySearchProvider>();
        provider.GetRequiredService<IMediaReportNarrativeGenerator>().Should().BeOfType<GeminiMediaReportNarrativeGenerator>();
        provider.GetRequiredService<IPromptExecutionEngine>().Should().BeOfType<GeminiPromptExecutionEngine>();
        provider.GetRequiredService<IFinalReportSectionGenerator>().Should().BeOfType<GeminiFinalReportSectionGenerator>();
        provider.GetRequiredService<IExecutiveSummaryRegenerator>().Should().BeOfType<GeminiExecutiveSummaryRegenerator>();
        provider.GetRequiredService<IReportArchiveQaEngine>().Should().BeOfType<GeminiReportArchiveQaEngine>();
        provider.GetRequiredService<IShorfahSectionContentGenerator>().Should().BeOfType<GeminiShorfahSectionContentGenerator>();
        provider.GetRequiredService<IIconEventDesignExtractor>().Should().BeOfType<GeminiIconEventDesignExtractor>();
        provider.GetRequiredService<IBackgroundImageGenerator>().Should().BeOfType<GeminiBackgroundImageGenerator>();

        // Why: even with a key configured, this port stays Template-backed -- it was never an AI call.
        provider.GetRequiredService<IIconEventImageRenderer>().Should().BeOfType<TemplateIconEventImageRenderer>();
        provider.GetRequiredService<IGeminiClient>().Should().BeOfType<GeminiClient>();
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
