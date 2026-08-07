using FluentValidation;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Validates <see cref="AssignShorfahSectionCommand"/>. Ports the Node source's <c>!userId</c> check (<c>shorfah.ts:873</c>).</summary>
public sealed class AssignShorfahSectionCommandValidator : AbstractValidator<AssignShorfahSectionCommand>
{
    /// <summary>Initializes a new instance of the <see cref="AssignShorfahSectionCommandValidator"/> class.</summary>
    public AssignShorfahSectionCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("بيانات ناقصة");
    }
}
