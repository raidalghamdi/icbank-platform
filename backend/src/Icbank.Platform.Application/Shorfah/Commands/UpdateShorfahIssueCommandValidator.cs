using FluentValidation;
using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Validates <see cref="UpdateShorfahIssueCommand"/>. Adds a real enum check on <see cref="UpdateShorfahIssueCommand.Status"/> the Node source lacked entirely.</summary>
public sealed class UpdateShorfahIssueCommandValidator : AbstractValidator<UpdateShorfahIssueCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateShorfahIssueCommandValidator"/> class.</summary>
    public UpdateShorfahIssueCommandValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => Enum.TryParse<ShorfahIssueStatus>(status, ignoreCase: true, out _))
            .When(x => x.Status is not null)
            .WithMessage("حالة العدد غير صالحة");
    }
}
