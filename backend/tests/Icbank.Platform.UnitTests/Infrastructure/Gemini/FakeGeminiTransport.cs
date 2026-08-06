using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// A scripted <see cref="IGeminiTransport"/> double: each call consumes the next scripted
/// response/exception for the model it targets (falling back to a shared default queue when no
/// per-model queue is registered), and every call is recorded so tests can assert exactly which
/// models were tried, in what order, and how many times -- all without a single real HTTP call.
/// </summary>
public sealed class FakeGeminiTransport : IGeminiTransport
{
    private readonly Queue<Func<GeminiGenerationResult>> _defaultScript = new();
    private readonly Dictionary<string, Queue<Func<GeminiGenerationResult>>> _perModelScript = new(StringComparer.Ordinal);

    /// <summary>Gets every call made, in order, as (model, attemptNumberForThatModel).</summary>
    public List<string> CallsInOrder { get; } = [];

    /// <summary>Queues a successful result to be returned for the next call to any model without its own queue.</summary>
    /// <param name="result">The result to return.</param>
    public void Enqueue(GeminiGenerationResult result) => _defaultScript.Enqueue(() => result);

    /// <summary>Queues an exception to be thrown for the next call to any model without its own queue.</summary>
    /// <param name="exception">The exception to throw.</param>
    public void Enqueue(Exception exception) => _defaultScript.Enqueue(() => throw exception);

    /// <summary>Queues a successful result scoped to one specific model id.</summary>
    /// <param name="model">The model id this response applies to.</param>
    /// <param name="result">The result to return.</param>
    public void EnqueueForModel(string model, GeminiGenerationResult result) => ScriptFor(model).Enqueue(() => result);

    /// <summary>Queues an exception scoped to one specific model id.</summary>
    /// <param name="model">The model id this exception applies to.</param>
    /// <param name="exception">The exception to throw.</param>
    public void EnqueueForModel(string model, Exception exception) => ScriptFor(model).Enqueue(() => throw exception);

    /// <inheritdoc />
    public Task<GeminiGenerationResult> GenerateContentAsync(string apiKey, GeminiGenerationRequest request, CancellationToken cancellationToken)
    {
        CallsInOrder.Add(request.Model);
        Queue<Func<GeminiGenerationResult>> queue = _perModelScript.TryGetValue(request.Model, out Queue<Func<GeminiGenerationResult>>? modelQueue)
            ? modelQueue
            : _defaultScript;

        if (queue.Count == 0)
        {
            throw new InvalidOperationException($"FakeGeminiTransport: no scripted response left for model '{request.Model}'.");
        }

        return Task.FromResult(queue.Dequeue()());
    }

    private Queue<Func<GeminiGenerationResult>> ScriptFor(string model)
    {
        if (!_perModelScript.TryGetValue(model, out Queue<Func<GeminiGenerationResult>>? queue))
        {
            queue = new Queue<Func<GeminiGenerationResult>>();
            _perModelScript[model] = queue;
        }

        return queue;
    }
}
