# Dropdown Submenus, Hosted Controls, and Split Button Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the existing `BootstrapDropdown` with arbitrary-depth native submenus and factory-created hosted WinForms controls, then add a Bootstrap-style `BootstrapSplitButton` whose primary region raises the normal command action while its separate chevron region opens the same Dropdown menu infrastructure.

**Architecture:** Keep `ToolStripDropDownMenu`/`ToolStripMenuItem`/`ToolStripControlHost` as the native popup, focus, keyboard, dismissal, and submenu engine. Extend the caller-owned `BootstrapDropdownItem` tree as the only public menu model, validate the tree before mutating native state, and build a recursive short-lived native snapshot for each effective opening. Hosted controls are factory-created so native `ToolStripControlHost` disposal has unambiguous ownership. Add `BootstrapSplitButton : Control` as a composite of two framework-owned `BootstrapButton` children plus one internal `BootstrapDropdown`; it shares connected-button seam/corner logic with `BootstrapButtonGroup` and uses an internal anchored-show path so the popup aligns to the whole split button rather than only the chevron child.

**Tech Stack:** C#, native Windows Forms `ToolStripDropDownMenu`, `ToolStripMenuItem`, `ToolStripSeparator`, `ToolStripControlHost`, existing `BootstrapDropdown`, `BootstrapButton`, `BootstrapButtonGroup`, Theme / Rendering / Icons / Compatibility infrastructure, `BootstrapVariant`, `BootstrapThemeManager`, `BootstrapThemeMetrics`, `DpiScaler`, `CornerRadius`, `IconDescriptor`, `IIconRenderer`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** Extension of `docs/plans/20260828-007-bootstrap-dropdown.md`, together with the repository rules in `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, and `docs/PUBLIC_API_BASELINE.md`. The feature scope added by this plan is exactly: nested Dropdown submenus, arbitrary hosted controls inside Dropdown menus, and split-button Dropdown behavior.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; public types added by this plan remain under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile from one shared implementation for both `net48` and `net8.0-windows` wherever practical.
- Preserve all existing Stage 7 Dropdown behavior unless this plan explicitly extends it: `Target`, `Items`, `Variant`, `MinimumWidth`, `Opened`, `Closed`, `Show()`, `Close()`, command activation, checked-state policy, target ownership, native popup lifecycle, theme/DPI rendering, and outside-click/Escape dismissal remain compatible.
- Continue using native `ToolStripDropDownMenu`; do not add a transparent/custom top-level `Form`, global mouse or keyboard hooks, a replacement message loop, or a second focus/placement engine.
- Native WinForms remains authoritative for Up/Down/Home/End/Left/Right/Enter/Escape navigation, submenu activation, focus transfer, outside-click dismissal, monitor working-area adjustment, and menu auto-close behavior.
- `BootstrapDropdownItem` instances and every nested `DropDownItems` collection are caller-owned models. The framework never disposes model objects.
- Every native `ToolStripItem`, generated icon `Bitmap`, `ToolStripControlHost`, and factory-created hosted `Control` is framework-owned for the lifetime of the current native snapshot and is disposed deterministically on rebuild or Dropdown disposal.
- Do not accept a caller-owned `Control` instance directly as the hosted-control API. `ToolStripControlHost` participates in native disposal of the hosted control, so a direct instance API would make ownership surprising. Use a factory that creates a fresh control for an effective opening.
- A hosted-control factory is invoked only while building an effective native snapshot. It must return a new non-disposed `Control`; returning `null` or an already disposed control is an invalid model and fails before the popup opens.
- Hosted controls remain alive after the popup closes until the native snapshot is next rebuilt or the owning Dropdown is disposed. This matches the existing Stage 7 snapshot lifetime and avoids disposing native ToolStrip items from inside the native `Closed` callback.
- The framework owns host chrome and placement around arbitrary hosted controls, but it does not rewrite application-specific interior state such as text, selected value, bindings, custom colors, or application event subscriptions. Factory code remains responsible for configuring the control it returns.
- `Enabled = false` on a hosted-control item disables both the `ToolStripControlHost` and returned control before opening so it cannot acquire interactive focus.
- A normal `Item` with one or more `DropDownItems` is a submenu parent. It is navigation-only while children exist: opening/navigating the submenu does not raise the parent model's `Click` event, and internal activation helpers must report such parents as non-activatable.
- A normal `Item` with no children is a leaf command and preserves the existing activation contract: enabled activation raises `Click` exactly once and does not mutate `Checked` automatically.
- `Separator` and `HostedControl` nodes cannot contain `DropDownItems`. Invalid trees fail before native rows are added.
- The same `BootstrapDropdownItem` instance may appear only once in one Dropdown tree. Duplicate-instance reuse and ancestor cycles fail validation before opening; the public model is a tree, not a graph.
- Structural and property changes while a popup is open are not live-bound. They apply on the next effective opening, except existing runtime presentation setters that already affect an open root popup (`Variant`, `MinimumWidth`, theme refresh) continue to do so for both classic-target and split-button opening paths.
- Image/check margins are computed independently for each native menu level. A child menu with icons/checks gets its own aligned margins even when the root does not, and vice versa.
- Submenu arrows remain native `ToolStripMenuItem` affordances. Do not add a second chevron into submenu item text or icon slots.
- Root `MinimumWidth` remains a logical 96-DPI floor for the root popup only. Submenus use native content measurement; do not add another public width property in this scope.
- Runtime theme changes must refresh every currently materialized menu level and every generated menu icon without recreating public model objects or factory-created hosted controls.
- Arbitrary hosted controls are not automatically re-themed by mutating their own custom properties. Framework-owned `ToolStrip` surfaces, border, padding, and surrounding chrome update; framework controls hosted by callers continue receiving theme updates through their own existing theme subscriptions.
- `BootstrapSplitButton` is a framework composite control, not a wrapper around native `ToolStripSplitButton`. It reuses `BootstrapButton` presentation and the existing Dropdown popup model/rendering so it looks and behaves consistently with the library.
- The split button owns both internal `BootstrapButton` children and its internal `BootstrapDropdown`. No dedicated strongly-typed public accessor is added for either child. Because `BootstrapSplitButton : Control`, the inherited WinForms `Controls` collection remains publicly enumerable by platform design; callers may technically obtain child references through that inherited surface, but those child controls remain framework-owned implementation details and callers must not remove, replace, mutate ownership-sensitive state on, or dispose them directly.
- The primary split region raises the inherited `Click` event of `BootstrapSplitButton` exactly once. The chevron region never raises the split button's primary `Click`; it only toggles the dropdown.
- `BootstrapSplitButton.Loading = true` shows the existing spinner-backed loading presentation on the primary region and disables dropdown opening from the chevron region until loading ends.
- The split button has two native focusable button regions, matching split-command semantics: Tab/Shift+Tab can focus the primary and chevron regions; Enter/Space on the primary activates the command; Enter/Space on the chevron opens/closes the menu; native menu keys take over once opened.
- The split button popup aligns to the left edge immediately below the complete split-button bounds. It must not align only below the narrow chevron child.
- Split-button `Variant`, `Outline`, `ButtonSize`, `BorderRadius`, enabled state, and icon renderer are applied coherently to both button regions; caller content icon/text/loading text belong to the primary region, while the secondary region always uses framework `ChevronDown` as the structural affordance.
- `Font` is an inherited caller-facing property and therefore part of the effective split-button contract. A caller-assigned split-button font is forwarded to both child buttons and becomes the popup presentation font. If the caller has not customized the outer font, the two framework child buttons continue using their existing theme-font behavior; the outer control may mirror the primary region for metadata/layout purposes without disabling the child buttons' theme subscriptions.
- Do not shadow or re-declare `AccessibleName` on `BootstrapSplitButton`. The two region accessible names must be resolved dynamically from the current outer `AccessibleName`/`Text` through internal accessibility objects so assignments made through a `Control` reference are observed without adding accidental public API.
- Connected split regions use the same seam-overlap and outer-corner rules as `BootstrapButtonGroup`. Extract/reuse focused internal layout logic rather than maintaining two subtly different seam/radius algorithms.
- Child controls added through `Controls.Add(...)` are disposed by `base Control.Dispose(disposing)`. `BootstrapSplitButton.Dispose(bool)` must detach its own child handlers and explicitly dispose only non-child owned resources such as the internal `BootstrapDropdown`; it must not explicitly dispose `_primaryButton` or `_dropDownButton` before calling `base.Dispose(disposing)`.
- Preserve designer-safe parameterless construction. No DI container, application bootstrap, initialized service locator, or new external package is required.
- All new public/protected members receive XML documentation. `TreatWarningsAsErrors` and `CS1591` must remain green.
- This plan extends a frozen public surface: the public-API baseline test must fail intentionally after the new public members are introduced, the exported surface must be reviewed, then `docs/PUBLIC_API_BASELINE.md` and the approved fingerprint are updated in the same implementation series.
- Final completion requires both target-framework builds, both relevant test targets, real-desktop Dropdown manual checks, demo coverage, documentation updates, public-API review, and no resource/event ownership regressions.

