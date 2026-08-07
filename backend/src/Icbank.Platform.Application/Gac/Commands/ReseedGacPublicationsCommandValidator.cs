using FluentValidation;
using Icbank.Platform.Domain.Gac;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>Validates <see cref="ReseedGacPublicationsCommand"/> (R-BE-034).</summary>
public sealed class ReseedGacPublicationsCommandValidator : AbstractValidator<ReseedGacPublicationsCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ReseedGacPublicationsCommandValidator"/> class.</summary>
    public ReseedGacPublicationsCommandValidator()
    {
        RuleForEach(command => command.Publications).SetValidator(new ReseedGacPublicationItemValidator());
    }

    private sealed class ReseedGacPublicationItemValidator : AbstractValidator<ReseedGacPublicationItem>
    {
        private static readonly string KnownCategories = string.Join(", ", Enum.GetNames<GacPublicationCategory>());
        private static readonly string KnownLanguages = string.Join(", ", Enum.GetNames<GacPublicationLanguage>());
        private static readonly string KnownSourceDomains = string.Join(", ", Enum.GetNames<GacPublicationSourceDomain>());

        public ReseedGacPublicationItemValidator()
        {
            RuleFor(item => item.TitleAr).NotEmpty();
            RuleFor(item => item.FileUrl).NotEmpty();
            RuleFor(item => item.Category).NotEmpty().Must(BeAKnownCategory)
                .WithMessage("category must be one of: " + KnownCategories);
            RuleFor(item => item.Language).NotEmpty().Must(BeAKnownLanguage)
                .WithMessage("language must be one of: " + KnownLanguages);
            RuleFor(item => item.SourceDomain).NotEmpty().Must(BeAKnownSourceDomain)
                .WithMessage("sourceDomain must be one of: " + KnownSourceDomains);
        }

        private static bool BeAKnownCategory(string value) => Enum.TryParse<GacPublicationCategory>(value, ignoreCase: true, out _);

        private static bool BeAKnownLanguage(string value) => Enum.TryParse<GacPublicationLanguage>(value, ignoreCase: true, out _);

        private static bool BeAKnownSourceDomain(string value) => Enum.TryParse<GacPublicationSourceDomain>(value, ignoreCase: true, out _);
    }
}
