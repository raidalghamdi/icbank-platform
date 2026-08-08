using FluentValidation;
using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>Validates <see cref="GenerateIconEventStudioCommand"/> (R-BE-034).</summary>
public sealed class GenerateIconEventStudioCommandValidator : AbstractValidator<GenerateIconEventStudioCommand>
{
    private const int MaxSizesPerRequest = 5;

    /// <summary>Initializes a new instance of the <see cref="GenerateIconEventStudioCommandValidator"/> class.</summary>
    public GenerateIconEventStudioCommandValidator()
    {
        RuleFor(command => command.Content).NotNull().WithMessage("محتوى التصميم مطلوب");
        RuleFor(command => command.Content.Headline).NotEmpty().WithMessage("headline مطلوب").When(command => command.Content is not null);

        RuleFor(command => command.Sizes)
            .Must(sizes => sizes is null || sizes.Count <= MaxSizesPerRequest)
            .WithMessage($"لا يمكن طلب أكثر من {MaxSizesPerRequest} مقاسات في الطلب الواحد");

        RuleForEach(command => command.Sizes)
            .Must(size => IconEventSizeCatalog.TryParse(size, out _))
            .WithMessage($"مقاس غير معروف — المقاسات المتاحة: {string.Join(", ", IconEventSizeCatalog.WireValues)}");
    }
}
