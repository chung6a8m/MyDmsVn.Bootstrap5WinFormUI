# BootstrapTabControl (Tabs) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Stage 4 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md` by adding a Bootstrap-inspired `BootstrapTabControl` that preserves native WinForms `TabControl` page hosting, selection, keyboard navigation, Designer behavior, and `TabPage` ownership while framework-painting the tab headers in `Tabs`, `Pills`, and `Underline` styles.

**Architecture:** `BootstrapTabControl : TabControl` remains the single page/selection authority. The framework sets the native control to owner-draw its headers, computes one native `ItemSize` that stays aligned with WinForms hit-testing, and paints only the header rectangles returned by the native control. A small internal `BootstrapTabControlRenderLogic` owns deterministic metric, width, palette, and paint-layout calculations; the public control owns theme/font/DPI lifecycle and delegates all page selection and keyboard behavior back to `TabControl`.

**Tech Stack:** C#, native Windows Forms, existing Theme / Rendering / Compatibility infrastructure, `BootstrapVariant`, `BootstrapVariantColorResolver`, `DpiScaler`, `RoundedPath`, `CornerRadius`, `ColorUtil`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** Stage 4 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md`, plus repository-wide constraints in `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; public control types remain under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile for both `net48` and `net8.0-windows` from one shared implementation wherever practical.
- `BootstrapTabControl` must derive directly from native `System.Windows.Forms.TabControl`.
- Native `TabPage` remains the only page type; do not create `BootstrapTabPage`, a parallel page collection, or a button-backed page model.
- Native `TabPages`, `SelectedIndex`, `SelectedTab`, `SelectedIndexChanged`, page hosting, focus, Ctrl+Tab, arrow navigation, mouse hit-testing, and Designer serialization remain authoritative.
- Owner drawing is limited to headers. Do not set global `ControlStyles.UserPaint` in a way that replaces the native `TabControl` body/page host painting path.
- Page content and caller-owned `TabPage` colors/layout remain caller-owned. Do not mutate every `TabPage.BackColor`, child control style, or page font merely to make the control look Bootstrap-like.
- `Variant` colors resolve through existing `BootstrapVariantColorResolver`; contrast text resolves through existing `ColorUtil`.
- Reuse `DpiScaler`, `RoundedPath`, `CornerRadius`, `BootstrapThemeManager`, theme typography, and theme metrics. Do not add a second theme manager, geometry helper, DPI calculator, or focus engine.
- `BorderRadius = -1` means current theme radius. Values below `-1` throw `ArgumentOutOfRangeException`.
- `TabStyle` and `Variant` reject undefined enum values with `InvalidEnumArgumentException` or the repository's established equivalent validation pattern.
- Designer construction must work without application bootstrap, DI, service locators, or pre-initialized global state.
- The control must unsubscribe from `BootstrapThemeManager.ThemeChanged` and dispose only theme-created font resources that it owns. A caller-assigned `Font` remains caller-owned.
- The component adds public/protected API after the frozen RC baseline. The API baseline must intentionally fail first, be reviewed, then receive a new approved fingerprint.
- No timer, animation, asynchronous scheduling, top-level window, P/Invoke painting hook, or external package is part of this stage.

---

## Platform Constraint Resolved During Planning

This section is an implementation planning decision informed by Microsoft WinForms documentation in addition to the repository roadmap.

Microsoft documents that `TabDrawMode.OwnerDrawFixed` is the supported owner-draw mode and that `TabControl` does not support variable tab sizes with owner drawing:

- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.tabcontrol.drawmode>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.tabdrawmode>

Microsoft also documents that `TabSizeMode.FillToRight` is intended to make each **row** fill the width and is applicable to controls with more than one row, so it is not a reliable implementation of this roadmap's normal single-row `Fill=true` contract:

- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.tabsizemode>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.tabcontrol.sizemode>

Therefore Stage 4 uses the following native-aligned sizing model:

- Framework-owned header presentation sets `DrawMode = TabDrawMode.OwnerDrawFixed` and `SizeMode = TabSizeMode.Fixed`.
- `Fill=false`: all native headers use one uniform width equal to the widest current tab's preferred header content width plus theme padding, with a theme-derived minimum width.
- `Fill=true`: all native headers use one uniform width computed from available client width divided by visible tab count, with the same minimum width floor.
- When the minimum width makes all headers wider than the available client area, native `TabControl` overflow/scroll behavior remains authoritative rather than introducing custom scrolling.
- At most `tabCount - 1` remainder pixels may remain when `ClientSize.Width` is not evenly divisible by the tab count; do not intercept mouse hit-testing merely to redistribute those pixels visually.
- The drawing code always paints the actual native `DrawItemEventArgs.Bounds`; it never paints a clickable header outside the native tab rectangle.

This decision preserves the roadmap's native-first rule and avoids a visual/header geometry model that disagrees with WinForms selection hit-testing.

---

## Stage 4 Public Contract

