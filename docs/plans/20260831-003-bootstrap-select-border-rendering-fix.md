# BootstrapSelect Border Rendering Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the `BootstrapSelect` visual border defects reproduced in the integrated Select demo so the closed selection shell paints validation/focus borders cleanly at supported DPI values and the open popup no longer shows a square native search-editor border colliding with the rounded overlay shell.

**Architecture:** Keep the existing `BootstrapSelect -> BootstrapOverlayDropDown -> BootstrapOverlaySurface -> BootstrapSelectDropDownContent` popup architecture and the existing owner-rendered result viewport. Harden the selection-shell paint geometry using the same border-width/inset/anti-alias strategy already proven by `BootstrapTextBox`, and replace the popup's directly bordered native `TextBox` with an internal `BootstrapTextBox`-based search wrapper that still delegates editing to one borderless native WinForms `TextBox`. Add regression-first geometry, bitmap, composition, keyboard-dialog-routing, accessibility, and interaction coverage so the visual fix cannot regress `Tab` traversal, focus semantics, native editing, or the accessibility tree. Do not introduce another overlay engine, another text-editing implementation, or any public API.

**Tech Stack:** C#, Windows Forms, GDI+/`GraphicsPath`, `SmoothingMode.AntiAlias`, `BootstrapTextBox`, existing Theme/Rendering/Overlay infrastructure, NUnit 4, STA WinForms tests, targets `net48;net8.0-windows`.

**Spec:** `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`, implemented by `docs/plans/20260829-005-bootstrap-select.md`. This is a corrective follow-up for the border defects reproduced in the integrated Select demo on 2026-08-31. `docs/BOOTSTRAP_SELECT.md` remains the current component behavior reference.

## Global Constraints

