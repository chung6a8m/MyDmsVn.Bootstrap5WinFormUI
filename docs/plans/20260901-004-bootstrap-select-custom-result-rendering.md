# BootstrapSelect Custom Result Rendering and Product Search Plan

> **For agentic workers:** REQUIRED SUB-SKILLS: Use `superpowers:test-driven-development` while implementing each task and `superpowers:verification-before-completion` before claiming the feature is complete. Use `superpowers:systematic-debugging` if a focused test fails for a reason different from the behavior frozen below. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend `BootstrapSelect` with a configurable, DPI-aware **uniform popup result-row height** so callers can use the existing renderer abstraction for Select2-style custom results, including a two-line product search result whose first line is product name and second line is `Unit | Unit price | Stock quantity`.

**Architecture:** Keep `IBootstrapSelectRenderer` as the only public custom-paint extension point and keep `BootstrapSelectItem.Tag` as caller-owned metadata. Add one public logical-pixel property, `BootstrapSelect.ResultRowHeight`, defaulting to `32`, then pass it through `BootstrapSelectDropDownController -> BootstrapSelectDropDownContent -> BootstrapSelectResultsView`. `BootstrapSelectResultsView` continues to use one constant row height for every row, so existing O(1) scrolling, hit testing, PageUp/PageDown, near-end paging, and visible-range calculations remain intact. The internal dropdown controller owns the popup's effective DPI and subscribes to the inherited `Control.DpiChangedAfterParent` event so an already-created/open popup reapplies presentation before it is repositioned when the owner moves between monitors. Do **not** add a `BootstrapSelect.OnDpiChangedAfterParent` override because protected declared members participate in the public API fingerprint. The demo implements a product renderer through `IBootstrapSelectRenderer`, measures secondary text with the same `Graphics` device context used for drawing, and delegates group headers, the closed single-selection surface, and chips to `BootstrapSelectRenderer` by composition.

**Tech Stack:** C# 12, Windows Forms owner painting, `System.Drawing`, `TextRenderer`, existing `DpiScaler`/theme infrastructure, inherited WinForms `DpiChangedAfterParent`, `IBootstrapSelectRenderer`, `BootstrapSelectItem.Tag`, `net48;net8.0-windows`, NUnit 4, STA WinForms tests.

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
- When the owner DPI changes after the popup has been created, the controller reapplies **all popup presentation metrics** at the new DPI before computing/moving bounds. Search-host height, result-row height, renderer contexts, surface radius/theme metrics, and popup placement therefore use one coherent effective DPI.
- DPI refresh is implemented by subscribing inside `BootstrapSelectDropDownController` to inherited `Control.DpiChangedAfterParent`; it does not add a new declared public/protected member to `BootstrapSelect`.
- The property does not change the closed control height, chip height, search textbox logical height, fonts, renderer ownership, item ownership, or selection semantics.
- `IBootstrapSelectRenderer` signatures remain unchanged.
- No measure callback, per-item height, hosted row control, template object model, or HTML-like rendering layer is added.

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
| `ResultRowHeight = 48`, DPI = 192 | Effective row height is `DpiScaler.Scale(48, 192)`. |
| Setter receives `0` or a negative value | Throw `ArgumentOutOfRangeException`; previous valid value remains. |
| Property changes before popup creation | First popup uses the configured height. |
| Property changes after popup creation while closed | Next open uses the new height without recreating the popup solely for this change. |
| Property changes while popup is open | Popup remains open, result geometry updates, and overlay bounds are recomputed/repositioned. |
| Popup is created, then owner DPI changes while closed | Cached popup presentation is reapplied at the new DPI; next open uses the new metrics without recreating solely for DPI. |
| Popup is open, then owner DPI changes 96 -> 144 -> 192 | Popup remains open; presentation is reapplied first, bounds are then recomputed/repositioned, and creation count is unchanged. |
| Mouse click with custom height | Hit testing identifies the row using the custom effective height. |
| Mouse wheel away from the scroll boundary | Scroll delta is row-based and uses the effective custom row height. |
| Mouse wheel reaches the last viewport | Offset is clamped to `totalHeight - viewportHeight`; the clamped value is **not required** to be a multiple of row height. |
| PageDown/PageUp | Number of rows moved is based on `ClientSize.Height / RowHeight`. |
| Async paging | Existing near-end and navigation-preservation behavior remains correct. |
| Group/loading/empty/error/create-value row | Uses the same uniform row height. |
| Custom renderer | Can read `context.Item.Tag` without the control interpreting or owning the object. |
| Product secondary text measurement | Uses the same `Graphics`/device context as `TextRenderer.DrawText`; primary and secondary bounds do not overlap at 96/144/192 DPI. |
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
- [ ] Preserve reset/preserve navigation semantics from plan `20260901-003`; presentation/DPI geometry changes must not reset logical result/navigation state.
- [ ] Subscribe to inherited `_owner.DpiChangedAfterParent` from the internal controller and unsubscribe in `Dispose`. Do **not** add a declared protected DPI override to `BootstrapSelect`, because the release fingerprint intentionally includes protected declared surface.
- [ ] Keep one controller-owned effective DPI and use it for both `ApplyPresentation` and `ComputeBounds`; do not read one DPI for painting and another for placement during one update.
- [ ] Mouse-wheel tests must distinguish an ordinary row-sized movement from the final clamp boundary. Never change correct clamp math merely to make `ScrollOffset % RowHeight == 0` true.
- [ ] Product text measurement must use the same `Graphics` device context as drawing. Do not use `TextRenderer.MeasureText(string, Font)` for the secondary line.
- [ ] Use TDD: failing focused test -> observe failure -> minimal implementation -> focused pass -> broader regression tests.
- [ ] Because this is an additive public API change after the release-candidate baseline, review the emitted API surface before updating the approved fingerprint.