### Public enum

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Defines the Bootstrap-inspired visual treatment used for tab headers.
/// </summary>
public enum BootstrapTabStyle
{
    Tabs = 0,
    Pills = 1,
    Underline = 2
}
```

### Public control

```csharp
[DefaultEvent(nameof(SelectedIndexChanged))]
public class BootstrapTabControl : TabControl
{
    public BootstrapTabStyle TabStyle { get; set; }    // default Tabs
    public BootstrapVariant Variant { get; set; }      // default Primary
    public bool Fill { get; set; }                     // default false
    public int BorderRadius { get; set; }              // default -1
}
```

### Public behavior

- `TabStyle = Tabs` paints a bordered active tab with semantic-variant emphasis and muted/inactive headers.
- `TabStyle = Pills` paints the active tab as a semantic filled rounded surface with contrasting text; inactive tabs remain neutral.
- `TabStyle = Underline` paints a semantic underline and semantic active text without replacing the page body.
- `Variant = Primary` by default and affects only selected/active semantic presentation.
- `Fill=false` uses the widest preferred current header width for every tab because native owner draw is fixed-size.
- `Fill=true` divides available single-row header width across the current tab count while respecting the theme-derived minimum width.
- `BorderRadius=-1` uses `BootstrapThemeManager.CurrentTheme.Metrics.Radius`; non-negative values are logical 96-DPI pixels and scale through `DpiScaler`.
- `TabPage.Enabled=false` is rendered with disabled theme colors. Selection behavior is **not** rewritten; framework tests compare it with a plain native `TabControl` on the same target.
- Focus remains native. When the `TabControl` itself has focus, the selected header receives a theme focus indicator without intercepting keyboard commands.
- `HotTrack` remains an inherited native property. When `HotTrack=true`, inactive enabled headers may use the theme hover surface based on header hit-testing; when false, no custom hover state is applied.
- Native `ImageList` / `TabPage.ImageIndex` / `TabPage.ImageKey` header images are preserved when present. No new `IconDescriptor` property is added in Stage 4.
- Native `ShowToolTips` / `TabPage.ToolTipText` remain untouched.

### Explicitly unsupported/new scope not added here

Do not add any of the following during Stage 4:

- `BootstrapTabPage` or a framework-specific page collection.
- Close buttons, closable tabs, dirty-state indicators, badges, counters, or per-tab context menus.
- Drag reorder, detachable tabs, overflow menus, custom scrolling buttons, or virtualized tabs.
- Per-tab `BootstrapVariant` or per-tab style overrides.
- New `IconDescriptor` APIs on `TabPage`.
- Animated selection indicators.
- A parallel accessibility/focus model.
- Custom page-body/chrome rendering through Win32 messages.
- A custom vertical text engine for `Alignment.Left` / `Alignment.Right`.

The Stage 4 Bootstrap visual contract is verified for the normal `Alignment.Top`, single-row usage used by the framework demo. Inherited native presentation knobs are not removed, but this stage does not promise Bootstrap-perfect vertical/multi-row styling.

---

## Header Rendering Contract

### Theme metrics

`BootstrapTabControlRenderLogic` derives all repeated geometry from existing theme tokens:

```csharp
internal readonly struct BootstrapTabHeaderMetrics
{
    public BootstrapTabHeaderMetrics(
        int height,
        int horizontalPadding,
        int contentSpacing,
        int minimumWidth,
        int borderWidth,
        int focusBorderWidth,
        int underlineHeight,
        int radius)
    {
        Height = height;
        HorizontalPadding = horizontalPadding;
        ContentSpacing = contentSpacing;
        MinimumWidth = minimumWidth;
        BorderWidth = borderWidth;
        FocusBorderWidth = focusBorderWidth;
        UnderlineHeight = underlineHeight;
        Radius = radius;
    }

    public int Height { get; }
    public int HorizontalPadding { get; }
    public int ContentSpacing { get; }
    public int MinimumWidth { get; }
    public int BorderWidth { get; }
    public int FocusBorderWidth { get; }
    public int UnderlineHeight { get; }
    public int Radius { get; }
}
```

Use these token mappings before DPI scaling:

```text
Height             = Metrics.ControlHeight
HorizontalPadding  = Metrics.SpacingMD
ContentSpacing     = Metrics.SpacingSM
MinimumWidth       = Metrics.ControlHeightLarge + Metrics.SpacingLG
BorderWidth        = Metrics.BorderWidth
FocusBorderWidth   = Metrics.FocusBorderWidth
UnderlineHeight    = max(Metrics.FocusBorderWidth, Metrics.BorderWidth)
Radius             = BorderRadius >= 0 ? BorderRadius : Metrics.Radius
```

`ResolveMetrics(BootstrapThemeMetrics metrics, int dpi, int borderRadius)` scales every logical value with `DpiScaler.Scale` and rejects `dpi <= 0` and `borderRadius < -1`.

### Uniform item width

The pure width helper is:

```csharp
internal static int CalculateUniformItemWidth(
    int tabCount,
    int availableWidth,
    IReadOnlyList<int> preferredContentWidths,
    BootstrapTabHeaderMetrics metrics,
    bool fill)
