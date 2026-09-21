# BootstrapPagination Control Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Bootstrap-inspired, keyboard-accessible WinForms pagination control that manages page-selection state, renders a bounded page window with ellipses, reuses the existing Button/ButtonGroup infrastructure, remains independent from any data source, and preserves the repository's dual-target and v1 API-compatibility rules.

**Architecture:** `BootstrapPagination` is a composite `Panel` in `MyDmsVn.Bootstrap5WinFormUI.Controls`. It owns one internal `BootstrapButtonGroup` and dynamically composes `BootstrapButton` instances for First/Previous/page-number/ellipsis/Next/Last entries; it does not custom-paint page links, subscribe to the theme directly, or couple itself to `BootstrapDataGridView`. A small internal pure layout helper computes the numbered-page/ellipsis model so range behavior is deterministic and heavily unit-tested without creating WinForms handles.

**Tech Stack:** C#, native WinForms, existing `BootstrapButton` / `BootstrapButtonGroup` / Theme / Rendering infrastructure, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`).

**Spec:** User request plus the deferred-component rule in `docs/COMPONENTS.md`; this plan's **Pagination contract** section is the feature-specific implementation baseline. Project-wide constraints come from `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, and `docs/PUBLIC_API_BASELINE.md`.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; the public control namespace is `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile for both `net48` and `net8.0-windows` from one shared code path wherever possible.
- Pagination is a composite control. Reuse `BootstrapButton` and `BootstrapButtonGroup`; do not introduce a second button renderer, selection engine, theme subscription layer, icon model, or animation/timer infrastructure.
- Pagination owns page-navigation state only. It must not own `DataSource`, query execution, async loading, filtering, sorting, `BootstrapDataGridView`, or any application paging protocol.
- Page numbering is 1-based.
- `TotalItems` is non-negative; `PageSize` is at least 1; `MaxVisiblePages` is at least 5.
- `TotalPages` is computed as `TotalItems == 0 ? 1 : 1 + ((TotalItems - 1) / PageSize)` to avoid integer overflow in `TotalItems + PageSize - 1`.
- Directly assigning `CurrentPage` outside `[1, TotalPages]` throws `ArgumentOutOfRangeException`. When `TotalItems` or `PageSize` reduces the valid page range, the control clamps its existing page internally and raises `PageChanged` exactly once if the effective page changes.
- Reassigning the same effective value must not raise duplicate `PageChanged` events.
- The active numbered page remains enabled/focusable, has `Selected = true`, and clicking it is a no-op. Ellipsis entries are disabled/non-focusable.
- Boundary navigation buttons are disabled when they cannot move the current page.
- Dynamic rebuilds must dispose removed child buttons because `BootstrapButton` owns theme subscriptions/resources.
- Child controls provide theme, hover, pressed, focus, disabled, DPI, font, and rendering behavior. `BootstrapPagination` should not subscribe to `BootstrapThemeManager.ThemeChanged` unless implementation reveals a concrete state that cannot be delegated.
- Designer construction must remain safe with a parameterless constructor and no application bootstrap.
- Public members require XML documentation and appropriate WinForms designer attributes.
- Pagination is a new exported API after the proposed v1 baseline. The baseline fingerprint must fail first, be deliberately reviewed, and be updated only after verifying the exported surface contains exactly the intended additions.
- No external package dependency is permitted for this feature.

---

## Pagination contract

### Public type

```csharp
[DefaultEvent(nameof(PageChanged))]
public class BootstrapPagination : Panel
{
    public event EventHandler? PageChanged;

    public int TotalItems { get; set; }          // default 0
    public int PageSize { get; set; }            // default 20
    public int CurrentPage { get; set; }         // default 1, 1-based
    public int TotalPages { get; }                // minimum 1
    public int MaxVisiblePages { get; set; }      // default 5, minimum 5
    public bool ShowFirstLast { get; set; }       // default true
    public bool ShowPreviousNext { get; set; }    // default true
    public BootstrapButtonSize ButtonSize { get; set; } // default Default
    public BootstrapVariant Variant { get; set; }       // default Primary
    public int BorderRadius { get; set; }         // default -1, forwarded to ButtonGroup
}
```

No `DataSource`, `Items`, `LoadPageAsync`, `PageChanging`, cancelable event, page-size selector, page-size collection, direct DataGrid binding, or alternate zero-based page API is part of this plan.

### Page-window semantics

`MaxVisiblePages` limits numbered page buttons only; First/Previous/Next/Last and ellipsis controls do not count toward this number.

For `TotalPages <= MaxVisiblePages`, show every numbered page and no ellipsis.

