# BootstrapSelect Custom Result Rendering and Product Search Plan

> **For agentic workers:** REQUIRED SUB-SKILLS: Use `superpowers:test-driven-development` while implementing each task and `superpowers:verification-before-completion` before claiming the feature is complete. Use `superpowers:systematic-debugging` if a focused test fails for a reason different from the behavior frozen below. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend `BootstrapSelect` with a configurable, DPI-aware **uniform popup result-row height** so callers can use the existing renderer abstraction for Select2-style custom results, including a two-line product search result whose first line is product name and second line is `Unit | Unit price | Stock quantity`.

**Architecture:** Keep `IBootstrapSelectRenderer` as the only public custom-paint extension point and keep `BootstrapSelectItem.Tag` as caller-owned metadata. Add one public logical-pixel property, `BootstrapSelect.ResultRowHeight`, defaulting to `32`, then pass it through `BootstrapSelectDropDownController -> BootstrapSelectDropDownContent -> BootstrapSelectResultsView`. `BootstrapSelectResultsView` continues to use one constant row height for every row, so existing O(1) scrolling, hit testing, PageUp/PageDown, near-end paging, and visible-range calculations remain intact. The demo implements a product renderer through `IBootstrapSelectRenderer` and delegates group headers, the closed single-selection surface, and chips to `BootstrapSelectRenderer` by composition.

**Tech Stack:** C# 12, Windows Forms owner painting, `System.Drawing`, `TextRenderer`, existing `DpiScaler`/theme infrastructure, `IBootstrapSelectRenderer`, `BootstrapSelectItem.Tag`, `net48;net8.0-windows`, NUnit 4, STA WinForms tests.

**Spec:** `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`

**Related plans and docs:**

- `docs/plans/20260829-005-bootstrap-select.md`
- `docs/plans/20260901-002-bootstrap-select-popup-sizing-fix.md`
- `docs/plans/20260901-003-bootstrap-select-popup-lifecycle-and-navigation-fix.md`
- `docs/BOOTSTRAP_SELECT.md`
- `docs/PUBLIC_API_BASELINE.md`

---

## Design delta

The approved BootstrapSelect design already includes:

- custom result/selection rendering through `IBootstrapSelectRenderer`;
- arbitrary caller-owned metadata through `BootstrapSelectItem.Tag`;
- owner-rendered result rows;
- DPI-aware geometry;
- constant-height result layout.

The approved design also explicitly excludes variable-height rows and HTML-like templates. Those exclusions remain unchanged.

The additive public API is:

```csharp
public int ResultRowHeight { get; set; }
```

Contract:

- Unit: logical 96-DPI pixels.
- Default: `32`.
- Valid values: positive integers only.
- Invalid values (`<= 0`): throw `ArgumentOutOfRangeException` and keep the previous value.
- Effective device height: `DpiScaler.Scale(ResultRowHeight, effectiveDpi)`.
- All popup rows for one control use the same effective height, including item, group, loading, empty, instruction, error/retry, and create-value rows.
- Changing the property while the popup is already created reapplies presentation; when the popup is open it also recomputes/repositions popup bounds immediately.
- The property does not change the closed control height, chip height, search textbox height, fonts, renderer ownership, item ownership, or selection semantics.
- `IBootstrapSelectRenderer` signatures remain unchanged.
- No measure callback, per-item height, hosted row control, template object model, or HTML-like rendering layer is added.

Recommended product result at 100% DPI:

```text
┌────────────────────────────────────────────────────────────┐
│ Sữa tươi Vinamilk 100% 1L                                │
│ Hộp | 36.500 | Tồn: 128                                  │
├────────────────────────────────────────────────────────────┤
│ Coca-Cola lon 330ml                                       │
│ Lon | 10.000 | Tồn: 56                                   │
└────────────────────────────────────────────────────────────┘
```

Recommended demo configuration:

```csharp
productSelect.ResultRowHeight = 48;
productSelect.Renderer = new BootstrapSelectProductRenderer();
```

---

## Required behavior matrix