```

Rules:

```text
tabCount == 0                  => metrics.MinimumWidth
fill == true                   => max(metrics.MinimumWidth, availableWidth / tabCount)
fill == false                  => max(metrics.MinimumWidth,
                                   max(preferredContentWidths) + 2 * metrics.HorizontalPadding)
preferredContentWidths.Count   => must equal tabCount when tabCount > 0
availableWidth                 => normalized with max(0, availableWidth)
```

Each preferred content width already includes text width and, when present, native image width plus `ContentSpacing`.

### Palette

Use one internal immutable palette:

```csharp
internal readonly struct BootstrapTabHeaderPalette
{
    public BootstrapTabHeaderPalette(
        Color background,
        Color border,
        Color foreground,
        Color accent,
        Color focus)
    {
        Background = background;
        Border = border;
        Foreground = foreground;
        Accent = accent;
        Focus = focus;
    }

    public Color Background { get; }
    public Color Border { get; }
    public Color Foreground { get; }
    public Color Accent { get; }
    public Color Focus { get; }
}
```

The pure resolver is:

```csharp
internal static BootstrapTabHeaderPalette ResolvePalette(
    BootstrapThemeColors colors,
    BootstrapVariant variant,
    BootstrapTabStyle style,
    bool selected,
    bool enabled,
    bool hovered)
```

Palette rules:

- Disabled always wins: `Background = colors.Surface`, `Foreground = colors.Disabled`, `Border = colors.Border`, `Accent = colors.Disabled`, `Focus = colors.Focus`.
- Inactive normal: `Background = colors.Surface`, `Foreground = colors.MutedText`, `Border = colors.Border`, `Accent = Color.Transparent`.
- Inactive hover when enabled: `Background = colors.Hover`, `Foreground = colors.Text`, neutral border, no semantic accent.
- Selected `Tabs`: `Background = colors.Surface`, `Foreground = semantic variant`, `Border = semantic variant`, `Accent = semantic variant`.
- Selected `Pills`: `Background = semantic variant`, `Foreground = ColorUtil.GetContrastingTextColor(semantic, colors.Light, colors.Dark)`, `Border = semantic`, `Accent = semantic`.
- Selected `Underline`: `Background = colors.Surface`, `Foreground = semantic variant`, `Border = colors.Surface`, `Accent = semantic variant`.
- `Focus = colors.Focus` for all enabled styles; rendering decides whether to paint it.

### Paint geometry

Use a pure layout record/struct rather than scattering `Rectangle.Inflate` calls through the control:

```csharp
internal readonly struct BootstrapTabHeaderLayout
{
    public BootstrapTabHeaderLayout(
        Rectangle surfaceBounds,
        Rectangle contentBounds,
        Rectangle imageBounds,
        Rectangle textBounds,
        Rectangle underlineBounds,
        Rectangle focusBounds,
        CornerRadius cornerRadius)
    {
        SurfaceBounds = surfaceBounds;
        ContentBounds = contentBounds;
        ImageBounds = imageBounds;
        TextBounds = textBounds;
        UnderlineBounds = underlineBounds;
        FocusBounds = focusBounds;
        CornerRadius = cornerRadius;
    }

    public Rectangle SurfaceBounds { get; }
    public Rectangle ContentBounds { get; }
    public Rectangle ImageBounds { get; }
    public Rectangle TextBounds { get; }
    public Rectangle UnderlineBounds { get; }
    public Rectangle FocusBounds { get; }
    public CornerRadius CornerRadius { get; }
}
```

`CalculateLayout(...)` takes the actual native header bounds, style, metrics, optional image size, and whether an image is present. It keeps all rectangles inside `DrawItemEventArgs.Bounds`, centers image + text as one content group, uses `EndEllipsis` for constrained text, and returns an empty `ImageBounds` when no native image exists.

Corner rules:

- `Tabs`: `new CornerRadius(radius, radius, 0f, 0f)` so the selected tab visually joins the page area.
- `Pills`: `new CornerRadius(radius)`.
- `Underline`: `CornerRadius.Empty`; no rounded surface is needed.

---

## File Structure

**Create product files**

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabStyle.cs` — public enum only.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabControlRenderLogic.cs` — internal metrics, width, palette, and header-layout helpers; no WinForms handle ownership.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabControl.cs` — public native-backed control, page subscriptions, owner-draw handler, theme/font/DPI lifecycle, and `Fill` sizing.

**Create tests**

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTabControlRenderLogicTests.cs` — pure metric/width/palette/layout tests.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTabControlTests.cs` — STA contract, native semantics, theme/DPI/lifecycle, drawing smoke, and interaction regressions.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/NavigationDemoFormTests.cs` — demo construction/integration smoke tests.

**Create demo**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs` — first shared Navigation page from the expansion roadmap; later Stage 7 Dropdown extends this form.

