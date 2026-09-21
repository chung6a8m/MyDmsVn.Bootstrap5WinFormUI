# BootstrapTooltip Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Stage 3 — `BootstrapTooltip` as a Bootstrap-themed WinForms extender component that preserves native `ToolTip` timing, association, popup positioning, and lifecycle semantics while owner-drawing the tooltip surface with the framework theme/rendering infrastructure on both `net48` and `net8.0-windows`.

**Architecture:** `BootstrapTooltip` is a `Component` + `IExtenderProvider`, never a `Form` and never a subclass of `System.Windows.Forms.ToolTip`. It owns exactly one native `ToolTip`, forwards association and timing behavior to that instance, and handles the native `Popup`/`Draw` owner-draw pipeline. Pure palette/geometry/measurement arithmetic lives in `BootstrapTooltipRenderLogic`; runtime event handlers only adapt native WinForms data into that logic and perform scoped GDI drawing.

**Tech Stack:** C#, Windows Forms, `System.ComponentModel`, `System.Drawing`, existing `BootstrapThemeManager`, `BootstrapThemeMetrics`, `BootstrapThemeTypography`, `BootstrapVariant`, `BootstrapVariantColorResolver`, `ColorUtil`, `DpiScaler`, `CornerRadius`, `RoundedPath`, NUnit 4, multi-target `net48;net8.0-windows`.

**Spec:** `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md` — Stage 3 — BootstrapTooltip.

## Global Constraints

- Keep the root namespace `MyDmsVn.Bootstrap5WinFormUI`; public component APIs stay under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- The product and test projects must continue to compile for both `net48` and `net8.0-windows` from one shared implementation wherever practical.
- Do not remove, rename, or change semantics of any existing public/protected member.
- Treat every new public/protected member as a frozen-v1 API change. The Stage 3 API must be reviewed before updating `Phase16PublicApiBaselineTests` and `docs/PUBLIC_API_BASELINE.md`.
- Reuse the existing theme/rendering infrastructure. Do not add a second theme manager, palette system, geometry helper, DPI helper, or tooltip-specific top-level rendering framework.
- Preserve native WinForms tooltip behavior where the roadmap does not explicitly require a Bootstrap-specific behavior: association, hover timing, popup placement, active/show-always behavior, disabled-control behavior, and message-loop integration remain native `ToolTip` responsibilities.
- Do not implement explicit Top/Bottom/Left/Right placement, HTML/rich content, interactive tooltip content, a custom top-level tooltip `Form`, a custom hover scheduler, or a custom popup placement engine.
- Designer construction must be safe without application bootstrap, dependency injection, service locators, or any theme initialization beyond the existing safe default in `BootstrapThemeManager`.
- All WinForms/native component tests that instantiate `ToolTip`, `Control`, or `Form` run in STA.
- All GDI objects created by Stage 3 code (`Font`, `Brush`, `Pen`, `GraphicsPath`) are disposed in the same scope that creates them.
- Stage 3 is independently shippable only when both target frameworks, relevant tests, demo coverage, documentation, and the reviewed API baseline are green.

---

## Stage Dependency Gate

The roadmap orders Stage 3 after Stage 1 (`BootstrapBadge`) and Stage 2 (`BootstrapAlert`) and defines a shared `FeedbackDemoForm` for Badge → Alert → Tooltip → Toast.

The current `main` branch at planning time does not yet contain `BootstrapBadge`, `BootstrapAlert`, or `FeedbackDemoForm`. Therefore Stage 3 implementation must not silently invent an alternate demo page or bypass the roadmap ordering.

Before Task 1:

- [ ] Rebase/synchronize onto the branch or commit where Stage 1 and Stage 2 are complete and green.
- [ ] Confirm `BootstrapVariant` and `BootstrapVariantColorResolver` are still the shared semantic color API.
- [ ] Confirm `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs` and `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs` exist from the earlier stages. If Stage 1/2 intentionally changed the shared-demo filename, reconcile that change with the roadmap before continuing rather than creating a second feedback page.
- [ ] Run the existing suite for both targets before Stage 3 edits so any pre-existing failure is separated from Tooltip work.

Commands:

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: both commands pass before Stage 3 implementation begins.

---

## Platform Constraints Resolved During Planning

These decisions make the roadmap executable without replacing native tooltip behavior.

### 1. Owner drawing requires a non-balloon native ToolTip

Microsoft WinForms documentation states that `Draw` is raised when `OwnerDraw == true` and `IsBalloon == false`; `IsBalloon` takes precedence over owner drawing. Stage 3 therefore initializes the one native instance with:

```csharp
_nativeToolTip.OwnerDraw = true;
_nativeToolTip.IsBalloon = false;
```

Do not expose `IsBalloon` in Stage 3. A caller must not be able to switch off the Bootstrap rendering pipeline through this wrapper.

Reference:
- https://learn.microsoft.com/dotnet/api/system.windows.forms.tooltip.ownerdraw
- https://learn.microsoft.com/dotnet/api/system.windows.forms.tooltip.draw

### 2. Popup size is set only in the native Popup event

WinForms documents `PopupEventArgs.ToolTipSize` as the point where an owner-drawn tooltip can change its bounds before `Draw` occurs. Stage 3 therefore measures the current caption in `Popup` and assigns only `e.ToolTipSize`. It must not change handle-recreating native properties from inside `Popup`.

Reference:
- https://learn.microsoft.com/dotnet/api/system.windows.forms.tooltip.popup

### 3. The caller's IContainer owns the wrapper, not the inner ToolTip

`BootstrapTooltip(IContainer container)` adds `this` to the supplied container. The inner `ToolTip` is constructed without that external container and is disposed only by `BootstrapTooltip.Dispose(bool)`.

This preserves the roadmap invariant “wrapper owns exactly one native ToolTip” and prevents two independent owners from disposing/siting the same native component.

Constructor pattern:

```csharp
public BootstrapTooltip()
{
    _nativeToolTip = CreateNativeToolTip();
}

public BootstrapTooltip(IContainer container)
    : this()
{
    if (container is null)
    {
        throw new ArgumentNullException(nameof(container));
    }

    container.Add(this);
}
```

### 4. Theme changes do not require a ThemeChanged subscription

`Popup` and `Draw` resolve `BootstrapThemeManager.CurrentTheme` at event time. Consequently a runtime theme switch affects the next display/draw without rebuilding `_nativeToolTip`, reassigning captions, or subscribing the component to a static event. This is both simpler and safer for component lifetime/leak behavior.

### 5. ContentPadding uses logical 96-DPI values

The roadmap requires `ContentPadding` and explicit 96/120/144/168/192 DPI coverage but does not define the default value. Stage 3 resolves this planning gap as follows:

- Public `ContentPadding` is expressed in logical 96-DPI pixels.
- Default is `8px` horizontal and `4px` vertical, derived from existing default tokens `SpacingSM` and `SpacingXS`:

```csharp
new Padding(
    BootstrapThemeMetrics.Default.SpacingSM,
    BootstrapThemeMetrics.Default.SpacingXS,
    BootstrapThemeMetrics.Default.SpacingSM,
    BootstrapThemeMetrics.Default.SpacingXS)
```

- Runtime drawing scales each edge with `DpiScaler.Scale`.
- Negative padding edges are rejected; zero padding is valid.
- Changing the active theme does not overwrite a user-assigned `ContentPadding` value.

### 6. Tooltip text uses the theme BodySmall typography token

The wrapper has no public `Font` property in the roadmap contract. Stage 3 renders using `BootstrapThemeManager.CurrentTheme.Typography.BodySmall`. Create a short-lived `Font` from that token in the `Popup`/`Draw` scope and dispose it immediately.

### 7. Stage 3 does not invent automatic wrapping

No `MaxWidth`/wrapping property exists in the roadmap contract. Preserve caption text exactly, including explicit line breaks, and use `TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding`. Do not silently introduce an arbitrary maximum width. Long text is verified for correct measurement/no truncation; screen-edge placement remains native behavior.

---

## Public Contract to Implement

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltip.cs` with exactly the Stage 3 public surface below, plus the necessary protected `Dispose(bool)` override inherited from `Component`:

```csharp
[ProvideProperty("ToolTip", typeof(Control))]
public class BootstrapTooltip : Component, IExtenderProvider
{
    public BootstrapTooltip();
    public BootstrapTooltip(IContainer container);

    public BootstrapVariant Variant { get; set; }      // default: Dark
    public Color CustomColor { get; set; }             // default: Color.Empty
    public int BorderRadius { get; set; }               // default: -1, theme sentinel
    public Padding ContentPadding { get; set; }         // default: 8,4,8,4 logical px

    public int InitialDelay { get; set; }
    public int ReshowDelay { get; set; }
    public int AutoPopDelay { get; set; }
    public bool Active { get; set; }
    public bool ShowAlways { get; set; }

    public bool CanExtend(object extendee);
    public void SetToolTip(Control control, string caption);
    public string GetToolTip(Control control);
}
```

Do not expose the owned native `ToolTip`, `OwnerDraw`, `IsBalloon`, `Popup`, `Draw`, `AutomaticDelay`, `UseAnimation`, `UseFading`, `ToolTipTitle`, `ToolTipIcon`, `Show`, or `Hide` in Stage 3.

### Public defaults and validation

| Member | Stage 3 rule |
| --- | --- |
| `Variant` | defaults to `BootstrapVariant.Dark`; reject undefined enum values |
| `CustomColor` | defaults to `Color.Empty`; any non-empty `Color` overrides semantic background |
| `BorderRadius` | defaults to `-1`; `-1` means current theme `Metrics.Radius`; `0+` is an explicit logical radius; reject `< -1` |
| `ContentPadding` | defaults to logical `(8,4,8,4)`; reject any negative edge |
| `InitialDelay` | direct native `ToolTip` forwarding; do not cache a second state |
| `ReshowDelay` | direct native `ToolTip` forwarding; do not cache a second state |
| `AutoPopDelay` | direct native `ToolTip` forwarding; do not cache a second state |
| `Active` | direct native `ToolTip` forwarding |
| `ShowAlways` | direct native `ToolTip` forwarding |
| `CanExtend` | `true` for any `Control`; `false` for null/non-controls/the wrapper itself |
| `SetToolTip` | reject null control/caption; empty caption is forwarded and removes the association using native semantics |
| `GetToolTip` | reject null control; return the native association string |

For timing values, do not hard-code framework-default milliseconds. Constructor tests compare Stage 3 defaults against a fresh native `ToolTip` so the wrapper stays faithful to each supported runtime.

---

## Ownership and Lifetime Contract

The following invariants are part of Stage 3 acceptance, not implementation details that may drift:

1. A `BootstrapTooltip` instance creates exactly one native `ToolTip` during construction.
2. The inner instance is initialized once and is never replaced due to theme changes, new associations, timing changes, or repeated hover.
3. The wrapper owns and disposes the inner instance.
4. Controls passed to `SetToolTip` are never disposed or reparented by the wrapper.
5. `BootstrapTooltip(IContainer)` adds only the wrapper to the caller container; the caller container does not directly own the native `ToolTip`.
6. `Dispose()` is idempotent according to normal `Component` semantics; event handlers are detached before/while disposing the native instance.
7. Stage 3 does not keep a second dictionary of `Control -> string`. Native `ToolTip` remains the single source of truth for associations.
8. Stage 3 does not subscribe to static theme events, avoiding a static-reference lifetime hazard.

Recommended private skeleton:

```csharp
private readonly ToolTip _nativeToolTip;
private BootstrapVariant _variant = BootstrapVariant.Dark;
private Color _customColor = Color.Empty;
private int _borderRadius = -1;
private Padding _contentPadding = CreateDefaultContentPadding();

