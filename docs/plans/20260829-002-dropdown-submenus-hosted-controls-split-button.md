# Dropdown Submenus, Hosted Controls, and Split Button Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the existing `BootstrapDropdown` with arbitrary-depth native submenus and factory-created hosted WinForms controls, then add a Bootstrap-style `BootstrapSplitButton` whose primary region raises the normal command action while its separate chevron region opens the same Dropdown menu infrastructure.

**Architecture:** Keep `ToolStripDropDownMenu`/`ToolStripMenuItem`/`ToolStripControlHost` as the native popup, focus, keyboard, dismissal, and submenu engine. Extend the caller-owned `BootstrapDropdownItem` tree as the only public menu model, build a recursive short-lived native snapshot for each effective opening, and make hosted controls factory-created so ownership is unambiguous when native `ToolStripControlHost` snapshots are disposed. Add `BootstrapSplitButton : Control` as a composite of two framework-owned `BootstrapButton` children and one internal `BootstrapDropdown`; it shares connected-button seam/corner logic with `BootstrapButtonGroup` and uses an internal anchored-show path so the popup aligns to the whole split button rather than only the chevron child.

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
- The framework owns the host chrome and placement around arbitrary hosted controls, but it does not rewrite application-specific interior state such as the hosted control's text, selected value, bindings, custom colors, or event subscriptions. Factory code remains responsible for configuring the control it returns.
- `Enabled = false` on a hosted-control item disables both the `ToolStripControlHost` and returned control before opening so it cannot acquire interactive focus.
- A normal `Item` with one or more `DropDownItems` is a submenu parent. It is navigation-only while children exist: opening/navigating the submenu does not raise the parent model's `Click` event.
- A normal `Item` with no children is a leaf command and preserves the existing activation contract: enabled activation raises `Click` exactly once and does not mutate `Checked` automatically.
- `Separator` and `HostedControl` nodes cannot contain `DropDownItems`. Invalid trees fail before native rows are added.
- The same `BootstrapDropdownItem` instance may appear only once in one Dropdown tree. Duplicate-instance reuse and ancestor cycles fail validation before opening; the public model is a tree, not a graph.
- Structural and property changes while a popup is open are not live-bound. They apply on the next effective opening, preserving the Stage 7 snapshot contract.
- Image/check margins are computed independently for each native menu level. A child menu with icons/checks gets its own aligned margins even when the root does not, and vice versa.
- Submenu arrows remain native `ToolStripMenuItem` affordances. Do not add a second chevron into submenu item text or icon slots.
- Root `MinimumWidth` remains a logical 96-DPI floor for the root popup only. Submenus use native content measurement; do not add another public width property in this scope.
- Runtime theme changes must refresh every currently materialized menu level and every generated menu icon without recreating public model objects or factory-created hosted controls.
- Arbitrary hosted controls are not automatically re-themed by mutating their own custom properties. Framework-owned `ToolStrip` surfaces, border, padding, and surrounding chrome update; framework controls hosted by callers continue receiving theme updates through their own existing theme subscriptions.
- `BootstrapSplitButton` is a framework composite control, not a wrapper around native `ToolStripSplitButton`. It reuses `BootstrapButton` presentation and the existing Dropdown popup model/rendering so it looks and behaves consistently with the library.
- The split button owns both internal `BootstrapButton` children and its internal `BootstrapDropdown`; callers do not receive ownership of or public references to those implementation controls.
- The primary split region raises the inherited `Click` event of `BootstrapSplitButton` exactly once. The chevron region never raises the split button's primary `Click`; it only toggles the dropdown.
- `BootstrapSplitButton.Loading = true` shows the existing spinner-backed loading presentation on the primary region and disables dropdown opening from the chevron region until loading ends.
- The split button has two native focusable button regions, matching split-command semantics: Tab/Shift+Tab can focus the primary and chevron regions; Enter/Space on the primary activates the command; Enter/Space on the chevron opens/closes the menu; native menu keys take over once opened.
- The split button popup aligns to the left edge immediately below the complete split-button bounds. It must not align only below the narrow chevron child.
- Split-button `Variant`, `Outline`, `ButtonSize`, `BorderRadius`, enabled state, and icon renderer are applied coherently to both button regions; caller content icon/text/loading text belong to the primary region, while the secondary region always uses framework `ChevronDown` as the structural affordance.
- Connected split regions use the same seam-overlap and outer-corner rules as `BootstrapButtonGroup`. Extract/reuse focused internal layout logic rather than maintaining two subtly different seam/radius algorithms.
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

Useful platform references for implementation review:

- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripdropdownitem.dropdownitems?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripmenuitem?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripcontrolhost?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripcontrolhost.-ctor?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripdropdown.autoclose?view=netframework-4.8.1>

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
| `Kind == Item`, children present | native submenu parent; parent `Click` does not dispatch |
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

### Public surface deliberately not added

- No direct public access to `ToolStripDropDownMenu`, `ToolStripMenuItem`, `ToolStripControlHost`, internal split child buttons, renderer, or native popup handles.
- No `Control` instance property on `BootstrapDropdownItem`; use `HostedControlFactory` so ownership is explicit.
- No live `INotifyPropertyChanged`/`INotifyCollectionChanged` binding while the popup is open.
- No async hosted-control factory or lazy async menu provider.
- No public submenu placement engine; native ToolStrip placement remains authoritative.
- No public submenu depth limit. Tree validation prevents cycles; practical depth remains subject to native UI usability.
- No radio groups, check groups, automatic checked-state mutation, shortcut registration, global hotkeys, or custom menu animation.
- No split-button selection policy; the split control is a command + dropdown affordance, not a toggle/radio control.
- No separate public dropdown alignment property in this scope. The classic `BootstrapDropdown` keeps its current target anchoring, while `BootstrapSplitButton` internally anchors below its full bounds.

---

## File Structure

### Product files

- Modify `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemKind.cs` — add `HostedControl` without renumbering existing enum members.
- Modify `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItem.cs` — add stable child collection and hosted-control factory.
- Modify `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs` — validate/build recursive snapshots, dispatch leaf clicks directly, recursively refresh presentation/resources, and add an internal anchored-show path shared by split button.
- Modify `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs` — render submenu arrows and hosted-control surrounding chrome coherently when native defaults require explicit theme painting; keep existing palette/metrics helpers authoritative.
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapConnectedButtonLayoutLogic.cs` — internal seam/corner calculations reused by ButtonGroup and SplitButton.
- Modify `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapButtonGroup.cs` — delegate seam/corner calculation to the extracted internal helper without changing public behavior.
- Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSplitButton.cs` — composite public control, child-button synchronization, primary/dropdown routing, layout, lifecycle, accessibility, and disposal.

