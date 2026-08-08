using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Creates reusable poster fragments while keeping all extracted display strings encoded.</summary>
internal static partial class IconEventHtmlFragments
{
    private static readonly Regex FontSizeRegex = new(@"font-size:(\d+)px", RegexOptions.CultureInvariant);

    internal static string BackgroundPattern(string? iconName, string color)
    {
        var icon = IconEventIconLibrary.Render(iconName, 60, color);
        return $"data:image/svg+xml;utf8,{Uri.EscapeDataString(icon).Replace("'", "%27", StringComparison.Ordinal)}";
    }

    internal static string DiamondMeshPattern(string color, double opacity = 0.08)
    {
        var svg = $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"600\" height=\"600\" viewBox=\"0 0 600 600\"><g fill=\"none\" stroke=\"{color}\" stroke-width=\"2\" opacity=\"{opacity.ToString(CultureInfo.InvariantCulture)}\"><path d=\"M 0 300 L 300 0 L 600 300 L 300 600 Z\"/><path d=\"M -150 300 L 150 0 L 450 300 L 150 600 Z\"/><path d=\"M 150 300 L 450 0 L 750 300 L 450 600 Z\"/><path d=\"M 0 150 L 300 -150 L 600 150 L 300 450 Z\"/><path d=\"M 0 450 L 300 150 L 600 450 L 300 750 Z\"/></g></svg>";
        return $"data:image/svg+xml;utf8,{Uri.EscapeDataString(svg).Replace("'", "%27", StringComparison.Ordinal)}";
    }

    internal static string RenderContactChip(string icon, string? text, IconEventPalette colors, int fontSize)
    {
        var safeText = IconEventRenderContext.Encode(text);
        var svg = IconEventIconLibrary.Render(icon, fontSize + 4, colors.Accent);
        return $"<div style=\"display:inline-flex;align-items:center;gap:12px;background:rgba(255,255,255,0.14);padding:14px 26px;border-radius:50px;border:1.5px solid rgba(255,255,255,0.22);direction:ltr;\"><span style=\"color:{colors.Accent};display:inline-flex;\">{svg}</span><span style=\"font-size:{fontSize}px;font-weight:700;color:#fff;\">{safeText}</span></div>";
    }

    internal static string RenderDepartment(string? department, IconEventRenderContext context, int? top = null, int? left = null, int zIndex = 10)
    {
        if (string.IsNullOrWhiteSpace(department) || IconEventSizeCatalog.SuppressesChrome(context.Input.Size))
        {
            return string.Empty;
        }

        IconEventSizeTokens tokens = context.Tokens;
        return $"<div style=\"position:absolute;top:{top ?? tokens.Margin}px;left:{left ?? tokens.Margin}px;background:{context.Palette.Accent};color:#fff;padding:{tokens.DeptPaddingV}px {tokens.DeptPaddingH}px;border-radius:0;font-weight:800;font-size:{tokens.DeptFont}px;letter-spacing:0.5px;line-height:1;white-space:nowrap;z-index:{zIndex};box-shadow:0 2px 6px rgba(0,0,0,0.15);\">{IconEventRenderContext.Encode(department)}</div>";
    }

    internal static string RenderLogo(string? logoUrl, string positionStyle, int heightPx, IconEventSizePreset size)
    {
        if (IconEventSizeCatalog.SuppressesChrome(size))
        {
            return string.Empty;
        }

        return IsExternalLogo(logoUrl) ? RenderExternalLogo(logoUrl!, positionStyle, heightPx) : RenderLogoSvg(positionStyle, heightPx);
    }

    internal static string RenderMetaChip(string icon, string? text, IconEventPalette colors, int fontSize)
    {
        var safeText = IconEventRenderContext.Encode(text);
        var svg = IconEventIconLibrary.Render(icon, fontSize + 4, colors.Accent);
        return $"<div style=\"display:inline-flex;align-items:center;gap:12px;background:rgba(255,255,255,0.14);padding:14px 26px;border-radius:50px;border:1.5px solid rgba(255,255,255,0.22);\"><span style=\"color:{colors.Accent};display:inline-flex;\">{svg}</span><span style=\"font-size:{fontSize}px;font-weight:700;color:#fff;\">{safeText}</span></div>";
    }

    internal static string RenderParagraphFlow(IconEventParagraphFlow flow, string paragraphStyle, IconEventPalette colors, int metaFont, string align = "center", int? subHeadSize = null, int? bulletSize = null)
    {
        var parts = new List<string>();
        var baseSize = GetBaseSize(paragraphStyle);
        var headingSize = subHeadSize ?? (int)Math.Round(baseSize * 1.15);
        var listSize = bulletSize ?? Math.Max(baseSize - 4, 22);
        for (var index = 0; index < flow.Blocks.Count; index++)
        {
            AppendBlock(parts, flow.Blocks, ref index, paragraphStyle, colors, metaFont, align, headingSize, listSize);
        }

        return string.Concat(parts);
    }

    internal static bool HasTextBlocks(IconEventParagraphFlow flow) => flow.Blocks.Any(block => block.Kind == "text");