private static ToolTip CreateNativeToolTip()
{
    var toolTip = new ToolTip
    {
        OwnerDraw = true,
        IsBalloon = false
    };

    return toolTip;
}
```

Wire `Popup` and `Draw` exactly once in the wrapper constructor after the native instance is configured.

---

## Extender Semantics

Implement the extender surface as a thin adapter:

```csharp
public bool CanExtend(object extendee)
{
    return extendee is Control;
}

public void SetToolTip(Control control, string caption)
{
    if (control is null)
    {
        throw new ArgumentNullException(nameof(control));
    }

    if (caption is null)
    {
        throw new ArgumentNullException(nameof(caption));
    }

    _nativeToolTip.SetToolTip(control, caption);
}

public string GetToolTip(Control control)
{
    if (control is null)
    {
        throw new ArgumentNullException(nameof(control));
    }

    return _nativeToolTip.GetToolTip(control);
}
```

Do not add custom mouse-enter/leave handlers to extendee controls. Doing so would duplicate native hover scheduling and create a per-control subscription cleanup problem that the roadmap explicitly avoids.

---

## Rendering Contract

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipRenderLogic.cs` as an internal, deterministic helper. It must contain no global theme access, no native `ToolTip`, no control handles, and no long-lived GDI objects.

Use small immutable internal value types so tests can validate behavior without displaying a popup:

```csharp
internal readonly struct BootstrapTooltipPalette
{
    public BootstrapTooltipPalette(Color background, Color border, Color foreground)
    {
        Background = background;
        Border = border;
        Foreground = foreground;
    }

    public Color Background { get; }
    public Color Border { get; }
    public Color Foreground { get; }
}

internal readonly struct BootstrapTooltipRenderMetrics
{
    public BootstrapTooltipRenderMetrics(Padding padding, int borderWidth, float radius)
    {
        Padding = padding;
        BorderWidth = borderWidth;
        Radius = radius;
    }

    public Padding Padding { get; }
    public int BorderWidth { get; }
    public float Radius { get; }
}
```

Recommended helper surface:

```csharp
internal static class BootstrapTooltipRenderLogic
{
    public static BootstrapTooltipPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        Color customColor);

    public static BootstrapTooltipRenderMetrics ResolveMetrics(
        BootstrapThemeMetrics metrics,
        Padding logicalPadding,
        int logicalBorderRadius,
        int dpi);

    public static Size CalculatePopupSize(
        Size measuredTextSize,
        BootstrapTooltipRenderMetrics metrics);

    public static Rectangle CalculateTextBounds(
        Rectangle outerBounds,
        BootstrapTooltipRenderMetrics metrics);
}
```

### Palette rules

- Background = `CustomColor` when not `Color.Empty`; otherwise `BootstrapVariantColorResolver.Resolve(colors, variant)`.
- Border = current theme `colors.Border`; do not invent a second hard-coded border palette.
- Foreground = `ColorUtil.GetContrastingTextColor(background, colors.Light, colors.Dark)`.
- Unknown variants are rejected through the existing semantic resolver.
- `ResolvePalette` rejects null `colors`.

### Metric rules

- `ContentPadding` is logical 96-DPI input and is scaled edge-by-edge with `DpiScaler.Scale`.
- Border width comes from `metrics.BorderWidth` and is scaled with `DpiScaler.Scale`.
- Radius = `metrics.Radius` when `BorderRadius == -1`, otherwise the explicit non-negative logical `BorderRadius`; then scale with `DpiScaler.Scale(float, dpi)`.
- Invalid DPI continues to be rejected by `DpiScaler`; do not duplicate DPI math.
- `CalculatePopupSize` returns measured text width/height + left/right/top/bottom padding + `2 * borderWidth`, using checked/saturating-safe arithmetic rather than allowing negative overflow.
- `CalculateTextBounds` symmetrically removes border + padding from the native outer rectangle and clamps width/height to zero if a pathological tiny rectangle is supplied.

### Text measurement rule

Text measurement remains in the wrapper because it depends on a real `Font`:

```csharp
var textFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
var textSize = TextRenderer.MeasureText(caption, font, Size.Empty, textFlags);
```

Preserve explicit newlines from the caption. Do not enable ellipsis or implicit maximum-width wrapping in Stage 3.

---

## Native Popup/Draw Event Pipeline

### Popup

On `_nativeToolTip.Popup`:

1. If `e.AssociatedControl` is null, leave the native size unchanged and return.
2. Read the caption from `_nativeToolTip.GetToolTip(e.AssociatedControl)`; do not use a mirror dictionary.
3. Resolve the current theme from `BootstrapThemeManager.CurrentTheme`.
4. Resolve DPI from `e.AssociatedControl.DeviceDpi`; fall back to `DpiScaler.DefaultDpi` only if the native value is invalid/unavailable.
5. Create a short-lived `Font` from `theme.Typography.BodySmall`.
6. Measure caption text with the Stage 3 flags.
7. Resolve scaled render metrics.
8. Set `e.ToolTipSize = BootstrapTooltipRenderLogic.CalculatePopupSize(...)`.
9. Dispose the font.

Do not assign `IsBalloon`, recreate the native instance, or alter associations inside `Popup`.

### Draw

On `_nativeToolTip.Draw`:

1. Resolve the current theme and current associated-control DPI.
2. Resolve palette and metrics using the same inputs used by Popup.
3. Create the theme `BodySmall` font.
4. Save the incoming `Graphics.SmoothingMode`; set `SmoothingMode.AntiAlias` for the rounded surface and restore it in `finally`.
5. Inset the path bounds by half the scaled border width so the stroke is not clipped by `e.Bounds`.
6. Create `CornerRadius(metrics.Radius)` and `RoundedPath.Create(surfaceBounds, cornerRadius)`.
7. Fill the path with `palette.Background`.
8. If `metrics.BorderWidth > 0`, stroke the same path with `palette.Border`.
9. Calculate the text rectangle through `CalculateTextBounds`.
10. Draw `e.ToolTipText` with `TextRenderer.DrawText`, `palette.Foreground`, and `NoPrefix | NoPadding`.
11. Dispose font/brush/pen/path in the same scope.

