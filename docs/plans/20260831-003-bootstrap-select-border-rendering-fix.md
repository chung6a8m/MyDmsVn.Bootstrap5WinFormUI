# BootstrapSelect Border Rendering Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the `BootstrapSelect` visual border defects reproduced in the integrated Select demo so the closed selection shell paints validation/focus borders cleanly at supported DPI values and the open popup no longer shows a square native search-editor border colliding with the rounded overlay shell.

**Architecture:** Keep the existing `BootstrapSelect -> BootstrapOverlayDropDown -> BootstrapOverlaySurface -> BootstrapSelectDropDownContent` popup architecture and the existing owner-rendered result viewport. Harden the selection-shell paint geometry using the same border-width/inset/anti-alias strategy already proven by `BootstrapTextBox`, and replace the popup's directly bordered native `TextBox` with an internal `BootstrapTextBox`-based search wrapper that still delegates editing to one borderless native WinForms `TextBox`. Add regression-first geometry, bitmap, composition, and interaction coverage; do not introduce another overlay engine, another text-editing implementation, or any public API.

**Tech Stack:** C#, Windows Forms, GDI+/`GraphicsPath`, `SmoothingMode.AntiAlias`, `BootstrapTextBox`, existing Theme/Rendering/Overlay infrastructure, NUnit 4, STA WinForms tests, targets `net48;net8.0-windows`.

**Spec:** `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`, implemented by `docs/plans/20260829-005-bootstrap-select.md`. This is a corrective follow-up for the border defects reproduced in the integrated Select demo on 2026-08-31. `docs/BOOTSTRAP_SELECT.md` remains the current component behavior reference.

## Global Constraints

- Before product-code changes, read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT_PLAN.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/BOOTSTRAP_SELECT.md`, the relevant `docs/COMPONENTS.md` sections, and the approved BootstrapSelect spec.
- Keep the root namespace `MyDmsVn.Bootstrap5WinFormUI` and project targets `net48;net8.0-windows` unchanged.
- Do not add a NuGet dependency, custom popup window, global hook, polling loop, or second placement engine.
- Keep `BootstrapOverlayDropDown`, `BootstrapOverlaySurface`, `BootstrapOverlayAnchorTracker`, and `BootstrapOverlayPlacementEngine` as the popup/placement infrastructure.
- Preserve the real native WinForms text editor for search so caret, selection, clipboard, IME, and Vietnamese input remain native editing responsibilities.
- Do not expose the native search editor or add public `SearchBorderRadius`, `SearchPadding`, `FocusBorderWidth`, or other corrective public API.
- Preserve `BootstrapSelect.ValidationState`, `BorderRadius`, single/multiple selection, custom values, local search, async paging/retry, popup keyboard behavior, tab traversal behavior, accessibility, RTL layout, and caller-owned renderer/provider semantics.
- Validation colors keep the existing priority implemented by `BootstrapTextBoxRenderLogic.ResolveBorderColor`; this fix changes border geometry/presentation, not validation semantics.
- Focused selection shells use the theme's `FocusBorderWidth`; unfocused shells use `BorderWidth`, matching the existing Bootstrap input family.
- All border widths, radii, search insets, and search heights must come from current theme metrics and `DpiScaler`; do not introduce repeated hard-coded visual pixels when an equivalent token exists.
- All owned `Pen`, `Brush`, `GraphicsPath`, and other GDI resources must be deterministically disposed.
- Add failing regression tests before each production change.
- UI/bitmap/control tests must run STA and non-parallelizable where shared theme state or real WinForms controls are involved.
- Do not modify `docs/plans/20260829-005-bootstrap-select.md`; keep the original implementation plan historical and use this file as the corrective follow-up.
- The fix must not change the public API fingerprint.

---

## Root-Cause Model to Preserve in the Fix

### Defect A — closed `BootstrapSelect` shell

The current `BootstrapSelect.OnPaint()` uses a fixed `0.5f` path inset while the border pen width is DPI-scaled. It also draws the rounded shell without explicitly enabling anti-aliasing. At 96 DPI the `0.5f` inset happens to match a 1px stroke, but at 120/144/192 DPI the stroke becomes wider while the path remains fixed, allowing half of the stroke to approach or cross the client clipping edge. The same implementation also uses `BorderWidth` even when focused instead of the theme's `FocusBorderWidth`.

The established `BootstrapTextBox.OnPaint()` pattern is the reference behavior:

```csharp
var borderWidth = Math.Max(
    1f,
    DpiScaler.Scale(
        (float)(containsFocus
            ? theme.Metrics.FocusBorderWidth
            : theme.Metrics.BorderWidth),
        dpi));