### Tests

- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs` — native characterization, tree validation, recursive snapshots, hosted-control ownership, nested activation, theme/DPI/resource behavior.
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapConnectedButtonLayoutLogicTests.cs` — seam/radius helper coverage.
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSplitButtonTests.cs` — defaults, forwarded properties, layout, focus regions, loading/disabled behavior, primary versus dropdown activation, lifecycle, disposal, theme/DPI.
- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/NavigationDemoFormTests.cs` — assert advanced Dropdown and split-button scenarios are represented.
- Modify `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs` — approve the new exported surface only after API review.

### Demo and docs

- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs` — add nested submenu, hosted control, mixed nested/hosted, split-button, keyboard, and lifetime scenarios.
- Modify `docs/COMPONENTS.md` — extend Dropdown contract and document `BootstrapSplitButton`.
- Modify `docs/TESTING.md` — add advanced Dropdown/split-button manual and automated matrix.
- Modify `docs/PUBLIC_API_BASELINE.md` — record approved public additions and compatibility rationale.
- Modify `README.md` and `docs/PACKAGE_README.md` only if their component inventories/examples currently enumerate Dropdown APIs; keep those inventories synchronized.

---

### Task 1: Characterize native submenu and hosted-control ownership behavior

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: native `ToolStripDropDownMenu`, `ToolStripMenuItem.DropDownItems`, `ToolStripControlHost`.
- Produces: executable evidence that later tasks may safely rely on native submenu hierarchy, leaf activation, host focusability, and hosted-control disposal semantics.

- [ ] **Step 1: Add a native submenu characterization test**

```csharp
[Test]
public void NativeMenuItemCharacterizationSupportsNestedDropDownItems()
{
    using var menu = new ToolStripDropDownMenu();
    var parent = new ToolStripMenuItem("Parent");
    var child = new ToolStripMenuItem("Child");
    parent.DropDownItems.Add(child);
    menu.Items.Add(parent);

    var clicks = 0;
    child.Click += (_, _) => clicks++;
    child.PerformClick();

    Assert.Multiple((Action)(() =>
    {
        Assert.That(parent.HasDropDownItems, Is.True);
        Assert.That(parent.DropDownItems[0], Is.SameAs(child));
        Assert.That(clicks, Is.EqualTo(1));
    }));
}
```

- [ ] **Step 2: Add a hosted-control ownership characterization test**

```csharp
[Test]
public void NativeControlHostCharacterizationOwnsHostedControlOnDispose()
{
    var control = new TextBox();
    var host = new ToolStripControlHost(control);

    host.Dispose();

    Assert.That(control.IsDisposed, Is.True);
}
```

If either characterization differs on a supported runtime, stop and document the target-specific native behavior before designing around it; do not emulate undocumented runtime internals.

- [ ] **Step 3: Run the two characterization tests on both targets**

Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~NativeMenuItemCharacterizationSupportsNestedDropDownItems|FullyQualifiedName~NativeControlHostCharacterizationOwnsHostedControlOnDispose"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~NativeMenuItemCharacterizationSupportsNestedDropDownItems|FullyQualifiedName~NativeControlHostCharacterizationOwnsHostedControlOnDispose"
```

Expected: PASS on both targets with the same ownership conclusions.

- [ ] **Step 4: Commit the characterization evidence**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
git commit -m "test: characterize advanced dropdown primitives"
```

---

### Task 2: Extend the public Dropdown item model for child menus and hosted-control factories

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemKind.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItem.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: existing `BootstrapDropdownItemCollection` null rejection and insertion-order semantics.
- Produces: `BootstrapDropdownItemKind.HostedControl`, stable `BootstrapDropdownItem.DropDownItems`, and nullable `Func<Control> HostedControlFactory` used by the recursive native builder in Task 3.

- [ ] **Step 1: Write failing model-contract tests**

Add tests covering all new defaults and compatibility values:

```csharp
[Test]
public void AdvancedItemModelPreservesExistingEnumValuesAndAddsStableChildCollection()
{
    var item = new BootstrapDropdownItem();
    var hosted = new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl);

    Assert.Multiple((Action)(() =>
    {
        Assert.That((int)BootstrapDropdownItemKind.Item, Is.EqualTo(0));
        Assert.That((int)BootstrapDropdownItemKind.Separator, Is.EqualTo(1));
        Assert.That((int)BootstrapDropdownItemKind.HostedControl, Is.EqualTo(2));
        Assert.That(item.DropDownItems, Is.SameAs(item.DropDownItems));
        Assert.That(item.DropDownItems, Is.Empty);
        Assert.That(item.HostedControlFactory, Is.Null);
        Assert.That(hosted.Kind, Is.EqualTo(BootstrapDropdownItemKind.HostedControl));
    }));
}
```

Also verify nested collections reject `null` exactly like root collections and that existing `Item`/`Separator` constructors still behave unchanged.

- [ ] **Step 2: Run the focused test and verify the new API is missing**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~AdvancedItemModel
```

Expected: build/test FAIL because `HostedControl`, `DropDownItems`, and `HostedControlFactory` do not exist yet.

- [ ] **Step 3: Add the enum value and stable item members**

Implementation shape:

```csharp
public sealed class BootstrapDropdownItem
{
    private readonly BootstrapDropdownItemCollection _dropDownItems;
    private string _text;

    public BootstrapDropdownItem(BootstrapDropdownItemKind kind)
    {
        if (!Enum.IsDefined(typeof(BootstrapDropdownItemKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported dropdown item kind.");
        }

        Kind = kind;
        _text = string.Empty;
        Enabled = true;
        _dropDownItems = new BootstrapDropdownItemCollection();
    }

    [Category("Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapDropdownItemCollection DropDownItems => _dropDownItems;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<Control>? HostedControlFactory { get; set; }
}
```

Add XML documentation that states the factory returns a framework-owned control instance for the next native snapshot.

- [ ] **Step 4: Run all existing and new Dropdown model tests on both targets**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapDropdownTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapDropdownTests
```

Expected: PASS; existing enum numeric values and existing item defaults are unchanged.

- [ ] **Step 5: Commit the model extension**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemKind.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItem.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
git commit -m "feat: model nested and hosted dropdown items"
```