- Before product-code changes, read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT_PLAN.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/BOOTSTRAP_SELECT.md`, the relevant `docs/COMPONENTS.md` sections, and the approved BootstrapSelect spec.
- Keep the root namespace `MyDmsVn.Bootstrap5WinFormUI` and project targets `net48;net8.0-windows` unchanged.
- Do not add a NuGet dependency, custom popup window, global hook, polling loop, or second placement engine.
- Keep `BootstrapOverlayDropDown`, `BootstrapOverlaySurface`, `BootstrapOverlayAnchorTracker`, and `BootstrapOverlayPlacementEngine` as the popup/placement infrastructure.
- Preserve the real native WinForms text editor for search so caret, selection, clipboard, IME, and Vietnamese input remain native editing responsibilities.
- Do not expose the native search editor or add public `SearchBorderRadius`, `SearchPadding`, `FocusBorderWidth`, or other corrective public API. Internal test-only inspection hooks are permitted only when required to exercise the real popup path and must not alter the public API fingerprint.
- Preserve `BootstrapSelect.ValidationState`, `BorderRadius`, single/multiple selection, custom values, local search, async paging/retry, popup keyboard behavior, tab traversal behavior, accessibility, RTL layout, and caller-owned renderer/provider semantics.
- Validation colors keep the existing priority implemented by `BootstrapTextBoxRenderLogic.ResolveBorderColor`; this fix changes border geometry/presentation, not validation semantics.
- Focused selection shells use the theme's `FocusBorderWidth`; unfocused shells use `BorderWidth`, matching the existing Bootstrap input family.
- All border widths, radii, search insets, and search heights must come from current theme metrics and `DpiScaler`; do not introduce repeated hard-coded visual pixels when an equivalent token exists.
- `BootstrapSelectDropDownContent.ApplyPresentation(theme, dpi)` may use the supplied `theme`/`dpi` for popup-content host metrics and result presentation, but the nested `BootstrapTextBox` continues to own its own painting/layout contract through `BootstrapThemeManager.CurrentTheme` and its real `DeviceDpi`. Synthetic `ApplyPresentation(..., 120/144/192)` tests must not claim that they changed the nested control's actual `DeviceDpi`.
- Light/Dark integration tests for the nested search wrapper must change `BootstrapThemeManager.CurrentTheme` itself, not merely pass a different theme object to `ApplyPresentation(...)`.
- `Tab` preservation must be tested through WinForms dialog-key preprocessing (`PreProcessMessage`/`ProcessDialogKey`) while the native search editor actually owns focus. Do not prove the contract only by calling `OnSearchKeyDown`, raising `KeyDown` directly, or invoking `TabRequested` manually.
- The search composition must expose one logical accessible text-editing node. The decorative/themed `BootstrapTextBox` wrapper must not become a second accessible text input in addition to the native editor.
- All owned `Pen`, `Brush`, `GraphicsPath`, and other GDI resources must be deterministically disposed.
- Add failing regression tests before each behavioral production change.
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

### Preservation hazard C — `Tab` routing changes when search becomes composite

The current direct native `TextBox` and the proposed `BootstrapTextBox` wrapper do not have identical WinForms dialog-key behavior. `BootstrapTextBox` owns the tab stop while its inner native editor has `TabStop = false`; it forwards native `KeyDown`/`PreviewKeyDown`, but a normal `Tab` is fundamentally a dialog key and is not guaranteed to arrive as `KeyDown` unless it is explicitly classified as an input key.

Therefore the fix must not assume that keeping this line alone is sufficient:

```csharp
_searchEditor.KeyDown += OnSearchKeyDown;
```

Normal `Tab` must be intercepted through the wrapper's `ProcessDialogKey` path, close the popup, and then continue WinForms-style traversal relative to the owner `BootstrapSelect`. The regression must start with the native search editor focused and drive a `WM_KEYDOWN/Tab` through `PreProcessMessage` so the actual classification path is exercised.

### Preservation hazard D — explicit presentation context versus real `BootstrapTextBox` context

`BootstrapSelectDropDownContent.ApplyPresentation(renderer, theme, dpi)` receives an explicit theme and logical DPI. This is appropriate for the search host's padding/height and for `BootstrapSelectResultsView`. However `BootstrapTextBox.OnPaint()` and `LayoutChildren()` intentionally read `BootstrapThemeManager.CurrentTheme` and the control's real `DeviceDpi`.

The implementation must preserve that primitive contract rather than add a second theme/DPI injection mechanism only for Select. Consequently:

- pure/synthetic 96/120/144/192 tests may verify search-host inset/height computed from the supplied `dpi`;
- they must not claim that the nested `BootstrapTextBox` border/internal padding was painted at that synthetic DPI;
- wrapper re-theming must be tested by changing `BootstrapThemeManager.CurrentTheme`;
- real wrapper DPI behavior is covered by its own primitive tests plus the Windows 100/125/150/200% manual matrix.

At runtime `BootstrapSelectDropDownController.ApplyPresentation()` already derives the theme from `BootstrapThemeManager.CurrentTheme` and DPI from the owner control, so the popup host and nested primitive normally share the same real environment.

### Preservation hazard E — duplicate accessibility nodes

A direct native search `TextBox` currently contributes one logical text-editing surface. A naïve replacement with the public `BootstrapTextBox` defaults can expose both the wrapper and the nested native editor as text-like accessibility nodes. The internal Select search specialization must make the wrapper a non-text container/decorative node and leave the native editor as the single logical accessible text input, with a stable `Search` accessible name.

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
| Light/Dark switch while open | Real `BootstrapThemeManager.CurrentTheme` change updates overlay, search wrapper, results, text, and borders without reconstructing public state |
| 96/120/144/192 logical DPI | Closed-shell metrics and search-host inset/height scale predictably; nested `BootstrapTextBox` continues to use its real `DeviceDpi` |
| Keyboard/search | Existing character input, Ctrl+A/C/V/X, Up/Down/Home/End/PageUp/PageDown, Enter, Escape, and Tab behavior remain unchanged |
| Tab from focused native search editor | Popup closes and focus advances to the next WinForms tab stop through the real dialog-key path; Shift+Tab remains reverse traversal |
| Accessibility | Search composition exposes one logical text-editing node; the themed wrapper is not a second text input |
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
  - Handles `Tab` in `ProcessDialogKey` so a normal dialog key can be reported without forcing `PreviewKeyDown.IsInputKey = true`.
  - Keeps the wrapper non-text in accessibility semantics and the actual native editor as the single accessible text input.
  - Keeps the actual native editor protected/private through `BootstrapTextBox`; no public editor API is added.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectRenderLogicTests.cs`
  - Pure DPI/geometry regression coverage for shell metrics.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
  - STA composition/layout/search/dialog-key regression coverage for the popup search field.

