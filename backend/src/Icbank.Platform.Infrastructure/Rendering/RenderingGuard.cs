namespace Icbank.Platform.Infrastructure.Rendering;

/// <summary>
/// Shared input/output guards for every local (non-outbound) rendering operation in this
/// namespace: PDF composition, DOCX composition, and document text extraction. These are CPU-bound
/// in-process operations, not network calls, so they are not candidates for Polly's HTTP-resilience
/// pipeline (R-BE-095 scopes Polly to outbound calls) -- instead every entry point here is wrapped
/// in an explicit wall-clock timeout via <see cref="RunWithTimeoutAsync{T}"/>, and every input/output
/// byte size is capped so a malicious or corrupt document cannot exhaust memory
/// (<see cref="EnsureWithinLimit"/> throws <see cref="RenderingValidationException"/>, never lets
/// the process reach an <see cref="OutOfMemoryException"/>).
/// </summary>
public static class RenderingGuard
{
    /// <summary>The largest input/output document this pipeline will accept, in bytes (25 MB, matching the existing upload cap in <c>UploadArchiveDocumentsCommandValidator</c>).</summary>
    public const long MaxDocumentBytes = 25 * 1024 * 1024;

    /// <summary>The wall-clock budget for a single local render/extract operation.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);

    /// <summary>Validates a byte count against <see cref="MaxDocumentBytes"/>, throwing a clear, catchable error instead of risking an out-of-memory condition downstream.</summary>
    /// <param name="byteCount">The size to validate.</param>
    /// <param name="subject">A short human-readable description of what is being measured, used in the error message.</param>
    public static void EnsureWithinLimit(long byteCount, string subject)
    {
        if (byteCount > MaxDocumentBytes)
        {
            throw new RenderingValidationException($"{subject} exceeds the {MaxDocumentBytes / (1024 * 1024)}MB limit ({byteCount} bytes).");
        }
    }

    /// <summary>Runs a CPU-bound rendering delegate on the thread pool with a wall-clock timeout.</summary>
    /// <typeparam name="T">The delegate's result type.</typeparam>
    /// <param name="render">The synchronous rendering work to run off the calling thread.</param>
    /// <param name="cancellationToken">The caller's cancellation token, observed in addition to the internal timeout.</param>
    /// <param name="timeout">An optional override of <see cref="DefaultTimeout"/>.</param>
    /// <returns>The rendering delegate's result.</returns>
    public static async Task<T> RunWithTimeoutAsync<T>(Func<T> render, CancellationToken cancellationToken, TimeSpan? timeout = null)
    {
        using var timeoutSource = new CancellationTokenSource(timeout ?? DefaultTimeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        var renderTask = Task.Run(render, linkedSource.Token);
        try
        {
            return await renderTask.WaitAsync(linkedSource.Token);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new RenderingValidationException($"Rendering exceeded the {(timeout ?? DefaultTimeout).TotalSeconds}-second timeout.");
        }
    }
}
