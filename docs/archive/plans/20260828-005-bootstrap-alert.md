# BootstrapAlert Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Stage 2 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md` by adding a Bootstrap-inspired inline `BootstrapAlert` feedback surface with semantic variant styling, optional source-neutral icon, optional keyboard-accessible dismiss affordance, deterministic dismissal semantics, Light/Dark and DPI support, and no popup/auto-hide behavior.

**Architecture:** `BootstrapAlert : UserControl` owns one framework-painted rounded surface and one private native `Button` used only as the close affordance. Alert text and the optional `IconDescriptor` are painted by the Alert itself; the native close button preserves mature WinForms focus, Tab, Enter, Space, accessibility, and click semantics while its glyph is painted through the existing `IIconRenderer`. A small internal `BootstrapAlertRenderLogic` owns deterministic theme-derived palette, DPI-scaled metric, and layout calculations; the public control owns theme/font/DPI lifecycle and dismissal state transitions.

**Tech Stack:** C#, native Windows Forms, existing Theme / Rendering / Icons infrastructure, `BootstrapVariant`, `BootstrapVariantColorResolver`, `BootstrapThemeManager`, `BootstrapThemeColors`, `BootstrapThemeMetrics`, `DpiScaler`, `RoundedPath`, `CornerRadius`, `ColorUtil`, `IconDescriptor`, `IIconRenderer`, `BootstrapIconRenderer`, `FrameworkIconGlyph`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** Stage 2 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md`, plus repository-wide constraints in `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; the public control remains under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile for both `net48` and `net8.0-windows` from one shared implementation wherever practical.
- Stage 1 `BootstrapBadge` must be green before Stage 2 starts. Do not bypass the roadmap gate merely because Alert is independently implementable.
- `BootstrapAlert` is an inline feedback surface only. Do not add auto-hide, timers, animation, stacking, overlay hosting, toast placement, popup windows, or transient notification ownership; those belong to Stage 8 `BootstrapToast`.
- Alert does not become another Card. Do not add Header/Body/Footer regions, arbitrary rich-content APIs, HTML/Markdown parsing, embedded action collections, or a framework-specific child-content model.
- The only framework-owned child control is the private native dismiss `Button`. Do not compose `BootstrapButton`; the roadmap classifies Alert as depending on existing Icons, not on Button command styling.
- `Variant` resolves through the existing `BootstrapVariantColorResolver`; do not add an alert-specific eight-entry color table.
- Palette tinting must use the same formula for all variants. Theme tokens remain authoritative for Light/Dark, disabled, focus, text, surface, and border behavior.
- `Icon` is a source-neutral `IconDescriptor`; `IconRenderer` defaults to the built-in `BootstrapIconRenderer.CreateDefault()` and rejects `null`.
- The close affordance uses `IconDescriptor.Framework(FrameworkIconGlyph.Close)` through the configured `IconRenderer`; do not draw a separate hand-coded X geometry.
- `BorderRadius = -1` means current theme radius. Values below `-1` throw `ArgumentOutOfRangeException` without mutating the previous value.
- Undefined `BootstrapVariant` values are rejected before changing state.
- Alert itself is not a tab stop. The private close button is visible and in the tab sequence only while `Dismissible = true` and the Alert is effectively usable.
- `Dismiss()` hides the Alert and raises `Dismissed` exactly once for each effective visible-to-hidden transition caused through the Alert dismissal path. Repeated calls while already not visible are no-ops. Showing the Alert again re-enables a later dismissal event naturally.
- Caller code that directly sets `Visible = false` does **not** synthesize a `Dismissed` event. `Dismissed` means dismissal through `Dismiss()` or the close affordance, not every visibility change.
- `Dismiss()` does not dispose the Alert, its parent, or any caller-owned object.
- Programmatic `Dismiss()` remains legal when `Enabled = false`; disabled state only prevents user activation of the child close button and changes presentation.
- Designer construction must work without application bootstrap, DI, service locators, initialized icon adapters, or a preconfigured theme. The framework's safe default Light theme remains sufficient.
- Runtime theme changes update palette and theme-owned typography. Caller-assigned `Font` remains caller-owned and is never disposed by Alert.
- DPI changes recompute padding, icon size, close-button size, border width, radius, and layout through `DpiScaler`.
- Temporary GDI objects are scoped with `using`; no `GraphicsPath`, `Pen`, `Brush`, `Font`, or bitmap is retained per paint frame.
- The component adds public/protected API after the frozen v1 baseline. The API baseline must intentionally fail first, be reviewed, then receive a deliberately approved fingerprint update.
- No new package reference, native P/Invoke, custom window procedure, independent focus engine, or animation scheduler is part of this stage.

---

## Stage 1 Prerequisite Gate

At the time this plan was authored, the repository roadmap requires Stage 1 to land before Alert. Stage 2 execution starts with a hard preflight check rather than silently creating an alternate feedback demo structure.

The following Stage 1 artifacts are expected before Task 1 begins:

```text
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBadge.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBadgeRenderLogic.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBadgeTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapBadgeRenderLogicTests.cs
demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs
```

Before implementation:

```powershell
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapBadge.cs
Test-Path demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs
Test-Path tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs
```

All three commands must return `True`. If any returns `False`, stop Stage 2 and finish Stage 1 first. Do not create a second feedback page or duplicate the navigation entry as a workaround.

Then verify the Stage 1 gate:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapBadge|FeedbackDemoForm"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapBadge|FeedbackDemoForm"
```

Expected: both commands pass before Alert work begins.

---

## Stage 2 Public Contract

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Dismissed))]
public class BootstrapAlert : UserControl
{
    public BootstrapVariant Variant { get; set; }      // Primary
    public IconDescriptor? Icon { get; set; }          // null
    public IIconRenderer IconRenderer { get; set; }    // built-in default, never null
    public bool Dismissible { get; set; }               // false
    public int BorderRadius { get; set; }               // -1 = theme radius

    public event EventHandler? Dismissed;

    public void Dismiss();
}
```

### Public behavior

