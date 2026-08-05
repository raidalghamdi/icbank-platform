namespace Icbank.Platform.Application.Common.Models;

/// <summary>
/// Intent-revealing return type for use-case handlers (R-BE-003, R-BE-090): success/failure is
/// part of the method signature instead of being signalled via exceptions-as-control-flow.
/// </summary>
/// <typeparam name="T">The type of the value produced on success.</typeparam>
/// <remarks>
/// Why: CA1000 ("do not declare static members on generic types") is suppressed below. The
/// conventions doc mandates <c>Result&lt;T&gt;.Success(...)</c>/<c>Failure(...)</c> factory
/// methods verbatim (§3.7) as the canonical, copy-once pattern; a non-generic factory class would
/// diverge from the contract every handler in the codebase is written against.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Result<T>.Success/Failure factory methods are the mandated pattern (conventions doc §3.7).")]
public readonly struct Result<T>
{
    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the produced value when <see cref="IsSuccess"/> is <c>true</c>; otherwise <c>null</c>.</summary>
    public T? Value { get; }

    /// <summary>Gets the failure reason when <see cref="IsSuccess"/> is <c>false</c>; otherwise <c>null</c>.</summary>
    public string? Error { get; }

    /// <summary>Creates a successful result wrapping <paramref name="value"/>.</summary>
    /// <param name="value">The value produced by the operation.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>Creates a failed result carrying <paramref name="error"/>.</summary>
    /// <param name="error">A human-readable description of why the operation failed.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static Result<T> Failure(string error) => new(false, default, error);
}