### Modify

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
  - Consume `BootstrapSelectRenderLogic` in `OnPaint()`.
  - Use focus-aware border thickness and anti-aliased drawing.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
  - Replace the direct `TextBox` with `BootstrapSelectSearchTextBox` inside an owned search band.
  - Use theme metrics for the host inset and field height.
  - Preserve search events and non-Tab keyboard routing.
  - Route wrapper dialog-key Tab requests to the controller with traversal direction.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
  - Close the popup for a search-field Tab request and explicitly continue owner-relative WinForms tab traversal after close.
  - Do not change placement, flip/shift, or popup construction architecture.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`
  - Add at most one internal test-only content inspection hook if required by the end-to-end dialog-key regression.
  - Do not expose the native editor and do not change public API.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectVisualRegressionTests.cs`
  - Add real bitmap regressions that connect the pure geometry rules to `BootstrapSelect.OnPaint()` output.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`
  - Prove forwarded printable input and real popup Tab traversal still work through the new wrapper.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectAccessibilityTests.cs`
  - Prove the search composition has one logical accessible text editor and does not turn the themed wrapper into a duplicate text-input node.

- `docs/BOOTSTRAP_SELECT.md`
  - Document the corrected theme/DPI shell and the themed/native search composition.

- `docs/TESTING.md`
  - Record the automated and manual regression matrix, including the synthetic-DPI boundary.

- `CHANGELOG.md`
  - Add an Unreleased Changed entry for the rendering hardening.

### Intentionally unchanged

- `BootstrapOverlayDropDown.cs`, `BootstrapOverlaySurface.cs`, and the shared placement engine: the root cause is not the overlay geometry engine.
- `BootstrapSelectRenderer.cs`: result/chip/text rendering is not responsible for the broken outer/search borders.
- `BootstrapSelect.Accessibility.cs`: the outer ComboBox-style accessibility contract does not need redesign; only the new internal search wrapper needs non-duplicating semantics.
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

- [ ] **Step 5: Add a focused bitmap regression that proves the Select really owns focus before sampling**

A non-shown Form is not sufficient evidence that `BootstrapSelect` entered the focused paint path. Use a real shown host and assert focus before `DrawToBitmap()`:

```csharp
using var host = new Form
{
    ShowInTaskbar = false,
    ClientSize = new Size(420, 120)
};
using var select = new BootstrapSelect
{
    Bounds = new Rectangle(20, 20, 340, 40)
};
host.Controls.Add(select);
host.Show();
Application.DoEvents();

Assert.That(select.Focus(), Is.True);
Application.DoEvents();
Assert.That(select.ContainsFocus || select.Focused, Is.True,
    "The bitmap regression must exercise the focused Select paint path.");

using var bitmap = new Bitmap(select.Width, select.Height);
select.DrawToBitmap(bitmap, select.ClientRectangle);
```

Sample both the right-most edge and the immediately inner pixel at the vertical midpoint. Compare both against `theme.Colors.Focus` versus `theme.Colors.Surface`. The immediately inner pixel must remain focus-border-dominated at 96 DPI with default `FocusBorderWidth = 2`, distinguishing the intended result from the old 1px path.

- [ ] **Step 6: Run the new focused tests and record the expected pre-fix failures**