var inset = borderWidth / 2f;
var bounds = new RectangleF(
    inset,
    inset,
    Math.Max(0f, ClientSize.Width - borderWidth),
    Math.Max(0f, ClientSize.Height - borderWidth));
```

The shell must be filled/drawn with `SmoothingMode.AntiAlias`, then the previous smoothing mode restored before leaving the paint block.

### Defect B — open popup search border

`BootstrapSelectDropDownContent` currently creates a directly hosted native search editor with:

```csharp
_searchEditor = new TextBox
{
    Dock = DockStyle.Top,
    BorderStyle = BorderStyle.FixedSingle,
    Margin = Padding.Empty
};
```

The popup outer shell is rounded and theme-painted by `BootstrapOverlaySurface`, while this native `FixedSingle` editor remains rectangular, native-colored, and flush against the popup content edge because the overlay intentionally uses `LogicalContentPadding = Padding.Empty`. The two unrelated borders visually collide, especially with an explicit owner `BorderRadius = 8`.

The replacement must remain a real native editor, but the native editor itself must be borderless and live inside the framework's themed `BootstrapTextBox` shell. The search field must also be inset from the popup outer edge so its rounded border cannot visually merge with the overlay border.

---

## Required Visual and Behavioral Contract

| Scenario | Required result |
| --- | --- |
| Neutral, unfocused Select | Theme `BorderWidth`, theme border color, complete rounded stroke inside client bounds |
| Focused Select | Theme `FocusBorderWidth`, theme focus color, complete rounded stroke inside client bounds |
| Invalid Select | Danger border color with correct normal/focus thickness and clean rounded corners |
| Valid Select | Success border color with correct normal/focus thickness and clean rounded corners |
| Disabled Select | Existing disabled palette preserved; no clipping or broken corners |
| Explicit `BorderRadius = 8` | Same logical radius applied after DPI scaling without square corner artifacts |
| Open searchable popup | Rounded outer overlay border remains visually distinct from an inset themed search field |
| Popup native editor | Exactly one native WinForms `TextBox` remains responsible for editing and has `BorderStyle.None` |
| Search disabled | Search band is hidden and results occupy the popup normally |
| Light/Dark switch while open | Overlay, search field, results, text, and borders update without reconstructing public state |
| 96/120/144/192 logical DPI | Border width, focus width, radius, search inset, and search field height scale predictably |
| Keyboard/search | Existing character input, Ctrl+A/C/V/X, Up/Down/Home/End/PageUp/PageDown, Enter, Escape, and Tab behavior remain unchanged |
| IME/Vietnamese input | Still handled by the native editor; no custom text-input pipeline is added |

---

## File Structure and Responsibilities

### Create

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderLogic.cs`
  - Pure/internal closed-shell metric calculation.
  - Owns DPI-scaled normal/focus border widths, radius, and path bounds/inset math.
  - Does not paint and does not change public API.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchTextBox.cs`
  - Internal `BootstrapTextBox` specialization for popup search.
  - Exposes only internal operations required by `BootstrapSelectDropDownContent`: focus native editor at end and append one forwarded character.
  - Keeps the actual native editor private/protected and borderless through `BootstrapTextBox`.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectRenderLogicTests.cs`
  - Pure DPI/geometry regression coverage for shell metrics.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
  - STA composition/layout/search regression coverage for the popup search field.

### Modify

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
  - Consume `BootstrapSelectRenderLogic` in `OnPaint()`.
  - Use focus-aware border thickness and anti-aliased drawing.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
  - Replace the direct `TextBox` with `BootstrapSelectSearchTextBox` inside an owned search band.
  - Use theme metrics for the inset and field height.
  - Preserve existing search events and keyboard routing.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectVisualRegressionTests.cs`
  - Add real bitmap regressions that connect the pure geometry rules to `BootstrapSelect.OnPaint()` output.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`
  - Extend only where necessary to prove forwarded printable input and search keyboard behavior still work through the new wrapper.

- `docs/BOOTSTRAP_SELECT.md`
  - Document the corrected theme/DPI shell and the themed/native search composition.

- `docs/TESTING.md`
  - Record the automated and manual regression matrix.

- `CHANGELOG.md`
  - Add an Unreleased Changed entry for the rendering hardening.

### Intentionally unchanged

- `BootstrapOverlayDropDown.cs`, `BootstrapOverlaySurface.cs`, and the shared placement engine: the root cause is not the overlay geometry engine.
- `BootstrapSelectRenderer.cs`: result/chip/text rendering is not responsible for the broken outer/search borders.
- Public `BootstrapSelect` API and `docs/PUBLIC_API_BASELINE.md`.

---

