# Finora Branding Assets

The canonical editable vector source is `src/Finora.App/Resources/AppIcon/appicon.svg`. The shape combines a wallet/ledger silhouette, a wallet opening, and an upward clarity/progress line. It intentionally avoids gambling, trading, and cryptocurrency imagery.

## Included sources

- `Resources/AppIcon/appicon.svg` — primary full icon source.
- `Resources/AppIcon/appiconfg.svg` — adaptive foreground source used by .NET MAUI.
- `Resources/AppIcon/appiconmonochrome.svg` — single-color source for system surfaces that require a mask/monochrome treatment.
- `Resources/Splash/splash.svg` — light splash mark.
- `Resources/Splash/splash-dark.svg` — dark splash source for platform-specific packaging where a separate dark launch asset is supported.

.NET MAUI produces the platform icon renditions from the declared `MauiIcon`; do not manually distort or crop the source. Verify generated Android adaptive icons, iOS/macOS icon sets, and Windows package assets on their native release toolchains.

## Wordmark and watermark

The UI wordmark is **Finora**. The attribution line is **Made by the Sanskar**. Do not place either inside the small app-icon glyph; small text is not legible at launcher sizes. The attribution belongs on splash/about/footer surfaces.

## Store listing guidance

- Store icon: render the primary source to each store's required square size without adding tiny text.
- Feature graphic: use a calm finance dashboard composition, the Finora wordmark, and no fabricated financial returns.
- Screenshots: use synthetic/sample financial data only; never capture real user finance data.
- Dark listing artwork: use the dark splash/background source while preserving sufficient contrast.
- Favicon/web companion: derive a square SVG/PNG from the primary icon if a web companion is introduced later.

Before publishing, regenerate raster assets using the current Android Studio/Xcode/Visual Studio packaging tools and verify safe zones at 24 px and store-preview sizes.
