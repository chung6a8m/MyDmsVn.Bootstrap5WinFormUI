# BootstrapTreeView Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a production-ready `BootstrapTreeView` that keeps native WinForms `TreeView` data, selection, expansion, editing, keyboard, drag/drop, image-list, checkbox, accessibility, and event semantics authoritative while replacing node presentation with Bootstrap-inspired, theme-aware, DPI-aware rendering.

**Architecture:** `BootstrapTreeView` derives directly from `System.Windows.Forms.TreeView`. The native control remains the tree/data/interaction engine. The framework sets `DrawMode = OwnerDrawAll` and owns only painting, hover bookkeeping needed for painting, DPI-scaled geometry, theme/font synchronization, and the minimal full-row hit-area correction required when native `FullRowSelect` is active. Rendering decisions and geometry live in small internal helpers so they can be tested without duplicating TreeView behavior. No custom tree model, virtualization layer, popup/window infrastructure, or replacement accessibility tree is introduced.

**Tech Stack:** C#, WinForms, `net48;net8.0-windows`, existing `BootstrapThemeManager`, `BootstrapVariant`, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`, NUnit, integrated demo application.

---

## Global Constraints

- Preserve the repository dependency direction documented in `AGENTS.md` and `docs/ARCHITECTURE.md`: the control may depend on Theme/Rendering/Compatibility, but shared infrastructure must not depend on `BootstrapTreeView`.
- Keep `System.Windows.Forms.TreeView` as the behavioral source of truth. Do **not** introduce parallel `Nodes`, `SelectedNode`, `CheckedNodes`, expansion state, focus model, keyboard engine, label editor, drag/drop engine, or accessibility object model.
- Preserve inherited native APIs, including at minimum: `Nodes`, `SelectedNode`, `CheckBoxes`, `ImageList`, `StateImageList`, `ImageIndex`, `SelectedImageIndex`, `Indent`, `ItemHeight`, `ShowLines`, `ShowPlusMinus`, `ShowRootLines`, `FullRowSelect`, `HideSelection`, `HotTracking`, `LabelEdit`, `Scrollable`, `PathSeparator`, `NodeMouseClick`, `BeforeSelect`/`AfterSelect`, `BeforeExpand`/`AfterExpand`, `BeforeCollapse`/`AfterCollapse`, `BeforeCheck`/`AfterCheck`, and drag/drop events.
- Do not duplicate inherited appearance/layout properties with aliases such as `NodeHeight` for `ItemHeight` or `NodeIndent` for `Indent` unless implementation evidence later proves native APIs insufficient. V1 should remain a small public API.
- Do not add custom data binding, lazy-loading, async child loading, tri-state checkboxes, filtering/search, loading/empty overlays, drag/drop policy, context menus, per-node command buttons, or a virtualized tree model in this plan.
- Do not add `BorderRadius` in V1. A native `TreeView` owns its HWND, scrolling, and non-client behavior; rounded clipping/non-client painting would create disproportionate lifecycle and scrollbar complexity for a first implementation.
- Do not add FontAwesome.Sharp, SVG, SkiaSharp, or another icon dependency. Tree node images continue through native `ImageList`/`StateImageList`; the small expand/collapse indicator and default checkbox marks are framework-owned vector primitives.
- Do not dispose caller-owned `ImageList`, `StateImageList`, `TreeNode.NodeFont`, or images.
- Avoid allocations in steady-state node painting. Do not create a new `Font`, `Pen`, `Brush`, `GraphicsPath`, or collection per node unless wrapped in deterministic disposal and no reusable/pure alternative exists.
- All semantic colors come from `BootstrapThemeManager.CurrentTheme`; do not hardcode Bootstrap hex values or Windows system colors for framework-owned presentation.
- All framework-owned pixel geometry must scale from the current DPI through `DpiScaler`; do not use `Math.Clamp` or other APIs unavailable on `net48`.
- Theme subscription must be paired with deterministic unsubscribe in `Dispose(bool)` and must remain safe after handle recreation.
- Tests that instantiate WinForms controls must run STA and non-parallel when they mutate global theme state.
- Build and test both target frameworks before completion.