For larger ranges, always reserve numbered slots for page `1` and `TotalPages`, place a contiguous middle window around `CurrentPage`, then insert one disabled ellipsis button (`…`) for each hidden gap. With `MaxVisiblePages = 5`:

```text
TotalPages=5,  CurrentPage=3  => 1 2 3 4 5
TotalPages=20, CurrentPage=1  => 1 2 3 4 … 20
TotalPages=20, CurrentPage=10 => 1 … 9 10 11 … 20
TotalPages=20, CurrentPage=20 => 1 … 17 18 19 20
```

The current page must always be present in the numbered window.

### Default visual/interaction model

```text
[«] [‹] [1] […] [9] [10*] [11] […] [20] [›] [»]
```

- `«` = First, `‹` = Previous, `›` = Next, `»` = Last.
- `*` indicates `Selected = true`; it is not literal text.
- Page controls use the configured `Variant` and `ButtonSize`.
- Pagination owns `Selected`; the child `BootstrapButtonGroup.SelectionMode` remains `None`.
- `BootstrapButtonGroup.BorderRadius` receives the Pagination `BorderRadius` value so connected-corner behavior remains centralized in the existing group.
- Navigation and numbered buttons remain normal `BootstrapButton` controls, preserving mouse, Tab, Enter, Space, focus rendering, disabled semantics, and accessibility behavior.
- `AccessibleRole` for the container is `Grouping`; set a useful default `AccessibleDescription` such as `"Pagination navigation."`.
- Child accessible names are `First page`, `Previous page`, `Page N`, `Current page N`, `Next page`, and `Last page` as applicable.

---

## File Structure

**Create product files**

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPaginationLayoutLogic.cs` — internal pure page-window calculation and item model.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPagination.cs` — public composite control, state validation, button composition, events, and designer metadata.

**Create test files**

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPaginationLayoutLogicTests.cs` — pure page-window/ellipsis tests.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPaginationContractTests.cs` — exported shape/defaults/validation contract.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPaginationTests.cs` — STA interaction/composition/lifecycle tests.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/PaginationDemoFormTests.cs` — demo smoke/integration tests.

**Create demo file**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/PaginationDemoForm.cs` — standalone scenarios plus a paged in-memory `BootstrapDataGridView` example.

**Modify integration/docs**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs` — add Pagination to integrated navigation.
- `docs/COMPONENTS.md` — move Pagination out of deferred components and record the finalized contract/reuse rules.
- `docs/ARCHITECTURE.md` — add `Pagination -> ButtonGroup/Button` to the composite-control dependency model.
- `docs/TESTING.md` — document automated and manual Pagination verification.
- `README.md` — include Pagination in the supported-control list/examples.
- `docs/PACKAGE_README.md` — include Pagination in the package-facing feature list.
- `CHANGELOG.md` — add an `Unreleased` section describing the compatible Pagination API addition rather than rewriting the existing `1.0.0-rc.1` history.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs` — deliberately approve the new exported fingerprint after review.
- `docs/PUBLIC_API_BASELINE.md` — record the newly approved fingerprint and why it changed.

---

### Task 1: Build deterministic page-window logic

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPaginationLayoutLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPaginationLayoutLogicTests.cs`

**Interfaces:**
- Produces internal `BootstrapPaginationItemKind` with `Page` and `Ellipsis` values.
- Produces internal immutable `BootstrapPaginationItem` carrying `Kind` and `Page`; `Page` is meaningful only for `Kind == Page`.
- Produces `internal static IReadOnlyList<BootstrapPaginationItem> Build(int totalPages, int currentPage, int maxVisiblePages)`.

- [ ] **Step 1: Write failing pure-logic tests** for small ranges, start/middle/end windows, exact `MaxVisiblePages`, current-page inclusion, one-page/empty-model representation, invalid arguments, and absence of duplicate page numbers.

Use concrete expectations such as:

```csharp
[TestCase(5, 3, 5, "1,2,3,4,5")]
[TestCase(20, 1, 5, "1,2,3,4,...,20")]
[TestCase(20, 10, 5, "1,...,9,10,11,...,20")]
[TestCase(20, 20, 5, "1,...,17,18,19,20")]
public void BuildReturnsExpectedWindow(int totalPages, int currentPage, int maxVisiblePages, string expected)
{
    var items = BootstrapPaginationLayoutLogic.Build(totalPages, currentPage, maxVisiblePages);
    Assert.That(Format(items), Is.EqualTo(expected));
}
```

