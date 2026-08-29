# Architecture

## 1. Architectural style

The framework is a layered native WinForms library with a small set of foundation services and reusable controls. The design intentionally favors explicit composition, simple ownership, and dependency direction over a deep inheritance hierarchy.

A base control abstraction may be introduced where it removes repeated theme/lifecycle plumbing, but the project must not create a large custom control hierarchy merely for conceptual purity.

## 2. Proposed solution structure

```text
/src
  /MyDmsVn.Bootstrap5WinFormUI
    /Theme
    /Animation
    /Icons
    /Rendering
    /Compatibility
    /Controls
      /Buttons
      /Inputs
      /Containers
      /Navigation
      /Feedback
      /Data
    /Internal

  /MyDmsVn.Bootstrap5WinFormUI.FontAwesome       (optional adapter, if needed)
  /MyDmsVn.Bootstrap5WinFormUI.Svg               (optional adapter, if renderer dependency warrants separation)

/tests
  /MyDmsVn.Bootstrap5WinFormUI.Tests

/demo
  /MyDmsVn.Bootstrap5WinFormUI.Demo
```

Do not create adapter projects until a concrete dependency requires the separation. The architecture permits them; it does not require premature packaging.

## 3. Namespace model

```text
MyDmsVn.Bootstrap5WinFormUI.Theme
MyDmsVn.Bootstrap5WinFormUI.Animation
MyDmsVn.Bootstrap5WinFormUI.Icons
MyDmsVn.Bootstrap5WinFormUI.Rendering
MyDmsVn.Bootstrap5WinFormUI.Compatibility
MyDmsVn.Bootstrap5WinFormUI.Controls
```

Control-specific child namespaces may be introduced if the namespace remains discoverable. Avoid mirroring every folder with an excessively deep namespace.

## 4. Dependency graph

```text
                 Compatibility
                     / | \
                    /  |  \
                 Theme | Rendering
                    \  |  /
                     Icons
                       |
                   Animation
                       |
        +--------------+--------------+
        |              |              |
      Spinner        Button        Collapse
        |              |              |
   Button.Loading   ButtonGroup     Accordion
                       |              |
                 ButtonToolbar   AccordionHeader
                       |
                  Pagination

 TextBox -----+
 NumericBox --+---- native NumericUpDown + Theme / Rendering
 ComboBox ----+---- native ComboBox + Theme / Rendering / Icons
 Dropdown ----+---- BootstrapButton target + native ToolStripDropDownMenu
 Card --------+---- shared Theme / Rendering / Icons
 Progress ----+---- Animation / LoopAnimation
 Sidebar -----+---- Button / Collapse / Icons
 DataGrid ----+---- Theme / Spinner (optional loading overlay)
 Pagination -+---- ButtonGroup / Button only; caller owns data paging
```

The diagram shows conceptual dependencies, not necessarily direct assembly references for every line.

`BootstrapNumericBox` deliberately depends only on the native WinForms `NumericUpDown` plus the existing Theme/Rendering foundation. It has no dependency on Tabs, ComboBox, Dropdown, custom popup infrastructure, or a framework-owned numeric parser. Later advanced-input controls may share the integrated demo page but not hidden implementation dependencies.

`BootstrapComboBox` is likewise shallow: it derives directly from native WinForms `ComboBox` and depends only on the existing Theme, Rendering, DPI, and source-neutral Icons infrastructure. It does **not** depend on `BootstrapNumericBox`, `BootstrapDropdown`, a custom popup host, or a framework item/binding model. Stage 7 `BootstrapDropdown` is a separate component and is not an implementation dependency of ComboBox.

`BootstrapDropdown` is a native command-popup composition. It depends on a caller-owned `BootstrapButton` only as target/anchor and as the source of `IIconRenderer`; it owns exactly one native `ToolStripDropDownMenu` plus one internal `BootstrapDropdownRenderer`. It does not replace ComboBox popup behavior, create a custom top-level form, install global hooks, or introduce a second message loop.

Tooltip and Popover share one pure overlay placement foundation:

```text
Controls (BootstrapTooltip / BootstrapPopover)
        +--> Rendering/BootstrapOverlayPlacementEngine
        +--> existing Theme + Rendering + DPI helpers

BootstrapPopover only
        +--> internal ToolStripDropDown overlay host
```