- `Text` is inherited from `Control`; native WinForms null-to-empty normalization remains authoritative. Alert only reacts to `OnTextChanged` for layout/repaint and does not redeclare a duplicate text property.
- `Variant` defaults to `BootstrapVariant.Primary` and selects the semantic basis color.
- `Icon` defaults to `null`. Setting/removing it updates layout and repaint without changing text or dismissal state.
- `IconRenderer` defaults to the framework renderer, is hidden from normal Designer serialization, and rejects `null` with `ArgumentNullException`.
- `Dismissible` defaults to `false`. When false, the close button is hidden and removed from the tab sequence. When true, the close button is shown and owns native keyboard activation.
- `BorderRadius = -1` uses `BootstrapThemeManager.CurrentTheme.Metrics.Radius`; a non-negative value is interpreted in logical 96-DPI pixels and scales through `DpiScaler`.
- `Dismiss()` is immediate and non-animated. If `Visible == true`, it sets `Visible = false` and raises `Dismissed` once. If `Visible == false`, it does nothing and raises no event.
- The close button `Click` handler calls `Dismiss()`; it must not contain a second event/visibility implementation.
- Setting `Visible = true` after a dismissal makes a later `Dismiss()` effective again. No separate `Reset()` or `ShowAlert()` API is added.
- Setting `Visible = false` directly raises no `Dismissed` event.
- `Enabled = false` uses disabled palette tokens and makes the child close button effectively disabled through normal WinForms parent/child behavior; programmatic `Dismiss()` still functions.
- Alert itself defaults to `TabStop = false` and `AccessibleRole = AccessibleRole.Alert` with a concise default accessibility description.
- The close button uses `AccessibleName = "Dismiss alert"` and `AccessibleDescription = "Dismisses this alert."`.
- There is no auto-hide duration, animation duration, show method, close reason enum, action button collection, or arbitrary content panel in Stage 2.

---

## Presentation Contract

### Theme-derived palette

Use one internal immutable palette:

```csharp
internal readonly struct BootstrapAlertPalette
{
    public BootstrapAlertPalette(
        Color surface,
        Color border,
        Color foreground,
        Color focus)
    {
        Surface = surface;
        Border = border;
        Foreground = foreground;
        Focus = focus;
    }

    public Color Surface { get; }
    public Color Border { get; }
    public Color Foreground { get; }
    public Color Focus { get; }
}
```

The resolver is:

```csharp
internal static BootstrapAlertPalette ResolvePalette(
    BootstrapThemeColors colors,
    BootstrapVariant variant,
    bool enabled)
```

Use these shared formula constants in `BootstrapAlertRenderLogic`; they are applied identically to every semantic variant and therefore are not an alert-specific variant table:

```csharp
private const float SurfaceSemanticAmount = 0.12f;
private const float BorderSemanticAmount = 0.45f;
private const float ForegroundSemanticAmount = 0.72f;
private const double MinimumTextContrast = 4.5d;
```

Enabled palette algorithm:

```csharp
var semantic = BootstrapVariantColorResolver.Resolve(colors, variant);
var surface = ColorUtil.Blend(semantic, colors.Surface, SurfaceSemanticAmount);
var border = ColorUtil.Blend(semantic, colors.Border, BorderSemanticAmount);
var foregroundCandidate = ColorUtil.Blend(semantic, colors.Text, ForegroundSemanticAmount);
var foreground = ColorUtil.GetContrastRatio(foregroundCandidate, surface) >= MinimumTextContrast
    ? foregroundCandidate
    : colors.Text;

return new BootstrapAlertPalette(surface, border, foreground, colors.Focus);
```

Disabled palette ignores semantic emphasis and uses existing neutral tokens:

```csharp
return new BootstrapAlertPalette(
    colors.SurfaceSecondary,
    colors.Border,
    colors.MutedText,
    colors.Disabled);
```

Rules:

- The formula works for Primary, Secondary, Success, Danger, Warning, Info, Light, and Dark without an eight-row lookup table.
- Light/Dark theme behavior changes naturally because `BootstrapThemeColors` changes.
- Warning/Light variants may fall back to `colors.Text` when the semantic-tinted foreground cannot meet the contrast threshold.
- Icon and close glyph use the same resolved `Foreground`; they do not invent separate semantic colors.
- Disabled always wins over `Variant` for presentation.

### DPI-scaled metrics

Use one internal immutable metric struct:

```csharp
internal readonly struct BootstrapAlertMetrics
{
    public BootstrapAlertMetrics(
        int horizontalPadding,
        int verticalPadding,
        int contentSpacing,
        int iconSize,
        int closeButtonSize,
        int borderWidth,
        int focusBorderWidth,
        int radius)
    {
        HorizontalPadding = horizontalPadding;
        VerticalPadding = verticalPadding;
        ContentSpacing = contentSpacing;
        IconSize = iconSize;
        CloseButtonSize = closeButtonSize;
        BorderWidth = borderWidth;
        FocusBorderWidth = focusBorderWidth;
        Radius = radius;
    }

    public int HorizontalPadding { get; }
    public int VerticalPadding { get; }
    public int ContentSpacing { get; }
    public int IconSize { get; }
    public int CloseButtonSize { get; }
    public int BorderWidth { get; }
    public int FocusBorderWidth { get; }
    public int Radius { get; }
}
```

The resolver is:

```csharp
internal static BootstrapAlertMetrics ResolveMetrics(
    BootstrapThemeMetrics metrics,
    int dpi,
    int borderRadius)
```

Token mapping before DPI scaling:

```text
HorizontalPadding = Metrics.SpacingMD
VerticalPadding   = Metrics.SpacingSM
ContentSpacing    = Metrics.SpacingSM
IconSize          = Metrics.SpacingLG
CloseButtonSize   = Metrics.ControlHeightSmall
BorderWidth       = Metrics.BorderWidth
FocusBorderWidth  = Metrics.FocusBorderWidth
Radius            = BorderRadius >= 0 ? BorderRadius : Metrics.Radius
```

`ResolveMetrics` rejects a null metrics object, `dpi <= 0`, and `borderRadius < -1`. Every logical value is scaled through `DpiScaler.Scale`; no control-local DPI formula is introduced.

At default metrics and 96 DPI the expected values are:

```text
HorizontalPadding = 12
VerticalPadding   = 8
ContentSpacing    = 8
IconSize          = 16
CloseButtonSize   = 28
BorderWidth       = 1
FocusBorderWidth  = 2
Radius            = 6
```

### Layout

Use one internal immutable layout struct:

```csharp
internal readonly struct BootstrapAlertLayout
{
    public BootstrapAlertLayout(
        Rectangle surfaceBounds,
        Rectangle contentBounds,
        Rectangle iconBounds,
        Rectangle textBounds,
        Rectangle closeBounds,
        CornerRadius cornerRadius)
    {
        SurfaceBounds = surfaceBounds;
        ContentBounds = contentBounds;
        IconBounds = iconBounds;
        TextBounds = textBounds;
        CloseBounds = closeBounds;
        CornerRadius = cornerRadius;
    }

    public Rectangle SurfaceBounds { get; }
    public Rectangle ContentBounds { get; }
    public Rectangle IconBounds { get; }
    public Rectangle TextBounds { get; }
    public Rectangle CloseBounds { get; }
    public CornerRadius CornerRadius { get; }
}
```

The pure layout helper is:

```csharp
internal static BootstrapAlertLayout CalculateLayout(
    Rectangle clientBounds,
    BootstrapAlertMetrics metrics,
    bool hasIcon,
    bool dismissible)
```

Layout rules:

1. `SurfaceBounds` is the supplied client rectangle normalized so width/height are never negative.
2. `ContentBounds` is `SurfaceBounds` inset by `HorizontalPadding` and `VerticalPadding`.
3. When `dismissible = true`, reserve a square `CloseBounds` at the trailing/right side of `ContentBounds`, vertically centered. Its side length is `min(CloseButtonSize, ContentBounds.Height)` and text reservation includes one `ContentSpacing` before the close slot.
4. When `hasIcon = true`, reserve a square `IconBounds` at the leading/left side, vertically centered. Its side length is `min(IconSize, ContentBounds.Height)` and text reservation includes one `ContentSpacing` after the icon slot.
5. `TextBounds` is the remaining rectangle between optional icon and optional close reservations. Width/height clamp to zero instead of producing negative rectangles on extremely narrow controls.
6. `CornerRadius = new CornerRadius(metrics.Radius)`.
7. Every non-empty icon/text/close rectangle is contained by `ContentBounds`; every content rectangle is contained by `SurfaceBounds`.
8. No layout helper allocates a WinForms control or GDI object.

Text painting uses:

```csharp
TextFormatFlags.NoPrefix |
TextFormatFlags.WordBreak |
TextFormatFlags.EndEllipsis |
TextFormatFlags.Left |
TextFormatFlags.VerticalCenter
```

The Alert does not expose a rich-text parser. Newlines in `Text` are honored by `TextRenderer`; long text wraps inside the caller-provided Alert bounds.

---

## Control Ownership and Lifecycle

### Private child close button

`BootstrapAlert` creates exactly one private native `Button` in its constructor:

```csharp
private readonly Button _dismissButton = new Button();
```

Configure it once:

```csharp
_dismissButton.AutoSize = false;
_dismissButton.Text = string.Empty;
_dismissButton.FlatStyle = FlatStyle.Flat;
_dismissButton.FlatAppearance.BorderSize = 0;
_dismissButton.FlatAppearance.MouseDownBackColor = Color.Transparent;
_dismissButton.FlatAppearance.MouseOverBackColor = Color.Transparent;
_dismissButton.UseVisualStyleBackColor = false;
_dismissButton.Visible = false;
_dismissButton.TabStop = false;
_dismissButton.AccessibleName = "Dismiss alert";
_dismissButton.AccessibleDescription = "Dismisses this alert.";
_dismissButton.Click += OnDismissButtonClick;
_dismissButton.Paint += OnDismissButtonPaint;
Controls.Add(_dismissButton);
```

Do not add a `Label` for Alert text or a `PictureBox` for Icon. Alert text/icon stay in one framework paint path, avoiding child layout/background seams.

`ApplyTheme()` sets the close button background to the current resolved Alert surface and foreground to the current resolved Alert foreground so the native button visually disappears into the Alert surface. The custom `Paint` handler renders the close icon through `_iconRenderer` and, when `_dismissButton.Focused && _dismissButton.ShowFocusCues`, paints a theme-focus rectangle inset by `SpacingXS` using the current DPI-scaled `FocusBorderWidth`.

### Theme font ownership

Follow the existing control pattern used by theme-aware controls:

```csharp
private bool _themeSubscribed;
private bool _settingThemeFont;
private bool _useThemeFont = true;
private Font? _themeFont;
```

- Constructor subscribes to `BootstrapThemeManager.ThemeChanged`, calls `ApplyThemeFont()`, then `ApplyTheme()`.
- `ApplyThemeFont()` creates a `Font` from `BootstrapThemeManager.CurrentTheme.Typography.Body` and assigns it while `_settingThemeFont = true`.
- `OnFontChanged` marks `_useThemeFont = false` only for caller-originated font changes, disposes the previously owned theme font, and invalidates/layouts the control.
- Theme changes recreate the theme font only while `_useThemeFont = true`.
- `Dispose(bool)` unsubscribes from `ThemeChanged`, disposes only `_themeFont`, and lets normal WinForms ownership dispose `_dismissButton`.
- Never dispose a caller-assigned `Font`, `IconDescriptor`, or `IIconRenderer`.

---

## File Structure

### Create product files

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlertRenderLogic.cs` — internal variant validation, palette, metric, and layout logic; no handle ownership and no GDI resource ownership.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlert.cs` — public Alert contract, private native dismiss button, text/icon painting, dismissal semantics, theme/font/DPI lifecycle, accessibility, and Designer metadata.

### Create tests

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertRenderLogicTests.cs` — pure palette/metric/layout tests across themes, variants, states, widths, and DPI values.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertTests.cs` — STA public contract, validation, close-button/native keyboard path, dismissal events, theme/font/DPI lifecycle, accessibility, paint smoke, and disposal tests.

### Modify shared Feedback demo created by Stage 1

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs` — extend the existing Badge feedback page with Alert scenarios; do not create another top-level form.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs` — extend existing smoke/coverage tests with Alert instances and dismissal reset behavior.

Stage 1 is responsible for adding the single `Feedback` navigation entry to `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`; Stage 2 must verify that entry still opens the shared form and must not add a duplicate navigation item.

### Modify documentation/API baseline