| Scenario | Expected behavior |
| --- | --- |
| Existing caller does not set `ResultRowHeight` | Existing 32-logical-pixel row behavior remains unchanged. |
| `ResultRowHeight = 48`, DPI = 96 | Effective row height is 48 px. |
| `ResultRowHeight = 48`, DPI = 144 | Effective row height is `DpiScaler.Scale(48, 144)`. |
| Setter receives `0` or a negative value | Throw `ArgumentOutOfRangeException`; previous valid value remains. |
| Property changes before popup creation | First popup uses the configured height. |
| Property changes after popup creation while closed | Next open uses the new height without recreating the popup solely for this change. |
| Property changes while popup is open | Popup remains open, result geometry updates, and overlay bounds are recomputed/repositioned. |
| Mouse click with custom height | Hit testing identifies the row using the custom effective height. |
| Mouse wheel with custom height | Scroll increments remain row-based and use the custom effective height. |
| PageDown/PageUp | Number of rows moved is based on `ClientSize.Height / RowHeight`. |
| Async paging | Existing near-end and navigation-preservation behavior remains correct. |
| Group/loading/empty/error/create-value row | Uses the same uniform row height. |
| Custom renderer | Can read `context.Item.Tag` without the control interpreting or owning the object. |
| Selected product after popup closes | Closed surface still displays the normal single-line `BootstrapSelectItem.Text` unless caller also customizes `DrawSelection`. |
| Multiple chips | Existing chip behavior remains unless caller customizes `DrawChip`. |

---

## Global constraints

- [ ] Read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, the BootstrapSelect section in `docs/COMPONENTS.md`, the spec, and the related plans above before product-code changes.
- [ ] If `docs/plans/20260901-003-bootstrap-select-popup-lifecycle-and-navigation-fix.md` is not implemented yet, implement/rebase it first. Both plans touch `BootstrapSelectResultsView`, `BootstrapSelectDropDownContent`, and `BootstrapSelectDropDownController`.
- [ ] Preserve one shared implementation for `net48;net8.0-windows`.
- [ ] Keep row height uniform per control. Do not implement variable-height rows, per-item measurement, prefix-height indexes, hosted controls, HTML parsing, or a second layout engine.
- [ ] Do not change any method signature on `IBootstrapSelectRenderer`.
- [ ] Do not unseal `BootstrapSelectRenderer` only for customization; use interface implementation plus composition.
- [ ] Keep `BootstrapSelectItem.Tag` untyped and caller-owned. Do not add product-specific fields to the core item model.
- [ ] Add no external package.
- [ ] Reuse `DpiScaler`, `BootstrapTheme`, and theme colors; do not hard-code semantic colors where theme tokens exist.
- [ ] Dispose demo-owned GDI objects created during painting. Never dispose `context.Font`, `context.Theme`, `context.Item`, `context.Item.Tag`, or caller-owned renderer instances.
- [ ] Preserve popup sizing/reposition behavior from plan `20260901-002`.
- [ ] Preserve reset/preserve navigation semantics from plan `20260901-003` if present; presentation geometry changes must not reset logical navigation state.
- [ ] Use TDD: failing focused test -> observe failure -> minimal implementation -> focused pass -> broader regression tests.
- [ ] Because this is an additive public API change after the release-candidate baseline, review the emitted API surface before updating the approved fingerprint.

---

## File structure and responsibilities

### Core files to modify

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
  - Add backing field and public `ResultRowHeight` property.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`
  - Add a narrow private helper for reapplying/repositioning popup presentation after layout-property changes.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
  - Forward `_owner.ResultRowHeight` into content presentation.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
  - Forward logical result row height into the results view.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`
  - Store logical row height and derive effective `RowHeight` from DPI.

### Core files to inspect, not change by default

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultLayout.cs`
  - Already accepts `rowHeight`; keep it generic and constant-height.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectRenderer.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderContexts.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderer.cs`
  - Existing renderer contract is sufficient.

### Demo files

- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProduct.cs`
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProductRenderer.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs`

### Test files

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BootstrapSelectDemoContractTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`

### Documentation files

- Modify: `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`
- Modify: `docs/BOOTSTRAP_SELECT.md`
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/PUBLIC_API_BASELINE.md`

---

## Task 1: Record the uniform-row-height design change

**Files:**

- Modify: `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`

**Interfaces:**

