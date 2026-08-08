namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Resolves supported color scheme names without allowing input into CSS declarations.</summary>
internal static class IconEventColorMap
{
    private static readonly Dictionary<string, IconEventPalette> Palettes =
        new Dictionary<string, IconEventPalette>(StringComparer.OrdinalIgnoreCase)
        {
            ["teal"] = new("#0E5862", "#0A4148", "#9DC41A", "#0E5862"),
            ["blue"] = new("#0069A7", "#00567D", "#46BCCD", "#0069A7"),
            ["green"] = new("#61A60E", "#009845", "#9DC41A", "#61A60E"),
            ["cyan"] = new("#46BCCD", "#0069A7", "#00567D", "#46BCCD"),
            ["navy"] = new("#194F90", "#00567D", "#46BCCD", "#194F90"),
        };

    internal static IconEventPalette Resolve(string? colorScheme) =>
        Palettes.TryGetValue(colorScheme ?? string.Empty, out IconEventPalette? palette) ? palette : Palettes["blue"];
}
