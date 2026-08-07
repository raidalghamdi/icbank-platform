using FluentValidation;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Validates <see cref="UploadArchiveDocumentsCommand"/>. Ports the Node source's 100-file/25MB-each multer limits.</summary>
public sealed class UploadArchiveDocumentsCommandValidator : AbstractValidator<UploadArchiveDocumentsCommand>
{
    private const int MaxFileCount = 100;
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    /// <summary>Initializes a new instance of the <see cref="UploadArchiveDocumentsCommandValidator"/> class.</summary>
    public UploadArchiveDocumentsCommandValidator()
    {
        RuleFor(x => x.Files).NotEmpty().WithMessage("لم يتم رفع أي ملف");
        RuleFor(x => x.Files).Must(files => files.Count <= MaxFileCount).WithMessage($"لا يمكن رفع أكثر من {MaxFileCount} ملف");
        RuleForEach(x => x.Files).Must(f => f.Content.LongLength <= MaxFileSizeBytes).WithMessage("حجم الملف يتجاوز الحد المسموح (25MB)");
    }
}
