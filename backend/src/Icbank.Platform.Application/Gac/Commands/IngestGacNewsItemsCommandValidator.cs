using FluentValidation;
using Icbank.Platform.Domain.Gac;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>Validates <see cref="IngestGacNewsItemsCommand"/>.</summary>
public sealed class IngestGacNewsItemsCommandValidator : AbstractValidator<IngestGacNewsItemsCommand>
{
    private const int MaxItems = 200;

    /// <summary>Initializes a new instance of the <see cref="IngestGacNewsItemsCommandValidator"/> class.</summary>
    public IngestGacNewsItemsCommandValidator()
    {
        RuleFor(command => command.Items).NotNull().Must(items => items.Count <= MaxItems)
            .WithMessage($"items \u064a\u062c\u0628 \u0623\u0646 \u062a\u062d\u062a\u0648\u064a \u0639\u0644\u0649 {MaxItems} \u0639\u0646\u0635\u0631\u064b\u0627 \u0643\u062d\u062f \u0623\u0642\u0635\u0649.");
        RuleForEach(command => command.Items).SetValidator(new IngestGacNewsItemValidator());
    }

    private sealed class IngestGacNewsItemValidator : AbstractValidator<IngestGacNewsItem>
    {
        public IngestGacNewsItemValidator()
        {
            RuleFor(item => item.TitleAr).NotEmpty().WithMessage("titleAr \u0645\u0637\u0644\u0648\u0628.");
            RuleFor(item => item.SourceUrl).NotEmpty().WithMessage("sourceUrl \u0645\u0637\u0644\u0648\u0628 \u0644\u0623\u0646\u0651\u0647 \u0645\u0641\u062a\u0627\u062d \u0645\u0646\u0639 \u0627\u0644\u062a\u0643\u0631\u0627\u0631.")
                .Must(BeAnAbsoluteUrl).WithMessage("sourceUrl \u064a\u062c\u0628 \u0623\u0646 \u064a\u0643\u0648\u0646 \u0631\u0627\u0628\u0637\u064b\u0627 \u0645\u0637\u0644\u0642\u064b\u0627.");
            RuleFor(item => item.Kind).Must(BeAKnownKind)
                .WithMessage("kind must be one of: " + string.Join(", ", Enum.GetNames<GacNewsKind>()));
            RuleFor(item => item.Category).Must(BeAKnownCategory)
                .WithMessage("category must be one of: " + string.Join(", ", Enum.GetNames<GacNewsCategory>()));
        }

        private static bool BeAnAbsoluteUrl(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private static bool BeAKnownKind(string? value) =>
            string.IsNullOrWhiteSpace(value) || Enum.TryParse<GacNewsKind>(value, ignoreCase: true, out _);

        private static bool BeAKnownCategory(string? value) =>
            string.IsNullOrWhiteSpace(value) || Enum.TryParse<GacNewsCategory>(value, ignoreCase: true, out _);
    }
}