- `docs/COMPONENTS.md` — add the finalized `BootstrapAlert` contract and behavior.
- `docs/TESTING.md` — add pure, STA, manual, theme, DPI, accessibility, dismissal, and lifecycle coverage for Alert.
- `README.md` — list Alert as supported inline feedback and point users to the Feedback demo.
- `docs/PACKAGE_README.md` — add package-facing Alert support and a minimal usage example.
- `CHANGELOG.md` — add the compatible Alert API under `Unreleased` without rewriting release history.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs` — intentionally approve the reviewed new exported fingerprint only after Alert contract tests are green.
- `docs/PUBLIC_API_BASELINE.md` — record the new fingerprint and exact approved `BootstrapAlert` additions.

No `docs/ARCHITECTURE.md` modification is required unless implementation discovers a contradiction. The roadmap already establishes Alert as a primitive feedback control consuming existing Theme/Rendering/Icons infrastructure and introduces no new subsystem.

---

### Task 1: Freeze pure Alert palette, metrics, and layout

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlertRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertRenderLogicTests.cs`

**Interfaces:**
- Produces internal `BootstrapAlertPalette`, `BootstrapAlertMetrics`, and `BootstrapAlertLayout` exactly as defined in this plan.
- Produces internal static methods `ValidateVariant(...)`, `ResolvePalette(...)`, `ResolveMetrics(...)`, and `CalculateLayout(...)`.
- Consumes existing `BootstrapThemeColors`, `BootstrapThemeMetrics`, `BootstrapVariant`, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`, and `CornerRadius`.
- Does not reference `BootstrapAlert`, `Control`, `Button`, or `Graphics`.

- [ ] **Step 1: Write failing variant validation tests.** Verify all eight defined `BootstrapVariant` values are accepted and an undefined value such as `(BootstrapVariant)999` throws `InvalidEnumArgumentException` or the repository-established equivalent selected for this implementation.

Use one explicit regression:

```csharp
Assert.That(
    () => BootstrapAlertRenderLogic.ValidateVariant((BootstrapVariant)999),
    Throws.TypeOf<InvalidEnumArgumentException>());
```

- [ ] **Step 2: Write failing default metric tests.** For `BootstrapThemeMetrics.Default`, 96 DPI, and `BorderRadius = -1`:

```csharp
var actual = BootstrapAlertRenderLogic.ResolveMetrics(
    BootstrapThemeMetrics.Default,
    96,
    -1);

Assert.Multiple((Action)(() =>
{
    Assert.That(actual.HorizontalPadding, Is.EqualTo(12));
    Assert.That(actual.VerticalPadding, Is.EqualTo(8));
    Assert.That(actual.ContentSpacing, Is.EqualTo(8));
    Assert.That(actual.IconSize, Is.EqualTo(16));
    Assert.That(actual.CloseButtonSize, Is.EqualTo(28));
    Assert.That(actual.BorderWidth, Is.EqualTo(1));
    Assert.That(actual.FocusBorderWidth, Is.EqualTo(2));
    Assert.That(actual.Radius, Is.EqualTo(6));
}));
```

Also cover 120/144/168/192 DPI, explicit `BorderRadius = 0`, explicit radius scaling, `BorderRadius = -2`, `dpi = 0`, and null metrics.

- [ ] **Step 3: Write failing enabled palette tests for every variant in both default Light and Dark themes.** For each variant:
  - semantic base comes from `BootstrapVariantColorResolver`;
  - surface equals 12% semantic blended into theme surface;
  - border equals 45% semantic blended into theme border;
  - foreground candidate equals 72% semantic blended into theme text;
  - candidate is used only when contrast against surface is at least `4.5`;
  - otherwise foreground falls back to `theme.Colors.Text`;
  - focus equals `theme.Colors.Focus`.

- [ ] **Step 4: Write failing disabled palette tests.** For every variant, assert disabled output is identical and equals `SurfaceSecondary`, `Border`, `MutedText`, and `Disabled`; semantic choice must not leak into disabled presentation.

- [ ] **Step 5: Write failing layout tests.** Cover these concrete cases at 96 DPI:

```text
Client 360x52, no icon, not dismissible
  => no IconBounds, no CloseBounds, TextBounds fills padded content.

Client 360x52, icon, not dismissible
  => 16px icon at left, 8px gap, remaining text.

Client 360x52, no icon, dismissible
  => 28px close slot at right, 8px gap before it.

Client 360x52, icon + dismissible
  => both reservations exist and TextBounds remains between them.

Client narrower than padding + icon + close reservations
  => every rectangle remains non-negative and contained; text may become empty.

Client 0x0
  => all rectangles empty; no exception.
```

Assert `CornerRadius` is uniform and equals the resolved radius.

- [ ] **Step 6: Run focused tests on `net8.0-windows`; verify RED because the helper does not exist.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapAlertRenderLogicTests
```

Expected: compilation/test failure references missing `BootstrapAlertRenderLogic` types/methods.

- [ ] **Step 7: Implement minimal pure logic.** Keep all helper types and constants `internal`; use `ColorUtil.Blend`, `ColorUtil.GetContrastRatio`, `BootstrapVariantColorResolver`, `DpiScaler`, and `CornerRadius` directly. Do not create a new shared color/DPI/geometry layer.

- [ ] **Step 8: Run pure tests on both targets; verify GREEN.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapAlertRenderLogicTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter BootstrapAlertRenderLogicTests
```

- [ ] **Step 9: Commit** `feat: add Alert render logic`.

### Task 2: Add the public Alert contract and designer-safe skeleton

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlert.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertTests.cs`

**Interfaces:**
- Public surface is exactly the Stage 2 contract in this plan; do not add convenience aliases.
- Consumes `BootstrapAlertRenderLogic` and existing icon/theme/render infrastructure.
- Owns one private native `Button`; does not expose it publicly.

- [ ] **Step 1: Write failing STA default/metadata tests.** Use `[Apartment(ApartmentState.STA)]` and assert:

```csharp
using var alert = new BootstrapAlert();

Assert.Multiple((Action)(() =>
{
    Assert.That(alert.Text, Is.EqualTo(string.Empty));
    Assert.That(alert.Variant, Is.EqualTo(BootstrapVariant.Primary));
    Assert.That(alert.Icon, Is.Null);
    Assert.That(alert.IconRenderer, Is.Not.Null);
    Assert.That(alert.Dismissible, Is.False);
    Assert.That(alert.BorderRadius, Is.EqualTo(-1));
    Assert.That(alert.TabStop, Is.False);
    Assert.That(alert.AccessibleRole, Is.EqualTo(AccessibleRole.Alert));
    Assert.That(alert.AccessibleDescription, Is.Not.Empty);
}));
```