---

## File structure and responsibilities

### Core files to modify

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
  - Add backing field and public `ResultRowHeight` property only.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`
  - Add a narrow private helper for reapplying/repositioning popup presentation after layout-property changes.
  - Add test-only forwarding/accessors only when needed; keep them internal.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
  - Forward `_owner.ResultRowHeight` into content presentation.
  - Own the popup effective DPI.
  - Subscribe/unsubscribe inherited `_owner.DpiChangedAfterParent`.
  - Reapply presentation before repositioning on DPI change.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
  - Forward logical result row height into the results view.
  - Expose effective results-row height internally if required for deterministic tests.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`
  - Store logical row height and derive effective `RowHeight` from DPI.
  - Add a narrow internal wheel-scroll helper so row-step and clamp behavior can be tested without `SendKeys`.

### Core files to inspect, not change by default

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultLayout.cs`
  - Already accepts `rowHeight`; keep it generic and constant-height.
  - Preserve the existing clamp rule `maxOffset = max(0, totalHeight - viewportHeight)` exactly.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectRenderer.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderContexts.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderer.cs`
  - Existing renderer contract is sufficient.

### Demo files

- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProduct.cs`
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProductRenderer.cs`
- Create when useful for deterministic geometry tests: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProductResultLayout.cs`
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

## Task 1: Record the uniform-row-height and DPI-lifecycle design change

**Files:**

- Modify: `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`

- [ ] **Step 1: Extend the approved public surface**

Add only:

```csharp
public int ResultRowHeight { get; set; }
```

Place it beside `DropDownWidth` / `MaxDropDownHeight` / `MaximumSelectionRows`.

- [ ] **Step 2: Add exact semantics**

Document:

```text
ResultRowHeight is uniform for every popup result row.
The unit is logical pixels at 96 DPI.
The default is 32.
The value must be greater than zero.
The effective height is DPI-scaled through DpiScaler.
Changing it while open reapplies presentation and repositions the popup.
An owner DPI transition also reapplies popup presentation before repositioning.
The DPI lifecycle implementation remains internal and adds no new declared public/protected BootstrapSelect member.
Variable-height rows and renderer measurement callbacks remain unsupported.
```

- [ ] **Step 3: Preserve non-goals**

Retain `variable-height result rows` and `HTML-like templates`. State explicitly that caller-configurable **uniform** row height does not imply per-item measurement.

- [ ] **Step 4: Commit**

```bash
git add docs/superpowers/specs/2026-08-29-bootstrap-select-design.md
git commit -m "docs: define BootstrapSelect result row height"
```

---

## Task 2: Add `ResultRowHeight`, propagate it, and make popup DPI refresh coherent

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`