Also assert `Build(1, 1, 5)` returns only page 1, `totalPages < 1` throws, `currentPage < 1 || currentPage > totalPages` throws, and `maxVisiblePages < 5` throws.

- [ ] **Step 2: Run** `dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapPaginationLayoutLogicTests` **and verify RED because the helper does not exist.**
- [ ] **Step 3: Implement the minimal pure algorithm.** For large ranges reserve page 1 and the last page, compute `middleCount = maxVisiblePages - 2`, center that contiguous middle window around `currentPage`, clamp it inside `[2, totalPages - 1]`, and insert ellipsis items when a gap exists before or after that window.
- [ ] **Step 4: Run the layout tests for `net8.0-windows` and `net48`; verify GREEN.**
- [ ] **Step 5: Commit** `feat: add pagination window logic`.

### Task 2: Freeze the public Pagination state contract

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPagination.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPaginationContractTests.cs`

**Interfaces:**
- Public surface is exactly the **Pagination contract** above; do not add convenience aliases during implementation.
- Consumes: `BootstrapButtonSize`, `BootstrapVariant`, existing `BootstrapButtonGroup`.
- Produces: a designer-safe `BootstrapPagination` with validated state and `PageChanged` semantics.

- [ ] **Step 1: Write failing contract tests** that reflect the public type and verify defaults: `TotalItems=0`, `PageSize=20`, `CurrentPage=1`, `TotalPages=1`, `MaxVisiblePages=5`, `ShowFirstLast=true`, `ShowPreviousNext=true`, `ButtonSize=Default`, `Variant=Primary`, `BorderRadius=-1`, `AutoSize=true`, `TabStop=false`, and `AccessibleRole.Grouping`.

- [ ] **Step 2: Add failing validation/state tests**:

```csharp
[Test]
public void ReducingTotalItemsClampsCurrentPageAndRaisesOneEvent()
{
    using var pagination = new BootstrapPagination { TotalItems = 100, PageSize = 10, CurrentPage = 10 };
    var count = 0;
    pagination.PageChanged += (_, _) => count++;

    pagination.TotalItems = 15;

    Assert.That(pagination.TotalPages, Is.EqualTo(2));
    Assert.That(pagination.CurrentPage, Is.EqualTo(2));
    Assert.That(count, Is.EqualTo(1));
}
```

Cover negative `TotalItems`, zero/negative `PageSize`, `MaxVisiblePages < 5`, direct `CurrentPage=0`, direct `CurrentPage>TotalPages`, same-page assignment, safe total-page calculation at `int.MaxValue`, and page-size changes that do/do-not clamp the current page.

- [ ] **Step 3: Run the contract tests on `net8.0-windows`; verify RED.**
- [ ] **Step 4: Implement state/validation only, keeping rebuild logic private and simple.** Update the backing field before raising `PageChanged`, raise after state is coherent, and do not raise for visual-only property changes.
- [ ] **Step 5: Run contract tests on both target frameworks; verify GREEN.**
- [ ] **Step 6: Commit** `feat: add BootstrapPagination state contract`.

### Task 3: Compose Bootstrap buttons and interaction behavior

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPagination.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPaginationTests.cs`

**Interfaces:**
- Consumes: `BootstrapPaginationLayoutLogic.Build(...)`.
- Owns: exactly one internal `BootstrapButtonGroup` with `SelectionMode = BootstrapButtonSelectionMode.None`, horizontal orientation, and no caller-visible child-management API.
- Child clicks update `CurrentPage`; they never mutate `TotalItems` or `PageSize`.

- [ ] **Step 1: Write failing STA composition tests** that find the child `BootstrapButtonGroup` from `pagination.Controls`, inspect its `BootstrapButton` children, and verify ordering/text/selection/disabled state for the four concrete page-window examples in this plan.
- [ ] **Step 2: Write failing navigation tests** using `BootstrapButton.PerformClick()` for First/Previous/page number/Next/Last. Assert one `PageChanged` event per effective move, no event when the active page is clicked, and no event from disabled boundary or ellipsis buttons.
- [ ] **Step 3: Write failing style-propagation tests** proving changes to `ButtonSize`, `Variant`, and `BorderRadius` update the existing group/buttons without changing `CurrentPage` or raising `PageChanged`.
- [ ] **Step 4: Write a failing disposal regression test** that captures old dynamic buttons, changes `TotalItems` enough to rebuild, and asserts every removed button has `IsDisposed == true`.
- [ ] **Step 5: Run `BootstrapPaginationTests` on `net8.0-windows`; verify RED.**
- [ ] **Step 6: Implement child composition.** Rebuild only when state changes alter visible items/navigation; dispose removed buttons before replacing them; wire each page/navigation click once; set `Selected`, `Enabled`, `TabStop`, semantic accessible names, `Variant`, `ButtonSize`, and connected group radius from current Pagination state.
- [ ] **Step 7: Verify inherited `Enabled=false` disables effective child interaction without mutating caller-owned page state. Verify zero items displays selected page `1` with all directional navigation disabled.**
- [ ] **Step 8: Run Pagination control tests for both targets; verify GREEN.**
- [ ] **Step 9: Commit** `feat: compose BootstrapPagination controls`.

