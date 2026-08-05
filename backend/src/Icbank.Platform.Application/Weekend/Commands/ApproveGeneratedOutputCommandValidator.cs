using FluentValidation;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Validates <see cref="ApproveGeneratedOutputCommand"/>.</summary>
public sealed class ApproveGeneratedOutputCommandValidator : AbstractValidator<ApproveGeneratedOutputCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ApproveGeneratedOutputCommandValidator"/> class.</summary>
    public ApproveGeneratedOutputCommandValidator()
    {
        RuleFor(x => x.OutputId).GreaterThan(0).WithMessage("id مطلوب");
    }
}