---

## Native Behavior to Preserve and Characterize

Before implementation, preserve evidence for the native behaviors this feature deliberately composes instead of replacing:

- `ToolStripDropDownItem.DropDownItems` is the native submenu hierarchy used by `ToolStripMenuItem`.
- `ToolStripControlHost` is the native host for arbitrary WinForms `Control` instances inside a ToolStrip/Dropdown.
- Native ToolStrip disposal owns native item resources and participates in disposing a hosted control; this is why the public model stores a factory instead of a reusable caller-owned control instance.
- Native `ToolStripMenuItem` submenu keyboard behavior owns Right/Left navigation and selection transitions.
- Native `ToolStripDropDown.AutoClose = true` remains authoritative for closing the entire menu chain when a leaf command activates or focus leaves the menu hierarchy.
- Native `Control.Controls` remains a public platform collection for composite controls. The framework can avoid adding dedicated child accessors, but cannot truthfully make child references impossible to obtain while implementing the split button as ordinary WinForms child controls.
- `Control.Dispose(bool)` owns disposal of child controls in `Controls`; framework code should not introduce a second explicit child-disposal path.

Useful platform references for implementation review:

- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripdropdownitem.dropdownitems?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripmenuitem?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripcontrolhost?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripcontrolhost.-ctor?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripdropdown.autoclose?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control.controls?view=windowsdesktop-8.0>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.control.dispose?view=windowsdesktop-8.0>

Do not make automated tests depend on exact undocumented pixel placement or runtime-private key-routing algorithms. Characterize only stable public/native behavior and verify full interaction on a real Windows desktop.

---

## Public Contract Added by This Plan

### Extended Dropdown item model

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public enum BootstrapDropdownItemKind
{
    Item = 0,
    Separator = 1,
    HostedControl = 2
}

public sealed class BootstrapDropdownItem
{
    public BootstrapDropdownItem();
    public BootstrapDropdownItem(BootstrapDropdownItemKind kind);

    public BootstrapDropdownItemKind Kind { get; }
    public string Text { get; set; }
    public IconDescriptor? Icon { get; set; }
    public bool Enabled { get; set; }
    public bool Checked { get; set; }
    public object? Tag { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapDropdownItemCollection DropDownItems { get; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<Control>? HostedControlFactory { get; set; }