---

## Reference Behavior and WinForms Adaptation

Bootstrap 5 does not define an official TreeView component. Therefore the design target is **Bootstrap visual language applied to the native WinForms TreeView contract**, not a web component port.

The implementation should follow these WinForms rules exactly:

1. `TreeViewDrawMode.OwnerDrawAll` means the framework must draw the complete node presentation: text, node image, state image/checkbox, expand-collapse indicator, and connector lines when enabled.
2. Under `OwnerDrawAll`, `DrawTreeNodeEventArgs.Bounds` spans the row width, while `TreeNode.Bounds` represents the native label hit region. Use the native label bounds as the anchor for text/content geometry instead of inventing a separate indentation engine.
3. Native `FullRowSelect` is ignored when `ShowLines == true`. The Bootstrap renderer must preserve that contract rather than forcing full-row selection in configurations where WinForms does not.
4. Root expand/collapse indicators are only shown when native `ShowRootLines == true`; otherwise `ShowPlusMinus` has no visible root indicator. Preserve that behavior.
5. When `CheckBoxes == true` and `StateImageList` is assigned, native TreeView semantics use the first two state images for unchecked/checked states. Preserve that contract rather than replacing those images with framework checkbox art.
6. Changing `CheckBoxes` at runtime can recreate the native TreeView handle and collapse nodes except the selected node. `BootstrapTreeView` must not attempt to hide or reverse that native side effect with a parallel expansion cache.
7. Owner drawing must not replace native keyboard behavior. Arrow keys, Home/End, PageUp/PageDown, `+`/`-`, `*`, Space for checks where supported by native behavior, F2 label edit, and standard selection events remain native responsibilities.

Useful implementation references:

- `TreeView.DrawNode`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.drawnode
- `TreeView.FullRowSelect`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.fullrowselect
- `TreeView.ShowPlusMinus`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.showplusminus
- `TreeView.ShowRootLines`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.showrootlines
- `TreeView.CheckBoxes`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.checkboxes
- `TreeView.StateImageList`: https://learn.microsoft.com/dotnet/api/system.windows.forms.treeview.stateimagelist

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
- `DrawMode` is framework-owned and remains `TreeViewDrawMode.OwnerDrawAll`; consumers should not be expected to replace the framework node renderer through `DrawNode`.
- Hide/restrict `DrawMode` in the designer only if that can be done without breaking the inherited public contract. Do not introduce a new public render-mode abstraction in V1.
- Existing `TreeNode.ForeColor`, `TreeNode.BackColor`, and `TreeNode.NodeFont` overrides remain meaningful for neutral nodes. Selection/disabled states may override colors when needed for legibility and state visibility.
- The control should not add custom selection events; use the inherited TreeView events.

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
- Moving between nodes invalidates only the old/new visible row bounds where practical rather than invalidating the whole control.

### Selected node

- Use `BootstrapVariantColorResolver.Resolve(theme.Colors, Variant)` as the active background.
- Use `ColorUtil.GetContrastingTextColor(...)` for selected text/glyph contrast.
- Respect native selection visibility: if the control does not have focus and `HideSelection == true`, render the node as unselected.
- If native full-row selection is effective (`FullRowSelect == true && ShowLines == false`), fill the visible row width. Otherwise keep the selected background scoped to the native content/label region.
- Draw a theme-derived focus cue only for the selected node when the control has keyboard focus and `ShowFocusCues` is true.

### Disabled control

- Use muted theme text/glyph colors and a theme surface background.
- Do not implement per-node disabled state in V1 because `TreeNode` has no native `Enabled` contract.

### Images and checkboxes