- Produces documented contract: `public int ResultRowHeight { get; set; }`, default `32`, uniform logical pixels.
- Preserves the existing non-goals for variable-height rows and HTML-like templates.

- [ ] **Step 1: Extend the approved public surface**

Add the property beside popup/layout members:

```csharp
public int DropDownWidth { get; set; }
public int MaxDropDownHeight { get; set; }
public int ResultRowHeight { get; set; }
public int MaximumSelectionRows { get; set; }
```

- [ ] **Step 2: Add exact semantics**

Document these rules in the spec:

```text
ResultRowHeight is uniform for every popup result row.
The unit is logical pixels at 96 DPI.
The default is 32.
The value must be greater than zero.
The effective height is DPI-scaled through DpiScaler.
Changing it while open reapplies presentation and repositions the popup.
Variable-height rows remain unsupported.
Renderer measurement callbacks remain unsupported.
```

- [ ] **Step 3: Keep the non-goal section explicit**

Retain `variable-height result rows` and `HTML-like templates`. Add one sentence that caller-configurable **uniform** row height is supported and does not imply per-item measurement.

- [ ] **Step 4: Commit the design change**

```bash
git add docs/superpowers/specs/2026-08-29-bootstrap-select-design.md
git commit -m "docs: define BootstrapSelect result row height"
```

---

## Task 2: Add and propagate `ResultRowHeight`

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`

**Interfaces:**

- Produces: `public int BootstrapSelect.ResultRowHeight { get; set; }`.
- `BootstrapSelectDropDownContent.ApplyPresentation(IBootstrapSelectRenderer renderer, BootstrapTheme theme, int dpi, int logicalResultRowHeight)`.
- `BootstrapSelectResultsView.ApplyPresentation(IBootstrapSelectRenderer renderer, BootstrapTheme theme, int dpi, int logicalRowHeight)`.
- Effective `BootstrapSelectResultsView.RowHeight` remains internal.

- [ ] **Step 1: Write failing public-property tests**

```csharp
[Test]
public void ResultRowHeightDefaultsToThirtyTwo()
{
    using var select = new BootstrapSelect();
    Assert.That(select.ResultRowHeight, Is.EqualTo(32));
}

[TestCase(0)]
[TestCase(-1)]
public void ResultRowHeightRejectsNonPositiveValues(int value)
{
    using var select = new BootstrapSelect { ResultRowHeight = 48 };

    Assert.Throws<ArgumentOutOfRangeException>(() => select.ResultRowHeight = value);
    Assert.That(select.ResultRowHeight, Is.EqualTo(48));
}
```

- [ ] **Step 2: Run the focused public tests and observe failure**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectTests"
```

Expected: compile/test failure because `ResultRowHeight` does not exist.

- [ ] **Step 3: Add the public property**

In `BootstrapSelect.cs`:

```csharp
private int _resultRowHeight = 32;

/// <summary>Gets or sets the uniform popup result-row height in logical pixels.</summary>
[Category("Layout")]
[DefaultValue(32)]
public int ResultRowHeight
{
    get => _resultRowHeight;
    set
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Result row height must be positive.");
        }

        if (_resultRowHeight == value)
        {
            return;
        }

        _resultRowHeight = value;
        RefreshDropDownPresentationAndLayout();
    }
}
```

- [ ] **Step 4: Add the popup refresh helper**

In `BootstrapSelect.Popup.cs`:

```csharp
private void RefreshDropDownPresentationAndLayout()
{
    if (_dropDownController is null)
    {
        return;
    }

    _dropDownController.ApplyPresentation();
    if (_dropDownController.IsOpen)
    {
        _dropDownController.Reposition();
    }
}
```

Do not close/recreate the popup solely because row height changed.

- [ ] **Step 5: Write failing results-view propagation tests**

```csharp
[Test]
public void ApplyPresentationUsesConfiguredLogicalRowHeight()
{
    using var view = new BootstrapSelectResultsView();
    view.ApplyPresentation(new BootstrapSelectRenderer(), BootstrapThemeManager.CurrentTheme, 96, 48);

    Assert.That(view.RowHeight, Is.EqualTo(48));
}

[Test]
public void ApplyPresentationScalesConfiguredRowHeightForDpi()
{
    using var view = new BootstrapSelectResultsView();
    view.ApplyPresentation(new BootstrapSelectRenderer(), BootstrapThemeManager.CurrentTheme, 144, 48);

    Assert.That(view.RowHeight, Is.EqualTo(DpiScaler.Scale(48, 144)));
}
```