### Task 1: Freeze the Closed-Shell Geometry and Bitmap Regressions

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectRenderLogicTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectVisualRegressionTests.cs`

**Interfaces:**
- Consumes: `BootstrapThemeMetrics`, `DpiScaler`, existing `BootstrapSelect.ValidationState`, `BorderRadius`, and `DrawToBitmap()`.
- Produces: failing tests for the missing `BootstrapSelectRenderLogic` contract and focused-border thickness; bitmap coverage for visible right/bottom stroke containment.

- [ ] **Step 1: Add failing pure metric tests for 96/120/144/192 DPI**

Create `BootstrapSelectRenderLogicTests.cs` and lock the expected metric API:

```csharp
[TestCase(96, 1f, 2f, 8f)]
[TestCase(120, 1.25f, 2.5f, 10f)]
[TestCase(144, 1.5f, 3f, 12f)]
[TestCase(192, 2f, 4f, 16f)]
public void ResolveMetricsScalesBorderFocusAndExplicitRadius(
    int dpi,
    float expectedBorder,
    float expectedFocus,
    float expectedRadius)
{
    var metrics = BootstrapSelectRenderLogic.ResolveMetrics(
        new Size(340, 40),
        BootstrapThemeMetrics.Default,
        dpi,
        borderRadius: 8,
        containsFocus: false);
    var focused = BootstrapSelectRenderLogic.ResolveMetrics(
        new Size(340, 40),
        BootstrapThemeMetrics.Default,
        dpi,
        borderRadius: 8,
        containsFocus: true);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(metrics.BorderWidth, Is.EqualTo(expectedBorder).Within(0.001f));
        Assert.That(focused.BorderWidth, Is.EqualTo(expectedFocus).Within(0.001f));
        Assert.That(metrics.Radius, Is.EqualTo(expectedRadius).Within(0.001f));
    }));
}
```

This initially fails to compile because `BootstrapSelectRenderLogic` does not exist.

- [ ] **Step 2: Add the exact inset/containment contract**

```csharp
[TestCase(96)]
[TestCase(120)]
[TestCase(144)]
[TestCase(192)]
public void ResolveMetricsInsetsPathByHalfOfActualStroke(int dpi)
{
    var clientSize = new Size(340, 40);
    var metrics = BootstrapSelectRenderLogic.ResolveMetrics(
        clientSize,
        BootstrapThemeMetrics.Default,
        dpi,
        borderRadius: -1,
        containsFocus: true);

    var expectedInset = metrics.BorderWidth / 2f;

    Assert.Multiple((Action)(() =>
    {
        Assert.That(metrics.BorderBounds.Left, Is.EqualTo(expectedInset).Within(0.001f));
        Assert.That(metrics.BorderBounds.Top, Is.EqualTo(expectedInset).Within(0.001f));
        Assert.That(metrics.BorderBounds.Right, Is.EqualTo(clientSize.Width - expectedInset).Within(0.001f));
        Assert.That(metrics.BorderBounds.Bottom, Is.EqualTo(clientSize.Height - expectedInset).Within(0.001f));
    }));
}
```

Also add null metrics, non-positive DPI, `BorderRadius < -1`, and empty/tiny client-size guards so malformed input never returns negative drawable geometry.

- [ ] **Step 3: Convert `BootstrapSelectVisualRegressionTests` to STA/non-parallelizable before adding real controls**

Add:

```csharp
[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapSelectVisualRegressionTests
```

Add `using System.Threading;` and `using System.Windows.Forms;`.

- [ ] **Step 4: Add a bitmap regression for invalid rounded border containment**

Use the same color-distance strategy already established by `BootstrapAlertBorderPaintingTests`:

```csharp
[Test]
public void InvalidRoundedShellPaintsRightAndBottomBorderInsideClientBounds()
{
    var originalTheme = BootstrapThemeManager.CurrentTheme;
    try
    {
        BootstrapThemeManager.CurrentTheme =
            BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

        using var host = new Form { ClientSize = new Size(420, 120) };
        using var select = new BootstrapSelect
        {
            Bounds = new Rectangle(20, 20, 340, 40),
            BorderRadius = 8,
            ValidationState = BootstrapValidationState.Invalid
        };
        host.Controls.Add(select);
        host.CreateControl();
        select.CreateControl();

        using var bitmap = new Bitmap(select.Width, select.Height);
        select.DrawToBitmap(bitmap, select.ClientRectangle);

        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        var rightEdge = bitmap.GetPixel(select.Width - 1, select.Height / 2);
        var bottomEdge = bitmap.GetPixel(select.Width / 2, select.Height - 1);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                ColorDistanceSquared(rightEdge, colors.Danger),
                Is.LessThan(ColorDistanceSquared(rightEdge, colors.Surface)));
            Assert.That(
                ColorDistanceSquared(bottomEdge, colors.Danger),
                Is.LessThan(ColorDistanceSquared(bottomEdge, colors.Surface)));
        }));
    }
    finally
    {
        BootstrapThemeManager.CurrentTheme = originalTheme;
    }
}
```

Add one class-local `ColorDistanceSquared(Color left, Color right)` helper identical in behavior to the Alert regression test; do not move it into product code.

- [ ] **Step 5: Add a focused bitmap regression that distinguishes 2px focus thickness from the old 1px path**

Create a form, focus the Select, call `Application.DoEvents()`, draw to bitmap, and sample both the edge and the immediately inner pixel at the vertical midpoint. Compare both against `theme.Colors.Focus` versus `theme.Colors.Surface`. The second pixel must remain border-dominated at 96 DPI with the default `FocusBorderWidth = 2`.

- [ ] **Step 6: Run the new focused tests and record the expected pre-fix failures**

Run:

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectRenderLogicTests|FullyQualifiedName~BootstrapSelectVisualRegressionTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectRenderLogicTests|FullyQualifiedName~BootstrapSelectVisualRegressionTests"
```