**Modify integration/docs**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs` — add one `Navigation` entry that opens `NavigationDemoForm`.
- `docs/COMPONENTS.md` — add finalized `BootstrapTabControl` contract and native-first rules.
- `docs/TESTING.md` — add pure/STA/manual Tabs coverage.
- `README.md` — list Tabs among supported controls and point to the demo.
- `docs/PACKAGE_README.md` — add package-facing Tabs support.
- `CHANGELOG.md` — add compatible Tabs API addition under `Unreleased` without rewriting release history.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs` — intentionally approve the reviewed new exported fingerprint.
- `docs/PUBLIC_API_BASELINE.md` — record the new fingerprint and exact approved Tabs additions.
- `docs/ARCHITECTURE.md` — modify only if its component dependency list needs an explicit `BootstrapTabControl -> native TabControl` native-backed edge; do not add a fake dependency on ButtonGroup.

---

### Task 1: Freeze deterministic header metrics, sizing, and palettes

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabStyle.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabControlRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTabControlRenderLogicTests.cs`

**Interfaces:**
- Produces public `BootstrapTabStyle` with exactly `Tabs`, `Pills`, and `Underline`.
- Produces internal `BootstrapTabHeaderMetrics`, `BootstrapTabHeaderPalette`, and `BootstrapTabHeaderLayout`.
- Produces internal static methods `ResolveMetrics(...)`, `CalculateUniformItemWidth(...)`, `ResolvePalette(...)`, and `CalculateLayout(...)` with the signatures in this plan.
- Consumes existing `BootstrapThemeMetrics`, `BootstrapThemeColors`, `BootstrapVariant`, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`, and `CornerRadius`.

- [ ] **Step 1: Write failing enum and metric tests.** Assert enum values are stable (`Tabs=0`, `Pills=1`, `Underline=2`). For default metrics at 96 DPI and `BorderRadius=-1`, assert:

```csharp
var metrics = BootstrapTabControlRenderLogic.ResolveMetrics(
    BootstrapThemeMetrics.Default,
    96,
    -1);

Assert.Multiple((Action)(() =>
{
    Assert.That(metrics.Height, Is.EqualTo(32));
    Assert.That(metrics.HorizontalPadding, Is.EqualTo(12));
    Assert.That(metrics.ContentSpacing, Is.EqualTo(8));
    Assert.That(metrics.MinimumWidth, Is.EqualTo(54)); // 38 + 16
    Assert.That(metrics.BorderWidth, Is.EqualTo(1));
    Assert.That(metrics.FocusBorderWidth, Is.EqualTo(2));
    Assert.That(metrics.UnderlineHeight, Is.EqualTo(2));
    Assert.That(metrics.Radius, Is.EqualTo(6));
}));
```

Also verify 120/144/168/192 DPI scaling, explicit radius scaling, `dpi <= 0` rejection through `DpiScaler`, and `BorderRadius < -1` rejection.

- [ ] **Step 2: Write failing uniform-width tests.** Cover zero tabs, one tab, three tabs, uneven division remainder, minimum-width floor, long labels, and mismatch between `tabCount` and `preferredContentWidths.Count`.

Concrete examples at 96 DPI with the default minimum width `54`:

```text
Fill=false, widths [20, 50, 80] => max(54, 80 + 24) = 104
Fill=true,  available 360, 3 tabs => 120
Fill=true,  available 150, 3 tabs => 54 minimum, total overflow 162
Fill=true,  available 361, 3 tabs => 120, one remainder pixel left native
```

- [ ] **Step 3: Write failing palette tests for Light and Dark themes.** For every `BootstrapVariant`, verify selected `Pills` uses the semantic variant as background and a contrast foreground; selected `Tabs`/`Underline` use semantic foreground/accent; inactive uses muted text; hover uses theme hover/text; disabled wins over selected/hover and uses disabled tokens.

- [ ] **Step 4: Write failing layout tests.** Cover `Tabs`, `Pills`, `Underline`, no-image, image+text, narrow bounds, zero-size bounds, radius normalization, underline inside the bottom edge, and focus rectangle containment. Assert every non-empty output rectangle is contained by the supplied native header bounds.

- [ ] **Step 5: Run the focused tests on `net8.0-windows`; verify RED because the enum/render helper do not exist.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapTabControlRenderLogicTests
```

- [ ] **Step 6: Implement the minimal pure logic.** Keep all helper types `internal`; do not reference `BootstrapTabControl` from the pure helper and do not create GDI objects in it.
- [ ] **Step 7: Run `BootstrapTabControlRenderLogicTests` for both `net8.0-windows` and `net48`; verify GREEN.**
- [ ] **Step 8: Commit** `feat: add tab header render logic`.

### Task 2: Add the public native-backed TabControl contract

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabControl.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTabControlTests.cs`

**Interfaces:**
- Public surface is exactly the Stage 4 contract in this plan; do not add convenience aliases.
- Consumes `BootstrapTabControlRenderLogic` and the native `TabControl.TabPages` collection.
- Framework-owned inherited header presentation values are `DrawMode=OwnerDrawFixed` and `SizeMode=Fixed`; page/selection members remain native.

