extern alias identity;

using Azure.Communication.Email;
using Azure.Storage.Blobs;
using Icbank.Platform.Application.Auth;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Gac.News;
using Icbank.Platform.Infrastructure.Gemini;
using Icbank.Platform.Infrastructure.Http;
using Icbank.Platform.Infrastructure.Identity;
using Icbank.Platform.Infrastructure.News;
using Icbank.Platform.Infrastructure.Notifications;
using Icbank.Platform.Infrastructure.Persistence;
using Icbank.Platform.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Icbank.Platform.Infrastructure;

/// <summary>
/// Composition-root extension for the Infrastructure layer (R-BE-004). Wires EF Core, the audit
/// interceptor, the current-user port, and outbound HTTP resilience policies.
/// </summary>
public static partial class DependencyInjection
{
    private const int MaxRetryAttempts = 3; // R-BE-095 — named, not a bare "3".
    private const double CircuitBreakerFailureRatio = 0.5;

    // Azure SQL closes idle connections and moves databases between nodes, so a request that
    // happens to land on a stale pooled connection fails once and succeeds immediately on a
    // retry. Without this the failure surfaces as a bare 500: two consecutive deploys had the
    // smoke test's login return 500 while the same call by hand returned 200/401 correctly.
    // That is a user-facing defect, not just a CI flake.
    private const int SqlMaxRetryAttempts = 5;

    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SqlMaxRetryDelay = TimeSpan.FromSeconds(10);

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
        AddNewsSourceServices(services, configuration);

