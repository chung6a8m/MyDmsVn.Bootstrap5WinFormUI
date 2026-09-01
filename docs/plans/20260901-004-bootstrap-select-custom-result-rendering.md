# BootstrapSelect Custom Result Rendering and Product Search Plan

> **For agentic workers:** REQUIRED SUB-SKILLS: Use `superpowers:test-driven-development` while implementing each task and `superpowers:verification-before-completion` before claiming the feature is complete. Use `superpowers:systematic-debugging` if a focused test fails for a reason different from the behavior frozen below. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the existing `BootstrapSelect` renderer architecture with a configurable, DPI-aware **uniform result-row height** so callers can build Select2-style custom result templates such as a two-line product search result (`Product name` + `Unit | Unit price | Stock quantity`) without introducing variable-height rows, hosted row controls, or HTML-like templates.

**Architecture:** Preserve `IBootstrapSelectRenderer` as the sole public custom-paint extension point and preserve `BootstrapSelectItem.Tag` as caller-owned metadata. Add one public logical-pixel property, `BootstrapSelect.ResultRowHeight`, defaulting to `32`, and flow it through the existing `BootstrapSelectDropDownController -> BootstrapSelectDropDownContent -> BootstrapSelectResultsView` presentation path. `BootstrapSelectResultsView` continues to use one constant row height for every row in a result set, so scrolling, hit testing, PageUp/PageDown, near-end paging, and visible-range calculations remain O(1). Add a demo-only product model and renderer that customizes `DrawResult` while delegating group headers, the closed single-selection surface, and multi-selection chips to the framework default renderer.

**Tech Stack:** C# 12, Windows Forms owner painting, `System.Drawing`, `TextRenderer`, existing Bootstrap theme/DPI infrastructure, `IBootstrapSelectRenderer`, `BootstrapSelectItem.Tag`, `net48;net8.0-windows`, NUnit 4, STA WinForms tests.

**Spec:** `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`

**Related plans and docs:**

- `docs/plans/20260829-005-bootstrap-select.md`
- `docs/plans/20260901-002-bootstrap-select-popup-sizing-fix.md`
- `docs/plans/20260901-003-bootstrap-select-popup-lifecycle-and-navigation-fix.md`
- `docs/BOOTSTRAP_SELECT.md`
- `docs/PUBLIC_API_BASELINE.md`

---

## Design delta approved by this plan

The original BootstrapSelect design already approves:

- custom result and selection rendering through `IBootstrapSelectRenderer`;
- caller-owned arbitrary metadata through `BootstrapSelectItem.Tag`;
- an owner-rendered results viewport;
- DPI-aware geometry;
- constant-height result virtualization/layout.

The original design explicitly excludes **variable-height result rows** and HTML-like templates. This plan does **not** remove either exclusion.

The additive design change is deliberately narrower:

```csharp
public int ResultRowHeight { get; set; } // logical pixels, default 32
```

Contract:

- The value is expressed in logical 96-DPI pixels, matching other logical layout properties in the control.
- Default is `32`, preserving current appearance and popup sizing for existing callers.
- Values less than or equal to zero throw `ArgumentOutOfRangeException`.
- The effective device-pixel height is `DpiScaler.Scale(ResultRowHeight, effectiveDpi)`.
- Every result row in one `BootstrapSelect` uses the same effective height, including item rows, group headers, loading/empty/error rows, and create-value rows.
- Changing the property while the popup is already created must update result layout; if the popup is open, bounds must be recomputed/repositioned immediately.
- The property changes only popup result-row geometry. It does not change the closed selection control height, chip height, search textbox height, font size, or renderer ownership semantics.
- `IBootstrapSelectRenderer` remains unchanged. No measure callback, item-template delegate, hosted-control API, or second renderer abstraction is added.

Recommended product-search presentation at 100% DPI:

```text
┌────────────────────────────────────────────────────────────┐
│ Sữa tươi Vinamilk 100% 1L                                │
│ Hộp | 36.500 | Tồn: 128                                  │
├────────────────────────────────────────────────────────────┤
│ Coca-Cola lon 330ml                                       │
│ Lon | 10.000 | Tồn: 56                                   │
└────────────────────────────────────────────────────────────┘
```