- [ ] **Step 1: Write failing STA default/metadata tests.** Use `[Apartment(ApartmentState.STA)]` and assert:

```csharp
using var tabs = new BootstrapTabControl();

Assert.Multiple((Action)(() =>
{
    Assert.That(tabs.TabStyle, Is.EqualTo(BootstrapTabStyle.Tabs));
    Assert.That(tabs.Variant, Is.EqualTo(BootstrapVariant.Primary));
    Assert.That(tabs.Fill, Is.False);
    Assert.That(tabs.BorderRadius, Is.EqualTo(-1));
    Assert.That(tabs.DrawMode, Is.EqualTo(TabDrawMode.OwnerDrawFixed));
    Assert.That(tabs.SizeMode, Is.EqualTo(TabSizeMode.Fixed));
    Assert.That(tabs.Alignment, Is.EqualTo(TabAlignment.Top));
    Assert.That(tabs.Multiline, Is.False);
    Assert.That(tabs.TabPages, Is.SameAs(((TabControl)tabs).TabPages));
}));
```

Also reflect `[DefaultEvent(nameof(SelectedIndexChanged))]` and verify parameterless Designer-safe construction/disposal without creating a Form.

- [ ] **Step 2: Write failing validation tests.** `BorderRadius=-1` and `0` are valid; `-2` throws. Undefined `BootstrapTabStyle` and undefined `BootstrapVariant` values throw without changing the previous property value.

- [ ] **Step 3: Write failing native collection/selection tests.** Add three ordinary `TabPage` instances through `tabs.TabPages.Add`, `AddRange`, remove one, and clear the collection. Assert the same instances are hosted; `SelectedIndex`, `SelectedTab`, and `SelectedIndexChanged` behave like native `TabControl`; there is no extra framework page collection/property/type.

Use a direct event-count regression:

```csharp
using var tabs = CreateTabs("One", "Two", "Three");
var count = 0;
tabs.SelectedIndexChanged += (_, _) => count++;

tabs.SelectedIndex = 1;
tabs.SelectedIndex = 1;
tabs.SelectedTab = tabs.TabPages[2];

Assert.That(count, Is.EqualTo(2));
Assert.That(tabs.SelectedIndex, Is.EqualTo(2));
```

- [ ] **Step 4: Run `BootstrapTabControlTests` on `net8.0-windows`; verify RED.**
- [ ] **Step 5: Implement the public properties with repository-standard XML documentation, `Category`, `Description`, and `DefaultValue` attributes.** Each effective visual property change calls `Invalidate`; `Fill` also recomputes native `ItemSize`.
- [ ] **Step 6: Subscribe to `ControlAdded` / `ControlRemoved` to attach/detach `TabPage.TextChanged` and `TabPage.EnabledChanged`.** A text change recomputes native `ItemSize`; enabled changes only invalidate unless they affect native layout.
- [ ] **Step 7: Keep header layout synchronized.** Recompute native `ItemSize` when pages are added/removed, tab text changes, `Fill` changes, `Font` changes, control size changes while filling, current theme metrics change, or DPI changes after parent.
- [ ] **Step 8: Run contract/native state tests on both targets; verify GREEN.**
- [ ] **Step 9: Commit** `feat: add BootstrapTabControl contract`.

### Task 3: Implement owner-drawn headers without replacing page semantics

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabControl.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTabControlTests.cs`

**Interfaces:**
- Draws only `DrawItemEventArgs.Bounds` for each native tab header.
- Does not override `SelectedIndex`, intercept `WndProc`, synthesize mouse selection, or custom-paint page bodies.
- Uses `TextRenderer` for header text and existing `RoundedPath.Create(...)` for rounded surfaces.

- [ ] **Step 1: Write a failing drawing smoke test.** Host a `BootstrapTabControl` with three pages in a temporary Form, create handles on the STA thread, draw the control with `DrawToBitmap`, and assert no exception for every `BootstrapTabStyle` in Light and Dark themes.

The smoke test is for paint stability only; pure pixel geometry remains covered by `BootstrapTabControlRenderLogicTests` rather than brittle screenshot assertions.

- [ ] **Step 2: Write failing native-image preservation tests.** Attach a small `ImageList`, assign images through `ImageIndex` and `ImageKey`, render to bitmap, and verify the control resolves the native image without replacing the page type or throwing. Keep native images optional and do not add a new public icon API.

- [ ] **Step 3: Write failing focus/disabled/hover state tests around the pure palette selection path.** Use a test subclass only if needed to expose the current hovered index or trigger `OnDrawItem`; do not expose implementation state publicly.

- [ ] **Step 4: Implement private `DrawItem` handling.** For each valid `e.Index`:
  1. Read the actual `TabPage` and `e.Bounds`.
  2. Resolve `selected = e.Index == SelectedIndex`.
  3. Resolve `enabled = Enabled && page.Enabled`.
  4. Resolve `hovered = HotTrack && e.Index == _hotTabIndex`.
  5. Resolve theme metrics/palette/layout.
  6. Paint the style surface using `RoundedPath` only where the style needs a rounded surface.
  7. Paint optional native `ImageList` image.
  8. Paint text with `TextRenderer.DrawText`, `HorizontalCenter`, `VerticalCenter`, `SingleLine`, `EndEllipsis`, and mnemonic-compatible prefix behavior.
  9. Paint selected underline for `Underline`.
  10. Paint focus indicator only when the control is focused and this is the selected enabled tab.

- [ ] **Step 5: Keep GDI lifetime scoped.** Use `using` for `GraphicsPath`, `SolidBrush`, `Pen`, and `Graphics.Save()/Restore()` when changing smoothing/clip state. Do not retain one brush/pen per page and do not allocate bitmaps per frame.
- [ ] **Step 6: Track hover only for presentation.** On `MouseMove`, find the native tab index by testing `GetTabRect(i).Contains(e.Location)`; invalidate only the old/new header rectangles when `_hotTabIndex` changes. On `MouseLeave`, clear hover and invalidate the previous header. Never change `SelectedIndex` from hover logic.
- [ ] **Step 7: Verify `Tabs` uses top-only rounded selected geometry, `Pills` uses uniform radius, and `Underline` paints no rounded selected surface.** Do not paint outside the native header bounds to fake unequal widths.
- [ ] **Step 8: Run focused control/render tests on both frameworks; verify GREEN.**
- [ ] **Step 9: Commit** `feat: render BootstrapTabControl headers`.

### Task 4: Harden theme, font, DPI, and native interaction lifecycle

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTabControl.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTabControlTests.cs`

