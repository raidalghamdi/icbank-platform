using FluentValidation;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Validates <see cref="SendShorfahSectionReminderCommand"/>. Ports the Node source's <c>!userId</c> check (<c>shorfah.ts:960</c>).</summary>
public sealed class SendShorfahSectionReminderCommandValidator : AbstractValidator<SendShorfahSectionReminderCommand>
{
    /// <summary>Initializes a new instance of the <see cref="SendShorfahSectionReminderCommandValidator"/> class.</summary>
    public SendShorfahSectionReminderCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("userId مطلوب");
    }
}