`BootstrapOverlayPlacementEngine` consumes only screen-pixel geometry, placement/collision values, and RTL state. It cannot depend on concrete controls, `Screen`, themes, handles, or DPI scaling. Tooltip keeps one native `ToolTip`; only Popover uses the internal interactive host.

## 5. Foundation responsibilities

### 5.1 Compatibility

Contains the smallest possible helpers needed to keep one codebase working across `net48` and `net8.0-windows`, for example clamp helpers or target-specific wrappers.

It must not become a dumping ground for general utilities.

### 5.2 Theme

Owns semantic colors, typography, metrics, theme mode, reduced-motion preference, and change notifications.

Recommended concepts:

```text
BootstrapTheme
BootstrapThemeColors
BootstrapThemeMetrics
BootstrapThemeTypography
BootstrapThemeMode
BootstrapThemeManager
```

`BootstrapThemeManager` owns the application-level current theme and emits a theme-changed notification. Controls subscribe through a reusable lifecycle mechanism and unsubscribe on disposal.

The implementation may retain the shorter historical name `AppTheme` only if it is deliberately selected during the public API review. Do not expose both names for the same concept.

### 5.3 Rendering

Owns reusable painting calculations and drawing helpers:

- Rounded paths/per-corner radii
- Stroke/bounds normalization
- Color transforms and contrast selection
- Text/icon/content layout
- DPI scaling helpers
- Double-buffer enabling when needed

Rendering helpers should be stateless whenever possible.

### 5.4 Icons

Defines source-neutral icon descriptors/providers and rendering contracts.

A control asks to render an icon in a target rectangle and color. It should not branch on SVG vs MDL2 vs FontAwesome.

Suggested conceptual model:

```text
IconDescriptor
IIconProvider
IIconRenderer
IconSourceKind
```

Simple framework-owned glyphs, such as an accordion chevron, may use an internal vector path to avoid unnecessary external dependencies.

### 5.5 Animation

Owns shared animation timing and easing.

Required primitives:

```text
BootstrapAnimation       // finite transition
BootstrapLoopAnimation   // repeating transition
BootstrapEasing
```

Animations run on the UI thread. Consumers receive normalized progress and invalidate only the necessary control.

The initial implementation should prefer correctness and deterministic disposal. A central scheduler can replace per-animation timers later if profiling shows a real scaling need; controls must still consume the same animation abstraction so that implementation can evolve without API churn.

Phase 4 finalizes this foundation as follows:

- `BootstrapAnimation` is a finite transition exposing `Duration`, `Easing`, normalized eased `Progress`, `IsRunning`, `Start()`, `Stop()`, `Restart()`, `ProgressChanged`, and `Completed`.
- `BootstrapLoopAnimation` exposes the same run-control concepts without finite completion; its progress wraps once per configured cycle.
- `BootstrapEasing` provides Linear, quadratic EaseIn, EaseOut, and EaseInOut curves with normalized input/output.
- A WinForms timer is used only as a UI-thread frame wake-up source. Elapsed time is calculated from a monotonic stopwatch, so progress and total duration do not depend on timer tick count.
- Internal clock and scheduler contracts keep timing deterministic in tests and allow a future central scheduler without changing consumer APIs.
- An optional `Control` owner centralizes visibility/disposal handling. Hiding an owner pauses scheduling and preserves logical progress; showing it resumes without counting hidden wall-clock time; disposing it stops further scheduling.
- Reduced motion is evaluated when a run starts or restarts. Finite animation immediately publishes its final state and completion; loop animation remains at stable zero without continuous scheduling.
- Phase 4 deliberately does not introduce a central scheduler. Profiling may justify replacing the internal scheduler later, while the public animation API remains stable.

## 6. Control architecture

### 6.1 Primitive controls

Primitive controls own one interaction/rendering concept, for example Button, Spinner, TextBox, NumericBox, ComboBox, Card, Collapse, or ProgressBar.

They may use shared foundation layers but must not duplicate them.