Recommended demo setting:

```csharp
productSelect.ResultRowHeight = 48;
productSelect.Renderer = new BootstrapSelectProductRenderer();
```

---

## Required behavior matrix

| Scenario | Expected behavior |
| --- | --- |
| Existing caller does not set `ResultRowHeight` | Effective result row remains 32 logical px; existing visual/layout behavior is unchanged. |
| `ResultRowHeight = 48` at 96 DPI | Every result row is 48 device px high. |
| `ResultRowHeight = 48` at 144 DPI | Every result row is DPI-scaled through `DpiScaler`; no unscaled 48-pixel geometry remains in results layout. |
| Setter receives `0` or negative value | Throw `ArgumentOutOfRangeException`; keep previous valid value. |
| Property changes before popup creation | First popup uses the new height. |
| Property changes after popup creation but while closed | Next open uses the new height without recreating the popup solely for this property. |
| Property changes while popup is open | Result viewport and popup preferred height are recomputed; open popup is repositioned using existing popup-sizing/placement logic. |
| Mouse hit testing with custom height | Clicking vertical position N activates the row computed using the custom effective height. |
| Mouse wheel scrolling with custom height | Scroll increment remains row-based and uses the custom effective height. |
| Up/Down/Home/End with custom height | Existing logical navigation behavior is unchanged. |
| PageDown/PageUp with custom height | Page size is derived from viewport height divided by the custom effective row height. |
| Near-end async paging with custom height | Existing near-end threshold semantics remain based on visible rows; custom height must not reintroduce navigation reset bugs. |
| Group/loading/empty/error/create-value rows | Use the same uniform custom height; no variable-height special case. |
| Custom renderer reads `context.Item.Tag` | Caller metadata is available without the control interpreting or owning it. |
| Closed single-selection surface | Default demo behavior shows `BootstrapSelectItem.Text`, not the two-line popup template. |
| Multiple-selection chips | Existing chip renderer behavior is preserved unless the caller customizes `DrawChip` themselves. |

---

## Global constraints

- [ ] Before product-code changes, read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, the BootstrapSelect section in `docs/COMPONENTS.md`, and all related plans/docs listed above.
- [ ] Treat `docs/plans/20260901-003-bootstrap-select-popup-lifecycle-and-navigation-fix.md` as a prerequisite when it has not yet been implemented. Both plans modify `BootstrapSelectResultsView`, `BootstrapSelectDropDownContent`, and `BootstrapSelectDropDownController`; implement/rebase plan 003 first so this feature does not overwrite reset/preserve navigation semantics.
- [ ] Preserve `net48;net8.0-windows` with one shared code path. Do not use APIs unavailable on `net48` unless already isolated behind repository compatibility infrastructure.
- [ ] Keep `ResultRowHeight` uniform per control. Do not implement per-item measuring, variable-height rows, a Fenwick tree/prefix-height index, hosted controls, HTML/template parsing, or a general layout engine.
- [ ] Do not change the signatures of `IBootstrapSelectRenderer.DrawResult`, `DrawGroupHeader`, `DrawSelection`, or `DrawChip`.
- [ ] Do not unseal `BootstrapSelectRenderer` merely to support the demo. The demo renderer should use interface implementation plus composition/delegation for the methods it does not customize.
- [ ] Keep `BootstrapSelectItem.Tag` caller-owned and untyped. Do not add product-specific fields to `BootstrapSelectItem` or product-specific API to the core library.
- [ ] Do not add an external dependency for formatting, templating, drawing, or layout.
- [ ] Reuse `DpiScaler`, `BootstrapTheme`, and existing theme colors. Do not hard-code semantic colors in the core control or demo renderer where theme tokens exist.
- [ ] Dispose every demo-owned `Font`, `Brush`, or other GDI resource created during painting. Do not dispose `context.Font`, `context.Theme`, `context.Item`, `context.Item.Tag`, or caller-owned renderer instances.
- [ ] Preserve the popup-sizing correction from plan `20260901-002`: presentation/result-size changes while open must end with `Reposition()` so the overlay stays within placement/collision bounds.
- [ ] Preserve the navigation semantics from plan `20260901-003` if already implemented: changing presentation geometry must not silently replace `PreserveNavigation` with reset semantics.
- [ ] Add failing tests before each behavior change. Run the focused failing test, implement the minimum change, then rerun the focused test before broadening verification.
- [ ] Because `ResultRowHeight` is an additive public API change after the release-candidate baseline, the public API fingerprint must be deliberately reviewed and updated only after confirming the exported diff contains exactly the intended addition.

