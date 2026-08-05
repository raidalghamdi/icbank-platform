using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.IconEvent.Queries;

/// <summary>Handles <see cref="ListIconEventIconsQuery"/>.</summary>
public sealed class ListIconEventIconsQueryHandler : IRequestHandler<ListIconEventIconsQuery, Result<IconEventIconCatalogDto>>
{
    /// <inheritdoc />
    public Task<Result<IconEventIconCatalogDto>> Handle(ListIconEventIconsQuery request, CancellationToken cancellationToken)
    {
        var icons = IconLibrary.All
            .Select(icon => new IconEventIconDto(icon.Name, icon.LabelAr, icon.Category.ToString().ToLowerInvariant(), icon.Keywords))
            .ToList();

        return Task.FromResult(Result<IconEventIconCatalogDto>.Success(new IconEventIconCatalogDto(icons, icons.Count)));
    }
}