Expected before implementation:
- render-logic tests: compile failure because the new internal helper is missing;
- focused thickness regression: fail against the current 1px-focused Select implementation;
- existing layout/RTL tests: remain conceptually unchanged.

- [ ] **Step 7: Commit regression tests**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectRenderLogicTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectVisualRegressionTests.cs
git commit -m "test: reproduce BootstrapSelect border rendering defects"
```

---

### Task 2: Fix `BootstrapSelect` Closed-Shell Paint Geometry

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderLogic.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectRenderLogicTests.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectVisualRegressionTests.cs`

**Interfaces:**
- Consumes: `BootstrapThemeMetrics`, `DpiScaler`, `BootstrapTextBoxRenderLogic.ResolveBorderColor`, `RoundedPath`.
- Produces: `BootstrapSelectRenderMetrics BootstrapSelectRenderLogic.ResolveMetrics(Size, BootstrapThemeMetrics, int, int, bool)` used directly by `BootstrapSelect.OnPaint()`.

- [ ] **Step 1: Implement the internal metric value type**

Use a small immutable type in `BootstrapSelectRenderLogic.cs`:

```csharp
internal readonly struct BootstrapSelectRenderMetrics
{
    public BootstrapSelectRenderMetrics(
        float borderWidth,
        RectangleF borderBounds,
        float radius)
    {
        BorderWidth = borderWidth;
        BorderBounds = borderBounds;
        Radius = radius;
    }

    public float BorderWidth { get; }
    public RectangleF BorderBounds { get; }
    public float Radius { get; }
}
```

- [ ] **Step 2: Implement `ResolveMetrics` with the actual stroke width as the inset source**

```csharp
internal static class BootstrapSelectRenderLogic
{
    public static BootstrapSelectRenderMetrics ResolveMetrics(
        Size clientSize,
        BootstrapThemeMetrics metrics,
        int dpi,
        int borderRadius,
        bool containsFocus)
    {
        if (metrics is null)
            throw new ArgumentNullException(nameof(metrics));
        if (dpi <= 0)
            throw new ArgumentOutOfRangeException(nameof(dpi));
        if (borderRadius < -1)
            throw new ArgumentOutOfRangeException(nameof(borderRadius));

        var logicalBorderWidth = containsFocus
            ? metrics.FocusBorderWidth
            : metrics.BorderWidth;
        var borderWidth = Math.Max(
            1f,
            DpiScaler.Scale((float)logicalBorderWidth, dpi));
        var inset = borderWidth / 2f;
        var logicalRadius = borderRadius >= 0
            ? borderRadius
            : metrics.Radius;

        return new BootstrapSelectRenderMetrics(
            borderWidth,
            new RectangleF(
                inset,
                inset,
                Math.Max(0f, clientSize.Width - borderWidth),
                Math.Max(0f, clientSize.Height - borderWidth)),
            DpiScaler.Scale((float)logicalRadius, dpi));
    }
}
```

Do not fold palette resolution or selection content layout into this helper; it is solely the shell metric boundary.

- [ ] **Step 3: Replace the fixed `0.5f` geometry in `BootstrapSelect.OnPaint()`**

Compute focus once and consume the helper:

```csharp
var containsFocus = ContainsFocus || Focused;
var metrics = BootstrapSelectRenderLogic.ResolveMetrics(
    ClientSize,
    theme.Metrics,
    dpi,
    _borderRadius,
    containsFocus);
```