**Interfaces:**

- New exported member: `public int BootstrapSelect.ResultRowHeight { get; set; }`.
- Internal presentation overloads receive logical row height and effective DPI.
- No new public/protected DPI member is declared on `BootstrapSelect`.

- [ ] **Step 1: Write failing property tests**

Cover default `32`, valid assignment, and rejection of `0/-1` while retaining the prior valid value.

- [ ] **Step 2: Add the property**

```csharp
private int _resultRowHeight = 32;

[Category("Layout")]
[DefaultValue(32)]
public int ResultRowHeight
{
    get => _resultRowHeight;
    set
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Result row height must be positive.");
        if (_resultRowHeight == value) return;

        _resultRowHeight = value;
        RefreshDropDownPresentationAndLayout();
    }
}
```

- [ ] **Step 3: Add the popup refresh helper**

```csharp
private void RefreshDropDownPresentationAndLayout()
{
    if (_dropDownController is null) return;

    _dropDownController.ApplyPresentation();
    if (_dropDownController.IsOpen)
    {
        _dropDownController.Reposition();
    }
}
```

- [ ] **Step 4: Make results-view row height configurable**

```csharp
private int _logicalRowHeight = 32;

internal int RowHeight => DpiScaler.Scale(_logicalRowHeight, _dpi);

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

If plan 003 has introduced explicit reset/preserve navigation modes, retain them. Presentation changes may clamp/reveal the existing highlight but must not replace results or choose a new logical highlight.

- [ ] **Step 5: Forward height through content/controller**

`BootstrapSelectDropDownContent.ApplyPresentation(...)` receives `logicalResultRowHeight` and calls:

```csharp
_resultsView.ApplyPresentation(renderer, theme, dpi, logicalResultRowHeight);
```

- [ ] **Step 6: Add one controller-owned effective DPI**

Do not let `ApplyPresentation()` and `ComputeBounds()` independently sample owner DPI during one transition. Use one field:

```csharp
private int _effectiveDpi = DpiScaler.DefaultDpi;

private int ResolveOwnerDpi()
    => _owner.DeviceDpi > 0 ? _owner.DeviceDpi : DpiScaler.DefaultDpi;

internal void ApplyPresentation()
    => ApplyPresentation(ResolveOwnerDpi());

internal void ApplyPresentation(int dpi)
{
    if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
    if (_surface is null || _content is null) return;

    _effectiveDpi = dpi;
    var theme = BootstrapThemeManager.CurrentTheme;
    _surface.LogicalBorderRadius = _owner.BorderRadius;
    _surface.ApplyTheme(theme, dpi);
    _content.Font = _owner.Font;
    _content.SearchEnabled = _owner.SearchEnabled;
    _content.ApplyPresentation(_owner.Renderer, theme, dpi, _owner.ResultRowHeight);
}
```

`ComputeBounds()` must use `_effectiveDpi` rather than independently reading `_owner.DeviceDpi`.

- [ ] **Step 7: Subscribe to inherited DPI lifecycle without widening declared API**

In the controller constructor/disposal path:

```csharp
_owner.DpiChangedAfterParent += OnOwnerDpiChangedAfterParent;
```

and:

```csharp
_owner.DpiChangedAfterParent -= OnOwnerDpiChangedAfterParent;
```

Handle it internally:

```csharp
private void OnOwnerDpiChangedAfterParent(object? sender, EventArgs e)
{
    ApplyOwnerDpiChange(ResolveOwnerDpi());
}

