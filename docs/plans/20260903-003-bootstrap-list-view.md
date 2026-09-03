# BootstrapListView Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a production-ready `BootstrapListView` that applies Bootstrap-inspired, theme-aware, DPI-aware presentation to the native WinForms `ListView` while preserving native items, columns, groups, selection, checks, images, view modes, virtual mode, label editing, keyboard, drag/drop, accessibility, and event semantics.

**Architecture:** `BootstrapListView` derives directly from `System.Windows.Forms.ListView`; the native control remains the data, layout, scrolling, selection, keyboard, editing, virtualization, and accessibility engine. The framework sets `OwnerDraw = true` and owns only painting, hover bookkeeping required for painting, theme/font synchronization, and DPI-scaled drawing geometry anchored to native item/subitem bounds. Small internal render/layout helpers keep visual-state decisions testable without introducing a parallel list model or replacing native interaction behavior.

**Tech Stack:** C#, WinForms, `net48;net8.0-windows`, existing `BootstrapThemeManager`, `BootstrapVariant`, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`, `TextRenderer`, NUnit, integrated demo application.

**Spec:** `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, plus the explicit public/visual contracts in this plan.

## Global Constraints

- Namespace remains `MyDmsVn.Bootstrap5WinFormUI`.
- Production library target frameworks remain exactly `net48;net8.0-windows`.
- Use native WinForms controls and behavior; Bootstrap is the visual/design inspiration, not a CSS/JavaScript runtime dependency.
- `BootstrapListView` must derive directly from `System.Windows.Forms.ListView`.
- Native `Items`, `Columns`, `Groups`, `SelectedItems`, `CheckedItems`, `LargeImageList`, `SmallImageList`, `StateImageList`, `View`, `MultiSelect`, `LabelEdit`, `VirtualMode`, `VirtualListSize`, `ListViewItemSorter`, drag/drop, keyboard, accessibility, and inherited events remain authoritative.
- Do not introduce a custom item collection, data-binding layer, selection model, sorting model, virtualization provider, group model, or replacement accessibility tree.
- Reuse `BootstrapThemeManager`, theme tokens, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`, and existing rendering conventions instead of creating component-local theme infrastructure.
- Do not require FontAwesome.Sharp or any new external package.
- Do not use `Math.Clamp` or other APIs unavailable on `net48` unless an existing compatibility abstraction already covers them.
- Dispose every framework-owned GDI resource and unsubscribe every framework-owned event subscription.
- Do not dispose caller-owned `ImageList`, `Image`, `Font`, `ListViewItem`, `ColumnHeader`, or `ListViewGroup` objects.
- Do not use independent timers, `Thread.Sleep`, or `Task.Delay` for hover, rendering, or lifecycle behavior.
- Theme switching, handle recreation, DPI changes, and disposal must not mutate caller-owned list data or native interaction state.
- Keep V1 public API intentionally small; inherited native properties are not renamed or aliased merely to sound more Bootstrap-like.

---

## Reference Behavior and WinForms Adaptation

Bootstrap 5 does not define an official desktop `ListView`. The design target is therefore **Bootstrap visual language applied to the native WinForms ListView contract**, with Bootstrap List Group/Table visual cues where they fit without weakening native desktop behavior.

Primary WinForms references:

- `ListView`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview
- `OwnerDraw`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.ownerdraw
- `DrawItem`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.drawitem
- `DrawSubItem`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.drawsubitem
- `DrawColumnHeader`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.drawcolumnheader
- `VirtualMode`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.virtualmode
- Tile view: https://learn.microsoft.com/dotnet/desktop/winforms/controls/how-to-enable-tile-view-in-a-windows-forms-listview-control

Implementation must preserve these native rules:

1. `ListView` supports `LargeIcon`, `SmallIcon`, `List`, `Details`, and `Tile`; V1 must remain usable in all five modes.
2. When `OwnerDraw == true`, non-`Details` views are rendered through `DrawItem`; `Details` additionally uses `DrawSubItem` and `DrawColumnHeader`.
3. In `Tile` view, subitem text is part of `DrawItem`; `DrawSubItem` is not the tile renderer.
4. In `Details`, `DrawSubItem` is raised only for subitems that have a corresponding `ColumnHeader`; the first subitem represents the parent `ListViewItem` itself.
5. `FullRowSelect`, `GridLines`, `HeaderStyle`, column alignment, column sizing/reordering, and header click behavior remain native contracts rather than framework aliases.
6. `VirtualMode` means `VirtualListSize` plus native retrieval/cache/search events are authoritative. Rendering code must not enumerate a shadow `Items` model, cache item identity, or require a populated normal `Items` collection.
7. `LabelEdit` remains the native in-place edit lifecycle. The framework does not replace the edit control or synthesize label-edit events.
8. `ListViewItemSorter` and native sorting remain caller-owned. V1 does not invent sort descriptors or sort glyph state.
9. `LargeImageList`, `SmallImageList`, and `StateImageList` are caller-owned. Rendering may read them but never clone, replace, resize, or dispose them behind the caller's back.
10. If `CheckBoxes == true`, checked state remains `ListViewItem.Checked`; owner drawing must reflect it without maintaining a second checked-state collection.
11. If a caller supplies `StateImageList` and a valid item `StateImageIndex`, render that state image rather than silently replacing it with framework checkbox art.
12. Group membership and native group behavior remain supported. WinForms exposes no `DrawListViewGroupHeader` owner-draw event, so V1 must leave group headers native/system-rendered instead of adding Win32 `NM_CUSTOMDRAW`/P/Invoke solely to theme them.
13. Native mouse/keyboard behavior remains authoritative: arrows, Home/End, PageUp/PageDown, Space/checkbox interaction, Ctrl/Shift multi-selection, activation, label edit, context menus, drag/drop, and inherited selection events must not be reimplemented.
14. `RightToLeft` / `RightToLeftLayout` and native hit-testing/layout remain authoritative. Custom text/image drawing must use native bounds as anchors rather than building a separate item-positioning engine.

