# Design System

## 1. Intent

The design system borrows Bootstrap 5's semantic color vocabulary and clean component treatment while adapting density, focus behavior, typography, and interaction to native Windows desktop applications.

Controls consume semantic tokens. Hex values are defaults owned by the theme, not values copied throughout rendering code.

## 2. Default semantic palette

Bootstrap-inspired semantic defaults:

| Token | Default |
| --- | --- |
| Primary | `#0D6EFD` |
| Secondary | `#6C757D` |
| Success | `#198754` |
| Danger | `#DC3545` |
| Warning | `#FFC107` |
| Info | `#0DCAF0` |
| Light | `#F8F9FA` |
| Dark | `#212529` |

These values define semantic variants. Application surfaces use separate tokens so the UI does not become a collection of saturated Bootstrap colors.

## 3. Light theme surface tokens

Initial defaults:

| Token | Purpose |
| --- | --- |
| Body | Application/form background |
| Surface | Card/input/control surface |
| SurfaceSecondary | Secondary/hovered surface |
| Border | Neutral border |
| Text | Primary text |
| MutedText | Secondary text |
| Disabled | Disabled foreground/background basis |
| Focus | Focus ring/border |
| Hover | Interactive hover overlay/token |
| Active | Pressed/selected overlay/token |

Exact non-semantic values should be centralized in the theme implementation and may be tuned during the demo/hardening phases.

## 4. Dark theme

Dark mode is not created by simply swapping white and black. It needs independent Surface, SurfaceSecondary, Border, Text, MutedText, Hover, Active, and Disabled tokens while retaining recognizable semantic Primary/Success/Danger/etc. variants with sufficient contrast.

Controls must not contain code such as `if dark then Color.Black else Color.White` except for deliberate contrast calculations. Prefer theme tokens or a shared contrast helper.

## 5. Typography

Default desktop typeface: Segoe UI.

Typography should be represented as tokens rather than new `Font` allocations inside every paint pass.

Recommended initial roles:

- Body
- BodySmall
- Label
- HeadingSmall
- HeadingMedium
- Monospace only when a component genuinely needs it

Font ownership must be explicit. Do not dispose shared theme fonts from individual controls.

## 6. Density and control sizing

The framework targets productive desktop/business applications, so the default density should remain compact without becoming cramped.

Recommended starting metrics at 100% DPI:

| Token | Initial value |
| --- | ---: |
| ControlHeightSmall | 28 px |
| ControlHeight | 32 px |
| ControlHeightLarge | 38 px |
| RadiusSmall | 4 px |
| Radius | 6 px |
| RadiusLarge | 8 px |
| BorderWidth | 1 px |
| FocusBorderWidth | 2 px |

These are design tokens, not permanent hard-coded API guarantees. Tune them visually in the demo before stable release.

## 7. Spacing scale

Use a small spacing scale rather than arbitrary values:

| Token | Initial value |
| --- | ---: |
| SpacingXS | 4 px |
| SpacingSM | 8 px |
| SpacingMD | 12 px |
| SpacingLG | 16 px |
| SpacingXL | 24 px |

Padding may reuse the same scale or expose semantic padding tokens if repeated patterns justify them.

All pixel metrics must be scaled through DPI helpers before rendering/layout where WinForms does not already scale them.

## 8. Interactive states

Every interactive custom control must deliberately define:

1. Normal
2. Hover
3. Pressed/Active
4. Focused
5. Disabled
6. Selected/Expanded when relevant
7. Loading when relevant

State precedence must be deterministic. For example, Disabled wins over Hover and Pressed; Focus may coexist with Selected; Loading suppresses click interaction but still communicates focus/disabled semantics appropriately.

## 9. Focus

Focus must be visible without relying only on color changes too subtle for keyboard users.

Use a focus border/ring based on the Focus token and scaled focus width. Do not remove focus indication because a mouse hover style looks cleaner.

## 10. Icons

Icons should visually align with the text baseline and control size. The framework should normalize icon size and color independent of source.

Internal structural icons such as chevrons should prefer simple vector paths owned by the framework. Business/action icons may come from SVG, MDL2, FontAwesome, or application-provided sources through the icon abstraction.

## 11. Shadows

Use shadow sparingly. Desktop business UI benefits more from clear borders and surface hierarchy than heavy elevation.

Card shadow must be optional. Avoid expensive blur generation on every paint. A lightweight precomputed/cached approach is acceptable if lifecycle and DPI are handled correctly.

## 12. Motion

Motion should clarify state change rather than decorate the UI.

Recommended starting durations:

- Hover/color transitions: usually immediate or very short
- Collapse/expand: approximately 160–220 ms
- Progress value transition: approximately 200–300 ms
- Spinner loop: approximately 750 ms

Reduced motion should shorten or disable nonessential motion while always applying the correct final state.

## 13. Validation states

Inputs should support at least neutral, valid/success, warning when needed, and error/danger states. Validation must not be communicated by color alone; supporting text/icon patterns can be added at component/application level.

## 14. Designer defaults

A control dropped from the Toolbox should have a sensible visible size and should render safely with the default theme without requiring code in `Program.Main` or `Form.Load`.

## 15. Phase 1 theme API

Phase 1 implements the design system as immutable token objects under `MyDmsVn.Bootstrap5WinFormUI.Theme`:

- `BootstrapTheme` groups mode, colors, metrics, typography, and the reduced-motion preference.
- `BootstrapThemeColors` owns semantic and application-surface colors; Light and Dark are separate palettes.
- `BootstrapThemeMetrics` owns unscaled 100%-DPI sizing, radius, border, and spacing values.
- `BootstrapThemeTypography` owns typography roles through `BootstrapFontToken` descriptors. Tokens describe font family, point size, and style; they do not own disposable GDI `Font` instances.
- `BootstrapThemeManager` provides a safe Light default and publishes `ThemeChanged` whenever the application assigns a different theme instance.

