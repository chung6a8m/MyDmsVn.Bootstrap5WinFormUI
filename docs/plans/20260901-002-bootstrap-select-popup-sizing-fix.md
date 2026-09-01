# BootstrapSelect Popup Sizing Regression Fix Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:test-driven-development` while implementing each task, and use `superpowers:verification-before-completion` before claiming the fix is complete. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the `BootstrapSelect` popup sizing regressions where local Single dropdowns alternate between full and truncated row sets across reopen cycles, and async Single dropdowns remain at one-row/loading height after provider results arrive or after a search such as `race` returns many matches.

**Architecture:** Keep the existing `BootstrapSelectDropDownContent` + `BootstrapSelectDropDownController` + shared overlay architecture. Separate **logical search-enabled state** from WinForms effective visibility so preferred-size calculations are deterministic even while the overlay hierarchy is hidden. Then make `RefreshResults()` own the invariant that changing the popup result set while the dropdown is open also synchronizes popup bounds. Do not add special-case sizing logic in async search callbacks, do not change provider semantics, and do not create a second popup/placement path.

**Tech Stack:** C# 12, WinForms, `net48;net8.0-windows`, NUnit 4, existing `BootstrapOverlayDropDown`/`BootstrapOverlaySurface`/placement infrastructure, existing `BootstrapSelect` async provider/search controller infrastructure.

**Related design:** `docs/plans/20260829-005-bootstrap-select.md` and `docs/superpowers/specs/2026-08-29-bootstrap-select-design.md`.

---

## Observed regressions to lock down

The implementation must cover all five reported UI observations:

1. **Local Single — first open:** all five local rows are visible.
2. **Local Single — second open:** the last row is currently clipped; repeated opens can alternate between full and truncated content.
3. **Async Single — first open:** after the first provider page completes, the popup currently remains visually tall enough for only one result row.
4. **Async Single — reopen:** a later open can have the expected multi-row height, demonstrating that the loaded logical rows exist and the first-open geometry is stale.
5. **Async Single — search `race`:** page 1 has many matching rows (the demo provider has 24 `Race sample` items and `PageSize = 20`) but the popup currently stays at one-row/loading height after completion.

These are two root causes, not five independent data defects.

### Root cause A — logical search state is coupled to effective WinForms visibility

`BootstrapSelectDropDownContent.SearchEnabled` currently reads `_searchHost.Visible`, and `GetPreferredSize()`, `FocusSearch()`, and `ForwardCharacter()` also consult `_searchHost.Visible`. In WinForms, effective `Visible` depends on ancestor visibility. The dropdown content lives below the overlay surface/content host, which is hidden outside the active popup lifecycle. Therefore a preferred-size calculation performed while the popup hierarchy is hidden can incorrectly behave as if search were disabled and omit the search band height.

**Required invariant:** `SearchEnabled` is logical component state. Ancestor visibility may affect painting/focusability, but it must never change the logical answer to whether the search band participates in popup layout.

### Root cause B — result refresh does not own geometry refresh

Async query startup replaces rows with the one-row `Loading...` result set. After the provider completes, `PublishRemoteCompletion()` refreshes logical rows, but `BootstrapSelectDropDownController.RefreshResults()` does not recompute popup bounds. As a result, the `BootstrapSelectResultsView` can contain 20 rows while the overlay window still has the height calculated for one loading row.

**Required invariant:** whenever `RefreshResults()` changes the result set while the dropdown is open, popup bounds are recomputed from the new preferred size before the UI is considered synchronized.

---

## Global constraints

- [ ] Read `AGENTS.md`, `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT_PLAN.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, the BootstrapSelect section in `docs/COMPONENTS.md`, and the related BootstrapSelect design/implementation documents before product-code changes.
- [ ] Preserve the existing public `BootstrapSelect` API. This regression fix requires **no public API additions, removals, or signature changes**.
- [ ] Preserve both TFMs and one shared code path: `net48;net8.0-windows`.
- [ ] Reuse `BootstrapOverlayDropDown`, `BootstrapOverlaySurface`, `BootstrapOverlayAnchorTracker`, and `BootstrapOverlayPlacementEngine`; do not add another overlay or sizing engine.
- [ ] Do not change `IBootstrapSelectDataProvider`, paging semantics, debounce semantics, cancellation/generation rejection, or the demo provider data to hide the problem.
- [ ] Do not fix async sizing by adding isolated `Reposition()` calls to each completion/retry/provider code path. The geometry invariant belongs in the dropdown controller's result-refresh operation.
- [ ] Do not infer search-enabled state from `Control.Visible`, `Parent.Visible`, `ContainsFocus`, or handle visibility.
- [ ] Keep result-row preferred sizing unchanged unless a failing regression test proves a separate bug. `BootstrapSelectResultsView.GetPreferredSize()` intentionally caps the preferred viewport at eight rows.
- [ ] Keep `MaxDropDownHeight`, working-area collision handling, DPI scaling, and lower/right-edge placement behavior intact.
- [ ] Use TDD: add the focused regression first, run it and confirm the expected failure, make the minimum implementation change, rerun focused tests, then run broader BootstrapSelect tests.
- [ ] UI-thread/async integration tests remain STA/non-parallel where required; do not replace deterministic controlled providers with timing sleeps.

---

## Task 1: Decouple logical search-enabled state from effective visibility

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs`

