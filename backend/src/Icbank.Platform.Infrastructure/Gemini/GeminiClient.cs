using System.Text.Json;

namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// Ports <c>geminiText</c>/<c>geminiJSON</c> from <c>aiProviders.ts</c> verbatim: for each model in
/// <see cref="GeminiModelChainBuilder"/>'s chain, make up to <see cref="AttemptsPerModel"/>
/// attempts with exponential backoff; a model-level error (see <see cref="GeminiErrorClassifier.IsModelLevel"/>)
/// skips straight to the next model without retrying; a transient error
/// (<see cref="GeminiErrorClassifier.IsTransient"/>) retries the same model until attempts are
/// exhausted, then moves on; any other error is rethrown immediately without trying further
/// models. When every model in the chain is exhausted, throws <see cref="GeminiUnavailableException"/>
/// with the Node source's verbatim shared Arabic fallback message.
/// </summary>
public sealed class GeminiClient : IGeminiClient
{
    /// <summary>The Node source's <c>maxOutputTokens</c> default.</summary>
    public const int DefaultMaxOutputTokens = 2048;

    /// <summary>The Node source's <c>temperature</c> default.</summary>
    public const double DefaultTemperature = 0.7;

    /// <summary>The Node source's verbatim JSON-only system prefix (<c>geminiJSON</c>), prepended so the model never wraps its answer in prose or markdown.</summary>
    public const string JsonOnlySystemPrefix = "أجب فقط بـ JSON صحيح بدون أي نص إضافي أو markdown أو شروحات. ";

    private const int AttemptsPerModel = 2;
    private const int MaxParseAttempts = 3;
    private const int BaseRetryDelayMs = 1200;
    private const int RetryJitterMaxMs = 400;
    private const int InterModelDelayMs = 800;

    private readonly IGeminiTransport _transport;
    private readonly string _apiKey;
    private readonly IGeminiDelay _delay;
    private readonly Random _jitterSource;

    /// <summary>Initializes a new instance of the <see cref="GeminiClient"/> class.</summary>
    /// <param name="transport">The single-attempt HTTP transport.</param>
    /// <param name="apiKey">The resolved Gemini API key (never logged; passed straight to the transport).</param>
    /// <param name="delay">The injectable sleep seam used for backoff waits.</param>
    /// <param name="jitterSource">The random source for backoff jitter (injectable so tests can assert exact delays).</param>
    public GeminiClient(IGeminiTransport transport, string apiKey, IGeminiDelay delay, Random jitterSource)
    {
        _transport = transport;
        _apiKey = apiKey;
        _delay = delay;
        _jitterSource = jitterSource;
    }