- Respect `ImageList`, node image key/index, selected image key/index, and TreeView fallback image key/index semantics.
- Respect `StateImageList` and `TreeNode.StateImageKey`/`StateImageIndex` when `CheckBoxes == false`.
- When `CheckBoxes == true`:
  - if `StateImageList` provides the native checked/unchecked images, render those images according to native semantics;
  - otherwise render a Bootstrap-themed checkbox visual from `TreeNode.Checked` without owning a second checked-state model.
- Image and state-image drawing must preserve aspect ratio within the native/DPI-scaled content slot and must never dispose caller-owned images.

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
- Mirror framework-owned geometry for right-to-left layouts while continuing to use native node bounds as the primary anchor.
- Scale only framework-owned gaps/glyph sizes via `DpiScaler`.
- Clip rectangles to the client area.
- Keep enough separation that checkbox/state image, node image, and text never overlap at 96/120/144/168/192 DPI.
- Never infer tree hierarchy from text width or custom collections; hierarchy level comes from the native node.

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
  - native TreeView subclass, public `Variant`, owner-draw orchestration, theme/font lifecycle, hover state, full-row native hit-area correction.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapTreeViewRenderLogic.cs`
  - pure visual-state and palette decisions.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapTreeViewLayout.cs`
  - DPI/RTL-aware node geometry derived from native bounds.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`
  - public/native contract, theme lifecycle, interaction, image/checkbox, label-edit and handle-recreation tests.
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
- [ ] Add a failing test asserting framework drawing uses `TreeViewDrawMode.OwnerDrawAll`.
- [ ] Add tests proving native properties still round-trip without framework state duplication: `SelectedNode`, `CheckBoxes`, `ImageList`, `StateImageList`, `FullRowSelect`, `ShowLines`, `ShowPlusMinus`, `ShowRootLines`, `LabelEdit`, `ItemHeight`, and `Indent`.
- [ ] Add a test proving changing `Variant` leaves `SelectedNode`, `Checked`, and `IsExpanded` unchanged.
- [ ] Add a test proving the class does not declare public aliases named `NodeHeight`, `NodeIndent`, `CheckedNodes`, `ExpandedNodes`, `Loading`, or `EmptyStateText`.
- [ ] Implement the smallest class skeleton and `Variant` property required to make these tests pass.
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

- [ ] Define layout inputs using primitive/native geometry: client bounds, `DrawTreeNodeEventArgs.Bounds`, `TreeNode.Bounds`, node level, DPI, RTL flags, effective full-row selection, expander/state-image/node-image presence, and image sizes.
- [ ] Add 96 DPI tests establishing non-overlapping expander/state image/node image/text rectangles.
- [ ] Add 120/144/168/192 DPI tests proving framework-owned glyphs and gaps scale monotonically and remain inside the visible row.
- [ ] Add tests for rows narrower than expected: rectangles clamp to non-negative sizes and text bounds may become empty without throwing.
- [ ] Add tests for horizontal scrolling/native label bounds shifted toward or outside the client area; layout must clip rather than recalculate hierarchy from scratch.
- [ ] Add RTL tests proving framework-owned slots mirror while selection/focus bounds remain correct.
- [ ] Add tests for `FullRowSelect` effective vs ineffective layouts.
- [ ] Implement layout using `DpiScaler` and existing compatibility-safe math.
- [ ] Keep layout independent of `Graphics`, `TreeView`, and `TreeNode` so the matrix is deterministic.
- [ ] Run layout tests on both TFMs.
- [ ] Commit, e.g. `feat: add TreeView owner-draw layout`.

---

# Task 4: Build the Owner-Draw Node Pipeline

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTreeView.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`