---

## File structure and responsibilities

### Core product files expected to change

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
  - Add `_resultRowHeight = 32`.
  - Add public `ResultRowHeight` with XML docs, `[Category("Layout")]`, `[DefaultValue(32)]`, validation, and presentation refresh.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`
  - Add a narrow private helper that reapplies popup presentation and repositions the popup when a layout-affecting public property changes.
  - Keep popup lifetime/input behavior unchanged.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
  - Pass logical `ResultRowHeight` into popup content presentation.
  - Preserve the existing `ApplyPresentation() -> Reposition()` behavior for an already-open popup through the owner helper.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
  - Accept logical result row height in `ApplyPresentation` and forward it to the results view.
  - Continue computing preferred popup height from `_resultsView.GetPreferredSize(...)`.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`
  - Store logical row height.
  - Compute `RowHeight` from logical height and current DPI instead of hard-coding `DpiScaler.Scale(32, _dpi)`.
  - Keep layout, scrolling, hit testing, paging, and near-end checks dependent on the single `RowHeight` property.

### Core files to inspect, but not change by default

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultLayout.cs`
  - Already accepts `rowHeight` as an argument and should remain generic constant-height math.

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/IBootstrapSelectRenderer.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderContexts.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelectRenderer.cs`
  - Existing custom rendering contract is sufficient; no public signature change is planned.

### Demo files expected to change

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs`
  - Add a product-search sample using the normal `BootstrapSelect` API.

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProduct.cs` **(new demo-only type)**
  - Hold product ID, name, unit, unit price, and stock quantity for the sample.

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProductRenderer.cs` **(new demo-only renderer)**
  - Implement two-line popup result drawing.
  - Delegate group/selection/chip rendering to `BootstrapSelectRenderer`.

### Tests expected to change

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs`
  - Public property default/validation tests.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
  - Custom row-height layout, preferred size, paging, and DPI tests.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
  - Verify preferred size reflects custom row height with/without search host.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
  - Verify open-popup bounds update when `ResultRowHeight` changes.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BootstrapSelectDemoContractTests.cs`
  - Freeze the product demo configuration and renderer wiring without relying on pixel-perfect screenshots.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
  - Review the additive property and update the approved API fingerprint only after inspecting the emitted API diff.

### Documentation expected to change

- `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`
  - Record the uniform configurable row-height design delta before core API implementation.

- `docs/BOOTSTRAP_SELECT.md`
  - Document `ResultRowHeight`, `Renderer`, `Tag`, and a product-search example.

- `docs/COMPONENTS.md`
  - Add the new layout property to the BootstrapSelect public contract summary.

- `docs/TESTING.md`
  - Add manual/custom-render acceptance checks at multiple DPI/theme states.

- `docs/PUBLIC_API_BASELINE.md`
  - Record the reviewed additive `ResultRowHeight` API and new approved fingerprint.

---

## Task 1: Record the uniform-row-height design change before modifying public API

**Files:**

- Modify: `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`

**Interfaces:**

- Consumes: existing approved `BootstrapSelect` design, including renderer abstraction and exclusion of variable-height rows.
- Produces: approved documented contract for `public int ResultRowHeight { get; set; }`, default `32` logical pixels.

- [ ] **Step 1: Extend the public-surface section**

Add the property alongside the existing popup/layout properties:

```csharp
public int DropDownWidth { get; set; }
public int MaxDropDownHeight { get; set; }
public int ResultRowHeight { get; set; }
public int MaximumSelectionRows { get; set; }
```

- [ ] **Step 2: Add explicit row-height semantics**

Document all of the following in the spec:

```text
ResultRowHeight is a uniform logical-pixel height for every popup result row.
Default: 32.
Must be > 0.
Scaled by DpiScaler for the active popup DPI.
Variable-height result rows remain a non-goal.
Renderer measurement callbacks remain a non-goal.
Changing the property while open reapplies presentation and repositions the popup.
```

- [ ] **Step 3: Verify the design delta does not contradict the non-goal list**

Keep `variable-height result rows` and `HTML-like templates` in the explicit non-goals. Add wording that a caller-configurable **uniform** height is supported and is not variable-height templating.

- [ ] **Step 4: Commit the design contract separately**

```bash
git add docs/superpowers/specs/2026-08-29-bootstrap-select-design.md
git commit -m "docs: define BootstrapSelect result row height"
```

---

## Task 2: Add the `ResultRowHeight` public property with validation and open-popup refresh

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs`