internal void ApplyOwnerDpiChange(int dpi)
{
    if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
    if (_dropDown is null) return;

    ApplyPresentation(dpi);
    if (_isOpen)
    {
        Reposition();
    }
}
```

Do **not** add `protected override void OnDpiChangedAfterParent(...)` to `BootstrapSelect`.

- [ ] **Step 8: Add deterministic DPI lifecycle tests**

Using existing STA popup patterns, open one select with `ResultRowHeight = 48`, capture `DropDownCreationCountForTest`, then drive the controller's internal DPI-change core through `96 -> 144 -> 192`. Assert after each transition:

```text
popup remains open
creation count is unchanged
effective result row height == DpiScaler.Scale(48, dpi)
bounds are recomputed and remain within working-area / MaxDropDownHeight constraints
```

Also test a created-but-closed popup: DPI change updates presentation and the next open uses the new row height without popup recreation.

Do not fake a Windows monitor switch in a unit test. The real monitor transition remains part of Task 6 manual acceptance; automated tests freeze the shared DPI-change core that the inherited event calls.

- [ ] **Step 9: Run focused tests and commit**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectTests|FullyQualifiedName~BootstrapSelectResultsViewTests|FullyQualifiedName~BootstrapSelectPopupTests"
```

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs \
        src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs \
        src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs \
        src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs \
        src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs
git commit -m "feat: add BootstrapSelect result row height"
```

---

## Task 3: Lock sizing, hit testing, wheel scrolling, paging, and open-popup behavior

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Modify `BootstrapSelectResultsView.cs` only for the narrow testable wheel helper described below.

- [ ] **Step 1: Prove existing constant-height layout supports 48px rows**

```csharp
var layout = BootstrapSelectResultLayout.Create(
    rowCount: 100,
    rowHeight: 48,
    viewportHeight: 144,
    scrollOffset: 96);

Assert.That(layout.FirstVisibleIndex, Is.EqualTo(2));
Assert.That(layout.LastVisibleIndex, Is.EqualTo(4));
Assert.That(layout.HitTestIndex(0), Is.EqualTo(2));
Assert.That(layout.HitTestIndex(143), Is.EqualTo(4));
```

No production change to `BootstrapSelectResultLayout` should be required.

- [ ] **Step 2: Add preferred-size and PageDown/PageUp tests**

With at least eight selectable rows and a 48 logical-pixel row at 96 DPI, assert:

```text
GetPreferredSize(...).Height == visibleRowCount * 48
Page(1) moves by max(1, ClientSize.Height / 48) selectable rows
Page(-1) uses the same page-row calculation in reverse
```

Use internal methods directly, not `SendKeys`.

- [ ] **Step 3: Extract a deterministic wheel helper**

Keep the production behavior unchanged while making it directly testable:

```csharp
internal void ScrollByWheelDelta(int delta, int scrollLines)
{
    var rows = Math.Max(1, scrollLines);
    var deltaRows = delta > 0 ? -rows : rows;
    var requested = Math.Max(0, _scrollOffset + (deltaRows * RowHeight));
    SetScrollOffset(requested);
}

protected override void OnMouseWheel(MouseEventArgs e)
{
    base.OnMouseWheel(e);
    ScrollByWheelDelta(e.Delta, SystemInformation.MouseWheelScrollLines);
}
```

- [ ] **Step 4: Test ordinary wheel movement and boundary clamping separately**

Ordinary movement: choose geometry where no clamp occurs and assert the offset changes by the expected whole-row amount.

Boundary case: freeze the case that disproves the old incorrect invariant:

```csharp
var layout = BootstrapSelectResultLayout.Create(
    rowCount: 10,
    rowHeight: 48,
    viewportHeight: 130,
    scrollOffset: int.MaxValue);

Assert.That(layout.ScrollOffset, Is.EqualTo(350)); // 10 * 48 - 130
Assert.That(layout.ScrollOffset % 48, Is.Not.EqualTo(0));
```

Then drive `ScrollByWheelDelta` to the same end boundary and assert `ScrollOffset == 350`.

**Do not** assert that every clamped offset is a multiple of `RowHeight`, and do not round `350` to `336` or `384` merely to manufacture row alignment. The authoritative invariant is `BootstrapSelectResultLayout` clamping.

- [ ] **Step 5: Add dropdown-content preferred-size tests**

At 96 DPI with three rows and `logicalResultRowHeight = 48`, verify:

```text
results contribution = 3 * 48
search-enabled preferred height = searchHostHeight + results contribution
search-disabled preferred height = results contribution
```

Derive search host height from active theme metrics/DPI rather than duplicating a magic number.

- [ ] **Step 6: Add open-popup row-height change regression**

Open a popup with enough items, capture creation count/bounds, set `ResultRowHeight = 48`, and assert:

```text
popup remains open
creation count unchanged
effective result row height updated
bounds recomputed and still respect MaxDropDownHeight / working area
```

- [ ] **Step 7: Run navigation/paging regressions**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectResultsViewTests|FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectPopupTests|FullyQualifiedName~BootstrapSelectProviderIntegrationTests|FullyQualifiedName~BootstrapSelectPagingTests|FullyQualifiedName~BootstrapSelectInteractionTests"
```

