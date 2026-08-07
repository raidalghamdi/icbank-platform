namespace Icbank.Platform.Application.Common.Models;

/// <summary>
/// Standard pagination request parameters for every collection endpoint (R-BE-033). The page
/// size is silently clamped rather than rejected, so a client requesting too much data degrades
/// gracefully instead of erroring.
/// </summary>
public record PagedQuery
{
    /// <summary>The upper bound on <see cref="PageSize"/> — R-BE-095 named constant, never inlined.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The default number of items returned per page when the caller does not specify one.</summary>
    public const int DefaultPageSize = 25;

    private readonly int _pageSize = DefaultPageSize;

    /// <summary>Gets the 1-based page number to return. Defaults to 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Gets the number of items per page, clamped to the inclusive range [1, <see cref="MaxPageSize"/>].</summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }
}