`BootstrapNumericBox` is a composition primitive: the wrapper owns exactly one borderless native `NumericUpDown`, while the native editor remains the source of truth for numeric value, range normalization/rejection, increment, decimal places, thousands formatting, typed editing, spin buttons, Up/Down keys, and wheel behavior. The wrapper owns only the Bootstrap shell, validation/focus priority, DPI-scaled layout, one public tab stop, theme-aware font/color lifecycle, and forwarding of the native `ValueChanged` event.

This architecture intentionally avoids custom numeric parsing, a second value/range state model, a second spin implementation, custom popup/editor infrastructure, or a separate input-event pipeline.

`BootstrapComboBox` uses inheritance rather than composition because the native `ComboBox` already owns the complete public data/selection/edit/drop-down contract. Native `Items`, `DataSource`, `DisplayMember`, `ValueMember`, `SelectedIndex`, `SelectedItem`, `SelectedValue`, text editing, autocomplete, focus/keyboard behavior, `DropDown` / `DropDownClosed`, and selection events remain authoritative inherited behavior. The framework owns `OwnerDrawFixed` item/closed-selection presentation, validation/focus shell border, theme/DPI metrics, theme-owned font lifecycle, and optional `LeadingIcon` rendering through the existing `IIconRenderer`.

The ComboBox editable native child, arrow button, hit-testing, and popup chrome remain WinForms/OS-owned. The shell is a conservative post-native-paint border overlay; no child-window replacement, private-field reflection, global hook, window region, custom `Form`, `ToolStripDropDown`, or `ListBox` host is introduced. `BorderRadius` therefore describes best-effort framework-controlled shell geometry only and does not promise rounded native popup chrome.

### 6.2 Composite controls

Composite controls assemble primitives:

- ButtonGroup contains Buttons and owns grouping/selection rules.
- ButtonToolbar contains ButtonGroups and owns toolbar layout only.
- Accordion owns AccordionItems; each item combines a focusable header with Collapse behavior.
- Sidebar reuses Collapse/animation for expandable sections.
- DataGridView may compose a Spinner overlay for loading.
- Pagination owns one horizontal ButtonGroup and dynamically composes Buttons for First/Previous/numeric/ellipsis/Next/Last presentation. A pure internal helper computes the bounded numeric window; the public control owns page state only.

Composite controls should not reach into private rendering internals of primitives.

Pagination intentionally has no direct dependency on `BootstrapDataGridView` or any other data control. Applications retain responsibility for querying, slicing, virtualizing, and binding data after `PageChanged`. Pagination also has no custom painting, timer, animation engine, or direct theme subscription; appearance and DPI/theme response flow through the composed ButtonGroup/Button controls.

### 6.3 Native command-popup composition

`BootstrapDropdown` is a non-visual `Component`, not a replacement Button and not a custom popup window. The caller owns `Target : BootstrapButton` and every `BootstrapDropdownItem` model. The component owns the native popup infrastructure and never disposes the target or public item models.

The public `Items` collection is authoritative. Every effective `Show()` destroys the previous native snapshot and rebuilds short-lived `ToolStripMenuItem` / `ToolStripSeparator` instances from current model values. This snapshot-per-open boundary deliberately avoids live collection synchronization and avoids leaking native menu objects into the public API. Mutable `Text`, `Icon`, `Enabled`, `Checked`, and `Tag` values therefore apply coherently on the next opening.

Native `ToolStripDropDownMenu` remains authoritative for focus, Up/Down/Home/End/Enter/Escape behavior, mouse hit-testing, AutoClose, outside-click/focus-loss dismissal, message-loop integration, and working-area placement. Framework code only attaches/detaches the target Click/Disposed events, maps a native activation back to one enabled command model, forwards actual native `Opened` / `Closed` transitions, and owns presentation/resources.

The internal `BootstrapDropdownRenderer` resolves palette and metrics from the shared Theme/Rendering/DPI foundation. `Variant` influences selected/checked accent only; neutral surface/text/border remain theme tokens. `MinimumWidth` is a logical 96-DPI floor and native content measurement may exceed it. Optional menu icons are rasterized through `Target.IconRenderer` at the target's current DPI and theme text color. Generated bitmaps and native snapshot rows are component-owned resources and are disposed on refresh/rebuild/disposal.