---

### Task 3: Validate Dropdown trees before native snapshot mutation

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: `Kind`, `DropDownItems`, `HostedControlFactory` from Task 2.
- Produces: one internal tree-validation path invoked before `ClearNativeItems()`/native rebuild; guarantees later recursive construction never sees malformed model state.

- [ ] **Step 1: Write failing validation tests**

Cover these cases separately:

```csharp
[Test]
public void DropdownTreeValidationRejectsSeparatorChildrenBeforeOpening()
{
    using var dropdown = new BootstrapDropdown();
    var separator = new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator);
    separator.DropDownItems.Add(new BootstrapDropdownItem { Text = "Illegal" });
    dropdown.Items.Add(separator);

    Assert.Throws<InvalidOperationException>((Action)(() =>
        BootstrapDropdown.ValidateItemTree(dropdown.Items)));
}

[Test]
public void DropdownTreeValidationRejectsHostedControlWithoutFactory()
{
    var items = new BootstrapDropdownItemCollection
    {
        new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
    };

    Assert.Throws<InvalidOperationException>((Action)(() =>
        BootstrapDropdown.ValidateItemTree(items)));
}

[Test]
public void DropdownTreeValidationRejectsDuplicateInstanceReuse()
{
    var shared = new BootstrapDropdownItem { Text = "Shared" };
    var parent = new BootstrapDropdownItem { Text = "Parent" };
    parent.DropDownItems.Add(shared);
    var items = new BootstrapDropdownItemCollection { shared, parent };

    Assert.Throws<InvalidOperationException>((Action)(() =>
        BootstrapDropdown.ValidateItemTree(items)));
}
```

Add a cycle case (`parent.DropDownItems.Add(parent)`) and valid mixed-depth case.

- [ ] **Step 2: Verify tests fail because validation does not exist**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~DropdownTreeValidation
```

Expected: FAIL at compile time for missing internal validation helper.

- [ ] **Step 3: Implement reference-identity tree validation**

Use one `HashSet<BootstrapDropdownItem>` for the whole traversal. Because the item type is sealed and does not override equality, ordinary reference semantics are sufficient; do not add public parent pointers solely for validation.

Validation rules:

```csharp
private static void ValidateItem(
    BootstrapDropdownItem item,
    HashSet<BootstrapDropdownItem> visited)
{
    if (!visited.Add(item))
    {
        throw new InvalidOperationException("A BootstrapDropdownItem instance can appear only once in one dropdown tree.");
    }

    if (item.Kind == BootstrapDropdownItemKind.Separator)
    {
        if (item.DropDownItems.Count != 0 || item.HostedControlFactory is not null)
        {
            throw new InvalidOperationException("Separator items cannot contain submenu items or hosted controls.");
        }
        return;
    }

    if (item.Kind == BootstrapDropdownItemKind.HostedControl)
    {
        if (item.DropDownItems.Count != 0 || item.HostedControlFactory is null)
        {
            throw new InvalidOperationException("Hosted-control items require a factory and cannot contain submenu items.");
        }
        return;
    }

    if (item.HostedControlFactory is not null)
    {
        throw new InvalidOperationException("Normal command items cannot define HostedControlFactory.");
    }

    foreach (var child in item.DropDownItems)
    {
        ValidateItem(child, visited);
    }
}
```

Keep the callable test seam `internal`, not public API.

- [ ] **Step 4: Run focused validation tests on both targets**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~DropdownTreeValidation
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~DropdownTreeValidation
```

Expected: PASS.

- [ ] **Step 5: Commit tree validation**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
git commit -m "feat: validate dropdown item trees"
```

---

### Task 4: Build recursive native menu snapshots and own hosted controls safely

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: validated public tree from Task 3, existing icon/theme snapshot infrastructure.
- Produces: recursive native `ToolStripMenuItem.DropDownItems`, factory-created `ToolStripControlHost`, direct leaf click dispatch, deterministic recursive disposal.

- [ ] **Step 1: Write failing recursive-activation tests**

Create a two- and three-level item tree and expose an internal snapshot-building seam only if necessary for deterministic tests. Verify:

- parent submenu items do not dispatch their model `Click`;
- nested leaf commands dispatch exactly once;
- disabled nested leaves do not dispatch;
- checked state is preserved and never auto-toggled;
- separators at nested levels become `ToolStripSeparator`;
- each native leaf's `Tag` maps back to the correct model.

Representative assertion:

```csharp
var parent = new BootstrapDropdownItem { Text = "Export" };
var pdf = new BootstrapDropdownItem { Text = "PDF", Checked = true };
parent.DropDownItems.Add(pdf);
var parentClicks = 0;
var pdfClicks = 0;
parent.Click += (_, _) => parentClicks++;
pdf.Click += (_, _) => pdfClicks++;

// Build the native snapshot through the internal test seam, then PerformClick on the child.
childNative.PerformClick();

Assert.Multiple((Action)(() =>
{
    Assert.That(parentClicks, Is.Zero);
    Assert.That(pdfClicks, Is.EqualTo(1));
    Assert.That(pdf.Checked, Is.True);
}));
```

- [ ] **Step 2: Write failing hosted-control factory/lifetime tests**

Use a small `TrackingControl : Control` test double with `Disposed` counter. Verify:

- factory is called once per effective snapshot rebuild, not while merely closing/reopening a still-unrebuilt test seam;
- returned control is inside a `ToolStripControlHost`;
- hosted item `Enabled = false` disables both host and control;
- a null-returning factory throws before popup opening;
- an already-disposed returned control throws before opening;
- rebuild disposes the previous factory-created control exactly once;
- Dropdown disposal disposes the current factory-created control exactly once.

- [ ] **Step 3: Run the new tests and verify current flat builder fails them**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Nested|FullyQualifiedName~HostedControl"
```

Expected: FAIL because the current builder only creates flat `ToolStripMenuItem`/`ToolStripSeparator` rows.

- [ ] **Step 4: Replace root-only `ItemClicked` dispatch with leaf-item click dispatch**

Do not rely on the root `_dropDown.ItemClicked` event to discover child activation. Attach the one framework handler directly to every native leaf command created by the recursive builder:

```csharp
private ToolStripMenuItem CreateCommandItem(BootstrapDropdownItem model)
{
    var item = new ToolStripMenuItem(model.Text)
    {
        Enabled = model.Enabled,
        Checked = model.Checked,
        CheckOnClick = false,
        Tag = model,
        AutoSize = true
    };

    if (model.DropDownItems.Count == 0)
    {
        item.Click += OnNativeLeafItemClick;
    }

    return item;
}

private void OnNativeLeafItemClick(object? sender, EventArgs e)
{
    if (sender is ToolStripMenuItem { Tag: BootstrapDropdownItem model })
    {
        ActivateItem(model);
    }
}
```

Remove the root `_dropDown.ItemClicked` subscription/handler after the new path is green so one activation has one dispatcher.

- [ ] **Step 5: Implement recursive child construction**

Create one recursive method that targets any `ToolStripItemCollection`:

```csharp
private void PopulateNativeItems(
    ToolStripItemCollection destination,
    BootstrapDropdownItemCollection models,
    BootstrapButton presentationSource)
{
    foreach (var model in models)
    {
        switch (model.Kind)
        {
            case BootstrapDropdownItemKind.Separator:
                destination.Add(new ToolStripSeparator());
                break;

            case BootstrapDropdownItemKind.HostedControl:
                destination.Add(CreateHostedControlItem(model));
                break;

            case BootstrapDropdownItemKind.Item:
                var nativeItem = CreateCommandItem(model);
                destination.Add(nativeItem);
                if (model.DropDownItems.Count > 0)
                {
                    PopulateNativeItems(nativeItem.DropDownItems, model.DropDownItems, presentationSource);
                }
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
```

The public item tree is validated before this method runs, so this method never silently repairs malformed nodes.

- [ ] **Step 6: Implement factory-created hosted-control rows**

```csharp
private ToolStripControlHost CreateHostedControlItem(BootstrapDropdownItem model)
{
    var control = model.HostedControlFactory!();
    if (control is null)
    {
        throw new InvalidOperationException("HostedControlFactory returned null.");
    }
    if (control.IsDisposed)
    {
        throw new InvalidOperationException("HostedControlFactory returned a disposed control.");
    }

    control.Enabled = model.Enabled;
    return new ToolStripControlHost(control)
    {
        Enabled = model.Enabled,
        AutoSize = true,
        Tag = model
    };
}
```

If native preferred-size behavior requires the host to copy the returned control's explicit `Size`, keep that adjustment internal and cover it with a deterministic preferred-size test; do not add a second public sizing API without evidence.

- [ ] **Step 7: Make exception paths leak-free**

If any later factory throws after earlier native items/controls were created, dispose the partially built native snapshot before rethrowing. Do not leave generated controls or images attached to `_dropDown` after a failed opening.

Add a test where the second hosted factory throws after the first created a tracking control; assert the first control is disposed and `Opened` remains zero.

- [ ] **Step 8: Run all Dropdown tests on both targets**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapDropdownTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapDropdownTests
```

Expected: PASS, including all pre-extension Stage 7 tests.

- [ ] **Step 9: Commit recursive snapshot construction**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
git commit -m "feat: build nested dropdown snapshots"
```

---

### Task 5: Apply renderer, image, theme, and DPI presentation recursively

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: native tree from Task 4, existing `BootstrapDropdownRenderer.ResolvePalette/ResolveMetrics`, target `IconRenderer` and target DPI.
- Produces: level-aware image/check margins, recursive icon generation, theme refresh, themed submenu arrows/borders, and hosted-control surrounding chrome.

- [ ] **Step 1: Write failing per-level margin tests**

Build root/child levels where only the child contains an icon and only the root contains a checked command. Verify each `ToolStripDropDownMenu` level independently resolves `ShowImageMargin` and `ShowCheckMargin` from only that level's model children.

Also test the inverse arrangement.

- [ ] **Step 2: Write failing recursive icon/theme tests**

Use the existing recording icon renderer. Put icons at root, child, and grandchild levels, open/build once, change Light → Dark, and assert every generated icon is rendered again using the same target renderer while public item instances and hosted control instances remain unchanged.

- [ ] **Step 3: Run the focused presentation tests and verify failure**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~DropdownRecursivePresentation|FullyQualifiedName~DropdownPerLevelMargins"
```

Expected: FAIL until presentation traversal is recursive.

- [ ] **Step 4: Add recursive ToolStrip traversal helpers**

One traversal must visit:

- root `_dropDown`;
- each submenu `ToolStripDropDown` owned by a `ToolStripMenuItem` with children;
- every leaf/menu item for padding and generated images;
- every separator for inset margins;
- every `ToolStripControlHost` for host-level padding/background without overwriting caller-owned interior state.

Set `_renderer` on each materialized submenu DropDown explicitly rather than depending on implicit renderer inheritance.

- [ ] **Step 5: Compute margins from sibling models at each level**

Create an internal helper such as:

```csharp
internal static (bool ShowImageMargin, bool ShowCheckMargin) ResolveLevelMargins(
    BootstrapDropdownItemCollection items)
```

Only normal command rows contribute icon/check margins. Hosted controls and separators do not create empty image/check columns.

- [ ] **Step 6: Refresh owned images recursively**

Before creating new images, clear every native item's image reference, dispose every image in `_ownedImages`, then walk all menu levels. Continue rendering with the current presentation source's `IconRenderer`, current theme text/muted color, current DPI, and existing renderer metrics.

A submenu parent may have its own `Icon` as well as children; render that icon like any other normal menu item.

- [ ] **Step 7: Preserve native submenu arrow rendering**

If the custom `ToolStripRenderer` suppresses or mismatches native submenu arrows on either target, override `OnRenderArrow` only to supply theme-appropriate foreground color and call the supported base/native arrow rendering path. Do not paint a second arrow in the text/icon layout.

Add a renderer test for enabled/disabled arrow foreground decision if project code introduces explicit arrow palette logic.

- [ ] **Step 8: Run renderer and Dropdown tests on both targets**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapDropdownTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapDropdownTests"
```

Expected: PASS at 96-DPI logic and existing tested DPI matrix values 120/144/168/192.

- [ ] **Step 9: Commit recursive presentation**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
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

- [ ] **Step 1: Write failing pure layout-logic tests**

Test seam overlap for 96/120/144/168/192 DPI and left/right horizontal corner shapes:

```csharp
[TestCase(96)]
[TestCase(120)]
[TestCase(144)]
[TestCase(168)]
[TestCase(192)]
public void SeamOverlapUsesScaledThemeBorderWidth(int dpi)
{
    var actual = BootstrapConnectedButtonLayoutLogic.ResolveSeamOverlap(
        BootstrapThemeMetrics.Default,
        dpi);

    Assert.That(actual, Is.EqualTo(Math.Max(
        1,
        DpiScaler.Scale(BootstrapThemeMetrics.Default.BorderWidth, dpi))));
}
```

Verify left segment corners are `(radius, 0, 0, radius)` and right segment corners are `(0, radius, radius, 0)`.

- [ ] **Step 2: Run focused tests and verify missing helper failure**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapConnectedButtonLayoutLogicTests
```

Expected: FAIL because the helper does not exist.

- [ ] **Step 3: Implement the internal helper without public API**

Keep responsibilities narrow:

```csharp
internal static class BootstrapConnectedButtonLayoutLogic
{
    internal static int ResolveSeamOverlap(BootstrapThemeMetrics metrics, int dpi) { ... }
    internal static CornerRadius ResolveHorizontalCorners(float radius, bool first, bool last) { ... }
    internal static CornerRadius ResolveVerticalCorners(float radius, bool first, bool last) { ... }
}
```

Validate null metrics, invalid DPI, and negative radius consistently with existing rendering helpers.

- [ ] **Step 4: Refactor `BootstrapButtonGroup` to use the helper**

Replace only its private seam/corner math. Do not change ordering, selection policy, equal-width behavior, public members, or layout measurements.

- [ ] **Step 5: Run ButtonGroup + helper tests on both targets**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapButtonGroupTests|FullyQualifiedName~BootstrapConnectedButtonLayoutLogicTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapButtonGroupTests|FullyQualifiedName~BootstrapConnectedButtonLayoutLogicTests"
```

Expected: PASS with no ButtonGroup behavioral change.

- [ ] **Step 6: Commit shared connected-button layout logic**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapConnectedButtonLayoutLogic.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapButtonGroup.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapConnectedButtonLayoutLogicTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapButtonGroupTests.cs
git commit -m "refactor: share connected button layout logic"
```

---

### Task 7: Add an internal anchored-show path without changing classic Dropdown API

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Interfaces:**
- Consumes: existing public `Show()` target path and recursive snapshot/presentation source from Tasks 3–5.
- Produces: internal `ShowFrom(BootstrapButton presentationSource, Control anchor, Point location)` used by `BootstrapSplitButton`; public `Show()` remains source-compatible and behavior-compatible.

- [ ] **Step 1: Write a failing internal-anchor lifecycle test**

Create a host form containing a presentation `BootstrapButton` and a wider plain anchor control. Use the internal show path and verify `Opened`/`Closed` still forward exactly once and that public `Target` remains unchanged.

Also verify null/disposed source or anchor arguments are rejected internally rather than producing native exceptions.

- [ ] **Step 2: Run the focused test and verify the internal path is missing**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~DropdownInternalAnchor
```

Expected: FAIL because `ShowFrom` does not exist.

- [ ] **Step 3: Funnel public `Show()` through one internal opening path**

Implementation shape:

```csharp
public void Show()
{
    ThrowIfDisposed();
    var target = _target ?? throw new InvalidOperationException(
        "A BootstrapDropdown Target must be assigned before Show is called.");

    ShowFrom(target, target, new Point(0, target.Height));
}

internal void ShowFrom(
    BootstrapButton presentationSource,
    Control anchor,
    Point location)
{
    // Validate source/anchor and no-op states.
    // Validate item tree before mutating the previous native snapshot.
    // Build/apply presentation.
    // Record the active presentation source only while popup is visible.
    // _dropDown.Show(anchor, location).
}
```

- [ ] **Step 4: Track the active presentation source for theme refresh**

The current theme-change path reads `_target`. That is insufficient for a split button whose internal Dropdown intentionally has no public `Target`. Add a private non-owning `_activePresentationSource` reference while the popup is visible; set it immediately before show and clear it in native `Closed` and failed-open cleanup.

Theme refresh uses `_activePresentationSource`, while public target event wiring still uses `_target` exactly as before.

- [ ] **Step 5: Run all Dropdown tests on both targets**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapDropdownTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapDropdownTests
```

Expected: PASS; classic `Target` click toggling and public `Show()` still behave exactly as Stage 7 specified.

- [ ] **Step 6: Commit the shared internal opening primitive**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
git commit -m "refactor: share dropdown anchored show path"
```

---

### Task 8: Implement `BootstrapSplitButton` visual composition and property synchronization

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSplitButton.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSplitButtonTests.cs`

**Interfaces:**
- Consumes: `BootstrapButton`, `BootstrapConnectedButtonLayoutLogic`, framework `ChevronDown`, existing theme/DPI metrics.
- Produces: two-region composite control with coherent BootstrapButton appearance, correct preferred/custom-width layout, and primary `Click` forwarding. Dropdown wiring is added in Task 9.

- [ ] **Step 1: Write failing default/property-contract tests**

Verify the public contract and defaults listed above. Also verify `Items` is one stable collection instance and `IconRenderer = null` throws `ArgumentNullException`.

Use reflection only for approved test seams if necessary; do not make child buttons public to simplify tests.

- [ ] **Step 2: Write failing layout tests**

Cover:

- preferred size = primary preferred width + chevron preferred width − scaled seam overlap;
- both regions have the same height;
- custom wider bounds grow the primary region while keeping the chevron region at its preferred width;
- custom width never makes either child negative; minimum width clamps to the two child minimum/preferred requirements;
- 96/120/144/168/192 DPI seam math uses the shared helper;
- left/right group corner radii use the same outer-corner rules as ButtonGroup.

- [ ] **Step 3: Write failing primary-action tests**

Verify one primary child activation calls the outer inherited `Click` exactly once with `BootstrapSplitButton` as sender, and loading/disabled state suppresses activation.

- [ ] **Step 4: Run the new test class and verify missing-control failure**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapSplitButtonTests
```

Expected: FAIL because `BootstrapSplitButton` does not exist.

- [ ] **Step 5: Create the composite and child buttons**

Constructor shape:

```csharp
public BootstrapSplitButton()
{
    SetStyle(
        ControlStyles.AllPaintingInWmPaint |
        ControlStyles.OptimizedDoubleBuffer |
        ControlStyles.ResizeRedraw |
        ControlStyles.SupportsTransparentBackColor,
        true);

    TabStop = false;
    BackColor = Color.Transparent;
    AccessibleRole = AccessibleRole.Grouping;

    _primaryButton = new BootstrapButton();
    _dropDownButton = new BootstrapButton
    {
        Text = string.Empty,
        Icon = IconDescriptor.Framework(FrameworkIconGlyph.ChevronDown),
        IconPosition = BootstrapIconPosition.Left
    };

    _primaryButton.Click += OnPrimaryButtonClick;
    Controls.Add(_primaryButton);
    Controls.Add(_dropDownButton);

    AutoSize = true;
    ApplyChildAppearance();
}
```

Do not let caller `Text`/`Icon` assignments replace the chevron icon of the secondary region.

- [ ] **Step 6: Forward public appearance/behavior properties**

For every forwarded property, validate using the same existing public setter by assigning to the child controls; do not duplicate enum validation tables.

Synchronization rules:

- `Text`, `Icon`, `IconPosition`, `Loading`, `LoadingText` → primary only.
- `Variant`, `Outline`, `ButtonSize`, `IconRenderer`, enabled state → both children.
- `BorderRadius` → logical outer radius source for both children; apply connected `GroupCornerRadius` values after resolution.
- `Loading = true` → primary gets loading presentation; secondary is disabled for interaction while loading.

- [ ] **Step 7: Implement preferred-size and custom-width layout**

Use `BootstrapConnectedButtonLayoutLogic.ResolveSeamOverlap(...)`. The chevron uses its preferred width; the primary gets remaining width when the outer control is wider than preferred.

Call `PerformLayout()`/`Invalidate()` when forwarded properties can affect preferred size. Handle `OnDpiChangedAfterParent`, `OnFontChanged`, `OnEnabledChanged`, and `OnLayout` without creating timers or GDI caches.

- [ ] **Step 8: Implement accessibility and focus semantics**

- outer control: grouping role, `TabStop = false`;
- primary child: focusable push button, accessible name derived from outer `AccessibleName` when set, otherwise `Text`;
- chevron child: focusable push button with accessible name `<primary name> menu` and description explaining that it opens additional commands;
- changes to outer `AccessibleName`/`Text` update derived child names without replacing an explicitly maintained caller-facing outer property.

Do not intercept Tab/Shift+Tab; native child focus traversal remains authoritative.

- [ ] **Step 9: Run split visual/property tests on both targets**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapSplitButtonTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapSplitButtonTests
```

Expected: PASS for composition/property/layout tests; dropdown-specific tests are added next.

- [ ] **Step 10: Commit the split-button shell**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSplitButton.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSplitButtonTests.cs
git commit -m "feat: add bootstrap split button shell"
```

---

### Task 9: Wire split-button Dropdown lifecycle, anchoring, keyboard, and ownership

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSplitButton.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSplitButtonTests.cs`

**Interfaces:**
- Consumes: internal `BootstrapDropdown.ShowFrom(...)` from Task 7 and recursive item model from Tasks 2–5.
- Produces: stable split `Items`, separate chevron toggle behavior, popup aligned below the full split bounds, lifecycle forwarding, loading/disabled no-op behavior, and deterministic disposal.

- [ ] **Step 1: Write failing split dropdown routing tests**

Verify:

- `Items` is the internal Dropdown's one stable collection;
- primary activation raises outer `Click` but does not open the popup;
- chevron activation opens popup but does not raise outer `Click`;
- activating chevron again closes popup;
- `ShowDropDown()` and `CloseDropDown()` use the same lifecycle;
- `Opened`/`Closed` sender is the `BootstrapSplitButton`, not internal Dropdown;
- empty/disabled/loading states do not raise `Opened`;
- a nested leaf command works through split-button `Items` exactly like classic Dropdown.

- [ ] **Step 2: Write a full-width anchor regression test**

Use a visible STA form and an internal observation seam for the requested anchor/location if direct native screen coordinates are unreliable. Assert the split path asks Dropdown to show using:

```text
anchor     = BootstrapSplitButton
location.X = 0
location.Y = BootstrapSplitButton.Height
```

Do not assert undocumented final screen clamping pixels.

- [ ] **Step 3: Write loading and selected-chevron state tests**

While open, the chevron child uses `Selected = true` as the active affordance. Native `Closed` resets it to false. Setting `Loading = true` while open closes the popup first, then disables further dropdown opening until loading returns false.

- [ ] **Step 4: Run the focused tests and verify routing is incomplete**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSplitButtonTests"
```

Expected: FAIL for missing internal Dropdown wiring.

- [ ] **Step 5: Create and own one internal Dropdown**

```csharp
_dropdown = new BootstrapDropdown
{
    Variant = _variant,
    MinimumWidth = _minimumWidth
};
_dropdown.Opened += OnDropDownOpened;
_dropdown.Closed += OnDropDownClosed;
_dropDownButton.Click += OnDropDownButtonClick;
```

Do not assign `_dropdown.Target`; split button intentionally controls the internal anchored-show path so classic `Target.Click` wiring cannot open at the chevron child's left edge.

- [ ] **Step 6: Forward model and lifecycle members**

`Items` directly returns `_dropdown.Items`. `Variant` and `MinimumWidth` synchronize to `_dropdown`. `Opened`/`Closed` are re-raised with `this` as sender only from actual native lifecycle callbacks.

- [ ] **Step 7: Implement one dropdown toggle path**

Both chevron activation and public `ShowDropDown()`/`CloseDropDown()` delegate to the same private methods. Open via:

```csharp
_dropdown.ShowFrom(
    _primaryButton,
    this,
    new Point(0, Height));
```

The primary button is the presentation source because it owns caller text/icon renderer/font/theme-compatible button settings; the complete split control is the anchor.

- [ ] **Step 8: Handle runtime state changes**

- outer disable while open → close Dropdown;
- `Loading` changing to true while open → close Dropdown before secondary disable;
- split disposal while open → close, detach internal handlers, dispose internal Dropdown and owned child buttons exactly once;
- internal Dropdown never disposes public item models;
- theme changes flow through child BootstrapButtons and the active Dropdown presentation-source mechanism without a split-specific theme subscription unless layout itself demonstrably needs one.

- [ ] **Step 9: Exercise two focus regions on an STA host**

Avoid brittle `SendKeys` assertions in CI. Automated tests should verify both internal buttons are `TabStop = true`, enabled-state transitions, and chevron `PerformClick()` routing. Real Left/Right/Tab/Enter/Escape behavior belongs in Task 10's manual matrix.

