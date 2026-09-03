# BootstrapListView Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a production-ready `BootstrapListView` that applies Bootstrap-inspired, theme-aware, DPI-aware presentation to the native WinForms `ListView` while preserving native items, columns, groups, selection, checks, images, view modes, virtual mode, label editing, keyboard, drag/drop, accessibility, and event semantics.

**Architecture:** `BootstrapListView` derives directly from `System.Windows.Forms.ListView`; the native control remains the data, layout, scrolling, selection, keyboard, editing, virtualization, and accessibility engine. The framework owns only Bootstrap-aware painting, hover bookkeeping required for painting, theme/font synchronization, double-buffering, and DPI-scaled framework gaps anchored to native item/subitem bounds. In `View.Details`, framework painting is deliberately `DrawSubItem`-centric to avoid the documented Win32 owner-draw bug where an extra `DrawItem` event can repaint a row without accompanying `DrawSubItem` events.

**Tech Stack:** C#, WinForms, `net48;net8.0-windows`, existing `BootstrapThemeManager`, `BootstrapVariant`, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`, `TextRenderer`, NUnit, integrated demo application.

**Spec:** `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, plus the explicit public/visual/native contracts in this plan.

## Global Constraints

- Namespace remains `MyDmsVn.Bootstrap5WinFormUI`.
- Production library target frameworks remain exactly `net48;net8.0-windows`.
- Use native WinForms controls and behavior; Bootstrap is the visual/design inspiration, not a CSS/JavaScript runtime dependency.
- `BootstrapListView` must derive directly from `System.Windows.Forms.ListView`.
- Native `Items`, `Columns`, `Groups`, `SelectedItems`, `CheckedItems`, `SelectedIndices`, `CheckedIndices`, `LargeImageList`, `SmallImageList`, `StateImageList`, `View`, `MultiSelect`, `LabelEdit`, `VirtualMode`, `VirtualListSize`, `ListViewItemSorter`, drag/drop, keyboard, accessibility, and inherited events remain authoritative **where the current native `ListView` mode supports them**.
- Do not introduce a custom item collection, data-binding layer, selection model, sorting model, virtualization provider, group model, or replacement accessibility tree.
- Reuse `BootstrapThemeManager`, theme tokens, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`, and existing rendering conventions instead of creating component-local theme infrastructure.
- Do not require FontAwesome.Sharp or any new external package.
- Do not use `Math.Clamp` or other APIs unavailable on `net48` unless an existing compatibility abstraction already covers them.
- Dispose every framework-owned GDI resource and unsubscribe every framework-owned event subscription.
- Do not dispose caller-owned `ImageList`, `Image`, `Font`, `ListViewItem`, `ColumnHeader`, or `ListViewGroup` objects.
- Do not use independent timers, `Thread.Sleep`, or `Task.Delay` for hover, rendering, or lifecycle behavior.
- Theme switching, handle recreation, DPI changes, and disposal must not mutate caller-owned list data or native interaction state.
- Keep V1 public API intentionally small; inherited native properties are not renamed or aliased merely to sound more Bootstrap-like.
- Do not add framework code that attempts to reverse a documented native coercion/restriction such as `Tile` becoming `LargeIcon` when `VirtualMode` is enabled.

---

## Review Corrections Locked Into This Plan

The following decisions are mandatory because they address known native `ListView` constraints and previously identified plan defects:

1. **Details owner-draw workaround:** framework code must not paint `Details` row backgrounds in `OnDrawItem`. All framework `Details` painting is performed in `OnDrawSubItem`; column 0 paints the row/base background before cells are painted. This avoids the documented Win32 bug where `DrawItem` may occur without the corresponding `DrawSubItem` events when the mouse passes over a row.
2. **Virtual-mode setup order:** subscribe `RetrieveVirtualItem` before assigning a positive `VirtualListSize`. The plan must never show an object initializer that sets `VirtualListSize > 0` before the handler exists.
3. **Hover hit testing:** use `ListView.HitTest(...)`, not `GetItemAt(...)`. `GetItemAt` is only valid for `Details` and `Tile`, while `HoverHighlight` must work in all supported normal views.
4. **Double buffering:** the subclass constructor sets protected `DoubleBuffered = true` to reduce owner-draw flicker.
5. **Virtual-mode restrictions:** when `VirtualMode == true`, normal `Items`, `SelectedItems`, and `CheckedItems` access is not a supported native contract; callers use retrieval events plus `SelectedIndices`/`CheckedIndices`. Enabling virtual mode while `View == Tile` may coerce the native view to `LargeIcon`; Bootstrap code must not force it back.
6. **Effective style colors:** public WinForms getters expose effective colors, not a reliable public “caller explicitly set this color” flag. V1 therefore preserves **observable effective styles**. A candidate item/subitem color is treated as a custom visual override only when it differs from the inherited list color. If the caller explicitly assigns exactly the same color as the inherited list color, that assignment is intentionally indistinguishable from inheritance and stripes may still apply.

---

## Reference Behavior and WinForms Adaptation

Bootstrap 5 does not define an official desktop `ListView`. The design target is therefore **Bootstrap visual language applied to the native WinForms ListView contract**, with Bootstrap List Group/Table visual cues where they fit without weakening native desktop behavior.

Primary WinForms references:

- `ListView`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview
- `View`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.view
- `OwnerDraw`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.ownerdraw
- `DrawItem`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.drawitem
- `DrawSubItem`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.drawsubitem
- `DrawColumnHeader`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.drawcolumnheader
- `HitTest`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.hittest
- `VirtualMode`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.virtualmode
- `VirtualListSize`: https://learn.microsoft.com/dotnet/api/system.windows.forms.listview.virtuallistsize
- Tile view: https://learn.microsoft.com/dotnet/desktop/winforms/controls/how-to-enable-tile-view-in-a-windows-forms-listview-control