Run:

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectRenderLogicTests|FullyQualifiedName~BootstrapSelectVisualRegressionTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectRenderLogicTests|FullyQualifiedName~BootstrapSelectVisualRegressionTests"
```

Expected before implementation:
- render-logic tests: compile failure because the new internal helper is missing;
- focused thickness regression: fail against the current 1px-focused Select implementation after confirmed focus acquisition;
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

### Task 3: Freeze Popup Search Composition, Dialog-Key, and Accessibility Regressions

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectAccessibilityTests.cs`
- Modify only if needed for end-to-end inspection: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`

**Interfaces:**
- Consumes: internal `BootstrapSelectDropDownContent`, current theme, existing popup/search test hooks, real WinForms `PreProcessMessage` dialog-key processing.
- Produces: failing tests requiring a `BootstrapTextBox` search shell, one borderless native editor, an inset search band, full-width result viewport, real Tab traversal, non-duplicating accessibility semantics, and preserved search behavior.

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

- [ ] **Step 3: Lock search-host inset and field height to supplied theme metrics without pretending to change `DeviceDpi`**

For synthetic 96/120/144/192 DPI values, apply presentation and assert only the metrics owned by `BootstrapSelectDropDownContent`:

```text
search-host horizontal/vertical inset = DpiScaler.Scale(theme.Metrics.SpacingXS, dpi)
search-host field allocation = DpiScaler.Scale(theme.Metrics.ControlHeightSmall, dpi)
search-host total height = field allocation + 2 * inset
```

Obtain the search wrapper and its immediate parent from the control tree. The result viewport must remain `DockStyle.Fill` and must not receive the search band's horizontal inset.

Do **not** assert that the nested `BootstrapTextBox` border width, radius, or internal editor padding was rendered at the synthetic `dpi` passed to `ApplyPresentation(...)`; that primitive uses its actual `DeviceDpi`. Real DPI painting remains covered by `BootstrapTextBox`'s own tests and the Windows scaling matrix in Task 5.

- [ ] **Step 4: Add `SearchEnabled = false` regression**

Set `content.SearchEnabled = false`, perform layout, and assert the search band's `Visible` state is false and the results viewport uses the available content area. Re-enable search and assert the same search wrapper returns without reconstructing the content object.

- [ ] **Step 5: Preserve text/search event semantics**

Subscribe to `SearchTextChanged`, set `SearchText = "Northwind"`, and assert exactly one logical event with the same text. Call `ClearSearchSilently()` and assert the text becomes empty without raising another search event.

- [ ] **Step 6: Extend interaction coverage for forwarded printable input without inventing a nonexistent search-text hook**

`BootstrapSelect` currently exposes `VisibleResultItemTextsForTest`, but it does not expose `CurrentSearchTextForTest`. Test the observable behavior through the existing outer-control input pipeline instead of adding a search-text API.

Add a test-only subclass in `BootstrapSelectInteractionTests`:

```csharp
private sealed class TestBootstrapSelect : BootstrapSelect
{
    internal void RaisePrintableKeyForTest(char character)
    {
        OnKeyPress(new KeyPressEventArgs(character));
    }

    internal bool ProcessDialogKeyForTest(Keys keyData)
    {
        return ProcessDialogKey(keyData);
    }
}
```

Host the Select on a shown Form so `OnHandleCreated()` has attached the existing popup input handlers. Add local items such as `Alpha` and `Northwind`, focus the Select, call `RaisePrintableKeyForTest('N')`, pump `Application.DoEvents()`, and assert:

- the popup is open;
- `VisibleResultItemTextsForTest` contains `Northwind`;
- `VisibleResultItemTextsForTest` does not contain `Alpha`.

This proves the forwarded printable character still reaches the same local-search pipeline without adding a demo-only handler or public API.

- [ ] **Step 7: Add an end-to-end `Tab` regression through the focused native editor's dialog-key preprocessing**

The test must use a shown Form with at least a previous tab stop, the `BootstrapSelect`, and a next tab stop. Open the searchable popup and obtain its content through existing internals. If the current test hooks cannot reach the content, add only this internal inspection property to `BootstrapSelect.Popup.cs`:

```csharp
internal BootstrapSelectDropDownContent? DropDownContentForTest =>
    _dropDownController?.Content;
```

This is an internal test hook only; do not expose the native editor itself.

Find the single native `TextBox` below the popup content, focus it, and verify focus before sending Tab. Drive the real preprocessing path:

```csharp
var native = Descendants(select.DropDownContentForTest!)
    .OfType<TextBox>()
    .Single();

Assert.That(native.Focus(), Is.True);
Application.DoEvents();
Assert.That(native.Focused, Is.True);

var message = Message.Create(
    native.Handle,
    0x0100, // WM_KEYDOWN
    (IntPtr)(int)Keys.Tab,
    IntPtr.Zero);

Assert.That(native.PreProcessMessage(ref message), Is.True);
Application.DoEvents();

Assert.Multiple((Action)(() =>
{
    Assert.That(select.IsDropDownOpenForTest, Is.False);
    Assert.That(nextControl.Focused, Is.True,
        "Tab from the popup search editor must continue owner-relative WinForms traversal.");
}));
```

Do not replace this with a direct `OnSearchKeyDown(Keys.Tab)` or `TabRequested?.Invoke()` test. Add a manual Shift+Tab check in Task 5; the internal routing implemented in Task 4 must retain direction so reverse traversal is supported too.

- [ ] **Step 8: Add a popup-search accessibility regression**

Extend `BootstrapSelectAccessibilityTests` with STA/non-parallelizable coverage for the new composition. After creating the content/search wrapper, assert:

```csharp
var search = Descendants(content)
    .OfType<BootstrapTextBox>()
    .Single();