- [ ] Add a protected test probe or narrowly scoped internal seam so tests can render a node into a bitmap without exposing a new public renderer API.
- [ ] Write a failing smoke test that creates a handle, adds visible nodes, renders to a bitmap, and completes without exceptions/GDI leaks in Light and Dark modes.
- [ ] Override `OnDrawNode` and call the pure render/layout helpers.
- [ ] Fill only the required background region: full visible row when native full-row selection is effective, otherwise the node content/label region.
- [ ] Draw node text with `TextRenderer` and flags that match native single-line tree behavior, ellipsis, no-prefix semantics where appropriate, and RTL.
- [ ] Respect `TreeNode.NodeFont`, `TreeNode.ForeColor`, and `TreeNode.BackColor` in neutral state.
- [ ] Draw selection text/background using `Variant` and a contrasting foreground.
- [ ] Draw focus cue only for the visible selected node when focus cues should be shown.
- [ ] Ensure drawing handles an empty node text, zero-size bounds, hidden nodes, partially clipped rows, and disposal without throwing.
- [ ] Do not call `e.DrawDefault = true` in `OwnerDrawAll`; all desired node elements must be deterministic under framework rendering.
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
- [ ] Confirm native mouse double-click and plus/minus hit locations still expand/collapse exactly once.
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
- [ ] Add tests proving caller-owned `StateImageList` survives control disposal.
- [ ] Add interaction tests that clicking the native state-image/checkbox hit region changes `TreeNode.Checked` and raises `BeforeCheck`/`AfterCheck` once.
- [ ] Implement image resolution as small private/internal functions; do not clone images into framework-owned lists.
- [ ] Draw images with appropriate interpolation/pixel-offset settings only when scaling is necessary; preserve caller image aspect ratio.
- [ ] Implement framework checkbox border/fill/checkmark from theme tokens and DPI-scaled primitives only when no caller state image owns the checkbox visual.
- [ ] Verify changing `CheckBoxes` at runtime does not trigger framework exceptions across native handle recreation; do not restore collapsed nodes automatically.
- [ ] Run both TFMs.
- [ ] Commit, e.g. `feat: preserve TreeView image and checkbox semantics`.

---

# Task 7: Add Hover, Selection, Focus, and Full-Row Hit-Area Behavior

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTreeView.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`

- [ ] Add tests for hover transitions using `GetNodeAt`/native hit testing: old hot node is cleared, new node is stored, MouseLeave clears hover.
- [ ] Add tests proving hover does not mutate `SelectedNode`.
- [ ] Add tests for focus enter/leave repaint behavior with `HideSelection` true and false.
- [ ] Add tests for native full-row selection click correction only when `FullRowSelect == true && ShowLines == false`.
- [ ] The correction must not steal clicks from native `PlusMinus`, `StateImage`, or label-edit regions.
- [ ] Use `TreeView.HitTest` and `TreeViewHitTestLocations` as the native geometry source. Do not implement a second arbitrary point-to-node search.
- [ ] Ensure a blank area below the last visible node does not change selection.
- [ ] Ensure right-click does not silently force selection unless that is already native behavior for the hit region.
- [ ] Ensure double-click still expands/collapses according to native TreeView rules and is not processed twice by the correction layer.
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
- [ ] Add tests for `OnDpiChangedAfterParent`: framework-owned glyph geometry and any framework-managed default sizing refreshes without changing node data/state.
- [ ] If the implementation applies a Bootstrap-friendly initial `ItemHeight`/`Indent`, track the last framework-assigned values and stop overwriting them once the caller changes the inherited property. Do not add duplicate public properties.
- [ ] Add handle-recreation tests around `CheckBoxes` and other native properties known to recreate the HWND; verify theme subscription and hover bookkeeping remain valid.
- [ ] Add disposal tests verifying `BootstrapThemeManager.ThemeChanged` subscription count returns to baseline.
- [ ] Add `BootstrapTreeView` to `Phase15HardeningTests.ThemeSwitchStressKeepsRepresentativeControlsUsableAndDisposalDetachesSubscriptions`.
- [ ] Ensure event handlers never touch a disposed control/handle.
- [ ] Run both TFMs.
- [ ] Commit, e.g. `fix: harden BootstrapTreeView lifecycle`.

---

# Task 9: Prove Native Keyboard, Label Editing, Events, and Accessibility Remain Intact

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTreeViewTests.cs`

