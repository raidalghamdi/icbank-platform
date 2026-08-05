using FluentValidation;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Validates <see cref="GrantShorfahSectionPermissionCommand"/>. Ports the Node source's <c>!permission || (!userId &amp;&amp; !roleName)</c> check (<c>shorfah.ts:519</c>).</summary>
public sealed class GrantShorfahSectionPermissionCommandValidator : AbstractValidator<GrantShorfahSectionPermissionCommand>
{
    /// <summary>Initializes a new instance of the <see cref="GrantShorfahSectionPermissionCommandValidator"/> class.</summary>
    public GrantShorfahSectionPermissionCommandValidator()
    {
        RuleFor(x => x.Permission).NotEmpty().WithMessage("بيانات ناقصة");
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.RoleName))
            .WithMessage("بيانات ناقصة");
    }
}