        return services;
    }

    /// <summary>
    /// Registers the news ingest providers selected by <c>NewsSources:EnabledProviders</c>.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <remarks>
    /// Every provider is constructed here and filtered by configuration rather than compiled in, so
    /// changing which upstream the Authority monitors is an App Service setting, not a release. An
    /// unrecognised key is logged and ignored instead of throwing: a typo in a config value must not
    /// take the whole API down at startup.
    /// </remarks>
    private static void AddNewsSourceServices(IServiceCollection services, IConfiguration configuration)
    {
        var options = new NewsSourceOptions();
        configuration.GetSection(NewsSourceOptions.SectionName).Bind(options);
        services.AddSingleton(options);
        services.AddSingleton(new NewsFetchSettings(
            options.Terms,
            options.Language,
            options.Region,
            options.WithinDays,
            options.MaxItemsPerTerm));

        // Configured here rather than per provider so every current and future provider inherits it.
        // The timeout is the important part: a fetch issues one request per term per provider in
        // sequence, so on HttpClient's 100-second default a single unresponsive upstream stalls the
        // whole job for minutes. The providers treat a timeout as an empty result, so failing fast
        // costs only that term's contribution to the run.
        services.AddHttpClient("news", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.RequestTimeoutSeconds, 1, 120));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(options.Language);
        });
        var newsDataApiKey = NewsDataApiKeyResolver.Resolve(configuration);

        foreach (var providerKey in options.EnabledProviders.Select(k => k.Trim()).Where(k => k.Length > 0).Distinct())
        {
            RegisterNewsProvider(services, providerKey, newsDataApiKey);
        }
    }

    private static void RegisterNewsProvider(IServiceCollection services, string providerKey, string? newsDataApiKey)
    {
        if (string.Equals(providerKey, GoogleNewsRssProvider.Key, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<INewsSourceProvider>(sp => new GoogleNewsRssProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("news"),
                sp.GetRequiredService<NewsSourceOptions>(),
                sp.GetRequiredService<ILogger<GoogleNewsRssProvider>>()));
            return;
        }

        if (string.Equals(providerKey, NewsDataIoProvider.Key, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<INewsSourceProvider>(sp => new NewsDataIoProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("news"),
                sp.GetRequiredService<NewsSourceOptions>(),
                newsDataApiKey,
                sp.GetRequiredService<ILogger<NewsDataIoProvider>>()));
            return;
        }

        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = loggerFactory.CreateLogger(typeof(DependencyInjection).FullName ?? nameof(DependencyInjection));
        LogUnknownNewsProvider(logger, providerKey);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unknown news provider key {ProviderKey} in NewsSources:EnabledProviders; ignoring it. Valid keys are google-news-rss and newsdata-io.")]
    private static partial void LogUnknownNewsProvider(ILogger logger, string providerKey);

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

        AddTemplateGeneratorServices(services, configuration);

        services.AddOptions<NotificationsOptions>().Bind(configuration.GetSection(NotificationsOptions.SectionName));
        services.AddOptions<AzureCommunicationServicesOptions>().Bind(configuration.GetSection(AzureCommunicationServicesOptions.SectionName));
        AddNotificationServices(services, configuration);
    }

    /// <summary>
    /// Registers the AI-backed content-generation ports (dashboard, weekend, international days,
    /// media monitoring) plus the non-AI rendering/rate-limiter ports that ride alongside them.
    /// Twelve of these ports switch between a real <c>Gemini*</c> implementation and the existing
    /// labelled <c>Template*</c>/<c>Encoded*</c> placeholder, selected by whether a Gemini API key
    /// is configured -- mirroring how <see cref="AddNotificationServices"/> switches between
    /// <c>Null*</c> and Azure-backed senders. An unconfigured environment degrades to placeholder
    /// Arabic rather than throwing at request time. Split out purely to keep both methods under the
    /// R-BE-091 40-line/method ceiling as the registration list grows; this is DI wiring, not a
    /// meaningful behavioural seam.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration, used to resolve the Gemini API key.</param>
    private static void AddTemplateGeneratorServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<Icbank.Platform.Application.Weekend.IDocumentTextExtractor, Weekend.CompositeDocumentTextExtractor>();
        services.AddScoped<Icbank.Platform.Application.AiYear.IAiYearReportDocxRenderer, AiYear.OpenXmlAiYearReportDocxBuilder>();
        services.AddSingleton<Icbank.Platform.Application.InternationalDays.IInternationalDaySearchRateLimiter, InternationalDays.InMemoryInternationalDaySearchRateLimiter>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IFinalReportPdfRenderer, MediaMonitoring.QuestPdfFinalReportPdfRenderer>();

        var apiKey = GeminiApiKeyResolver.Resolve(configuration);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            LogMissingGeminiKeyWarning();
            AddTemplateOnlyGenerators(services);
            return;
        }

        AddGeminiHttpInfrastructure(services, configuration, apiKey);
        AddGeminiBackedGenerators(services);
    }

    /// <summary>
    /// Logs one clear startup warning when no Gemini API key is configured, mirroring the Node
    /// source's <c>console.warn</c> when <c>GEMINI_API_KEY</c>/<c>GOOGLE_AI_API_KEY</c>/
    /// <c>AI_INTEGRATIONS_GEMINI_API_KEY</c> are all unset. Resolves a throwaway
    /// <see cref="ILoggerFactory"/> at registration time (before the host's own logging pipeline is
    /// necessarily built) purely to emit this one diagnostic -- no secret value is ever involved.
    /// </summary>
    private static void LogMissingGeminiKeyWarning()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = loggerFactory.CreateLogger(typeof(DependencyInjection).FullName ?? nameof(DependencyInjection));
        LogGeminiKeyMissing(logger);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gemini API key not configured (checked GEMINI_API_KEY, GOOGLE_AI_API_KEY, AI_INTEGRATIONS_GEMINI_API_KEY). All AI-backed features will use labelled placeholder Arabic content instead of real Gemini output.")]
    private static partial void LogGeminiKeyMissing(ILogger logger);

    /// <summary>Registers the thirteen honest labelled placeholders, used when no Gemini API key is configured, so the platform degrades to labelled placeholder Arabic instead of throwing.</summary>
    /// <param name="services">The DI service collection.</param>
    private static void AddTemplateOnlyGenerators(IServiceCollection services)
    {
        services.AddScoped<Icbank.Platform.Application.Dashboard.IExecutiveSummaryGenerator, Dashboard.TemplateExecutiveSummaryGenerator>();
        services.AddScoped<Icbank.Platform.Application.Weekend.IWeekendContentGenerator, Weekend.TemplateWeekendContentGenerator>();
        services.AddScoped<Icbank.Platform.Application.Weekend.IWeekStartMessageGenerator, Weekend.TemplateWeekStartMessageGenerator>();
        services.AddScoped<Icbank.Platform.Application.InternationalDays.IInternationalDaySearchProvider, InternationalDays.TemplateInternationalDaySearchProvider>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IMediaReportNarrativeGenerator, MediaMonitoring.TemplateMediaReportNarrativeGenerator>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IPromptExecutionEngine, MediaMonitoring.TemplatePromptExecutionEngine>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IFinalReportSectionGenerator, MediaMonitoring.TemplateFinalReportSectionGenerator>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IExecutiveSummaryRegenerator, MediaMonitoring.TemplateExecutiveSummaryRegenerator>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IReportArchiveQaEngine, MediaMonitoring.TemplateReportArchiveQaEngine>();
        services.AddScoped<Icbank.Platform.Application.Shorfah.IShorfahSectionContentGenerator, Shorfah.TemplateShorfahSectionContentGenerator>();
        services.AddScoped<Icbank.Platform.Application.Designs.IconEvent.IIconEventDesignExtractor, Designs.TemplateIconEventDesignExtractor>();
        services.AddScoped<Icbank.Platform.Application.Designs.Composer.IBackgroundImageGenerator, Designs.TemplateBackgroundImageGenerator>();
    }

    /// <summary>
    /// Registers the shared Gemini HTTP/resilience plumbing: the bare named <c>"gemini"</c>
    /// <see cref="HttpClient"/> (no Polly -- <see cref="GeminiClient"/> has its own bespoke
    /// retry/backoff/model-fallback loop per BUSINESS-RULES.md), the real-time delay seam, a
    /// shared jitter source, and <see cref="IGeminiClient"/> itself. The resolved API key is
    /// captured only inside this factory closure -- it is never bound onto <see cref="GeminiOptions"/>,
    /// logged, or persisted anywhere, and this method is only reached once a non-empty key exists.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration, used to build <see cref="GeminiOptions"/>.</param>
    /// <param name="apiKey">The resolved Gemini API key (never logged).</param>
    private static void AddGeminiHttpInfrastructure(IServiceCollection services, IConfiguration configuration, string apiKey)
    {
        services.AddSingleton(BuildGeminiOptions(configuration));
        services.AddHttpClient("gemini");
        services.AddSingleton<IGeminiTransport>(sp => new HttpGeminiTransport(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("gemini"),
            sp.GetRequiredService<GeminiOptions>()));
        services.AddSingleton<IGeminiDelay, TaskDelayGeminiDelay>();
        services.AddSingleton(new Random());
        services.AddSingleton<IGeminiClient>(sp => new GeminiClient(
            sp.GetRequiredService<IGeminiTransport>(),
            apiKey,
            sp.GetRequiredService<IGeminiDelay>(),
            sp.GetRequiredService<Random>()));
    }

    /// <summary>
    /// Builds <see cref="GeminiOptions"/> from configuration section <c>Gemini</c> (base URL) plus
    /// the Node source's exact three model-override environment variable names --
    /// <c>GEMINI_TEXT_MODEL</c>, <c>GEMINI_PRO_MODEL</c>, <c>GEMINI_IMAGE_MODEL</c> -- read directly
    /// off <see cref="IConfiguration"/> (which already surfaces environment variables) rather than
    /// through the <c>Gemini:*</c> section, since the Node original reads bare env var names, not a
    /// nested config path.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The resolved <see cref="GeminiOptions"/>.</returns>
    private static GeminiOptions BuildGeminiOptions(IConfiguration configuration)
    {
        var options = new GeminiOptions();
        configuration.GetSection(GeminiOptions.SectionName).Bind(options);

        options.TextModel = configuration["GEMINI_TEXT_MODEL"] is { Length: > 0 } textModel ? textModel : options.TextModel;
        options.ProModel = configuration["GEMINI_PRO_MODEL"] is { Length: > 0 } proModel ? proModel : options.ProModel;
        options.ImageModel = configuration["GEMINI_IMAGE_MODEL"] is { Length: > 0 } imageModel ? imageModel : options.ImageModel;
        return options;
    }

    /// <summary>
    /// Registers the twelve real Gemini-backed generators (everything except
    /// <c>IIconEventImageRenderer</c>, which is HTML-to-PNG rasterization, not an AI call, and so
    /// always stays <see cref="Designs.TemplateIconEventImageRenderer"/> regardless of key presence).
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    private static void AddGeminiBackedGenerators(IServiceCollection services)
    {
        services.AddScoped<Icbank.Platform.Application.Dashboard.IExecutiveSummaryGenerator, Dashboard.GeminiExecutiveSummaryGenerator>();
        services.AddScoped<Icbank.Platform.Application.Weekend.IWeekendContentGenerator, Weekend.GeminiWeekendContentGenerator>();
        services.AddScoped<Icbank.Platform.Application.Weekend.IWeekStartMessageGenerator, Weekend.GeminiWeekStartMessageGenerator>();
        services.AddScoped<Icbank.Platform.Application.InternationalDays.IInternationalDaySearchProvider, InternationalDays.GeminiInternationalDaySearchProvider>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IMediaReportNarrativeGenerator, MediaMonitoring.GeminiMediaReportNarrativeGenerator>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IPromptExecutionEngine, MediaMonitoring.GeminiPromptExecutionEngine>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IFinalReportSectionGenerator, MediaMonitoring.GeminiFinalReportSectionGenerator>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IExecutiveSummaryRegenerator, MediaMonitoring.GeminiExecutiveSummaryRegenerator>();
        services.AddScoped<Icbank.Platform.Application.MediaMonitoring.IReportArchiveQaEngine, MediaMonitoring.GeminiReportArchiveQaEngine>();
        services.AddScoped<Icbank.Platform.Application.Shorfah.IShorfahSectionContentGenerator, Shorfah.GeminiShorfahSectionContentGenerator>();
        services.AddScoped<Icbank.Platform.Application.Designs.IconEvent.IIconEventDesignExtractor, Designs.GeminiIconEventDesignExtractor>();
        services.AddScoped<Icbank.Platform.Application.Designs.Composer.IBackgroundImageGenerator, Designs.GeminiBackgroundImageGenerator>();
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

    /// <summary>
    /// Registers Wave 3b Designs/Composer and Icon Event Designs ports: storage writer, rate
    /// limiter, seed catalogs, and rendering placeholders. <c>IIconEventDesignExtractor</c> and
    /// <c>IBackgroundImageGenerator</c> are intentionally NOT registered here -- they switch between
    /// Gemini and Template implementations in <see cref="AddTemplateGeneratorServices"/> based on
    /// API key presence, and registering them again here would silently override that choice
    /// (DI resolves the last registration for a given interface).
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    private static void AddDesignsServices(IServiceCollection services)
    {
        // Why: IObjectStorageWriter's provider selection is wired in AddObjectStorageServices
        // (called from AddSecurityServices) alongside the other three storage ports, so all four
        // share one Provider switch instead of being able to drift independently.
        services.AddSingleton<Icbank.Platform.Application.Designs.IDesignGenerationRateLimiter, Designs.InMemoryDesignGenerationRateLimiter>();
        services.AddScoped<Icbank.Platform.Application.Designs.IconEvent.IIconEventHtmlRenderer, Designs.EncodedIconEventHtmlRenderer>();
        services.AddScoped<Icbank.Platform.Application.Designs.IconEvent.IIconEventImageRenderer, Designs.TemplateIconEventImageRenderer>();
        services.AddScoped<Icbank.Platform.Application.Designs.Composer.IDesignTemplateSeedCatalog, Designs.CuratedDesignTemplateSeedCatalog>();
        services.AddScoped<Icbank.Platform.Application.Designs.Composer.IGacLogoSeedCatalog, Designs.CuratedGacLogoSeedCatalog>();
        services.AddScoped<Icbank.Platform.Application.Designs.Composer.IDesignComposer, Designs.PlaceholderDesignComposer>();
    }

    /// <summary>
    /// Registers Wave 4a Shorfah issue-lifecycle ports: notification/URL/rate-limiter/export-rendering
    /// placeholders. <c>IShorfahSectionContentGenerator</c> is intentionally NOT registered here for
    /// the same reason as the two Designs ports above -- see <see cref="AddDesignsServices"/>.
    /// </summary>
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
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
                maxRetryCount: SqlMaxRetryAttempts,
                maxRetryDelay: SqlMaxRetryDelay,
                errorNumbersToAdd: null))
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