- [ ] **Step 10: Run split + Dropdown tests on both targets**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapSplitButtonTests|FullyQualifiedName~BootstrapDropdownTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapSplitButtonTests|FullyQualifiedName~BootstrapDropdownTests"
```

Expected: PASS.

- [ ] **Step 11: Commit complete split-button behavior**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapSplitButton.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapSplitButtonTests.cs
git commit -m "feat: connect split button dropdown behavior"
```

---

### Task 10: Expand Navigation demo and real-desktop verification matrix

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/NavigationDemoFormTests.cs`

**Interfaces:**
- Consumes: complete advanced Dropdown and `BootstrapSplitButton` public APIs.
- Produces: visible integration examples and manual verification coverage without exposing internals.

- [ ] **Step 1: Write failing demo-coverage assertions**

Extend `NavigationDemoFormTests` to verify the demo includes discoverable controls/scenarios for:

- nested submenu;
- hosted textbox/check/list-style custom control;
- mixed nested + hosted submenu content;
- split-button primary action;
- split-button nested Dropdown;
- runtime Light/Dark scenario.

Use `AccessibleName` or stable control type discovery rather than display-pixel assertions.

- [ ] **Step 2: Run the demo test and verify scenarios are absent**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~NavigationDemoFormTests
```

Expected: FAIL for missing advanced scenarios.

- [ ] **Step 3: Add a nested command scenario**

Example model shape:

```csharp
var export = new BootstrapDropdownItem { Text = "Export" };
export.DropDownItems.Add(new BootstrapDropdownItem { Text = "PDF" });
var spreadsheet = new BootstrapDropdownItem { Text = "Spreadsheet" };
spreadsheet.DropDownItems.Add(new BootstrapDropdownItem { Text = "XLSX" });
spreadsheet.DropDownItems.Add(new BootstrapDropdownItem { Text = "CSV" });
export.DropDownItems.Add(spreadsheet);
dropdown.Items.Add(export);
```

Wire leaf clicks to `_selectionStatus` so activation depth is visible.

- [ ] **Step 4: Add hosted-control scenarios using factories**

At least one factory should return an interactive native `TextBox`/`CheckBox` container and one should return an existing framework control such as `BootstrapTextBox` if its semantics fit. Configure each fresh instance entirely inside its factory and use application handlers to update `_selectionStatus`.

The demo text must explicitly state that the factory-created control is framework-owned after return and a fresh instance is created for a later snapshot rebuild.

- [ ] **Step 5: Add a split-button scenario**

Use one `BootstrapSplitButton` with:

- primary text/icon action updating status;
- root leaf command;
- nested submenu;
- one hosted control;
- `AccessibleName` identifying the split control;
- a loading-toggle companion control or menu action so users can observe that loading suppresses both primary and dropdown activation as specified.

- [ ] **Step 6: Expand manual-verification instructions**

The demo matrix must include:

1. Mouse: primary click versus chevron click; repeated open/close; outside click.
2. Keyboard: Tab/Shift+Tab between split regions; Enter/Space on each region; Up/Down/Home/End in a menu; Right to enter submenu; Left to return; Enter on leaf; Escape at nested and root levels.
3. Hosted controls: focus textbox/check/custom control, edit/toggle values, move back to menu items, outside-click dismissal, reopen and confirm factory-created state policy.
4. States: enabled/disabled leaf, disabled submenu parent, disabled hosted control, checked leaf, split loading state.
5. Theme: switch Light/Dark while root/submenu is visible; verify all menu levels repaint and hosted framework controls update themselves.
6. DPI: 100/125/150/175/200%, including nested menu arrows, icon/check margins, split seam, and caret sizing.
7. Screens: near bottom/right edges and secondary monitor; confirm native popup/submenu placement stays on usable working area.
8. Lifetime: repeatedly open/close/rebuild hosted controls, close the demo form while a nested menu is open, and confirm no stale windows, disposed-control exceptions, or increasing GDI artifacts.

- [ ] **Step 7: Run demo tests on both targets**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~NavigationDemoFormTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~NavigationDemoFormTests
```

Expected: PASS.

- [ ] **Step 8: Run the demo manually on a Windows desktop**

```powershell
dotnet run --project demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release
```

Expected: every manual matrix item above behaves without application exceptions or stale popup artifacts.

- [ ] **Step 9: Commit demo integration**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/NavigationDemoFormTests.cs
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

- [ ] **Step 1: Run the public-API baseline before approving changes**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~Phase16PublicApiBaselineTests
```

Expected: FAIL because the enum member, item members, and `BootstrapSplitButton` public surface are not in the approved baseline yet. Treat this as the required API review gate, not as a fingerprint to update mechanically.

- [ ] **Step 2: Review the exported surface against this plan**

Confirm the exported diff contains only the intended additions:

```text
BootstrapDropdownItemKind.HostedControl
BootstrapDropdownItem.DropDownItems
BootstrapDropdownItem.HostedControlFactory
BootstrapSplitButton and the exact public members listed in this plan
```

Reject accidental exports such as child-button accessors, native ToolStrip properties, internal layout helpers, internal `ShowFrom`, native snapshot builders, or test seams.

- [ ] **Step 3: Update `docs/COMPONENTS.md`**

Document:

- recursive item-tree semantics;
- submenu parent versus leaf command behavior;
- hosted-control factory ownership/lifetime;
- invalid-tree rules;
- per-level image/check margins;
- snapshot/no-live-binding behavior;
- split-button primary and chevron semantics;
- split loading behavior;
- accessibility/focus regions;
- full-width split popup anchoring;
- examples showing nested commands and hosted-control factory use.

- [ ] **Step 4: Update `docs/TESTING.md`**

Record automated coverage boundaries and the Task 10 real-desktop matrix, especially Right/Left submenu keyboard navigation, hosted-control focus, monitor-edge placement, multi-DPI split seam, and repeated factory-created control disposal.

- [ ] **Step 5: Synchronize public component inventories**

If `README.md` or `docs/PACKAGE_README.md` lists supported controls/APIs, add `BootstrapSplitButton` and advanced Dropdown capabilities there. Do not add unrelated marketing copy.

- [ ] **Step 6: Update the approved API fingerprint and baseline documentation**

After the surface review passes, update `Phase16PublicApiBaselineTests.cs` and `docs/PUBLIC_API_BASELINE.md` together. Record that existing Dropdown members were retained and the new APIs are additive except for adding an enum member, which must be called out for switch-expression consumers.