Runtime theme changes do not rebuild caller state. If the popup is open, the component re-resolves presentation, regenerates owned icon bitmaps, and invalidates the native popup; when closed, the next snapshot naturally uses the current theme. Disposal removes the static theme subscription, target wiring, native event handlers, native items/images, renderer owner, and popup owner chain deterministically.

Stage 7 intentionally excludes submenus, split-button semantics, automatic checked-state toggling, custom popup forms, global input hooks, custom screen-edge placement, timers, animation, and a live item-binding engine. Those capabilities require separate future design rather than being inferred from native implementation details.

## 7. Theme lifecycle

Target behavior:

```text
Application changes CurrentTheme
            |
            v
BootstrapThemeManager.ThemeChanged
            |
            v
controls recompute cached theme-dependent state
            |
            v
Invalidate()
```

Application code must not be required to call `RefreshTheme()` on every control.

Designer-created controls must still have a safe default theme when no manager has been configured.

Pagination is an example of composition avoiding redundant theme ownership: the container itself does not subscribe to `BootstrapThemeManager`; its existing ButtonGroup/Button children already participate in the established theme lifecycle.

NumericBox subscribes once to `BootstrapThemeManager.ThemeChanged` because the wrapper paints its own shell and owns a theme-created font. Runtime changes recompute the shell/native-editor palette and replace only the framework-owned font. Disposal removes that static subscription; once a caller assigns `Font`, caller font ownership is preserved.

ComboBox follows the same deterministic ownership rule: it subscribes once for shell/item palette and theme-created font updates, reapplies owner-draw metrics after DPI/font/handle changes, and never recreates or rebinds native item/selection state merely because the theme changed. Disposal removes the static subscription; caller-assigned fonts remain caller-owned.

Dropdown subscribes once because an open native popup can outlive the target click that created its current snapshot. Theme changes while open regenerate only framework-owned menu images and invalidate/reapply token presentation. A closed Dropdown has no native rows to synchronize; its next opening resolves the current theme. Disposal removes the static subscription.

Popover subscribes to the static theme event only while open. Closing/disposal removes that handler and the target/ancestor tracker. Tooltip continues resolving theme at popup/draw time without a static subscription.

## 8. Animation lifecycle

A consumer starts animation only when it can render useful frames. On hide/dispose, it pauses/stops or releases the animation according to component semantics.

Rapid state changes must be deterministic. For example, calling Collapse while an Expand animation is active should transition from the current visual progress rather than jumping through an invalid state.

Reduced motion should shorten or skip nonessential transitions while preserving final state changes.

Finite `Stop()` freezes current progress and `Start()` resumes it; `Restart()` always begins from zero. Natural finite completion stops scheduling before publishing `Completed`, allowing event handlers to stop, restart, or dispose safely. Starting a previously completed finite animation begins a new run from zero.

Loop `Stop()` and `Start()` freeze/resume the current cycle position; `Restart()` returns to zero. Loop animation does not expose a finite completion event.

Pagination, NumericBox, ComboBox, and Dropdown are non-animated and must not introduce scheduling solely for state, validation, focus, selection, or popup changes.

## 9. Resource ownership

The owner that creates a disposable resource is responsible for disposing it unless ownership is explicitly transferred.

For paint-time resources, prefer scoped `using` lifetime. For cached resources, recreate only when the cache key changes and dispose the previous instance.

Event subscriptions crossing object lifetimes must be explicitly removed.

Animation objects own and deterministically dispose their frame scheduler and owner-lifecycle subscriptions. They never own or dispose the optional WinForms control supplied as lifecycle owner.

Pagination owns its internal ButtonGroup and dynamically-created Button children through normal WinForms containment. When a paging-structure change replaces buttons, removed buttons are explicitly detached and disposed. It does not own application data, data controls, timers, or theme-manager subscriptions.

NumericBox owns its native `NumericUpDown` through normal WinForms containment and owns only framework-created theme fonts. It detaches editor/theme event handlers on disposal and never disposes a caller-assigned font.

ComboBox owns no application items, `DataSource`, binding manager, popup window, native child window, or caller font. Paint-time `Brush`, `Pen`, `GraphicsPath`, and `Graphics` instances are scoped and disposed immediately; the only cross-lifetime resource is its framework-created theme font plus the deterministic theme subscription.