Custom geometry must not reintroduce the highlight/scroll reset fixed by plan 003.

- [ ] **Step 8: Commit**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs
git commit -m "test: cover BootstrapSelect custom row geometry"
```

---

## Task 4: Add a two-line Product Search demo renderer with DPI-correct text measurement

**Files:**

- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProduct.cs`
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProductRenderer.cs`
- Create if used: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProductResultLayout.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BootstrapSelectDemoContractTests.cs`

- [ ] **Step 1: Add the demo contract test**

Locate the sample using `Name = "productSearchSelect"` and assert:

```text
ResultRowHeight == 48
renderer type == BootstrapSelectProductRenderer
at least three items exist
every item Tag is product metadata
item.Text equals metadata Name
```

Keep demo metadata types internal.

- [ ] **Step 2: Add the demo-only product type**

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

- [ ] **Step 3: Implement product text layout using the same `Graphics` device context**

The old form below is explicitly forbidden because measurement is detached from the drawing device context:

```csharp
// Do not use this:
TextRenderer.MeasureText("Ag", secondaryFont).Height;
```

Measure with the popup `Graphics` object used for drawing:

```csharp
const TextFormatFlags drawFlags = TextFormatFlags.Left
    | TextFormatFlags.VerticalCenter
    | TextFormatFlags.EndEllipsis
    | TextFormatFlags.SingleLine
    | TextFormatFlags.NoPrefix;

const TextFormatFlags measureFlags = TextFormatFlags.Left
    | TextFormatFlags.SingleLine
    | TextFormatFlags.NoPrefix;

var proposedMeasureSize = new Size(
    Math.Max(1, contentBounds.Width),
    Math.Max(1, contentBounds.Height));

var secondaryHeight = TextRenderer.MeasureText(
    graphics,
    "Ag",
    secondaryFont,
    proposedMeasureSize,
    measureFlags).Height;

secondaryHeight = Math.Min(contentBounds.Height, secondaryHeight);
var primaryHeight = Math.Max(0, contentBounds.Height - secondaryHeight - lineGap);
var primaryBounds = new Rectangle(
    contentBounds.Left,
    contentBounds.Top,
    contentBounds.Width,
    primaryHeight);
var secondaryBounds = new Rectangle(
    contentBounds.Left,
    Math.Min(contentBounds.Bottom, primaryBounds.Bottom + lineGap),
    contentBounds.Width,
    secondaryHeight);
```

Normalize/clamp the final rectangles so both are contained in `contentBounds` and `primaryBounds.Bottom <= secondaryBounds.Top`. If this geometry is easier to test as a small demo-only helper, extract `BootstrapSelectProductResultLayout`; do not move product-specific layout into the core assembly.

- [ ] **Step 4: Implement renderer composition**

`BootstrapSelectProductRenderer.DrawResult` must:

1. Delegate to `BootstrapSelectRenderer` when `Tag` is not `BootstrapSelectProduct`.
2. Mirror the default highlight/selected/hot background precedence.
3. Use theme text/muted colors and disabled state.
4. Scale horizontal/vertical inset and line gap with `DpiScaler`.
5. Create/dispose only renderer-owned `Brush`/derived `Font` objects.
6. Draw both lines with the same `Graphics` object used in Step 3.
7. Delegate `DrawGroupHeader`, `DrawSelection`, and `DrawChip` to the default renderer.

- [ ] **Step 5: Add 96/144/192 geometry coverage**

Use a `Bitmap`/`Graphics` test harness and the demo-only layout helper (or reflection over an internal helper without making it public) to calculate product text bounds at 96, 144, and 192 DPI-equivalent row geometry. Assert:

```text
primary bounds are contained in row content bounds
secondary bounds are contained in row content bounds
primary.Bottom <= secondary.Top
both bounds have non-negative width/height
long primary text can be ellipsized without entering secondary bounds
```