### Task 4: Harden designer, keyboard, accessibility, and layout behavior

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapPagination.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapPaginationTests.cs`

**Interfaces:**
- Pagination itself is non-focusable; focus stays on actionable child Buttons.
- No direct theme-manager subscription is introduced.
- No custom painting or per-control timer is introduced.

- [ ] **Step 1: Add failing tests** for parameterless designer construction, `GetPreferredSize`, current-page inclusion after repeated state changes, child Tab-stop rules, ellipsis `Enabled=false`/`TabStop=false`, accessible names, and repeated property changes without duplicate child controls/event handlers.
- [ ] **Step 2: Add a lifecycle stress test** that performs at least 100 rebuild-triggering state changes, then disposes the Pagination and asserts the owned ButtonGroup and current child Buttons are disposed.
- [ ] **Step 3: Run focused tests; verify RED where behavior is not yet complete.**
- [ ] **Step 4: Complete layout/lifecycle behavior using normal WinForms ownership.** Keep `BackColor=Color.Transparent`, `AutoSize=true`, `AutoSizeMode=GrowAndShrink`, and avoid a theme subscription because child controls already react to runtime theme changes.
- [ ] **Step 5: Run Pagination tests on both targets; verify GREEN.**
- [ ] **Step 6: Commit** `test: harden BootstrapPagination lifecycle`.

### Task 5: Add an integrated Pagination demo

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/PaginationDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/PaginationDemoFormTests.cs`

**Interfaces:**
- Demo consumes only public Pagination APIs.
- DataGrid integration remains application-owned: the demo slices an in-memory list after `PageChanged`; Pagination never receives the grid/data source.

- [ ] **Step 1: Write a failing demo smoke test** that constructs `PaginationDemoForm`, verifies it contains at least one `BootstrapPagination`, and confirms integrated `MainForm` navigation contains a Pagination page definition.
- [ ] **Step 2: Implement demo scenarios** for: small range without ellipsis; 20-page middle window; first/last boundary state; zero items; Small/Default/Large button sizes; toggles for First/Last and Previous/Next; and a `BootstrapDataGridView` showing an in-memory paged dataset with `PageSize=10`.
- [ ] **Step 3: In the DataGrid scenario, handle `PageChanged` in the form by slicing the original list according to `(CurrentPage - 1) * PageSize` and rebinding the page only.** Do not add paging behavior to `BootstrapDataGridView`.
- [ ] **Step 4: Add `Pagination` to `MainForm` near the DataGrid demo with a description covering page windows, ellipses, keyboard interaction, and decoupled grid integration.**
- [ ] **Step 5: Build the demo for `net8.0-windows`; verify zero compile errors.**
- [ ] **Step 6: Run `PaginationDemoFormTests`; verify GREEN.**
- [ ] **Step 7: Commit** `demo: add BootstrapPagination scenarios`.

### Task 6: Finalize documentation and deliberately approve the API addition

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/TESTING.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

