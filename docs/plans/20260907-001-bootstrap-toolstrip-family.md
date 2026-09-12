# Bootstrap ToolStrip Family Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add production-ready `BootstrapToolStrip`, `BootstrapMenuStrip`, `BootstrapContextMenuStrip`, and `BootstrapStatusStrip` controls that retain the native WinForms ToolStrip family as the authoritative behavior engine while replacing the default Windows presentation with the framework's Bootstrap-like theme, semantic variants, DPI-aware rendering, and lifecycle conventions.

**Architecture:** Each public control derives directly from its native WinForms counterpart. Native `Items`, `ToolStripItem` subclasses, layout, keyboard/mnemonic processing, shortcut routing, overflow, menu/drop-down lifecycle, MDI merge, accessibility, and status-strip sizing remain authoritative. A shared internal `ToolStripRenderer` implementation owns Bootstrap presentation for the whole family, while a small shared appearance controller owns renderer installation, semantic `Variant`, theme subscription, framework-owned typography, created/open drop-down tracking for repaint/lifecycle only, invalidation, and disposal. The existing `BootstrapDropdownRenderer` is refactored onto the same rendering core so `BootstrapDropdown` and the new controls do not drift into separate menu-rendering systems.

**Tech Stack:** C#, Windows Forms, `net48;net8.0-windows`, `BootstrapThemeManager`, `BootstrapVariant`, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`, native `ToolStripRenderer`, NUnit, integrated WinForms demo.

**Spec / repository context:**

- `AGENTS.md`
- `docs/ARCHITECTURE.md`
- `docs/DESIGN_SYSTEM.md`
- `docs/COMPONENTS.md`
- `docs/COMPATIBILITY.md`
- `docs/TESTING.md`
- Existing native-backed controls such as `BootstrapTreeView` and `BootstrapListView`
- Existing `Controls/BootstrapDropdownRenderer.cs`, which already renders a native `ToolStripDropDownMenu`
- Existing `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/WinFormsTestEnvironment.cs`
- Microsoft WinForms `ToolStripRenderer`, `ToolStrip`, `MenuStrip`, `ContextMenuStrip`, `StatusStrip`, overflow, and merge contracts

---

## Global Constraints

1. **Native-first behavior is non-negotiable.** Do not build a custom menu engine, toolbar layout engine, overflow menu, status-layout engine, keyboard router, accessibility tree, shortcut dispatcher, or popup window.
2. **Direct native inheritance is required:**
   - `BootstrapToolStrip : ToolStrip`
   - `BootstrapMenuStrip : MenuStrip`
   - `BootstrapContextMenuStrip : ContextMenuStrip`
   - `BootstrapStatusStrip : StatusStrip`
3. **The framework owns presentation, not item semantics.** Existing `ToolStripButton`, `ToolStripLabel`, `ToolStripDropDownButton`, `ToolStripSplitButton`, `ToolStripMenuItem`, `ToolStripSeparator`, `ToolStripTextBox`, `ToolStripComboBox`, `ToolStripProgressBar`, `ToolStripStatusLabel`, and related native item types remain valid and authoritative.
4. **Do not introduce a parallel public item model in v1.** There must not be `BootstrapToolStripItem`, `BootstrapMenuItem`, or collection adapters that mirror native `Items`.
5. **Do not hide or reimplement inherited `Renderer`, `RenderMode`, `Items`, `LayoutStyle`, `AllowMerge`, `GripStyle`, `ImageScalingSize`, `ShowItemToolTips`, `CanOverflow`, `AutoSize`, `RightToLeft`, or accessibility properties.**
6. **Installing a caller-supplied renderer is an explicit opt-out from Bootstrap painting.** The controls must not silently replace a renderer assigned by application code after construction.
7. **Do not globally mutate `ToolStripManager.Renderer`.** Styling must be per-control so adding one Bootstrap control cannot restyle unrelated native ToolStrips in the process.
8. **Do not use custom popup `Form`s, global keyboard hooks, polling timers, or Win32 subclassing to implement menu behavior.** Native `ToolStripDropDown`/`ContextMenuStrip` behavior remains the host.
9. **No rounded popup-window/non-client geometry in v1.** Bootstrap-like border radius must not be simulated by region clipping because that risks hit-testing, shadow, overflow, accessibility, and DPI regressions.
10. **Native `Image`/`ImageList`/`ImageScaling` behavior remains authoritative.** The renderer may tint only framework-drawn vector affordances such as arrows, checks, grips, and overflow indicators; it must not recolor caller images.
11. **Hosted controls stay native in v1.** `ToolStripTextBox`, `ToolStripComboBox`, and `ToolStripProgressBar` keep their native inner rendering and editing behavior. The family controls may provide coherent outer-strip typography/foreground context but must not embed Bootstrap input controls inside `ToolStripControlHost` automatically.
12. **Both target frameworks must build.** Avoid APIs unavailable on `net48` unless guarded by the repository's compatibility pattern.
13. **All GUI-heavy tests must be STA, deterministic, and bounded.** Reuse `WinFormsTestEnvironment` for fail-fast global exception behavior. Tests that require real menu activation, popup lifetime, keyboard/focus routing, `BeginInvoke`, timers, or already-open surfaces must additionally run inside the bounded STA message-loop host introduced by Task 0; `[Apartment(ApartmentState.STA)]` alone is not sufficient for those scenarios.
14. **Theme subscriptions and GDI resources must be released.** No static-event leaks and no undisposed framework-owned `Font`, `Pen`, `Brush`, or `GraphicsPath` resources.
15. **Theme changes must affect an already-visible strip immediately and an already-open drop-down on the next repaint.** Reopening is not required to pick up a new theme.
16. **DPI-sensitive geometry belongs in shared pure render logic and must use `DpiScaler`.** Do not scatter hard-coded pixel constants across the four public controls.
17. **Do not regress `BootstrapDropdown`.** The existing native `ToolStripDropDownMenu` behavior and presentation are part of the acceptance criteria for the shared renderer refactor.
18. **Preserve caller ownership.** Once application code explicitly replaces the control font, renderer, item sizing, image sizing, padding, or other native layout properties, a theme change must not overwrite those caller choices unless the existing WinForms contract itself does so.
19. **Renderer propagation and drop-down lifecycle tracking are separate concerns.** Native renderer inheritance/propagation should be left alone when sufficient, but the framework may still track already-created/open native drop-down surfaces narrowly for repaint, variant/theme refresh, subscription cleanup, and opt-out correctness. Tracking must never force lazy drop-down creation.
20. **Split-button Bootstrap painting must preserve native split semantics.** `OnRenderSplitButtonBackground` must paint both logical portions, the divider, and the drop-down arrow from native item bounds; `OnRenderArrow` is not a substitute because native `ToolStripSplitButton` does not invoke it separately for its own arrow.
21. **Separator rendering must honor orientation.** `ToolStripSeparatorRenderEventArgs.Vertical` determines whether the separator line is vertical or horizontal; shared render logic must support both.
22. **Status-label border semantics remain native-authoritative.** Custom status-label painting must preserve `ToolStripStatusLabel.BorderSides` and `BorderStyle`; Bootstrap theming must not silently erase caller-configured borders.