**Interfaces:** Internal-only behavioral correction. No public API change.

### Step 1: Add a deterministic hidden-ancestor regression test

- [ ] Add a test proving that hiding an ancestor does not change the logical `SearchEnabled` value and does not change the content's preferred height.

Use the existing `CreatePresentedContent(96)` and descendant helpers. The test should establish a real result count (for example five item rows), capture preferred size while the ancestor is visible, hide the ancestor, and request preferred size again.

Suggested shape:

```csharp
[Test]
public void HiddenAncestorDoesNotDisableLogicalSearchOrShrinkPreferredHeight()
{
    using var host = new Panel { Visible = true };
    using var content = CreatePresentedContent(96);
    content.SetResults(CreateItemResults(5));
    host.Controls.Add(content);

    var proposed = new Size(340, 320);
    var visiblePreferred = content.GetPreferredSize(proposed);

    host.Visible = false;
    var hiddenPreferred = content.GetPreferredSize(proposed);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(content.SearchEnabled, Is.True);
        Assert.That(hiddenPreferred.Height, Is.EqualTo(visiblePreferred.Height));
        Assert.That(hiddenPreferred.Width, Is.EqualTo(visiblePreferred.Width));
    }));
}
```

If the existing test helpers do not have a result-set factory, add a small private helper in this test file that constructs five `BootstrapSelectResultRow` item rows. Keep it test-local; do not expose product internals merely for the test.

### Step 2: Confirm the test fails for the current implementation

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests.HiddenAncestorDoesNotDisableLogicalSearchOrShrinkPreferredHeight"
```

Expected before the fix: failure because `_searchHost.Visible` becomes effectively false through the hidden ancestor, causing either `SearchEnabled == false` and/or a preferred height that drops the search band.

Do not weaken the assertion to match current behavior.

### Step 3: Introduce explicit logical state in `BootstrapSelectDropDownContent`

- [ ] Add a private logical field initialized to the component default:

```csharp
private bool _searchEnabled = true;
```

- [ ] Change `SearchEnabled` so the getter returns `_searchEnabled` and the setter updates both the logical field and the search host's presentation visibility.

Target semantics:

```csharp
internal bool SearchEnabled
{
    get => _searchEnabled;
    set
    {
        if (_searchEnabled == value)
        {
            return;
        }

        _searchEnabled = value;
        _searchHost.Visible = value;
        PerformLayout();
    }
}
```

- [ ] Replace semantic/layout decisions that currently inspect `_searchHost.Visible` with `_searchEnabled`:
  - `GetPreferredSize()` — include search height when logical search is enabled.
  - `FocusSearch()` — choose search editor vs. results view from logical state.
  - `ForwardCharacter()` — allow/disallow text forwarding from logical state.

- [ ] Keep `_searchHost.Visible = value` in the setter because the search band still must actually hide when search is disabled. The correction is specifically that effective WinForms visibility is no longer the source of truth.

### Step 4: Preserve explicit search-disabled behavior

- [ ] Extend or reuse `DisablingSearchRemovesTheBandAndRestoresTheSameWrapperWhenReenabled` to ensure the change does not accidentally force the search host visible when logical search is disabled.
- [ ] If useful, add an assertion that preferred height with `SearchEnabled = false` excludes exactly the search band even when an ancestor is hidden. Avoid pixel magic; derive the search host height from the existing child composition/helper.

### Step 5: Run content tests on both TFMs

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectDropDownContentTests"
```

Expected: all pass.

### Step 6: Commit the logical-state fix

