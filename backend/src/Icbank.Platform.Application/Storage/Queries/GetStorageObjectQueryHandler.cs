using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Storage.Queries;

/// <summary>Handles <see cref="GetStorageObjectQuery"/>.</summary>
public sealed class GetStorageObjectQueryHandler : IRequestHandler<GetStorageObjectQuery, Result<StoredObject>>
{
    // Why: weekend-place images are uploaded under "weekend/" and the shipped frontend reads
    // them through this authenticated proxy. Keeping the prefix in the hardened validator makes
    // those images reachable without reintroducing the Node app's public-bucket assumption
    // (BUSINESS-RULES.md §12.1, AMBIGUOUS-BR-10).
    private static readonly string[] AllowedPrefixes = { "ai-year/2026/", "designs/", "gac/", "shorfah/", "weekend/" };

    private readonly ISafeStoragePathValidator _pathValidator;
    private readonly IObjectStorageReader _storageReader;

    /// <summary>Initializes a new instance of the <see cref="GetStorageObjectQueryHandler"/> class.</summary>
    /// <param name="pathValidator">The traversal-safe path validator (closes SEC-17).</param>
    /// <param name="storageReader">The object-storage read port.</param>
    public GetStorageObjectQueryHandler(ISafeStoragePathValidator pathValidator, IObjectStorageReader storageReader)
    {
        _pathValidator = pathValidator;
        _storageReader = storageReader;
    }

    /// <inheritdoc />
    public async Task<Result<StoredObject>> Handle(GetStorageObjectQuery request, CancellationToken cancellationToken)
    {
        SafePathValidationResult validation = _pathValidator.Validate(request.RawPath, AllowedPrefixes);
        if (!validation.IsValid)
        {
            return Result<StoredObject>.Failure(validation.RejectionReason ?? "invalid_path");
        }

        StoredObject? found = await _storageReader.OpenAsync(validation.NormalizedPath!, cancellationToken);
        return found is null
            ? Result<StoredObject>.Failure("not_found")
            : Result<StoredObject>.Success(found);
    }
}