---

## Public API Contract

Keep v1 intentionally small. The four controls should expose the same semantic variant property and otherwise inherit the native public API.

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public class BootstrapToolStrip : ToolStrip
{
    [Category("Appearance")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant { get; set; }
}

public class BootstrapMenuStrip : MenuStrip
{
    [Category("Appearance")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant { get; set; }
}

public class BootstrapContextMenuStrip : ContextMenuStrip
{
    public BootstrapContextMenuStrip();
    public BootstrapContextMenuStrip(IContainer container);

    [Category("Appearance")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant { get; set; }
}

public class BootstrapStatusStrip : StatusStrip
{
    [Category("Appearance")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant { get; set; }
}
```

### Variant semantics

`Variant` is an accent, not a request to flood-fill the entire strip.

- Neutral chrome comes from theme `Surface` / `SurfaceSecondary`, `Text`, `MutedText`, and `Border` tokens.
- `Variant` colors interactive selected, hot, checked, and pressed states.
- `BootstrapVariantColorResolver` is the single semantic-color resolver.
- Disabled states use theme disabled/muted colors rather than the selected variant.
- Changing `Variant` invalidates the owning strip and currently-created/open Bootstrap-owned drop-down surfaces.
- If the caller replaced the Bootstrap renderer with a custom renderer, `Variant` remains a valid stored property but has no promise to affect that caller renderer, and the framework must stop forcing Bootstrap renderer propagation into descendant surfaces.

### Designer contract

- The controls must be usable from the WinForms designer like their native bases.
- Native `Items` designer serialization remains the source of truth.
- `BootstrapContextMenuStrip(IContainer)` must call the native container constructor so designer component lifetime remains correct.
- Do not create custom designers unless a concrete designer defect is proven by a regression test.

---

## Native Behavior Contract by Control

### `BootstrapToolStrip`

Must preserve native behavior for:

- `Items` and all standard ToolStrip item types;
- horizontal/vertical layout and docking;
- `GripStyle`, grip dragging behavior where applicable, and `LayoutStyle`;
- keyboard navigation and mnemonics;
- `ToolStripButton.Checked` / `CheckOnClick` semantics;
- split-button main/drop-down actions, native `ButtonBounds` / `DropDownButtonBounds`, and RTL mirroring;
- `CanOverflow`, per-item `Overflow`, and native `ToolStripOverflowButton` creation;
- `AllowMerge`, item `MergeAction`, and `MergeIndex`;
- native tooltips;
- native image sizing and item autosizing;
- horizontal and vertical separator orientation.

### `BootstrapMenuStrip`

Must preserve native behavior for:

- Alt/menu activation and mnemonic navigation;
- `ToolStripMenuItem.ShortcutKeys` and shortcut display text;
- checked menu items and `CheckOnClick`;
- nested/cascading drop-downs;
- `DropDownOpening`, `DropDownOpened`, `DropDownClosed`, and item click events;
- `MdiWindowListItem`;
- MDI menu merge and `ToolStripManager.Merge` semantics;
- right-aligned items and RTL behavior.

### `BootstrapContextMenuStrip`

Must preserve native behavior for:

- `Show(...)` overloads and native screen/control placement;
- `SourceControl` and `OwnerItem` semantics;
- cancelable `Opening` and `Closing` events;
- `Opened` and `Closed` event ordering;
- `AutoClose` behavior;
- nested/cascading drop-downs;
- shortcuts, access keys, images, checks, separators, and disabled items;
- assigning the same context menu to multiple controls without stale source ownership.

### `BootstrapStatusStrip`

Must preserve native behavior for:

- native table-layout behavior;
- `SizingGrip` and grip hit behavior;
- `ToolStripStatusLabel.Spring` width allocation;
- status label image/text behavior plus caller-configured `BorderSides` and `BorderStyle`;
- `ToolStripDropDownButton` and `ToolStripSplitButton` in a status strip;
- hosted `ToolStripProgressBar` behavior;
- `ShowItemToolTips`, RTL, and normal ToolStrip keyboard/accessibility behavior.

---

## Shared Rendering Contract

### Internal types

Create one rendering family rather than four independent renderers:

```text
Controls/Internal/
  BootstrapToolStripSurfaceKind.cs
  BootstrapToolStripRenderLogic.cs
  BootstrapToolStripRendererBase.cs
  BootstrapToolStripRenderer.cs
  BootstrapToolStripAppearanceController.cs
```

Keep `Controls/BootstrapDropdownRenderer.cs` as a thin dropdown-specific adapter over the shared base so its current internal identity can remain available while duplicate rendering logic disappears.

Suggested roles:

```csharp
internal enum BootstrapToolStripSurfaceKind
{
    ToolBar,
    MenuBar,
    DropDown,
    StatusBar
}
```

`BootstrapToolStripRendererBase : ToolStripRenderer` determines the surface kind from the actual native owner at render time:

- `StatusStrip` -> `StatusBar`
- `MenuStrip` -> `MenuBar`
- `ToolStripDropDown` / `ContextMenuStrip` / overflow drop-down -> `DropDown`
- other `ToolStrip` -> `ToolBar`

`BootstrapToolStripRenderLogic` must contain pure, unit-testable calculations for:

- semantic palette resolution;
- disabled/selected/pressed/checked state precedence;
- strip-surface classification where a native object is not required;
- DPI-scaled border width;
- separator inset and orientation-aware line geometry;
- arrow/checkmark size and stroke;
- split-button divider/arrow geometry from native button/drop-down bounds;
- grip-dot geometry;
- overflow-chevron geometry;
- image-margin background and inset;
- state fill blending using `ColorUtil`.

### Required renderer hooks

The shared renderer should own only the parts that require Bootstrap presentation and should leave native image/content layout intact where possible.

Implement and test at least:

- `OnRenderToolStripBackground`
- `OnRenderToolStripBorder`
- `OnRenderImageMargin`
- `OnRenderMenuItemBackground`
- `OnRenderButtonBackground`
- `OnRenderDropDownButtonBackground`
- `OnRenderSplitButtonBackground`
- `OnRenderItemText`
- `OnRenderItemCheck`
- `OnRenderArrow`
- `OnRenderSeparator`
- `OnRenderGrip`
- `OnRenderOverflowButtonBackground`
- `OnRenderStatusStripSizingGrip`
- `OnRenderToolStripStatusLabelBackground`

`OnRenderSplitButtonBackground` has a stronger contract than the other background hooks. Native `ToolStripSplitButton.OnPaint` calls `DrawSplitButton(...)` and then image/text rendering; it does not separately invoke `DrawArrow(...)` for the split-button arrow. Therefore this override must use the native `ToolStripSplitButton.ButtonBounds` and `DropDownButtonBounds` to render the correct main/drop-down states, divider, and arrow, including RTL mirroring. Do not infer these rectangles from hard-coded LTR geometry.

`OnRenderSeparator` must use `ToolStripSeparatorRenderEventArgs.Vertical`: vertical ToolStrip separators draw a vertical line with top/bottom inset; horizontal/drop-down separators draw a horizontal line with left/right inset.

`OnRenderToolStripStatusLabelBackground` must preserve the native meaning of `ToolStripStatusLabel.BorderSides` and `BorderStyle`. The renderer may theme the background/border colors where feasible, but it must not drop requested border sides or collapse distinct native border styles into “no border”.

Do **not** custom-render caller-provided item images unless a specific defect proves it necessary; let the base/native renderer path handle images so transparency and `ImageScaling` stay native.

### State precedence

Use one deterministic precedence across menus and toolbars:

```text
Disabled
  > Pressed / open drop-down owner
  > Checked
  > Selected / hot
  > Normal
```

The exact fill can vary by surface kind, but two controls showing the same semantic state must derive colors from the same palette logic.

### Visual targets

- **ToolBar:** neutral Bootstrap surface, subtle border only where needed by the native strip bounds, compact item hover/pressed/checked fills, DPI-scaled grip and overflow affordances, orientation-correct separators, and split buttons with a visible native-shaped divider/arrow affordance.
- **MenuBar:** neutral menu surface with subtle separating edge; top-level items use flat Bootstrap-like selected/pressed states; opened menu owner remains visibly active while its drop-down is open.
- **DropDown / ContextMenu:** Bootstrap surface, one-pixel logical border, muted image margin, selected row fill, custom vector checkmark and submenu arrow, themed separators.
- **StatusBar:** neutral surface with top separating border, unobtrusive status-label background, preserved status-label border sides/styles, variant states only for interactive button/drop-down/split items, themed sizing grip.

No rendered state may depend on a process-global renderer.

---

## Theme, Font, Renderer, and Drop-down Ownership

Use one `BootstrapToolStripAppearanceController` instance per public strip.

Responsibilities:

1. Install one framework-owned `BootstrapToolStripRenderer` during control construction.
2. Store and validate `Variant`, forward it to the renderer, and invalidate the owner.
3. Subscribe to `BootstrapThemeManager.ThemeChanged` exactly once.
4. Track only already-created/open native child drop-down surfaces needed for repaint/lifecycle; never access `DropDown` merely to force lazy creation.
5. On theme or variant change, invalidate the owner and tracked Bootstrap-owned child drop-down surfaces that still exist.
6. Apply `BootstrapThemeManager.CurrentTheme.Typography.Body` as the default strip font using the same framework-owned-font pattern already used by `BootstrapTreeView`.
7. Detect an external `FontChanged`; after the caller changes `Font`, stop replacing it on later theme changes and dispose only the previous framework-owned font.
8. Never dispose a caller-owned font or caller-owned renderer.
9. If `Renderer` no longer references the framework-owned renderer, do not reinstall it on theme changes and stop forcing Bootstrap renderer propagation into child drop-downs.
10. Detach child-drop-down/item event handlers and clear tracked references when items are removed, surfaces close/dispose, the owner handle is recreated where relevant, or the owner is disposed.
11. Unsubscribe all events and dispose framework-owned font resources when the owner is disposed.

The controller may observe public events on `ToolStrip`; it must not require a new public base class because the four native base classes are different.

---

## Drop-down Renderer Propagation vs Lifecycle Tracking

Bootstrap menu presentation must continue into native child drop-downs, overflow menus, and nested submenus without creating custom popup windows. Treat the following as two separate concerns.

### A. Renderer propagation

1. First characterize native renderer propagation behavior with tests.
2. Normal Bootstrap-owned submenus must use the same shared renderer and current `Variant` as their owning strip.
3. A submenu/drop-down with an explicitly assigned caller renderer must not be overwritten.
4. Dynamic menu items added after control construction must receive the same behavior as initially-created items.
5. Do not recursively pre-create every `DropDown` merely to style it; preserve lazy native creation.
6. If native propagation is sufficient, add no renderer-assignment lifecycle code.
7. If native propagation is insufficient, use narrowly-scoped `ToolStripDropDownItem.DropDownOpening` / actual-created-drop-down wiring in the internal appearance controller. Do not implement a replacement drop-down lifecycle.

### B. Created/open surface tracking

1. Even when native renderer propagation is sufficient, the controller may still need event-driven tracking of already-created/open surfaces so theme/variant changes can repaint them immediately.
2. Tracking must be proportional to surfaces actually created/opened and must not walk or instantiate the complete menu tree.
3. Tracking must not imply renderer ownership: a caller-custom-rendered drop-down may be observed for lifecycle cleanup but must never have its renderer replaced or disposed.
4. When the root strip opts out by replacing its framework renderer, subsequent theme/variant changes must not reassert Bootstrap rendering on descendant surfaces.
5. Closing/removing/disposal must release all tracking/event subscriptions; no stale `ToolStripDropDown` references may survive.

The characterization tests are a gate: choose the smallest behavior-preserving renderer wiring supported by native WinForms, while independently retaining only the lifecycle tracking required for repaint and cleanup.

---

## DPI, RTL, Accessibility, and Performance Contract

- All custom geometry must be derived from `DeviceDpi` with a 96-DPI fallback and `DpiScaler`.
- Do not take ownership of native `ImageScalingSize`; honor caller/native sizing.
- Repaint correctly after parent DPI changes and handle recreation.
- `RightToLeft` must remain native. Arrow direction, split areas, image margins, and checkmark alignment must follow native event/item bounds rather than hard-coded LTR positions.
- Native accessible roles/names and keyboard navigation remain authoritative; the renderer must not replace accessible objects.
- Rendering must allocate only short-lived GDI objects inside `using` scopes; no per-paint retained brushes/pens without explicit disposal ownership.
- Theme or variant changes invalidate; ordinary mouse movement must not trigger whole-form invalidation.
- Do not recursively walk a large menu tree on every paint. Child-drop-down tracking must be event-driven and proportional to actual created/open surfaces.

---

## Test Execution Rules

Focused GUI tests should run on `net8.0-windows` during TDD and always use a hang safety net:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --filter "FullyQualifiedName~BootstrapToolStrip" `
  --blame-hang --blame-hang-timeout 5m
```

Use `[Apartment(ApartmentState.STA)]` for tests that only need STA ownership while synchronously creating/rendering controls. **Do not rely on `[Apartment]` alone** for tests requiring real message dispatch. Any test that opens a native drop-down, verifies Alt/mnemonic/shortcut/focus routing, waits for `BeginInvoke`, observes popup open/close ordering, or changes theme/variant while a popup remains open must execute its UI scenario through `WinFormsMessageLoopTestHost` from Task 0.

Use `[NonParallelizable]` for tests that mutate `BootstrapThemeManager.CurrentTheme` or another process-global WinForms state.

The final gate is the repository test script:

```powershell
./test.ps1
```

Do not claim completion from a focused test run alone.

---

### Task 0: Add a bounded STA WinForms message-loop test host

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/WinFormsMessageLoopTestHost.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/WinFormsMessageLoopTestHostTests.cs`
- Reuse without weakening: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/WinFormsTestEnvironment.cs`

**Consumes:** existing `WinFormsTestEnvironment` fail-fast unhandled-exception configuration, NUnit.

**Produces:** one reusable test helper that runs real WinForms interaction work on a dedicated STA UI thread with a running message pump, propagates UI exceptions back to NUnit, shuts down deterministically, and has a bounded timeout.

- [x] **Step 1: Write failing host tests.** Require: work runs on `ApartmentState.STA`; `SynchronizationContext.Current`/message dispatch is available after loop startup; `BeginInvoke` completes; exceptions thrown by UI work are rethrown on the calling test thread with the original exception preserved; disposal terminates the UI thread; and a deliberately non-completing action triggers the host timeout instead of hanging the test process.
- [x] **Step 2: Implement the minimal host around a dedicated STA `Thread` plus `Application.Run(ApplicationContext)`.** Signal readiness only after the UI thread has initialized its message loop. Marshal work onto the UI thread with `Control.BeginInvoke`/equivalent WinForms dispatch, capture exceptions with `ExceptionDispatchInfo`, and provide a bounded synchronous `Run(Action)` / `Run<T>(Func<T>)` API suitable for NUnit tests.
- [x] **Step 3: Ensure shutdown is deterministic.** `Dispose` must request `ApplicationContext.ExitThread()` on the UI thread, join with a bounded timeout, and fail the test rather than leave an orphan UI thread when shutdown does not complete.
- [x] **Step 4: Verify the host does not replace `WinFormsTestEnvironment`.** Global `Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException, threadScope: false)` remains configured by the existing fixture before GUI scenarios.
- [x] **Step 5: Run the infrastructure tests with hang protection.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --filter "FullyQualifiedName~WinFormsMessageLoopTestHost" `
  --blame-hang --blame-hang-timeout 2m
```

Expected: PASS with no dialog and no orphan process/thread.

- [x] **Step 6: Commit the test infrastructure.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/WinFormsMessageLoopTestHost.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Infrastructure/WinFormsMessageLoopTestHostTests.cs
git commit -m "test: add WinForms message loop host"
```

---

### Task 1: Extract the shared ToolStrip rendering core without changing `BootstrapDropdown`

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripSurfaceKind.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripRenderLogic.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripRendererBase.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripRenderer.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs`
- Modify only if required for constructor/type wiring: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToolStripRenderLogicTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

**Consumes:** existing `BootstrapDropdownRenderer`, `BootstrapTheme`, `BootstrapThemeMetrics`, `BootstrapVariantColorResolver`, `DpiScaler`, `ColorUtil`.

**Produces:** one reusable ToolStrip rendering engine with `BootstrapDropdownRenderer` delegating to it.

- [x] **Step 1: Add failing pure render-logic tests.** Cover 96/144/192 DPI metric scaling; surface-kind palette differences; disabled/pressed/checked/selected precedence; variant accent resolution; horizontal and vertical separator geometry; check/arrow geometry; split-button divider/arrow geometry derived from supplied native rectangles; and dark/light theme colors.

```csharp
[TestCase(96)]
[TestCase(144)]
[TestCase(192)]
public void ResolveMetrics_scales_all_custom_geometry_from_dpi(int dpi)
{
    var result = BootstrapToolStripRenderLogic.ResolveMetrics(
        BootstrapTheme.CreateDefault(BootstrapThemeMode.Light).Metrics,
        dpi);

    Assert.That(result.BorderWidth, Is.GreaterThan(0));
    Assert.That(result.ArrowSize, Is.GreaterThan(0));
    Assert.That(result.SeparatorInset, Is.GreaterThanOrEqualTo(0));
}
```

- [x] **Step 2: Add orientation regression tests before extracting the current separator code.** Construct render-logic inputs for `vertical: false` and `vertical: true`; assert horizontal separators vary X while holding Y constant, and vertical separators vary Y while holding X constant. This prevents the existing dropdown-only horizontal assumption from leaking into vertical `BootstrapToolStrip`.
- [x] **Step 3: Add a `BootstrapDropdown` regression test before refactoring.** Create a dropdown with normal, selected, disabled, checked, separator, and nested items and assert that the renderer remains custom, its variant is propagated, and its resolved palette/metrics match the pre-refactor behavior.
- [x] **Step 4: Run the focused tests and verify the new render-logic tests fail because the shared types do not exist.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --filter "FullyQualifiedName~BootstrapToolStripRenderLogicTests|FullyQualifiedName~BootstrapDropdownTests" `
  --blame-hang --blame-hang-timeout 5m
```

Expected: new tests fail to compile or fail assertions while the existing dropdown regression remains green before moving logic.

- [x] **Step 5: Implement `BootstrapToolStripSurfaceKind` and pure `BootstrapToolStripRenderLogic`.** Move palette/metrics/state calculations out of `BootstrapDropdownRenderer`; use `BootstrapVariantColorResolver`, `ColorUtil`, and `DpiScaler`; no `Control` handles inside pure calculations. Include orientation-aware separator geometry and split-button helper geometry that accepts already-resolved native rectangles rather than calculating layout ownership.
- [x] **Step 6: Implement `BootstrapToolStripRendererBase : ToolStripRenderer`.** Move common drawing overrides from `BootstrapDropdownRenderer` into the base and add button, split-button, grip, overflow, status sizing-grip, and `OnRenderToolStripStatusLabelBackground` hooks needed by the new family.
- [x] **Step 7: Implement split-button painting correctly.** In `OnRenderSplitButtonBackground`, inspect `ToolStripSplitButton.ButtonBounds` and `DropDownButtonBounds`; paint main/drop-down portions from their native states, draw the divider, and draw the arrow there. Do not expect `OnRenderArrow` to be invoked separately for the split-button arrow.
- [x] **Step 8: Implement `BootstrapToolStripRenderer` as the concrete renderer used by the four new controls.** Keep variant mutable internally so the appearance controller can update it without replacing renderer instances.
- [x] **Step 9: Reduce `BootstrapDropdownRenderer` to a thin adapter over the shared base.** Remove duplicate drawing/palette logic only after the shared tests cover it. Do not change `BootstrapDropdown` public API or popup ownership.
- [x] **Step 10: Re-run focused render/dropdown tests and verify they pass.**
- [x] **Step 11: Commit the rendering extraction.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownRenderer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToolStripRenderLogicTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
git commit -m "refactor: share ToolStrip rendering infrastructure"
```

---

### Task 2: Add shared appearance ownership and `BootstrapToolStrip`

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripAppearanceController.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToolStrip.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToolStripTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToolStripInteractionTests.cs`

**Consumes:** Task 0 message-loop host, Task 1 shared renderer, current theme/font ownership conventions, native `ToolStrip`.

**Produces:** first public native-backed ToolStrip control and the appearance controller reused by Tasks 3–5.

- [x] **Step 1: Write failing contract tests for the appearance controller and public control.** Assert direct inheritance, default `Variant.Primary`, a framework custom renderer at construction, native `Items`, and no mutation of `ToolStripManager.Renderer`.
- [x] **Step 2: Add failing theme/font ownership tests.** Verify theme changes invalidate and update framework-owned typography; a caller-assigned font survives later theme changes; disposing the control unsubscribes the theme handler and does not dispose the caller font.
- [x] **Step 3: Add failing renderer-ownership tests.** Assign a caller renderer after construction, change theme and variant, and verify the framework never reinstalls or disposes the caller renderer.
- [x] **Step 4: Add failing ToolStrip behavior tests.** Cover button click, `CheckOnClick`, drop-down button opening, split-button main/drop-down action separation, item enable/disable, tooltip/native image preservation, horizontal/vertical layout, and dynamic item add/remove. Use `WinFormsMessageLoopTestHost` for tests that actually open a drop-down.
- [x] **Step 5: Add split-button render assertions.** Render a `ToolStripSplitButton` in LTR and RTL and assert the framework paints a visible divider plus arrow inside the native `DropDownButtonBounds`; verify main and drop-down hot/pressed states can differ without altering native bounds.
- [x] **Step 6: Add horizontal/vertical separator render assertions.** A vertical ToolStrip must receive a vertical separator; a horizontal ToolStrip/drop-down must receive a horizontal separator.
- [x] **Step 7: Add a native-overflow characterization test.** Use a constrained-width real ToolStrip handle with enough items to force overflow inside `WinFormsMessageLoopTestHost`; verify native overflow creation and item ownership work before any Bootstrap-specific assertion.
- [x] **Step 8: Run the focused tests and confirm failure before implementation.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --filter "FullyQualifiedName~BootstrapToolStripTests|FullyQualifiedName~BootstrapToolStripInteractionTests" `
  --blame-hang --blame-hang-timeout 5m
```

- [x] **Step 9: Implement `BootstrapToolStripAppearanceController`.** Own only the framework renderer, `Variant`, theme subscription, framework font, created/open child-surface tracking needed for repaint/lifecycle, invalidation, and disposal. Observe caller `FontChanged` and renderer replacement without taking ownership of caller objects.
- [x] **Step 10: Implement `BootstrapToolStrip : ToolStrip`.** Keep the class thin: construct the appearance controller, forward `Variant`, preserve all inherited behavior, and dispose the controller from `Dispose(bool)`.
- [x] **Step 11: Make the shared renderer cover toolbar button/drop-down/split/grip/overflow/separator states using the already-tested pure logic.** Do not change native item layout rectangles.
- [x] **Step 12: Verify overflow is still native and the overflow surface receives Bootstrap painting without a custom overflow implementation.**
- [x] **Step 13: Re-run the Task 2 tests and Task 1 regression tests.**
- [x] **Step 14: Commit `BootstrapToolStrip`.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripAppearanceController.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToolStrip.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToolStripTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToolStripInteractionTests.cs
git commit -m "feat: add BootstrapToolStrip"
```

---

### Task 3: Add `BootstrapMenuStrip` with native menu, shortcut, and merge semantics

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapMenuStrip.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapMenuStripTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapMenuStripInteractionTests.cs`
- Modify only if renderer propagation or created/open-surface tracking requires shared wiring: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripAppearanceController.cs`

**Consumes:** Task 0 message-loop host, shared appearance controller and renderer; native `MenuStrip` / `ToolStripMenuItem`.

**Produces:** Bootstrap-themed native menu bar with nested drop-down continuity.

- [x] **Step 1: Write failing public contract tests.** Assert direct `MenuStrip` inheritance, default variant, native `Items`, standard `ToolStripMenuItem` compatibility, and custom Bootstrap renderer installation.
- [x] **Step 2: Write message-loop-backed interaction tests for mnemonics and shortcuts.** Host the menu in a real `Form` inside `WinFormsMessageLoopTestHost`; verify menu-item click events, Alt/mnemonic activation, `ShortcutKeys`, check state, disabled items, and nested menu click behavior remain native. Do not synthesize these behaviors by calling framework-private methods.
- [x] **Step 3: Characterize renderer propagation into first-level and nested native drop-downs.** Open a top-level menu through the message loop, inspect the actual created `ToolStripDropDown`, open a child submenu, and record whether WinForms already uses the owner renderer.
- [x] **Step 4: Separately characterize repaint tracking requirements.** With a real first-level and nested drop-down open, change `Variant` and Light/Dark theme and verify the visible popup surfaces repaint without close/reopen. This requirement applies even if native renderer propagation itself needs no custom assignment code.
- [x] **Step 5: Lock renderer continuity without conflating it with lifecycle tracking.** Bootstrap-owned dropdowns use the shared renderer/current variant; caller-custom-rendered dropdowns keep their explicit renderer. If native propagation is sufficient, add no renderer-assignment code. Independently retain only the event-driven created/open-surface tracking required for repaint and cleanup.
- [x] **Step 6: Add renderer opt-out descendant regression.** Open a submenu once, close it, replace the root `Renderer` with a sentinel caller renderer, mutate theme/variant, and open/navigate the submenu again. Verify the framework does not force its renderer back onto root or descendant surfaces and does not dispose the sentinel.
- [x] **Step 7: Write a native MDI/merge characterization and regression test.** Use two `BootstrapMenuStrip` instances with `AllowMerge`, `MergeAction`, and `MergeIndex`; exercise `ToolStripManager.Merge`/revert or the existing WinForms MDI pattern and prove that styling does not alter native item identity or merge order.
- [x] **Step 8: Add RTL/right-alignment rendering tests.** Use event/native item bounds and verify no hard-coded LTR arrow/check/split assumptions leak into the shared renderer.
- [x] **Step 9: Run failing tests.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --filter "FullyQualifiedName~BootstrapMenuStrip" `
  --blame-hang --blame-hang-timeout 5m
```

- [x] **Step 10: Implement `BootstrapMenuStrip : MenuStrip` as a thin appearance-controller host.** Do not override keyboard processing or menu activation unless a failing native-compatibility test proves a rendering-only override cannot satisfy the requirement.
- [x] **Step 11: Extend only shared infrastructure needed for top-level-menu active state, renderer propagation when native propagation is insufficient, and created/open-surface repaint tracking.** The opened top-level item must remain visibly active while its native dropdown is open.
- [x] **Step 12: Re-run MenuStrip, ToolStrip, message-loop-host, and BootstrapDropdown regression tests.**
- [x] **Step 13: Commit `BootstrapMenuStrip`.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapMenuStrip.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripAppearanceController.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapMenuStripTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapMenuStripInteractionTests.cs
git commit -m "feat: add BootstrapMenuStrip"
```

---

### Task 4: Add `BootstrapContextMenuStrip` while preserving native popup ownership and lifecycle

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapContextMenuStrip.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapContextMenuStripTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapContextMenuStripInteractionTests.cs`

**Consumes:** Task 0 message-loop host, native `ContextMenuStrip`, shared appearance controller/renderer.

**Produces:** Bootstrap-themed context menu with native placement, ownership, event order, and designer container lifetime.

- [x] **Step 1: Write failing constructor/designer contract tests.** Cover both parameterless construction and `BootstrapContextMenuStrip(IContainer)`; disposing the container must dispose the menu using native component semantics.
- [x] **Step 2: Write message-loop-backed tests for `SourceControl`.** Assign one menu to two controls, show it for each owner in turn, and verify native `SourceControl` reports the current source without framework caching.
- [x] **Step 3: Write message-loop-backed cancelable lifecycle tests.** Record `Opening`, `Opened`, `Closing`, and `Closed`; verify canceling `Opening` prevents display and canceling `Closing` follows the native contract. The Bootstrap control must not invent extra open/close events.
- [x] **Step 4: Write nested menu/check/shortcut tests.** Use images, checked items, disabled items, separators, nested menu items, and access keys; ensure the shared renderer changes presentation only.
- [x] **Step 5: Write theme/variant-change-while-open regression.** Open the real context menu through `WinFormsMessageLoopTestHost`, switch `BootstrapThemeManager.CurrentTheme` and `Variant`, force/await repaint, and verify the same active native drop-down resolves the new palette without closing/recreating the menu.
- [x] **Step 6: Run tests and verify they fail before the class exists.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --filter "FullyQualifiedName~BootstrapContextMenuStrip" `
  --blame-hang --blame-hang-timeout 5m
```

- [x] **Step 7: Implement `BootstrapContextMenuStrip : ContextMenuStrip`.** Forward the container constructor to `base(container)`, create the appearance controller in both constructors through one initialization path, forward `Variant`, and dispose shared appearance state safely.
- [x] **Step 8: Do not add placement, ownership, or keyboard overrides.** Any code beyond theme/renderer lifecycle must be justified by a failing regression test and remain inside shared ToolStrip infrastructure where possible.
- [x] **Step 9: Re-run context-menu tests plus MenuStrip/Dropdown/message-loop regressions.**
- [x] **Step 10: Commit `BootstrapContextMenuStrip`.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapContextMenuStrip.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapContextMenuStripTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapContextMenuStripInteractionTests.cs
git commit -m "feat: add BootstrapContextMenuStrip"
```

---

### Task 5: Add `BootstrapStatusStrip` with Spring, status-label border, and sizing-grip compatibility

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapStatusStrip.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapStatusStripTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapStatusStripInteractionTests.cs`
- Modify as required for status rendering only: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripRendererBase.cs`
- Modify as required for pure status metrics only: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripRenderLogic.cs`

**Consumes:** native `StatusStrip`, `ToolStripStatusLabel`, `ToolStripDropDownButton`, `ToolStripSplitButton`, `ToolStripProgressBar`.

**Produces:** themed status bar without replacing native table layout, caller-configured status-label borders, or hosted controls.

- [x] **Step 1: Write failing public contract tests.** Assert direct native inheritance, standard item types, default variant, and Bootstrap renderer.
- [x] **Step 2: Write a constrained-width Spring regression.** Add left/right labels plus one `ToolStripStatusLabel { Spring = true }`, resize the containing form, and assert the spring label receives native remaining width rather than a framework-computed width.
- [x] **Step 3: Add status-label border compatibility tests.** Create labels covering `BorderSides.None`, one side, multiple sides, and representative `Border3DStyle` values. Render through the Bootstrap renderer and assert requested sides remain present while non-requested sides are not invented. The test must fail if custom `OnRenderToolStripStatusLabelBackground` merely fills the background and drops the native border contract.
- [x] **Step 4: Write sizing-grip rendering/behavior tests.** Toggle `SizingGrip`; verify the renderer draws only when enabled and uses DPI-scaled themed geometry. Do not replace native sizing hit behavior.
- [x] **Step 5: Add interactive status-item tests.** Exercise a status drop-down button, split button, tooltip, disabled item, and checked/pressed states where supported.
- [x] **Step 6: Add hosted progress-bar compatibility.** Insert `ToolStripProgressBar`, change value/style, resize the strip, and prove the Bootstrap renderer does not take over the hosted control's native value/editing/render lifecycle.
- [x] **Step 7: Run focused tests and confirm failure before implementation.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --filter "FullyQualifiedName~BootstrapStatusStrip" `
  --blame-hang --blame-hang-timeout 5m
```

- [x] **Step 8: Implement `BootstrapStatusStrip : StatusStrip` as a thin appearance-controller host.** Preserve all native defaults unless a visual-only adjustment is explicitly tested.
- [x] **Step 9: Implement status-specific shared renderer hooks.** Paint top border, `OnRenderToolStripStatusLabelBackground`, interactive item states, and sizing grip from shared theme/DPI logic; preserve `ToolStripStatusLabel.BorderSides` and `BorderStyle`; do not compute Spring layout.
- [x] **Step 10: Re-run StatusStrip, ToolStrip, MenuStrip, ContextMenuStrip, and BootstrapDropdown focused suites.**
- [x] **Step 11: Commit `BootstrapStatusStrip`.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapStatusStrip.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripRendererBase.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapStatusStripTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapStatusStripInteractionTests.cs
git commit -m "feat: add BootstrapStatusStrip"
```

---

### Task 6: Harden the whole family for renderer opt-out, overflow, nested menus, merge, DPI, RTL, and lifecycle

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToolStripFamilyRegressionTests.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToolStripFamilyDpiTests.cs`
- Modify only for defects proven by these tests:
  - `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripAppearanceController.cs`
  - `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripRendererBase.cs`
  - `src/MyDmsVn.Bootstrap5WinFormUI/Controls/Internal/BootstrapToolStripRenderLogic.cs`
  - one or more of the four new public controls

**Consumes:** completed control family and Task 0 message-loop host.

**Produces:** cross-control compatibility evidence and regression protection.

- [ ] **Step 1: Add a four-control theme-switch test.** Place all four controls on/under one form inside `WinFormsMessageLoopTestHost`, open a menu/context surface, change Light -> Dark -> Light, and verify each still uses the same native items and current shared palette without closing the open surface.
- [ ] **Step 2: Add caller-renderer opt-out coverage for every public control.** Assign a sentinel renderer, mutate theme and `Variant`, and verify its reference survives unchanged.
- [ ] **Step 3: Add descendant opt-out coverage.** Create/open nested surfaces under a Bootstrap renderer, close them, replace the root renderer with a sentinel, then reopen/navigate. Verify no framework renderer is force-reinstalled on root/descendants and no caller renderer is disposed.
- [ ] **Step 4: Add caller-font ownership coverage for every public control.** Verify no theme switch disposes or replaces a caller font.
- [ ] **Step 5: Add dynamic-item coverage.** Add/remove/reinsert toolbar and menu items after handle creation; add nested menu items after the first menu opening; ensure no stale drop-down tracking, stale renderer subscriptions, or duplicate event handlers.
- [ ] **Step 6: Add overflow regression at 96/144/192 logical DPI scenarios.** The test should prove overflow item ownership/order remains native while custom overflow affordances scale from `DpiScaler`.
- [ ] **Step 7: Add horizontal/vertical separator DPI regression.** At 96/144/192 logical DPI, separator orientation remains correct and inset/stroke scale through shared render logic.
- [ ] **Step 8: Add split-button DPI/RTL regression.** At representative DPI values and both LTR/RTL, divider and arrow stay inside native `DropDownButtonBounds` and the renderer never invents alternative split rectangles.
- [ ] **Step 9: Add status-label border regression.** Theme and variant changes must not remove caller `BorderSides`/`BorderStyle`.
- [ ] **Step 10: Add menu merge/revert regression.** Verify merge does not duplicate framework event subscriptions, does not transfer renderer ownership incorrectly, and reversion restores native item ownership/order.
- [ ] **Step 11: Add dispose/handle-recreation regression.** Create/dispose/recreate handles, show/hide forms, and verify no `ObjectDisposedException`, static theme-event leak, orphan message-loop work, or stale drop-down reference.
- [ ] **Step 12: Add RTL screenshots/render assertions where deterministic.** At minimum cover submenu arrows, checks, separators, split buttons, overflow, and status grip without assuming left-side coordinates.
- [ ] **Step 13: Run the entire family plus dropdown suite.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --filter "FullyQualifiedName~BootstrapToolStrip|FullyQualifiedName~BootstrapMenuStrip|FullyQualifiedName~BootstrapContextMenuStrip|FullyQualifiedName~BootstrapStatusStrip|FullyQualifiedName~BootstrapDropdown|FullyQualifiedName~WinFormsMessageLoopTestHost" `
  --blame-hang --blame-hang-timeout 5m
```

- [ ] **Step 14: Fix only failures demonstrated by tests and keep fixes in shared infrastructure whenever the behavior is common.**
- [ ] **Step 15: Commit hardening.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToolStripFamilyRegressionTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToolStripFamilyDpiTests.cs
git commit -m "test: harden Bootstrap ToolStrip family"
```

---

### Task 7: Add integrated demo coverage and document the public contracts

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ToolStripFamilyDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/ToolStripFamilyDemoFormTests.cs`
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify if control inventory is maintained there: `README.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`

**Consumes:** finished control family and integrated demo navigation.

**Produces:** discoverable demo, documented ownership boundaries, and public API baseline coverage.

- [ ] **Step 1: Write a failing demo contract test.** Require one integrated page that creates all four controls and can be constructed/disposed repeatedly under STA without leaking theme subscriptions. Any test that opens demo menus/drop-downs must use `WinFormsMessageLoopTestHost`.
- [ ] **Step 2: Implement `ToolStripFamilyDemoForm`.** Demonstrate at minimum:
  - a `BootstrapMenuStrip` with mnemonics, keyboard shortcut, checked item, disabled item, separator, nested submenu, and a runtime theme/variant-visible state;
  - a `BootstrapToolStrip` with normal button, checked button, drop-down button, split button, vertical/horizontal separator examples where practical, tooltip, native image, and a constrained-width overflow section;
  - a `BootstrapContextMenuStrip` assigned to a visible content panel with checked/disabled/nested items and `Opening` feedback;
  - a `BootstrapStatusStrip` with left label, Spring label, right label, at least one label demonstrating `BorderSides`/`BorderStyle`, drop-down/split command, progress bar, and sizing grip;
  - a small event log showing native click/open/close/source-control behavior without modal dialogs.
- [ ] **Step 3: Add one integrated-demo navigation entry in `MainForm.ConfigurePages()`.** Suggested title: `Menus / ToolStrips`; description should explicitly mention MenuStrip, ContextMenuStrip, ToolStrip, StatusStrip, shortcuts, overflow, and native behavior.
- [ ] **Step 4: Add demo tests for repeated construction/disposal, theme switching, and navigation registration.**
- [ ] **Step 5: Document all four controls in `docs/COMPONENTS.md`.** Include direct native base type, `Variant`, native-authoritative APIs, renderer opt-out behavior, hosted-control limitation, split-button renderer ownership, orientation-aware separators, and StatusStrip border preservation.
- [ ] **Step 6: Update `docs/ARCHITECTURE.md`.** Record `BootstrapToolStripRendererBase`/appearance controller as shared infrastructure; state that `BootstrapDropdown` and the ToolStrip family intentionally share the same rendering system; and document the distinction between native renderer propagation and framework tracking of already-created/open surfaces for repaint/lifecycle only.
- [ ] **Step 7: Update README control inventory if it currently lists implemented controls.** Do not duplicate full component documentation there.
- [ ] **Step 8: Extend `Phase16PublicApiBaselineTests` for the four public classes and `Variant` property, including the `BootstrapContextMenuStrip(IContainer)` constructor.** Internal renderer/helper/test-host types must not leak into the public library API baseline.
- [ ] **Step 9: Run demo/public API focused tests.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --filter "FullyQualifiedName~ToolStripFamilyDemoFormTests|FullyQualifiedName~Phase16PublicApiBaselineTests" `
  --blame-hang --blame-hang-timeout 5m
```

- [ ] **Step 10: Manually inspect the demo in Light and Dark themes at 100%, 150%, and 200% Windows scaling.** Verify no clipped checks/arrows/text, split-button divider/arrow correctness, no stale menu after application deactivation, no overflow placement defect, separator orientation correctness, no status Spring or status-label-border regression, and no theme mismatch between owner and opened submenu.
- [ ] **Step 11: Commit demo/docs/API baseline.**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ToolStripFamilyDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/ToolStripFamilyDemoFormTests.cs docs/COMPONENTS.md docs/ARCHITECTURE.md README.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs
git commit -m "docs: document Bootstrap ToolStrip family"
```

---

### Task 8: Final compatibility and repository verification

**Files:** no new production file is expected. Modify only files required by a demonstrated failure.

**Consumes:** all previous tasks.

**Produces:** fresh build/test evidence for both target frameworks and a final implementation audit against this plan.

- [ ] **Step 1: Re-read this plan and make a requirement checklist.** Confirm every Global Constraint, public contract, native behavior contract, shared rendering hook, test-host rule, theme/font ownership rule, renderer-propagation/lifecycle distinction, and demo requirement has either test coverage or an explicit documented limitation.
- [ ] **Step 2: Scan the implementation for forbidden architecture.** There must be no custom popup Form, global hook, `ToolStripManager.Renderer` mutation, parallel item model, hand-built overflow engine, status Spring width calculation, caller-resource disposal, framework-forced renderer reassignment, hard-coded split-button layout, horizontal-only separator implementation, or stale child-drop-down tracking.
- [ ] **Step 3: Scan for placeholders.** Search changed code/docs for `TODO`, `TBD`, `FIXME`, `NotImplementedException`, and comments deferring required behavior.

```powershell
git grep -n -E "TODO|TBD|FIXME|NotImplementedException" -- src/MyDmsVn.Bootstrap5WinFormUI demo/MyDmsVn.Bootstrap5WinFormUI.Demo tests/MyDmsVn.Bootstrap5WinFormUI.Tests docs
```

Review matches; pre-existing unrelated matches are not failures, but no new required behavior may be left as a placeholder.

- [ ] **Step 4: Run the full repository verification script.**

```powershell
./test.ps1
```

Expected: zero failing tests and zero hangs.

- [ ] **Step 5: Build both library targets explicitly if `test.ps1` does not already provide clear per-target evidence.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net48
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -f net8.0-windows
```

Expected: both builds exit 0.

- [ ] **Step 6: Inspect final diff/stat and verify scope.**

```powershell
git status --short
git diff --stat HEAD~1..HEAD
```

If implementation is delivered as multiple commits, compare against the implementation branch base instead of `HEAD~1`.

- [ ] **Step 7: Commit any final verified corrections, then rerun the exact verification command affected by those corrections before claiming completion.**

---

## Acceptance Criteria

The implementation is complete only when all of the following are true:

- `BootstrapToolStrip`, `BootstrapMenuStrip`, `BootstrapContextMenuStrip`, and `BootstrapStatusStrip` exist as direct native subclasses and expose the agreed small public API.
- Native WinForms item collections and standard `ToolStripItem` types remain authoritative; no parallel item model exists.
- All four controls use one shared Bootstrap rendering infrastructure by default.
- `BootstrapDropdownRenderer` shares that same rendering core and existing `BootstrapDropdown` tests remain green.
- Theme Light/Dark and semantic `Variant` changes repaint normal and already-open Bootstrap-owned surfaces correctly without requiring reopen.
- Renderer propagation uses native WinForms behavior where sufficient, while already-created/open-surface tracking remains a separate, narrow, event-driven repaint/lifecycle mechanism.
- Caller-assigned `Font` and `Renderer` survive later theme changes and are never disposed by the framework; root renderer opt-out does not cause framework renderer reassignment to descendants.
- ToolStrip overflow remains native and works under constrained width.
- Horizontal and vertical ToolStrip separators render in the native orientation with DPI-aware geometry.
- `ToolStripSplitButton` keeps native main/drop-down action behavior and native bounds while Bootstrap painting includes its divider and arrow in LTR and RTL.
- MenuStrip shortcuts, mnemonics, checks, nested menus, and merge/MDI behavior remain native.
- ContextMenuStrip placement, `SourceControl`, `Opening`/`Closing`, and nested menus remain native.
- StatusStrip Spring sizing, sizing grip, status labels, caller-configured `BorderSides`/`BorderStyle`, native progress bar, and interactive status items remain native.
- Renderer geometry is DPI-aware and RTL-safe; no hard-coded LTR layout assumptions are introduced.
- Theme subscriptions, child-drop-down tracking subscriptions, and framework-owned fonts/resources are disposed deterministically.
- GUI interaction tests that require real dispatch use the bounded STA `WinFormsMessageLoopTestHost`; automated GUI tests cannot hang indefinitely on unexpected dialogs or missing message pumps.
- Integrated demo coverage exists and is navigable from `MainForm`.
- Public API baseline and component/architecture documentation are updated.
- `./test.ps1` passes and both `net48` and `net8.0-windows` library builds have fresh success evidence.

---

## Explicitly Out of Scope for v1

- Custom Bootstrap-specific public ToolStrip item subclasses.
- Replacing native menu keyboard navigation, mnemonic processing, shortcut routing, or accessibility objects.
- Animated menu opening/closing.
- Rounded popup window regions or custom drop shadows.
- A process-global Bootstrap `ToolStripManager.Renderer` mode.
- Automatic conversion of ordinary `ToolStrip`/`MenuStrip`/`StatusStrip` instances into Bootstrap controls.
- Deep custom styling of `ToolStripTextBox`, `ToolStripComboBox`, or `ToolStripProgressBar` interiors.
- Custom toolbar persistence/customization UI, command routing framework, ribbon behavior, or docking framework.
- Owner-defined menu search, command palette, or virtualized menu item model.

These can be planned separately only after the native-backed v1 family is stable.

---

## Reference Links

Use these only to confirm native contracts during implementation; repository tests remain the executable source of truth for compatibility:

- ToolStripRenderer: https://learn.microsoft.com/dotnet/api/system.windows.forms.toolstriprenderer
- ToolStrip rendering architecture: https://learn.microsoft.com/dotnet/desktop/winforms/controls/how-to-set-the-toolstrip-renderer-at-run-time
- ToolStrip overview: https://learn.microsoft.com/dotnet/desktop/winforms/controls/toolstrip-control-windows-forms
- ContextMenuStrip: https://learn.microsoft.com/dotnet/api/system.windows.forms.contextmenustrip
- StatusStrip: https://learn.microsoft.com/dotnet/api/system.windows.forms.statusstrip
- ToolStripStatusLabel.Spring: https://learn.microsoft.com/dotnet/api/system.windows.forms.toolstripstatuslabel.spring
- ToolStrip overflow: https://learn.microsoft.com/dotnet/desktop/winforms/controls/how-to-manage-toolstrip-overflow
- ToolStripManager.Merge: https://learn.microsoft.com/dotnet/api/system.windows.forms.toolstripmanager.merge