Do not call `e.DrawBackground()` or `e.DrawText()` because those helpers would use the native `ToolTip.BackColor`/`ForeColor` rather than the Stage 3 palette contract.

---

# Task 1 — Lock the Stage 3 Contract with Failing Tests

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipRenderLogicTests.cs`
- Later modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`

- [ ] **Step 1: Add constructor/default/extender tests before production code**

Create an STA fixture:

```csharp
[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapTooltipTests
{
    [Test]
    public void DefaultsMatchStage3Contract()
    {
        using var tooltip = new BootstrapTooltip();
        using var native = new ToolTip();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tooltip.Variant, Is.EqualTo(BootstrapVariant.Dark));
            Assert.That(tooltip.CustomColor, Is.EqualTo(Color.Empty));
            Assert.That(tooltip.BorderRadius, Is.EqualTo(-1));
            Assert.That(tooltip.ContentPadding, Is.EqualTo(new Padding(8, 4, 8, 4)));
            Assert.That(tooltip.InitialDelay, Is.EqualTo(native.InitialDelay));
            Assert.That(tooltip.ReshowDelay, Is.EqualTo(native.ReshowDelay));
            Assert.That(tooltip.AutoPopDelay, Is.EqualTo(native.AutoPopDelay));
            Assert.That(tooltip.Active, Is.EqualTo(native.Active));
            Assert.That(tooltip.ShowAlways, Is.EqualTo(native.ShowAlways));
        }));
    }
}
```

Add tests for:

- parameterless construction;
- `IContainer` construction sites/adds the wrapper and container disposal disposes it;
- null `IContainer` rejected;
- `[ProvideProperty("ToolTip", typeof(Control))]` present;
- type derives `Component` and implements `IExtenderProvider`;
- `CanExtend` true for `Button`, `TextBox`, `Panel`, and `Form`;
- `CanExtend` false for `null`, `object`, and the `BootstrapTooltip` component itself;
- `SetToolTip`/`GetToolTip` round-trip;
- replacing a caption updates the native association;
- empty caption removes it;
- multiple controls keep independent captions;
- null control and null caption guards;
- undefined variant rejected;
- `BorderRadius < -1` rejected while `-1`, `0`, and positive values are accepted;
- any negative `ContentPadding` edge rejected while `Padding.Empty` is accepted.

- [ ] **Step 2: Add forwarding and ownership tests**

Add a private reflection helper inside the test file to retrieve native `ToolTip` fields only for Stage 3 invariants:

```csharp
private static ToolTip GetOwnedNativeToolTip(BootstrapTooltip tooltip)
{
    var fields = typeof(BootstrapTooltip)
        .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
        .Where(field => typeof(ToolTip).IsAssignableFrom(field.FieldType))
        .ToArray();

    Assert.That(fields, Has.Length.EqualTo(1));
    return (ToolTip)fields[0].GetValue(tooltip)!;
}
```

Test:

- exactly one inner native `ToolTip` field exists;
- `OwnerDraw == true` and `IsBalloon == false`;
- setting/getting each timing property affects that same inner instance immediately;
- `Active` and `ShowAlways` forward immediately;
- theme switches do not replace the native instance;
- adding/removing associations does not replace the native instance;
- disposing wrapper disposes native component but does not dispose any associated control;
- disposing twice is safe.

Do not assert private field names; assert the ownership invariant by type/count.

- [ ] **Step 3: Add failing pure render-logic tests**

In `BootstrapTooltipRenderLogicTests.cs`, add deterministic tests for:

Palette:

```csharp
[Test]
public void DarkVariantUsesSemanticBackgroundAndContrastingForeground()
{
    var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

    var palette = BootstrapTooltipRenderLogic.ResolvePalette(
        colors,
        BootstrapVariant.Dark,
        Color.Empty);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(palette.Background, Is.EqualTo(colors.Dark));
        Assert.That(palette.Border, Is.EqualTo(colors.Border));
        Assert.That(
            palette.Foreground,
            Is.EqualTo(ColorUtil.GetContrastingTextColor(colors.Dark, colors.Light, colors.Dark)));
    }));
}
```

Also cover every `BootstrapVariant`, custom-color override, light custom color → dark foreground, dark custom color → light foreground, null colors, invalid variant.

Metrics and popup size:

- `(8,4,8,4)` scales to `(8,4,8,4)` at 96 DPI;
- 120, 144, 168, 192 DPI use existing `DpiScaler` rounding;
- `BorderRadius == -1` uses theme `Radius`;
- explicit `BorderRadius == 0` remains square;
- explicit positive radius scales;
- border width scales;
- measured text + scaled padding + border gives exact popup size;
- multiline measured text size is treated as supplied, with no truncation;
- text bounds subtract the same border/padding used by popup sizing;
- tiny bounds clamp to non-negative width/height.

Use a `[TestCase]` matrix for required DPIs:

```csharp
[TestCase(96)]
[TestCase(120)]
[TestCase(144)]
[TestCase(168)]
[TestCase(192)]
public void MetricsScaleAtRequiredDpis(int dpi)
{
    // assert against DpiScaler.Scale rather than duplicate manual rounding
}
```

- [ ] **Step 4: Run the new tests and confirm RED**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net8.0-windows --filter "FullyQualifiedName~BootstrapTooltip"
```

Expected: compile/test failure because `BootstrapTooltip` and `BootstrapTooltipRenderLogic` do not yet exist. This is the intentional TDD red state.

- [ ] **Step 5: Commit the failing contract tests**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipRenderLogicTests.cs
git commit -m "test: define BootstrapTooltip contract"
```

---