Implementation must preserve these native rules:

1. `ListView` supports `LargeIcon`, `SmallIcon`, `List`, `Details`, and `Tile`; V1 remains usable in all five normal modes.
2. When `OwnerDraw == true`, non-`Details` views are rendered through `DrawItem`; `Details` additionally uses `DrawSubItem` and `DrawColumnHeader`.
3. **Framework `Details` drawing uses `DrawSubItem` for all custom row/cell painting.** `OnDrawItem` in `Details` must not paint a framework background because the underlying Win32 control can raise an extra `DrawItem` without corresponding `DrawSubItem` events while the mouse moves over a row.
4. In `Tile` view, subitem text is part of `DrawItem`; `DrawSubItem` is not the tile renderer.
5. In `Details`, `DrawSubItem` is raised only for subitems that have a corresponding `ColumnHeader`; the first subitem represents the parent `ListViewItem` itself.
6. `FullRowSelect`, `GridLines`, `HeaderStyle`, column alignment, column sizing/reordering, and header click behavior remain native contracts rather than framework aliases.
7. `ListView.HitTest(x, y)` is the framework hover hit-test API. Do not use `GetItemAt` because it is only valid for `Details` and `Tile`.
8. `VirtualMode` means `VirtualListSize` plus native retrieval/cache/search events are authoritative. Rendering code must not enumerate a shadow `Items` model, cache virtual item identity, or require a populated normal `Items` collection.
9. When `VirtualMode == true` and `VirtualListSize > 0`, `RetrieveVirtualItem` must already be handled before the positive size is assigned.
10. In virtual mode, `Items`, `SelectedItems`, and `CheckedItems` are not valid native access paths. Use virtual retrieval plus `SelectedIndices` and `CheckedIndices` where selection/check information is needed.
11. When `VirtualMode` is enabled while the view is `Tile`, WinForms may change the view to `LargeIcon`. Bootstrap must accept the resulting native `View` value and must not restore `Tile` behind the caller's back.
12. `LabelEdit` remains the native in-place edit lifecycle. The framework does not replace the edit control or synthesize label-edit events.
13. `ListViewItemSorter` and native sorting remain caller-owned. V1 does not invent sort descriptors or sort glyph state.
14. `LargeImageList`, `SmallImageList`, and `StateImageList` are caller-owned. Rendering may read them but never clone, replace, resize, or dispose them behind the caller's back.
15. If `CheckBoxes == true`, checked state remains `ListViewItem.Checked`; owner drawing reflects it without maintaining a second checked-state collection. Native view limitations still apply; notably, do not manufacture framework checkboxes in `Tile` merely because the property value is true.
16. If a caller supplies `StateImageList` and a valid item `StateImageIndex`, render that state image rather than silently replacing it with framework checkbox art.
17. Group membership remains native. Native group display support varies by view; for example, `List` does not display groups. Bootstrap must not copy/reassign items to simulate groups where the native view does not support them.
18. WinForms exposes no `DrawListViewGroupHeader` owner-draw event, so V1 leaves group headers native/system-rendered instead of adding Win32 `NM_CUSTOMDRAW`/P/Invoke solely to theme them.
19. Native mouse/keyboard behavior remains authoritative: arrows, Home/End, PageUp/PageDown, Space/checkbox interaction, Ctrl/Shift multi-selection, activation, label edit, context menus, drag/drop, and inherited selection events must not be reimplemented.
20. `RightToLeft` / `RightToLeftLayout` and native layout remain authoritative. Custom text/image drawing uses native bounds as anchors rather than building a separate item-positioning engine.
21. The subclass sets protected `DoubleBuffered = true`; it does not introduce a public buffering API.

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
- `OwnerDraw` is framework-owned and remains `true` while Bootstrap rendering is active. Do not add a public render-mode abstraction in V1.
- `DoubleBuffered` is enabled internally and remains protected/native.
- Existing item/subitem colors and fonts remain meaningful as **effective public styles**. V1 does not claim to distinguish “explicitly assigned the same value as inherited” from normal inheritance because WinForms exposes no reliable public flag for that distinction.
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

- Rounded clipping/borders around a native HWND and its scrollbars require non-client/region behavior outside the owner-draw item contract.
- Native `ListView` does not expose a reliable managed item-collection changed event suitable for a zero-data overlay; adding an empty-state API would require Win32 interception or a composite host. Keep that out of V1.
- Loading overlays are useful but orthogonal to the native-backed rendering contract and should be considered with a future reusable data-state overlay abstraction rather than copied ad hoc from `BootstrapDataGridView`.
- Header sorting and group-header custom drawing require extra state/Win32 surface area not necessary for a production-ready native-backed V1.

---

## Visual Contract

### Surface and border

- Use theme `Surface` as the normal list background and `Text` as normal text.
- Preserve inherited `BorderStyle`; do not replace it with a rounded framework border.
- Do not paint over native scrollbars.

### Selection

Resolve the active accent with:

```csharp
var accent = BootstrapVariantColorResolver.Resolve(theme.Colors, Variant);
var selectionText = ColorUtil.GetContrastingTextColor(
    accent,
    theme.Colors.Light,
    theme.Colors.Dark);
```

- Active selected items use the resolved variant color and contrasting text.
- When focus is outside and `HideSelection == false`, keep selection visible with a subdued theme treatment.
- When focus is outside and `HideSelection == true`, do not force a hidden selection highlight.
- In `Details`, `FullRowSelect == false` means only the first/label selection region receives selected treatment; other cells retain their neutral/hover background.

