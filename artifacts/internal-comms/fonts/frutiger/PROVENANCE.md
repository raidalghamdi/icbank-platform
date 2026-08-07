# Frutiger LT Arabic — provenance and licensing

Frutiger LT Arabic is the GAC-approved typeface for this platform. It is the only text
face the application declares, on the web UI and in generated PDFs.

## Files

| File | Weight | Used by |
|---|---|---|
| `FrutigerLTArabic-Light.woff2` | 300 | Web — large display headings |
| `FrutigerLTArabic-Roman.woff2` | 400 | Web — body, preloaded |
| `FrutigerLTArabic-Bold.woff2` | 700 | Web — emphasis and section heads, preloaded |

The PDF renderer carries the same three faces as TTF embedded resources under
`backend/src/Icbank.Platform.Infrastructure/Rendering/Fonts/`.

## Licensing — resolve before production

**These binaries are not confirmed to be licensed for web use.** They were supplied as
`.ttf` files whose names carry the `alfont_com` prefix, which is not an authorised
Monotype distribution channel. Their own embedded `name` records identify them as
Monotype/Linotype proprietary software:

- `name` ID 7 (trademark): *Frutiger is a trademark of Monotype Imaging Inc.*
- `name` ID 13 (licence): *This font software is the property of Monotype Imaging Inc.*
- `name` ID 14 (licence URL): `http://www.monotype.com`, `http://www.linotype.com/license`
- `OS/2.fsType` = **4** — *Preview & Print embedding*

`fsType=4` permits embedding a font in a document for viewing and printing. It does
**not** grant the right to serve the font file over HTTP, which is what `@font-face`
does — every visitor downloads a copy. Self-hosted web use requires a separate Monotype
webfont licence, normally counted in pageviews or domains.

Two actions are required before this reaches production:

1. **Confirm GAC's Frutiger licence covers self-hosted web embedding** for this platform's
   domains and traffic, and covers embedding in generated PDFs.
2. **Replace these binaries with the files from GAC's official brand package.** Beyond the
   licence question, the supplied files are of mixed provenance and will not render
   consistently: the 55 Roman is Monotype "Version 4.00 Build 1000" while the 45 Light and
   65 Bold are Linotype "Version 1.00; 2007". Different vintages of the same family can
   carry different metrics and spacing.

Swapping the binaries later requires no code change as long as the filenames are kept.

## Modifications made

The **web** `.woff2` files are unmodified apart from WOFF2 compression.

The **PDF** `.ttf` files have rewritten `name` records (IDs 1, 2, 4, 6, 16, 17) plus
aligned `usWeightClass`, `fsSelection` and `macStyle`. The vendor files disagree about
their own identity — the 55 Roman reports the family "Frutiger LT Arabic 55 Roman", and
the 65 Bold reports the family "Frutiger LT Arabic 45 Light" with a Bold subfamily.
QuestPDF resolves faces by the family name inside the file, so registered as-supplied
they become three unrelated families and bold text silently renders as Regular. The
outlines are untouched.

## Coverage

All three files carry 559 glyphs over 589 codepoints and the complete Arabic shaping set:
`init`, `medi`, `fina`, `isol`, `ccmp`, `rlig`, `calt`, plus `mark`/`mkmk` for diacritic
positioning. Latin is included, so the same family serves both scripts.

Checked against every character the interface renders: no Arabic or Latin gaps. The only
uncovered characters are emoji and dingbats, which resolve to the platform emoji font in
any case and never came from the text face.

## Rule

Do not add another `@font-face`, and never load a face from a third-party CDN. A CDN font
fails behind the corporate firewall, leaks traffic off-network, and silently puts an
unapproved face on screen whenever it is unreachable. The `landing.py` harness asserts
that Frutiger LT Arabic is the only declared family and that no external font host is
referenced; it will fail the build if either changes.

## Verified

Web: the harness checks, at all five widths, that Frutiger LT Arabic is the only declared
family, that nothing is served off-origin, that no third-party font stylesheet is linked,
that `body` resolves to the approved face, and that all three weights decode. Each guard
was proved by reintroducing the fault it is meant to catch and confirming it fails.

Backend: a PDF rendered through the QuestPDF path embeds two distinct subsets,
`FrutigerLTArabic` and `FrutigerLTArabicBold`. Bold is a real weight, not a synthesised
one — which is what the unnormalised name tables would have produced. Arabic shaping,
joining and diacritics were checked on the rendered page.