# Task 2 — Implement the Pure Tooltip Rendering Logic

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipRenderLogic.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipRenderLogicTests.cs`

- [ ] **Step 1: Add immutable internal palette/metric value types**

Implement only the values required by the tests. Keep them `internal` so Stage 3 does not expand public API unnecessarily.

- [ ] **Step 2: Implement palette resolution using existing helpers**

Core logic:

```csharp
var background = customColor.IsEmpty
    ? BootstrapVariantColorResolver.Resolve(colors, variant)
    : customColor;

var foreground = ColorUtil.GetContrastingTextColor(
    background,
    colors.Light,
    colors.Dark);

return new BootstrapTooltipPalette(background, colors.Border, foreground);
```

Do not duplicate the semantic variant switch already present in `BootstrapVariantColorResolver`.

- [ ] **Step 3: Implement DPI-scaled metrics**

Use existing `DpiScaler` for every logical value. Resolve the radius sentinel before scaling:

```csharp
var logicalRadius = logicalBorderRadius == -1
    ? metrics.Radius
    : logicalBorderRadius;
```

Keep `ContentPadding` logical so the same public setting renders proportionally across monitor DPI changes.

- [ ] **Step 4: Implement popup-size and text-bounds arithmetic**

The helper must use one consistent definition of border/padding so measurement and drawing cannot drift.

- [ ] **Step 5: Run pure tests on both targets**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net48 --filter "FullyQualifiedName~BootstrapTooltipRenderLogicTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net8.0-windows --filter "FullyQualifiedName~BootstrapTooltipRenderLogicTests"
```

Expected: render-logic tests pass; wrapper tests remain red until Task 3.

- [ ] **Step 6: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipRenderLogicTests.cs
git commit -m "feat: add BootstrapTooltip rendering logic"
```

---

# Task 3 — Implement the Extender Wrapper and Native Forwarding

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltip.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs`

- [ ] **Step 1: Add the component/extender shell and XML documentation**

Because the product project treats CS1591 as an error, document the public class, constructors, properties, and extender methods immediately.

Start with:

```csharp
[ProvideProperty("ToolTip", typeof(Control))]
public class BootstrapTooltip : Component, IExtenderProvider
{
    private readonly ToolTip _nativeToolTip;

    public BootstrapTooltip()
    {
        _nativeToolTip = new ToolTip
        {
            OwnerDraw = true,
            IsBalloon = false
        };

        _nativeToolTip.Popup += OnNativeToolTipPopup;
        _nativeToolTip.Draw += OnNativeToolTipDraw;
    }

    public BootstrapTooltip(IContainer container)
        : this()
    {
        if (container is null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        container.Add(this);
    }
}
```

Do not pass `container` to `_nativeToolTip`.

- [ ] **Step 2: Implement appearance properties and validation**

Use backing fields only for Bootstrap visual state (`Variant`, `CustomColor`, `BorderRadius`, `ContentPadding`). Validate setters and avoid any native handle recreation.

For `Variant`, use the existing resolver to validate a candidate against the current theme before assigning it; do not create a duplicate `Enum.IsDefined` switch.

For `ContentPadding`, validate each edge is `>= 0`.

- [ ] **Step 3: Implement timing/state properties as direct native forwarding**

Example:

```csharp
public int InitialDelay
{
    get => _nativeToolTip.InitialDelay;
    set => _nativeToolTip.InitialDelay = value;
}
```

Repeat for `ReshowDelay`, `AutoPopDelay`, `Active`, and `ShowAlways` with no duplicate fields.

Do not catch/translate native `ArgumentOutOfRangeException`; direct forwarding includes native validation behavior.

- [ ] **Step 4: Implement extender API**

Use the thin adapter from the Extender Semantics section. Do not track controls in another collection.

- [ ] **Step 5: Implement deterministic disposal**

```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        _nativeToolTip.Popup -= OnNativeToolTipPopup;
        _nativeToolTip.Draw -= OnNativeToolTipDraw;
        _nativeToolTip.Dispose();
    }

    base.Dispose(disposing);
}
```

No associated control is disposed.

- [ ] **Step 6: Make wrapper/forwarding tests green before drawing code**

At this step `Popup`/`Draw` handlers may call small private methods not yet implemented or be minimal no-op adapters only if necessary to compile. Do not fake owner-draw behavior just to satisfy tests; Task 4 completes that pipeline.

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net48 --filter "FullyQualifiedName~BootstrapTooltipTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Debug -f net8.0-windows --filter "FullyQualifiedName~BootstrapTooltipTests"
```

Expected: constructor/extender/forwarding/lifecycle tests pass.

- [ ] **Step 7: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltip.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs
git commit -m "feat: add BootstrapTooltip extender wrapper"
```

---