    /// <inheritdoc />
    public async Task<GeminiGenerationResult> GenerateTextAsync(string prompt, GeminiCallOptions options, CancellationToken cancellationToken)
    {
        GeminiGenerationResult result = await RunModelChainAsync(prompt, options, cancellationToken).ConfigureAwait(false);
        if (options.UseGoogleSearchTool && options.RequireGrounding)
        {
            EnsureGrounded(result);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<GeminiGenerationResult> GenerateJsonAsync(string prompt, GeminiCallOptions options, CancellationToken cancellationToken)
    {
        GeminiCallOptions jsonOptions = options with { SystemInstruction = JsonOnlySystemPrefix + (options.SystemInstruction ?? string.Empty) };
        var requireGrounding = options.UseGoogleSearchTool && options.RequireGrounding;

        Exception? lastParseFailure = null;
        for (var parseAttempt = 1; parseAttempt <= MaxParseAttempts; parseAttempt++)
        {
            GeminiGenerationResult result = await RunModelChainAsync(prompt, jsonOptions, cancellationToken).ConfigureAwait(false);
            if (requireGrounding)
            {
                EnsureGrounded(result);
            }

            var candidate = GeminiJsonExtractor.StripFencesAndPreamble(result.Text);
            if (TryParse(candidate, out lastParseFailure))
            {
                return result with { Text = candidate };
            }

            var repaired = GeminiJsonExtractor.RepairTruncated(candidate);
            if (TryParse(repaired, out lastParseFailure))
            {
                return result with { Text = repaired };
            }

            if (parseAttempt < MaxParseAttempts)
            {
                await _delay.Wait(TimeSpan.FromMilliseconds(BaseRetryDelayMs * parseAttempt), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new GeminiUnavailableException(lastParseFailure ?? new JsonException("Gemini JSON output could not be parsed after retries and repair."));
    }

    /// <inheritdoc />
    public async Task<GeminiGenerationResult> GenerateImageAsync(string prompt, GeminiCallOptions options, CancellationToken cancellationToken)
    {
        var request = new GeminiGenerationRequest(
            options.Model, options.SystemInstruction, prompt, options.MaxOutputTokens, options.Temperature, UseGoogleSearchTool: false, ResponseMimeType: null, options.ThinkingBudget);

        (GeminiGenerationResult? Result, Exception? Error) outcome = await TryModelAsync(request, cancellationToken).ConfigureAwait(false);
        if (outcome.Result is null)
        {
            throw new GeminiUnavailableException(outcome.Error ?? new InvalidOperationException("Gemini image generation failed with no recorded error."));
        }

        if (outcome.Result.InlineImages.Count == 0)
        {
            throw new GeminiUnavailableException("Gemini image call returned no inline image data.");
        }

        return outcome.Result;
    }

    private static bool TryParse(string text, out Exception? failure)
    {
        try
        {
            using var parsed = JsonDocument.Parse(text);
            failure = null;
            return true;
        }
        catch (JsonException ex)
        {
            failure = ex;
            return false;
        }
    }

    private static void EnsureGrounded(GeminiGenerationResult result)
    {
        if (result.SearchQueries.Count == 0 && result.Citations.Count == 0)
        {
            throw new GeminiGroundingAbsentException();
        }
    }

    private async Task<GeminiGenerationResult> RunModelChainAsync(string prompt, GeminiCallOptions options, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> chain = GeminiModelChainBuilder.Build(options.Model);
        Exception? lastError = null;

        for (var modelIndex = 0; modelIndex < chain.Count; modelIndex++)
        {
            var model = chain[modelIndex];
            var request = new GeminiGenerationRequest(
                model, options.SystemInstruction, prompt, options.MaxOutputTokens, options.Temperature, options.UseGoogleSearchTool, ResponseMimeType: null, options.ThinkingBudget);

            (GeminiGenerationResult? Result, Exception? Error) outcome = await TryModelAsync(request, cancellationToken).ConfigureAwait(false);
            if (outcome.Result is not null)
            {
                return outcome.Result;
            }

            lastError = outcome.Error;
            var isLastModel = modelIndex == chain.Count - 1;
            if (!isLastModel)
            {
                await _delay.Wait(TimeSpan.FromMilliseconds(InterModelDelayMs), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new GeminiUnavailableException(lastError ?? new InvalidOperationException("Gemini model chain exhausted with no recorded error."));
    }

    private async Task<(GeminiGenerationResult? Result, Exception? Error)> TryModelAsync(GeminiGenerationRequest request, CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= AttemptsPerModel; attempt++)
        {
            try
            {
                GeminiGenerationResult result = await _transport.GenerateContentAsync(_apiKey, request, cancellationToken).ConfigureAwait(false);
                return (result, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
                if (GeminiErrorClassifier.IsModelLevel(ex.Message))
                {
                    return (null, ex);
                }

                if (!GeminiErrorClassifier.IsTransient(ex.Message))
                {
                    throw;
                }

                if (attempt < AttemptsPerModel)
                {
                    var jitter = _jitterSource.Next(0, RetryJitterMaxMs);
                    await _delay.Wait(TimeSpan.FromMilliseconds((BaseRetryDelayMs * attempt) + jitter), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return (null, lastError);
    }
}