---

## Public Contract to Implement

```csharp
public class BootstrapListView : ListView
{
    public BootstrapVariant Variant { get; set; } = BootstrapVariant.Primary;

    public bool Striped { get; set; }

    public bool HoverHighlight { get; set; } = true;
}
```

### Public API rules

- Default `Variant` is `BootstrapVariant.Primary`.
- Default `Striped` is `false`.
- Default `HoverHighlight` is `true`.
- Changing `Variant`, `Striped`, or `HoverHighlight` invalidates presentation only; it must not mutate items, selection, checked state, groups, virtual state, view mode, focus, scrolling, or image lists.
- `Striped` applies only to row-oriented `View.Details` and `View.List`. It has no visual effect in `LargeIcon`, `SmallIcon`, or `Tile`, but the property value remains unchanged when the view changes.
- `HoverHighlight` is presentation-only. Do not enable or alter inherited `HotTracking`, `HoverSelection`, `Activation`, or selection behavior to implement it.
- `OwnerDraw` is framework-owned and should remain `true` while Bootstrap rendering is active. Hide/restrict it in the designer only if this can be done without breaking the inherited public contract; do not add a public render-mode abstraction in V1.
- Existing `ListViewItem.ForeColor`, `ListViewItem.BackColor`, `ListViewItem.Font`, `ListViewSubItem.ForeColor`, `ListViewSubItem.BackColor`, and `ListViewSubItem.Font` remain meaningful for neutral items/cells. Selected, inactive-selection, hover, and disabled states may override colors where necessary for legibility and state visibility.
- Do not add custom selection, checked, activation, sorting, label-edit, or virtual-mode events. Use inherited `ListView` events.

### Explicit V1 exclusions

Do **not** add these public APIs in V1:

```text
BorderRadius
ItemPadding
HeaderStyle          // native HeaderStyle already exists
NodeHeight           // irrelevant / avoid copied TreeView naming
FullRowSelect        // native property already exists
SelectedItem / SelectedItems aliases
CheckedItems aliases
EmptyText / EmptyStateText
Loading / LoadingText
Custom item renderer delegate
Custom item model / binding source
SortDirection / SortColumn
GroupHeaderStyle
```

Rationale:

- Rounded clipping/borders around a native HWND and its scrollbars require non-client/region behavior that is outside the owner-draw item contract.
- Native ListView does not expose a reliable managed item-collection changed event suitable for a zero-data overlay; adding an empty-state API would either require Win32 message interception or a separate host/composite control. Keep that out of V1.
- Loading overlays are useful but orthogonal to the native-backed rendering contract and should be considered together with a future reusable data-state overlay abstraction rather than copied ad hoc from `BootstrapDataGridView`.
- Header sorting and group-header custom drawing require extra state/Win32 surface area not necessary for a production-ready native-backed V1.

---

## Visual Contract

### Surface and border

- Use theme `Surface` as the normal list background and `Text` as normal text.
- Preserve inherited `BorderStyle`; do not replace it with a rounded framework border.
- When `BorderStyle.FixedSingle` is used, native border behavior remains authoritative. The framework may set foreground/background theme colors but must not alter border style on every theme change.
- Do not paint over native scrollbars.

### Selection

- Resolve the active accent with:

```csharp
var accent = BootstrapVariantColorResolver.Resolve(theme.Colors, Variant);
var selectionText = ColorUtil.GetContrastingTextColor(
    accent,
    theme.Colors.Light,
    theme.Colors.Dark);
```

- Active selected items use the resolved variant color and contrasting text.
- When focus is outside the control and `HideSelection == false`, keep selection visible with a subdued theme surface/border treatment rather than pretending it is active focus.
- When focus is outside and `HideSelection == true`, do not force a selection highlight that native ListView intends to hide.
- In `Details`, respect `FullRowSelect`: if false, do not turn selection into a full-row semantic that native ListView did not request.

### Stripes

- `Striped == true` uses `SurfaceSecondary` for odd-index neutral rows only in `Details` and `List`.
- Explicit neutral item/subitem background overrides take precedence over striped background.
- Selection and hover take precedence over striped background.
- Use stable native item index; do not renumber per group or create a shadow index map.

### Hover

- Hover background is a subtle theme-derived highlight; do not use the full active selection color.
- Hover never changes `SelectedIndices`, `FocusedItem`, `Checked`, or activation state.
- Track only the currently hot item index/reference needed to invalidate old/new item bounds.
- Mouse leave clears hover and invalidates only the previously hot item when practical.

### Disabled

- Disabled presentation uses `MutedText` and a subdued surface while preserving enough contrast to read content.
- Do not mutate `Enabled` or item state to manufacture disabled visuals.

### Focus

- Draw a focus cue only when the control is focused, focus cues should be shown, and the native focused/selected item warrants it.
- Focus cue geometry follows native item/label bounds and `FullRowSelect` semantics.
- Do not synthesize focus on mouse hover or selected-but-unfocused rows.

### Details header

- Draw `ColumnHeader` background with `SurfaceSecondary`, text with theme `Text`, and separators with theme `Border`.
- Respect inherited `HeaderStyle`, `ColumnHeader.TextAlign`, column order, native bounds, and RTL.
- Do not add a sort glyph because native `ListView` has no framework-owned sort state in this control.

### Images and state images

- Read images from caller-owned `LargeImageList`, `SmallImageList`, and `StateImageList` according to native `View`/item indexes.
- Use native `ListViewItem.GetBounds(ItemBoundsPortion.Icon/Label/Entire)` and event bounds as primary geometry anchors where available.
- Never scale or mutate the caller's `ImageList.ImageSize` automatically.
- Clip drawing to the event/client bounds and tolerate missing/invalid image indexes without throwing.
- If `StateImageList` supplies the current state image, use it. Otherwise, when `CheckBoxes == true`, draw a lightweight framework-owned checkbox glyph from native `item.Checked` without storing check state separately.

