using System.Text.Json;
using FluentValidation;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Validates <see cref="EditWeekendDraftContentCommand"/>. Ports the Node source's <c>typeof content === "object"</c> shape check.</summary>
public sealed class EditWeekendDraftContentCommandValidator : AbstractValidator<EditWeekendDraftContentCommand>
{
    /// <summary>Initializes a new instance of the <see cref="EditWeekendDraftContentCommandValidator"/> class.</summary>
    public EditWeekendDraftContentCommandValidator()
    {
        RuleFor(x => x.ContentJson)
            .NotEmpty().WithMessage("content مطلوب")
            .Must(IsJsonObject).WithMessage("content مطلوب");
    }

    private static bool IsJsonObject(string contentJson)
    {
        try
        {
            using var document = JsonDocument.Parse(contentJson);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
