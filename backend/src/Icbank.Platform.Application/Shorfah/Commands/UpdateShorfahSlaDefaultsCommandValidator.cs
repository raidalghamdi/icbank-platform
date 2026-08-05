using FluentValidation;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Validates <see cref="UpdateShorfahSlaDefaultsCommand"/>. Ports the Node source's <c>!Array.isArray(defaults)</c> check (<c>shorfah.ts:279</c>).</summary>
public sealed class UpdateShorfahSlaDefaultsCommandValidator : AbstractValidator<UpdateShorfahSlaDefaultsCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateShorfahSlaDefaultsCommandValidator"/> class.</summary>
    public UpdateShorfahSlaDefaultsCommandValidator()
    {
        RuleFor(x => x.Defaults).NotNull().WithMessage("defaults يجب أن يكون مصفوفة");
    }
}