### Text

- Use `TextRenderer` for list/header text.
- Respect item/subitem font overrides in neutral states.
- Respect `ColumnHeader.TextAlign` in `Details`.
- Use `NoPrefix` so ampersands in data are not treated as mnemonics.
- Apply end ellipsis or wrapping according to the native view's available label region; do not let text draw over images, state icons, neighboring columns, or scrollbars.
- Mirror horizontal text/layout flags for RTL.

### Group headers

- V1 does not custom-paint group headers because WinForms owner-draw events do not expose them.
- Do not disable `ShowGroups`, remove groups, copy items out of groups, or use P/Invoke custom draw to work around that limitation.
- Demo/docs must call out that item surfaces are Bootstrap-themed while group headers remain native/system-rendered in V1.

---

## Internal Rendering Contract

Create focused, allocation-light internal helpers rather than placing all state/color/layout decisions inline in `BootstrapListView`.

### `BootstrapListViewRenderLogic`

```csharp
internal enum BootstrapListViewItemVisualState
{
    Neutral,
    Hovered,
    SelectedActive,
    SelectedInactive,
    Disabled
}

internal readonly struct BootstrapListViewItemPalette
{
    public BootstrapListViewItemPalette(Color backColor, Color foreColor, Color borderColor)
    {
        BackColor = backColor;
        ForeColor = foreColor;
        BorderColor = borderColor;
    }

    public Color BackColor { get; }
    public Color ForeColor { get; }
    public Color BorderColor { get; }
}

internal static class BootstrapListViewRenderLogic
{
    internal static BootstrapListViewItemVisualState ResolveState(
        bool enabled,
        bool selected,
        bool controlFocused,
        bool hideSelection,
        bool hovered);

    internal static bool ShouldUseStripe(View view, bool striped, int itemIndex);

    internal static BootstrapListViewItemPalette ResolvePalette(
        BootstrapTheme theme,
        BootstrapVariant variant,
        BootstrapListViewItemVisualState state,
        bool striped,
        Color explicitBackColor,
        Color explicitForeColor);
}
```

Required precedence:

```text
Disabled
  > SelectedActive
  > SelectedInactive
  > Hovered
  > explicit neutral item/subitem colors
  > striped neutral background
  > normal theme surface
```

If `selected == true`, `controlFocused == false`, and `hideSelection == true`, resolve to neutral/hover according to the mouse rather than forcing `SelectedInactive`.

### `BootstrapListViewLayoutLogic`

```csharp
internal static class BootstrapListViewLayoutLogic
{
    internal static Rectangle Deflate(Rectangle bounds, int horizontal, int vertical);

    internal static Rectangle GetFocusBounds(
        View view,
        Rectangle itemBounds,
        Rectangle labelBounds,
        bool fullRowSelect);

    internal static TextFormatFlags GetTextFlags(
        HorizontalAlignment alignment,
        bool rightToLeft,
        bool wordWrap);

    internal static Rectangle GetTileTextBounds(
        Rectangle itemBounds,
        Rectangle imageBounds,
        int gap,
        bool rightToLeft);
}
```

Rules:

- Inputs are already-native bounds whenever possible.
- Helpers are pure and must not access control handles, global theme state, `Items`, or image lists.
- Scale only framework-owned gaps/insets with `DpiScaler`; do not rescale native item/column bounds a second time.
- Return empty rectangles safely when geometry collapses; no negative width/height.

---

## File Structure and Responsibilities

Create:

```text
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewRenderLogic.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewLayoutLogic.cs

tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewRenderLogicTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewLayoutLogicTests.cs

demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ListViewDemoForm.cs
```

Modify:

```text
demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs
docs/COMPONENTS.md
```

Responsibilities:

- `BootstrapListView.cs`: native subclass, public properties, owner-draw event handling/overrides, theme/font lifecycle, hover invalidation, image/state-image drawing coordination, handle/DPI lifecycle.
- `BootstrapListViewRenderLogic.cs`: visual state and palette resolution only.
- `BootstrapListViewLayoutLogic.cs`: deterministic rectangle/text-flag helpers only.
- `BootstrapListViewTests.cs`: native contract, public API, lifecycle, view-mode, virtual-mode, selection/check/image/group/label-edit behavior.
- Render/layout tests: pure logic, theme/color precedence, geometry, RTL, zero-size cases.
- `ListViewDemoForm.cs`: integrated visual/manual verification across modes and states.
- `docs/COMPONENTS.md`: supported contract, examples, V1 limitations.

---