# Task 4 — Complete Owner-Draw Popup Measurement and Painting

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltip.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipRenderLogicTests.cs`

- [ ] **Step 1: Add a private theme-font factory**

Keep it private to avoid public API growth:

```csharp
private static Font CreateFont(BootstrapFontToken token)
{
    return new Font(
        token.FontFamilyName,
        token.SizeInPoints,
        token.Style,
        GraphicsUnit.Point);
}
```

- [ ] **Step 2: Implement Popup measurement**

Use associated-control DPI and current theme every time. The event handler must not mutate global theme state or native associations.

Add a testable internal/private calculation seam only if necessary; prefer keeping deterministic arithmetic in `BootstrapTooltipRenderLogic` rather than exposing event internals.

- [ ] **Step 3: Implement Draw using scoped GDI resources**

Use `RoundedPath` + `CornerRadius`, one fill brush, optional border pen, and `TextRenderer.DrawText`.

Restore graphics state altered by the wrapper. Do not leak `SmoothingMode.AntiAlias` into native graphics after the handler returns.

- [ ] **Step 4: Add integration-oriented rendering tests without flaky hover automation**

Do not depend on cursor movement, sleep-based hover timing, or a visible desktop in CI. Test the parts that can be deterministic:

- reflect the native instance and verify owner-draw configuration;
- resolve theme/palette before and after a `BootstrapThemeManager.CurrentTheme` switch and verify the wrapper retains the same native ToolTip/associations;
- verify `BodySmall` font token can be instantiated on both targets;
- create a bitmap/graphics and invoke extracted internal drawing helper only if Stage 3 implementation introduces one; otherwise keep pixel-level tests on pure render logic and reserve native `Draw` event visual behavior for demo/manual verification;
- verify custom color changes do not alter captions or replace the native instance;
- verify border radius/padding changes do not alter native timing/associations.

Avoid reflection invocation of WinForms internal `ToolTip` event machinery; that is more brittle than the behavior being protected.

- [ ] **Step 5: Add theme-switch association preservation test**

Pattern:

```csharp
var originalTheme = BootstrapThemeManager.CurrentTheme;
try
{
    using var tooltip = new BootstrapTooltip();
    using var button = new Button();
    tooltip.SetToolTip(button, "Details");
    var nativeBefore = GetOwnedNativeToolTip(tooltip);

    BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(tooltip.GetToolTip(button), Is.EqualTo("Details"));
        Assert.That(GetOwnedNativeToolTip(tooltip), Is.SameAs(nativeBefore));
    }));
}
finally
{
    BootstrapThemeManager.CurrentTheme = originalTheme;
}
```

- [ ] **Step 6: Run all Tooltip tests on both targets**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapTooltip"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapTooltip"
```

Expected: all Stage 3 control/render tests pass.

- [ ] **Step 7: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltip.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipRenderLogicTests.cs
git commit -m "feat: owner draw BootstrapTooltip"
```

---

# Task 5 — Extend the Shared Feedback Demo

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs`
- Verify, normally no change: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`

The shared demo page should already exist from Stage 1/2. Do not create a separate Tooltip demo form unless the roadmap itself is intentionally revised.

- [ ] **Step 1: Add failing demo-structure tests**

Require the Feedback page to expose anchors for at least:

- default Dark tooltip;
- semantic variant tooltip;
- custom-color tooltip;
- multiline/long-text tooltip;
- live timing example;
- controls demonstrating multiple associations from the same `BootstrapTooltip` instance.

Also assert the form owns/disposes its `BootstrapTooltip` component through a `components` container or explicit deterministic disposal.

- [ ] **Step 2: Add BootstrapTooltip components/anchors to FeedbackDemoForm**

Use normal controls such as `BootstrapButton`, `Label`, or `TextBox` as anchors. Keep demo labels explicit about behavior.

Recommended scenarios:

```text
Default: Dark tooltip
Variant: Primary / Success / Warning
Custom color: user-defined background with auto-contrast text
Multiline: caption containing explicit newline
Timing: InitialDelay / ReshowDelay / AutoPopDelay values shown next to anchor
Multiple anchors: one BootstrapTooltip instance associated with 3+ controls
```

- [ ] **Step 3: Demonstrate runtime theme switching through the existing app theme controls**

Do not rebuild captions when theme changes. The same anchor should display with the new theme on its next hover.

- [ ] **Step 4: Verify the MainForm navigation contract**

Stage 1 should already have added one “Feedback” navigation entry. Confirm Stage 3 does not add a duplicate “Tooltip” navigation node. Only update `MainForm.cs` if earlier-stage integration left the Feedback page unreachable.

- [ ] **Step 5: Run demo tests**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~FeedbackDemoFormTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~FeedbackDemoFormTests"
```

- [ ] **Step 6: Manual demo matrix**

Run the demo and inspect:

| Scenario | Expected |
| --- | --- |
| Hover several associated controls repeatedly | native popup timing remains stable; no duplicate popup/window behavior |
| Change `InitialDelay` at runtime | next hover follows native new delay |
| Change `ReshowDelay` at runtime | moving between anchors follows native new delay |
| Change `AutoPopDelay` at runtime | displayed tooltip lifetime follows native value |
| `Active=false` | native tooltip display stops; captions remain associated |
| `ShowAlways` toggle | behavior matches native WinForms semantics |
| Light → Dark theme | next popup redraws from new theme without re-association |
| Dark → Light theme | same as above |
| Dark / semantic / custom backgrounds | foreground remains readable by contrast helper |
| Explicit multiline caption | lines render intact; size includes padding |
| Long caption | no Stage 3 truncation/ellipsis is introduced |
| 96/120/144/168/192 DPI | padding, border, and radius scale consistently |
| Disabled anchor where native ToolTip supports display | wrapper does not add contradictory custom behavior |
| Dispose feedback form | no native tooltip survives through wrapper ownership |

- [ ] **Step 7: Commit**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs
git commit -m "demo: showcase BootstrapTooltip"
```

If `MainForm.cs` is unchanged, omit it from `git add`.

---

# Task 6 — Documentation and Deliberate Public API Baseline Review

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/PUBLIC_API_BASELINE.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`

- [ ] **Step 1: Document the component contract**

`docs/COMPONENTS.md` must describe:

- `BootstrapTooltip` is a nonvisual extender component;
- it wraps exactly one native `ToolTip` rather than inheriting it;
- designer/runtime `SetToolTip`/`GetToolTip` use;
- visual properties and defaults;
- timing/state properties are native forwarding;
- owner-draw theme behavior and automatic contrast foreground;
- logical 96-DPI `ContentPadding`/`BorderRadius` semantics;
- lifetime/ownership responsibility;
- out-of-scope placement/rich/interactive content.

Include a minimal usage example:

```csharp
using var tooltip = new BootstrapTooltip
{
    Variant = BootstrapVariant.Dark,
    ContentPadding = new Padding(8, 4, 8, 4)
};

tooltip.SetToolTip(saveButton, "Save the current document");
```

For designer usage, explain that `BootstrapTooltip` appears as a component and provides a `ToolTip` extender property to controls.

