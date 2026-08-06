extern alias identity;

using Azure.Communication.Email;
using Azure.Storage.Blobs;
using Icbank.Platform.Application.Auth;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Infrastructure.Http;
using Icbank.Platform.Infrastructure.Identity;
using Icbank.Platform.Infrastructure.Notifications;
using Icbank.Platform.Infrastructure.Persistence;
using Icbank.Platform.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace Icbank.Platform.Infrastructure;

/// <summary>
/// Composition-root extension for the Infrastructure layer (R-BE-004). Wires EF Core, the audit
/// interceptor, the current-user port, and outbound HTTP resilience policies.
/// </summary>
public static class DependencyInjection
{
    private const int MaxRetryAttempts = 3; // R-BE-095 — named, not a bare "3".
    private const double CircuitBreakerFailureRatio = 0.5;
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Registers persistence, identity, and outbound-HTTP infrastructure services.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration, used for the SQL Server connection string.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddSecurityServices(services, configuration);
        AddDesignsServices(services);
        AddShorfahServices(services);
        AddSsoServices(services, configuration);
        AddSeeding(services, configuration);
        AddPersistence(services, configuration);
        AddResilientHttpClients(services);

        return services;
    }

    /// <summary>Registers the current-user/request-context ports, identity/auth ports, and other security-related singletons/scoped services.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration, used for JWT options binding.</param>
    private static void AddSecurityServices(IServiceCollection services, IConfiguration configuration)
    {
        AddCoreIdentityAndAuthServices(services);
        AddContentGenerationServices(services, configuration);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), "Jwt:SigningKey must be configured.")
            .ValidateOnStart();

        services.AddOptions<DownloadTokenOptions>()
            .Bind(configuration.GetSection(DownloadTokenOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), "DownloadTokens:SigningKey must be configured.")
            .ValidateOnStart();
    }

    /// <summary>Registers the current-user/request-context ports and the core identity/auth singletons/scoped services (R-BE-004: composition root only).</summary>
    /// <param name="services">The DI service collection.</param>
    private static void AddCoreIdentityAndAuthServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IRequestContext, HttpRequestContext>();

        // Why: identity/auth ports registered here per R-BE-004 (composition root only) —
        // Application/Api consume these purely through the interfaces defined in Application.
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IDownloadTokenService, DownloadTokenService>();
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<Icbank.Platform.Application.Common.Interfaces.IAsyncQueryExecutor, Persistence.EfAsyncQueryExecutor>();
        services.AddScoped<IResourceAuthorizationService, Security.ResourceAuthorizationService>();
        services.AddSingleton<ISafeStoragePathValidator, Security.SafeStoragePathValidator>();
        services.AddSingleton<IHtmlSanitizer, Security.GanssHtmlSanitizer>();
        services.AddSingleton<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();
        services.AddSingleton<Icbank.Platform.Application.Common.Interfaces.IDateTimeProvider, Identity.SystemDateTimeProvider>();
    }

    /// <summary>
    /// Registers storage-backed and notification-backed content-generation ports (dashboard,
    /// weekend, international days, media monitoring), plus the storage/notification provider
    /// switches themselves. Split out of <see cref="AddSecurityServices"/> purely to keep each
    /// method within the project's method-length limit: the registrations still all run as one
    /// step from <see cref="AddInfrastructure"/>'s point of view.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration, used for options binding and the storage/notification provider switches.</param>
    private static void AddContentGenerationServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<Storage.ObjectStorageOptions>().Bind(configuration.GetSection(Storage.ObjectStorageOptions.SectionName));
        services.AddOptions<Storage.AzureBlobStorageOptions>().Bind(configuration.GetSection(Storage.AzureBlobStorageOptions.SectionName));
        AddObjectStorageServices(services, configuration);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), "Jwt:SigningKey must be configured.")
            .ValidateOnStart();

        AddTemplateGeneratorServices(services);

        services.AddOptions<NotificationsOptions>().Bind(configuration.GetSection(NotificationsOptions.SectionName));
        services.AddOptions<AzureCommunicationServicesOptions>().Bind(configuration.GetSection(AzureCommunicationServicesOptions.SectionName));
        AddNotificationServices(services, configuration);
    }

    /// <summary>
    /// Registers the template/content-generator and rendering ports (dashboard, weekend, AI Year,
    /// international days, media monitoring) that <see cref="AddSecurityServices"/> used to inline
    /// directly. Split out purely to keep both methods under the R-BE-091 40-line/method ceiling as
    /// the registration list grows; this is DI wiring, not a meaningful behavioural seam.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    private static void AddTemplateGeneratorServices(IServiceCollection services)
    {
        services.AddScoped<Icbank.Platform.Application.Dashboard.IExecutiveSummaryGenerator, Dashboard.TemplateExecutiveSummaryGenerator>();
        services.AddScoped<Icbank.Platform.Application.Weekend.IWeekendContentGenerator, Weekend.TemplateWeekendContentGenerator>();
        services.AddScoped<Icbank.Platform.Application.Weekend.IWeekStartMessageGenerator, Weekend.TemplateWeekStartMessageGenerator>();
        services.AddScoped<Icbank.Platform.Application.Weekend.IDocumentTextExtractor, Weekend.CompositeDocumentTextExtractor>();
        services.AddScoped<Icbank.Platform.Application.AiYear.IAiYearReportDocxRenderer, AiYear.OpenXmlAiYearReportDocxBuilder>();
        services.AddScoped<Icbank.Platform.Application.InternationalDays.IInternationalDaySearchProvider, InternationalDays.TemplateInternationalDaySearchProvider>();
        services.AddSingleton<Icbank.Platform.Application.InternationalDays.IInternationalDaySearchRateLimiter, InternationalDays.InMemoryInternationalDaySearchRateLimiter>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IMediaReportNarrativeGenerator, MediaMonitoring.TemplateMediaReportNarrativeGenerator>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IPromptExecutionEngine, MediaMonitoring.TemplatePromptExecutionEngine>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IFinalReportSectionGenerator, MediaMonitoring.TemplateFinalReportSectionGenerator>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IFinalReportPdfRenderer, MediaMonitoring.QuestPdfFinalReportPdfRenderer>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IExecutiveSummaryRegenerator, MediaMonitoring.TemplateExecutiveSummaryRegenerator>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IReportArchiveQaEngine, MediaMonitoring.TemplateReportArchiveQaEngine>();
    }

    /// <summary>
    /// Registers <c>IObjectStorageReader</c>, <c>IObjectUploadUrlIssuer</c>, <c>IObjectStorageWriter</c>,
    /// and <c>IObjectStorageDeleter</c> using either the FileSystem or AzureBlob backend, selected by
    /// <c>ObjectStorage:Provider</c> (default <see cref="Storage.ObjectStorageProvider.FileSystem"/>).
    /// All four ports are switched together so they can never point at different backends. The
    /// <see cref="BlobServiceClient"/> is authenticated with <c>Azure.Identity.DefaultAzureCredential</c>
    /// (the API's managed identity in a deployed environment) -- no storage account key is ever
    /// read from configuration.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration, used to read <c>ObjectStorage:Provider</c> and <c>ObjectStorage:AzureBlob:ServiceUri</c>.</param>
    private static void AddObjectStorageServices(IServiceCollection services, IConfiguration configuration)
    {
        Storage.ObjectStorageProvider provider = configuration.GetValue<Storage.ObjectStorageProvider>("ObjectStorage:Provider");
        if (provider == Storage.ObjectStorageProvider.AzureBlob)
        {
            var serviceUri = configuration["ObjectStorage:AzureBlob:ServiceUri"]
                ?? throw new InvalidOperationException("ObjectStorage:AzureBlob:ServiceUri must be configured when ObjectStorage:Provider is AzureBlob.");
            services.AddSingleton(new BlobServiceClient(new Uri(serviceUri), new identity::Azure.Identity.DefaultAzureCredential()));
            services.AddSingleton<Icbank.Platform.Application.Storage.IObjectStorageReader, Storage.AzureBlobObjectStorageReader>();
            services.AddSingleton<Icbank.Platform.Application.Storage.IObjectUploadUrlIssuer, Storage.AzureBlobObjectUploadUrlIssuer>();
            services.AddSingleton<Icbank.Platform.Application.Storage.IObjectStorageWriter, Storage.AzureBlobObjectStorageWriter>();
            services.AddSingleton<Icbank.Platform.Application.Storage.IObjectStorageDeleter, Storage.AzureBlobObjectStorageDeleter>();
        }
        else
        {
            services.AddSingleton<Icbank.Platform.Application.Storage.IObjectStorageReader, Storage.FileSystemObjectStorageReader>();
            services.AddSingleton<Icbank.Platform.Application.Storage.IObjectUploadUrlIssuer, Storage.FileSystemObjectUploadUrlIssuer>();
            services.AddSingleton<Icbank.Platform.Application.Storage.IObjectStorageWriter, Storage.FileSystemObjectStorageWriter>();
            services.AddSingleton<Icbank.Platform.Application.Storage.IObjectStorageDeleter, Storage.FileSystemObjectStorageDeleter>();
        }
    }

    /// <summary>
    /// Registers <c>IReportEmailSender</c> and <c>IShorfahNotificationSender</c> using either the
    /// Null (honest no-op) or AzureCommunicationServices backend, selected by
    /// <c>Notifications:Provider</c> (default <see cref="NotificationsProvider.Null"/>). Both ports
    /// are switched together. The <see cref="EmailClient"/> is authenticated with
    /// <c>Azure.Identity.DefaultAzureCredential</c> -- no connection string/key is ever read from configuration.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration, used to read <c>Notifications:Provider</c> and <c>Notifications:AzureCommunicationServices:Endpoint</c>.</param>
    private static void AddNotificationServices(IServiceCollection services, IConfiguration configuration)
    {
        NotificationsProvider provider = configuration.GetValue<NotificationsProvider>("Notifications:Provider");
        if (provider == NotificationsProvider.AzureCommunicationServices)
        {
            var endpoint = configuration["Notifications:AzureCommunicationServices:Endpoint"]
                ?? throw new InvalidOperationException("Notifications:AzureCommunicationServices:Endpoint must be configured when Notifications:Provider is AzureCommunicationServices.");
            services.AddSingleton(new EmailClient(new Uri(endpoint), new identity::Azure.Identity.DefaultAzureCredential()));
            services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IReportEmailSender, AzureCommunicationServicesReportEmailSender>();
            services.AddScoped<Icbank.Platform.Application.Shorfah.IShorfahNotificationSender, AzureCommunicationServicesShorfahNotificationSender>();
        }
        else
        {
            services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IReportEmailSender, MediaMonitoring.NullReportEmailSender>();
            services.AddScoped<Icbank.Platform.Application.Shorfah.IShorfahNotificationSender, Shorfah.NullShorfahNotificationSender>();
        }
    }

    /// <summary>Registers Wave 3b Designs/Composer and Icon Event Designs ports: storage writer, rate limiter, seed catalogs, and AI/rendering placeholders.</summary>
    /// <param name="services">The DI service collection.</param>
    private static void AddDesignsServices(IServiceCollection services)
    {
        // Why: IObjectStorageWriter's provider selection is wired in AddObjectStorageServices
        // (called from AddSecurityServices) alongside the other three storage ports, so all four
        // share one Provider switch instead of being able to drift independently.
        services.AddSingleton<Icbank.Platform.Application.Designs.IDesignGenerationRateLimiter, Designs.InMemoryDesignGenerationRateLimiter>();
        services.AddScoped<Icbank.Platform.Application.Designs.IconEvent.IIconEventDesignExtractor, Designs.TemplateIconEventDesignExtractor>();
        services.AddScoped<Icbank.Platform.Application.Designs.IconEvent.IIconEventHtmlRenderer, Designs.EncodedIconEventHtmlRenderer>();
        services.AddScoped<Icbank.Platform.Application.Designs.IconEvent.IIconEventImageRenderer, Designs.TemplateIconEventImageRenderer>();
        services.AddScoped<Icbank.Platform.Application.Designs.Composer.IDesignTemplateSeedCatalog, Designs.CuratedDesignTemplateSeedCatalog>();
        services.AddScoped<Icbank.Platform.Application.Designs.Composer.IGacLogoSeedCatalog, Designs.CuratedGacLogoSeedCatalog>();
        services.AddScoped<Icbank.Platform.Application.Designs.Composer.IDesignComposer, Designs.PlaceholderDesignComposer>();
        services.AddScoped<Icbank.Platform.Application.Designs.Composer.IBackgroundImageGenerator, Designs.TemplateBackgroundImageGenerator>();
    }

    /// <summary>Registers Wave 4a Shorfah issue-lifecycle ports: notification/URL/rate-limiter/export-rendering placeholders.</summary>
    /// <param name="services">The DI service collection.</param>
    private static void AddShorfahServices(IServiceCollection services)
    {
        // Why: IShorfahNotificationSender's provider selection is wired in AddNotificationServices
        // alongside IReportEmailSender's, so the two email-sending ports share one Provider switch.
        services.AddScoped<Icbank.Platform.Application.Shorfah.IShorfahUrlProvider, Shorfah.ConfigurationShorfahUrlProvider>();
        services.AddSingleton<Icbank.Platform.Application.Shorfah.IShorfahSendInitialRateLimiter, Shorfah.InMemoryShorfahSendInitialRateLimiter>();
        services.AddScoped<Icbank.Platform.Application.Shorfah.IShorfahDocxRenderer, Shorfah.OpenXmlShorfahDocxRenderer>();
        services.AddScoped<Icbank.Platform.Application.Shorfah.IShorfahIssuePdfRenderer, Shorfah.QuestPdfShorfahIssuePdfRenderer>();
        services.AddScoped<Icbank.Platform.Application.Shorfah.IShorfahSectionAccessService, Shorfah.ShorfahSectionAccessService>();
        services.AddScoped<Icbank.Platform.Application.Shorfah.IShorfahSectionContentGenerator, Shorfah.TemplateShorfahSectionContentGenerator>();
        services.AddSingleton<Icbank.Platform.Application.Shorfah.IShorfahSectionGenerationRateLimiter, Shorfah.InMemoryShorfahSectionGenerationRateLimiter>();
    }

    /// <summary>Registers Azure AD SSO support: the distributed state cache, options binding, the IdP <c>HttpClient</c>, and the SSO ports.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration, used for Azure AD options binding.</param>
    private static void AddSsoServices(IServiceCollection services, IConfiguration configuration)
    {
        // Why: Azure AD SSO (BUSINESS-RULES.md §11.2/§11.3) — the distributed cache is a
        // single-instance in-memory implementation by default; swap AddDistributedMemoryCache()
        // for a Redis-backed registration to support horizontal scaling (see AUTH-PORT-NOTES.md).
        services.AddDistributedMemoryCache();
        services.AddOptions<AzureAdOptions>().Bind(configuration.GetSection(AzureAdOptions.SectionName));
        services.AddHttpClient("idp");
        services.AddScoped<IAzureAdClient, AzureAdClient>();
        services.AddSingleton<ISsoStateStore, DistributedSsoStateStore>();
        services.AddScoped<ISsoOptionsProvider, AzureAdOptionsProvider>();
    }

    /// <summary>Registers database-seeding options and the <see cref="Seeding.DatabaseSeeder"/>.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration, used for seed options binding.</param>
    private static void AddSeeding(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<Seeding.SeedOptions>().Bind(configuration.GetSection(Seeding.SeedOptions.SectionName));
        services.AddScoped<Seeding.DatabaseSeeder>();
    }

    /// <summary>Registers the <see cref="AuditInterceptor"/>, the EF Core <see cref="AppDbContext"/>, and the narrow <c>IApplicationDbContext</c> port Application consumes.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration, used to resolve the SQL Server connection string.</param>
    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        // Why: AuditInterceptor depends on the scoped ICurrentUserService, so it must itself be
        // scoped (a singleton cannot consume a scoped dependency — ASP.NET Core's DI validator
        // rejects this at startup with ValidateScopes/ValidateOnBuild).
        services.AddScoped<AuditInterceptor>();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<AppDbContext>((serviceProvider, options) => options
            .UseSqlServer(connectionString)
            .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>()));

        // Why: Application depends only on the narrow IApplicationDbContext port (R-BE-002);
        // this resolves it to the same scoped AppDbContext instance EF Core already manages.
        services.AddScoped<Icbank.Platform.Application.Common.Interfaces.IApplicationDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());
    }

    /// <summary>Registers the named <c>HttpClient</c> instances used for outbound calls, wrapped in a standard resilience pipeline (Polly v8 via <c>Microsoft.Extensions.Http.Resilience</c>).</summary>
    /// <param name="services">The DI service collection.</param>
    private static void AddResilientHttpClients(IServiceCollection services)
    {
        services.AddHttpClient("downstream")
            .AddHttpMessageHandler(() => new CorrelationPropagationHandler())
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = MaxRetryAttempts;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.AttemptTimeout.Timeout = AttemptTimeout;
                options.CircuitBreaker.FailureRatio = CircuitBreakerFailureRatio;
            });
    }
}