### Stripes and observable caller colors

- `Striped == true` uses `SurfaceSecondary` for odd-index neutral rows only in `Details` and `List`.
- Selection and hover take precedence over stripes.
- Determine caller-color overrides from **observable effective values**, not `Color.Empty` sentinels:

```csharp
var effectiveBack = styleSource.BackColor;
var effectiveFore = styleSource.ForeColor;
var hasBackOverride = effectiveBack.ToArgb() != BackColor.ToArgb();
var hasForeOverride = effectiveFore.ToArgb() != ForeColor.ToArgb();
```

- For subitems, choose `styleSource` according to `UseItemStyleForSubItems`: use the item/first-subitem style when true, otherwise use the current subitem style.
- A differing effective caller background wins over a neutral stripe.
- If a caller explicitly assigns exactly the current inherited `BackColor`/`ForeColor`, V1 treats it as inherited because that intent is not observable through the public WinForms API.
- Do not use reflection to access internal WinForms `CustomBackColor`, `CustomForeColor`, or `CustomFont` flags.

### Hover

- Hover background is a subtle theme-derived highlight, not the full selected color.
- Hover never changes `SelectedIndices`, `FocusedItem`, `Checked`, or activation state.
- Determine hover with `HitTest(e.X, e.Y).Item` in every normal view.
- Track only the current hot item index needed to invalidate old/new bounds.
- Mouse leave clears hover and invalidates only the previous item when practical.

### Disabled and focus

- Disabled presentation uses `MutedText` and a subdued surface while preserving readable contrast.
- Draw a focus cue only when the control is focused, focus cues should be shown, and the native focused/selected item warrants it.
- Focus cue geometry follows native item/label bounds and `FullRowSelect` semantics.

### Details owner-draw strategy

- `OnDrawColumnHeader` renders themed headers.
- `OnDrawItem` performs **no framework painting when `View == View.Details`**. It may call `base.OnDrawItem(e)` so the inherited event contract remains intact.
- `OnDrawSubItem` owns framework painting for `Details`:
  - when `ColumnIndex == 0`, first paint the complete native row/base background once;
  - paint selection accent only across the full row when `FullRowSelect == true`;
  - with `FullRowSelect == false`, keep the row base neutral/striped/hovered and apply active selection only to the first-item/label region;
  - then paint the current cell contents.
- This structure is mandatory; do not regress to “framework background in `DrawItem`, text in `DrawSubItem`”.
- Demo/manual acceptance must repeatedly move the mouse across `Details` rows and verify subitem text never disappears.

### Details header

- Draw `ColumnHeader` background with `SurfaceSecondary`, text with theme `Text`, separators with theme `Border`.
- Respect `HeaderStyle`, `ColumnHeader.TextAlign`, display order, native bounds, and RTL.
- Do not add a sort glyph because the framework owns no sort state.

### Images and state images

- Read images from caller-owned `LargeImageList`, `SmallImageList`, and `StateImageList` according to native `View` and item indexes.
- Use native `ListViewItem.GetBounds(ItemBoundsPortion.Icon/Label/Entire)` and draw-event bounds as geometry anchors where available.
- Never scale or mutate caller `ImageList.ImageSize` automatically.
- Clip drawing and tolerate missing/invalid indexes without throwing.
- If a valid caller `StateImageList`/`StateImageIndex` applies, render it.
- Otherwise, when native checkbox presentation is valid for the current view and `CheckBoxes == true`, draw a lightweight themed checkbox glyph from `item.Checked` without storing checked state.
- Do not fabricate checkbox visuals in a view where native `ListView` does not support them.

### Text

- Use `TextRenderer`.
- Respect effective item/subitem font according to `UseItemStyleForSubItems`.
- Respect `ColumnHeader.TextAlign` in `Details`.
- Use `NoPrefix` so ampersands in data are not treated as mnemonics.
- Apply ellipsis or wrapping according to native view bounds.
- Mirror horizontal layout/text flags for RTL.

### Group headers

- V1 does not custom-paint group headers.
- Do not disable `ShowGroups`, remove groups, copy items out of groups, or use P/Invoke custom draw to simulate group support.
- Demo/docs explicitly state that group headers remain native/system-rendered and that group visibility itself follows native view restrictions.

---

## Internal Rendering Contract

Create focused, allocation-light internal helpers rather than placing state/color/layout decisions inline in `BootstrapListView`.

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

    internal static bool HasEffectiveColorOverride(Color candidate, Color inheritedColor);

    internal static BootstrapListViewItemPalette ResolvePalette(
        BootstrapTheme theme,
        BootstrapVariant variant,
        BootstrapListViewItemVisualState state,
        bool striped,
        bool hasCallerBackColor,
        Color callerBackColor,
        bool hasCallerForeColor,
        Color callerForeColor);
}
```

Required precedence:

```text
Disabled
  > SelectedActive
  > SelectedInactive
  > Hovered
  > observable caller color override
  > striped neutral background
  > normal theme surface