**Interfaces:**
- Theme lifecycle follows existing controls such as `BootstrapDataGridView`: theme-created font is owned by the control until caller assigns another font.
- Native page selection remains unmodified; interaction tests are regressions against native behavior.

- [ ] **Step 1: Write failing runtime theme-switch tests.** Construct under Light theme, capture `ItemSize`/header render palette through the pure helper, switch to Dark, and verify theme-derived colors and theme-owned font update without replacing any `TabPage` or changing `SelectedIndex`.
- [ ] **Step 2: Write failing caller-font ownership tests.** Assign a caller-created `Font`, switch theme, and assert the same caller font remains assigned. Dispose the tabs and verify the caller can still use/dispose its font independently.
- [ ] **Step 3: Write failing DPI/layout tests.** Through a test seam around `ResolveMetrics` / `ApplyHeaderItemSize`, verify the same tab set produces scaled header height/padding/radius at 96/120/144/168/192 DPI. The production path must use `DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi`.
- [ ] **Step 4: Write a Fill regression test using real native `ItemSize`.** With a 360-pixel client width and 3 pages at 96 DPI, `Fill=true` produces width 120 unless the native client width after handle creation differs; in that case assert against `BootstrapTabControlRenderLogic.CalculateUniformItemWidth(...)` using the actual `ClientSize.Width`. With a narrow client, assert the minimum width floor is preserved and native overflow remains usable.
- [ ] **Step 5: Add native interaction characterization helpers.** Define test-only `NativeTabControlProbe : TabControl` and `BootstrapTabControlProbe : BootstrapTabControl` helpers that expose protected `ProcessCmdKey(ref Message, Keys)` only inside the test assembly. Run the same page set and key sequence through both probes and compare resulting `SelectedIndex` / event counts.
- [ ] **Step 6: Cover keyboard/mouse regressions.** At minimum:
  - Ctrl+Tab and Ctrl+Shift+Tab cycle the same way as native.
  - Left/Right navigation while the tab strip has focus matches native behavior.
  - Programmatic `SelectedIndex` and `SelectedTab` changes raise the same effective event count as native.
  - Mouse selection of a header uses native hit-testing and yields one effective `SelectedIndexChanged`.
  - A disabled `TabPage` comparison uses the plain native probe as the expected behavior; the framework must not add a second selection policy.
  - `ShowToolTips` / `ToolTipText` values remain caller-owned.

- [ ] **Step 7: Implement theme/font/DPI lifecycle.** Subscribe once to `BootstrapThemeManager.ThemeChanged`; on theme changes recreate only a theme-owned font, recompute header `ItemSize`, and invalidate. Use the established `_settingThemeFont`, `_useThemeFont`, `_themeFont`, `_themeSubscribed` pattern from current controls.
- [ ] **Step 8: Dispose deterministically.** Unsubscribe theme manager, detach page `TextChanged` / `EnabledChanged` handlers, dispose the theme-owned font, and then call base disposal. Do not separately dispose caller-owned `TabPage` children outside normal WinForms parent disposal.
- [ ] **Step 9: Add a stress regression** that creates/adds/removes/renames pages and toggles `TabStyle`, `Fill`, theme, and selected index at least 100 times, then disposes the control and verifies no post-disposal theme callback or retained page event path throws.
- [ ] **Step 10: Run all `BootstrapTabControlTests` for both targets; verify GREEN.**
- [ ] **Step 11: Commit** `test: harden BootstrapTabControl lifecycle`.