    public event EventHandler? Click;
}
```

Existing members retain their current defaults. New defaults and validation are:

| Member/state | Contract |
| --- | --- |
| `DropDownItems` | one stable empty collection instance for every item |
| `HostedControlFactory` | `null` |
| `Kind == Item`, no children | existing leaf command behavior |
| `Kind == Item`, children present | native submenu parent; `CanActivate(...) == false`; parent `Click` does not dispatch |
| `Kind == Separator` | `DropDownItems` must be empty and `HostedControlFactory` must be `null` at effective opening |
| `Kind == HostedControl` | `DropDownItems` must be empty; `HostedControlFactory` must be non-null and return a new, non-disposed `Control` |
| duplicate model instance | invalid tree; effective opening throws `InvalidOperationException` before native snapshot mutation |
| undefined `BootstrapDropdownItemKind` | existing `ArgumentOutOfRangeException` behavior |

`Text`, `Icon`, `Checked`, and `Click` remain stored model properties for all kinds, but are ignored for native hosted-control and separator activation. `Enabled` is meaningful for a hosted-control row and is propagated to its native host/control.

### New split-button control

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Click))]
public class BootstrapSplitButton : Control
{
    public BootstrapSplitButton();

    public override string Text { get; set; }
    public BootstrapVariant Variant { get; set; }
    public bool Outline { get; set; }
    public BootstrapButtonSize ButtonSize { get; set; }
    public IconDescriptor? Icon { get; set; }
    public BootstrapIconPosition IconPosition { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IIconRenderer IconRenderer { get; set; }

    public int BorderRadius { get; set; }
    public bool Loading { get; set; }
    public string LoadingText { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapDropdownItemCollection Items { get; }

    public int MinimumWidth { get; set; }

    public event EventHandler? Opened;
    public event EventHandler? Closed;

    public void ShowDropDown();
    public void CloseDropDown();
}
```

Defaults mirror `BootstrapButton`/`BootstrapDropdown`: `Text = string.Empty`, `Variant = Primary`, `Outline = false`, `ButtonSize = Default`, `Icon = null`, `IconPosition = Left`, default framework icon renderer, `BorderRadius = -1`, `Loading = false`, `LoadingText = string.Empty`, stable empty `Items`, and `MinimumWidth = 0`.

The inherited `Control.Click` event is the public primary-action event; do not introduce `PrimaryClick`, `ButtonClick`, or another alias. `Opened`/`Closed` forward the internal Dropdown's actual native lifecycle. `ShowDropDown()` uses the same benign no-op policy as Dropdown for disabled/loading/empty states and is idempotent while open; `CloseDropDown()` is idempotent while closed.

Inherited `Font`, `AccessibleName`, `AccessibleDescription`, `Enabled`, `Controls`, `TabStop`, and ordinary `Control` members remain platform API rather than newly declared split-button API. The implementation must nevertheless honor the effective behavior defined in this plan for font propagation, accessibility, ownership, and focus.

### Public surface deliberately not added

- No dedicated public property or method exposing `ToolStripDropDownMenu`, `ToolStripMenuItem`, `ToolStripControlHost`, `_primaryButton`, `_dropDownButton`, renderer, native popup handles, or internal anchored-show/test seams. The inherited `Control.Controls` collection remains present because that is WinForms platform API; this plan does not claim otherwise.
- No `Control` instance property on `BootstrapDropdownItem`; use `HostedControlFactory` so ownership is explicit.
- No live `INotifyPropertyChanged`/`INotifyCollectionChanged` binding while the popup is open.
- No async hosted-control factory or lazy async menu provider.
- No public submenu placement engine; native ToolStrip placement remains authoritative.
- No public submenu depth limit. Tree validation prevents cycles; practical depth remains subject to native UI usability.
- No radio groups, check groups, automatic checked-state mutation, shortcut registration, global hotkeys, or custom menu animation.
- No split-button selection policy; the split control is a command + dropdown affordance, not a toggle/radio control.
- No separate public dropdown alignment property in this scope. The classic `BootstrapDropdown` keeps its current target anchoring, while `BootstrapSplitButton` internally anchors below its full bounds.
- No shadowed `AccessibleName`, `Font`, or other inherited `Control` property merely to observe assignments.

---

## File Structure

### Product files

- Modify `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemKind.cs` — add `HostedControl` without renumbering existing enum members.
- Modify `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItem.cs` — add stable child collection and hosted-control factory.
- Modify `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs` — validate/build recursive snapshots, define leaf-only activation, recursively refresh presentation/resources, and add an internal anchored-show path shared by split button; open-popup `MinimumWidth` must work through either classic target or active split presentation source.
- Modify `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs` — render submenu arrows and hosted-control surrounding chrome coherently when native defaults require explicit theme painting; keep existing palette/metrics helpers authoritative.
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapConnectedButtonLayoutLogic.cs` — internal seam/corner calculations reused by ButtonGroup and SplitButton.
- Modify `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapButtonGroup.cs` — delegate seam/corner calculation to the extracted internal helper without changing public behavior.
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSplitButton.cs` — composite public control, child-button synchronization, font policy, primary/dropdown routing, layout, lifecycle, dynamic accessibility, and disposal.

### Tests

- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs` — native characterization, tree validation, leaf-only activation, recursive snapshots, hosted-control ownership, nested activation, theme/DPI/resource behavior, active presentation source, and open-popup `MinimumWidth`.
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapConnectedButtonLayoutLogicTests.cs` — seam/radius helper coverage.
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSplitButtonTests.cs` — defaults, forwarded properties, font semantics, layout, focus regions, accessibility names, loading/disabled behavior, primary versus dropdown activation, inherited `Controls` ownership expectations, lifecycle, disposal, theme/DPI.
- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/NavigationDemoFormTests.cs` — assert advanced Dropdown and split-button scenarios are represented.
- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs` — approve the new exported surface only after API review.

### Demo and docs

- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs` — add nested submenu, hosted control, mixed nested/hosted, split-button, custom-font, keyboard, and lifetime scenarios.
- Modify `docs/COMPONENTS.md` — extend Dropdown contract and document `BootstrapSplitButton`, including the inherited `Controls` caveat and ownership contract.
- Modify `docs/TESTING.md` — add advanced Dropdown/split-button manual and automated matrix.
- Modify `docs/PUBLIC_API_BASELINE.md` — record approved public additions and compatibility rationale.
- Modify `README.md` and `docs/PACKAGE_README.md` only if their component inventories/examples currently enumerate Dropdown APIs; keep those inventories synchronized.

---

### Task 1: Characterize native submenu, hosted-control, and composite-control ownership behavior

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`
- Modify or create focused split-button characterization tests only if needed.

**Interfaces:**
- Consumes: native `ToolStripDropDownMenu`, `ToolStripMenuItem.DropDownItems`, `ToolStripControlHost`, `Control.Controls`, and normal WinForms child disposal.
- Produces: executable evidence that later tasks may safely rely on native submenu hierarchy, hosted-control disposal, and base-control child ownership.

- [ ] **Step 1: Add a native submenu characterization test** verifying a `ToolStripMenuItem` owns native child items and a nested leaf can raise its own native `Click`.
- [ ] **Step 2: Add a hosted-control ownership characterization test** verifying disposing `ToolStripControlHost` disposes the hosted `Control` on both supported runtimes.
- [ ] **Step 3: Add a composite child ownership characterization test** with a small local `Control` subclass that adds a tracking child, calls only `base.Dispose(disposing)`, and verifies the child is disposed once. This test exists to prevent the later split implementation from adding an unnecessary explicit child-disposal path.
- [ ] **Step 4: Run characterization tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~Characterization"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Characterization"
```

Expected: PASS with the same ownership conclusions. If behavior differs on a supported runtime, stop and document the target-specific native behavior before designing around it.

- [ ] **Step 5: Commit characterization evidence.**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSplitButtonTests.cs
git commit -m "test: characterize advanced dropdown primitives"
```

Omit the split-button test path from `git add` if the characterization was kept in an existing test file.

---

### Task 2: Extend the public Dropdown item model for child menus and hosted-control factories

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemKind.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItem.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: existing `BootstrapDropdownItemCollection` null rejection and insertion-order semantics.
- Produces: `BootstrapDropdownItemKind.HostedControl`, stable `BootstrapDropdownItem.DropDownItems`, and nullable `Func<Control> HostedControlFactory` used by the recursive native builder.

- [ ] Write failing model-contract tests for enum numeric compatibility, stable child collection, nested collection null rejection, factory default, and existing constructor behavior.
- [ ] Add `HostedControl = 2` without renumbering `Item = 0` or `Separator = 1`.
- [ ] Add one stable `_dropDownItems` collection in every model instance.
- [ ] Add `[Browsable(false)]`, `[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]` nullable `HostedControlFactory` with XML docs stating that the returned control becomes framework-owned for the native snapshot.
- [ ] Run all Dropdown model tests on both targets.
- [ ] Commit the model extension.

```bash
git commit -m "feat: model nested and hosted dropdown items"
```

---

### Task 3: Validate Dropdown trees before native snapshot mutation

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: `Kind`, `DropDownItems`, `HostedControlFactory` from Task 2.
- Produces: one internal tree-validation path invoked before `ClearNativeItems()`/native rebuild; guarantees recursive construction never sees malformed model state.

- [ ] Add failing tests for separator children, separator factory, hosted item without factory, hosted item with children, normal item with a hosted factory, duplicate-instance reuse, direct/indirect cycles, and a valid mixed-depth tree.
- [ ] Implement reference-identity traversal with one `HashSet<BootstrapDropdownItem>` for the whole tree. Because `BootstrapDropdownItem` is sealed and does not override equality, ordinary reference semantics are sufficient.
- [ ] Keep validation callable from tests only through an `internal` seam; do not make it public API.
- [ ] Ensure validation completes before native snapshot mutation. A validation exception leaves the previous closed snapshot/resources untouched until normal cleanup/disposal.
- [ ] Run focused validation tests on both targets.
- [ ] Commit tree validation.

```bash
git commit -m "feat: validate dropdown item trees"
```

---

### Task 4: Build recursive native menu snapshots, define leaf-only activation, and own hosted controls safely

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: validated public tree from Task 3, existing icon/theme snapshot infrastructure.
- Produces: recursive native `ToolStripMenuItem.DropDownItems`, factory-created `ToolStripControlHost`, leaf-only activation semantics, deterministic recursive disposal.

- [ ] **Step 1: Write failing recursive-activation tests.** Verify parent submenu models do not dispatch `Click`, nested leaves dispatch exactly once, disabled leaves do not dispatch, checked state is not auto-toggled, separators work at nested levels, and `Tag` maps native rows back to the correct model.
- [ ] **Step 2: Correct the internal activation contract.** Update `CanActivate(...)` so a submenu parent is never considered a command:

```csharp
internal static bool CanActivate(BootstrapDropdownItem item)
{
    return item.Kind == BootstrapDropdownItemKind.Item
        && item.Enabled
        && item.DropDownItems.Count == 0;
}
```

Add a direct test asserting `CanActivate(parentWithChildren) == false`. `ActivateItem(...)` remains the single command-dispatch helper and must inherit this leaf-only semantic.

- [ ] **Step 3: Replace root-only `ItemClicked` dispatch with direct native leaf handlers.** Attach the framework handler only to native leaf `ToolStripMenuItem` instances. Remove `_dropDown.ItemClicked` subscription/handler after the new route is green so one activation has one dispatcher.
- [ ] **Step 4: Implement one recursive `PopulateNativeItems(...)` method** targeting any `ToolStripItemCollection`. Parent items receive recursively populated `DropDownItems`; separators and hosted controls are inserted at any valid level.
- [ ] **Step 5: Implement factory-created hosted-control rows.** Factory return must be non-null and not disposed. Propagate `Enabled` to both control and host.
- [ ] **Step 6: Add lifetime tests.** Factory is invoked once per effective rebuild, rebuild disposes previous controls once, Dropdown disposal disposes the current hosted control once, and closing alone does not dispose the current snapshot.
- [ ] **Step 7: Make exception paths leak-free.** If a later factory fails after earlier native rows/controls/images were created, dispose the partial new snapshot before rethrowing and leave `Opened` at zero.
- [ ] **Step 8: Run all Dropdown tests on both targets.**
- [ ] **Step 9: Commit recursive snapshot construction.**

```bash
git commit -m "feat: build nested dropdown snapshots"
```

---

### Task 5: Apply renderer, image, theme, and DPI presentation recursively

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: native tree from Task 4, existing `BootstrapDropdownRenderer.ResolvePalette/ResolveMetrics`, active presentation source `IconRenderer`/`Font`/DPI.
- Produces: level-aware image/check margins, recursive icon generation, theme refresh, themed submenu arrows/borders, hosted-control surrounding chrome.

- [ ] Add per-level margin tests where only child levels have icons/checks and inverse cases where only the root has them.
- [ ] Add recursive icon/theme tests at root/child/grandchild levels; theme changes rerender every owned image without recreating public item models or factory-created hosted controls.
- [ ] Add recursive ToolStrip traversal helpers covering root dropdown, every submenu dropdown, menu rows, separators, and control hosts.
- [ ] Assign the framework renderer explicitly to each materialized submenu rather than relying on implicit inheritance.
- [ ] Compute `ShowImageMargin`/`ShowCheckMargin` from sibling models at each level. Hosted controls/separators do not create empty image/check columns.
- [ ] Clear image references recursively before disposing/recreating owned images.
- [ ] Preserve native submenu arrows. Only override `OnRenderArrow` if the custom renderer suppresses/mismatches them; do not paint a second arrow in content layout.
- [ ] Run Dropdown/renderer tests on both targets and the tested DPI matrix 96/120/144/168/192.
- [ ] Commit recursive presentation.

```bash
git commit -m "feat: theme nested dropdown levels"
```

---

### Task 6: Extract connected-button seam/corner logic for reuse

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapConnectedButtonLayoutLogic.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapButtonGroup.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapConnectedButtonLayoutLogicTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapButtonGroupTests.cs`

**Interfaces:**
- Consumes: `BootstrapThemeMetrics.BorderWidth`, `DpiScaler`, `CornerRadius`, configured/theme `BorderRadius` values.
- Produces: internal seam overlap and connected-edge corner helpers used by existing `BootstrapButtonGroup` and new `BootstrapSplitButton`.

- [ ] Add pure tests for seam overlap at 96/120/144/168/192 DPI and horizontal/vertical corner shapes.
- [ ] Implement a narrow internal helper; do not add public API.
- [ ] Refactor only existing ButtonGroup private seam/corner math. Do not change ordering, selection policy, equal-width behavior, or public members.
- [ ] Run ButtonGroup + helper tests on both targets.
- [ ] Commit shared layout logic.

```bash
git commit -m "refactor: share connected button layout logic"
```

---

### Task 7: Add an internal anchored-show path and make active presentation state authoritative while open

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: existing public `Show()` target path and recursive snapshot/presentation implementation.
- Produces: internal `ShowFrom(BootstrapButton presentationSource, Control anchor, Point location)` for `BootstrapSplitButton`; one `_activePresentationSource` for runtime presentation updates while any popup is visible.

- [ ] Write an internal-anchor lifecycle test using a visible STA host. Verify actual `Opened`/`Closed` counts and that public `Target` remains unchanged.
- [ ] Funnel public `Show()` through one internal opening path:

```csharp
public void Show()
{
    ThrowIfDisposed();
    var target = _target ?? throw new InvalidOperationException(
        "A BootstrapDropdown Target must be assigned before Show is called.");

    ShowFrom(target, target, new Point(0, target.Height));
}
```

- [ ] `ShowFrom(...)` validates non-null/non-disposed source and anchor, applies benign no-op state checks, validates the model tree before mutation, builds the snapshot, records the active presentation source, applies presentation, and calls `_dropDown.Show(anchor, location)`.
- [ ] Keep `_activePresentationSource` as a non-owning reference only while the popup is visible. Clear it from native `Closed`, failed-open cleanup, and disposal.
- [ ] **Fix runtime `MinimumWidth` behavior for split openings.** The public `MinimumWidth` setter must update an already-visible root popup using the current presentation source, not only `_target`. Resolve the live source as `_activePresentationSource` while visible; fall back to `_target` only when appropriate. The setter keeps existing DPI scaling semantics and remains a no-op for submenus.
- [ ] Theme refresh and any other open-popup presentation refresh similarly use `_activePresentationSource`; classic target event wiring continues to use `_target` exactly as before.
- [ ] Add tests: classic `MinimumWidth` while open still updates, `ShowFrom` with `Target == null` opens from an internal source, and changing `MinimumWidth` while such a split-style popup is visible updates root `MinimumSize` at the active source DPI.
- [ ] Run all Dropdown tests on both targets.
- [ ] Commit shared anchored-show/presentation state.

```bash
git commit -m "refactor: share dropdown anchored show path"
```

---

### Task 8: Implement `BootstrapSplitButton` visual composition, property/font synchronization, and dynamic accessibility

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSplitButton.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSplitButtonTests.cs`

**Interfaces:**
- Consumes: `BootstrapButton`, `BootstrapConnectedButtonLayoutLogic`, framework `ChevronDown`, existing theme/DPI metrics.
- Produces: two-region composite control with coherent BootstrapButton appearance, correct preferred/custom-width layout, primary `Click` forwarding, defined inherited-font behavior, and accessibility that does not require new public properties.

- [ ] **Step 1: Write failing default/property tests.** Verify the declared public contract, stable `Items`, null rejection for `IconRenderer`, and no dedicated public child-button accessors.
- [ ] **Step 2: Characterize the inherited `Controls` reality in tests/documentation.** It is acceptable and expected that the two framework-owned children can be enumerated through `split.Controls`; tests must not assert that child references are impossible to obtain. Instead assert that no additional strongly-typed public accessor exists and disposal ownership remains with the parent.
- [ ] **Step 3: Write failing layout tests.** Cover preferred width, seam overlap, equal heights, wider custom bounds, no negative child widths, and 96/120/144/168/192 DPI corner/seam behavior.
- [ ] **Step 4: Write failing primary-action tests.** Primary child activation raises outer inherited `Click` exactly once with `BootstrapSplitButton` as sender. Disabled/loading suppress activation.
- [ ] **Step 5: Create the composite children.** Outer `TabStop = false`, children remain focusable. Secondary child always uses framework `ChevronDown`; caller `Text`/`Icon` never replaces it.
- [ ] **Step 6: Forward appearance/behavior properties.** `Text`, `Icon`, `IconPosition`, `Loading`, `LoadingText` -> primary only. `Variant`, `Outline`, `ButtonSize`, `IconRenderer`, enabled state -> both. `BorderRadius` supplies outer connected radii. `Loading = true` disables secondary interaction.
- [ ] **Step 7: Implement the inherited Font contract without fighting child theme subscriptions.**

  Required policy:

  - Do not declare a new `Font` property.
  - Track whether the outer font has been explicitly/customly assigned through normal `OnFontChanged` handling, with an internal synchronization guard so framework mirroring does not mark the font as custom.
  - While the split button is still using framework/default theme font behavior, allow each `BootstrapButton` child to keep its own existing theme subscription. Do not repeatedly assign `child.Font` during theme refresh, because doing so would intentionally disable that child's internal theme-font mode.
  - Mirror the primary child font to the outer control only under an internal guard when needed for metadata/layout consistency.
  - When caller code changes `split.Font`, forward that font to both child buttons. That intentionally converts both children to custom-font mode, matching existing `BootstrapButton` semantics.
  - Once caller-custom font mode is active, later theme changes must not overwrite it.
  - The dropdown presentation source is `_primaryButton`, therefore an open split dropdown automatically uses the same effective caller/custom or themed font as the visible primary region.

  Add tests for default themed font, caller custom font forwarding, theme change before customization, theme change after customization, and dropdown presentation font equivalence.

- [ ] **Step 8: Implement dynamic accessibility without shadowing `AccessibleName`.**

  Use an internal child-button subclass/accessibility object (or equivalent internal accessibility adapter) so accessible names are computed at query time from the owner:

  - primary region name = current outer `AccessibleName` when non-empty, otherwise current `Text`;
  - chevron region name = `<primary accessible name> menu` (with a sensible fallback such as `Menu` when the primary name is empty);
  - chevron accessible description explains that it opens additional commands;
  - changing outer `Text` or assigning `AccessibleName` through either a `BootstrapSplitButton` reference or a base `Control` reference is reflected without requiring a shadowed property or an `AccessibleNameChanged` event that WinForms does not provide;
  - no additional public/protected accessibility type/member is introduced solely for the test seam.

  Tests should query child `AccessibilityObject.Name`/description after changing outer metadata, rather than depending on cached child `AccessibleName` property values.

- [ ] **Step 9: Implement preferred-size/custom-width layout.** Chevron keeps its preferred width; primary receives remaining width. Use shared seam logic and connected corner radii.
- [ ] **Step 10: Handle `OnDpiChangedAfterParent`, `OnFontChanged`, `OnEnabledChanged`, and `OnLayout` with reentrancy guards as needed; do not add timers or persistent GDI caches.**
- [ ] **Step 11: Run split visual/property/font/accessibility tests on both targets.**
- [ ] **Step 12: Commit the split-button shell.**

```bash
git commit -m "feat: add bootstrap split button shell"
```

---

### Task 9: Wire split-button Dropdown lifecycle, anchoring, runtime state, and ownership

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSplitButton.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSplitButtonTests.cs`

**Interfaces:**
- Consumes: internal `BootstrapDropdown.ShowFrom(...)` from Task 7 and recursive item model from Tasks 2–5.
- Produces: stable split `Items`, separate chevron toggle behavior, popup aligned below full split bounds, lifecycle forwarding, loading/disabled no-op behavior, deterministic ownership with one child-disposal path.

- [ ] Write routing tests: primary click does not open; chevron opens/toggles without primary click; public show/close uses same lifecycle; `Opened`/`Closed` sender is outer split; empty/disabled/loading states do not open; nested leaf activation works through split `Items`.
- [ ] Add a full-width anchor regression test. Assert requested `anchor = this`, `location = new Point(0, Height)`, without asserting undocumented final screen clamping pixels.
- [ ] Add loading/selected-chevron tests. While open, secondary `Selected = true`; native close resets it. Setting loading true while open closes first, then disables opening.
- [ ] Create one internal `BootstrapDropdown`. Do not assign its public `Target`; open through `ShowFrom(_primaryButton, this, new Point(0, Height))`.
- [ ] `Items` directly returns `_dropdown.Items`. `Variant` and `MinimumWidth` synchronize to `_dropdown`. Because Task 7 made `_activePresentationSource` authoritative while visible, changing split `MinimumWidth` while the split popup is open must update the live root popup immediately.
- [ ] Outer disable while open closes Dropdown. `Loading = true` while open closes before disabling secondary interaction.
- [ ] **Dispose with exactly one child-control disposal owner.** In `Dispose(bool disposing)`:

  1. close the popup if needed;
  2. detach `_dropdown.Opened`/`Closed` and child event handlers owned by `BootstrapSplitButton`;
  3. explicitly dispose the internal `_dropdown` because it is an owned `Component`, not a child in `Controls`;
  4. do **not** call `_primaryButton.Dispose()` or `_dropDownButton.Dispose()` directly;
  5. call `base.Dispose(disposing)` and let `Control` dispose child controls in its inherited `Controls` collection exactly once.

  Add tracking tests that child controls reach disposed state through parent disposal and that no implementation-owned event subscription survives. Tests should not require custom double-dispose counting from framework controls whose `Dispose()` is idempotent; instead verify there is no explicit second disposal path in the designed lifecycle and no exception/resource regression.

- [ ] Do not attempt to block callers from enumerating inherited `Controls` through unsupported hiding/replacement tricks. Document those children as framework-owned implementation details.
- [ ] Run split + Dropdown tests on both targets.
- [ ] Commit complete split-button behavior.

```bash
git commit -m "feat: connect split button dropdown behavior"
```

---

### Task 10: Expand Navigation demo and real-desktop verification matrix

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/NavigationDemoFormTests.cs`

**Interfaces:**
- Consumes: complete advanced Dropdown and `BootstrapSplitButton` public APIs.
- Produces: visible integration examples and manual verification coverage without exposing internal implementation members.

- [ ] Extend demo coverage assertions for nested submenu, hosted controls, mixed nested/hosted content, split primary action, split nested Dropdown, runtime Light/Dark, and a custom-font split example.
- [ ] Add nested command scenarios with visible status updates for leaf activation depth.
- [ ] Add hosted-control factory scenarios using fresh native/framework controls and visible state updates. Demo text must state that returned controls become framework-owned snapshot controls.
- [ ] Add a split-button scenario with primary action, nested menu, hosted control, loading toggle, and `AccessibleName`.
- [ ] Add a custom-font scenario demonstrating that split primary/chevron and dropdown presentation remain coherent after caller font assignment and later theme switches.
- [ ] Expand manual matrix:

  1. Mouse: primary versus chevron; repeated open/close; outside click.
  2. Keyboard: Tab/Shift+Tab between regions; Enter/Space on each; native menu Up/Down/Home/End/Right/Left/Enter/Escape.
  3. Hosted controls: focus/edit/toggle, move back to menu items, outside-click dismissal, reopen and confirm snapshot/factory state policy.
  4. States: disabled leaf/submenu/host, checked leaf, split loading state.
  5. Theme/font: Light/Dark while root/submenu visible; default themed fonts; caller custom split font remains custom across theme changes; popup matches primary region font.
  6. Accessibility: inspect primary and menu region names before/after changing outer `Text` and `AccessibleName`.
  7. DPI: 100/125/150/175/200%, including submenu arrows, margins, split seam, chevron sizing, custom font layout.
  8. Screens: bottom/right edges and secondary monitor; native working-area placement remains usable.
  9. Lifetime: repeated open/close/rebuild, form disposal while nested popup open, no stale windows/disposed-control exceptions/increasing GDI artifacts.
  10. Ownership: inherited `Controls` may enumerate internal region controls, but demo/application code does not mutate/remove/dispose them.

- [ ] Run demo tests on both targets.
- [ ] Run the demo manually on a real Windows desktop.
- [ ] Commit demo integration.

```bash
git commit -m "demo: showcase advanced dropdown composition"
```

---

### Task 11: Update component/testing documentation and review public API baseline

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/PUBLIC_API_BASELINE.md`
- Modify: `README.md` if its component inventory requires synchronization
- Modify: `docs/PACKAGE_README.md` if its package inventory/examples require synchronization
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`

**Interfaces:**
- Consumes: implementation-complete public surface from Tasks 2 and 8–9.
- Produces: stable documented contract and intentionally reviewed v1 public-API fingerprint.

- [ ] Run the public-API baseline before approving changes. Expected: FAIL for deliberate additive API.
- [ ] Review exported surface. Expected additions are only:

```text
BootstrapDropdownItemKind.HostedControl
BootstrapDropdownItem.DropDownItems
BootstrapDropdownItem.HostedControlFactory
BootstrapSplitButton and the exact declared public members listed in this plan
```

- [ ] Explicitly reject accidental exports such as child-button accessors, native ToolStrip members, layout helpers, `ShowFrom`, snapshot builders, test seams, shadowed `AccessibleName`, or shadowed `Font`.
- [ ] Document recursive Dropdown semantics, hosted-control ownership/lifetime, validation rules, per-level margins, split routing/loading/focus, full-width anchoring, and examples.
- [ ] Document the composite ownership nuance accurately: `BootstrapSplitButton` does not add strongly-typed child accessors, but inherited WinForms `Controls` remains public; those children are framework-owned and unsupported for caller mutation/removal/disposal.
- [ ] Document inherited Font policy and dynamic region accessibility behavior.
- [ ] Update `docs/TESTING.md` with automated/manual boundaries, especially native submenu keyboard navigation, hosted focus, monitor placement, multi-DPI seam, custom-font persistence, accessibility-name queries, and repeated hosted-control disposal.
- [ ] Synchronize README/package inventories only when needed.
- [ ] Update `Phase16PublicApiBaselineTests.cs` and `docs/PUBLIC_API_BASELINE.md` together only after exported surface review. Call out the enum-member addition for switch-expression consumers.
- [ ] Re-run public API tests on both targets.
- [ ] Commit docs/API approval.

```bash
git commit -m "docs: approve advanced dropdown api"
```

---

### Task 12: Full regression, resource-lifetime review, and completion gate

**Files:**
- Review all files modified by Tasks 1–11.
- Modify only the specific implementation/test/doc file responsible for any discovered regression.

**Interfaces:**
- Consumes: complete feature implementation.
- Produces: release-quality evidence that both frameworks remain green and no duplicate popup/layout/resource/ownership infrastructure was introduced.

- [ ] Build product for .NET Framework 4.8.

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
```

- [ ] Build product for .NET 8 Windows.

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
```

- [ ] Run full tests for both frameworks.

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

- [ ] Run repository scripts.

```powershell
./build.ps1
./test.ps1
```

- [ ] Perform explicit ownership/lifecycle review for root/nested native items, leaf handlers, generated icon bitmaps, factory-created hosted controls, `ToolStripControlHost`, active presentation source, classic target wiring, internal Dropdown, child buttons, theme subscriptions, open-popup disposal, and failed factory cleanup.
- [ ] Confirm the split child controls are disposed only through `base Control.Dispose`, while the internal `BootstrapDropdown` is explicitly disposed by the split owner.
- [ ] Confirm no implementation relies on making inherited `Controls` inaccessible or hides platform members to simulate privacy.
- [ ] Confirm `CanActivate(...)` and every activation route define submenu parents as navigation-only.
- [ ] Confirm live `MinimumWidth` and theme refresh work for both classic `_target` and split `_activePresentationSource` openings.
- [ ] Confirm default theme-font mode remains active until caller customizes `BootstrapSplitButton.Font`, then remains custom across theme switches.
- [ ] Confirm dynamic accessible names follow current outer `AccessibleName`/`Text` without a new shadowed public property.
- [ ] Search changed product files for APIs unavailable on `net48`, unintended `#if`, new packages, runtime-private ToolStrip internals, or duplicate placement/focus code.
- [ ] Perform the Task 10 real-desktop matrix one final time.
- [ ] Inspect final diff/public API diff.

```bash
git diff --check
git status --short
```

- [ ] Commit only responsibility-specific final corrections if verification finds regressions; do not create an empty completion commit.

---

## Acceptance Criteria

The plan is complete only when all of the following are true:

1. Existing flat `BootstrapDropdown` behavior and public API remain compatible.
2. Normal Dropdown items can contain arbitrary-depth `DropDownItems` and native keyboard submenu navigation works without custom key routing.
3. Submenu parents are navigation-only: they have no native leaf handler, `CanActivate(...)` returns false for them, and enabled leaf commands raise model `Click` exactly once.
4. `HostedControl` items create fresh controls through `HostedControlFactory`; malformed factories fail before opening; framework-owned hosted controls are disposed deterministically without disposing caller-owned models.
5. Duplicate/cyclic item graphs and illegal separator/host child combinations fail validation before native snapshot mutation.
6. Icons, checks, disabled presentation, margins, theme updates, DPI scaling, renderer, and disposal work at every menu level.
7. `BootstrapSplitButton` provides distinct primary and chevron focus/action regions with connected Bootstrap styling and adds no dedicated public child-button accessor. Documentation accurately acknowledges that inherited WinForms `Controls` can enumerate child controls and states that they remain framework-owned implementation details.
8. Split primary activation raises inherited `Click`; chevron activation toggles Dropdown only; loading suppresses both as specified.
9. Split popup anchors immediately below the complete split-button bounds while native WinForms retains screen collision/working-area behavior.
10. Changing `MinimumWidth` while a popup is open updates the root popup for both classic-target and split-button/internal-anchor paths using the active presentation source DPI.
11. Caller-assigned `BootstrapSplitButton.Font` is propagated to both button regions and the dropdown presentation; default theme-font behavior remains intact until customization, and custom font is not overwritten by later theme changes.
12. Split-region accessible names are resolved dynamically from current outer `AccessibleName`/`Text` without shadowing/redeclaring inherited `AccessibleName`.
13. Split disposal explicitly owns/disposes the internal `BootstrapDropdown`, while child button disposal is left to `base Control.Dispose(disposing)` with no second explicit child-disposal path.
14. `BootstrapButtonGroup` behavior is unchanged after shared seam/corner logic extraction.
15. Light/Dark, 100–200% DPI, disabled/loading, nested keyboard navigation, hosted-control focus, outside-click/Escape, monitor-edge placement, custom-font behavior, accessibility metadata, repeated rebuild, and disposal are verified.
16. `net48` and `net8.0-windows` builds and complete test suites pass.
17. Demo/documentation/public-API baseline describe the final behavior and ownership contract accurately.
18. No new external dependency, custom popup Form, global hook, duplicate placement engine, per-control timer, leaked GDI object, stale event subscription, hidden ownership transfer, shadowed platform property, or unsupported attempt to conceal inherited `Controls` is introduced.

## Self-Review Checklist Applied to This Plan

- **Scope coverage:** nested submenus, arbitrary hosted controls, and split-button behavior each have dedicated model, implementation, tests, demo, docs, lifecycle, and API-baseline tasks.
- **Compatibility coverage:** existing Dropdown public members and enum numeric values are preserved; both target frameworks remain required; native primitives are characterized before dependence.
- **Activation consistency:** submenu parents are non-activatable in both native handler wiring and internal `CanActivate(...)` semantics.
- **Ownership coverage:** caller-owned models/targets and framework-owned native snapshots/host controls are explicit; split child ownership now matches WinForms `Controls`/`Dispose` reality instead of claiming impossible reference privacy.
- **Font coverage:** inherited `Font` has an explicit default-theme versus caller-custom policy, with dropdown presentation tied to the primary region.
- **Accessibility coverage:** region names derive dynamically from outer metadata without shadowing `AccessibleName` or relying on a nonexistent change event.
- **Runtime presentation coverage:** `_activePresentationSource` is authoritative for theme and `MinimumWidth` refresh while any popup is visible, including split openings with no public `Target`.
- **Interaction coverage:** mouse, keyboard, focus, submenu direction, Escape/outside click, split regions, hosted controls, custom font, and accessibility have automated or real-desktop verification paths.
- **Rendering coverage:** per-level margins, icons, Light/Dark, DPI, native submenu arrow, connected seams, and runtime theme refresh are addressed.
- **API discipline:** no native ToolStrip detail, internal child accessor, shadowed `Font`/`AccessibleName`, or internal opening/layout/test seam is exported merely for implementation convenience.
- **Placeholder scan:** every implementation checkpoint identifies concrete files, responsibilities, tests, commands or verification expectations, and commit boundaries; no unresolved review finding remains.