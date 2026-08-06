using FluentAssertions;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Exercises <see cref="GeminiClient"/>'s resilience policy end to end against
/// <see cref="FakeGeminiTransport"/> and <see cref="FakeGeminiDelay"/> -- the retry sequence
/// (two attempts per model), exponential backoff timing, the fixed model fallback order,
/// transient-vs-model-level error classification, the JSON parse-with-repair ladder, and the
/// grounding-absent failure path. No test in this file sleeps in real time or reaches the network;
/// <see cref="FakeGeminiDelay"/> records requested durations instead of waiting, which is what lets
/// every backoff assertion run in milliseconds.
/// </summary>
public sealed class GeminiClientTests
{
    private const string Primary = "gemini-2.5-pro";
    private readonly FakeGeminiTransport _transport = new();
    private readonly FakeGeminiDelay _delay = new();
    private readonly Random _fixedJitter = new(12345);
    private readonly GeminiClient _sut;

    public GeminiClientTests()
    {
        _sut = new GeminiClient(_transport, "test-api-key", _delay, _fixedJitter);
    }

    // ── Success path ────────────────────────────────────────────────────────
    [Fact]
    public async Task GenerateTextAsync_FirstAttemptSucceeds_ReturnsResultWithoutAnyRetryOrDelay()
    {
        _transport.Enqueue(GeminiTestResults.Text("hello"));

        GeminiGenerationResult result = await _sut.GenerateTextAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        result.Text.Should().Be("hello");
        _transport.CallsInOrder.Should().Equal(Primary);
        _delay.Requested.Should().BeEmpty("a first-attempt success must never wait");
    }

    // ── Retry sequence: two attempts per model ─────────────────────────────
    [Fact]
    public async Task GenerateTextAsync_TransientErrorOnce_RetriesSameModelAndSucceedsOnSecondAttempt()
    {
        _transport.EnqueueForModel(Primary, new InvalidOperationException("503 Service Unavailable"));
        _transport.EnqueueForModel(Primary, GeminiTestResults.Text("recovered"));

        GeminiGenerationResult result = await _sut.GenerateTextAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        result.Text.Should().Be("recovered");
        _transport.CallsInOrder.Should().Equal(Primary, Primary);
        _delay.Requested.Should().HaveCount(1, "exactly one backoff wait between the two same-model attempts");
    }

    [Fact]
    public async Task GenerateTextAsync_TransientErrorOnBothAttempts_FallsBackToNextModelInChain()
    {
        _transport.EnqueueForModel(Primary, new InvalidOperationException("503 Service Unavailable"));
        _transport.EnqueueForModel(Primary, new InvalidOperationException("503 Service Unavailable"));
        _transport.EnqueueForModel("gemini-2.5-flash", GeminiTestResults.Text("from fallback"));

        GeminiGenerationResult result = await _sut.GenerateTextAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        result.Text.Should().Be("from fallback");
        _transport.CallsInOrder.Should().Equal(Primary, Primary, "gemini-2.5-flash");
    }

    [Fact]
    public async Task GenerateTextAsync_AllModelsExhausted_ThrowsGeminiUnavailableExceptionWithArabicFallbackMessage()
    {
        foreach (var model in new[] { Primary, "gemini-2.5-flash", "gemini-2.5-flash-lite", "gemini-flash-latest" })
        {
            _transport.EnqueueForModel(model, new InvalidOperationException("503 Service Unavailable"));
            _transport.EnqueueForModel(model, new InvalidOperationException("503 Service Unavailable"));
        }

        Func<Task> act = () => _sut.GenerateTextAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        (await act.Should().ThrowAsync<GeminiUnavailableException>())
            .Which.Message.Should().Be(GeminiUnavailableException.FallbackMessageAr);
        _transport.CallsInOrder.Should().HaveCount(8, "2 attempts x 4 models in the chain");
    }

