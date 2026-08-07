using FluentValidation;
using Icbank.Platform.Domain.Gac;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>Validates <see cref="IngestGacSocialPostsCommand"/> (R-BE-034), mirroring the Node source's Zod schema (max 100 posts).</summary>
public sealed class IngestGacSocialPostsCommandValidator : AbstractValidator<IngestGacSocialPostsCommand>
{
    private const int MaxPosts = 100;

    /// <summary>Initializes a new instance of the <see cref="IngestGacSocialPostsCommandValidator"/> class.</summary>
    public IngestGacSocialPostsCommandValidator()
    {
        RuleFor(command => command.Posts).NotNull().Must(posts => posts.Count <= MaxPosts)
            .WithMessage($"posts must contain at most {MaxPosts} items.");
        RuleForEach(command => command.Posts).SetValidator(new IngestGacSocialPostItemValidator());
    }

    private sealed class IngestGacSocialPostItemValidator : AbstractValidator<IngestGacSocialPostItem>
    {
        public IngestGacSocialPostItemValidator()
        {
            RuleFor(post => post.ExternalId).NotEmpty();
            RuleFor(post => post.Platform).NotEmpty().Must(BeAKnownPlatform)
                .WithMessage("platform must be one of: " + string.Join(", ", Enum.GetNames<GacSocialPlatform>()));
        }

        private static bool BeAKnownPlatform(string value) => Enum.TryParse<GacSocialPlatform>(value, ignoreCase: true, out _);
    }
}