- [ ] **Step 6: Run the results-view tests and observe failure**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectResultsViewTests"
```

Expected: failure because current presentation methods do not accept logical row height and `RowHeight` still scales hard-coded `32`.

- [ ] **Step 7: Replace the hard-coded results-view height**

In `BootstrapSelectResultsView.cs`:

```csharp
private int _logicalRowHeight = 32;

internal int RowHeight => DpiScaler.Scale(_logicalRowHeight, _dpi);
```

Update presentation:

```csharp
internal void ApplyPresentation(
    IBootstrapSelectRenderer renderer,
    BootstrapTheme theme,
    int dpi,
    int logicalRowHeight)
{
    _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    _theme = theme ?? throw new ArgumentNullException(nameof(theme));
    if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
    if (logicalRowHeight <= 0) throw new ArgumentOutOfRangeException(nameof(logicalRowHeight));

    _dpi = dpi;
    _logicalRowHeight = logicalRowHeight;
    BackColor = theme.Colors.Surface;
    ClampScroll();
    EnsureHighlightedVisible();
    Invalidate();
}
```

If plan 003 has added explicit reset/preserve navigation modes, retain them. `ApplyPresentation` may clamp/reveal the existing highlight after geometry changes but must not replace results or choose a new highlight.

- [ ] **Step 8: Forward height through content/controller**

`BootstrapSelectDropDownContent.ApplyPresentation` receives `logicalResultRowHeight` and forwards:

```csharp
_resultsView.ApplyPresentation(renderer, theme, dpi, logicalResultRowHeight);
```

`BootstrapSelectDropDownController.ApplyPresentation` calls:

```csharp
_content.ApplyPresentation(_owner.Renderer, theme, dpi, _owner.ResultRowHeight);
```

- [ ] **Step 9: Rerun focused tests**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectTests|FullyQualifiedName~BootstrapSelectResultsViewTests"
```

Expected: PASS.

- [ ] **Step 10: Commit the core API/propagation**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs \
        src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs \
        src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs \
        src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs \
        src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs
git commit -m "feat: add BootstrapSelect result row height"
```

---

## Task 3: Lock sizing, hit testing, scrolling, paging, and open-popup behavior

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Modify product files from Task 2 only if a failing regression exposes an actual missing propagation path.

**Interfaces:** Keep using `BootstrapSelectResultLayout.Create(rowCount, rowHeight, viewportHeight, scrollOffset)` unchanged.

- [ ] **Step 1: Add custom-height layout/hit-test coverage**

```csharp
[Test]
public void CustomRowHeightDrivesVisibleRangeAndHitTesting()
{
    var layout = BootstrapSelectResultLayout.Create(
        rowCount: 100,
        rowHeight: 48,
        viewportHeight: 144,
        scrollOffset: 96);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(layout.FirstVisibleIndex, Is.EqualTo(2));
        Assert.That(layout.LastVisibleIndex, Is.EqualTo(4));
        Assert.That(layout.HitTestIndex(0), Is.EqualTo(2));
        Assert.That(layout.HitTestIndex(143), Is.EqualTo(4));
    }));
}
```

This should pass without changing `BootstrapSelectResultLayout`, proving the constant-height primitive already supports the feature.

- [ ] **Step 2: Add preferred-size and PageDown tests**

Build a deterministic result set with at least eight selectable item rows. Apply a 48 logical-pixel row height at 96 DPI, set a controlled client height, and assert:

```text
GetPreferredSize(...).Height == visibleRowCount * 48
Page(1) moves by max(1, ClientSize.Height / 48) selectable rows
Page(-1) reverses by the same page-row calculation when enough rows exist
```

Use internal methods directly instead of `SendKeys`.

- [ ] **Step 3: Add mouse-wheel offset coverage**

Invoke the existing mouse-wheel path using the test technique already used in this repository. Assert that the resulting `ScrollOffset` is a valid multiple of the effective custom `RowHeight` and is clamped by `BootstrapSelectResultLayout`.

- [ ] **Step 4: Add dropdown-content preferred-size tests**

At 96 DPI, configure three rows and `logicalResultRowHeight = 48`. Verify:

```text
results contribution = 3 * 48
search-enabled preferred height = searchHostHeight + results contribution
search-disabled preferred height = results contribution
```

Derive search host height from active theme metrics/DPI rather than duplicating a magic number.

- [ ] **Step 5: Add an open-popup property-change regression**

Using existing STA/non-parallel popup-test patterns:

1. Create a visible host form and `BootstrapSelect` with enough items to make height observable.
2. Open the popup.
3. Capture `DropDownCreationCountForTest` and `DropDownBoundsForTest`.
4. Set `select.ResultRowHeight = 48`.
5. Assert popup remains open.
6. Assert creation count is unchanged.
7. Assert bounds are recomputed and remain within `MaxDropDownHeight`/working-area constraints.

Do not require one exact X/Y coordinate because overlay collision logic may flip/shift legitimately.

- [ ] **Step 6: Run focused geometry/popup tests**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectResultsViewTests|FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectPopupTests"
```