Dropdown owns its one `ToolStripDropDownMenu`, transient native snapshot items, generated icon bitmaps, native event subscriptions, and theme subscription. It never owns the caller's `BootstrapButton`, `BootstrapDropdownItem` instances, icon descriptors, or icon renderer. Rebuild/theme refresh clears native image references before disposing bitmaps so ToolStrip cannot retain disposed image objects.

Popover owns its internal surface, `ToolStripControlHost`, `ToolStripDropDown`, rounded regions, and open-only subscriptions. It never owns or disposes `Target` or `Content`; content is detached before owned popup infrastructure is disposed.

## 10. Designer architecture

The framework must not require a service locator, DI container, or application bootstrap merely to instantiate a control in the Designer.

Use parameterless constructors with safe defaults. Runtime services should have defaults or be attached lazily.

Designer-specific code should be isolated and must not leak into runtime rendering behavior.

Pagination follows the same rule: its parameterless constructor creates a valid page-one state and internal ButtonGroup without requiring theme or application initialization.

NumericBox follows the same rule: its parameterless constructor creates the one native editor with native default range/value/increment settings. Only the wrapper's documented properties are designer-facing; the private native child is an implementation detail and must not appear in generated Designer code.

ComboBox also has a safe parameterless constructor. The inherited native collection/binding/selection properties remain designer/runtime APIs; framework-specific designer-facing state is limited to validation, radius, and optional leading icon. `IconRenderer` is intentionally hidden from Designer serialization and defaults to the existing source-neutral renderer.

Dropdown has a safe parameterless constructor, stable content-serialized `Items`, and nullable `Target`. Creating it at design time requires neither a live form handle nor application bootstrap. The native popup is private and is not designer-serialized.

## 11. Error handling philosophy

Invalid public property values should be normalized or rejected consistently. Do not allow negative sizes, invalid Min/Max ranges, or impossible animation duration states to produce painting exceptions.

Recoverable rendering failures should fail gracefully rather than crash the host form. Programmer-contract violations may throw argument exceptions where that improves diagnosis.

Animation durations must be greater than zero. Easing delegates must be non-null, and published eased values are normalized before reaching consumers.

Pagination rejects negative `TotalItems`, non-positive `PageSize`, `MaxVisiblePages < 5`, and direct `CurrentPage` values outside the current one-based range. When a caller changes `TotalItems` or `PageSize` so that the existing current page becomes invalid, Pagination normalizes that derived state by clamping to the new last page and emits one `PageChanged` event.

NumericBox deliberately preserves native `NumericUpDown` range/error behavior: direct `Value` assignments outside the current native range throw the native `ArgumentOutOfRangeException`, and range-property mutations use native normalization semantics. Framework-only `ValidationState` rejects undefined enum values and `BorderRadius` rejects values below the `-1` theme sentinel before state mutation.

ComboBox deliberately preserves native `ComboBox` exceptions/restrictions for item, binding, style, and autocomplete APIs rather than translating them. Framework-only `ValidationState` rejects undefined enum values, `BorderRadius` rejects values below the `-1` theme sentinel, and `IconRenderer` rejects `null` before state mutation.

Dropdown rejects undefined `BootstrapVariant` values and negative logical `MinimumWidth`. `Show()` throws only when no `Target` is assigned; with an assigned target it is a no-op when already open, empty, disabled, loading, or disposed. `Close()` is idempotent. Disabled commands and separators never dispatch model `Click`, and framework activation never mutates `Checked`.

## 12. Evolution rules

Before the first stable release, public APIs may change deliberately to improve consistency. Every such change must update `docs/COMPONENTS.md`, relevant examples, and `docs/DECISIONS.md` when architectural.

After a stable compatibility baseline is declared, breaking public changes require an explicit compatibility policy.

The Pagination, NumericBox, ComboBox, and Dropdown API additions change the proposed v1 release-candidate fingerprint intentionally. Their exported surfaces are reviewed before updating the approved fingerprint, while helper/layout/renderer types remain internal and `AssemblyVersion` stays `1.0.0.0`.