## Task 1: Lock the Native-Backed Public Contract with Failing Tests

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs`

**Interfaces:**
- Consumes: native `System.Windows.Forms.ListView`, existing `BootstrapVariant`.
- Produces: `BootstrapListView`, `Variant`, `Striped`, `HoverHighlight` used by all later tasks.

- [ ] **Step 1: Add the STA/non-parallel contract fixture and failing default/native tests.**

Use the existing control-test convention:

```csharp
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapListViewTests
{
    [Test]
    public void DefaultsMatchNativeBackedContract()
    {
        using var list = new BootstrapListView();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list, Is.InstanceOf<ListView>());
            Assert.That(list.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(list.Striped, Is.False);
            Assert.That(list.HoverHighlight, Is.True);
            Assert.That(list.OwnerDraw, Is.True);
        }));
    }

    [Test]
    public void NativeCollectionsAndBehaviorPropertiesRemainCallerOwned()
    {
        using var list = new BootstrapListView
        {
            View = View.Details,
            FullRowSelect = true,
            CheckBoxes = true,
            MultiSelect = true,
            LabelEdit = true,
            HeaderStyle = ColumnHeaderStyle.Clickable
        };
        var column = new ColumnHeader { Text = "Name", Width = 180 };
        var group = new ListViewGroup("Group A");
        var item = new ListViewItem("Alpha") { Group = group, Checked = true };

        list.Columns.Add(column);
        list.Groups.Add(group);
        list.Items.Add(item);
        item.Selected = true;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.Columns[0], Is.SameAs(column));
            Assert.That(list.Groups[0], Is.SameAs(group));
            Assert.That(list.Items[0], Is.SameAs(item));
            Assert.That(list.SelectedItems.Contains(item), Is.True);
            Assert.That(list.CheckedItems.Contains(item), Is.True);
            Assert.That(list.FullRowSelect, Is.True);
            Assert.That(list.LabelEdit, Is.True);
        }));
    }
}
```

- [ ] **Step 2: Add a failing reflection test that prevents API duplication/feature creep.**

```csharp
[Test]
public void V1DoesNotDeclareAliasesOrDeferredDataStateApis()
{
    var declared = typeof(BootstrapListView)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        .Select(x => x.Name)
        .ToArray();

    Assert.That(declared, Is.EquivalentTo(new[]
    {
        nameof(BootstrapListView.Variant),
        nameof(BootstrapListView.Striped),
        nameof(BootstrapListView.HoverHighlight)
    }));
}
```

If designer-only shadowing of inherited `OwnerDraw` is later proven necessary, adjust this test deliberately and document why; do not accidentally grow the runtime API.

- [ ] **Step 3: Add a failing test proving presentation property changes do not mutate native state.**

Create two items, select/check one, set `Variant`, `Striped`, and `HoverHighlight`, then assert the same item objects, selection, checked state, `View`, `TopItem` where available, groups, and image-list references remain unchanged.

- [ ] **Step 4: Run focused tests and verify RED.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListView"
```

Expected: compile/test failure because `BootstrapListView` does not exist.

- [ ] **Step 5: Implement the minimal public class skeleton.**

Start with:

```csharp
public class BootstrapListView : ListView
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private bool _striped;
    private bool _hoverHighlight = true;

    public BootstrapListView()
    {
        OwnerDraw = true;
    }

    [Category("Appearance")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            if (_variant == value) return;
            _variant = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    [DefaultValue(false)]
    public bool Striped
    {
        get => _striped;
        set
        {
            if (_striped == value) return;
            _striped = value;
            Invalidate();
        }
    }

    [Category("Appearance")]
    [DefaultValue(true)]
    public bool HoverHighlight
    {
        get => _hoverHighlight;
        set
        {
            if (_hoverHighlight == value) return;
            _hoverHighlight = value;
            Invalidate();
        }
    }
}
```

Add matching `Description` attributes and XML docs following existing controls.

- [ ] **Step 6: Run focused tests on both target frameworks.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListView"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapListView"
```

Expected: PASS for the contract slice.

- [ ] **Step 7: Commit the contract slice.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs
git commit -m "feat: add BootstrapListView native contract"
```

---

## Task 2: Add Pure Visual-State and Layout Logic Before Painting

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewRenderLogic.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewLayoutLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewLayoutLogicTests.cs`

**Interfaces:**
- Consumes: `BootstrapTheme`, `BootstrapVariant`, `BootstrapVariantColorResolver.Resolve(BootstrapThemeColors, BootstrapVariant)`, `ColorUtil.GetContrastingTextColor(...)`, native `View`/`HorizontalAlignment`.
- Produces: `BootstrapListViewItemVisualState`, `BootstrapListViewItemPalette`, `BootstrapListViewRenderLogic`, `BootstrapListViewLayoutLogic` consumed by owner drawing.

- [ ] **Step 1: Write failing state-precedence tests.**

Cover at least:

```csharp
Assert.That(
    BootstrapListViewRenderLogic.ResolveState(true, false, true, true, false),
    Is.EqualTo(BootstrapListViewItemVisualState.Neutral));

Assert.That(
    BootstrapListViewRenderLogic.ResolveState(true, true, true, true, false),
    Is.EqualTo(BootstrapListViewItemVisualState.SelectedActive));

Assert.That(
    BootstrapListViewRenderLogic.ResolveState(true, true, false, false, false),
    Is.EqualTo(BootstrapListViewItemVisualState.SelectedInactive));

Assert.That(
    BootstrapListViewRenderLogic.ResolveState(true, true, false, true, true),
    Is.EqualTo(BootstrapListViewItemVisualState.Hovered));

Assert.That(
    BootstrapListViewRenderLogic.ResolveState(false, true, true, false, true),
    Is.EqualTo(BootstrapListViewItemVisualState.Disabled));
```

- [ ] **Step 2: Write failing stripe and palette tests.**

Assert:

- odd `Details`/`List` rows stripe only when enabled;
- icon/tile modes never stripe;
- explicit neutral colors override stripe;
- selected active uses `BootstrapVariantColorResolver.Resolve(...)`;
- selection foreground uses `ColorUtil.GetContrastingTextColor(...)`;
- disabled uses muted text;
- inactive selection differs from active selection.

- [ ] **Step 3: Write failing geometry/text-flag tests.**

Cover:

```csharp
[TestCase(View.Details, true)]
[TestCase(View.List, true)]
[TestCase(View.SmallIcon, false)]
[TestCase(View.LargeIcon, false)]
[TestCase(View.Tile, false)]
public void FullRowFocusUsesEntireItemOnlyForRowOrientedViews(View view, bool expected)
```

Also test zero/negative-collapse safety, RTL text flags, left/center/right header alignment, tile text rectangles on both LTR and RTL, and `Deflate` clamping to `Rectangle.Empty` rather than returning negative dimensions.

- [ ] **Step 4: Run pure tests and verify RED.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListViewRenderLogic|FullyQualifiedName~BootstrapListViewLayoutLogic"
```

- [ ] **Step 5: Implement the exact internal contracts from this plan.**