**Interfaces:**

- Produces: `public int BootstrapSelect.ResultRowHeight { get; set; }`.
- Produces: private owner helper `RefreshDropDownPresentationAndLayout()` used by layout-affecting setters.
- Consumes later: `BootstrapSelectDropDownController.ApplyPresentation()` and `Reposition()`.

- [ ] **Step 1: Write failing default/validation tests**

Add focused tests equivalent to:

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

- [ ] **Step 2: Run focused tests and observe the missing-property failure**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectTests"
```

Expected: compile/test failure because `ResultRowHeight` does not yet exist.

- [ ] **Step 3: Add the backing field and public property**

Implement the core contract in `BootstrapSelect.cs`:

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

- [ ] **Step 4: Add the narrow partial-class helper**

In `BootstrapSelect.Popup.cs`, keep controller details out of the main property implementation:

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

Do not recreate or close the popup solely because the property changed.

- [ ] **Step 5: Rerun the focused property tests**

Use the Task 2 test command. Expected: PASS.

- [ ] **Step 6: Commit the public property independently**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.cs \
        src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Popup.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectTests.cs
git commit -m "feat: add BootstrapSelect result row height"
```

---

## Task 3: Flow logical row height through popup presentation into `BootstrapSelectResultsView`

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`

**Interfaces:**

- `BootstrapSelectDropDownContent.ApplyPresentation(IBootstrapSelectRenderer renderer, BootstrapTheme theme, int dpi, int logicalResultRowHeight)`.
- `BootstrapSelectResultsView.ApplyPresentation(IBootstrapSelectRenderer renderer, BootstrapTheme theme, int dpi, int logicalRowHeight)`.
- `BootstrapSelectResultsView.RowHeight` remains internal effective device-pixel geometry.

- [ ] **Step 1: Write failing custom-height/DPI tests for results view**

Add tests that construct the results view, apply presentation, and assert the effective row height. Use repository theme defaults and a deterministic DPI:

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

If the test namespace does not already import Theme/Rendering, add only the required using directives.

- [ ] **Step 2: Run focused results-view tests and verify failure**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectResultsViewTests"
```

Expected: failure because the presentation methods do not yet accept logical row height and `RowHeight` still hard-codes 32.

- [ ] **Step 3: Replace the hard-coded logical row height in results view**

Use a field with the existing default:

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

If plan 003 has already added reset/preserve navigation state, retain those semantics. `ApplyPresentation` may clamp/reveal existing navigation after geometry changes, but must not replace the current result set or choose a new highlight.

- [ ] **Step 4: Forward the new argument through content and controller**

Update content forwarding:

```csharp
_resultsView.ApplyPresentation(renderer, theme, dpi, logicalResultRowHeight);
```

Update controller presentation:

```csharp
_content.ApplyPresentation(_owner.Renderer, theme, dpi, _owner.ResultRowHeight);
```

No new public/internal type is needed.

- [ ] **Step 5: Rerun focused results-view tests**

Expected: PASS at both 96 and 144 DPI assertions.

- [ ] **Step 6: Run existing BootstrapSelect layout/navigation tests**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectLayoutTests|FullyQualifiedName~BootstrapSelectResultsViewTests|FullyQualifiedName~BootstrapSelectInteractionTests"
```

Expected: PASS; default row height remains behaviorally compatible.

- [ ] **Step 7: Commit the presentation flow**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs \
        src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs \
        src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs
git commit -m "feat: apply custom BootstrapSelect row height"
```