### Task 5: Add the shared Navigation demo page

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/NavigationDemoFormTests.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`

**Interfaces:**
- Demo uses only public `BootstrapTabControl` / native `TabPage` APIs.
- This is the shared Navigation page specified by the component-expansion roadmap; Stage 7 later extends this form with Dropdown scenarios instead of creating a second navigation top-level page.

- [ ] **Step 1: Write a failing demo smoke test.** Construct `NavigationDemoForm` on STA, recursively find `BootstrapTabControl` instances, and assert the form demonstrates all three `BootstrapTabStyle` values and at least one `Fill=true` control.
- [ ] **Step 2: Implement a `Tabs` section** with three ordinary `TabPage` instances containing nested focusable controls (`TextBox`, `Button`, `CheckBox`) so Tab/Shift+Tab behavior inside the selected page can be manually verified.
- [ ] **Step 3: Implement a `Pills` section** with `Fill=true`, semantic `Variant=Primary`, and at least four pages so equal-width behavior is obvious during resize.
- [ ] **Step 4: Implement an `Underline` section** with a non-default semantic variant such as `Success`, one deliberately long tab caption to exercise ellipsis/overflow, and one `TabPage.Enabled=false` to expose disabled presentation while retaining native semantics.
- [ ] **Step 5: Add a native-image example** using an `ImageList` on one control and `TabPage.ImageIndex`/`ImageKey`, demonstrating preservation of native header images without a Bootstrap-specific page type.
- [ ] **Step 6: Add a small event-status label** that displays `SelectedIndexChanged` observations from the demo controls. This is demo/application code only; do not add a framework event wrapper.
- [ ] **Step 7: Modify `MainForm.ConfigurePages()`** to add one top-level `Navigation` entry with a description such as `"Tabs with native page selection, keyboard navigation, fill layout, and Bootstrap header styles."` and factory `() => new NavigationDemoForm()`.
- [ ] **Step 8: Build the demo for `net8.0-windows` and run `NavigationDemoFormTests`; verify GREEN.**
- [ ] **Step 9: Commit** `demo: add BootstrapTabControl navigation scenarios`.

### Task 6: Finalize documentation and deliberately approve the API addition

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`
- Modify only if required: `docs/ARCHITECTURE.md`

- [ ] **Step 1: Update `docs/COMPONENTS.md`.** Add a finalized `BootstrapTabControl` section containing the exact public enum/properties, native page/selection authority, the fixed owner-draw sizing decision, Fill semantics, theme/DPI behavior, native image preservation, and the explicit no-`BootstrapTabPage` rule.
- [ ] **Step 2: Update `docs/TESTING.md`.** Record pure render-logic tests, STA collection/selection/keyboard tests, theme/font/DPI/lifecycle coverage, and manual checks for Light/Dark, 100/125/150/175/200%, focus, disabled page, long captions, fill/non-fill, and native image/tooltips.
- [ ] **Step 3: Update `README.md` and `docs/PACKAGE_README.md`.** Add `BootstrapTabControl` to the supported component list and state that it keeps ordinary WinForms `TabPage` / selection APIs.
- [ ] **Step 4: Add an `Unreleased` changelog entry** describing `BootstrapTabControl` with Tabs/Pills/Underline header styles and native-backed navigation. Do not rewrite the existing release-candidate history.
- [ ] **Step 5: Update `docs/ARCHITECTURE.md` only if needed** to record `BootstrapTabControl -> native TabControl` as a native-backed control. Do **not** create a `TabControl -> ButtonGroup` dependency.
- [ ] **Step 6: Run only the API baseline test before changing the hash:**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline
```

Expected: FAIL and print `Actual fingerprint:` plus the reconstructed exported API.

- [ ] **Step 7: Review the reconstructed API line-by-line.** Intended exported additions are:
  - `MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapTabStyle` with values `Tabs`, `Pills`, `Underline`.
  - `MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapTabControl : System.Windows.Forms.TabControl`.
  - Parameterless constructor.
  - Public properties `TabStyle`, `Variant`, `Fill`, `BorderRadius`.
  - Only protected overrides that the implementation genuinely requires; private event handlers are preferred when they avoid unnecessary protected API growth.
  - No exported render-logic types.
  - No `BootstrapTabPage`.

- [ ] **Step 8: Copy the reviewed deterministic `Actual fingerprint` into `ApprovedV1Fingerprint` in `Phase16PublicApiBaselineTests.cs`; write the same value into `docs/PUBLIC_API_BASELINE.md` and note Tabs as an intentional compatible API addition after Pagination.** Keep `AssemblyVersion` at `1.0.0.0`.
- [ ] **Step 9: Run the API baseline tests on `net8.0-windows` and `net48`; verify GREEN.**
- [ ] **Step 10: Commit** `docs: finalize BootstrapTabControl contract`.

### Task 7: Complete dual-target verification and manual UI gate

**Files:**
- No new files expected; fix only BootstrapTabControl-related defects uncovered by verification.

- [ ] **Step 1: Build .NET Framework 4.8:**

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release -f net48
```