Remove the fixed:

```csharp
new RectangleF(0.5f, 0.5f, ClientSize.Width - 1f, ClientSize.Height - 1f)
```

and stop scaling `theme.Metrics.BorderWidth` independently inside `OnPaint()`.

- [ ] **Step 4: Paint the shell with anti-aliasing and restore the previous graphics state**

Use:

```csharp
var graphics = e.Graphics;
var previousSmoothing = graphics.SmoothingMode;
graphics.SmoothingMode = SmoothingMode.AntiAlias;
try
{
    using var path = RoundedPath.Create(
        metrics.BorderBounds,
        new CornerRadius(metrics.Radius));
    using var background = new SolidBrush(
        Enabled ? theme.Colors.Surface : theme.Colors.SurfaceSecondary);
    using var pen = new Pen(
        BootstrapTextBoxRenderLogic.ResolveBorderColor(
            theme.Colors,
            _validationState,
            containsFocus,
            Enabled),
        metrics.BorderWidth);

    graphics.FillPath(background, path);
    graphics.DrawPath(pen, path);
}
finally
{
    graphics.SmoothingMode = previousSmoothing;
}
```

Add `using System.Drawing.Drawing2D;` to `BootstrapSelect.cs`. Keep selection/chip/clear/arrow rendering after the shell block so this change does not rewrite `IBootstrapSelectRenderer` behavior.

- [ ] **Step 5: Run the metric and bitmap regressions on both TFMs**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectRenderLogicTests|FullyQualifiedName~BootstrapSelectVisualRegressionTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectRenderLogicTests|FullyQualifiedName~BootstrapSelectVisualRegressionTests"
```

Expected: pass.

- [ ] **Step 6: Run existing validation/focus/interaction Select tests before touching popup search composition**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectTests|FullyQualifiedName~BootstrapSelectInteractionTests|FullyQualifiedName~BootstrapSelectLayoutTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectTests|FullyQualifiedName~BootstrapSelectInteractionTests|FullyQualifiedName~BootstrapSelectLayoutTests"
```

Expected: pass.

- [ ] **Step 7: Commit the shell fix**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectRenderLogicTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectVisualRegressionTests.cs
git commit -m "fix: harden BootstrapSelect shell border rendering"
```

---

### Task 3: Freeze Popup Search Composition Regressions

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`

**Interfaces:**
- Consumes: internal `BootstrapSelectDropDownContent`, current theme, existing popup/search test hooks.
- Produces: failing tests requiring a `BootstrapTextBox` search shell, one borderless native editor, an inset search band, full-width result viewport, and preserved search/keyboard behavior.

- [ ] **Step 1: Add an STA fixture for the internal popup content**

Start the new test class with:

```csharp
[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapSelectDropDownContentTests
```

Use `BootstrapThemeManager.CurrentTheme` in setup/teardown so Light/Dark changes do not leak between tests.

- [ ] **Step 2: Add a failing composition test**

After applying presentation and performing layout, require one `BootstrapTextBox` search wrapper and one native borderless editor beneath it:

```csharp
[Test]
public void SearchFieldUsesThemedWrapperWithBorderlessNativeEditor()
{
    using var content = new BootstrapSelectDropDownContent
    {
        Size = new Size(340, 180)
    };
    content.ApplyPresentation(
        new BootstrapSelectRenderer(),
        BootstrapThemeManager.CurrentTheme,
        96);
    content.PerformLayout();

    var search = Descendants(content)
        .OfType<BootstrapTextBox>()
        .Single();
    var native = Descendants(search)
        .OfType<TextBox>()
        .Single();

    Assert.That(native.BorderStyle, Is.EqualTo(BorderStyle.None));
    Assert.That(search.Left, Is.GreaterThan(0));
    Assert.That(search.Top, Is.GreaterThan(0));
    Assert.That(search.Right, Is.LessThan(content.ClientSize.Width));
}
```

Add one local recursive `Descendants(Control root)` helper to the test file.

This must fail on current code because the current search control is a direct `TextBox` with `BorderStyle.FixedSingle` and there is no `BootstrapTextBox` wrapper.

- [ ] **Step 3: Lock search inset and field height to theme metrics at supported DPI values**

For 96/120/144/192 DPI, apply presentation and assert:

```text
horizontal/vertical search inset = DpiScaler.Scale(theme.Metrics.SpacingXS, dpi)
search field height = DpiScaler.Scale(theme.Metrics.ControlHeightSmall, dpi)
search band height = field height + 2 * inset
```

Obtain the search wrapper and its immediate parent from the control tree. The result viewport must remain `DockStyle.Fill` and must not receive the search band's horizontal inset.