---

## Task 4: Lock popup sizing, hit testing, scrolling, and PageDown behavior to the custom uniform height

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Modify product files only if a failing test exposes a missing propagation path.

**Interfaces:** Existing `BootstrapSelectResultLayout.Create(rowCount, rowHeight, viewportHeight, scrollOffset)` remains unchanged and is the constant-height geometry primitive.

- [ ] **Step 1: Add constant-height math coverage at 48 px**

Extend layout tests with an explicit custom height:

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

This proves the existing layout primitive needs no variable-height redesign.

- [ ] **Step 2: Add results-view preferred-size/page-size coverage**

Use a deterministic result set of at least eight item rows. With `logicalRowHeight = 48`, assert:

```text
GetPreferredSize(...).Height == visibleRows * effectiveRowHeight
Page(1) advances by max(1, ClientSize.Height / effectiveRowHeight) selectable rows
Mouse-wheel offset changes in multiples of effectiveRowHeight
```

Prefer invoking the existing internal methods directly instead of `SendKeys`.

- [ ] **Step 3: Add dropdown-content preferred-size coverage**

For three result rows at 96 DPI and `ResultRowHeight = 48`, verify the result area contributes `3 * 48` pixels and the existing search host height is added only when `SearchEnabled == true`.

Do not duplicate the search-host height formula in production code; the test may derive the expected value from the configured theme metrics/DPI.

- [ ] **Step 4: Add an open-popup resizing regression**

Create/show a `BootstrapSelect` using the same STA/non-parallel conventions as existing popup tests, open it with enough rows to make preferred height observable, capture `DropDownBoundsForTest`, then set:

```csharp
select.ResultRowHeight = 48;
```

Assert that:

```text
- popup remains open;
- popup creation count does not increase solely because of the property change;
- popup bounds are recomputed;
- resulting height respects MaxDropDownHeight and working-area collision handling.
```

Do not assert an exact screen coordinate when placement/collision can legitimately flip or shift; assert the geometry contract already used by popup tests.

- [ ] **Step 5: Run focused layout/content/popup tests**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectResultsViewTests|FullyQualifiedName~BootstrapSelectDropDownContentTests|FullyQualifiedName~BootstrapSelectPopupTests"
```

Expected: PASS.

- [ ] **Step 6: Run paging/navigation regressions from plan 003 if present**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectProviderIntegrationTests|FullyQualifiedName~BootstrapSelectPagingTests"
```

Expected: PASS; changing row geometry must not reintroduce highlight/scroll reset during async paging.

- [ ] **Step 7: Commit behavioral coverage**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectResultsViewTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs
git commit -m "test: cover BootstrapSelect custom row geometry"
```

Include any minimal product-file correction in the same commit only when a failing regression required it.

---

## Task 5: Add the two-line product-search renderer demo

**Files:**

- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProduct.cs`
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProductRenderer.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BootstrapSelectDemoContractTests.cs`

**Interfaces:**

- Product metadata is stored in `BootstrapSelectItem.Tag`.
- Popup drawing consumes `BootstrapSelectResultRenderContext.Item`, `.Bounds`, `.State`, `.Dpi`, `.Theme`, and `.Font`.
- Closed selection/chips/group headers continue through the default `BootstrapSelectRenderer`.

- [ ] **Step 1: Add a failing demo contract test**

Freeze the intended sample through control configuration rather than screenshot coordinates. Locate the product sample control by a stable `Name`, then assert:

```csharp
Assert.That(productSelect.ResultRowHeight, Is.EqualTo(48));
Assert.That(productSelect.Renderer, Is.TypeOf<BootstrapSelectProductRenderer>());
Assert.That(productSelect.Items.Count, Is.GreaterThanOrEqualTo(3));
Assert.That(productSelect.Items[0].Tag, Is.TypeOf<BootstrapSelectProduct>());
```

Also assert each sample item's `Text` equals its product `Name`, so the closed selection surface and default matcher remain useful without product-specific core behavior.

- [ ] **Step 2: Run the demo contract test and verify failure**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDemoContractTests"
```

