# BootstrapTreeView Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a production-ready `BootstrapTreeView` that keeps native WinForms `TreeView` data, selection, expansion, editing, keyboard, drag/drop, image-list, checkbox, accessibility, hit-testing, and event semantics authoritative while replacing node presentation with Bootstrap-inspired, theme-aware, DPI-aware rendering.

**Architecture:** `BootstrapTreeView` derives directly from `System.Windows.Forms.TreeView`. The native control remains the tree/data/interaction/hit-test engine. The framework initializes `DrawMode = OwnerDrawAll` and owns only painting, hover bookkeeping needed for painting, DPI-scaled framework geometry, theme/font synchronization, and only an evidence-gated full-row hit-area correction if a real native behavior gap is proven by baseline tests. Rendering decisions and geometry live in small internal helpers so they can be tested without duplicating TreeView behavior. Framework-drawn expander/state-image/image rectangles must be validated against native `TreeView.HitTest` regions so presentation cannot drift away from native interaction semantics. No custom tree model, virtualization layer, popup/window infrastructure, or replacement accessibility tree is introduced.

**Tech Stack:** C#, WinForms, `net48;net8.0-windows`, existing `BootstrapThemeManager`, `BootstrapVariant`, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`, NUnit, integrated demo application.

---

## Global Constraints

- Preserve the repository dependency direction documented in `AGENTS.md` and `docs/ARCHITECTURE.md`: the control may depend on Theme/Rendering/Compatibility, but shared infrastructure must not depend on `BootstrapTreeView`.
- Keep `System.Windows.Forms.TreeView` as the behavioral source of truth. Do **not** introduce parallel `Nodes`, `SelectedNode`, `CheckedNodes`, expansion state, focus model, keyboard engine, label editor, drag/drop engine, point-to-node engine, or accessibility object model.
- Preserve inherited native APIs, including at minimum: `Nodes`, `SelectedNode`, `CheckBoxes`, `ImageList`, `StateImageList`, `ImageIndex`, `SelectedImageIndex`, `Indent`, `ItemHeight`, `ShowLines`, `ShowPlusMinus`, `ShowRootLines`, `FullRowSelect`, `HideSelection`, `HotTracking`, `LabelEdit`, `Scrollable`, `PathSeparator`, `NodeMouseClick`, `DrawNode`, `BeforeSelect`/`AfterSelect`, `BeforeExpand`/`AfterExpand`, `BeforeCollapse`/`AfterCollapse`, `BeforeCheck`/`AfterCheck`, `ItemDrag`, and drag/drop events.
- Do not duplicate inherited appearance/layout properties with aliases such as `NodeHeight` for `ItemHeight` or `NodeIndent` for `Indent` unless implementation evidence later proves native APIs insufficient. V1 should remain a small public API.
- Do not add custom data binding, lazy-loading, async child loading, tri-state checkboxes, filtering/search, loading/empty overlays, drag/drop policy, context menus, per-node command buttons, or a virtualized tree model in this plan.
- Do not add `BorderRadius` in V1. A native `TreeView` owns its HWND, scrolling, and non-client behavior; rounded clipping/non-client painting would create disproportionate lifecycle and scrollbar complexity for a first implementation.
- Do not add FontAwesome.Sharp, SVG, SkiaSharp, or another icon dependency. Tree node images continue through native `ImageList`/`StateImageList`; the small expand/collapse indicator and default checkbox marks are framework-owned vector primitives.
- Do not dispose caller-owned `ImageList`, `StateImageList`, `TreeNode.NodeFont`, or images.
- Avoid allocations in steady-state node painting. Do not create a new `Font`, `Pen`, `Brush`, `GraphicsPath`, or collection per node unless wrapped in deterministic disposal and no reusable/pure alternative exists.
- All semantic colors come from `BootstrapThemeManager.CurrentTheme`; do not hardcode Bootstrap hex values or Windows system colors for framework-owned presentation.
- All framework-owned pixel geometry must scale from the current DPI through `DpiScaler`; do not use `Math.Clamp` or other APIs unavailable on `net48`.
- Native `TreeView.HitTest`/`TreeViewHitTestLocations` is the interaction-geometry oracle. A framework-drawn glyph must not imply a clickable area that native hit testing reports somewhere else.
- `DrawMode` is an inherited public non-virtual property. The framework sets it to `OwnerDrawAll` by default, but V1 must not claim that consumer assignments can be technically prevented. Assigning another `DrawMode` value is unsupported because it disables or invalidates Bootstrap rendering assumptions.
- Do not shadow the inherited `DrawMode` property solely to pretend it is immutable or to hide it from IntelliSense/designer. If a future designer-only filtering mechanism can hide it without altering the public contract, that may be considered separately.
- Theme subscription must be paired with deterministic unsubscribe in `Dispose(bool)` and must remain safe after handle recreation.
- Tests that instantiate WinForms controls must run STA and non-parallel when they mutate global theme state.
- Build and test both target frameworks before completion.

---

## Reference Behavior and WinForms Adaptation

Bootstrap 5 does not define an official TreeView component. Therefore the design target is **Bootstrap visual language applied to the native WinForms TreeView contract**, not a web component port.

The implementation should follow these WinForms rules exactly:

1. `TreeViewDrawMode.OwnerDrawAll` means the framework must draw the complete node presentation: text, node image, state image/checkbox, expand-collapse indicator, and connector lines when enabled.
2. Under `OwnerDrawAll`, `DrawTreeNodeEventArgs.Bounds` spans the row width, while `TreeNode.Bounds` represents the native label hit region. Use native row/label geometry as anchors instead of inventing a separate indentation engine.
3. Native `FullRowSelect` is ignored when `ShowLines == true`. The Bootstrap renderer must preserve that contract rather than forcing full-row selection in configurations where WinForms does not.
4. Root expand/collapse indicators are only shown when native `ShowRootLines == true`; otherwise `ShowPlusMinus` has no visible root indicator. Preserve that behavior.
5. When `CheckBoxes == true` and `StateImageList` is assigned, native TreeView semantics use the first two state images for unchecked/checked states. Preserve that contract rather than replacing those images with framework checkbox art.
6. `StateImageList.ImageSize` must not be treated like normal `ImageList.ImageSize` for layout. Native TreeView state-image display geometry has its own behavior; framework state-image placement must align with the native `StateImage` hit-test region and must not grow merely because a caller changes `StateImageList.ImageSize`.
7. Changing `CheckBoxes` at runtime can recreate the native TreeView handle and collapse nodes except the selected node. `BootstrapTreeView` must not attempt to hide or reverse that native side effect with a parallel expansion cache.
8. `Scrollable`, `ImageIndex`, and `SelectedImageIndex` can also participate in native handle recreation/runtime native-state changes. Lifecycle tests must explicitly cover these properties rather than using a vague “other properties” bucket.
9. Owner drawing must not replace native keyboard behavior. Arrow keys, Home/End, PageUp/PageDown, `+`/`-`, `*`, Space for checks where supported by native behavior, F2 label edit, and standard selection events remain native responsibilities.
10. Owner drawing must not replace native mouse hit testing. Expander, state-image/checkbox, node-image, label, and right-of-label behavior must be derived from or validated against `TreeView.HitTest`.
11. The inherited `DrawNode` event must remain observable and be raised exactly once for a node draw. `BootstrapTreeView.OnDrawNode` must call `base.OnDrawNode(e)` exactly once. The event is not a V1 renderer-replacement hook: framework rendering remains authoritative and consumer `e.DrawDefault` requests must not switch the control back to native painting.
12. `TreeNode.NodeFont` can be larger than the row height. V1 preserves `NodeFont` but does not silently resize the entire tree per node; callers using unusually large node fonts may need to increase inherited `ItemHeight`.
13. Drag initiation and drag/drop remain native. Framework hover/full-row adaptation must not create duplicate selection or `ItemDrag` behavior.

Useful implementation references:

- `TreeView.DrawNode`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.drawnode
- `TreeView.DrawMode`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.drawmode
- `TreeView.HitTest`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.hittest
- `TreeViewHitTestLocations`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeviewhittestlocations
- `TreeView.FullRowSelect`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.fullrowselect
- `TreeView.ShowPlusMinus`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.showplusminus
- `TreeView.ShowRootLines`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.showrootlines
- `TreeView.CheckBoxes`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.checkboxes
- `TreeView.StateImageList`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.stateimagelist
- `TreeNode.NodeFont`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treenode.nodefont
- `TreeView.Scrollable`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.scrollable
- `TreeView.ImageIndex`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.imageindex
- `TreeView.SelectedImageIndex`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.selectedimageindex

---

## Public Contract to Implement