Expected: PASS.

- [ ] **Step 7: Run paging/navigation regressions**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectProviderIntegrationTests|FullyQualifiedName~BootstrapSelectPagingTests|FullyQualifiedName~BootstrapSelectInteractionTests"
```

Expected: PASS. Custom geometry must not reintroduce highlight/scroll reset during async paging.

- [ ] **Step 8: Commit geometry/regression coverage**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs
git commit -m "test: cover BootstrapSelect custom row geometry"
```

Include a core-file correction in this commit only if a new failing test proved it necessary.

---

## Task 4: Add a two-line Product Search demo renderer

**Files:**

- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProduct.cs`
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProductRenderer.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BootstrapSelectDemoContractTests.cs`

**Interfaces:**

- Product metadata lives only in `BootstrapSelectItem.Tag`.
- `BootstrapSelectItem.Text` remains the product name so default matching and the closed selection surface stay useful.
- Custom popup drawing consumes `BootstrapSelectResultRenderContext`.

- [ ] **Step 1: Add a failing demo contract test**

Locate the product sample by a stable control name (`productSearchSelect`) and assert:

```csharp
Assert.That(productSelect.ResultRowHeight, Is.EqualTo(48));
Assert.That(productSelect.Renderer.GetType().Name, Is.EqualTo("BootstrapSelectProductRenderer"));
Assert.That(productSelect.Items.Count, Is.GreaterThanOrEqualTo(3));
Assert.That(productSelect.Items[0].Tag, Is.Not.Null);
Assert.That(productSelect.Items[0].Tag!.GetType().Name, Is.EqualTo("BootstrapSelectProduct"));
```

For every sample item, use reflection on the demo-only `Tag` object to verify its `Name` property equals `BootstrapSelectItem.Text`. This avoids making demo implementation types public just for tests.