Expected: zero errors.

- [ ] **Step 2: Build .NET 8 Windows:**

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release -f net8.0-windows
```

Expected: zero errors.

- [ ] **Step 3: Run all tests for both target frameworks:**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: all tests pass.

- [ ] **Step 4: Search the implementation for prohibited architecture.** Confirm no `BootstrapTabPage`, no `BootstrapButtonGroup` dependency, no second page collection, no timer/animation, no `Task.Delay`, no `Thread.Sleep`, no P/Invoke paint hook, no custom top-level window, and no replacement selection engine.
- [ ] **Step 5: Run the integrated demo and manually verify `Tabs`, `Pills`, and `Underline`:** selected/inactive/disabled presentation; Fill true/false; long labels; resize; native images; page child controls; Light/Dark live switching; caller font; mouse hover when `HotTrack=true`; selected focus indicator; mouse click selection; Tab/Shift+Tab through page content; Left/Right and Ctrl+Tab navigation; and `SelectedIndexChanged` event count.
- [ ] **Step 6: Run manual DPI verification at Windows display scaling 100%, 125%, 150%, 175%, and 200%.** Confirm header height/padding/radius scale without clipped text, header/body overlap, or paint outside native hit rectangles.
- [ ] **Step 7: Manually stress native overflow.** Resize a control with many/long tabs until native scroll buttons appear; verify clicking/keyboard selection remains aligned with the painted headers and no custom scrolling UI appears.
- [ ] **Step 8: Repeatedly create/dispose Navigation demo forms and switch themes.** Watch for obvious unbounded GDI handles, USER handles, stale theme subscriptions, post-disposal callbacks, or exceptions from detached `TabPage` events.
- [ ] **Step 9: If verification required code changes, rerun Steps 1-8 and commit fixes as** `fix: harden BootstrapTabControl verification`. **If no fixes were required, do not create an empty commit.**

---

## Definition of Done

Stage 4 is complete only when all of the following are true:

- `BootstrapTabStyle` exports exactly `Tabs`, `Pills`, and `Underline`.
- `BootstrapTabControl` derives directly from native `TabControl` and exposes only `TabStyle`, `Variant`, `Fill`, and `BorderRadius` as new public feature properties.
- Ordinary native `TabPage` instances remain the sole page type and native `TabPages` remains the sole page collection.
- Owner-drawn header sizing stays aligned with native hit-testing through fixed native `ItemSize`; no parallel visual rectangle model exists.
- `Fill=false` and `Fill=true` behave according to the fixed-size platform decision documented in this plan.
- Native collection, selected-index/tab, event, mouse, keyboard, tooltip, image, and page-hosting behavior is preserved or explicitly characterized against a plain native `TabControl`.
- Tabs/Pills/Underline palettes use existing theme/variant/color infrastructure and work in Light/Dark modes.
- DPI scaling uses `DpiScaler` and passes 96/120/144/168/192-DPI automated/manual checks.
- Caller-assigned fonts remain caller-owned; theme-created font resources and theme/page event subscriptions are released on disposal.
- `NavigationDemoForm` exists, `MainForm` links to it, and the demo exercises all required Stage 4 states.
- Both target frameworks build and all automated tests pass.
- `docs/COMPONENTS.md`, `docs/TESTING.md`, README/package docs, changelog, and public API baseline are updated.
- The public API fingerprint change is deliberately reviewed and approved; assembly compatibility remains `1.0.0.0`.
- No duplicate Theme/Rendering/DPI/focus infrastructure, timer, animation engine, custom page model, or ButtonGroup-based tab implementation is introduced.

## Self-Review

- **Roadmap coverage:** Stage 4 architecture, contract, native `TabPage` rule, header-only painting, Fill behavior, semantic variant selection, disabled/inactive theme states, native keyboard/focus authority, demo scenarios, dual-target verification, documentation, and API-baseline review each map to explicit tasks.
- **Platform conflict resolved explicitly:** WinForms owner drawing is fixed-size, so the plan does not pretend that non-fill owner-drawn tabs can each keep independently measured widths. Uniform `ItemSize` keeps painting and native mouse hit-testing consistent.
- **Placeholder scan:** No `TBD`, `TODO`, generic “add tests”, or unspecified implementation step remains. The future API hash is intentionally obtained from the deterministic baseline test after the exported surface exists rather than guessed in advance.
- **Type consistency:** `BootstrapTabStyle`, `BootstrapVariant`, `bool Fill`, and `int BorderRadius` are used consistently. All metrics/layout/palette helpers are internal and the public page model remains `TabPage`.
- **Scope check:** Dropdown, closable/reorderable tabs, per-tab Bootstrap metadata, animation, custom vertical text, custom overflow, custom page chrome, and a framework page type remain outside Stage 4 so the stage stays independently testable and shippable.