- [ ] Commit only the Task 1 files:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownContent.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectDropDownContentTests.cs
git commit -m "fix: stabilize BootstrapSelect search band sizing"
```

---

## Task 2: Lock the local reopen-cycle regression at popup level

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs`
- Product fix expected from Task 1; modify product code in this task only if the new integration test exposes a distinct issue.

### Step 1: Add a repeated open/close geometry regression

- [ ] Add a test using a real shown `Form` and a local Single `BootstrapSelect` containing the same shape as the demo: five rows, including one disabled row and one long-caption row.
- [ ] Open the dropdown, pump messages, capture `DropDownBoundsForTest.Height`, close it, and repeat for at least three open cycles.
- [ ] Assert:
  - the popup object is reused (`DropDownCreationCountForTest` stays `1`),
  - all logical result rows remain present,
  - the popup height is identical across every open cycle,
  - no cycle loses the equivalent of the final row.

Suggested core assertion:

```csharp
Assert.That(secondBounds.Height, Is.EqualTo(firstBounds.Height));
Assert.That(thirdBounds.Height, Is.EqualTo(firstBounds.Height));
Assert.That(select.VisibleResultItemTextsForTest, Has.Count.EqualTo(5));
```

Do not assert an unexplained hard-coded popup height. The contract is **stable geometry for the same logical rows and configuration**.

### Step 2: Confirm the regression fails against the pre-fix behavior if tested independently

- [ ] If implementing from a clean branch, run this test before Task 1's product change to confirm the open/reopen alternation. If Task 1 is already committed, document in the commit message/test name that this is the integration guard for the reported regression and verify it passes.

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectPopupTests"
```

### Step 3: Verify no lifecycle regression

- [ ] Keep the existing `PopupIsLazyReusedAndRaisesLifecycleEvents` expectations intact.
- [ ] Confirm `DropDownOpened`/`DropDownClosed`, lazy creation, focus restoration, and search clearing behavior are not changed merely to stabilize size.

### Step 4: Run popup tests on both TFMs and commit

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectPopupTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectPopupTests"
```

- [ ] Commit the integration guard:

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPopupTests.cs
git commit -m "test: cover BootstrapSelect reopen sizing"
```

---

## Task 3: Make result refresh synchronize open-popup geometry

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectProviderIntegrationTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs`
- Inspect only unless tests prove otherwise: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSelect.Search.cs`
- Inspect only unless tests prove otherwise: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectResultsView.cs`

**Interfaces:** Internal controller invariant. Async provider contracts and search-controller semantics stay unchanged.

### Step 1: Add a first-page loading-to-loaded geometry regression

- [ ] Add an STA integration test in `BootstrapSelectProviderIntegrationTests` using `BootstrapSelectControlledProvider(honorCancellation: false)` and the existing `RunOnIsolatedWinFormsThread` / `PumpUntil` helpers.
- [ ] Configure `SearchDebounce = TimeSpan.Zero`, `PageSize = 20`, and place the select in a sufficiently large shown form so normal max-height behavior is measurable without a lower-edge collision dominating the test.
- [ ] Open the dropdown and wait until the controlled provider has received page 1 while leaving the provider task pending. Capture the one-row/loading `DropDownBoundsForTest.Height`.
- [ ] Complete page 1 with exactly 20 items and wait until `VisibleResultItemTextsForTest` contains those 20 logical items.
- [ ] Without closing/reopening the dropdown, assert the current popup height is greater than the loading height and the popup is still open.

Suggested test name:

```text
FirstPageCompletionReflowsOpenPopupFromLoadingHeight
```

Suggested assertions:

```csharp
Assert.Multiple((Action)(() =>
{
    Assert.That(select.IsDropDownOpenForTest, Is.True);
    Assert.That(select.VisibleResultItemTextsForTest, Has.Count.EqualTo(20));
    Assert.That(select.DropDownBoundsForTest.Height, Is.GreaterThan(loadingHeight));
}));
```

The test must fail before the controller fix because logical rows update but bounds remain at loading height.

### Step 2: Add a search-completion regression for the reported `race` scenario

- [ ] In the same integration fixture, add a test that first establishes a normal loaded query, then calls `SetSearchTextForTest("race")` while the dropdown remains open.
- [ ] Wait for a controlled `race` page-1 query, capture the loading-state popup height, and complete it with 20 items named `Race sample 01` through `Race sample 20`.
- [ ] Assert all 20 rows are logically present and popup height grows in the **same open session**.

Suggested test name:

```text
SearchCompletionReflowsOpenPopupForTwentyRaceMatches
```