```

If `selected == true`, `controlFocused == false`, and `hideSelection == true`, resolve to neutral/hover instead of `SelectedInactive`.

`HasEffectiveColorOverride(candidate, inheritedColor)` compares observable effective ARGB values. It does not use `Color.Empty` as proof of caller intent.

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
- Helpers are pure and must not access handles, global theme state, `Items`, or image lists.
- Scale only framework-owned gaps/insets with `DpiScaler`; do not rescale native bounds a second time.
- Return `Rectangle.Empty` safely when geometry collapses.

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

- `BootstrapListView.cs`: native subclass, three public appearance properties, double buffering, owner-draw routing, theme/font lifecycle, `HitTest` hover invalidation, image/state-image drawing coordination, handle/DPI lifecycle.
- `BootstrapListViewRenderLogic.cs`: visual state, effective-color override detection, palette resolution only.
- `BootstrapListViewLayoutLogic.cs`: deterministic rectangle/text-flag helpers only.
- `BootstrapListViewTests.cs`: native contract, public API, lifecycle, view-mode, virtual-mode restrictions/setup order, hover, selection/check/image/group/label-edit behavior.
- Render/layout tests: pure state/palette/effective-color/geometry/RTL cases.
- `ListViewDemoForm.cs`: integrated visual/manual verification across normal views and native virtual-mode behavior.
- `docs/COMPONENTS.md`: supported contract, examples, view-specific native restrictions, V1 limitations.

---

## Task 1: Lock the Native-Backed Public Contract and Buffering with Failing Tests

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs`

**Interfaces:**
- Consumes: native `System.Windows.Forms.ListView`, existing `BootstrapVariant`.
- Produces: `BootstrapListView`, `Variant`, `Striped`, `HoverHighlight`, framework-owned `OwnerDraw = true`, protected double buffering enabled.

- [ ] **Step 1: Add STA/non-parallel default/native tests.**

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
    private sealed class TestBootstrapListView : BootstrapListView
    {
        public bool DoubleBufferedForTest => DoubleBuffered;
        public void RecreateHandleForTest() => RecreateHandle();
    }

    [Test]
    public void DefaultsMatchNativeBackedContract()
    {
        using var list = new TestBootstrapListView();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list, Is.InstanceOf<ListView>());
            Assert.That(list.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(list.Striped, Is.False);
            Assert.That(list.HoverHighlight, Is.True);
            Assert.That(list.OwnerDraw, Is.True);
            Assert.That(list.DoubleBufferedForTest, Is.True);
        }));
    }
}
```

- [ ] **Step 2: Add a reflection test that prevents API duplication/feature creep.**

```csharp
[Test]
public void V1DeclaresOnlyBootstrapAppearanceProperties()
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

- [ ] **Step 3: Add native ownership and presentation-only mutation tests.**

Create items, columns, groups, image lists, selection and checked state in normal mode. Change `Variant`, `Striped`, and `HoverHighlight`; assert the same caller objects/references and native state remain unchanged.

- [ ] **Step 4: Run focused tests and verify RED.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListView"
```

Expected: compile/test failure because `BootstrapListView` does not exist.

- [ ] **Step 5: Implement the minimal class skeleton including double buffering.**

```csharp
public class BootstrapListView : ListView
{
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private bool _striped;
    private bool _hoverHighlight = true;