    // ── Exponential backoff timing (never actually sleeps) ─────────────────
    [Fact]
    public async Task GenerateTextAsync_TransientRetry_RequestsExponentiallyGrowingDelayWithJitterBound()
    {
        _transport.EnqueueForModel(Primary, new InvalidOperationException("429 rate limit"));
        _transport.EnqueueForModel(Primary, GeminiTestResults.Text("ok"));

        await _sut.GenerateTextAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        _delay.Requested.Should().HaveCount(1);

        // Node source: delay = 1200 * attempt + jitter(0..400); attempt 1 here.
        _delay.Requested[0].TotalMilliseconds.Should().BeGreaterThanOrEqualTo(1200)
            .And.BeLessThan(1600);
    }

    [Fact]
    public async Task GenerateTextAsync_SecondTransientAttemptWithinSameModel_UsesLargerBaseDelayThanFirst()
    {
        // Force model 1 to exhaust both attempts (so we see the "attempt" scaling isn't reset),
        // then verify the inter-model delay is the fixed 800ms, distinct from the per-attempt one.
        _transport.EnqueueForModel(Primary, new InvalidOperationException("503 unavailable"));
        _transport.EnqueueForModel(Primary, new InvalidOperationException("503 unavailable"));
        _transport.EnqueueForModel("gemini-2.5-flash", GeminiTestResults.Text("ok"));

        await _sut.GenerateTextAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        _delay.Requested.Should().HaveCount(2, "one retry-within-model wait, one inter-model wait");
        _delay.Requested[0].TotalMilliseconds.Should().BeGreaterThanOrEqualTo(1200).And.BeLessThan(1600, "attempt=1 backoff: 1200*1 + jitter(0..400)");
        _delay.Requested[1].Should().Be(TimeSpan.FromMilliseconds(800), "fixed inter-model delay, not exponential");
    }

    // ── Model fallback order, including duplicate-skipping ─────────────────
    [Fact]
    public async Task GenerateTextAsync_PrimaryIsAFallbackModelItself_SkipsDuplicateInChain()
    {
        // Primary == one of the fallback tier names -> chain must not repeat it.
        _transport.EnqueueForModel("gemini-2.5-flash", new InvalidOperationException("503 unavailable"));
        _transport.EnqueueForModel("gemini-2.5-flash", new InvalidOperationException("503 unavailable"));
        _transport.EnqueueForModel("gemini-2.5-flash-lite", GeminiTestResults.Text("ok"));

        await _sut.GenerateTextAsync("prompt", new GeminiCallOptions("gemini-2.5-flash"), CancellationToken.None);

        _transport.CallsInOrder.Should().Equal("gemini-2.5-flash", "gemini-2.5-flash", "gemini-2.5-flash-lite");
    }

    [Fact]
    public void ModelChainBuilder_ForArbitraryPrimary_ProducesFixedOrderPrimaryThenThreeFlashTiers()
    {
        IReadOnlyList<string> chain = GeminiModelChainBuilder.Build("gemini-2.5-pro");

        chain.Should().Equal("gemini-2.5-pro", "gemini-2.5-flash", "gemini-2.5-flash-lite", "gemini-flash-latest");
    }

    // ── Transient vs. model-level classification ────────────────────────────
    [Theory]
    [InlineData("503 Service Unavailable")]
    [InlineData("model is UNAVAILABLE right now")]
    [InlineData("server overloaded, try again")]
    [InlineData("experiencing high demand")]
    [InlineData("HTTP 429 Too Many Requests")]
    [InlineData("rate limit exceeded")]
    [InlineData("deadline exceeded")]
    [InlineData("request timeout")]
    [InlineData("ECONNRESET")]
    [InlineData("socket hang up")]
    [InlineData("fetch failed")]
    public void GeminiErrorClassifier_TransientMarkers_AreClassifiedAsTransientNotModelLevel(string message)
    {
        GeminiErrorClassifier.IsTransient(message).Should().BeTrue();
        GeminiErrorClassifier.IsModelLevel(message).Should().BeFalse();
    }

    [Theory]
    [InlineData("404 model not found")]
    [InlineData("NOT_FOUND")]
    [InlineData("model gemini-1.0-pro is no longer available")]
    [InlineData("requested entity is not found")]
    [InlineData("PERMISSION_DENIED: caller lacks access")]
    [InlineData("this feature is unsupported for this model")]
    public void GeminiErrorClassifier_ModelLevelMarkers_AreClassifiedAsModelLevelNotTransient(string message)
    {
        GeminiErrorClassifier.IsModelLevel(message).Should().BeTrue();
        GeminiErrorClassifier.IsTransient(message).Should().BeFalse();
    }