Expected: FAIL because the product sample/types do not yet exist.

- [ ] **Step 3: Add the demo-only product data type**

Use a focused model similar to:

```csharp
internal sealed class BootstrapSelectProduct
{
    internal BootstrapSelectProduct(int id, string name, string unit, decimal unitPrice, decimal stockQuantity)
    {
        Id = id;
        Name = name;
        Unit = unit;
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

Keep it inside the demo assembly; do not move product semantics into the core package.

- [ ] **Step 4: Implement `BootstrapSelectProductRenderer` with composition**

Skeleton:

```csharp
internal sealed class BootstrapSelectProductRenderer : IBootstrapSelectRenderer
{
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

        // Draw theme-aware background from context.State.
        // Draw line 1 from product.Name.
        // Draw line 2 from Unit | formatted UnitPrice | StockQuantity.
    }

    public void DrawGroupHeader(Graphics graphics, BootstrapSelectGroupRenderContext context)
        => _defaultRenderer.DrawGroupHeader(graphics, context);

    public void DrawSelection(Graphics graphics, BootstrapSelectSelectionRenderContext context)
        => _defaultRenderer.DrawSelection(graphics, context);

    public void DrawChip(Graphics graphics, BootstrapSelectChipRenderContext context)
        => _defaultRenderer.DrawChip(graphics, context);
}
```

For `DrawResult`, implement the actual drawing rather than leaving the comments above in code:

1. Resolve row background from `Highlighted/Selected/Hot` using the same precedence as the default renderer.
2. Use `context.Theme.Colors.Text` for line 1, `MutedText` for line 2, and muted text for disabled rows.
3. Use DPI-scaled horizontal/vertical insets.
4. Derive a smaller second-line font from `context.Font` with a bounded size reduction; wrap it in `using`.
5. Use `TextRenderer.DrawText` with `EndEllipsis | SingleLine | NoPrefix` for each line.
6. Format price and stock with a deterministic culture in the demo (for example `CultureInfo.GetCultureInfo("vi-VN")`) so the sample visibly demonstrates `36.500`-style grouping.
7. If `Tag` is not a product, delegate completely to the default renderer.

Do not call `_defaultRenderer.DrawResult` first and paint over its text; custom rows should have one intentional paint pass.

- [ ] **Step 5: Add the product sample to `BootstrapSelectDemoForm`**

Configure a normal `BootstrapSelect`:

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

Populate at least three products:

```csharp
var product = new BootstrapSelectProduct(1001, "Sữa tươi Vinamilk 100% 1L", "Hộp", 36500m, 128m);
productSelect.Items.Add(new BootstrapSelectItem(product.Id, product.Name) { Tag = product });
```

Use realistic differing name lengths, units, prices, and stock quantities so ellipsis/alignment are visible during manual acceptance.

- [ ] **Step 6: Rerun the demo contract test**

Expected: PASS.

- [ ] **Step 7: Commit the sample independently**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProduct.cs \
        demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectProductRenderer.cs \
        demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/BootstrapSelectDemoContractTests.cs
git commit -m "demo: add BootstrapSelect product result template"
```

---

## Task 6: Document the public custom-rendering workflow

**Files:**