Reflect `[DefaultProperty(nameof(Text))]`, `[DefaultEvent(nameof(Dismissed))]`, and the expected `DefaultValue` attributes. Construct and dispose without creating a Form or initializing application theme state.

- [ ] **Step 2: Write failing text normalization/change tests.** Assign normal, multiline, empty, and `null` text. Assert native `Text` normalization leaves `null` as `string.Empty`; `TextChanged` remains the normal inherited event and is raised once per effective change.

- [ ] **Step 3: Write failing property validation/state-preservation tests.** Verify:
  - all valid variants assign;
  - `(BootstrapVariant)999` throws and leaves the old value unchanged;
  - `BorderRadius = -1`, `0`, and positive values assign;
  - `BorderRadius = -2` throws and leaves old value unchanged;
  - `IconRenderer = null` throws and keeps the previous renderer;
  - assigning/removing `Icon` does not change `Text`, `Visible`, or `Dismissible`.

- [ ] **Step 4: Write failing private-child characterization tests.** Inspect `alert.Controls` and assert there is exactly one framework-owned native `Button`; it starts hidden/non-tabbable, has the required accessibility strings, and no framework `Panel`, `Label`, or `BootstrapButton` content region is created.

- [ ] **Step 5: Run `BootstrapAlertTests` on `net8.0-windows`; verify RED.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapAlertTests
```

- [ ] **Step 6: Implement constructor and public properties.** Configure these styles:

```csharp
SetStyle(
    ControlStyles.UserPaint |
    ControlStyles.AllPaintingInWmPaint |
    ControlStyles.OptimizedDoubleBuffer |
    ControlStyles.ResizeRedraw |
    ControlStyles.SupportsTransparentBackColor,
    true);

BackColor = Color.Transparent;
TabStop = false;
AccessibleRole = AccessibleRole.Alert;
AccessibleDescription = "Bootstrap-inspired inline alert message.";
```

Use a static default renderer and static close descriptor:

```csharp
private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();
private static readonly IconDescriptor CloseIcon = IconDescriptor.Framework(FrameworkIconGlyph.Close);
```

- [ ] **Step 7: Configure the private close button exactly once in the constructor.** Add only this owned child, wire `Click` and `Paint`, and keep it hidden/non-tabbable until `Dismissible = true`.

- [ ] **Step 8: Add repository-standard XML documentation, `Category`, `Description`, `DefaultValue`, `Browsable`, and `DesignerSerializationVisibility` attributes.** `IconRenderer` is `[Browsable(false)]` and `[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]` as in existing icon-consuming controls.

- [ ] **Step 9: Run contract tests on both targets; verify GREEN before painting/dismissal work.**

- [ ] **Step 10: Commit** `feat: add BootstrapAlert contract`.

### Task 3: Implement Alert painting, icon rendering, and DPI layout

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlert.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertTests.cs`

**Interfaces:**
- Alert paints background, border, optional icon, and text.
- Private close button paints only its glyph/focus indicator; it must not create a second Alert surface.
- Uses `BootstrapAlertRenderLogic.CalculateLayout(...)` for all child/icon/text rectangles.

- [ ] **Step 1: Write a failing layout-to-child integration test.** Create an Alert sized `360x52`, set `Dismissible = true`, call `PerformLayout()`, and assert the private button bounds equal `BootstrapAlertRenderLogic.CalculateLayout(...).CloseBounds` for the current theme/DPI.

- [ ] **Step 2: Write a failing icon-renderer spy test.** Supply a test `IIconRenderer` that records calls, set a framework icon, render with `DrawToBitmap`, and assert exactly one Alert icon render call uses the calculated `IconBounds` and resolved foreground color. With `Icon = null`, no Alert icon call occurs.

The close glyph is a separate call only when the child close button itself paints; keep assertions separated so text/icon painting and close-button painting are independently diagnosable.

- [ ] **Step 3: Write failing paint smoke tests.** Host the Alert in a temporary Form on STA, create handles, and call `DrawToBitmap` for:
  - all eight variants;
  - Light and Dark themes;
  - enabled/disabled;
  - icon/no-icon;
  - dismissible/non-dismissible;
  - short text;
  - explicit multiline text such as `"Upload failed.\r\nCheck the connection and try again."`;
  - `BorderRadius = -1`, `0`, and a positive custom radius.

Assert no exception and retain pure color/geometry expectations in `BootstrapAlertRenderLogicTests` rather than brittle full-image pixel snapshots.

- [ ] **Step 4: Implement `OnLayout`.** Resolve current theme, current DPI (`DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi`), metrics, and layout; assign `_dismissButton.Bounds = layout.CloseBounds`; set button `Visible` and `TabStop` from `Dismissible`; do not reposition arbitrary caller-added children.

- [ ] **Step 5: Implement `OnPaint`.** Use this order:
  1. resolve current theme/palette/metrics/layout;
  2. return early for non-positive surface bounds;
  3. save the previous `SmoothingMode` and use `SmoothingMode.AntiAlias` for rounded geometry;
  4. create one rounded path with `RoundedPath.Create(...)`;
  5. fill the surface with `SolidBrush(palette.Surface)`;
  6. draw the border only when `metrics.BorderWidth > 0`;
  7. render `Icon` through `_iconRenderer.TryRender(...)` when icon bounds are non-empty;
  8. draw inherited `Text` with `TextRenderer.DrawText(...)` in `layout.TextBounds` using the resolved foreground and multiline flags;
  9. restore the previous smoothing mode in `finally`.

- [ ] **Step 6: Implement close-button theme/paint behavior.** `ApplyTheme()` sets `_dismissButton.BackColor` to Alert surface and `ForeColor` to Alert foreground. `OnDismissButtonPaint` renders `CloseIcon` inside a DPI-scaled inset rectangle and draws a focus rectangle with `palette.Focus` only when the button is actually focused and focus cues are shown.

- [ ] **Step 7: Handle text/icon/radius/enabled/DPI invalidation.**
  - `OnTextChanged` -> `PerformLayout()` + `Invalidate()`.
  - `Icon` setter -> `PerformLayout()` + `Invalidate()`.
  - `Dismissible` setter -> update child visibility/tab-stop, `PerformLayout()` + `Invalidate()`.
  - `BorderRadius` setter -> `Invalidate()`.
  - `OnEnabledChanged` -> `ApplyTheme()` + child invalidation + Alert invalidation.
  - `OnDpiChangedAfterParent` -> `PerformLayout()` + child invalidation + Alert invalidation.
  - `OnSizeChanged` may rely on normal `OnLayout`; do not add an independent resize scheduler.

