using System.Reflection;
using System.Text;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Loads the immutable poster assets once so document rendering has no filesystem dependency.</summary>
internal static class IconEventVisualAssets
{
    private const string ResourcePrefix = "Icbank.Platform.Infrastructure.Designs.Assets.";

    private static readonly Lazy<string> FontCssValue = new(CreateFontCss);

    private static readonly Lazy<string> AutoFitScriptValue = new(() => ReadText("IconEventAutoFit.js"));

    private static readonly Lazy<string> GacLogoWhiteDataUriValue = new(() => CreateDataUri("image/png", "GacLogoWhite.png"));

    private static readonly Lazy<string> GacLogoWhiteSvgValue = new(() => ReadText("GacLogoWhite.svg"));

    private static readonly Lazy<string> StatsHeroBackgroundDataUriValue = new(() => CreateDataUri("image/png", "StatsHeroBackground.png"));

    internal static string FontCss => FontCssValue.Value;

    internal static string AutoFitScript => AutoFitScriptValue.Value;

    internal static string GacLogoWhiteDataUri => GacLogoWhiteDataUriValue.Value;

    internal static string GacLogoWhiteSvg => GacLogoWhiteSvgValue.Value;

    internal static string StatsHeroBackgroundDataUri => StatsHeroBackgroundDataUriValue.Value;

    private static string CreateDataUri(string mediaType, string resourceName) =>
        $"data:{mediaType};base64,{Convert.ToBase64String(ReadBytes(resourceName))}";

    private static string CreateFontCss()
    {
        var light = CreateDataUri("font/ttf", "Frutiger-LT-Arabic-45-Light.ttf");
        var roman = CreateDataUri("font/ttf", "Frutiger-LT-Arabic-55-Roman.ttf");
        var bold = CreateDataUri("font/ttf", "Frutiger-LT-Arabic-65-Bold.ttf");
        return CreateFontFace(light, "300") + CreateFontFace(roman, "400") + CreateFontFace(roman, "500") + CreateFontFace(bold, "600") + CreateFontFace(bold, "700") + CreateFontFace(bold, "800") + CreateFontFace(bold, "900");
    }

    private static string CreateFontFace(string source, string weight) =>
        $"@font-face{{font-family:'Frutiger LT Arabic';src:url('{source}') format('truetype');font-weight:{weight};font-style:normal;font-display:block;}}";

    private static byte[] ReadBytes(string resourceName)
    {
        using Stream stream = OpenStream(resourceName);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static string ReadText(string resourceName)
    {
        using Stream stream = OpenStream(resourceName);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static Stream OpenStream(string resourceName)
    {
        Assembly assembly = typeof(IconEventVisualAssets).Assembly;
        Stream? stream = assembly.GetManifestResourceStream(ResourcePrefix + resourceName);
        return stream ?? throw new InvalidOperationException($"Embedded design asset '{resourceName}' was not found.");
    }
}