- Modify: `docs/BOOTSTRAP_SELECT.md`
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`

**Interfaces:** Documentation must describe only APIs that now exist: `ResultRowHeight`, `Renderer`, render contexts, and `BootstrapSelectItem.Tag`.

- [ ] **Step 1: Add `ResultRowHeight` to the BootstrapSelect API reference**

Document:

```text
Type: int
Default: 32
Unit: logical pixels at 96 DPI
Validation: > 0
Purpose: uniform popup result-row height
```

State explicitly that it is **not** a per-item or variable-height measurement API.

- [ ] **Step 2: Add a custom result-rendering section**

Explain the Select2 analogy without claiming API compatibility:

```text
Select2 templateResult        -> IBootstrapSelectRenderer.DrawResult
Select2 templateSelection     -> IBootstrapSelectRenderer.DrawSelection
result object custom metadata -> BootstrapSelectItem.Tag
```

Explain that a caller can implement only the desired visual behavior while delegating unchanged methods to `BootstrapSelectRenderer` through composition.

- [ ] **Step 3: Add the product-search example**

Include complete usage showing:

```csharp
productSelect.ResultRowHeight = 48;
productSelect.Renderer = new ProductRenderer();
productSelect.Items.Add(new BootstrapSelectItem(product.Id, product.Name) { Tag = product });
```

Then show the two-line layout:

```text
Product name
Unit | Unit price | Stock quantity
```

Clarify that provider-backed items work the same way as local items as long as the provider supplies `Tag` metadata on each returned `BootstrapSelectItem`.

- [ ] **Step 4: Update `docs/COMPONENTS.md`**

Add `ResultRowHeight` to the BootstrapSelect layout/property summary and preserve the statement that variable-height result rows are outside the component contract.

- [ ] **Step 5: Update manual test guidance**

Add checks for:

```text
- default 32px logical rows in Light/Dark;
- 48px product rows in Light/Dark;
- 100%, 150%, and 200% DPI;
- disabled product row;
- mouse hover/highlight/selected colors;
- keyboard Down/PageDown with custom row height;
- long product name ellipsis;
- popup max-height/collision behavior;
- selection surface remains single-line after choosing a two-line popup result.
```

- [ ] **Step 6: Commit docs**

```bash
git add docs/BOOTSTRAP_SELECT.md docs/COMPONENTS.md docs/TESTING.md
git commit -m "docs: document BootstrapSelect custom result rendering"
```

---

## Task 7: Review and update the v1 public API baseline deliberately

**Files:**

- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

**Interfaces:** The only intended newly exported member from this plan is:

```text
public property System.Int32 ResultRowHeight
```

on `MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapSelect`.

No new exported type, renderer method, event, enum, protected member, or external dependency is intended.

- [ ] **Step 1: Run the existing baseline test before changing its approved fingerprint**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~ExportedApiMatchesApprovedV1Baseline"
```

Expected: FAIL and print an `Actual fingerprint:` plus reconstructed API surface.

- [ ] **Step 2: Review the emitted API diff manually**

Compare the reconstructed BootstrapSelect surface with the current approved baseline. The change is acceptable only if the exported delta caused by this plan is exactly the `ResultRowHeight` property described above.

If any extra exported type/member appears, correct visibility before updating the fingerprint.

- [ ] **Step 3: Add a narrow semantic API assertion**

Add a release-contract test similar to:

```csharp
[Test]
public void BootstrapSelectResultRowHeightIsTheOnlyReviewedRowTemplateApiAddition()
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

This freezes the decision that the feature is uniform-height custom painting, not variable-height templating.

- [ ] **Step 4: Update the fingerprint only after review**

Copy the exact `Actual fingerprint:` emitted by Step 1 after confirming Step 2. Replace `ApprovedV1Fingerprint` with that reviewed value. Do not guess or precompute the value in the plan.

- [ ] **Step 5: Update `docs/PUBLIC_API_BASELINE.md`**

Record:

```text
BootstrapSelect adds ResultRowHeight : int.
Default and semantic contract remain documented in BOOTSTRAP_SELECT.md.
No existing exported signature changed.
No new exported renderer/template/measurement type was added.
AssemblyVersion remains 1.0.0.0.
Approved fingerprint: <the exact value verified in Step 4>.
```

When writing the actual file, replace the angle-bracket description with the verified fingerprint value; do not commit a placeholder token.

- [ ] **Step 6: Rerun release baseline tests**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

Expected: PASS.

- [ ] **Step 7: Commit the reviewed baseline**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs \
        docs/PUBLIC_API_BASELINE.md
git commit -m "chore: approve BootstrapSelect row height API"
```

---

## Task 8: Full cross-target verification and manual acceptance

**Files:**

- No planned product changes. Fix only regressions discovered by verification, keeping fixes scoped to the task that introduced them.

**Interfaces:** Final package behavior and public API from Tasks 1–7.

- [ ] **Step 1: Build the core package for .NET Framework 4.8**

```bash
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
```

Expected: PASS with no new warnings attributable to this feature.

- [ ] **Step 2: Build the core package for .NET 8 Windows**

```bash
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
```

Expected: PASS.