- [ ] **Step 8: Run paint/layout tests on both targets; verify GREEN.**

- [ ] **Step 9: Commit** `feat: render BootstrapAlert presentation`.

### Task 4: Implement one-path dismissal, native keyboard activation, and accessibility

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlert.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertTests.cs`

**Interfaces:**
- `Dismiss()` is the only dismissal state transition method.
- Child button `Click` calls `Dismiss()`.
- No timer/animation/close reason is added.

- [ ] **Step 1: Write failing programmatic dismissal tests.** Use exact event counting:

```csharp
using var alert = new BootstrapAlert { Visible = true };
var dismissed = 0;
alert.Dismissed += (_, _) => dismissed++;

alert.Dismiss();
alert.Dismiss();

Assert.That(alert.Visible, Is.False);
Assert.That(dismissed, Is.EqualTo(1));

alert.Visible = true;
alert.Dismiss();

Assert.That(alert.Visible, Is.False);
Assert.That(dismissed, Is.EqualTo(2));
```

- [ ] **Step 2: Write failing direct-visibility tests.** Setting `Visible = false` directly must not raise `Dismissed`; setting `Visible = true` must not raise it either. This distinguishes Alert dismissal semantics from generic WinForms visibility changes.

- [ ] **Step 3: Write failing disabled-programmatic test.** With `Enabled = false` and `Visible = true`, `Dismiss()` still hides and raises one event because it is an explicit programmatic command, not a user input path.

- [ ] **Step 4: Write failing close-button path test.** Set `Dismissible = true`, host the Alert in a visible STA Form, locate the private native button through `Controls.OfType<Button>().Single()`, call `PerformClick()`, and assert the same visible/event results as `Dismiss()`.

- [ ] **Step 5: Write failing focusability/accessibility tests.** Assert:
  - Alert `TabStop = false` always;
  - dismiss button `Visible=false`, `TabStop=false` when not dismissible;
  - dismiss button `Visible=true`, `TabStop=true` when dismissible;
  - parent disabled state makes the child effectively disabled through WinForms;
  - button retains native `AccessibleRole.PushButton` behavior and required accessible strings;
  - toggling `Dismissible` repeatedly does not add duplicate buttons or duplicate click handlers.

- [ ] **Step 6: Implement `Dismiss()` with one effective-transition check.** Keep it intentionally small:

```csharp
public void Dismiss()
{
    if (!Visible)
    {
        return;
    }

    Visible = false;
    Dismissed?.Invoke(this, EventArgs.Empty);
}
```

Do not add a second `_dismissed` state flag; the roadmap semantics are expressed by effective visibility and the explicit dismissal path.

- [ ] **Step 7: Make `OnDismissButtonClick` call `Dismiss()` and contain no additional visibility/event logic.** This is the regression boundary preventing mouse and keyboard dismissal from diverging.

- [ ] **Step 8: Characterize native Enter/Space behavior rather than synthesizing keys in product code.** The automated test may use `PerformClick()` as the deterministic native activation path; manual verification must Tab to the close button and activate with Enter and Space in the demo. Do not add `ProcessCmdKey` or key handlers solely to imitate behavior already owned by `Button`.

- [ ] **Step 9: Run focused dismissal/accessibility tests on both targets; verify GREEN.**

- [ ] **Step 10: Commit** `feat: add Alert dismissal behavior`.

### Task 5: Add theme/font ownership and disposal hardening

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlert.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertTests.cs`

**Interfaces:**
- Uses existing static `BootstrapThemeManager.ThemeChanged` lifecycle.
- Alert owns only theme-created fonts and the child control created by normal WinForms ownership.
- Caller-assigned `Font`, `IconDescriptor`, and `IIconRenderer` remain caller-owned.

- [ ] **Step 1: Write failing runtime theme-switch test.** Construct/host Alert under Light theme, capture resolved surface/foreground through a renderer spy or paint smoke, switch to Dark theme, and verify Alert/close button invalidate and repaint using Dark tokens without reconstructing the control or changing public state.

- [ ] **Step 2: Write failing theme-created-font test.** Verify initial Alert font matches `CurrentTheme.Typography.Body`. Switch to a theme with a different body font token and verify Alert adopts the new theme font when no caller font has been assigned.

- [ ] **Step 3: Write failing caller-font ownership test.** Assign a caller-created `Font`, switch themes, and assert the same font instance remains assigned. Dispose Alert and verify the caller can still use/dispose its own font without `ObjectDisposedException`.

- [ ] **Step 4: Write failing disposal/theme-subscription regression.** Dispose Alert, then switch `BootstrapThemeManager.CurrentTheme`; no callback should throw or touch disposed children. If existing tests use reflection/weak references for theme subscription cleanup, follow that established test pattern rather than exposing subscription state publicly.

- [ ] **Step 5: Write failing repeated lifecycle stress test.** Create, optionally host, theme-switch, toggle `Dismissible`, and dispose at least 100 Alerts in an STA loop. The automated test asserts no exceptions and no duplicate child controls; manual hardening later checks GDI/USER handle growth.

- [ ] **Step 6: Implement `ApplyThemeFont`, `DisposeThemeFont`, `OnFontChanged`, `OnThemeChanged`, and `Dispose(bool)` using the repository's existing theme-owned-font pattern.** No global cache or finalizer is added.

- [ ] **Step 7: Run `BootstrapAlertTests` completely on both targets; verify GREEN.**

- [ ] **Step 8: Commit** `fix: harden Alert theme lifecycle`.

