using FluentValidation;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>
/// Validates <see cref="ExportActivityLogQuery"/>. A malformed or absurd date range must fail
/// fast with a clear Problem Details response rather than silently returning zero rows or, worse,
/// running an unbounded table scan every filter-less export already caps via
/// <see cref="ExportActivityLogQueryHandler.MaxRows"/>.
/// </summary>
public sealed class ExportActivityLogQueryValidator : AbstractValidator<ExportActivityLogQuery>
{
    /// <summary>Initializes a new instance of the <see cref="ExportActivityLogQueryValidator"/> class.</summary>
    public ExportActivityLogQueryValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue)
            .WithMessage("معرف المستخدم غير صالح");

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("يجب أن يسبق تاريخ البداية تاريخ النهاية");
    }
}