- [ ] **Step 2: Run the demo contract test and observe failure**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDemoContractTests"
```

Expected: FAIL because the product sample is not present.

- [ ] **Step 3: Add the demo-only product type**

```csharp
internal sealed class BootstrapSelectProduct
{
    internal BootstrapSelectProduct(int id, string name, string unit, decimal unitPrice, decimal stockQuantity)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
    }

    internal int Id { get; }
    internal string Name { get; }
    internal string Unit { get; }
    internal decimal UnitPrice { get; }
    internal decimal StockQuantity { get; }
}
```

Keep this type in the demo assembly only.

- [ ] **Step 4: Implement the custom renderer completely**

Use composition instead of inheritance:

```csharp
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal sealed class BootstrapSelectProductRenderer : IBootstrapSelectRenderer
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private readonly BootstrapSelectRenderer _defaultRenderer = new BootstrapSelectRenderer();

    public void DrawResult(Graphics graphics, BootstrapSelectResultRenderContext context)
    {
        if (graphics is null) throw new ArgumentNullException(nameof(graphics));
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (context.Item.Tag is not BootstrapSelectProduct product)
        {
            _defaultRenderer.DrawResult(graphics, context);
            return;
        }

        var colors = context.Theme.Colors;
        var highlighted = (context.State & BootstrapSelectRenderState.Highlighted) != 0;
        var selected = (context.State & BootstrapSelectRenderState.Selected) != 0;
        var hot = (context.State & BootstrapSelectRenderState.Hot) != 0;
        var disabled = (context.State & BootstrapSelectRenderState.Disabled) != 0;
        var background = highlighted || selected
            ? colors.Active
            : hot
                ? colors.Hover
                : colors.Surface;

        using (var backgroundBrush = new SolidBrush(background))
        {
            graphics.FillRectangle(backgroundBrush, context.Bounds);
        }

        var horizontalInset = DpiScaler.Scale(8, context.Dpi);
        var verticalInset = DpiScaler.Scale(4, context.Dpi);
        var lineGap = DpiScaler.Scale(1, context.Dpi);
        var contentBounds = Rectangle.Inflate(context.Bounds, -horizontalInset, -verticalInset);
        var secondaryFontSize = Math.Max(6f, context.Font.SizeInPoints - 1f);
        using var secondaryFont = new Font(
            context.Font.FontFamily,
            secondaryFontSize,
            context.Font.Style,
            GraphicsUnit.Point);

        var secondaryHeight = TextRenderer.MeasureText("Ag", secondaryFont).Height;
        var primaryHeight = Math.Max(0, contentBounds.Height - secondaryHeight - lineGap);
        var primaryBounds = new Rectangle(
            contentBounds.Left,
            contentBounds.Top,
            contentBounds.Width,
            primaryHeight);
        var secondaryBounds = new Rectangle(
            contentBounds.Left,
            contentBounds.Bottom - secondaryHeight,
            contentBounds.Width,
            secondaryHeight);

        var primaryColor = disabled ? colors.MutedText : colors.Text;
        var secondaryColor = colors.MutedText;
        var secondaryText = product.Unit
            + " | " + product.UnitPrice.ToString("N0", DisplayCulture)
            + " | Tồn: " + product.StockQuantity.ToString("N0", DisplayCulture);
        const TextFormatFlags flags = TextFormatFlags.Left
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.EndEllipsis
            | TextFormatFlags.SingleLine
            | TextFormatFlags.NoPrefix;

        TextRenderer.DrawText(graphics, product.Name, context.Font, primaryBounds, primaryColor, flags);
        TextRenderer.DrawText(graphics, secondaryText, secondaryFont, secondaryBounds, secondaryColor, flags);
    }

    public void DrawGroupHeader(Graphics graphics, BootstrapSelectGroupRenderContext context)
        => _defaultRenderer.DrawGroupHeader(graphics, context);

    public void DrawSelection(Graphics graphics, BootstrapSelectSelectionRenderContext context)
        => _defaultRenderer.DrawSelection(graphics, context);

    public void DrawChip(Graphics graphics, BootstrapSelectChipRenderContext context)
        => _defaultRenderer.DrawChip(graphics, context);
}
```

This code intentionally mirrors the default background-state precedence, uses theme colors, scales spacing by DPI, disposes its derived font/brush, and delegates non-product/fallback rendering to the framework renderer.

- [ ] **Step 5: Add the product sample to the demo form**

Create a normal Single `BootstrapSelect`:

```csharp
var productSelect = new BootstrapSelect
{
    Name = "productSearchSelect",
    Placeholder = "Tìm sản phẩm...",
    SearchEnabled = true,
    SelectionMode = BootstrapSelectMode.Single,
    ResultRowHeight = 48,
    DropDownWidth = 420,
    Renderer = new BootstrapSelectProductRenderer()
};
```

Populate realistic data:

```csharp
var products = new[]
{
    new BootstrapSelectProduct(1001, "Sữa tươi Vinamilk 100% 1L", "Hộp", 36500m, 128m),
    new BootstrapSelectProduct(1002, "Coca-Cola lon 330ml", "Lon", 10000m, 56m),
    new BootstrapSelectProduct(1003, "Nước khoáng Lavie 500ml", "Chai", 6000m, 240m)
};