var native = Descendants(search)
    .OfType<TextBox>()
    .Single();

Assert.Multiple((Action)(() =>
{
    Assert.That(search.AccessibilityObject.Role,
        Is.Not.EqualTo(AccessibleRole.Text));
    Assert.That(native.AccessibilityObject.Role,
        Is.EqualTo(AccessibleRole.Text));
    Assert.That(native.AccessibilityObject.Name,
        Is.EqualTo("Search"));
    Assert.That(
        Descendants(search)
            .Count(control =>
                control.AccessibilityObject.Role == AccessibleRole.Text),
        Is.EqualTo(1));
}));
```

The purpose is not to redesign the outer `BootstrapSelectAccessibleObject`; it is to prevent the new wrapper from creating a second logical text input.

- [ ] **Step 9: Run popup-content, interaction, and accessibility regressions before implementation**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectInteractionTests|FullyQualifiedName~BootstrapSelectAccessibilityTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectInteractionTests|FullyQualifiedName~BootstrapSelectAccessibilityTests"
```

Expected before implementation:
- composition/inset tests fail because there is no `BootstrapTextBox` wrapper;
- accessibility composition test fails for the same reason;
- the real Tab regression is expected to expose whether the current direct-editor routing can be preserved automatically or requires the explicit dialog-key route specified in Task 4;
- existing search/outer accessibility tests remain guard rails.

- [ ] **Step 10: Commit popup regression tests**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectAccessibilityTests.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs
git commit -m "test: reproduce BootstrapSelect popup search border regressions"
```

If `BootstrapSelect.Popup.cs` did not need the internal inspection property, omit it from the commit.

---

### Task 4: Replace the Native `FixedSingle` Search Border with a Themed Search Surface

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchTextBox.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectAccessibilityTests.cs`

**Interfaces:**
- Consumes: public `BootstrapTextBox` behavior, protected native `Editor`, current theme metrics, current `SearchTextChanged`/keyboard routing contract, normal WinForms dialog-key traversal.
- Produces: an internal search editor that keeps native editing while presenting one framework-owned rounded border inside an inset search band, catches Tab through `ProcessDialogKey`, and exposes only one logical accessible text input.

- [ ] **Step 1: Add the internal `BootstrapSelectSearchTextBox` specialization with native editing, dialog-key routing, and non-duplicating accessibility**

Use `BootstrapTextBox` rather than adding another custom input shell:

```csharp
internal sealed class BootstrapSelectSearchTextBox : BootstrapTextBox
{
    internal BootstrapSelectSearchTextBox()
    {
        AccessibleRole = AccessibleRole.Client;
        AccessibleName = null;
        AccessibleDescription = null;

        Editor.AccessibleRole = AccessibleRole.Text;
        Editor.AccessibleName = "Search";
        Editor.AccessibleDescription = "Filters BootstrapSelect results.";
    }

    internal event Action<bool>? TabNavigationRequested;

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

    protected override bool ProcessDialogKey(Keys keyData)
    {
        var keyCode = keyData & Keys.KeyCode;
        var modifiers = keyData & Keys.Modifiers;
        if (keyCode == Keys.Tab &&
            (modifiers & (Keys.Alt | Keys.Control)) == Keys.None)
        {
            var reverse = (modifiers & Keys.Shift) == Keys.Shift;
            TabNavigationRequested?.Invoke(reverse);
            return true;
        }

        return base.ProcessDialogKey(keyData);
    }
}
```

Do not set `PreviewKeyDownEventArgs.IsInputKey = true` for Tab. Doing so would turn Tab into a normal input key and could suppress standard traversal semantics. The wrapper catches the dialog key at the correct `ProcessDialogKey` layer instead.

Do not expose `Editor` publicly. `BootstrapTextBox` already guarantees the inner native editor uses `BorderStyle.None` and forwards ordinary editing/key events through the wrapper.

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

- [ ] **Step 4: Apply explicit theme/DPI metrics only to the search host and preserve the primitive's real theme/DPI contract**

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

Because `_searchEditor` is `DockStyle.Fill`, the parent height/padding determines its allocated field height. Keep `BorderRadius = -1` so the search shell follows `BootstrapTextBox`'s normal current-theme radius behavior. Do not copy the owner's explicit popup radius into the nested field; the inner field is a separate visual surface.