- [ ] **Step 4: Add `SearchEnabled = false` regression**

Set `content.SearchEnabled = false`, perform layout, and assert the search band's `Visible` state is false and the results viewport uses the available content area. Re-enable search and assert the field returns without reconstructing the content object.

- [ ] **Step 5: Preserve text/search event semantics**

Subscribe to `SearchTextChanged`, set `SearchText = "Northwind"`, and assert exactly one logical event with the same text. Call `ClearSearchSilently()` and assert the text becomes empty without raising another search event.

- [ ] **Step 6: Extend interaction coverage for forwarded printable input**

In `BootstrapSelectInteractionTests`, open a searchable Select and use the existing internal popup hooks to forward a printable character. Assert the popup remains open, `CurrentSearchTextForTest`/the existing search-text hook reflects the character, and the filtered result set changes through the same local-search pipeline. Do not add a demo-only key handler.

- [ ] **Step 7: Run the new popup-content and existing interaction tests before implementation**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectInteractionTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectInteractionTests"
```

Expected before implementation: the composition/inset tests fail; existing search behavior tests remain a guard rail.

- [ ] **Step 8: Commit popup regression tests**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs
git commit -m "test: reproduce BootstrapSelect popup search border defect"
```

---

### Task 4: Replace the Native `FixedSingle` Search Border with a Themed Search Surface

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchTextBox.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`

**Interfaces:**
- Consumes: public `BootstrapTextBox` behavior, protected native `Editor`, current theme metrics, current `SearchTextChanged`/keyboard routing contract.
- Produces: an internal search editor that keeps native editing while presenting one framework-owned rounded border inside an inset search band.

- [ ] **Step 1: Add the internal `BootstrapSelectSearchTextBox` specialization**

Use `BootstrapTextBox` rather than adding another custom input shell:

```csharp
internal sealed class BootstrapSelectSearchTextBox : BootstrapTextBox
{
    internal void FocusEditorAtEnd()
    {
        Focus();
        Editor.Focus();
        Editor.SelectionStart = Editor.TextLength;
        Editor.SelectionLength = 0;
    }

    internal void AppendCharacter(char character)
    {
        if (char.IsControl(character))
            return;

        Editor.AppendText(character.ToString());
        FocusEditorAtEnd();
    }
}
```

Do not expose `Editor` publicly. `BootstrapTextBox` already guarantees the inner native editor uses `BorderStyle.None` and forwards editing/key events through the wrapper.

- [ ] **Step 2: Add an owned search band to `BootstrapSelectDropDownContent`**

Replace the direct `_searchEditor : TextBox` field with:

```csharp
private readonly Panel _searchHost;
private readonly BootstrapSelectSearchTextBox _searchEditor;
```

Construct them as:

```csharp
_searchHost = new Panel
{
    Dock = DockStyle.Top,
    Margin = Padding.Empty,
    Padding = Padding.Empty
};
_searchEditor = new BootstrapSelectSearchTextBox
{
    Dock = DockStyle.Fill,
    Margin = Padding.Empty,
    ShowClearButton = false,
    BorderRadius = -1
};
_resultsView = new BootstrapSelectResultsView
{
    Dock = DockStyle.Fill,
    Margin = Padding.Empty
};

_searchHost.Controls.Add(_searchEditor);
Controls.Add(_resultsView);
Controls.Add(_searchHost);
```

Do not add a border to `_searchHost`; only the nested `BootstrapTextBox` paints the search-field border.

- [ ] **Step 3: Make `SearchEnabled` control the band, not only the editor**

```csharp
internal bool SearchEnabled
{
    get => _searchHost.Visible;
    set
    {
        _searchHost.Visible = value;
        PerformLayout();
    }
}
```

This prevents an empty padded strip from remaining when search is disabled.

- [ ] **Step 4: Apply theme/DPI metrics to the search band**

Inside `ApplyPresentation(...)` calculate:

```csharp
var inset = DpiScaler.Scale(theme.Metrics.SpacingXS, dpi);
var fieldHeight = DpiScaler.Scale(theme.Metrics.ControlHeightSmall, dpi);
_searchHost.Padding = new Padding(inset);
_searchHost.Height = fieldHeight + (inset * 2);
_searchHost.BackColor = theme.Colors.Surface;
_searchEditor.Font = Font;
_searchEditor.Height = fieldHeight;
```

Because `_searchEditor` is `DockStyle.Fill`, the parent height/padding determines its final field height. Keep `BorderRadius = -1` so the search shell follows the current theme radius automatically. Do not copy the owner's explicit popup radius into the nested field; the inner field is a separate visual surface.

- [ ] **Step 5: Preserve `SearchText`, silent clear, focus, and forwarded-character behavior**

Keep `SearchText` delegated to `_searchEditor.Text`. Replace native-only operations with:

```csharp
internal void FocusSearch()
{
    if (_searchHost.Visible)
        _searchEditor.FocusEditorAtEnd();
    else
        _resultsView.Focus();
}