    [Fact]
    public void GeminiErrorClassifier_UnrecognizedMessage_IsNeitherTransientNorModelLevel()
    {
        GeminiErrorClassifier.IsTransient("some totally unrelated failure").Should().BeFalse();
        GeminiErrorClassifier.IsModelLevel("some totally unrelated failure").Should().BeFalse();
    }

    [Fact]
    public async Task GenerateTextAsync_ModelLevelError_MakesOnlyOneAttemptOnThatModel()
    {
        _transport.EnqueueForModel(Primary, new InvalidOperationException("PERMISSION_DENIED"));
        _transport.EnqueueForModel("gemini-2.5-flash", GeminiTestResults.Text("ok"));

        GeminiGenerationResult result = await _sut.GenerateTextAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        result.Text.Should().Be("ok");
        _transport.CallsInOrder.Should().Equal(Primary, "gemini-2.5-flash");

        // Model-level errors skip the same-model retry (no 1200ms-scaled backoff), but the chain loop
        // still applies its fixed 800ms inter-model pacing delay before trying the next model -- that
        // delay is not a retry-backoff, it is the chain's own "give the next tier a beat" pacing.
        _delay.Requested.Should().Equal(TimeSpan.FromMilliseconds(800));
    }

    [Fact]
    public async Task GenerateTextAsync_NonTransientNonModelLevelError_RethrowsImmediatelyWithoutFallback()
    {
        _transport.EnqueueForModel(Primary, new InvalidOperationException("totally unexpected failure"));

        Func<Task> act = () => _sut.GenerateTextAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("totally unexpected failure");
        _transport.CallsInOrder.Should().Equal(Primary);
    }

    // ── JSON extraction and repair ladder ───────────────────────────────────
    [Fact]
    public async Task GenerateJsonAsync_CleanJson_ReturnsAsIs()
    {
        _transport.Enqueue(GeminiTestResults.Text("{\"a\":1}"));

        GeminiGenerationResult result = await _sut.GenerateJsonAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        result.Text.Should().Be("{\"a\":1}");
    }

    [Fact]
    public async Task GenerateJsonAsync_LeadingMarkdownFence_StripsFenceBeforeParsing()
    {
        // StripFencesAndPreamble only strips a *leading* fence (Node's `.replace(/^```.../...)`) --
        // prose appearing BEFORE the fence is not itself a supported shape (the Node system prompt's
        // whole purpose is to prevent the model from emitting prose at all), so this test uses a
        // fence at the very start of the text, which is the documented/handled shape.
        _transport.Enqueue(GeminiTestResults.Text("```json\n{\"a\":1,\"b\":2}\n```"));

        GeminiGenerationResult result = await _sut.GenerateJsonAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        result.Text.Should().Be("{\"a\":1,\"b\":2}");
    }

    [Fact]
    public async Task GenerateJsonAsync_TruncatedFlatArray_RepairsToLastCompleteElement()
    {
        // RepairTruncated's "safe boundary" tracking only fires at container depth 1 for the SAME
        // bracket type as the root (it does not descend into a differently-typed nested container --
        // e.g. objects inside a truncated array do not themselves advance the tracked depth). A flat
        // array of scalars is the shape this ladder cleanly repairs.
        _transport.Enqueue(GeminiTestResults.Text("[\"one\",\"two\",\"thre"));

        GeminiGenerationResult result = await _sut.GenerateJsonAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        result.Text.Should().Be("[\"one\",\"two\"]");
    }

