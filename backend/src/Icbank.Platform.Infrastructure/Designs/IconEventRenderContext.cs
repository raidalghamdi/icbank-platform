using System.Net;
using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Collects resolved dimensions, styling and encoded display values for one poster.</summary>
internal sealed class IconEventRenderContext
{
    internal IconEventRenderContext(IconEventInput input)
    {
        Input = input;
        IconEventSizeSpec size = IconEventSizeCatalog.Resolve(input.Size);
        Width = size.Width;
        Height = size.Height;
        Tokens = IconEventSizeTokens.Resolve(input.Size);
        Palette = IconEventColorMap.Resolve(input.ColorScheme);
        Plan = IconEventContentPlanner.Plan(input);
        Headline = Encode(Plan.Headline);
    }

    internal IconEventInput Input { get; }

    /// <summary>Gets the copy this canvas will render, already cut to what it can hold.</summary>
    internal IconEventContentPlan Plan { get; }

    internal int Height { get; }

    internal string Headline { get; }

    internal IconEventPalette Palette { get; }

    internal IconEventSizeTokens Tokens { get; }

    internal int Width { get; }

    internal static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    /// <summary>Converts a length authored against the 2000x1125 reference canvas into this canvas.</summary>
    /// <param name="referenceLength">The length in reference-canvas pixels.</param>
    /// <returns>The equivalent length for the current preset, never below one pixel.</returns>
    /// <remarks>
    /// Only the type ramp lives in <see cref="IconEventSizeTokens"/>; the geometry inside each layout
    /// (icon plates, column offsets, grid tiles) was authored once at 2000x1125. Emitting those raw
    /// values on a 639x479 card overflows the canvas and clips the headline, so every geometric
    /// length is projected through the canvas height instead of being hard-coded.
    /// </remarks>
    internal int Px(int referenceLength) => Math.Max(1, (int)Math.Round(referenceLength * (Height / 1125.0), MidpointRounding.AwayFromZero));
}