- [ ] **Step 7: Re-run public API tests on both targets**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~Phase16PublicApiBaselineTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~Phase16PublicApiBaselineTests
```

Expected: PASS with identical intended public surface across both target frameworks.

- [ ] **Step 8: Commit docs and API approval**

```bash
git add docs/COMPONENTS.md docs/TESTING.md docs/PUBLIC_API_BASELINE.md README.md docs/PACKAGE_README.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs
git commit -m "docs: approve advanced dropdown api"
```

If README/package README did not require changes, omit them from `git add` rather than making cosmetic edits.

---

### Task 12: Full regression, resource-lifetime review, and completion gate

**Files:**
- Review all files modified by Tasks 1–11.
- Modify only the specific implementation/test/doc file responsible for any discovered regression.

**Interfaces:**
- Consumes: complete feature implementation.
- Produces: release-quality evidence that both frameworks remain green and no duplicate popup/layout/resource infrastructure was introduced.

- [ ] **Step 1: Build product for .NET Framework 4.8**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
```

Expected: PASS with zero warnings promoted to errors.

- [ ] **Step 2: Build product for .NET 8 Windows**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
```

Expected: PASS with zero warnings promoted to errors.

- [ ] **Step 3: Run the full .NET Framework 4.8 test suite**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
```

Expected: PASS.

- [ ] **Step 4: Run the full .NET 8 Windows test suite**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: PASS.

- [ ] **Step 5: Run repository build/test scripts**

```powershell
./build.ps1
./test.ps1
```

Expected: PASS with the same supported environment assumptions already documented by the repository.

- [ ] **Step 6: Perform an explicit ownership/lifecycle code review**

Trace and verify all of the following paths from allocation to release:

- root and nested native `ToolStripItem` objects;
- leaf `Click` event subscriptions;
- generated icon `Bitmap` objects at all menu depths;
- factory-created hosted controls;
- `ToolStripControlHost` objects;
- `_activePresentationSource` lifetime;
- classic caller-owned `Target` wiring;
- split-owned child buttons and internal Dropdown;
- theme subscriptions owned by existing framework controls;
- open popup during target/split/form disposal;
- failed factory after earlier controls were created.

Expected: every framework-owned resource has one deterministic release path; no caller-owned model or classic Dropdown target is disposed by the framework.

- [ ] **Step 7: Perform a compatibility code review**

Search changed product files for APIs unavailable on `net48`, unintended `#if`, accidental new package references, direct runtime-specific ToolStrip internals, or duplicate custom placement/focus code.

Expected: one shared source path remains practical for both targets.

- [ ] **Step 8: Perform the Task 10 real-desktop matrix one final time**

Expected: submenu keyboard/focus, hosted controls, split primary/chevron routing, theme, DPI, screen-edge placement, repeated rebuild, and disposal all remain correct after final regression fixes.

- [ ] **Step 9: Inspect final diff and public API diff**

```bash
git diff --check
git status --short
```

Expected: no whitespace errors, no generated binaries, no unrelated files, no undocumented public member.

- [ ] **Step 10: Commit any final regression-only corrections**

If verification required corrections, commit them by responsibility, for example:

```bash
git add <specific-files-that-fixed-the-regression>
git commit -m "fix: harden advanced dropdown lifecycle"
```

If no corrections were needed, do not create an empty commit.

---

## Acceptance Criteria

The plan is complete only when all of the following are true:

1. Existing flat `BootstrapDropdown` behavior and public API remain compatible.
2. Normal Dropdown items can contain arbitrary-depth `DropDownItems` and native keyboard submenu navigation works without custom key routing.
3. Submenu parents are navigation-only and enabled leaf commands raise model `Click` exactly once.
4. `HostedControl` items create fresh controls through `HostedControlFactory`; malformed factories fail before opening; framework-owned hosted controls are disposed deterministically without disposing caller-owned models.
5. Duplicate/cyclic item graphs and illegal separator/host child combinations fail validation before native snapshot mutation.
6. Icons, checks, disabled presentation, margins, theme updates, DPI scaling, renderer, and disposal work at every menu level.
7. `BootstrapSplitButton` provides distinct primary and chevron focus/action regions with connected Bootstrap styling and no public exposure of internal children.
8. Split primary activation raises inherited `Click`; chevron activation toggles Dropdown only; loading suppresses both as specified.
9. Split popup anchors immediately below the complete split-button bounds while native WinForms retains screen collision/working-area behavior.
10. `BootstrapButtonGroup` behavior is unchanged after shared seam/corner logic extraction.
11. Light/Dark, 100–200% DPI, disabled/loading, nested keyboard navigation, hosted-control focus, outside-click/Escape, monitor-edge placement, repeated rebuild, and disposal are verified.
12. `net48` and `net8.0-windows` builds and complete test suites pass.
13. Demo/documentation/public-API baseline describe the final behavior and ownership contract accurately.
14. No new external dependency, custom popup Form, global hook, duplicate placement engine, per-control timer, leaked GDI object, stale event subscription, or hidden ownership transfer is introduced.

## Self-Review Checklist Applied to This Plan

- **Scope coverage:** nested submenus, arbitrary hosted controls, and split-button behavior each have dedicated model, implementation, tests, demo, docs, lifecycle, and API-baseline tasks.
- **Compatibility coverage:** the plan preserves existing Dropdown public members and enum numeric values, keeps both target frameworks, and characterizes native primitives before depending on them.
- **Ownership coverage:** caller-owned models/targets and framework-owned native snapshots/host controls are separated explicitly; factory-created hosted controls remove ambiguous disposal semantics.
- **Interaction coverage:** mouse, keyboard, focus, submenu direction, Escape/outside click, split regions, and hosted control interaction all have automated or real-desktop verification paths.
- **Rendering coverage:** per-level margins, icons, Light/Dark, DPI, native submenu arrow, connected button seams, and runtime theme refresh are addressed.
- **Type consistency:** `DropDownItems`, `HostedControlFactory`, `HostedControl`, `BootstrapSplitButton`, `ShowDropDown()`, `CloseDropDown()`, and internal `ShowFrom(...)` use the same names and responsibilities throughout all tasks.
- **API discipline:** no native ToolStrip implementation detail or internal child button is exposed merely for testing; only the additive public surface listed in the Public Contract section is approved.
- **Placeholder scan:** every implementation checkpoint identifies concrete files, interfaces, tests, commands, expected results, and commit boundaries; no unspecified future implementation step remains.