- [ ] **Step 2: Update testing documentation**

`docs/TESTING.md` must add Stage 3 coverage categories:

- extender contract;
- native forwarding;
- ownership/disposal;
- pure palette/geometry measurement;
- required DPI matrix;
- theme switching without association recreation;
- manual native hover/placement checks.

Explicitly state why cursor/sleep-based native hover tests are not CI assertions: they are OS/message-loop behavior preserved by composition and are covered by manual demo smoke testing.

- [ ] **Step 3: Update user-facing README/package docs and changelog**

Add `BootstrapTooltip` to supported feedback components and document its high-level value without overstating unsupported Bootstrap JS placement features.

- [ ] **Step 4: Intentionally run the release API baseline before changing it**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline"
```

Expected: FAIL and print the actual normalized API plus fingerprint because Stage 3 adds a public exported type/member surface.

Do not immediately copy the hash. First review the printed Stage 3 API and confirm:

- type is `BootstrapTooltip : System.ComponentModel.Component`;
- only the two planned public constructors exist;
- only the planned appearance/timing/extender properties/methods are public;
- no native `ToolTip` property leaked public;
- no accidental public render-logic type leaked;
- no accidental public event or helper method leaked;
- the only protected Stage 3 declaration beyond inherited members is the required `Dispose(bool)` override;
- no existing exported member disappeared or changed signature.

- [ ] **Step 5: Update the approved fingerprint only after the review passes**

Replace `ApprovedV1Fingerprint` with the reviewed actual fingerprint generated by the test. Do not hand-calculate the hash.

- [ ] **Step 6: Update `docs/PUBLIC_API_BASELINE.md`**

Record `BootstrapTooltip` as a deliberate post-baseline API addition under the component expansion work, with its public contract summarized. Preserve the document's compatibility policy; do not imply old v1 members are mutable.

- [ ] **Step 7: Run baseline test on both targets**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

Expected: pass with the same reviewed exported API on both targets.

- [ ] **Step 8: Commit**

```powershell
git add docs/COMPONENTS.md docs/TESTING.md README.md docs/PACKAGE_README.md CHANGELOG.md docs/PUBLIC_API_BASELINE.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs
git commit -m "docs: document BootstrapTooltip"
```

---

# Task 7 — Full Cross-Target Verification and Stage Completion

**Files:**
- Verify all Stage 3 files and affected shared files.

- [ ] **Step 1: Build the product for both targets with warnings-as-errors**

```powershell
dotnet build .\src\MyDmsVn.Bootstrap5WinFormUI\MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48 --no-restore
dotnet build .\src\MyDmsVn.Bootstrap5WinFormUI\MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows --no-restore
```

If restore has not run in the worktree, omit `--no-restore` on the first build only.

Expected: zero warnings and zero errors on both targets.

- [ ] **Step 2: Run all tests on both targets**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: entire suite passes, not only Stage 3 filters.

- [ ] **Step 3: Run repository build/test entrypoints if they are part of CI**

```powershell
.\build.ps1
.\test.ps1
```

Expected: both scripts complete successfully.

- [ ] **Step 4: Inspect the diff for scope creep**

```powershell
git status --short
git diff --check
git diff --stat
git diff -- src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltip.cs
git diff -- src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipRenderLogic.cs
```

Confirm:

- no custom top-level tooltip `Form` exists;
- no per-anchor mouse-event subscriptions were introduced;
- no second caption dictionary exists;
- no new package dependency was added;
- no duplicate variant/DPI/geometry helpers were created;
- inner native ToolTip remains exactly one instance;
- direct native forwarding has no mirror timing fields;
- theme switching is draw-time resolution, not static-event subscription;
- API baseline change contains only reviewed additions.

- [ ] **Step 5: Perform final manual designer/runtime smoke test**

At minimum:

1. Drop/add `BootstrapTooltip` to a form component container.
2. Set tooltip captions on multiple controls.
3. Verify caption replacement/removal.
4. Verify Light/Dark runtime switch.
5. Verify `Variant`, `CustomColor`, `BorderRadius`, and `ContentPadding` appearance.
6. Verify timing changes.
7. Verify at least 96/144/192 DPI if all five DPI environments are not directly available; pure tests still cover 96/120/144/168/192 deterministically.
8. Close/dispose the form and ensure no tooltip window remains.

- [ ] **Step 6: Create the stage completion commit**

If Tasks 1–6 were committed incrementally and the worktree is clean, no extra commit is required. Otherwise stage all remaining intentional Stage 3 changes and use the roadmap commit message:

```powershell
git add -A
git commit -m "feat: add BootstrapTooltip"
```

Do not include unrelated Stage 4+ work.

---

## Test Matrix

| Area | net48 | net8.0-windows | Notes |
| --- | --- | --- | --- |
| Constructor + public defaults | Required | Required | STA |
| IContainer ownership | Required | Required | Wrapper is contained; inner native ToolTip is not separately contained |
| Extender CanExtend/Set/Get | Required | Required | STA |
| Caption replace/remove | Required | Required | Native source of truth |
| Multiple associated controls | Required | Required | No mirror dictionary |
| Delay forwarding | Required | Required | Compare to/set native behavior |
| Active/ShowAlways forwarding | Required | Required | Native behavior |
| One native ToolTip invariant | Required | Required | Reflection test by type/count |
| Disposal/non-ownership of controls | Required | Required | Associated controls remain undisposed |
| Variant palette | Required | Required | Pure test |
| Custom-color palette | Required | Required | Pure test |
| Foreground contrast selection | Required | Required | `ColorUtil` |
| Border/radius resolution | Required | Required | Pure test |
| DPI 96/120/144/168/192 | Required | Required | Pure deterministic matrix |
| Popup size arithmetic | Required | Required | Pure deterministic test |
| Theme switch preserves associations | Required | Required | STA |
| Theme switch preserves inner native instance | Required | Required | STA |
| OwnerDraw/IsBalloon configuration | Required | Required | STA |
| Native hover/placement | Manual | Manual | Do not use sleep/cursor CI automation |
| Feedback demo integration | Required | Required | Structural tests + manual visual smoke |
| Public API baseline | Required | Required | Same reviewed surface |

---

## File Change Summary

### Create

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltip.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTooltipRenderLogic.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTooltipRenderLogicTests.cs`