Keep helpers pure. Use `Color.Empty` to mean “no explicit caller color” and do not access `BootstrapThemeManager.CurrentTheme` from the helpers; pass the theme explicitly.

- [ ] **Step 6: Run pure tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListViewRenderLogic|FullyQualifiedName~BootstrapListViewLayoutLogic"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapListViewRenderLogic|FullyQualifiedName~BootstrapListViewLayoutLogic"
```

- [ ] **Step 7: Commit the pure rendering foundation.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewRenderLogic.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewLayoutLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewRenderLogicTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewLayoutLogicTests.cs
git commit -m "test: define BootstrapListView rendering contract"
```

---

## Task 3: Implement Theme/Font Lifecycle and `Details` Owner Drawing

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs`

**Interfaces:**
- Consumes: Task 2 render/layout helpers, `BootstrapThemeManager`, `DpiScaler`.
- Produces: production owner drawing for `View.Details`, shared theme/font lifecycle used by every view.

- [ ] **Step 1: Add failing runtime-theme and font-lifecycle tests.**

Use the same pattern as `BootstrapDataGridViewTests`:

- construct under Light theme and assert `BackColor`/`ForeColor` use theme tokens;
- switch to Dark and assert presentation updates;
- set a custom caller font, switch themes, and assert the framework no longer overwrites the caller font;
- dispose the control, switch theme again, and assert no exception/event leak;
- recreate the handle and assert `OwnerDraw` remains active without losing items/selection.

- [ ] **Step 2: Add test seams for deterministic drawing without pixel snapshots.**

Prefer testing pure state/layout and native contract. Where the control must prove event routing, create a test subclass that invokes protected lifecycle methods or attaches to inherited draw events; do not introduce public debug APIs solely for tests.

- [ ] **Step 3: Implement theme subscription and theme-font ownership.**

Mirror the proven `BootstrapDataGridView` lifecycle pattern:

```csharp
private bool _themeSubscribed;
private bool _settingThemeFont;
private bool _useThemeFont = true;
private Font? _themeFont;
```

Constructor:

```csharp
BootstrapThemeManager.ThemeChanged += OnThemeChanged;
_themeSubscribed = true;
ApplyThemeFont();
ApplyTheme();
```

`OnFontChanged` marks theme-font ownership off only for caller-driven font changes. `Dispose(bool)` unsubscribes and disposes only `_themeFont`.

- [ ] **Step 4: Wire `DrawColumnHeader`, `DrawItem`, and `DrawSubItem` for `Details`.**

Use protected overrides when available (`OnDrawColumnHeader`, `OnDrawItem`, `OnDrawSubItem`) rather than subscribing the control to its own public events.

Header renderer must:

- fill `SurfaceSecondary`;
- draw theme border separators;
- use `ColumnHeader.TextAlign` plus RTL-aware `TextRenderer` flags;
- honor event clipping and zero-sized bounds;
- avoid sort glyphs and custom header hit-testing.

- [ ] **Step 5: Render `Details` row backgrounds with native semantics.**

For each item:

1. Resolve active/inactive/hidden selection from `item.Selected`, `Focused`, `HideSelection`.
2. Resolve hover from framework hover bookkeeping only.
3. Apply `Striped` only for neutral odd rows.
4. Respect explicit item background in neutral state.
5. If `FullRowSelect == true`, fill the row visual across event/native row bounds.
6. If `FullRowSelect == false`, do not turn all subitems into a selected row visual; confine selection treatment to the native label/first-item selection region.

- [ ] **Step 6: Render `Details` subitems.**

For subitem index 0:

- reserve/draw state image or checkbox if present;
- reserve/draw small image if present;
- draw item text using item/subitem font/color precedence.

For other subitems:

- use `DrawListViewSubItemEventArgs.Bounds` as the cell anchor;
- respect `ColumnHeader.TextAlign`;
- respect `UseItemStyleForSubItems` and explicit subitem styles;
- clip to cell bounds;
- use `NoPrefix | EndEllipsis | VerticalCenter` and RTL flags.

- [ ] **Step 7: Implement themed checkbox fallback without state duplication.**

Rules:

```text
if valid caller StateImageList + StateImageIndex:
    draw caller state image
else if CheckBoxes:
    draw framework checkbox glyph from item.Checked
else:
    draw no state glyph
```

The fallback glyph uses theme border/surface and the resolved variant accent when checked. It must read `item.Checked` only; never write it during paint.

- [ ] **Step 8: Implement `GridLines`-aware separators only after manual verification.**

First verify whether native grid lines remain visible under owner drawing on both TFMs. If native lines are already correct, do not double-draw. If owner draw requires framework separators, draw theme `Border` lines only when `GridLines == true` and add a regression test around the chosen behavior. The goal is semantic preservation, not forcing custom lines unconditionally.

- [ ] **Step 9: Run focused tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListView"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapListView"
```

- [ ] **Step 10: Commit `Details` rendering.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs
git commit -m "feat: theme BootstrapListView details view"
```

---

## Task 4: Implement `List`, `SmallIcon`, `LargeIcon`, and `Tile` Rendering

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewLayoutLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewLayoutLogicTests.cs`

**Interfaces:**
- Consumes: `DrawItem`, native `ListViewItem.GetBounds(ItemBoundsPortion...)`, image lists, Task 2/3 palette rules.
- Produces: complete owner drawing for all five native `View` values.

- [ ] **Step 1: Add a failing five-view smoke test.**

For each `View` value, create a handle with at least two items, images where applicable, one selected item, and call `DrawToBitmap` or the safest existing test harness supported by the repository. Assert no exception and that native `View`, item count, selection, and image-list references remain unchanged after drawing.

Do not make fragile per-pixel color assertions for Windows-native text/image rendering.

- [ ] **Step 2: Implement shared item-bound anchoring.**

Use:

```csharp
var entire = item.GetBounds(ItemBoundsPortion.Entire);
var icon = item.GetBounds(ItemBoundsPortion.Icon);
var label = item.GetBounds(ItemBoundsPortion.Label);
```