foreach (var product in products)
{
    productSelect.Items.Add(new BootstrapSelectItem(product.Id, product.Name)
    {
        Tag = product
    });
}
```

Place the sample in the existing BootstrapSelect demo layout using the same spacing/theme conventions as adjacent samples.

- [ ] **Step 6: Rerun the demo contract test**

Expected: PASS.

- [ ] **Step 7: Commit the demo**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProduct.cs \
        demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProductRenderer.cs \
        demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BootstrapSelectDemoContractTests.cs
git commit -m "demo: add BootstrapSelect product result template"
```

---

## Task 5: Document usage and deliberately review the public API baseline

**Files:**

- Modify: `docs/BOOTSTRAP_SELECT.md`
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

**Interfaces:** The only intended new exported member from this plan is `BootstrapSelect.ResultRowHeight : int`.

- [ ] **Step 1: Document `ResultRowHeight` and renderer templating**

In `docs/BOOTSTRAP_SELECT.md`, document:

```text
ResultRowHeight
Type: int
Default: 32
Unit: logical pixels at 96 DPI
Validation: > 0
Meaning: uniform popup result-row height
```

Add this conceptual mapping, while clearly stating that the APIs are not JavaScript-compatible:

```text
Select2 templateResult        -> IBootstrapSelectRenderer.DrawResult
Select2 templateSelection     -> IBootstrapSelectRenderer.DrawSelection
Custom result metadata        -> BootstrapSelectItem.Tag
```

Include a complete product example using `ResultRowHeight = 48`, `Renderer`, and `Tag`. Explain that provider-backed results work identically when provider-created items carry the metadata in `Tag`.

- [ ] **Step 2: Update component/testing docs**

In `docs/COMPONENTS.md`, add `ResultRowHeight` to the BootstrapSelect property/layout summary and keep variable-height rows explicitly unsupported.

In `docs/TESTING.md`, add manual checks for:

```text
- default 32 logical px and custom 48 logical px;
- Light and Dark themes;
- 100%, 150%, and 200% DPI;
- disabled/hot/highlighted/selected rows;
- long product-name ellipsis;
- Down/PageDown/PageUp/Home/End;
- mouse wheel and click hit testing;
- MaxDropDownHeight and collision behavior;
- closed selected value remains single-line.
```

- [ ] **Step 3: Run the existing public API fingerprint test before editing the approved hash**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~ExportedApiMatchesApprovedV1Baseline"
```

Expected: FAIL and print `Actual fingerprint:` plus the reconstructed exported API.

- [ ] **Step 4: Review the emitted surface**

Confirm the only intended exported delta caused by this plan is:

```text
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSelect
  public property System.Int32 ResultRowHeight
```

If any additional exported type/member appears, correct its visibility first and rerun Step 3. Do not update the baseline until the surface is clean.

- [ ] **Step 5: Add a semantic release-contract test**

```csharp
[Test]
public void BootstrapSelectRowTemplateApiRemainsUniformHeightOnly()
{
    var property = typeof(BootstrapSelect).GetProperty(nameof(BootstrapSelect.ResultRowHeight));

    Assert.Multiple((Action)(() =>
    {
        Assert.That(property, Is.Not.Null);
        Assert.That(property!.PropertyType, Is.EqualTo(typeof(int)));
        Assert.That(typeof(BootstrapSelect).GetEvent("MeasureResultItem"), Is.Null);
        Assert.That(typeof(BootstrapSelect).GetProperty("ItemTemplate"), Is.Null);
    }));
}
```

- [ ] **Step 6: Update the approved fingerprint with the exact emitted value**

After Step 4 confirms the API delta, copy the exact fingerprint printed by the failing test into `ApprovedV1Fingerprint`. The plan deliberately does not contain or guess that future compiled hash.

Update `docs/PUBLIC_API_BASELINE.md` with the same reviewed hash and a sentence that BootstrapSelect adds only `ResultRowHeight : int`; no existing signature, renderer method, template/measurement type, or `AssemblyVersion` changed.

- [ ] **Step 7: Rerun release baseline tests**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

Expected: PASS.

- [ ] **Step 8: Commit docs/baseline**

```bash
git add docs/BOOTSTRAP_SELECT.md \
        docs/COMPONENTS.md \
        docs/TESTING.md \
        docs/PUBLIC_API_BASELINE.md \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs
git commit -m "docs: document BootstrapSelect custom result rendering"
```

---

## Task 6: Full verification and manual acceptance

**Files:**

- No planned changes. Fix only verified regressions and keep each fix scoped to its originating task.

**Interfaces:** Final public API and behavior from Tasks 1-5.

- [ ] **Step 1: Build core for both targets**

```bash
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
```

Expected: PASS.

- [ ] **Step 2: Build demo for both targets**

```bash
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release -f net48
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release -f net8.0-windows
```

Expected: PASS.

- [ ] **Step 3: Run BootstrapSelect-focused tests for both targets**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapSelect"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect"
```

Expected: PASS.

- [ ] **Step 4: Run complete tests for both targets**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: PASS.

- [ ] **Step 5: Manual Product Search acceptance**

Verify all of these in the demo:

```text
1. Each product popup row shows exactly two visual lines at ResultRowHeight = 48.
2. Line 1 is product name.
3. Line 2 is Unit | Unit price | Stock quantity.
4. Long product names ellipsize without overlapping the second line.
5. Hot, highlighted, selected, and disabled states remain readable in Light/Dark.
6. Down, Up, Home, End, PageDown, PageUp, Enter, mouse wheel, and mouse click target the expected row.
7. Async paging/navigation regressions from plan 003 remain fixed.
8. Changing ResultRowHeight while open keeps the popup open and recomputes bounds.
9. MaxDropDownHeight and screen collision handling still work.
10. Selected product displays as normal single-line text after closing.
11. 100%, 150%, and 200% DPI preserve spacing, text clipping, scrolling, and hit testing.
```

- [ ] **Step 6: Final diff/API/resource review**

Confirm:

```text
- no product-specific type exists in the core assembly;
- IBootstrapSelectRenderer signatures did not change;
- no variable-height or measurement API exists;
- no external package was added;
- the only intended exported API addition is ResultRowHeight;
- the reviewed fingerprint in test/docs matches the compiled assembly;
- no bin/, obj/, package, or IDE-state files are staged.
```

Do not create an empty verification commit. Commit only an actual correction discovered during verification.

---

## Acceptance criteria

- `BootstrapSelect.ResultRowHeight` exists, defaults to `32`, rejects non-positive values, and uses logical pixels.
- Existing callers retain current appearance/layout without setting the new property.
- Custom height is DPI-scaled and drives row painting bounds, preferred size, visible-range math, hit testing, mouse wheel, and PageUp/PageDown through the existing constant-height model.
- Changing the height while open reapplies presentation/repositions popup without recreating it solely for that property change.
- `IBootstrapSelectRenderer` and render-context signatures are unchanged.
- Variable-height rows, measure callbacks, hosted row controls, and HTML-like templates remain unsupported.
- The demo contains a product search sample whose `BootstrapSelectItem.Text` is product name and whose `Tag` contains unit, price, and stock metadata.
- Product popup results render two lines with theme-aware state colors and DPI-aware spacing.
- Closed selection/group/chip behavior delegates to the framework default renderer in the demo.
- Paging/navigation behavior from plan `20260901-003` remains stable.
- Public API review confirms the only intended exported addition is `BootstrapSelect.ResultRowHeight` and the fingerprint is deliberately updated from actual compiled output.
- Core, demo, and test projects build/pass for both `net48` and `net8.0-windows`.
- Manual Light/Dark and 100%/150%/200% DPI acceptance passes.

---

## Self-review checklist

Before declaring implementation complete:

- [ ] Every result-row vertical calculation reads effective `RowHeight`; the only remaining hard-coded logical `32` values in this path are the documented default and tests that intentionally assert it.
- [ ] `BootstrapSelectResultLayout` remains generic constant-height math.
- [ ] Presentation geometry changes preserve current logical results/navigation state.
- [ ] Demo-owned GDI objects are disposed; caller-owned context/data objects are not disposed.
- [ ] Product/culture formatting remains demo-only.
- [ ] Public docs and fingerprint match actual compiled API.
- [ ] No unfinished marker or guessed API hash is committed.
