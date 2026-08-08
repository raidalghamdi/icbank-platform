using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.IconEvent;

/// <summary>Renders an <see cref="IconEventInput"/> into a complete HTML poster document.</summary>
public interface IIconEventHtmlRenderer
{
    /// <summary>Renders the given input into a complete, encoded HTML document.</summary>
    /// <param name="input">The fully-resolved design input.</param>
    /// <returns>The rendered HTML document string.</returns>
    string Render(IconEventInput input);
}