This test intentionally mirrors the demo's first page (`PageSize = 20`) without taking a test dependency on the demo assembly.

### Step 3: Run the new tests and confirm the geometry assertions fail

- [ ] Run each focused test before the product change:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~FirstPageCompletionReflowsOpenPopupFromLoadingHeight"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~SearchCompletionReflowsOpenPopupForTwentyRaceMatches"
```

Expected before fix: the 20 logical items arrive, but `DropDownBoundsForTest.Height` stays equal to the one-row/loading height.

If the rows themselves are missing, stop and investigate provider/search behavior; do not continue with a sizing fix until the test proves the defect is geometry, not data.

### Step 4: Centralize the result-set/geometry invariant in `RefreshResults()`

- [ ] Change `BootstrapSelectDropDownController.RefreshResults()` so it remains the single operation that pushes the current logical result set into the content and, when the popup is already open, synchronizes placement/bounds afterward.

Target shape:

```csharp
internal void RefreshResults()
{
    if (_content is null)
    {
        return;
    }

    _content.SetResults(
        _owner.BuildCurrentPopupResultSet(
            _content.SearchEnabled ? _content.SearchText : string.Empty));

    if (_isOpen)
    {
        Reposition();
    }
}
```

- [ ] Preserve the `Open()` ordering:

```text
EnsureCreated
→ RefreshResults       // _isOpen is false: do not reposition yet
→ ApplyPresentation
→ ComputeBounds
→ _isOpen = true
→ ShowAt
```

This avoids redundant movement during initial construction while still ensuring every later refresh during an open session reflows geometry.

### Step 5: Remove duplicate caller-owned repositioning

- [ ] In `OnSearchTextChanged`, remove the explicit:

```csharp
if (_isOpen) Reposition();
```

because `RefreshResults()` now owns that invariant.

The resulting flow should be conceptually:

```csharp
private void OnSearchTextChanged(string text)
{
    _owner.NotifyPopupSearchTextChanged(text);
    RefreshResults();
}
```

- [ ] Review all controller/search call sites of `RefreshResults()` and ensure none now perform a redundant immediate `Reposition()` solely because rows changed.
- [ ] Do **not** add a one-off `Reposition()` to `PublishRemoteCompletion()`. Its existing `RefreshResults()` call should become sufficient by design.
- [ ] Keep explicit `Reposition()` uses that respond to true anchor/theme/window geometry changes rather than result-set changes.

### Step 6: Check for re-entrancy and paging side effects

- [ ] Verify the new invariant does not cause recursive `RefreshResults()` → `Reposition()` → `RefreshResults()` loops. `Reposition()` should only compute/move bounds.
- [ ] Verify `SetResults()` still resets result viewport/highlight as designed and that `NearEndReached` paging does not repeatedly fire merely because the popup is resized.
- [ ] Keep stale-generation rejection, cancellation, retry, and provider replacement behavior unchanged.

### Step 7: Run async integration tests on both TFMs

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelectProviderIntegrationTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelectProviderIntegrationTests"
```

Expected: existing provider replacement/comparer/thread-affinity tests and the new geometry tests all pass.

### Step 8: Commit the controller invariant

- [ ] Commit:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapSelectDropDownController.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectProviderIntegrationTests.cs
git commit -m "fix: reflow BootstrapSelect popup after result refresh"
```

---

## Task 4: Exercise adjacent BootstrapSelect behavior for regressions

**Files:**
- Test only unless failures identify a real regression:
  - `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectPagingTests.cs`
  - `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectFirstPageRetryTests.cs`
  - `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectConcurrencyTests.cs`
  - `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectInteractionTests.cs`
  - `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectLifecycleTests.cs`
  - `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSelectVisualRegressionTests.cs`

### Step 1: Run all BootstrapSelect tests on .NET 8

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapSelect"
```

- [ ] Specifically review failures related to:
  - first-page retry and later-page retry,
  - stale async completion/generation rejection,
  - near-end paging,
  - Tab/Escape/focus behavior,
  - popup reuse/close lifecycle,
  - lower/right-edge placement,
  - light/dark visual output.

### Step 2: Run all BootstrapSelect tests on .NET Framework 4.8

- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapSelect"
```

Expected: parity with the .NET 8 behavior.

### Step 3: Do not paper over unrelated failures

- [ ] If an existing test fails because geometry is now correctly refreshed, update the expectation only when it encodes the old stale-size bug. Do not broaden tolerances or remove lifecycle assertions to make the suite green.
- [ ] If a failure reveals a distinct bug, isolate it with a failing test and either fix it as a clearly justified extension of this plan or stop and document it separately.

---

## Task 5: Build both targets and manually verify the five reported scenarios

**Files:** No product-file change expected. The existing demo scenarios are sufficient and should not be changed merely to make the fix look correct.

### Step 1: Build the solution

- [ ] Run:

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release
```