Create and switch the application theme through the manager:

```csharp
var theme = BootstrapTheme.CreateDefault(
    BootstrapThemeMode.Dark,
    reducedMotion: true);

BootstrapThemeManager.CurrentTheme = theme;
```

Controls introduced in later phases must read semantic values from `BootstrapThemeManager.CurrentTheme`, subscribe to `ThemeChanged` only for the lifetime in which they need notifications, and unsubscribe deterministically when disposed. Reduced motion is part of the theme so animation infrastructure can consume one application-level preference without introducing a second settings channel.

## 16. Phase 2 rendering and DPI API

Phase 2 adds reusable stateless foundation types under `MyDmsVn.Bootstrap5WinFormUI.Rendering`:

- `DpiScaler` treats design-token pixels as 96-DPI logical pixels and scales integer/floating values plus common `Size`, `Padding`, and `Rectangle` geometry for a target DPI.
- `CornerRadius` represents uniform or independent top-left/top-right/bottom-right/bottom-left radii. `NormalizeTo(...)` proportionally reduces oversized radii so adjacent corners do not overlap.
- `RoundedPath.Create(...)` creates a closed `GraphicsPath` from rectangle bounds and `CornerRadius`. The caller owns and must dispose the returned path.
- `ColorUtil` provides sRGB relative luminance, contrast ratio, higher-contrast foreground selection, and color blending.
- `ContentLayoutHelper` arranges two optional horizontal content items as one aligned group after applying WinForms `Padding`; it is intended for later icon/text combinations without coupling controls to an icon source.

Example DPI scaling from the 100%-DPI metric baseline:

```csharp
var controlHeight = DpiScaler.Scale(theme.Metrics.ControlHeight, targetDpi);
var padding = DpiScaler.Scale(new Padding(theme.Metrics.SpacingSM), targetDpi);
```

Example rounded-path ownership:

```csharp
using var path = RoundedPath.Create(
    bounds,
    new CornerRadius(DpiScaler.Scale((float)theme.Metrics.Radius, targetDpi)));

graphics.FillPath(brush, path);
```

The foundation deliberately does not expose a reflection-based double-buffer toggle or create a base-control hierarchy in Phase 2. Concrete custom-painted controls should use normal protected WinForms buffering styles (`DoubleBuffered` / `SetStyle`) in their own construction path. A shared control base or lifecycle helper should be introduced only when later controls demonstrate real repeated behavior that cannot be cleanly expressed with standard WinForms APIs.

The demo application's **Rendering / DPI** window renders the same foundation geometry at virtual 96/120/144/168/192 DPI (100/125/150/175/200%) and responds to runtime theme changes. This gives a repeatable visual verification path, but it does not replace testing the application under actual Windows display-scaling settings.

## 17. Phase 3 icon API

Phase 3 adds source-neutral icon infrastructure under `MyDmsVn.Bootstrap5WinFormUI.Icons`:

- `IconDescriptor` describes the source and source-specific value without exposing renderer implementation details to controls.
- `IconSourceKind` distinguishes Segoe MDL2, SVG, framework vector, and optional/external sources.
- `IIconProvider` handles one source family; `IIconRenderer` is the control-facing rendering contract.
- `BootstrapIconRenderer` dispatches a descriptor to registered providers in order and returns `false` when no provider can render it.
- `SegoeMdl2IconProvider` renders Windows Segoe MDL2 Assets glyphs and fails gracefully when the expected font is unavailable.
- `FrameworkVectorIconProvider` renders small framework-owned structural glyphs such as chevrons, check, close, plus, and minus without an external package.
- `SvgIconProvider` delegates SVG markup to an `ISvgIconRenderer` supplied by the application or an optional adapter package. The core assembly intentionally does not choose or reference an SVG library.

Controls should render through `IIconRenderer` rather than branch on source kind:

```csharp
var renderer = BootstrapIconRenderer.CreateDefault();
var icon = IconDescriptor.SegoeMdl2('\uE713');

renderer.TryRender(graphics, icon, iconBounds, theme.Colors.Text);
```

To add SVG support, compose the provider with a compatible renderer adapter:

```csharp
var renderer = new BootstrapIconRenderer(new IIconProvider[]
{
    new SegoeMdl2IconProvider(),
    new FrameworkVectorIconProvider(),
    new SvgIconProvider(mySvgRenderer)
});
```

`ISvgIconRenderer` is intentionally small: an adapter receives SVG markup, a target rectangle, and a requested foreground color. The adapter owns any SVG-library-specific parsing, caching, recoloring, and disposal policy. If a chosen SVG library cannot support both target frameworks, it belongs in an optional adapter assembly rather than the core package.

FontAwesome.Sharp follows the same optional-integration rule. An optional adapter can implement `IIconProvider`, accept descriptors created with `IconDescriptor.External("FontAwesome.Sharp", iconName)`, resolve `iconName` to the package's icon type, and render it. Only that adapter package references FontAwesome.Sharp; `MyDmsVn.Bootstrap5WinFormUI` remains dependency-free from FontAwesome.Sharp.

The demo application's **Icons** window renders Segoe MDL2 and framework vector descriptors through the same `IIconRenderer` path. Switch Light/Dark while the window is open and resize it to verify source-neutral color/alignment behavior. SVG adapter behavior is covered through the provider contract and automated delegation tests; applications should visually validate their chosen SVG adapter separately.