Do not add an internal `ApplyTheme(theme, dpi)` seam to `BootstrapTextBox` just for Select. Its border painting and internal editor padding continue to use `BootstrapThemeManager.CurrentTheme` plus actual `DeviceDpi`. `BootstrapSelectDropDownController.ApplyPresentation()` already supplies the current global theme and owner DPI to the surrounding popup content at runtime.

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

- [ ] **Step 6: Split ordinary search keyboard routing from `Tab` dialog-key routing**

Continue subscribing ordinary editing/navigation events:

```csharp
_searchEditor.TextChanged += OnSearchTextChanged;
_searchEditor.KeyDown += OnSearchKeyDown;
_searchEditor.TabNavigationRequested += reverse =>
    TabRequested?.Invoke(reverse);
```

Change the content event from:

```csharp
internal event Action? TabRequested;
```

to:

```csharp
internal event Action<bool>? TabRequested;
```

where the Boolean is `reverse` for Shift+Tab.

Remove `case Keys.Tab` from `OnSearchKeyDown`; normal Tab is no longer expected to be a `KeyDown` input key. Keep Up/Down/Home/End/PageDown/PageUp/Enter/Escape in the existing switch and keep the early Ctrl+A/C/V/X return so native editing owns those operations.

- [ ] **Step 7: Continue owner-relative tab traversal after the popup closes**

In `BootstrapSelectDropDownController.EnsureCreated()` replace the old parameterless Tab subscription with:

```csharp
_content.TabRequested += OnTabRequested;
```

Add:

```csharp
private void OnTabRequested(bool reverse)
{
    Close(false);

    if (_owner.IsDisposed || !_owner.IsHandleCreated)
        return;

    _owner.BeginInvoke(new Action(() =>
    {
        if (_owner.IsDisposed || !_owner.Enabled)
            return;

        var container = (Control?)_owner.FindForm() ?? _owner.Parent;
        container?.SelectNextControl(
            _owner,
            forward: !reverse,
            tabStopOnly: true,
            nested: true,
            wrap: true);
    }));
}
```

The deferred traversal is intentional: allow the `ToolStripDropDown` close path to complete first, then resume from the owner Select in the same direction a normal WinForms Tab/Shift+Tab would use. Do not move focus to `_resultsView` as an intermediate Tab target.

- [ ] **Step 8: Update preferred-size calculation to use the search band height**

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

- [ ] **Step 9: Run popup-content, interaction, accessibility, popup, and paging regressions on both TFMs**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectInteractionTests|FullyQualifiedName~BootstrapSelectAccessibilityTests|FullyQualifiedName~BootstrapSelectPopupTests|FullyQualifiedName~BootstrapSelectPagingTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectInteractionTests|FullyQualifiedName~BootstrapSelectAccessibilityTests|FullyQualifiedName~BootstrapSelectPopupTests|FullyQualifiedName~BootstrapSelectPagingTests"
```

Expected: pass, including the native-editor `PreProcessMessage(Tab)` integration regression.

- [ ] **Step 10: Run TextBox tests because BootstrapSelect now composes that primitive in the popup**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapTextBoxTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapTextBoxTests"
```

Expected: pass. The Select specialization must not change the public primitive's generic Tab semantics.