    [Fact]
    public async Task GenerateJsonAsync_TruncationInsideNestedObjectValue_LosesWholeNestedValue_ExhaustsAndThrows()
    {
        // Honest limitation: RepairTruncated only tracks depth for the root's own bracket type, so
        // truncation *inside* a nested object value is not something it can safely repair -- trimming
        // to the last depth-1 boundary here would mean truncating before the nested object even opens,
        // i.e. before any safe boundary exists at all if the truncation happens before the first
        // top-level comma. This documents that the repair ladder is a best-effort heuristic, not a
        // general JSON recovery tool, and callers must not assume nested truncation is recoverable.
        const string truncated = "{\"nested\":{\"x\":1,\"y\":2,\"z\":\"trunc";
        _transport.Enqueue(GeminiTestResults.Text(truncated));
        _transport.Enqueue(GeminiTestResults.Text(truncated));
        _transport.Enqueue(GeminiTestResults.Text(truncated));

        Func<Task> act = () => _sut.GenerateJsonAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        await act.Should().ThrowAsync<GeminiUnavailableException>();
    }

    [Fact]
    public async Task GenerateJsonAsync_TrailingCommentaryAfterTopLevelObject_IsNotRepairable_ExhaustsAndThrows()
    {
        // Documents the flip side of the above: a bare top-level object (not an array) with trailing
        // prose after its closing brace is NOT something RepairTruncated can fix, because depth never
        // revisits 1 after the object's own close. Every one of the 3 parse attempts gets this same
        // unparseable text back, so the ladder exhausts and GenerateJsonAsync must fail loudly rather
        // than silently truncate to something that was never validated.
        _transport.Enqueue(GeminiTestResults.Text("{\"a\":1} -- hope that helps!"));
        _transport.Enqueue(GeminiTestResults.Text("{\"a\":1} -- hope that helps!"));
        _transport.Enqueue(GeminiTestResults.Text("{\"a\":1} -- hope that helps!"));

        Func<Task> act = () => _sut.GenerateJsonAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        await act.Should().ThrowAsync<GeminiUnavailableException>();
    }

    [Fact]
    public async Task GenerateJsonAsync_TruncatedJson_RepairsByClosingLastCompleteElement()
    {
        // Truncated mid-value for the third field; repair should close after the last complete "b":2,
        _transport.Enqueue(GeminiTestResults.Text("{\"a\":1,\"b\":2,\"c\":\"incomple"));

        GeminiGenerationResult result = await _sut.GenerateJsonAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        result.Text.Should().Be("{\"a\":1,\"b\":2}");
    }

    [Fact]
    public async Task GenerateJsonAsync_MalformedBeyondRepair_RetriesUpToThreeTimesThenThrowsUnavailable()
    {
        _transport.Enqueue(GeminiTestResults.Text("not json at all, no braces here"));
        _transport.Enqueue(GeminiTestResults.Text("still not json"));
        _transport.Enqueue(GeminiTestResults.Text("nope"));

        Func<Task> act = () => _sut.GenerateJsonAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        await act.Should().ThrowAsync<GeminiUnavailableException>();
        _transport.CallsInOrder.Should().HaveCount(3, "3-attempt parse ladder, one model call each since every attempt succeeds at the transport level");
    }

    [Fact]
    public async Task GenerateJsonAsync_ParseFailureThenSuccess_RetriesAndSucceedsWithoutExhausting()
    {
        _transport.Enqueue(GeminiTestResults.Text("no json here"));
        _transport.Enqueue(GeminiTestResults.Text("{\"ok\":true}"));

        GeminiGenerationResult result = await _sut.GenerateJsonAsync("prompt", new GeminiCallOptions(Primary), CancellationToken.None);

        result.Text.Should().Be("{\"ok\":true}");
        _delay.Requested.Should().HaveCount(1, "one wait between the two parse attempts");
    }