- [ ] **Step 1: Update `docs/COMPONENTS.md`.** Add a finalized `BootstrapPagination` section with the exact public concepts/defaults/page-window rules from this plan and remove `Pagination` from the Deferred components list.
- [ ] **Step 2: Update `docs/ARCHITECTURE.md`.** Add `Pagination -> ButtonGroup -> Button` to the conceptual dependency graph and list Pagination as a composite control that owns navigation state but not data retrieval.
- [ ] **Step 3: Update `docs/TESTING.md`** with pure layout tests, STA state/keyboard/accessibility/lifecycle coverage, runtime Light/Dark and DPI manual checks, and the decoupled DataGrid demo scenario.
- [ ] **Step 4: Update `README.md` and `docs/PACKAGE_README.md`** to advertise `BootstrapPagination` without claiming automatic data-source/DataGrid paging.
- [ ] **Step 5: Add a new `## [Unreleased]` section to `CHANGELOG.md`** and record the compatible Pagination API addition. Do not modify the historical contents of `1.0.0-rc.1`.
- [ ] **Step 6: Run only the v1 API baseline test before changing its hash:**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline
```

Expected: FAIL, printing `Actual fingerprint:` plus the reconstructed exported API.

- [ ] **Step 7: Review the reconstructed surface line-by-line.** The only new exported type should be `MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapPagination : System.Windows.Forms.Panel`, with only the constructor/inherited-visible overrides and public members specified by this plan. `BootstrapPaginationLayoutLogic`, its item type, and item kind must remain internal and therefore absent from the baseline output.
- [ ] **Step 8: Copy the deterministic `Actual fingerprint` from the reviewed failure into `ApprovedV1Fingerprint` in `Phase16PublicApiBaselineTests.cs`, and write the same value into `docs/PUBLIC_API_BASELINE.md` with a note that Pagination is an intentional compatible addition approved after `1.0.0-rc.1`.** Keep `AssemblyVersion` at `1.0.0.0`.
- [ ] **Step 9: Rerun the API baseline tests for `net8.0-windows` and `net48`; verify GREEN.**
- [ ] **Step 10: Commit** `docs: finalize BootstrapPagination contract`.

### Task 7: Complete dual-target verification and manual UI gate

**Files:**
- No new files expected; fix only Pagination-related defects uncovered by verification.

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

- [ ] **Step 3: Run all tests for both targets:**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: all tests pass.

- [ ] **Step 4: Search the Pagination implementation for prohibited infrastructure.** Confirm it contains no direct `System.Windows.Forms.Timer`, `Task.Delay`, `Thread.Sleep`, direct `BootstrapThemeManager.ThemeChanged` subscription, DataGrid/DataSource ownership, or duplicate button-rendering logic.
- [ ] **Step 5: Run the demo and manually verify:** page 1/middle/last windows; ellipses; zero-items state; First/Previous/Next/Last enablement; mouse; Tab/Shift+Tab; Enter/Space; focus visibility; disabled parent; Small/Default/Large; live Light/Dark switching; 96/120/144/192 DPI; resize; 100+ state changes; and the in-memory DataGrid paging sample.
- [ ] **Step 6: Specifically verify that changing Pagination pages never changes DataGrid sorting/selection APIs or requires a reference from Pagination to DataGrid.**
- [ ] **Step 7: If verification required code changes, rerun Steps 1-5 and commit the fixes as** `fix: harden BootstrapPagination verification`. **If no fixes were required, do not create an empty commit.**

---

## Definition of done

Pagination is complete only when all of the following are true:

- `BootstrapPagination` exposes exactly the planned public API with XML documentation.
- Page calculation is deterministic, 1-based, bounded, and overflow-safe.
- First/Previous/numbered/ellipsis/Next/Last states are correct at every boundary.
- The control reuses `BootstrapButtonGroup` and `BootstrapButton` instead of duplicating rendering or selection infrastructure.
- Rebuilds dispose old dynamic buttons and final disposal releases the entire child tree.
- Keyboard, focus, disabled, accessibility, theme, and DPI behavior come from existing primitives and pass automated/manual checks.
- Pagination is data-source agnostic; DataGrid integration exists only as demo/application composition.
- Both target frameworks build and all tests pass.
- `docs/COMPONENTS.md`, architecture/testing/package docs, README, and changelog are updated.
- The public API fingerprint change is intentionally reviewed and approved; assembly version remains `1.0.0.0`.
- No external dependency, timer, animation engine, or direct theme lifecycle is added.

## Self-review

- **Spec coverage:** The deferred-component requirement to declare reused foundations is satisfied explicitly: Pagination composes ButtonGroup/Button and delegates theme/rendering/DPI/focus behavior to them. State model, page window, validation, lifecycle, demo, docs, and frozen-API handling all map to concrete tasks.
- **Placeholder scan:** No `TBD`, deferred implementation placeholder, or unspecified error-handling step remains. The API fingerprint is intentionally discovered from the deterministic baseline test only after the final exported surface exists; the plan specifies the exact review/update procedure rather than guessing a future hash.
- **Type consistency:** The plan consistently uses `int` for `TotalItems`, `PageSize`, `CurrentPage`, `TotalPages`, and `MaxVisiblePages`; `BootstrapButtonSize` for child sizing; `BootstrapVariant` for semantic color; and `EventHandler` for `PageChanged` without introducing an extra public EventArgs type.
- **Scope check:** Page-size selector UI, remote-loading protocol, automatic DataGrid paging, cancelable navigation, async state, and data-fetch abstractions are intentionally excluded so this remains one independently testable control feature.