    public BootstrapListView()
    {
        OwnerDraw = true;
        DoubleBuffered = true;
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

Add matching XML docs and `Description` attributes following existing controls.

- [ ] **Step 6: Run focused tests on both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListView"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapListView"

git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs
git commit -m "feat: add BootstrapListView native contract"
```

---

## Task 2: Add Pure Visual-State, Effective-Style, and Layout Logic

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewRenderLogic.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewLayoutLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewRenderLogicTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewLayoutLogicTests.cs`

**Interfaces:**
- Consumes: `BootstrapTheme`, `BootstrapVariant`, `ColorUtil`, native `View`/`HorizontalAlignment`.
- Produces: pure state/palette/effective-style/layout helpers consumed by all owner drawing.

- [ ] **Step 1: Write failing state-precedence tests.**

Cover neutral, hovered, selected-active, selected-inactive, hidden selection, and disabled precedence.

- [ ] **Step 2: Write failing stripe/effective-color tests.**

Required assertions include:

```csharp
Assert.That(
    BootstrapListViewRenderLogic.HasEffectiveColorOverride(Color.Red, Color.White),
    Is.True);
Assert.That(
    BootstrapListViewRenderLogic.HasEffectiveColorOverride(Color.White, Color.White),
    Is.False);
```

Also assert:

- odd `Details`/`List` rows stripe only when enabled;
- icon/tile modes never stripe;
- observable differing caller colors override neutral stripes;
- an effective color equal to the inherited list color is treated as inherited;
- selected active uses `BootstrapVariantColorResolver.Resolve(...)` and contrasting text;
- disabled uses muted text;
- inactive selection differs from active selection.

- [ ] **Step 3: Write failing geometry/text-flag tests.**

Cover row-oriented focus, icon/tile focus, zero/negative collapse, RTL, left/center/right header alignment, and tile text rectangles.

- [ ] **Step 4: Run pure tests and verify RED.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListViewRenderLogic|FullyQualifiedName~BootstrapListViewLayoutLogic"
```

- [ ] **Step 5: Implement the exact helper contracts in this plan.**

Do not use reflection to access internal WinForms style flags. Do not use `Color.Empty` as evidence that a caller did or did not explicitly assign a public color.

- [ ] **Step 6: Run pure tests on both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListViewRenderLogic|FullyQualifiedName~BootstrapListViewLayoutLogic"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapListViewRenderLogic|FullyQualifiedName~BootstrapListViewLayoutLogic"

git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewRenderLogic.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewLayoutLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewRenderLogicTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewLayoutLogicTests.cs
git commit -m "test: define BootstrapListView rendering contract"
```

---

## Task 3: Implement Theme/Font Lifecycle and Win32-Safe `Details` Owner Drawing

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs`

**Interfaces:**
- Consumes: Task 2 helpers, `BootstrapThemeManager`, `DpiScaler`.
- Produces: production `Details` rendering that never mixes framework row-background painting in `DrawItem` with framework text painting in `DrawSubItem`.

- [ ] **Step 1: Add failing runtime-theme/font/handle tests.**

Use the same ownership pattern as `BootstrapDataGridViewTests`: Light/Dark updates, caller font takeover, safe disposal, handle recreation, `OwnerDraw` remains true, and caller data survives.

- [ ] **Step 2: Add a test seam that proves `Details` framework painting is subitem-centric.**

Use a test subclass with protected-method wrappers and counters in private/protected test-only overrides. The test must prove that invoking the `Details` item path does not execute framework row background painting, while column-0 subitem drawing owns the row-background path. Do not add public diagnostic APIs to production code.

- [ ] **Step 3: Implement theme subscription and theme-font ownership.**

Mirror the existing framework lifecycle pattern:

```csharp
private bool _themeSubscribed;
private bool _settingThemeFont;
private bool _useThemeFont = true;
private Font? _themeFont;
```

Subscribe once, apply theme/font, release only framework-owned font, and unsubscribe exactly once in `Dispose(bool)`.

- [ ] **Step 4: Implement the mandatory `Details` event-routing workaround.**

```csharp
protected override void OnDrawItem(DrawListViewItemEventArgs e)
{
    if (View == View.Details)
    {
        // No framework Details painting here. This avoids the documented
        // Win32 DrawItem-without-DrawSubItem hover repaint bug.
        base.OnDrawItem(e);
        return;
    }

    DrawNonDetailsItem(e);
    base.OnDrawItem(e);
}
```

`OnDrawColumnHeader` renders the framework header and then preserves the inherited event contract.

`OnDrawSubItem` performs all framework `Details` row/cell painting and then preserves the inherited event contract.

- [ ] **Step 5: Paint the `Details` row/base background only from column 0.**

When `e.ColumnIndex == 0`:

1. resolve native item state;
2. obtain native complete row/item bounds;
3. paint the base neutral/stripe/hover/inactive-selection background once;
4. if `FullRowSelect == true` and selection is visible, apply selection to the full row;
5. if `FullRowSelect == false`, keep the full row base neutral/stripe/hover and reserve active selection treatment for the first label/item region.

For later columns, never repaint the full row background.

- [ ] **Step 6: Paint `Details` cell content and effective styles.**

For the first cell:

- resolve state image/native checkbox applicability;
- resolve small image;
- draw first-item text within native label/cell bounds.

For later cells:

- choose effective style source with `UseItemStyleForSubItems`;
- compare effective colors to `BackColor`/`ForeColor` using `HasEffectiveColorOverride`;
- respect `ColumnHeader.TextAlign`;
- use `NoPrefix | EndEllipsis | VerticalCenter` plus RTL flags;
- clip to the cell bounds.

- [ ] **Step 7: Implement state-image/checkbox rendering without state duplication.**

```text
valid caller StateImageList + StateImageIndex
    => draw caller state image
else native checkbox presentation valid for current view && CheckBoxes
    => draw themed glyph from item.Checked
else
    => no framework state glyph
```

Never write `item.Checked` during painting.

- [ ] **Step 8: Verify grid-line behavior before adding custom separators.**

Run both TFMs with `GridLines = true/false`. If native lines remain correct under the chosen owner-draw path, do not double-draw. If framework separators are required, draw theme `Border` lines only when `GridLines == true` and add a focused regression test.

- [ ] **Step 9: Add a manual regression entry for the Win32 hover repaint bug.**

The demo checklist must include:

```text
Details Win32 owner-draw regression:
- populate 3+ columns and 20+ rows
- move mouse repeatedly across every part of several rows
- move between selected, hovered, and neutral rows
- repeat with FullRowSelect true and false
- subitem text/images must never disappear or be covered by a late row background
```

- [ ] **Step 10: Run focused tests on both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListView"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapListView"

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
- Consumes: non-Details `DrawItem`, native `GetBounds(...)`, image lists, Task 2/3 palette rules.
- Produces: complete owner drawing for all five **normal** native `View` values.

- [ ] **Step 1: Add a five-view normal-mode smoke test.**

For each `View`, create a handle with items/images and render through the safest existing harness. Assert no exception and that `View`, item count, selection, and image-list references remain unchanged by Bootstrap drawing.

- [ ] **Step 2: Anchor geometry to native bounds.**

```csharp
var entire = item.GetBounds(ItemBoundsPortion.Entire);
var icon = item.GetBounds(ItemBoundsPortion.Icon);
var label = item.GetBounds(ItemBoundsPortion.Label);
```

Only calculate framework gaps around these native anchors.

- [ ] **Step 3: Implement `View.List`.**

Apply neutral/stripe/hover/selection, small image, single-line label, native scrolling/hit regions. Do not simulate group headers because native `List` does not display groups.

- [ ] **Step 4: Implement `View.SmallIcon` and `View.LargeIcon`.**

Use caller image lists and native icon/label bounds; no stripes; preserve native selection/focus geometry.

- [ ] **Step 5: Implement `View.Tile`.**

Render primary text plus visible subitem lines from `DrawItem`; `DrawSubItem` is not used. Use `TileSize`, native item/icon bounds, columns, RTL-aware tile layout, clipping, and `MutedText` for secondary lines unless an observable effective caller foreground differs from the inherited list foreground.

Do not draw framework checkbox fallback in `Tile` merely because `CheckBoxes` is true; follow native view capability.

- [ ] **Step 6: Add missing/invalid-image tests.**

Cover `ImageIndex == -1`, missing `ImageKey`, index beyond list count, absent image list, runtime image-list replacement, and caller disposal/replacement according to normal WinForms ownership.

- [ ] **Step 7: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListView"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapListView"

git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewLayoutLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewLayoutLogicTests.cs
git commit -m "feat: render all BootstrapListView view modes"
```

---

## Task 5: Preserve Hover, Virtual Mode, Groups, Label Editing, and Native Interaction

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs`

**Interfaces:**
- Consumes: native `HitTest`, focus, virtual, group, edit, selection APIs.
- Produces: interaction-safe presentation bookkeeping with no duplicate behavior model.

- [ ] **Step 1: Add hover-is-presentation-only tests for all five normal views.**

For `Details`, `List`, `SmallIcon`, `LargeIcon`, and `Tile`, use a test subclass to exercise mouse movement. Assert selection, checked state, `FocusedItem`, `HotTracking`, `HoverSelection`, and `Activation` are unchanged by framework hover bookkeeping.

- [ ] **Step 2: Implement hover with `HitTest`, never `GetItemAt`.**

Keep only:

```csharp
private int _hoveredItemIndex = -1;
```

On mouse move:

```csharp
var hit = HitTest(e.X, e.Y);
var index = hit.Item?.Index ?? -1;
```

Compare with `_hoveredItemIndex`, invalidate old/new native item rectangles, and never call selection/focus/check APIs. On mouse leave, clear the index. If virtual-size/handle changes make a stored index invalid, clear it and fall back to `Invalidate()` rather than caching item references.

- [ ] **Step 3: Add a failing test that locks virtual-mode setup order.**

Use the correct sequence:

```csharp
using var list = new BootstrapListView
{
    VirtualMode = true,
    View = View.Details
};

list.Columns.Add("Name", 180);
list.RetrieveVirtualItem += (_, e) =>
    e.Item = new ListViewItem($"Item {e.ItemIndex}");
list.VirtualListSize = 1000;
```

Assert positive `VirtualListSize` succeeds because `RetrieveVirtualItem` is already handled.

Also add a native-contract test demonstrating the unsupported ordering throws on the platform/runtime under test; Bootstrap must not hide that native exception.

- [ ] **Step 4: Add virtual-mode restriction/coercion tests.**

Cover:

```csharp
using var list = new BootstrapListView { View = View.Tile };
list.VirtualMode = true;
Assert.That(list.View, Is.EqualTo(View.LargeIcon));
```

Do not assign `VirtualListSize > 0` in that coercion test.

With a properly configured positive virtual list, assert Bootstrap code:

- does not access/enumerate normal `Items`, `SelectedItems`, or `CheckedItems` in paint/hover paths;
- preserves `VirtualListSize`;
- uses no shadow item collection;
- accepts `SelectedIndices`/`CheckedIndices` as the native index-based access path;
- does not force a coerced `LargeIcon` view back to `Tile`.

- [ ] **Step 5: Add runtime `VirtualListSize` change tests.**

Change `VirtualListSize` after the handler is installed and ensure framework hover/paint code does not retain stale virtual item references or out-of-range indexes.

- [ ] **Step 6: Add group-preservation/view-restriction tests.**

With `ShowGroups = true`, verify groups/items remain the same objects across theme/Variant changes. In a native view that displays groups, confirm Bootstrap does not disable/reassign them. Switch to `View.List` and assert Bootstrap does not invent group rendering or mutate membership to compensate for the native view restriction.

- [ ] **Step 7: Add label-edit/focus/native-keyboard preservation coverage.**

Verify `LabelEdit` survives theme/handle changes and Bootstrap does not raise edit events manually. Do not add `ProcessCmdKey`, `OnKeyDown`, or a message filter unless a reproducible owner-draw regression proves native behavior is broken.

Manual checks:

```text
Up / Down / Left / Right as appropriate to view
Home / End
PageUp / PageDown
Ctrl+click and Shift+click with MultiSelect
Space with CheckBoxes where supported
F2 / label edit when LabelEdit=true
Enter / ItemActivate according to Activation
Tab into and out of the ListView
Context menu keyboard invocation
```

- [ ] **Step 8: Run both TFMs and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~BootstrapListView"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter "FullyQualifiedName~BootstrapListView"

git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs
git commit -m "test: harden BootstrapListView native interaction"
```

---

## Task 6: DPI, RTL, Handle Recreation, Resource, Flicker, and Performance Hardening

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapListView.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapListViewLayoutLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapListViewLayoutLogicTests.cs`

**Interfaces:**
- Consumes: existing DPI/theme infrastructure.
- Produces: stable rendering across DPI/theme/RTL/lifecycle scenarios without leaks, flicker-prone framework choices, or state mutation.

- [ ] **Step 1: Add DPI tests for framework-owned measurements only.**

Use 96, 120, 144, 168, 192 DPI. Scale framework gaps/vector glyph metrics only; never scale native item/column bounds a second time and never resize caller image lists.

- [ ] **Step 2: Handle runtime DPI changes.**

Use the same shared-target DPI lifecycle hook as existing controls. Invalidate framework-owned cached measurements only. Do not change caller `TileSize`, column widths, image-list size, or native item positions.

- [ ] **Step 3: Add RTL tests.**

Test pure layout/text flags plus handle-level `RightToLeft = Yes` and `RightToLeftLayout = true` in `Details`, `List`, and `Tile` normal mode.

- [ ] **Step 4: Add handle-recreation tests.**

Assert caller items/columns/groups/selection/check state/image-list references survive according to native behavior, `OwnerDraw` remains enabled, `DoubleBufferedForTest` remains true, hover index does not become stale, and theme subscription is not duplicated.

- [ ] **Step 5: Audit GDI allocations and flicker behavior.**

Rules:

- `DoubleBuffered` remains true;
- use `TextRenderer` rather than allocating `StringFormat` per cell;
- dispose framework `Pen`/`Brush` objects immediately;
- do not allocate a new `Font` per item/tile line;
- do not clone images;
- do not build LINQ collections inside paint events;
- do not enumerate all items to render one item;
- do not reintroduce `Details` framework row-background painting in `OnDrawItem`.

- [ ] **Step 6: Add large-list performance smoke scenarios.**

```text
Normal Details: 5,000 items
Virtual Details: RetrieveVirtualItem handler installed first, then VirtualListSize = 100,000
```

The purpose is to catch O(n)-per-paint framework code, not to assert machine-specific timing thresholds.

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

```text
1. Details
   - 3+ columns / many subitems
   - FullRowSelect on/off
   - GridLines on/off
   - Striped on/off
   - CheckBoxes
   - image + state image
   - MultiSelect
   - dedicated repeated-hover regression scenario

2. List / SmallIcon / LargeIcon / Tile
   - normal-mode view switcher
   - long labels
   - image lists
   - selection + hover + focus
   - native view restrictions demonstrated rather than hidden

3. Groups
   - 2+ groups in a native group-capable view
   - switch to List to demonstrate that Bootstrap does not fake group rendering
   - explicit note that group headers remain native-rendered in V1

4. Virtual Mode
   - set VirtualMode = true
   - install RetrieveVirtualItem handler
   - then set VirtualListSize = 100000
   - show/document Tile -> LargeIcon native coercion
   - scroll/search smoke check

5. Runtime / lifecycle
   - Variant selector
   - Light/Dark through integrated theme switch
   - enabled/disabled
   - HideSelection on/off
   - RTL toggle
   - label editing
```

- [ ] **Step 2: Add the page to `MainForm.ConfigurePages()`.**

```csharp
AddPage(
    "ListView",
    "Native-backed Details/List/Icon/Tile views with selection, checks, images, groups, virtual mode, Bootstrap theming, and DPI-aware owner drawing.",
    () => new ListViewDemoForm());
```

- [ ] **Step 3: Document `BootstrapListView` in `docs/COMPONENTS.md`.**

Document:

- direct `ListView` inheritance;
- `Variant`, `Striped`, `HoverHighlight`;
- `DoubleBuffered` enabled internally;
- all five normal views;
- `Details` Win32 owner-draw workaround implemented internally;
- hover uses native `HitTest`;
- `Striped` only in `Details`/`List`;
- effective-style limitation: exact same-color explicit assignment is indistinguishable from inheritance;
- virtual-mode setup order: handler before positive size;
- virtual-mode invalid collections and index-based selection/check access;
- Tile-to-LargeIcon native coercion under virtual mode;
- native group/view restrictions;
- caller ownership of items/columns/groups/image lists;
- no V1 rounded border, loading/empty overlay, custom sort state, custom group-header renderer.

Normal-mode example:

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

Virtual-mode example with correct ordering:

```csharp
var list = new BootstrapListView
{
    Dock = DockStyle.Fill,
    View = View.Details,
    VirtualMode = true
};

list.Columns.Add("Name", 240);
list.RetrieveVirtualItem += (_, e) =>
    e.Item = new ListViewItem($"Item {e.ItemIndex}");
list.VirtualListSize = 100000;
```

- [ ] **Step 4: Execute the manual acceptance matrix.**

```text
[ ] Light theme
[ ] Dark theme
[ ] 96 DPI
[ ] 144 DPI / 150%
[ ] 192 DPI / 200% where available
[ ] Details
[ ] List
[ ] SmallIcon
[ ] LargeIcon
[ ] Tile normal mode
[ ] FullRowSelect true / false
[ ] GridLines true / false
[ ] CheckBoxes where native view supports them
[ ] StateImageList
[ ] MultiSelect Ctrl/Shift
[ ] Groups in supported view
[ ] List view does not fake groups
[ ] VirtualMode with handler installed before positive VirtualListSize
[ ] Virtual Tile coercion accepted, not reversed
[ ] LabelEdit
[ ] Keyboard navigation
[ ] Tab into/out of control
[ ] Hover works in every normal view without selection mutation
[ ] Details repeated-hover text-disappearance regression passes
[ ] HideSelection true / false
[ ] Disabled state
[ ] Runtime Variant switch
[ ] Runtime Light/Dark switch
[ ] RTL
[ ] Rapid normal-view switches
[ ] Form close/disposal with no exception
```

- [ ] **Step 5: Build demo and commit docs/demo.**

```powershell
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows -c Debug

git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ListViewDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs docs/COMPONENTS.md
git commit -m "docs: add BootstrapListView demo and guidance"
```

---

## Task 8: Final API Review, Compatibility Verification, and Definition of Done

**Files:**
- Review all files created/modified by Tasks 1–7.
- Modify only the concrete ListView files that contain an actual finding.

**Interfaces:**
- Consumes: complete implementation.
- Produces: release-ready `BootstrapListView` slice.

- [ ] **Step 1: Perform API-surface review.**

Confirm the only declared public V1 properties are:

```text
Variant
Striped
HoverHighlight
```

- [ ] **Step 2: Search for forbidden parallel state and corrected-plan regressions.**

Search for accidental additions/usages resembling:

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
GetItemAt(
Color.Empty // when used as caller-style intent sentinel
```

Also inspect `OnDrawItem` and verify the `Details` branch performs no framework background/content painting.

- [ ] **Step 3: Verify virtual-mode implementation mechanically.**

Confirm:

- no paint/hover code enumerates `Items`, `SelectedItems`, or `CheckedItems` while virtual mode is active;
- no framework code sets a positive `VirtualListSize` before the caller/demo has installed `RetrieveVirtualItem`;
- no framework code restores `Tile` after native virtual-mode coercion;
- hover stores only an index, not a virtual `ListViewItem` reference.

- [ ] **Step 4: Verify resource/lifecycle ownership.**

Confirm theme event unsubscription, framework-font disposal, no caller resource disposal, no per-item GDI leak, no duplicate self-subscription, no timer/message filter, and double buffering remains enabled after handle recreation.

- [ ] **Step 5: Run final builds/tests.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net8.0-windows -c Release
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net48 -c Release

dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows -c Release
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 -c Release

dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows -c Release
```

Expected: all commands succeed with no new warnings attributable to `BootstrapListView`.

- [ ] **Step 6: Review final diff.**

```powershell
git status --short
git diff --stat
git diff --check
```

Only intended `BootstrapListView` implementation/tests/demo/docs changes should remain.

- [ ] **Step 7: Commit a final hardening fix only if Task 8 changed concrete files.**

If Task 8 finds and fixes an implementation defect, stage exactly the corrected ListView files and commit:

```powershell
git commit -m "fix: finalize BootstrapListView compatibility"
```

Do not create an empty final commit.

---

## Definition of Done

`BootstrapListView` is complete when all of the following are true:

- [ ] `BootstrapListView` derives directly from native `ListView`.
- [ ] Both `net48` and `net8.0-windows` production builds succeed.
- [ ] Full test suite passes on both target frameworks.
- [ ] `OwnerDraw` renders Bootstrap-aware presentation without a parallel item/selection model.
- [ ] Protected `DoubleBuffered` is enabled to reduce owner-draw flicker.
- [ ] In `Details`, framework row backgrounds are never painted from `OnDrawItem`; custom row/cell painting is `OnDrawSubItem`-centric.
- [ ] Repeated mouse movement across `Details` rows never causes subitem text/images to disappear behind a late background repaint.
- [ ] `Details`, `List`, `SmallIcon`, `LargeIcon`, and `Tile` normal modes render and interact correctly.
- [ ] Hover uses `HitTest`, works across all normal views, and never mutates selection/focus/check/activation.
- [ ] `Variant`, `Striped`, and `HoverHighlight` match documented defaults and do not mutate native state.
- [ ] `Striped` is limited to neutral `Details`/`List` rows.
- [ ] Observable differing caller colors can override neutral stripes; exact same-color explicit assignments are documented as indistinguishable from inheritance.
- [ ] No reflection/internal WinForms style flags are used to infer caller color intent.
- [ ] Selection honors focus, `HideSelection`, and `FullRowSelect` semantics.
- [ ] Checkboxes/state images reflect native capability/state without duplicate checked-state storage.
- [ ] Caller image lists and images remain caller-owned.
- [ ] Virtual demo/tests install `RetrieveVirtualItem` before assigning positive `VirtualListSize`.
- [ ] Virtual paint/hover code does not depend on normal `Items`, `SelectedItems`, or `CheckedItems` collections.
- [ ] Native `SelectedIndices`/`CheckedIndices` remain the index-based virtual-mode access path.
- [ ] Native `Tile` -> `LargeIcon` coercion under virtual mode is accepted and never reversed by Bootstrap code.
- [ ] `VirtualMode` works without a shadow collection or O(n)-per-paint framework scan.
- [ ] Groups remain caller-owned and native view restrictions are respected; V1 native group-header rendering limitation is documented.
- [ ] Label editing, keyboard navigation, multi-selection, activation, context menus, and drag/drop remain native behavior.
- [ ] Light/Dark runtime switching updates presentation without changing data or interaction state.
- [ ] Caller-set fonts remain caller-owned after explicit override.
- [ ] DPI/RTL scenarios do not double-scale native geometry.
- [ ] Handle recreation does not duplicate subscriptions or preserve stale hover/virtual-item references.
- [ ] No GDI/event/timer resource leak is introduced.
- [ ] Integrated demo covers all normal views, native virtual-mode behavior, groups, checks/images, selection, theme, hover regression, and lifecycle scenarios.
- [ ] `docs/COMPONENTS.md` documents public API, native ownership, virtual-mode ordering/restrictions, effective-style limitation, and V1 exclusions.
- [ ] Final diff contains no unrelated changes.

## Implementation Notes for Reviewers

Review this control against the architectural principle already used by `BootstrapDataGridView` and planned for `BootstrapTreeView`: **native WinForms behavior is the source of truth; the framework owns presentation and shared infrastructure only.**

Highest-risk review areas:

1. the documented Win32 `Details` owner-draw bug: framework `OnDrawItem` must not repaint `Details` row backgrounds;
2. `FullRowSelect == false` selection geometry;
3. state-image/checkbox placement without changing checked state or fabricating unsupported Tile checkboxes;
4. `Tile` subitem layout through `DrawItem`;
5. hover through `HitTest` across all normal views;
6. virtual-mode ordering, invalid normal collections, and native Tile-to-LargeIcon coercion;
7. group/view restrictions and native group-header rendering;
8. effective caller color precedence without relying on inaccessible internal WinForms style flags;
9. avoiding O(n) work and per-item allocations in paint paths;
10. maintaining one implementation path compatible with both `net48` and `net8.0-windows`.