    [Fact]
    public async Task GenerateJsonAsync_AlwaysPrependsJsonOnlySystemPrefixVerbatim()
    {
        GeminiGenerationRequest? captured = null;
        _transport.Enqueue(GeminiTestResults.Text("{}"));

        // Wrap transport to capture the request's system instruction via a second fake that delegates.
        var capturingTransport = new CapturingTransport(_transport, r => captured = r);
        var client = new GeminiClient(capturingTransport, "key", _delay, _fixedJitter);

        await client.GenerateJsonAsync("prompt", new GeminiCallOptions(Primary, SystemInstruction: "extra context"), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.SystemInstruction.Should().Be(GeminiClient.JsonOnlySystemPrefix + "extra context");
    }

    // ── Grounding-absent failure path ───────────────────────────────────────
    [Fact]
    public async Task GenerateTextAsync_GroundingRequiredButAbsent_ThrowsGeminiGroundingAbsentException()
    {
        _transport.Enqueue(GeminiTestResults.Text("plausible-looking but unverified answer"));

        Func<Task> act = () => _sut.GenerateTextAsync(
            "prompt",
            new GeminiCallOptions(Primary, UseGoogleSearchTool: true, RequireGrounding: true),
            CancellationToken.None);

        await act.Should().ThrowAsync<GeminiGroundingAbsentException>();
    }

    [Fact]
    public async Task GenerateTextAsync_GroundingRequiredAndPresent_Succeeds()
    {
        _transport.Enqueue(GeminiTestResults.Grounded("grounded answer"));

        GeminiGenerationResult result = await _sut.GenerateTextAsync(
            "prompt",
            new GeminiCallOptions(Primary, UseGoogleSearchTool: true, RequireGrounding: true),
            CancellationToken.None);

        result.Text.Should().Be("grounded answer");
    }

    [Fact]
    public async Task GenerateJsonAsync_GroundingRequiredButAbsent_ThrowsBeforeAttemptingJsonParse()
    {
        _transport.Enqueue(GeminiTestResults.Text("{\"a\":1}"));

        Func<Task> act = () => _sut.GenerateJsonAsync(
            "prompt",
            new GeminiCallOptions(Primary, UseGoogleSearchTool: true, RequireGrounding: true),
            CancellationToken.None);

        await act.Should().ThrowAsync<GeminiGroundingAbsentException>("grounding must be checked even for otherwise-valid JSON");
    }

    // ── GenerateImageAsync ───────────────────────────────────────────────────
    [Fact]
    public async Task GenerateImageAsync_Success_ReturnsInlineImage()
    {
        _transport.Enqueue(GeminiTestResults.Image("base64data"));

        GeminiGenerationResult result = await _sut.GenerateImageAsync("prompt", new GeminiCallOptions("gemini-2.5-flash-image"), CancellationToken.None);

        result.InlineImages.Should().ContainSingle().Which.Base64Data.Should().Be("base64data");
    }

    [Fact]
    public async Task GenerateImageAsync_NoInlineImageReturned_ThrowsGeminiUnavailableException()
    {
        _transport.Enqueue(GeminiTestResults.Text("I can't draw that."));

        Func<Task> act = () => _sut.GenerateImageAsync("prompt", new GeminiCallOptions("gemini-2.5-flash-image"), CancellationToken.None);

        await act.Should().ThrowAsync<GeminiUnavailableException>();
    }

    [Fact]
    public async Task GenerateImageAsync_TransientErrorThenSuccess_RetriesSameModelOnly_NoFallbackChain()
    {
        _transport.EnqueueForModel("gemini-2.5-flash-image", new InvalidOperationException("503 unavailable"));
        _transport.EnqueueForModel("gemini-2.5-flash-image", GeminiTestResults.Image("recovered"));

        GeminiGenerationResult result = await _sut.GenerateImageAsync("prompt", new GeminiCallOptions("gemini-2.5-flash-image"), CancellationToken.None);

        result.InlineImages.Should().ContainSingle().Which.Base64Data.Should().Be("recovered");
        _transport.CallsInOrder.Should().Equal("gemini-2.5-flash-image", "gemini-2.5-flash-image");
    }

    /// <summary>A transport wrapper that captures the last request passed through, used to inspect the exact system-instruction text sent.</summary>
    private sealed class CapturingTransport : IGeminiTransport
    {
        private readonly IGeminiTransport _inner;
        private readonly Action<GeminiGenerationRequest> _capture;

        public CapturingTransport(IGeminiTransport inner, Action<GeminiGenerationRequest> capture)
        {
            _inner = inner;
            _capture = capture;
        }

        public Task<GeminiGenerationResult> GenerateContentAsync(string apiKey, GeminiGenerationRequest request, CancellationToken cancellationToken)
        {
            _capture(request);
            return _inner.GenerateContentAsync(apiKey, request, cancellationToken);
        }
    }
}