- [ ] Add a small test subclass exposing protected key/message hooks only where NUnit cannot drive the native control through ordinary APIs.
- [ ] Verify Up/Down navigation changes `SelectedNode` through the native path and produces one selection event sequence.
- [ ] Verify Left/Right collapse/expand/navigate behavior is not intercepted by `BootstrapTreeView`.
- [ ] Verify Home/End and PageUp/PageDown continue to navigate visible nodes without framework reset of selection.
- [ ] Verify expand/collapse keys still work and framework code does not introduce its own key-state machine.
- [ ] With `CheckBoxes == true`, verify Space changes native checked state where supported by the native control and raises native check events exactly once.
- [ ] With `LabelEdit == true`, verify `BeginEdit()`/F2 can create the native label editor and commit/cancel through the native lifecycle.
- [ ] Verify `NodeMouseClick`, `BeforeSelect`/`AfterSelect`, `BeforeExpand`/`AfterExpand`, and `BeforeCheck`/`AfterCheck` are not duplicated by framework mouse handling.
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
  - `StateImageList` behavior;
  - node `ImageList` normal/selected images;
  - node-specific `ForeColor`/`BackColor`/`NodeFont` overrides;
  - disabled control;
  - label editing;
  - deep hierarchy with scrolling;
  - long text and constrained width;
  - RTL/right-to-left layout where supported;
  - rapid expand/collapse and theme changes.
- [ ] Add a small diagnostics panel showing `SelectedNode`, checked state, and last native event so manual verification can detect duplicate/missing events.
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
- [ ] Document the single new V1 public property `Variant` and explicitly point users to inherited `TreeView` properties for nodes, checkboxes, images, layout, selection, label editing, and expansion.
- [ ] Document that `DrawMode` is framework-owned because complete owner drawing is required for consistent themed nodes.
- [ ] Document V1 non-goals: custom data binding, async loading, tri-state model, filtering/search, per-node disabled state, rounded shell, virtualized tree, and replacement accessibility.
- [ ] Add TreeView-specific manual matrix items to `docs/TESTING.md`: keyboard, focus exit/return, checkbox/state-image interaction, label editing, FullRowSelect vs ShowLines, RTL, DPI, scroll, theme switch, handle recreation, and disposal.
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
- [ ] Manually verify Light/Dark themes, enabled/disabled, hover, selected focused/unfocused, `HideSelection`, keyboard, label editing, image lists, state images, checkboxes, scrollbars, RTL, expand/collapse, disposal/reopen, and rapid state changes.
- [ ] Inspect GDI object usage while repeatedly expanding/collapsing, scrolling, hovering, and switching themes; no monotonic leak should appear from framework painting.
- [ ] Verify no caller-owned `ImageList`, `StateImageList`, images, or node fonts are disposed by the control.
- [ ] Verify changing `CheckBoxes` at runtime follows native handle-recreation behavior and does not produce framework exceptions or duplicate theme subscriptions.
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

- `BootstrapTreeView` is a direct native `TreeView` subclass and introduces no parallel tree/data/selection model.
- The V1 public API adds only the justified Bootstrap-specific surface (`Variant`) and reuses native TreeView APIs everywhere else.
- Native node selection, expand/collapse, checkbox, image-list, state-image, label editing, keyboard navigation, drag/drop events, and accessibility remain functional.
- Node painting is Bootstrap-inspired, Light/Dark aware, semantic-variant aware, and visually stable across supported DPI values.
- `ShowLines`, `ShowPlusMinus`, `ShowRootLines`, `FullRowSelect`, `HideSelection`, `ImageList`, `StateImageList`, and per-node font/color settings have documented/tested behavior.
- Hover and full-row painting do not create duplicate native mouse/selection/check events.
- Theme changes do not reset tree content, selection, checked state, or expansion state.
- Handle recreation does not leak theme subscriptions or framework resources.
- Caller-owned images/image lists/fonts are never disposed by the control.
- The integrated demo includes realistic tree scenarios and diagnostics for native events.
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