The purpose is to freeze the renderer's device-context-aware layout contract; a pixel-perfect screenshot test is not required.

- [ ] **Step 6: Add the product sample**

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

Populate realistic `BootstrapSelectProduct` objects through `BootstrapSelectItem.Tag`, leaving `BootstrapSelectItem.Text` equal to the product name.

- [ ] **Step 7: Run demo tests/build and commit**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDemoContractTests"
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release -f net8.0-windows
```

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProduct.cs \
        demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProductRenderer.cs \
        demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProductResultLayout.cs \
        demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BootstrapSelectDemoContractTests.cs
git commit -m "demo: add BootstrapSelect product result template"
```

If no separate layout helper file was created, omit it from `git add` rather than creating an empty file.

---

## Task 5: Document usage and deliberately review the public API baseline

**Files:**

- Modify: `docs/BOOTSTRAP_SELECT.md`
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

- [ ] **Step 1: Document `ResultRowHeight` and renderer templating**

Document:

```text
ResultRowHeight
Type: int
Default: 32
Unit: logical pixels at 96 DPI
Validation: > 0
Meaning: uniform popup result-row height
```

Include the conceptual mapping:

```text
Select2 templateResult        -> IBootstrapSelectRenderer.DrawResult
Select2 templateSelection     -> IBootstrapSelectRenderer.DrawSelection
Custom result metadata        -> BootstrapSelectItem.Tag
```

Explain that provider-backed results work identically when provider-created items carry metadata in `Tag`.

- [ ] **Step 2: Document DPI lifecycle and wheel boundary semantics**

`docs/BOOTSTRAP_SELECT.md` / `docs/TESTING.md` must say:

```text
- changing ResultRowHeight while open reapplies presentation/repositions without recreation;
- moving an open popup between DPI contexts reapplies presentation before placement;
- all popup metrics use one effective DPI for each refresh;
- normal wheel increments are row-based;
- the final clamped ScrollOffset may be a partial-row offset because maxOffset = totalHeight - viewportHeight;
- product secondary text is measured and drawn with the same device context.
```

- [ ] **Step 3: Run the public API fingerprint test before editing the approved hash**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~ExportedApiMatchesApprovedV1Baseline"
```

Expected: FAIL and print the actual fingerprint/exported surface.

- [ ] **Step 4: Review emitted surface**

The only intended exported delta is:

```text
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSelect
  public property System.Int32 ResultRowHeight
```

Specifically verify there is **no** newly declared protected `OnDpiChangedAfterParent` override or other DPI member on `BootstrapSelect`. The controller's subscription to the inherited event is implementation-only.

If any extra exported member appears, correct visibility/design first; do not approve the hash.

- [ ] **Step 5: Keep a semantic release-contract test**

Assert `ResultRowHeight` exists and remains `int`, while `MeasureResultItem` and `ItemTemplate` remain absent. Also assert no newly declared `BootstrapSelect` DPI member was introduced by this plan if the baseline helper can express that check cleanly.

- [ ] **Step 6: Update the approved hash from actual compiled output only**

Copy the exact fingerprint printed by the failing compatibility test after Step 4 has confirmed the intended surface. Update `docs/PUBLIC_API_BASELINE.md` with the same reviewed value and state that the DPI lifecycle implementation is internal.

- [ ] **Step 7: Run release baseline tests and commit**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

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

- [ ] **Step 1: Build core for both targets**

```bash
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
```

- [ ] **Step 2: Build demo for both targets**

```bash
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release -f net48
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release -f net8.0-windows
```

- [ ] **Step 3: Run BootstrapSelect-focused tests for both targets**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapSelect"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect"
```

- [ ] **Step 4: Run complete tests for both targets**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

- [ ] **Step 5: Manual Product Search acceptance**

Verify:

```text
1. Each product popup row shows exactly two visual lines at ResultRowHeight = 48.
2. Line 1 is product name.
3. Line 2 is Unit | Unit price | Stock quantity.
4. Long product names ellipsize without overlapping line 2.
5. Hot/highlighted/selected/disabled states remain readable in Light/Dark.
6. Down/Up/Home/End/PageDown/PageUp/Enter/mouse wheel/mouse click target expected rows.
7. Async paging/navigation regressions from plan 003 remain fixed.
8. Changing ResultRowHeight while open keeps the popup open and recomputes bounds.
9. MaxDropDownHeight and collision handling remain correct.
10. Selected product displays normal single-line text after closing.
11. At 100%, 150%, and 200% DPI, spacing/text clipping/hit testing remain correct.
12. With the popup open, move the owner Form between monitors with different DPI (where available): popup stays open, row/search/surface metrics rescale, bounds reposition, and no stale-DPI painting remains.
13. Move back to the original DPI and verify scaling reverses without popup recreation or navigation reset.
14. At the last scroll viewport, wheel scrolling reaches the true clamped end even when the final offset is not row-aligned.
```

- [ ] **Step 6: Final diff/API/resource review**

Confirm:

```text
- no product-specific type exists in the core assembly;
- IBootstrapSelectRenderer signatures did not change;
- no variable-height or measurement API exists;
- no external package was added;
- BootstrapSelect declares no new protected DPI override;
- controller subscribes/unsubscribes DpiChangedAfterParent safely;
- ComputeBounds and presentation use the same controller-owned effective DPI;
- the only intended exported API addition is ResultRowHeight;
- the reviewed fingerprint matches the compiled assembly;
- no bin/, obj/, package, or IDE-state files are staged.
```

Do not create an empty verification commit. Commit only an actual correction discovered during verification.

---

## Acceptance criteria

- `BootstrapSelect.ResultRowHeight` exists, defaults to `32`, rejects non-positive values, and uses logical pixels.
- Existing callers retain current appearance/layout without setting the new property.
- Custom height is DPI-scaled and drives painting bounds, preferred size, visible-range math, hit testing, wheel scrolling, and PageUp/PageDown through the existing constant-height model.
- Changing height while open reapplies presentation/repositions without recreation.
- Owner DPI transitions reapply popup presentation before placement, with one controller-owned effective DPI used consistently for search metrics, row geometry, render contexts, surface theme/radius, and bounds.
- DPI lifecycle uses inherited `DpiChangedAfterParent` subscription from internal code and adds no declared public/protected `BootstrapSelect` DPI member.
- Ordinary wheel increments are row-based, while the final clamp remains the exact `totalHeight - viewportHeight` even when that value is not divisible by `RowHeight`.
- `IBootstrapSelectRenderer` and render-context signatures are unchanged.
- Variable-height rows, measure callbacks, hosted row controls, and HTML-like templates remain unsupported.
- Product metadata remains demo-only in `BootstrapSelectItem.Tag`.
- Product results render two lines with theme-aware colors and DPI-aware spacing.
- Product secondary text is measured with the same `Graphics` device context used to draw it, and automated 96/144/192 geometry checks prove the two line rectangles do not overlap.
- Paging/navigation behavior from plan `20260901-003` remains stable.
- Public API review confirms the only intended exported addition is `BootstrapSelect.ResultRowHeight` and the fingerprint is updated only from actual compiled output.
- Core, demo, and tests pass for `net48` and `net8.0-windows`.

---

## Self-review checklist

Before declaring implementation complete:

- [ ] Every result-row vertical calculation reads effective `RowHeight`; hard-coded logical `32` remains only as the documented/default value and intentional tests.
- [ ] `BootstrapSelectResultLayout` remains generic constant-height math and its clamp semantics are unchanged.
- [ ] No test incorrectly requires the final clamped scroll offset to be row-aligned.
- [ ] Presentation/DPI changes preserve logical result/navigation state.
- [ ] The internal controller unsubscribes `DpiChangedAfterParent` during disposal.
- [ ] No declared protected DPI override was added to `BootstrapSelect`.
- [ ] Popup `ApplyPresentation` and `ComputeBounds` use the same effective DPI for one transition.
- [ ] Demo-owned GDI objects are disposed; caller-owned context/data objects are not disposed.
- [ ] Product secondary-line measurement uses the same `Graphics` device context as drawing.
- [ ] 96/144/192 product layout tests prove primary/secondary bounds are contained and non-overlapping.
- [ ] Product/culture formatting remains demo-only.
- [ ] Public docs and fingerprint match actual compiled API.
- [ ] No unfinished marker or guessed API hash is committed.