Treat these as authoritative geometry. Use `DrawListViewItemEventArgs.Bounds` as the clipping/event region. Only calculate framework gaps around those native anchors.

- [ ] **Step 3: Implement `View.List`.**

- fill neutral/stripe/hover/selection background;
- draw state/checkbox and small image without overlapping the native label region;
- draw single-line text with ellipsis;
- apply `Striped` here;
- preserve horizontal list scrolling and native hit regions.

- [ ] **Step 4: Implement `View.SmallIcon`.**

- draw small image using the caller image list;
- draw label beside native icon bounds;
- no stripe treatment;
- selection/hover follows item/label bounds rather than pretending it is a full-width row.

- [ ] **Step 5: Implement `View.LargeIcon`.**

- draw large image centered in/around native icon bounds;
- draw label using native label bounds with centered, wrapping text consistent with the native large-icon layout;
- no stripe treatment;
- focus cue follows label/item geometry;
- tolerate long labels and partially clipped icon rows.

- [ ] **Step 6: Implement `View.Tile`.**

Because `DrawSubItem` is not the tile renderer:

- render primary item text plus visible subitem lines from `DrawItem`;
- use `TileSize`, event/item bounds, native icon bounds, `Columns`, and `BootstrapListViewLayoutLogic.GetTileTextBounds(...)`;
- keep primary text visually stronger only if this can be done with the existing item font without synthesizing/leaking per-frame fonts; otherwise use one font and theme color hierarchy;
- use `MutedText` for secondary lines unless the caller supplied an explicit subitem foreground;
- clip the number of rendered lines to available tile height;
- no stripe treatment.

- [ ] **Step 7: Add missing/invalid-image regression tests.**

Cover:

- `ImageIndex == -1`;
- `ImageKey` not found;
- image index beyond list count;
- no image list;
- caller replaces an image list at runtime;
- caller disposes/replaces an image list according to normal WinForms ownership rules.

Rendering must skip missing images without throwing or changing item state.

- [ ] **Step 8: Run focused tests on both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListView"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapListView"

git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewLayoutLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewLayoutLogicTests.cs
git commit -m "feat: render all BootstrapListView view modes"
```

---

## Task 5: Preserve Hover, Focus, Virtual Mode, Groups, Label Editing, and Native Interaction

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs`

**Interfaces:**
- Consumes: native mouse/focus/virtual/group/edit APIs.
- Produces: interaction-safe presentation bookkeeping with no duplicate behavior model.

- [ ] **Step 1: Add failing hover-is-presentation-only tests.**

Use a test subclass to expose mouse methods if necessary. Move the pointer from item A to item B and then outside; assert:

- selection does not change;
- checked state does not change;
- `FocusedItem` is not reassigned by hover code;
- `HotTracking`, `HoverSelection`, and `Activation` remain exactly as the caller set them;
- disabling `HoverHighlight` clears framework hover painting state without mutating native item state.

- [ ] **Step 2: Implement allocation-light hover bookkeeping.**

Keep only what is needed to repaint:

```csharp
private int _hoveredItemIndex = -1;
```

On mouse move:

1. use native `GetItemAt(e.X, e.Y)`;
2. compare its current `Index` with `_hoveredItemIndex`;
3. invalidate old/new item rectangles using native bounds;
4. never call selection/focus/check APIs.

On mouse leave, clear the index and invalidate the old item. Handle virtual mode and handle recreation defensively; if an index becomes invalid, fall back to `Invalidate()` rather than throwing.

- [ ] **Step 3: Add failing virtual-mode tests.**

Create:

```csharp
using var list = new BootstrapListView
{
    VirtualMode = true,
    VirtualListSize = 1000,
    View = View.Details
};
list.Columns.Add("Name", 180);
list.RetrieveVirtualItem += (_, e) =>
    e.Item = new ListViewItem($"Item {e.ItemIndex}");
```

Assert handle creation/render smoke tests do not require a populated normal `Items` collection, do not change `VirtualListSize`, and preserve the caller's `RetrieveVirtualItem` event ownership.

Add a separate test changing `VirtualListSize` at runtime and ensure Bootstrap code does not cache stale item references/indexes.

- [ ] **Step 4: Add group-preservation tests.**

With `ShowGroups = true`, multiple groups, and grouped items:

- assert groups/items remain the exact same objects after theme/Variant changes;
- assert no framework code disables `ShowGroups`;
- assert owner drawing item surfaces does not remove/reassign groups;
- manual demo verification records that group headers are native/system-rendered in V1.

- [ ] **Step 5: Add label-edit preservation tests.**

Verify `LabelEdit = true` remains enabled through theme changes/handle recreation and Bootstrap code does not manually raise `BeforeLabelEdit`/`AfterLabelEdit`. Where automated edit UI is too environment-sensitive, test property/event ownership and cover actual editing manually in the demo matrix.

- [ ] **Step 6: Add keyboard/native-selection manual contract checks to the demo checklist, not custom key handlers.**

No `ProcessCmdKey`, `OnKeyDown`, or message filter should be added unless a reproducible owner-draw regression proves native behavior is broken. Required manual checks:

```text
Up / Down / Left / Right as appropriate to view
Home / End
PageUp / PageDown
Ctrl+click and Shift+click with MultiSelect
Space with CheckBoxes
F2 / label edit when LabelEdit=true
Enter / ItemActivate according to Activation
Tab into and out of the ListView
Context menu keyboard invocation
```

- [ ] **Step 7: Add focus/HideSelection regression tests.**

Pure render-logic tests cover color state; control tests must prove toggling focus/HideSelection does not change selected item objects or event semantics.

- [ ] **Step 8: Run tests and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListView"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapListView"