- [ ] If the repository's standard `build.ps1` includes additional required checks, run it as well:

```powershell
./build.ps1
```

Expected: both `net48` and `net8.0-windows` product/test targets compile without new warnings/errors.

### Step 2: Manual local-Single verification

- [ ] Launch the integrated demo and open **Select → Single / local search / clear**.
- [ ] Open/close the dropdown at least six consecutive times.
- [ ] Verify every open displays the same five logical rows, including the long `Tailspin Toys ...` final row.
- [ ] Verify the popup does not alternate between full and truncated height.
- [ ] Verify the disabled `Adventure Works` row is still visible but not selectable.

### Step 3: Manual async-first-open verification

- [ ] Open **Async single / delayed provider / paging** from a clean demo run.
- [ ] Observe the initial loading state, then wait for page 1.
- [ ] Verify the popup grows/reflows during the same open session to the normal multi-row viewport; do not close/reopen it to obtain the correct size.
- [ ] Verify scrolling near the end still requests the next page.

### Step 4: Manual `race` verification

- [ ] With the async Single popup open, type `race`.
- [ ] Verify the temporary loading state may contract to loading height, but after the winning query completes the popup immediately expands again in the same open session.
- [ ] Verify page 1 exposes the expected 20 `Race sample` logical results, subject only to the existing eight-row preferred viewport and scrolling; it must not remain visually limited to one result.
- [ ] Type quickly through multiple `race...` query generations and verify stale completions do not overwrite the latest query.

### Step 5: Placement and DPI sanity checks

- [ ] Repeat an async or local open near the lower/right edge using the existing placement scenario; verify the resize still goes through the shared placement engine and remains inside the working area.
- [ ] Test at least 100% and one scaled DPI setting (for example 125% or 150%) if available. Search-band and row heights should scale using existing metrics; no new hard-coded pixel compensation should appear.
- [ ] Toggle Light/Dark theme while open and ensure the popup remains correctly sized after re-presentation/repositioning.

### Step 6: Final full test pass

- [ ] Run the full test project for both targets:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
```

- [ ] Confirm there is no public API baseline/fingerprint change. This fix should be internal-only.
- [ ] Confirm `git diff --check` is clean and only intended source/test files changed.

---

## Acceptance criteria

The work is complete only when all of the following are true:

- [ ] `BootstrapSelectDropDownContent.SearchEnabled` reports logical state independently of ancestor visibility.
- [ ] `GetPreferredSize()` includes the search band whenever logical search is enabled, even while the overlay hierarchy is hidden.
- [ ] Local Single popup geometry is stable across repeated open/close cycles; the last row no longer alternates between visible and clipped.
- [ ] Async first-page completion changes the popup from loading-height to loaded-result height without requiring a close/reopen.
- [ ] A `race` search returning 20 page-1 matches grows the open popup after completion and exposes the full result set through the existing viewport/scroll behavior.
- [ ] `RefreshResults()` is the owner of the result-set → open-popup geometry synchronization invariant.
- [ ] `PublishRemoteCompletion()` does not contain an ad hoc sizing workaround; its normal refresh path is sufficient.
- [ ] No duplicate overlay/placement logic is introduced.
- [ ] Existing paging, retry, debounce, cancellation, stale-generation rejection, selection, Tab/Escape/focus, theme, DPI, and placement tests remain green.
- [ ] Both `net48` and `net8.0-windows` build and test successfully.
- [ ] No public API change is introduced.

---

## Expected final change set

The minimal intended product change is small:

```text
src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/
  BootstrapSelectDropDownContent.cs       # logical SearchEnabled state
  BootstrapSelectDropDownController.cs    # RefreshResults reflows open popup

tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/
  BootstrapSelectDropDownContentTests.cs  # hidden-ancestor/preferred-size regression
  BootstrapSelectPopupTests.cs            # repeated local reopen regression
  BootstrapSelectProviderIntegrationTests.cs # loading→loaded and race geometry regressions
```

`BootstrapSelect.Search.cs`, `BootstrapSelectResultsView.cs`, overlay infrastructure, demo provider, and public API should remain unchanged unless a newly failing test proves a separate defect that cannot be fixed within the two root causes above.