    private static void AppendBlock(List<string> parts, IReadOnlyList<IconEventParagraphBlock> blocks, ref int index, string paragraphStyle, IconEventPalette colors, int metaFont, string align, int headingSize, int listSize)
    {
        IconEventParagraphBlock block = blocks[index];
        if (block.Kind == "text")
        {
            parts.Add($"<p style=\"{paragraphStyle}\">{IconEventRenderContext.Encode(block.Content)}</p>");
            return;
        }

        if (block.Kind == "sub-heading")
        {
            AppendHeading(parts, blocks, ref index, colors, align, headingSize, listSize);
            return;
        }

        if (block.Kind == "bullet-list")
        {
            parts.Add(RenderBulletList(block.Items ?? Array.Empty<string>(), colors, align, listSize));
            return;
        }

        parts.Add(RenderInlineContact(block, colors, metaFont));
    }

    private static void AppendHeading(List<string> parts, IReadOnlyList<IconEventParagraphBlock> blocks, ref int index, IconEventPalette colors, string align, int headingSize, int listSize)
    {
        IconEventParagraphBlock heading = blocks[index];
        var joinsList = index + 1 < blocks.Count && blocks[index + 1].Kind == "bullet-list";
        var listAlign = align == "center" ? "right" : align;
        var headingHtml = RenderSubHeading(heading.Content, colors, align, headingSize, joinsList);
        if (joinsList)
        {
            index++;
            parts.Add($"<div style=\"display:flex;flex-direction:column;gap:6px;text-align:{listAlign};\">{headingHtml}{RenderBulletList(blocks[index].Items ?? Array.Empty<string>(), colors, align, listSize)}</div>");
        }
        else
        {
            parts.Add(headingHtml);
        }
    }

    private static int GetBaseSize(string paragraphStyle)
    {
        Match match = FontSizeRegex.Match(paragraphStyle);
        return match.Success && int.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var size) ? size : 32;
    }

    private static bool IsExternalLogo(string? logoUrl) =>
        Uri.TryCreate(logoUrl, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string RenderBulletList(IReadOnlyList<string> items, IconEventPalette colors, string align, int bulletSize)
    {
        var listAlign = align == "center" ? "right" : align;
        var dotSize = Math.Max(6, (int)Math.Round(bulletSize * 0.32));
        var dotGap = Math.Max(8, (int)Math.Round(bulletSize * 0.4));
        var dotMarginTop = (int)Math.Round(bulletSize * 0.5);
        var listGap = Math.Max(6, (int)Math.Round(bulletSize * 0.35));
        var html = new StringBuilder();
        foreach (var item in items)
        {
            html.Append(CultureInfo.InvariantCulture, $"<li style=\"display:flex;align-items:flex-start;gap:{dotGap}px;text-align:{listAlign};direction:rtl;\"><span style=\"flex-shrink:0;margin-top:{dotMarginTop}px;width:{dotSize}px;height:{dotSize}px;border-radius:50%;background:{colors.Accent};\"></span><span style=\"flex:1;font-size:{bulletSize}px;line-height:1.55;font-weight:500;color:#fff;opacity:0.95;\">{IconEventRenderContext.Encode(item)}</span></li>");
        }

        return $"<ul style=\"list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:{listGap}px;text-align:{listAlign};\">{html}</ul>";
    }

    private static string RenderExternalLogo(string logoUrl, string positionStyle, int heightPx) =>
        $"<img src=\"{IconEventRenderContext.Encode(logoUrl)}\" style=\"{positionStyle};height:{heightPx}px;\" crossorigin=\"anonymous\" alt=\"GAC\" />";

    private static string RenderInlineContact(IconEventParagraphBlock block, IconEventPalette colors, int metaFont)
    {
        var icon = block.Kind == "email-chip" ? "mail" : "phone";
        return $"<div style=\"text-align:center;margin:6px 0;\">{RenderContactChip(icon, block.Content, colors, metaFont)}</div>";
    }

    private static string RenderLogoSvg(string positionStyle, int heightPx)
    {
        var widthPx = (int)Math.Round(heightPx * 3.22);
        var style = $"{positionStyle};height:{heightPx}px;width:{widthPx}px;";
        return LogoOpenTagRegex().Replace(IconEventVisualAssets.GacLogoWhiteSvg, $"<svg$1 style=\"{style}\" preserveAspectRatio=\"xMidYMid meet\">", 1);
    }

    private static string RenderSubHeading(string content, IconEventPalette colors, string align, int size, bool tightBottom)
    {
        var top = tightBottom ? "0" : "10";
        var bottom = tightBottom ? "6" : "4";
        return $"<h3 style=\"font-size:{size}px;color:{colors.Accent};font-weight:800;margin:{top}px 0 {bottom}px;line-height:1.25;text-align:{align};\">{IconEventRenderContext.Encode(content)}</h3>";
    }

    [GeneratedRegex("<svg([^>]*)>", RegexOptions.CultureInvariant)]
    private static partial Regex LogoOpenTagRegex();
}
