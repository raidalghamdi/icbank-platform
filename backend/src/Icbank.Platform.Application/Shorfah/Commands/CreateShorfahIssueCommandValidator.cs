using FluentValidation;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Validates <see cref="CreateShorfahIssueCommand"/>. Ports the Node source's <c>!titleAr || !month || !year</c> check plus real month/year bounds it lacked (API-SURFACE.md §22).</summary>
public sealed class CreateShorfahIssueCommandValidator : AbstractValidator<CreateShorfahIssueCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateShorfahIssueCommandValidator"/> class.</summary>
    public CreateShorfahIssueCommandValidator()
    {
        RuleFor(x => x.TitleAr).NotEmpty().WithMessage("بيانات ناقصة");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("بيانات ناقصة");
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).WithMessage("بيانات ناقصة");
        RuleFor(x => x.IssueNo).GreaterThan(0).When(x => x.IssueNo.HasValue).WithMessage("رقم العدد غير صالح");
        RuleFor(x => x.ContributionsOpenAt)
            .LessThanOrEqualTo(x => x.ContributionsCloseAt)
            .When(x => x.ContributionsOpenAt.HasValue && x.ContributionsCloseAt.HasValue)
            .WithMessage("تاريخ فتح المساهمات يجب أن يسبق تاريخ إغلاقها");
    }
}