git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs
git commit -m "test: harden BootstrapListView native interaction"
```

---

## Task 6: DPI, RTL, Handle-Recreation, Resource, and Performance Hardening

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewLayoutLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewLayoutLogicTests.cs`

**Interfaces:**
- Consumes: existing DPI/theme infrastructure.
- Produces: stable rendering across DPI/theme/RTL/lifecycle scenarios without leaks or state mutation.

- [ ] **Step 1: Add DPI-scaling tests for framework-owned measurements only.**

Use `DpiScaler.Scale(...)` through deterministic layout inputs for 96, 120, 144, 168, and 192 DPI. Assert:

- gaps/insets scale monotonically;
- native bounds passed to helpers are not scaled a second time;
- zero-size and narrow columns safely collapse;
- checkbox/vector-glyph size uses DPI-aware framework metrics;
- image-list dimensions are not changed.

- [ ] **Step 2: Handle runtime DPI changes.**

Override the same DPI lifecycle hook used elsewhere in the repository (`OnDpiChangedAfterParent` where available in the shared target surface), invalidate cached framework-owned geometry only, then repaint. Do not resize caller image lists or change native `TileSize`, column widths, or item positions behind the caller's back.

- [ ] **Step 3: Add RTL tests.**

Test pure text/layout flags and a handle-level smoke scenario with:

```csharp
RightToLeft = RightToLeft.Yes,
RightToLeftLayout = true
```

for `Details`, `List`, and `Tile`. Assert no state mutation and no exception. Demo must visually verify state image/icon/text order mirrors correctly.

- [ ] **Step 4: Add handle-recreation tests.**

Exercise native properties known to recreate or materially update the handle when practical, plus explicit `RecreateHandle()` from a test subclass. Assert:

- items/columns/groups remain caller-owned;
- selected/checked state remains native;
- image-list references remain caller-owned;
- `OwnerDraw`/framework rendering continues;
- theme subscription is not duplicated.

Do not add a parallel state cache to “restore” native behavior that WinForms itself intentionally changes during a handle recreation.

- [ ] **Step 5: Audit GDI allocations inside draw paths.**

Rules:

- use `TextRenderer` rather than allocating `StringFormat` per cell;
- create `Pen`/`Brush` only when an existing static/control-paint path cannot express the visual, and wrap them in `using`;
- do not allocate a new `Font` per item/tile line;
- do not clone images;
- do not build LINQ collections inside per-item paint events;
- do not enumerate all items merely to render one item.

- [ ] **Step 6: Add a large-list performance smoke scenario.**

Demo/test harness should include at least:

```text
Normal mode: 5,000 items in Details
Virtual mode: VirtualListSize = 100,000
```

The purpose is to catch O(n)-per-paint framework code, not to assert machine-specific millisecond thresholds in unit tests.

- [ ] **Step 7: Run focused and full tests on both TFMs.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListView"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapListView"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48
```

- [ ] **Step 8: Commit hardening.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewLayoutLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewLayoutLogicTests.cs
git commit -m "fix: harden BootstrapListView rendering lifecycle"
```

---

## Task 7: Add Integrated Demo Coverage and Component Documentation

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ListViewDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Modify: `docs/COMPONENTS.md`

**Interfaces:**
- Consumes: completed `BootstrapListView` public contract.
- Produces: discoverable demo/manual acceptance surface and supported-component documentation.

- [ ] **Step 1: Build `ListViewDemoForm` with explicit scenario sections.**

The demo must expose at least:

```text
1. Details
   - columns/subitems
   - FullRowSelect on/off
   - GridLines on/off
   - Striped on/off
   - CheckBoxes
   - image + state image
   - MultiSelect

2. List / SmallIcon / LargeIcon / Tile
   - view switcher
   - long labels
   - image lists
   - selection + hover + focus

3. Groups
   - 2+ groups
   - explicit note that group headers are native-rendered in V1

4. Virtual Mode
   - VirtualListSize = 100000
   - RetrieveVirtualItem implementation
   - scroll/search smoke check

5. Runtime / lifecycle
   - Variant selector
   - Light/Dark through the integrated global theme switch
   - enabled/disabled
   - HideSelection on/off
   - RTL toggle where the demo architecture permits it
   - label editing
```

Use native controls for demo-only toggles unless an existing Bootstrap control is clearly appropriate. Do not add production APIs just to simplify the demo.

- [ ] **Step 2: Add the page to `MainForm.ConfigurePages()`.**

Place it near `DataGrid`/other data presentation controls:

```csharp
AddPage(
    "ListView",
    "Native-backed Details/List/Icon/Tile views with selection, checks, images, groups, virtual mode, Bootstrap theming, and DPI-aware owner drawing.",
    () => new ListViewDemoForm());
```

- [ ] **Step 3: Document `BootstrapListView` in `docs/COMPONENTS.md`.**

Include:

- direct inheritance from `ListView`;
- V1 public properties `Variant`, `Striped`, `HoverHighlight`;
- all five supported native views;
- preserved native collections/events/virtual mode;
- caller ownership of image lists/items/columns/groups;
- `Striped` only affects `Details`/`List`;
- group headers remain native-rendered in V1;
- no V1 `BorderRadius`, loading/empty-state overlay, custom sort state, or custom group-header renderer;
- short usage example.

Use an example such as:

```csharp
var list = new BootstrapListView
{
    Dock = DockStyle.Fill,
    View = View.Details,
    FullRowSelect = true,
    GridLines = true,
    Striped = true,
    Variant = BootstrapVariant.Primary
};

list.Columns.Add("Code", 120);
list.Columns.Add("Name", 240);
list.Items.Add(new ListViewItem(new[] { "P-001", "Northwind Widget" }));
```

- [ ] **Step 4: Execute the manual acceptance matrix.**

Verify on both a .NET 8 demo run and the `net48` build/runtime path used by the repository:

```text
[ ] Light theme
[ ] Dark theme
[ ] 96 DPI
[ ] 144 DPI or 150% Windows scaling
[ ] 192 DPI or 200% Windows scaling where available
[ ] Details
[ ] List
[ ] SmallIcon
[ ] LargeIcon
[ ] Tile
[ ] FullRowSelect true / false
[ ] GridLines true / false
[ ] CheckBoxes and StateImageList
[ ] MultiSelect Ctrl/Shift
[ ] Groups
[ ] VirtualMode
[ ] LabelEdit
[ ] Keyboard navigation
[ ] Tab into/out of control
[ ] Hover without selection mutation
[ ] HideSelection true / false
[ ] Disabled state
[ ] Runtime Variant switch
[ ] Runtime Light/Dark switch
[ ] RTL
[ ] Rapid view switches
[ ] Form close/disposal with no exception
```

- [ ] **Step 5: Build the demo and commit docs/demo.**

```powershell
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows -c Debug

git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ListViewDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs docs/COMPONENTS.md
git commit -m "docs: add BootstrapListView demo and guidance"
```

---

## Task 8: Final API Review, Compatibility Verification, and Definition of Done

**Files:**
- Review all files created/modified by Tasks 1–7.
- Modify only files required to fix findings.

**Interfaces:**
- Consumes: complete implementation.
- Produces: release-ready `BootstrapListView` slice.

- [ ] **Step 1: Perform an API-surface review.**

Confirm `BootstrapListView` declares only the intended V1 public properties:

```text
Variant
Striped
HoverHighlight
```

Inherited native APIs remain the way callers manipulate items, columns, groups, selection, checks, images, virtual mode, sorting, label editing, drag/drop, keyboard, and accessibility.

- [ ] **Step 2: Search for forbidden parallel state/feature creep.**

Search the implementation for accidental additions resembling:

```text
_customItems
_selectedItems
_checkedItems
_virtualItems
_sortColumn
_sortDirection
_groupHeaderRenderer
Loading
EmptyStateText
BorderRadius
Thread.Sleep
Task.Delay
Timer
```

Any state required purely for hover/theme/font ownership is acceptable; parallel data/selection state is not.

- [ ] **Step 3: Verify resource/lifecycle ownership.**

Confirm:

- theme event unsubscribed exactly once;
- framework theme font disposed exactly once;
- no caller font/image/image-list disposed;
- no per-item GDI leak;
- no self-subscribed draw/mouse handler duplicated after handle recreation;
- no timer/message filter remains after disposal.

- [ ] **Step 4: Run final builds/tests.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net8.0-windows -c Release
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net48 -c Release

dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows -c Release
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 -c Release

dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows -c Release
```

Expected: all commands succeed with no new warnings attributable to `BootstrapListView`.

- [ ] **Step 5: Review the final diff for accidental unrelated changes.**

```powershell
git status --short
git diff --stat
git diff --check
```

Only intended ListView implementation/tests/demo/docs changes should remain.

- [ ] **Step 6: Commit any final hardening fix separately.**

Example only when a real finding exists:

```powershell
git add <files-fixed-after-review>
git commit -m "fix: finalize BootstrapListView compatibility"
```

Do not create an empty “finalize” commit if Task 8 finds nothing.

---

## Definition of Done

`BootstrapListView` is complete when all of the following are true:

- [ ] `BootstrapListView` derives directly from native `ListView`.
- [ ] Both `net48` and `net8.0-windows` production builds succeed.
- [ ] Full test suite passes on both target frameworks.
- [ ] `OwnerDraw` renders Bootstrap-aware item presentation without a parallel item/selection model.
- [ ] `Details`, `List`, `SmallIcon`, `LargeIcon`, and `Tile` all render and interact correctly.
- [ ] `Variant`, `Striped`, and `HoverHighlight` match the documented defaults and do not mutate native state.
- [ ] `Striped` is limited to neutral `Details`/`List` rows.
- [ ] Selection honors focus, `HideSelection`, and `FullRowSelect` semantics.
- [ ] Checkboxes/state images reflect native item state and caller-provided state images without duplicate checked-state storage.
- [ ] Caller image lists and images remain caller-owned.
- [ ] `VirtualMode` works without a shadow collection or O(n)-per-paint framework scan.
- [ ] Groups remain intact; V1 native group-header rendering limitation is documented.
- [ ] Label editing, keyboard navigation, multi-selection, activation, context menus, and drag/drop remain native behavior.
- [ ] Light/Dark runtime switching updates presentation without changing data or interaction state.
- [ ] Caller-set fonts remain caller-owned after the first explicit override.
- [ ] DPI/RTL scenarios are visually verified and do not double-scale native geometry.
- [ ] Handle recreation does not duplicate framework subscriptions or introduce stale hover/item references.
- [ ] No GDI/event/timer resource leak is introduced.
- [ ] Integrated demo covers all five views, groups, virtual mode, checks/images, selection, theme, and lifecycle scenarios.
- [ ] `docs/COMPONENTS.md` documents public API, native ownership, usage, and V1 exclusions.
- [ ] Final diff contains no unrelated changes.

## Implementation Notes for Reviewers

Review this control against the same architectural principle already used by `BootstrapDataGridView` and planned for `BootstrapTreeView`: **native WinForms behavior is the source of truth; the framework owns presentation and shared infrastructure only.** A visually ambitious change is not acceptable if it requires replacing native selection, virtualization, item collections, keyboard behavior, accessibility, or caller-owned images.

The highest-risk review areas are:

1. `Details` owner drawing with `FullRowSelect == false`;
2. checkbox/state-image placement without modifying checked state;
3. `Tile` subitem layout because it is handled by `DrawItem`, not `DrawSubItem`;
4. hover bookkeeping under virtual mode and handle recreation;
5. group-header expectations, which must remain explicitly native-rendered in V1;
6. avoiding O(n) work in paint paths for large/virtual lists;
7. maintaining `net48` compatibility while sharing one implementation path with `net8.0-windows`.