internal void ForwardCharacter(char character)
{
    if (!_searchHost.Visible || char.IsControl(character))
        return;

    _searchEditor.AppendCharacter(character);
}
```

`ClearSearchSilently()` continues to guard `_suppressSearchChanged` and calls `_searchEditor.Clear()`.

- [ ] **Step 6: Keep keyboard routing on the wrapper `KeyDown` event**

Continue subscribing:

```csharp
_searchEditor.TextChanged += OnSearchTextChanged;
_searchEditor.KeyDown += OnSearchKeyDown;
```

The wrapper forwards its native editor key event, so the existing switch for Up/Down/Home/End/PageDown/PageUp/Enter/Escape/Tab remains unchanged. Keep the early Ctrl+A/C/V/X return so native editing owns those operations.

- [ ] **Step 7: Update preferred-size calculation to use the search band height**

Replace the old native-search `Math.Max(_searchEditor.Height, Scale(30))` calculation with:

```csharp
var searchHeight = _searchHost.Visible
    ? _searchHost.Height
    : 0;
return new Size(
    Math.Max(160, proposedSize.Width),
    searchHeight + results.Height);
```

Do not alter result row height or `MaxDropDownHeight`; `BootstrapSelectDropDownController.ComputeBounds()` continues to clamp the composed preferred height.

- [ ] **Step 8: Run popup-content, interaction, popup, and paging regressions on both TFMs**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectInteractionTests|FullyQualifiedName~BootstrapSelectPopupTests|FullyQualifiedName~BootstrapSelectPagingTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectInteractionTests|FullyQualifiedName~BootstrapSelectPopupTests|FullyQualifiedName~BootstrapSelectPagingTests"
```

Expected: pass.

- [ ] **Step 9: Run TextBox tests because BootstrapSelect now composes that primitive in the popup**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapTextBoxTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapTextBoxTests"
```

Expected: pass.

- [ ] **Step 10: Commit the popup search fix**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchTextBox.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs
git commit -m "fix: theme BootstrapSelect popup search border"
```

---

### Task 5: Theme/DPI Integration, Documentation, and Full Verification

**Files:**
- Modify: `docs/BOOTSTRAP_SELECT.md`
- Modify: `docs/TESTING.md`
- Modify: `CHANGELOG.md`
- Verify without modification: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Manual verification surface: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs`

**Interfaces:**
- Consumes: final shell metrics, final popup search composition, current integrated demo.
- Produces: documented regression contract, unchanged public API, dual-target build/test evidence, and manual visual acceptance across themes/scaling.

- [ ] **Step 1: Add Light/Dark open-popup regression coverage before final documentation**

Extend `BootstrapSelectDropDownContentTests` with a theme-switch smoke test that applies Light then Dark presentation to the same content instance and asserts:

- the search wrapper remains the same object;
- `SearchText` remains unchanged;
- the native editor remains `BorderStyle.None`;
- search-host `BackColor` changes to the new theme surface;
- the result viewport remains present and full-width beneath the search band.

Do not assert exact screenshot bytes across Windows versions.

- [ ] **Step 2: Run every BootstrapSelect test on both target frameworks**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelect"
```

Expected: all pass, including selection, matcher, provider, paging/retry, concurrency, lifecycle, accessibility, popup, interaction, layout, new render logic, new popup-content tests, and visual regressions.

- [ ] **Step 3: Verify the public API baseline did not change**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

Expected: pass with no fingerprint update.

- [ ] **Step 4: Update `docs/BOOTSTRAP_SELECT.md`**

Under **Theme, DPI, RTL, and popup placement**, explicitly document:

- closed-shell stroke bounds are inset by half of the actual DPI-scaled normal/focus border width;
- focus uses `FocusBorderWidth`, while validation continues to select the existing semantic color token;
- rounded shell painting is anti-aliased;
- searchable popups use a themed `BootstrapTextBox` shell around one borderless native WinForms editor, preserving native caret/clipboard/IME behavior;
- the search field is inset from the rounded popup shell using theme spacing.

Do not describe the internal wrapper type as public API.

- [ ] **Step 5: Update `docs/TESTING.md`**

Add BootstrapSelect rendering regression coverage to the appropriate pure/STA sections:

```text
- shell metrics at logical DPI 96/120/144/192, including normal/focus widths, radius, and half-stroke inset;
- bitmap right/bottom border containment and focused thickness;
- popup search wrapper composition with one borderless native editor;
- theme-metric search inset/height and SearchEnabled layout;
- Light/Dark open-popup re-presentation without search-state loss;
- manual 100/125/150/200% Windows scaling visual check for rounded validation/focus and popup search borders.
```

- [ ] **Step 6: Add an Unreleased Changed entry to `CHANGELOG.md`**

Add one release-facing bullet similar to:

```markdown
- Hardened `BootstrapSelect` border rendering with focus-aware DPI-scaled stroke insets and anti-aliased rounded shells, and replaced the popup's flush native `FixedSingle` search border with an inset Bootstrap-themed search surface while preserving native WinForms text editing.
```

- [ ] **Step 7: Build both product targets**

```powershell
dotnet build .\src\MyDmsVn.Bootstrap5WinFormUI\MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
dotnet build .\src\MyDmsVn.Bootstrap5WinFormUI\MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
```

Expected: zero errors.

- [ ] **Step 8: Run the complete automated suite on both TFMs**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: all tests pass.

- [ ] **Step 9: Run the integrated demo manual matrix on Windows**

Launch:

```powershell
dotnet run --project .\demo\MyDmsVn.Bootstrap5WinFormUI.Demo\MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release -f net8.0-windows
```

Open **Select** and verify the existing **Validation / explicit radius** scenario plus one normal searchable Select.

For each of Light and Dark themes:

1. Verify neutral, focused, invalid, and valid selection-shell borders have continuous right/bottom edges and smooth rounded corners.
2. Verify `BorderRadius = 8` remains visibly rounded with no square overdraw.
3. Open the popup and confirm the outer rounded border is visually separate from the inset search-field border.
4. Confirm no native square `FixedSingle` rectangle touches or visually cuts the overlay corners.
5. Type/search with normal ASCII and Vietnamese IME input.
6. Verify Up/Down, Home/End, PageUp/PageDown, Enter, Escape, Tab, Ctrl+A/C/V/X, and reopening continue to work.
7. Repeat at Windows scaling 100%, 125%, 150%, and 200% where available.
8. Move the window near monitor edges and across monitors to confirm the rendering fix did not alter flip/shift placement.

- [ ] **Step 10: Commit documentation after verification evidence is green**

```powershell
git add docs/BOOTSTRAP_SELECT.md docs/TESTING.md CHANGELOG.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs
git commit -m "docs: document BootstrapSelect border rendering regression coverage"
```

---

## Acceptance Checklist

- [ ] `BootstrapSelect` no longer uses a fixed `0.5f` inset independent of the actual stroke width.
- [ ] Focused shell thickness uses `theme.Metrics.FocusBorderWidth`; unfocused thickness uses `theme.Metrics.BorderWidth`.
- [ ] Border bounds are inset by `actualBorderWidth / 2f` at 96/120/144/192 logical DPI.
- [ ] Rounded selection-shell painting uses `SmoothingMode.AntiAlias` and restores the previous smoothing mode.
- [ ] Validation color priority remains unchanged.
- [ ] Popup search no longer uses a directly visible native `BorderStyle.FixedSingle` editor.
- [ ] Popup search contains one real native WinForms `TextBox` with `BorderStyle.None` inside a `BootstrapTextBox` shell.
- [ ] Search field is inset using `SpacingXS` and sized from `ControlHeightSmall`.
- [ ] Result viewport remains owner-rendered and full-width; no per-row child controls are introduced.
- [ ] Search disabled mode has no empty search-band gap.
- [ ] Search text, Ctrl+A/C/V/X, keyboard row navigation, Enter/Escape/Tab, IME, and forwarded printable input are preserved.
- [ ] Light/Dark switching re-themes an open popup without losing search state.
- [ ] Overlay placement/flip/shift behavior is unchanged.
- [ ] Existing caller ownership and lifecycle behavior is unchanged.
- [ ] All BootstrapSelect tests pass on `net48` and `net8.0-windows`.
- [ ] Full test suite passes on both TFMs.
- [ ] Public API baseline passes without fingerprint changes.
- [ ] Integrated demo visually passes Light/Dark and 100/125/150/200% Windows scaling checks.

## Out of Scope

This corrective plan does not redesign Select2 behavior, selection semantics, result rendering, paging, accessibility, placement, or overlay lifecycle. It does not add search clear buttons, new public styling knobs, animations, custom text parsing, or a custom IME layer. If implementation reveals an independent overlay-window clipping defect after the native `FixedSingle` collision is removed, capture that as a separate regression and corrective plan rather than expanding this fix without evidence.