- [ ] **Step 11: Commit the popup search fix**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectSearchTextBox.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectAccessibilityTests.cs
git commit -m "fix: theme BootstrapSelect popup search border"
```

---

### Task 5: Theme/DPI Integration, Documentation, and Full Verification

**Files:**
- Modify: `docs/BOOTSTRAP_SELECT.md`
- Modify: `docs/TESTING.md`
- Modify: `CHANGELOG.md`
- Verify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectAccessibilityTests.cs`
- Verify without modification: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Manual verification surface: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs`

**Interfaces:**
- Consumes: final shell metrics, final popup search composition, current integrated demo.
- Produces: documented regression contract, unchanged public API, dual-target build/test evidence, and manual visual/keyboard/accessibility acceptance across themes/scaling.

- [ ] **Step 1: Add a real Light/Dark manager-switch regression before final documentation**

Extend `BootstrapSelectDropDownContentTests` with a theme-switch smoke test that changes `BootstrapThemeManager.CurrentTheme` itself and reuses the same content instance:

```csharp
var originalTheme = BootstrapThemeManager.CurrentTheme;
try
{
    using var content = new BootstrapSelectDropDownContent
    {
        Size = new Size(340, 180)
    };

    BootstrapThemeManager.CurrentTheme =
        BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
    content.ApplyPresentation(
        new BootstrapSelectRenderer(),
        BootstrapThemeManager.CurrentTheme,
        96);
    content.SearchText = "Northwind";
    content.PerformLayout();

    var search = Descendants(content)
        .OfType<BootstrapTextBox>()
        .Single();
    var native = Descendants(search)
        .OfType<TextBox>()
        .Single();
    var lightSearch = search;
    var lightHostColor = search.Parent!.BackColor;

    BootstrapThemeManager.CurrentTheme =
        BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
    content.ApplyPresentation(
        new BootstrapSelectRenderer(),
        BootstrapThemeManager.CurrentTheme,
        96);
    Application.DoEvents();

    Assert.Multiple((Action)(() =>
    {
        Assert.That(
            Descendants(content).OfType<BootstrapTextBox>().Single(),
            Is.SameAs(lightSearch));
        Assert.That(content.SearchText, Is.EqualTo("Northwind"));
        Assert.That(native.BorderStyle, Is.EqualTo(BorderStyle.None));
        Assert.That(search.Parent!.BackColor,
            Is.EqualTo(BootstrapThemeManager.CurrentTheme.Colors.Surface));
        Assert.That(search.Parent!.BackColor, Is.Not.EqualTo(lightHostColor));
    }));
}
finally
{
    BootstrapThemeManager.CurrentTheme = originalTheme;
}
```

Also assert the result viewport remains present and full-width beneath the search band. Do not simulate a theme switch by passing a Dark theme to `ApplyPresentation(...)` while leaving `BootstrapThemeManager.CurrentTheme` Light; that would not exercise the nested `BootstrapTextBox` contract.

Do not assert exact screenshot bytes across Windows versions.

- [ ] **Step 2: Run every BootstrapSelect test on both target frameworks**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelect"
```

Expected: all pass, including selection, matcher, provider, paging/retry, concurrency, lifecycle, accessibility, popup, interaction, layout, new render logic, new popup-content tests, visual regressions, and real Tab dialog-key regression.

- [ ] **Step 3: Verify the public API baseline did not change**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

Expected: pass with no fingerprint update.

- [ ] **Step 4: Update `docs/BOOTSTRAP_SELECT.md`**

Under **Keyboard and focus behavior** and **Theme, DPI, RTL, and popup placement**, explicitly document:

- closed-shell stroke bounds are inset by half of the actual DPI-scaled normal/focus border width;
- focus uses `FocusBorderWidth`, while validation continues to select the existing semantic color token;
- rounded shell painting is anti-aliased;
- searchable popups use a themed `BootstrapTextBox` shell around one borderless native WinForms editor, preserving native caret/clipboard/IME behavior;
- the search field is inset from the rounded popup shell using theme spacing;
- Tab/Shift+Tab from the native search editor are handled as dialog navigation, close the popup, and resume owner-relative WinForms traversal;
- the wrapper is presentation/container semantics while the native editor remains the single logical accessible search input;
- synthetic popup-content DPI tests verify host allocation, while the nested `BootstrapTextBox` uses real `DeviceDpi` like the rest of the primitive family.

Do not describe the internal wrapper type as public API.

- [ ] **Step 5: Update `docs/TESTING.md`**

Add BootstrapSelect rendering regression coverage to the appropriate pure/STA sections:

```text
- shell metrics at logical DPI 96/120/144/192, including normal/focus widths, radius, and half-stroke inset;
- focused bitmap tests only after a shown Form confirms the Select actually owns focus;
- bitmap right/bottom border containment and focused thickness;
- popup search wrapper composition with one borderless native editor;
- synthetic theme-metric search-host inset/height and SearchEnabled layout at 96/120/144/192;
- explicit note that synthetic ApplyPresentation DPI does not mutate BootstrapTextBox.DeviceDpi;
- Tab through the native search editor's PreProcessMessage/ProcessDialogKey path, including popup close and next-control focus;
- one logical accessible search Text node rather than wrapper + native duplication;
- real BootstrapThemeManager Light/Dark switching without search-state loss;
- manual 100/125/150/200% Windows scaling visual check for rounded validation/focus and popup search borders.
```

- [ ] **Step 6: Add an Unreleased Changed entry to `CHANGELOG.md`**

Add one release-facing bullet similar to:

```markdown
- Hardened `BootstrapSelect` border rendering with focus-aware DPI-scaled stroke insets and anti-aliased rounded shells, and replaced the popup's flush native `FixedSingle` search border with an inset Bootstrap-themed search surface while preserving native WinForms text editing, Tab traversal, and accessible search semantics.
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
6. Verify Up/Down, Home/End, PageUp/PageDown, Enter, Escape, Ctrl+A/C/V/X, and reopening continue to work.
7. With the caret in the native search editor, press Tab and verify the popup closes and focus moves to the next form control. Reopen, press Shift+Tab, and verify reverse traversal to the previous form control.
8. Inspect with Windows Narrator or Accessibility Insights where available: the search surface must announce one search/text editor rather than two nested text inputs.
9. Repeat at Windows scaling 100%, 125%, 150%, and 200% where available; verify both the outer popup and nested `BootstrapTextBox` at their real monitor DPI.
10. Move the window near monitor edges and across monitors to confirm the rendering fix did not alter flip/shift placement.

- [ ] **Step 10: Commit documentation after verification evidence is green**

```powershell
git add docs/BOOTSTRAP_SELECT.md docs/TESTING.md CHANGELOG.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectAccessibilityTests.cs
git commit -m "docs: document BootstrapSelect border rendering regression coverage"
```

---

## Acceptance Checklist

- [ ] `BootstrapSelect` no longer uses a fixed `0.5f` inset independent of the actual stroke width.
- [ ] Focused shell thickness uses `theme.Metrics.FocusBorderWidth`; unfocused thickness uses `theme.Metrics.BorderWidth`.
- [ ] Border bounds are inset by `actualBorderWidth / 2f` at 96/120/144/192 logical DPI.
- [ ] Focused bitmap regression uses a shown Form and proves the Select actually owns focus before sampling focus-border thickness.
- [ ] Rounded selection-shell painting uses `SmoothingMode.AntiAlias` and restores the previous smoothing mode.
- [ ] Validation color priority remains unchanged.
- [ ] Popup search no longer uses a directly visible native `BorderStyle.FixedSingle` editor.
- [ ] Popup search contains one real native WinForms `TextBox` with `BorderStyle.None` inside a `BootstrapTextBox` shell.
- [ ] Search field host is inset using `SpacingXS` and allocated from `ControlHeightSmall`.
- [ ] Synthetic 96/120/144/192 popup-content tests verify host metrics only and do not claim to mutate nested `BootstrapTextBox.DeviceDpi`.
- [ ] Result viewport remains owner-rendered and full-width; no per-row child controls are introduced.
- [ ] Search disabled mode has no empty search-band gap.
- [ ] Search text, Ctrl+A/C/V/X, keyboard row navigation, Enter/Escape, IME, and forwarded printable input are preserved.
- [ ] Tab from the focused native search editor is verified through `PreProcessMessage`/`ProcessDialogKey`, closes the popup, and advances focus to the next owner-relative tab stop.
- [ ] Shift+Tab preserves reverse traversal direction.
- [ ] Tab preservation does not rely on forcing `PreviewKeyDown.IsInputKey = true`.
- [ ] Search wrapper is not exposed as a second accessible text input; the native editor is the single logical accessible Text node with a stable Search name.
- [ ] Light/Dark switching changes `BootstrapThemeManager.CurrentTheme` and re-themes an open popup without losing search state.
- [ ] Overlay placement/flip/shift behavior is unchanged.
- [ ] Existing caller ownership and lifecycle behavior is unchanged.
- [ ] All BootstrapSelect tests pass on `net48` and `net8.0-windows`.
- [ ] Full test suite passes on both TFMs.
- [ ] Public API baseline passes without fingerprint changes.
- [ ] Integrated demo visually/keyboard-accessibility passes Light/Dark and 100/125/150/200% Windows scaling checks.

## Out of Scope

This corrective plan does not redesign Select2 behavior, selection semantics, result rendering, paging, outer `BootstrapSelect` accessibility, placement, or overlay lifecycle. It does not add search clear buttons, new public styling knobs, animations, custom text parsing, or a custom IME layer. The accessibility work is limited to preventing the new internal search wrapper from becoming a duplicate logical text input, and the keyboard work is limited to preserving Tab/Shift+Tab behavior across the composition change. If implementation reveals an independent overlay-window clipping defect after the native `FixedSingle` collision is removed, capture that as a separate regression and corrective plan rather than expanding this fix without evidence.