### Modify after Stage 1/2 dependency is present

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs`
- `docs/COMPONENTS.md`
- `docs/TESTING.md`
- `README.md`
- `docs/PACKAGE_README.md`
- `CHANGELOG.md`
- `docs/PUBLIC_API_BASELINE.md`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`

### Verify; modify only if prior-stage integration is incomplete

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`

### Must not require changes

- `src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj` — no new package dependency.
- `src/MyDmsVn.Bootstrap5WinFormUI/Theme/*` — reuse current theme infrastructure.
- `src/MyDmsVn.Bootstrap5WinFormUI/Rendering/DpiScaler.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Rendering/ColorUtil.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Rendering/CornerRadius.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Rendering/RoundedPath.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapVariant.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapVariantColorResolver.cs`

If implementation pressure suggests changing these shared primitives, stop and demonstrate why the Stage 3 contract cannot be met through the existing APIs before broadening scope.

---

## Explicit Non-Goals

Stage 3 does **not** implement:

- a `BootstrapTooltip : ToolTip` inheritance hierarchy;
- a custom tooltip `Form` or native-window subclass;
- Bootstrap JS/Popper placement (`top`, `bottom`, `left`, `right`, offsets, fallback placement);
- HTML parsing, Markdown, rich text, images, buttons, links, or interactive content;
- caller-controlled `IsBalloon`, tooltip icon/title, fading/animation, or manual `Show`/`Hide` wrapper APIs;
- a tooltip-specific animation engine;
- per-anchor mouse/keyboard event subscriptions;
- an accessibility focus surface independent of the associated control;
- arbitrary automatic word wrapping/max width not present in the roadmap;
- Toast or later feedback components.

---

## Stage 3 Acceptance Checklist

- [ ] `BootstrapTooltip` is a public `Component` + `IExtenderProvider`, not a `ToolTip` subclass.
- [ ] `[ProvideProperty("ToolTip", typeof(Control))]` is present and designer-safe.
- [ ] Parameterless and `IContainer` constructors work on both targets.
- [ ] The wrapper owns exactly one native `ToolTip`.
- [ ] The caller container owns only the wrapper.
- [ ] `CanExtend`, `SetToolTip`, and `GetToolTip` follow the Stage 3 contract.
- [ ] Multiple controls, caption replacement, and caption removal work.
- [ ] No separate association dictionary exists.
- [ ] `InitialDelay`, `ReshowDelay`, `AutoPopDelay`, `Active`, and `ShowAlways` forward directly to native state.
- [ ] Native instance uses `OwnerDraw=true`, `IsBalloon=false`.
- [ ] Popup size comes from measured text + DPI-scaled padding/border.
- [ ] Draw uses `RoundedPath`, `CornerRadius`, current theme, semantic/custom background, contrasting foreground, and scoped GDI resources.
- [ ] `Variant=Dark`, `CustomColor=Color.Empty`, `BorderRadius=-1`, `ContentPadding=(8,4,8,4)` defaults are locked by tests.
- [ ] 96/120/144/168/192 DPI logic is covered deterministically.
- [ ] Theme changes affect future drawing without replacing the native instance or user associations.
- [ ] The wrapper does not subscribe to static theme events.
- [ ] Associated controls are never owned/disposed by the tooltip component.
- [ ] Shared Feedback demo contains Tooltip scenarios and does not duplicate navigation.
- [ ] Both target builds pass with warnings-as-errors.
- [ ] Entire test suite passes on both targets.
- [ ] Public API baseline failure was reviewed before approving the new fingerprint.
- [ ] Documentation/changelog/package README describe supported behavior and explicit non-goals.
- [ ] No Stage 4+ scope is included.

---

## Plan Self-Review

### Roadmap coverage

- Stage 3 architecture decision: covered by wrapper/ownership/extender sections.
- Exact public contract: locked in the Public Contract section and Task 1 tests.
- One owned native ToolTip: explicit invariant + reflection test.
- Native delay/active/show-always forwarding: Task 1 + Task 3.
- OwnerDraw Popup measurement: Rendering Contract + Task 4.
- Bootstrap painting: semantic/custom color, auto contrast, radius, border, theme typography, scoped GDI resources.
- Required DPI matrix: 96/120/144/168/192 in Task 1/Test Matrix.
- Theme switching without recreating associations: explicit no-static-subscription decision + lifecycle test.
- Multiple anchors, long text, disabled/native behavior, repeated hover, timing changes, Light/Dark, DPI, disposal: demo/manual matrix.
- Both targets, docs, API baseline, commit: Tasks 6–7.

### No placeholder decisions remain

The roadmap did not explicitly define `ContentPadding` default, font role, owner-draw balloon interaction, or external-container ownership of the inner native component. This plan resolves each gap explicitly so implementation does not need to guess:

- padding = logical `(8,4,8,4)` derived from `SpacingSM`/`SpacingXS`;
- typography = current theme `BodySmall`;
- `IsBalloon=false` is fixed to preserve owner drawing;
- caller container contains wrapper; wrapper alone owns inner native ToolTip.

### Type/API consistency

- `BootstrapTooltipRenderLogic` and its value types are internal.
- No new package is required.
- Existing `BootstrapVariantColorResolver`, `ColorUtil`, `DpiScaler`, `CornerRadius`, and `RoundedPath` APIs are sufficient.
- `Dispose(bool)` is the only necessary protected override added by the public component implementation.
- Public surface stays within the roadmap contract; unsupported native ToolTip APIs remain intentionally unexposed.