Keep the V1 public surface intentionally small:

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public class BootstrapTreeView : TreeView
{
    [Category("Appearance")]
    [Description("Specifies the semantic color variant used for active tree presentation.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant { get; set; }
}
```

### Public API rules

- Default `Variant` is `BootstrapVariant.Primary`.
- Changing `Variant` invalidates the control and does not mutate selection, checked state, expanded state, node data, or focus.
- Constructor/default initialization sets inherited `DrawMode = TreeViewDrawMode.OwnerDrawAll`.
- `DrawMode` remains the inherited public property because it cannot be overridden safely. Bootstrap rendering is supported only while it remains `OwnerDrawAll`; assigning a different value is explicitly unsupported rather than being silently “corrected” by a shadow property.
- Do not introduce a new public render-mode abstraction in V1.
- Existing `TreeNode.ForeColor`, `TreeNode.BackColor`, and `TreeNode.NodeFont` overrides remain meaningful for neutral nodes. Selection/disabled states may override colors when needed for legibility and state visibility.
- The inherited `DrawNode` event remains observable and fires once per framework draw, but V1 does not promise that `DrawNode` subscribers can replace framework painting or enable `DrawDefault`.
- The control should not add custom selection, expansion, checkbox, drag/drop, or renderer events; use the inherited TreeView events.

---

## Visual Contract

### Neutral node

- Background: theme surface, or `TreeNode.BackColor` when explicitly provided.
- Foreground: theme text, or `TreeNode.ForeColor` when explicitly provided.
- Font: `TreeNode.NodeFont` when supplied; otherwise the control font.
- Connector lines: theme border/muted-border color when `ShowLines`/`ShowRootLines` require them.
- Expand/collapse indicator: small framework vector chevron/triangle in muted text color.

### Hovered node

- Apply a subtle theme-derived hover surface only when the node is enabled through the parent control and is not currently rendered as selected.
- Hover is presentation only; it must not select or focus a node.
- `HotTracking` remains a native property with its native interaction semantics; framework hover bookkeeping must not mutate or substitute the native `HotTracking` contract.
- Moving between nodes invalidates only the old/new visible row bounds where practical rather than invalidating the whole control.

### Selected node

- Use `BootstrapVariantColorResolver.Resolve(theme.Colors, Variant)` as the active background.
- Use `ColorUtil.GetContrastingTextColor(...)` for selected text/glyph contrast.
- Respect native selection visibility: if the control does not have focus and `HideSelection == true`, render the node as unselected.
- If native full-row selection is effective (`FullRowSelect == true && ShowLines == false`), fill the visible row width. Otherwise keep the selected background scoped to the native content/label region.
- Full-row **painting** does not automatically imply custom full-row **mouse selection**. Native behavior is tested first; a correction layer is added only if the baseline proves a real mismatch.
- Draw a theme-derived focus cue only for the selected node when the control has keyboard focus and `ShowFocusCues` is true.

### Disabled control

- Use muted theme text/glyph colors and a theme surface background.
- Do not implement per-node disabled state in V1 because `TreeNode` has no native `Enabled` contract.

### Images and checkboxes

- Respect `ImageList`, node image key/index, selected image key/index, and TreeView fallback image key/index semantics.
- Normal/selected node images use the caller image and preserve aspect ratio inside the resolved node-image slot; do not dispose or clone caller-owned images.
- Respect `StateImageList` and `TreeNode.StateImageKey`/`StateImageIndex` when `CheckBoxes == false`.
- State-image layout is independent from `StateImageList.ImageSize`: use native TreeView state-image/hit-test geometry as the presentation anchor, and do not expand the state slot simply because the caller changes `StateImageList.ImageSize`.
- When `CheckBoxes == true`:
  - if `StateImageList` provides the native checked/unchecked images, render those images according to native semantics;
  - otherwise render a Bootstrap-themed checkbox visual from `TreeNode.Checked` without owning a second checked-state model.
- The center of framework-drawn expander/state-image/node-image geometry must resolve to the corresponding native `TreeViewHitTestLocations` region in handle-backed parity tests wherever that element is interactive.

### Font and row-height ownership

- The framework manages a Bootstrap-friendly default `ItemHeight` only while the inherited property remains framework-owned.
- Compute the framework default from the active theme body font plus DPI-scaled vertical padding, bounded so native TreeView requirements are respected.
- Track the last framework-assigned `ItemHeight`. If the caller changes inherited `ItemHeight` to another value, stop overwriting it on later theme/DPI changes.
- Do **not** framework-manage `Indent` in V1; preserve the native/caller value because native indentation is part of hit-test geometry.
- `TreeNode.NodeFont` remains caller-owned and may exceed the managed row height. Document that callers must increase `ItemHeight` when intentionally using unusually large per-node fonts.

### Label editing

- When native label editing starts, allow the native edit control to own editing behavior and text input.
- Theme the native edit child opportunistically only if it can be done without subclassing/replacing the native label-edit lifecycle. Functional native label editing takes precedence over decorative parity.

---

## Internal Rendering Contract

Create pure/internal helpers instead of placing all state logic in `OnDrawNode`.

### `BootstrapTreeViewRenderLogic`

Responsibilities:

- Resolve node state into semantic colors.
- Decide whether selection is visibly active using `selected`, `focused`, and `HideSelection` inputs.
- Decide whether full-row selection is effective using `FullRowSelect && !ShowLines`.
- Resolve selected text contrast from the semantic variant.
- Resolve neutral/hover/disabled text and surface colors from theme tokens.
- Resolve whether root/non-root expander indicators are visible from `ShowPlusMinus`, `ShowRootLines`, node level, and child count.
- Resolve whether native state images or framework checkbox art should be used.

Prefer immutable internal structs such as:

```csharp
internal readonly struct BootstrapTreeViewVisualState
{
    public BootstrapTreeViewVisualState(
        bool enabled,
        bool selected,
        bool hovered,
        bool focused,
        bool hideSelection,
        bool fullRowSelect,
        bool showLines)
    {
        // Assign fields/properties only.
    }
}
```

Do not depend on a live `TreeView` from pure palette tests when primitive inputs are enough.

### `BootstrapTreeViewLayout`

Responsibilities:

- Accept native row bounds and `TreeNode.Bounds` as anchors.
- Produce rectangles for row background, focus cue, expander, state/checkbox image, node image, and text.
- Treat normal node-image and state-image slots as distinct concepts; do not feed `StateImageList.ImageSize` into the normal image-size path.
- Mirror framework-owned geometry for right-to-left layouts while continuing to use native node bounds as the primary anchor.
- Scale only framework-owned gaps/glyph sizes via `DpiScaler`.
- Clip rectangles to the client area.
- Keep enough separation that checkbox/state image, node image, and text never overlap at 96/120/144/168/192 DPI.
- Never infer tree hierarchy from text width or custom collections; hierarchy level comes from the native node.
- Pure layout tests prove deterministic rectangle math, but handle-backed tests remain authoritative for validating the rectangles against native `HitTest` geometry.

Suggested internal immutable result:

```csharp
internal readonly struct BootstrapTreeViewNodeLayout
{
    public Rectangle RowBounds { get; }
    public Rectangle SelectionBounds { get; }
    public Rectangle ExpanderBounds { get; }
    public Rectangle StateImageBounds { get; }
    public Rectangle NodeImageBounds { get; }
    public Rectangle TextBounds { get; }
    public Rectangle FocusBounds { get; }
}
```

---

## File Structure and Responsibilities

### New files

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTreeView.cs`
  - native TreeView subclass, public `Variant`, owner-draw orchestration, theme/font lifecycle, hover state, native hit-test parity checks/adaptation, and evidence-gated full-row correction only if required.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapTreeViewRenderLogic.cs`
  - pure visual-state and palette decisions.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapTreeViewLayout.cs`
  - DPI/RTL-aware node geometry derived from native bounds.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`
  - public/native contract, theme lifecycle, hit-test parity, interaction, drag/drop, image/checkbox, label-edit and handle-recreation tests.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewRenderLogicTests.cs`
  - palette/state decision tests.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewLayoutTests.cs`
  - geometry/DPI/RTL tests.
- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/TreeViewDemoForm.cs`
  - integrated manual verification page.

### Existing files to modify

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
  - register the TreeView demo page.
- `docs/COMPONENTS.md`
  - document `BootstrapTreeView` scope, native-backed contract, and public API.
- `docs/TESTING.md`
  - add TreeView-specific interaction/owner-draw verification expectations if the current test matrix is component-oriented.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Hardening/Phase15HardeningTests.cs`
  - include `BootstrapTreeView` in representative theme-switch/disposal coverage.
- `README.md`
  - add `BootstrapTreeView` to the supported controls/component table if that table is maintained there.
- `CHANGELOG.md`
  - add the component under the current unreleased section only if repository convention requires unreleased feature entries during implementation.

---

# Task 1: Lock the Native-Backed Contract with Failing Tests

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTreeView.cs`

- [ ] Add an STA/non-parallel NUnit fixture for `BootstrapTreeView`.
- [ ] Add a failing test asserting `BootstrapTreeView` derives from `TreeView` and exposes the inherited `Nodes` collection without a shadow property.
- [ ] Add a failing test asserting default `Variant == BootstrapVariant.Primary`.
- [ ] Add a failing test asserting constructor/default framework drawing uses `TreeViewDrawMode.OwnerDrawAll`.
- [ ] Add a reflection test proving `BootstrapTreeView` does **not** declare/shadow a second public `DrawMode` property; the inherited setter remains part of the native API even though non-`OwnerDrawAll` values are unsupported by Bootstrap rendering.
- [ ] Add tests proving native properties still round-trip without framework state duplication: `SelectedNode`, `CheckBoxes`, `ImageList`, `StateImageList`, `FullRowSelect`, `ShowLines`, `ShowPlusMinus`, `ShowRootLines`, `HotTracking`, `LabelEdit`, `ItemHeight`, `Indent`, and `Scrollable`.
- [ ] Add a test proving changing `Variant` leaves `SelectedNode`, `Checked`, and `IsExpanded` unchanged.
- [ ] Add a test proving the class does not declare public aliases named `NodeHeight`, `NodeIndent`, `CheckedNodes`, `ExpandedNodes`, `Loading`, or `EmptyStateText`.
- [ ] Implement the smallest class skeleton and `Variant` property required to make these tests pass.
- [ ] Initialize inherited `DrawMode` to `OwnerDrawAll`; do not add a shadow setter that claims to make the property immutable.
- [ ] Add `Category`, `Description`, and `DefaultValue` metadata matching existing control conventions.
- [ ] Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter BootstrapTreeViewTests
```

- [ ] Run the same filtered fixture for `net48`.
- [ ] Commit the contract separately, e.g. `feat: add BootstrapTreeView native-backed contract`.

---

# Task 2: Implement and Test Pure Visual-State Resolution

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapTreeViewRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewRenderLogicTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTreeView.cs`

- [ ] Write failing tests for neutral Light/Dark theme palette resolution.
- [ ] Write failing tests for every `BootstrapVariant` selection background and contrasting selected text.
- [ ] Write tests proving `HideSelection == true` suppresses selected presentation when the control is unfocused.
- [ ] Write tests proving `HideSelection == false` keeps selected presentation visible when unfocused.
- [ ] Write tests proving full-row selection is effective only when `FullRowSelect == true && ShowLines == false`.
- [ ] Write tests proving disabled presentation wins over hover and uses muted semantic tokens.
- [ ] Write tests for expander visibility:
  - no children => no expander;
  - `ShowPlusMinus == false` => no expander;
  - root node + `ShowRootLines == false` => no root expander;
  - child node with children + `ShowPlusMinus == true` => expander visible.
- [ ] Write tests for checkbox/state-image mode selection, including `CheckBoxes == true` + two-entry `StateImageList` behavior.
- [ ] Implement pure state/palette helpers with no control handles and no GDI object allocation.
- [ ] Reuse `BootstrapVariantColorResolver` and `ColorUtil`; do not add duplicate semantic color maps.
- [ ] Run focused render-logic tests on both TFMs.
- [ ] Commit, e.g. `feat: add TreeView visual state resolution`.

---

# Task 3: Implement DPI- and RTL-Aware Node Layout

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapTreeViewLayout.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewLayoutTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`

- [ ] Define layout inputs using primitive/native geometry: client bounds, `DrawTreeNodeEventArgs.Bounds`, `TreeNode.Bounds`, node level, DPI, RTL flags, effective full-row selection, expander/state-image/node-image presence, normal image size, and a distinct native state-image slot input.
- [ ] Do **not** use `StateImageList.ImageSize` as the state-image display-slot size.
- [ ] Add 96 DPI tests establishing non-overlapping expander/state image/node image/text rectangles.
- [ ] Add 120/144/168/192 DPI tests proving framework-owned glyphs and gaps scale monotonically and remain inside the visible row.
- [ ] Add tests for rows narrower than expected: rectangles clamp to non-negative sizes and text bounds may become empty without throwing.
- [ ] Add tests for horizontal scrolling/native label bounds shifted toward or outside the client area; layout must clip rather than recalculate hierarchy from scratch.
- [ ] Add RTL tests proving framework-owned slots mirror while selection/focus bounds remain correct.
- [ ] Add tests for `FullRowSelect` effective vs ineffective layouts.
- [ ] Add a regression layout test with a `StateImageList` configured to a non-default `ImageSize`; state-image display geometry must remain based on the native state slot rather than expand to the configured image-list size.
- [ ] Add handle-backed parity tests at representative LTR/RTL, horizontal-scroll, checkbox/state-image/image-list combinations: the center (or another stable interior point) of framework `ExpanderBounds`, `StateImageBounds`, and `NodeImageBounds` must resolve through native `HitTest` to `PlusMinus`, `StateImage`, and `Image` respectively when those elements are present.
- [ ] Repeat the hit-test parity matrix at 96 DPI and at least one high-DPI scale; final manual verification covers the full DPI matrix.
- [ ] Implement layout using `DpiScaler` and existing compatibility-safe math.
- [ ] Keep pure layout independent of `Graphics`, `TreeView`, and `TreeNode`; use separate handle-backed tests for native parity.
- [ ] If a proposed custom rectangle cannot be made to overlap the corresponding native hit region without inventing a second interaction engine, adjust the visual geometry to native behavior instead of adding custom click handling.
- [ ] Run layout and parity tests on both TFMs.
- [ ] Commit, e.g. `feat: add TreeView owner-draw layout`.

---

# Task 4: Build the Owner-Draw Node Pipeline

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTreeView.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`

- [ ] Add a protected test probe or narrowly scoped internal seam so tests can render a node into a bitmap without exposing a new public renderer API.
- [ ] Write a failing smoke test that creates a handle, adds visible nodes, renders to a bitmap, and completes without exceptions/GDI leaks in Light and Dark modes.
- [ ] Override `OnDrawNode` and call the pure render/layout helpers.
- [ ] Preserve the inherited `DrawNode` event: call `base.OnDrawNode(e)` exactly once per draw and add a test proving one event notification, not zero or duplicates.
- [ ] Treat inherited `DrawNode` as observable rather than a V1 renderer-replacement hook. Ensure consumer `e.DrawDefault = true` cannot switch the Bootstrap control back to native drawing; framework rendering remains authoritative.
- [ ] Fill only the required background region: full visible row when native full-row selection is effective, otherwise the node content/label region.
- [ ] Draw node text with `TextRenderer` and flags that match native single-line tree behavior, ellipsis, no-prefix semantics where appropriate, and RTL.
- [ ] Respect `TreeNode.NodeFont`, `TreeNode.ForeColor`, and `TreeNode.BackColor` in neutral state.
- [ ] Add a handle-backed rendering test proving a normal theme font fits the framework-managed default `ItemHeight` without clipping at representative DPI values.
- [ ] Add a regression test documenting the supported behavior for an intentionally oversized `TreeNode.NodeFont`: the renderer must not throw or dispose the font, and callers may need to raise `ItemHeight` to avoid native row clipping.
- [ ] Draw selection text/background using `Variant` and a contrasting foreground.
- [ ] Draw focus cue only for the visible selected node when focus cues should be shown.
- [ ] Ensure drawing handles an empty node text, zero-size bounds, hidden nodes, partially clipped rows, and disposal without throwing.
- [ ] Do not allow `e.DrawDefault = true` to become the normal render path in `OwnerDrawAll`; all desired node elements must be deterministic under framework rendering.
- [ ] Keep local GDI resources inside `using` scopes and prefer existing rendering helpers where available.
- [ ] Run focused tests on both TFMs.
- [ ] Commit, e.g. `feat: owner draw BootstrapTreeView nodes`.

---

# Task 5: Render Native Tree Structure and Expand/Collapse Indicators

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTreeView.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapTreeViewLayout.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewLayoutTests.cs`

- [ ] Add tests for expanded vs collapsed glyph orientation.
- [ ] Add tests that root expanders obey `ShowRootLines` and non-root expanders obey `ShowPlusMinus`.
- [ ] Add tests that leaf nodes never draw an expander.
- [ ] Add tests for connector-line geometry with `ShowLines`, including ancestor continuation lines and last-child termination.
- [ ] Implement small framework vector expand/collapse glyphs using theme/DPI-aware strokes; do not depend on icon packages.
- [ ] Implement connector-line painting from native hierarchy (`Parent`, `PrevNode`, `NextNode`, `Level`) without storing a parallel hierarchy.
- [ ] Use theme border/muted color for connector lines and expander neutral state; use selected foreground when the expander sits on selected background.
- [ ] Add handle-backed tests proving the rendered expander interior lies inside the native `PlusMinus` hit-test region for root/non-root, LTR/RTL, horizontal-scroll, and representative DPI configurations.
- [ ] Confirm native mouse click/double-click on the plus/minus region expands/collapses exactly once without framework-synthesized expand/collapse.
- [ ] Confirm programmatic `Expand`, `Collapse`, `ExpandAll`, and `CollapseAll` still raise native events and repaint correctly.
- [ ] Run both target frameworks.
- [ ] Commit, e.g. `feat: render TreeView hierarchy indicators`.

---

# Task 6: Preserve ImageList, StateImageList, and Checkbox Semantics

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTreeView.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapTreeViewLayout.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`

- [ ] Add tests for `ImageList` normal image resolution through node `ImageKey`/`ImageIndex` and TreeView fallback image values.
- [ ] Add tests for selected image resolution through `SelectedImageKey`/`SelectedImageIndex`.
- [ ] Add tests proving caller-owned `ImageList` survives TreeView disposal.
- [ ] Add tests for `StateImageList` + `StateImageKey`/`StateImageIndex` when `CheckBoxes == false`.
- [ ] Add tests for `CheckBoxes == true` without a `StateImageList`: renderer reads only `TreeNode.Checked` and draws framework checkbox art.
- [ ] Add tests for `CheckBoxes == true` with a state list: unchecked uses list index 0 and checked uses list index 1, matching native WinForms semantics.
- [ ] Add a regression test assigning a deliberately non-default `StateImageList.ImageSize`; the visual state-image/checkbox slot must still align with native `StateImage` hit testing and must not simply adopt the configured image-list size.
- [ ] Add tests proving caller-owned `StateImageList` survives control disposal.
- [ ] Add interaction tests that clicking the native state-image/checkbox hit region changes `TreeNode.Checked` and raises `BeforeCheck`/`AfterCheck` once.
- [ ] Add handle-backed parity tests proving framework `StateImageBounds` and `NodeImageBounds` visually overlap the native `StateImage` and `Image` hit regions across LTR/RTL and representative DPI configurations.
- [ ] Implement image resolution as small private/internal functions; do not clone images into framework-owned lists.
- [ ] Draw normal node images with appropriate interpolation/pixel-offset settings only when scaling is necessary; preserve caller image aspect ratio.
- [ ] Render state images within the native state-image slot; do not derive that slot from `StateImageList.ImageSize`.
- [ ] Implement framework checkbox border/fill/checkmark from theme tokens and DPI-scaled primitives only when no caller state image owns the checkbox visual, and keep that checkbox visual inside the native `StateImage` hit region.
- [ ] Verify changing `CheckBoxes` at runtime does not trigger framework exceptions across native handle recreation; do not restore collapsed nodes automatically.
- [ ] Run both TFMs.
- [ ] Commit, e.g. `feat: preserve TreeView image and checkbox semantics`.

---

# Task 7: Add Hover, Selection, Focus, and Evidence-Gated Full-Row Hit Behavior

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTreeView.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`

- [ ] Add tests for hover transitions using `GetNodeAt`/native hit testing: old hot node is cleared, new node is stored, MouseLeave clears hover.
- [ ] Add tests proving framework hover does not mutate `SelectedNode` and does not change inherited `HotTracking`.
- [ ] Add tests for focus enter/leave repaint behavior with `HideSelection` true and false.
- [ ] Before writing any full-row correction, create a handle-backed **native baseline** test using a plain `TreeView` configured with the same `DrawMode = OwnerDrawAll`, `FullRowSelect = true`, and `ShowLines = false`. Record whether clicking a native `RightOfLabel`/row point already selects the expected node and which native events fire.
- [ ] Run the same baseline under both `net48` and `net8.0-windows` test targets. If native behavior already matches the visual contract, implement **no** full-row mouse correction.
- [ ] Only if the baseline proves a real mismatch, add the smallest correction necessary and lock it behind failing regression tests that demonstrate the gap.
- [ ] Any correction must use `TreeView.HitTest` and `TreeViewHitTestLocations` as the geometry source; do not implement a second arbitrary point-to-node search.
- [ ] Any correction must act only on the exact gap proven by the baseline and must not steal clicks from native `PlusMinus`, `StateImage`, `Image`, `Label`, or label-edit regions.
- [ ] Any correction must preserve event counts: one selection sequence for a click, no duplicate `NodeMouseClick`, and no duplicate expand/check behavior.
- [ ] Ensure a blank area below the last visible node does not change selection.
- [ ] Ensure right-click does not silently force selection unless that is already the intended native behavior for the proven gap.
- [ ] Ensure double-click still expands/collapses according to native TreeView rules and is not processed twice by any correction layer.
- [ ] Add a drag-initiation regression test proving mouse handling does not suppress or duplicate `ItemDrag` after selecting/dragging a node.
- [ ] Invalidate only affected row bounds for hover/focus changes when safe; fall back to full invalidation for theme/DPI changes.
- [ ] Run both TFMs.
- [ ] Commit, e.g. `feat: add TreeView interactive presentation states`.

---

# Task 8: Theme, Font, DPI, Handle-Recreation, and Disposal Lifecycle

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTreeView.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Hardening/Phase15HardeningTests.cs`

- [ ] Follow the existing `BootstrapDataGridView` theme-font pattern: use theme body font until the caller explicitly assigns `Font`, then preserve caller ownership.
- [ ] Add tests proving a runtime Light/Dark switch updates `BackColor`, `ForeColor`, and rendered node palette without replacing `Nodes` or selection.
- [ ] Add tests proving a caller-assigned `Font` remains after later theme changes.
- [ ] Add tests proving framework-owned theme fonts are disposed when replaced/disposed.
- [ ] Implement the explicit V1 `ItemHeight` ownership policy: assign a theme/DPI-derived default, remember the last framework-assigned value, refresh it on later theme/DPI changes only while the current value still equals the framework-owned value, and permanently stop overwriting it once the caller assigns another value.
- [ ] Do not apply the same ownership mechanism to `Indent`; keep inherited/native indentation caller-controlled in V1.
- [ ] Add tests for `OnDpiChangedAfterParent`: framework-owned glyph geometry and framework-owned default `ItemHeight` refresh without changing node data/state; caller-owned `ItemHeight` remains unchanged.
- [ ] Add tests proving an oversized caller `NodeFont` is never disposed or silently used to mutate the whole tree's `ItemHeight`.
- [ ] Add explicit handle-recreation/runtime-native-state tests for `CheckBoxes`, `Scrollable`, `ImageIndex`, and `SelectedImageIndex`; verify theme subscription, framework-owned font/item-height tracking, and hover bookkeeping remain valid after each change.
- [ ] Where native handle recreation collapses nodes or otherwise changes native state, assert that `BootstrapTreeView` does not restore a parallel cached state.
- [ ] Add disposal tests verifying `BootstrapThemeManager.ThemeChanged` subscription count returns to baseline.
- [ ] Add `BootstrapTreeView` to `Phase15HardeningTests.ThemeSwitchStressKeepsRepresentativeControlsUsableAndDisposalDetachesSubscriptions`.
- [ ] Ensure event handlers never touch a disposed control/handle.
- [ ] Run both TFMs.
- [ ] Commit, e.g. `fix: harden BootstrapTreeView lifecycle`.

---

# Task 9: Prove Native Keyboard, Label Editing, Events, Drag/Drop, HotTracking, and Accessibility Remain Intact

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`

- [ ] Add a small test subclass exposing protected key/message hooks only where NUnit cannot drive the native control through ordinary APIs.
- [ ] Verify Up/Down navigation changes `SelectedNode` through the native path and produces one selection event sequence.
- [ ] Verify Left/Right collapse/expand/navigate behavior is not intercepted by `BootstrapTreeView`.
- [ ] Verify Home/End and PageUp/PageDown continue to navigate visible nodes without framework reset of selection.
- [ ] Verify expand/collapse keys still work and framework code does not introduce its own key-state machine.
- [ ] With `CheckBoxes == true`, verify Space changes native checked state where supported by the native control and raises native check events exactly once.
- [ ] With `LabelEdit == true`, verify `BeginEdit()`/F2 can create the native label editor and commit/cancel through the native lifecycle.
- [ ] Verify inherited `DrawNode` is raised exactly once per node draw while framework painting remains authoritative.
- [ ] Verify `NodeMouseClick`, `BeforeSelect`/`AfterSelect`, `BeforeExpand`/`AfterExpand`, and `BeforeCheck`/`AfterCheck` are not duplicated by framework mouse handling.
- [ ] Verify `HotTracking` still round-trips and native hot-tracking behavior is not replaced by the framework's private hover bookkeeping.
- [ ] Verify `ItemDrag` fires exactly once for a representative drag gesture and is not suppressed/duplicated by hover or any evidence-gated full-row correction.
- [ ] With `AllowDrop = true`, add smoke coverage for `DragEnter`, `DragOver`, and `DragDrop` through the native control path. The framework must not introduce a drag/drop policy or a second drag engine.
- [ ] Verify owner drawing leaves the native accessibility object hierarchy available; do not replace `CreateAccessibilityInstance` unless a demonstrated defect requires it.
- [ ] Verify the control remains reachable by Tab according to native `TabStop` behavior and does not trap focus on Shift+Tab/Tab transitions to sibling controls.
- [ ] Run both TFMs.
- [ ] Commit, e.g. `test: cover BootstrapTreeView native interaction contract`.

---

# Task 10: Add Integrated Demo Coverage

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/TreeViewDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Optionally create/modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/*` only if the current demo has structural registration tests.

- [ ] Add a `TreeViewDemoForm` that demonstrates realistic business hierarchies rather than synthetic one-level nodes, for example organization, product/category, or permission structures.
- [ ] Include scenarios on the same page for:
  - default hierarchy and selection;
  - Light/Dark runtime theme switching through the existing shell;
  - all semantic `Variant` values or a compact variant selector;
  - `ShowLines`/`ShowRootLines`/`ShowPlusMinus` combinations;
  - `FullRowSelect` behavior with and without lines;
  - `CheckBoxes` without a custom state list;
  - `StateImageList` behavior, including a deliberately non-default `StateImageList.ImageSize` to expose visual/hit-test drift;
  - node `ImageList` normal/selected images;
  - node-specific `ForeColor`/`BackColor`/`NodeFont` overrides and an `ItemHeight` adjustment example for a large `NodeFont`;
  - disabled control;
  - native `HotTracking` on/off;
  - label editing;
  - drag initiation and an `AllowDrop` smoke scenario without framework reorder policy;
  - deep hierarchy with scrolling;
  - long text and constrained width;
  - horizontal scroll with expander/image/state-image hit testing;
  - RTL/right-to-left layout where supported;
  - rapid expand/collapse and theme changes.
- [ ] Add a small diagnostics panel showing `SelectedNode`, checked state, last native event, last hit-test location, and drag event so manual verification can detect duplicate/missing events or visual/hit-test drift.
- [ ] Register a new `TreeView` page in `MainForm.ConfigurePages()` near other navigation/data controls.
- [ ] Do not create a second standalone demo launcher or bypass the integrated theme shell.
- [ ] Build the demo for both target frameworks.
- [ ] Commit, e.g. `demo: add BootstrapTreeView scenarios`.

---

# Task 11: Documentation and API Review

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `README.md`
- Modify: `CHANGELOG.md` only if required by current repository convention

- [ ] Add a `BootstrapTreeView` section to `docs/COMPONENTS.md` describing it as a native-backed control.
- [ ] Document the single new V1 public property `Variant` and explicitly point users to inherited `TreeView` properties for nodes, checkboxes, images, layout, selection, label editing, expansion, hot tracking, and drag/drop.
- [ ] Document that constructor/default configuration uses `DrawMode = OwnerDrawAll`, but the inherited public setter cannot be made immutable safely; changing it away from `OwnerDrawAll` is unsupported rather than silently prevented.
- [ ] Document inherited `DrawNode` as an observable event, not a V1 renderer-replacement hook; framework rendering remains authoritative.
- [ ] Document the `ItemHeight` ownership policy and the caveat that unusually large per-node `NodeFont` values may require the caller to increase inherited `ItemHeight`.
- [ ] Document that state-image display geometry follows native TreeView behavior and is not simply the value of `StateImageList.ImageSize`.
- [ ] Document that full-row mouse correction is evidence-gated: no custom selection layer exists unless baseline native tests demonstrate a specific gap.
- [ ] Document V1 non-goals: custom data binding, async loading, tri-state model, filtering/search, per-node disabled state, rounded shell, virtualized tree, replacement accessibility, and drag/drop reorder policy.
- [ ] Add TreeView-specific manual matrix items to `docs/TESTING.md`: keyboard, focus exit/return, `DrawNode` count, checkbox/state-image interaction, native hit-test parity, label editing, FullRowSelect vs ShowLines, HotTracking, ItemDrag/AllowDrop, RTL, DPI, horizontal/vertical scroll, theme switch, explicit handle recreation (`CheckBoxes`, `Scrollable`, `ImageIndex`, `SelectedImageIndex`), and disposal.
- [ ] Add the component to the README supported-control list/table and link to `docs/COMPONENTS.md` if that is the existing convention.
- [ ] Review public reflection output and confirm no accidental public helper types escaped from `Controls.Internal` or the assembly surface.
- [ ] Confirm all XML docs compile without warnings under both TFMs.
- [ ] Commit, e.g. `docs: document BootstrapTreeView`.

---

# Task 12: Final Cross-Framework Verification

**Files:**
- No new files expected unless verification exposes a defect.

- [ ] Run formatting/static repository checks required by `AGENTS.md` and existing build scripts.
- [ ] Build the full solution for `net48`.
- [ ] Build the full solution for `net8.0-windows`.
- [ ] Run the full test project for `net48`.
- [ ] Run the full test project for `net8.0-windows`.
- [ ] Run `build.ps1` if it is the repository's release-quality verification entry point.
- [ ] Manually verify the integrated demo at 96, 120, 144, 168, and 192 DPI where practical.
- [ ] Manually verify Light/Dark themes, enabled/disabled, hover, native HotTracking, selected focused/unfocused, `HideSelection`, keyboard, label editing, image lists, state images, checkboxes, scrollbars, RTL, expand/collapse, drag initiation/drop smoke behavior, disposal/reopen, and rapid state changes.
- [ ] At each practical DPI/RTL configuration, verify that visible expander, state-image/checkbox, and node-image glyphs coincide with native `HitTest` regions; no visibly clickable glyph may sit outside its native region.
- [ ] Inspect GDI object usage while repeatedly expanding/collapsing, scrolling, hovering, and switching themes; no monotonic leak should appear from framework painting.
- [ ] Verify no caller-owned `ImageList`, `StateImageList`, images, or node fonts are disposed by the control.
- [ ] Verify a deliberately oversized `NodeFont` behaves according to the documented `ItemHeight` policy and does not trigger framework-wide row-size mutation.
- [ ] Verify a non-default `StateImageList.ImageSize` does not move/resize the framework state-image slot away from native hit testing.
- [ ] Verify `CheckBoxes`, `Scrollable`, `ImageIndex`, and `SelectedImageIndex` runtime changes follow native handle-recreation/state behavior without framework exceptions, cached expansion restoration, or duplicate theme subscriptions.
- [ ] Verify Tab/Shift+Tab can move focus out of the TreeView and Alt/activation changes do not leave stale hover/focus visuals.
- [ ] Confirm `git diff --check` is clean and only intentional files changed.
- [ ] Create the final implementation commit only after all checks pass.

Suggested commands:

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -f net48
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -f net8.0-windows

dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows

./build.ps1
git diff --check
```

---

## Acceptance Criteria

Implementation is complete only when all of the following are true:

- `BootstrapTreeView` is a direct native `TreeView` subclass and introduces no parallel tree/data/selection/hit-test model.
- The V1 public API adds only the justified Bootstrap-specific surface (`Variant`) and reuses native TreeView APIs everywhere else.
- Default configuration uses inherited `DrawMode = OwnerDrawAll`; documentation accurately states that assigning another inherited `DrawMode` value is unsupported rather than technically impossible.
- Inherited `DrawNode` remains observable exactly once per draw while framework painting remains authoritative.
- Native node selection, expand/collapse, checkbox, image-list, state-image, label editing, keyboard navigation, HotTracking, `ItemDrag`, drag/drop events, and accessibility remain functional.
- Framework expander/state-image/node-image presentation is validated against native `TreeView.HitTest` geometry across representative LTR/RTL, scrolling, and DPI configurations.
- Node painting is Bootstrap-inspired, Light/Dark aware, semantic-variant aware, and visually stable across supported DPI values.
- `ShowLines`, `ShowPlusMinus`, `ShowRootLines`, `FullRowSelect`, `HideSelection`, `ImageList`, `StateImageList`, `ItemHeight`, `HotTracking`, and per-node font/color settings have documented/tested behavior.
- State-image layout does not incorrectly use `StateImageList.ImageSize` as the display-slot size.
- Framework-managed default `ItemHeight` updates with theme/DPI only until the caller takes ownership; `Indent` remains native/caller-controlled.
- Full-row custom mouse selection exists only if a baseline native test demonstrates a real gap; if it exists, it is narrowly scoped and produces no duplicate native events.
- Hover/full-row adaptation does not create duplicate native mouse/selection/check/drag events.
- Theme changes do not reset tree content, selection, checked state, or expansion state.
- Runtime changes to `CheckBoxes`, `Scrollable`, `ImageIndex`, and `SelectedImageIndex` do not leak theme subscriptions/resources or cause framework state restoration that conflicts with native handle recreation.
- Caller-owned images/image lists/fonts are never disposed by the control.
- The integrated demo includes realistic tree scenarios, native hit-test diagnostics, and diagnostics for selection/check/drag events.
- Full tests pass for both `net48` and `net8.0-windows`.
- Documentation and supported-control lists are updated.

---

## Deferred Follow-up Candidates

These are intentionally outside this V1 plan and should be proposed separately only after the native-backed control is stable:

- `BootstrapTreeNode` convenience metadata/type.
- Tri-state checkbox abstraction and parent/child propagation policies.
- Async/lazy child loading with loading/error placeholders.
- Search/filter/highlight helpers.
- Per-node disabled/read-only semantics.
- Custom node renderer/template callbacks.
- Drag/drop insertion indicators and reorder helpers.
- Context action buttons or badges.
- Virtualized/large-tree data source abstraction.
- Optional source-neutral framework icons independent of native `ImageList`.
- Rounded outer shell/non-client scrollbar integration.

Keeping these separate prevents the first TreeView implementation from becoming a replacement tree framework and preserves the project's native-first design philosophy.