### Task 6: Extend the shared Feedback demo with Alert scenarios

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs`

**Interfaces:**
- Reuses the single Feedback page established by Stage 1 Badge.
- Adds Alert examples only; does not create another top-level `AlertDemoForm` or another MainForm navigation entry.

- [ ] **Step 1: Write failing demo smoke tests before modifying the form.** Construct `FeedbackDemoForm` on STA and assert the control tree contains:
  - at least one existing `BootstrapBadge` from Stage 1;
  - at least eight `BootstrapAlert` instances covering all semantic variants;
  - at least one Alert with `Icon != null`;
  - at least one Alert with `Dismissible = true`;
  - at least one multiline Alert;
  - at least one disabled Alert.

- [ ] **Step 2: Add an `Alerts` section below/alongside the existing Badge section using standard WinForms layout containers.** Keep demo code application-owned; no new product API exists solely to simplify the demo.

Required visible scenarios:

```text
Primary   — short message, no icon, not dismissible
Secondary — short message
Success   — IconDescriptor.Framework(FrameworkIconGlyph.Check)
Danger    — dismissible
Warning   — long/multiline text
Info      — dismissible + icon
Light     — contrast regression
Dark      — contrast regression
Disabled  — one semantic Alert with Enabled=false
Custom radius — one Alert with BorderRadius=0 or a positive explicit radius
```

- [ ] **Step 3: Add a `Restore dismissed alerts` demo button owned by the demo form, not by `BootstrapAlert`.** The button sets the demo dismissible Alerts' `Visible = true` so users can repeatedly verify the `Dismissed` event and keyboard path. Do not add a product `Reset()` API.

- [ ] **Step 4: Add a small demo status label updated by `Dismissed` events.** Example: `"Last dismissed: Danger"`. This proves the event is useful without introducing business state into the control.

- [ ] **Step 5: Extend demo tests.** Programmatically click one dismissible Alert's native close button, verify it becomes not visible, invoke the demo restore action, and verify it is visible again. Assert the control count remains stable after several dismiss/restore cycles.

- [ ] **Step 6: Verify the existing single Feedback navigation entry still opens `FeedbackDemoForm`.** Do not modify `MainForm.cs` unless Stage 1 failed to satisfy its own roadmap requirement; the Stage 1 prerequisite gate should prevent that situation from reaching this task.

- [ ] **Step 7: Build the demo for both targets.**

```powershell
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release -f net8.0-windows
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release -f net48
```

- [ ] **Step 8: Commit** `demo: add BootstrapAlert scenarios`.

### Task 7: Document Alert behavior and deliberately update the public API baseline

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

**Interfaces:**
- Documentation must describe the implemented Stage 2 contract exactly; do not advertise Toast behavior, arbitrary content, or APIs that are not present.
- API baseline update is last, after the full Alert behavior is reviewed.

- [ ] **Step 1: Add the finalized `BootstrapAlert` section to `docs/COMPONENTS.md`.** Include public concepts and these rules:
  - inline text feedback only;
  - semantic theme-derived subtle palette;
  - optional source-neutral icon;
  - optional native close affordance;
  - one-path dismissal event semantics;
  - no auto-hide/overlay/Toast behavior;
  - Light/Dark, DPI, Designer, accessibility, and lifecycle behavior.

- [ ] **Step 2: Extend `docs/TESTING.md`.** Add Alert coverage to:
  - pure logic tests: tint palette, contrast fallback, metrics, layout;
  - STA tests: defaults, validation, dismiss event count, close `PerformClick`, direct visibility non-event, accessibility, theme/font ownership, disposal;
  - manual feedback checks: all variants, icon/no-icon, dismissible/non-dismissible, multiline, disabled, keyboard close;
  - DPI matrix: icon/close/text alignment at 100/125/150/175/200%;
  - theme matrix: Light/Dark creation and runtime switches;
  - resource checks: repeated creation/disposal with no retained theme subscription or GDI growth.

- [ ] **Step 3: Update `README.md` and `docs/PACKAGE_README.md`.** Include one minimal package-facing example:

```csharp
var alert = new BootstrapAlert
{
    Text = "Changes saved successfully.",
    Variant = BootstrapVariant.Success,
    Icon = IconDescriptor.Framework(FrameworkIconGlyph.Check),
    Dismissible = true,
    Dock = DockStyle.Top
};

alert.Dismissed += (_, _) =>
{
    // Application-owned follow-up behavior.
};
```

Do not imply `Dismissed` disposes or removes the control.

- [ ] **Step 4: Add an `Unreleased` changelog entry.** State that `BootstrapAlert` adds inline semantic feedback, optional source-neutral icon, keyboard-accessible dismissal, and theme/DPI support. Do not claim Toast/overlay/animation support.

- [ ] **Step 5: Run the public API baseline test before editing its fingerprint and verify RED.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline
```

Expected: FAIL with the actual new SHA-256 fingerprint and exported API listing.

- [ ] **Step 6: Review the exported Alert API in the failure output before approval.** Confirm the principal additions are exactly:

```text
class MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapAlert : System.Windows.Forms.UserControl
public ctor()
public property MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapVariant Variant
public property MyDmsVn.Bootstrap5WinFormUI.Icons.IconDescriptor Icon
public property MyDmsVn.Bootstrap5WinFormUI.Icons.IIconRenderer IconRenderer
public property System.Boolean Dismissible
public property System.Int32 BorderRadius
public event System.EventHandler Dismissed
public method System.Void Dismiss()
```

Also review declared protected overrides introduced by the control (`OnPaint`, `OnLayout`, `OnTextChanged`, `OnEnabledChanged`, `OnFontChanged`, `OnDpiChangedAfterParent`, `Dispose`, and any other actually declared protected member). Remove accidental convenience APIs before approving the new fingerprint.

- [ ] **Step 7: Update `ApprovedV1Fingerprint` with the reviewed actual fingerprint and update `docs/PUBLIC_API_BASELINE.md` with the same fingerprint plus exact Alert additions.** Never guess the hash in advance.

- [ ] **Step 8: Re-run baseline tests for both targets; verify GREEN.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter Phase16PublicApiBaselineTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter Phase16PublicApiBaselineTests
```

- [ ] **Step 9: Commit** `docs: document BootstrapAlert API`.

### Task 8: Run the complete Stage 2 verification gate

**Files:**
- No new source files expected.
- Fix only Stage 2 regressions discovered by this gate; do not start Stage 3 Tooltip work.

**Interfaces:**
- Stage 2 is independently shippable only after both targets, automated tests, demo, manual scenarios, docs, and API baseline are green.

- [ ] **Step 1: Build the product for both targets.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
```

Expected: 0 errors on both targets.