- [ ] **Step 3: Build the demo for both targets**

```bash
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release -f net48
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release -f net8.0-windows
```

Expected: PASS.

- [ ] **Step 4: Run BootstrapSelect-focused tests for both targets**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapSelect"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect"
```

Expected: PASS.

- [ ] **Step 5: Run the complete test suite for both targets**

```bash
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: PASS.

- [ ] **Step 6: Manually inspect the Product Search demo**

Verify at minimum:

```text
1. Popup rows show exactly two visual lines with comfortable padding at ResultRowHeight = 48.
2. Line 1 is product name; line 2 is Unit | Unit price | Stock quantity.
3. Long names ellipsize instead of overlapping line 2 or the popup edge.
4. Hover, keyboard highlight, selected, and disabled states remain readable in Light and Dark themes.
5. Down, Up, PageDown, PageUp, Home, End, Enter, mouse wheel, and mouse click target the expected row.
6. Async paging demo regressions from plan 003 remain fixed.
7. Popup resizes/repositions correctly if ResultRowHeight is changed while open.
8. Popup respects MaxDropDownHeight and screen collision handling.
9. Selected product displays as the normal single-line selection text after popup closes.
10. 100%, 150%, and 200% DPI preserve proportions and row hit testing.
```

- [ ] **Step 7: Inspect repository diff and API surface before completion**

Confirm:

```text
- no product-specific type was added to the core assembly;
- no IBootstrapSelectRenderer signature changed;
- no variable-height measurement API was introduced;
- no external package was added;
- public API delta is exactly ResultRowHeight plus the deliberately updated fingerprint;
- no generated binaries/bin/obj files are staged.
```

- [ ] **Step 8: Commit any verification-only documentation correction if one was required**

If verification required no correction, do not create an empty commit. If a real documentation correction was necessary, commit only that correction with a descriptive message.

---

## Acceptance criteria

Implementation is complete only when all of the following are true:

- `BootstrapSelect.ResultRowHeight` exists, defaults to `32`, rejects non-positive values, and is expressed in logical pixels.
- Existing callers that do not set the property retain current 32-logical-pixel result-row behavior.
- A value such as `48` is DPI-scaled and drives painting bounds, preferred result size, hit testing, mouse-wheel scrolling, visible-range calculation, and PageUp/PageDown row count through the existing constant-height layout model.
- Changing `ResultRowHeight` while the popup is open reapplies presentation and repositions the popup without recreating it solely for the property change.
- `IBootstrapSelectRenderer` and all existing render-context signatures remain unchanged.
- Variable-height rows, measure callbacks, hosted per-row controls, and HTML-like templates remain unsupported by design.
- The demo includes a product search sample where `BootstrapSelectItem.Text` is the product name and `Tag` contains product metadata.
- The demo popup renders line 1 as product name and line 2 as `Unit | Unit price | Stock quantity` with theme-aware states and DPI-aware spacing.
- Closed selection and chip rendering continue to use the default renderer in the demo through composition/delegation.
- Navigation/paging behavior from plan `20260901-003` remains stable with custom row heights.
- Public API baseline review confirms the only intended exported addition is `BootstrapSelect.ResultRowHeight` and the reviewed fingerprint is updated accordingly.
- Core, demo, and tests build/pass for both `net48` and `net8.0-windows`.
- Manual Light/Dark and 100%/150%/200% DPI checks pass.

---

## Self-review checklist for the implementer

Before declaring the plan complete, re-read the design delta and verify:

- [ ] Every use of result-row vertical geometry reads the effective `RowHeight`; no second hard-coded `32` remains in the result viewport path except the documented default field/property value and tests that intentionally verify the default.
- [ ] `BootstrapSelectResultLayout` remains constant-height and does not acquire product/template knowledge.
- [ ] Open-popup property changes preserve logical result/navigation state and only update presentation geometry.
- [ ] Demo GDI objects created per paint are disposed; caller-owned context objects are not disposed.
- [ ] Product formatting is demo-only and does not leak locale/business concepts into the core library.
- [ ] Public documentation and API fingerprint match the actual compiled assembly.
- [ ] No step left a placeholder marker or an unverified expected hash in committed files.