- [ ] **Step 2: Run focused Alert tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapAlert|FeedbackDemoForm"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapAlert|FeedbackDemoForm"
```

Expected: all focused tests pass.

- [ ] **Step 3: Run the full test suite for both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: no regression in existing Button, TextBox, Card, Pagination, theme, rendering, icon, animation, hardening, or release tests.

- [ ] **Step 4: Build the integrated demo for both targets.**

```powershell
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release -f net48
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release -f net8.0-windows
```

- [ ] **Step 5: Manual Feedback demo matrix.** Launch the integrated demo, choose **Feedback**, and verify:
  1. Badge scenarios from Stage 1 remain unchanged.
  2. All eight Alert variants are visually distinguishable in Light theme.
  3. Switch to Dark theme while the form remains open; every Alert updates in place.
  4. Success/Info icon examples stay centered and use resolved foreground color.
  5. Multiline text wraps without overlapping icon or close affordance.
  6. Disabled Alert uses neutral disabled presentation.
  7. Dismissible Alert close button is reachable by Tab.
  8. Enter dismisses the focused Alert exactly once.
  9. Restore it, focus the close button again, and Space dismisses exactly once.
  10. Clicking close follows the same event/visibility behavior.
  11. `Restore dismissed alerts` makes hidden examples visible again without recreating controls.
  12. Rapid dismiss/restore does not duplicate controls, handlers, or stale paint artifacts.

- [ ] **Step 6: Real Windows DPI matrix.** Repeat the Feedback page at:

```text
100%
125%
150%
175%
200%
```

Verify:

- border/radius scale cleanly;
- text is not clipped;
- multiline text remains usable;
- icon remains sharp/aligned;
- close glyph remains centered in its native button;
- close focus indicator remains visible;
- icon/text/close spacing does not overlap;
- explicit `BorderRadius = 0` remains square and explicit positive radius scales proportionally.

- [ ] **Step 7: Designer verification in Visual Studio.** Add `BootstrapAlert` to a WinForms form without application theme bootstrap, set `Text`, `Variant`, `Dismissible`, `BorderRadius`, and an assignable `IconDescriptor` where the Designer supports it, save/reopen, and verify construction/serialization does not throw. Confirm `IconRenderer` is not serialized by default and no animation/timer starts in Designer.

- [ ] **Step 8: Lifecycle/resource manual stress.** Repeatedly open/close the Feedback demo or create/dispose Alert-heavy forms while watching GDI/USER handles. Confirm no unbounded growth, no retained theme callbacks, and no timer count increase because Alert owns no timer.

- [ ] **Step 9: Inspect the final diff for scope.** Expected product/demo/test/docs changes are limited to Alert plus extension of the existing Feedback page and required API/docs files. Reject accidental Tooltip/Toast work, unrelated refactors, package additions, or new infrastructure.

- [ ] **Step 10: Commit any verification-only fixes** with a focused message such as `fix: harden BootstrapAlert verification` only if the gate found a real Stage 2 defect. If no fixes were needed, create no empty commit.

---

## Acceptance Checklist

Stage 2 is complete only when every item below is true:

- [ ] Stage 1 Badge prerequisite gate is green.
- [ ] `BootstrapAlert` compiles for `net48` and `net8.0-windows`.
- [ ] Public contract contains `Variant`, `Icon`, `IconRenderer`, `Dismissible`, `BorderRadius`, `Dismissed`, and `Dismiss()` with no unapproved convenience API.
- [ ] Alert derives from `UserControl` and owns only one private native dismiss `Button` as framework child UI.
- [ ] No Alert-specific semantic color table exists; one formula works for all variants.
- [ ] Enabled palette derives from semantic color + current theme surface/border/text; disabled palette uses existing neutral tokens.
- [ ] Foreground contrast fallback is tested for Light/Dark and difficult semantic colors.
- [ ] Icon rendering uses the configured source-neutral `IIconRenderer`.
- [ ] Close rendering uses `FrameworkIconGlyph.Close` through the same icon renderer.
- [ ] Alert itself is non-focusable; close button is in the tab sequence only when dismissible.
- [ ] Native close button provides mouse, Tab, Enter, and Space behavior without custom key-routing code.
- [ ] `Dismiss()` and close click share one path.
- [ ] Repeated dismissal raises no duplicate event.
- [ ] Re-show then dismiss raises exactly one new event.
- [ ] Direct `Visible=false` raises no `Dismissed` event.
- [ ] Programmatic dismissal does not dispose the control.
- [ ] Disabled Alert cannot be user-dismissed through the child button but can still be programmatically dismissed.
- [ ] Multiline text, icon/text/close rectangles, and narrow-width clamping are covered by tests.
- [ ] Theme runtime switching and caller-owned font behavior are covered.
- [ ] Theme subscription and theme-created font resources are released on disposal.
- [ ] No timer, animation scheduler, popup, overlay, or top-level window is introduced.
- [ ] Shared Feedback demo is extended; no duplicate Alert top-level demo form/navigation item is created.
- [ ] `docs/COMPONENTS.md`, `docs/TESTING.md`, `README.md`, `docs/PACKAGE_README.md`, and `CHANGELOG.md` are updated.
- [ ] Public API baseline intentionally fails, is reviewed, and is deliberately updated in both baseline test and `docs/PUBLIC_API_BASELINE.md`.
- [ ] Focused tests pass on both targets.
- [ ] Full tests pass on both targets.
- [ ] Demo builds on both targets.
- [ ] Manual Light/Dark, keyboard, dismissal, Designer, and 100/125/150/175/200% DPI checks pass.

## Explicit Non-Goals for Stage 2

Do not add any of the following while executing this plan:

- Auto-hide duration or countdown.
- Fade/slide animation.
- Toast stacking or viewport placement.
- Overlay/top-level notification host.
- Close reason enum.
- Undo/action button collection.
- Rich arbitrary child content API.
- Header/body/footer regions.
- HTML, Markdown, links, or rich text parsing.
- Per-alert custom background/foreground palette properties not present in the roadmap contract.
- A new alert-specific icon model.
- A new theme manager, color utility, DPI scaler, rounded-path helper, focus engine, or timer.
- `BootstrapButton` composition for the close affordance.
- Stage 3 Tooltip or later roadmap work.

## Recommended Commit Sequence

```text
feat: add Alert render logic
feat: add BootstrapAlert contract
feat: render BootstrapAlert presentation
feat: add Alert dismissal behavior
fix: harden Alert theme lifecycle
demo: add BootstrapAlert scenarios
docs: document BootstrapAlert API
fix: harden BootstrapAlert verification   # only when the final gate finds a defect
```

Keep each commit independently buildable/testable where practical. Do not squash away the intentional RED/GREEN review boundaries during implementation unless the repository workflow explicitly requires a single final commit.
