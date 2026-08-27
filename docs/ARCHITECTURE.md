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

 TextBox -----+
 Card --------+---- shared Theme / Rendering / Icons
 Progress ----+---- Animation / LoopAnimation
 Sidebar -----+---- Button / Collapse / Icons
 DataGrid ----+---- Theme / Spinner (optional loading overlay)
```

The diagram shows conceptual dependencies, not necessarily direct assembly references for every line.

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

Primitive controls own one interaction/rendering concept, for example Button, Spinner, TextBox, Card, Collapse, or ProgressBar.

They may use shared foundation layers but must not duplicate them.

### 6.2 Composite controls

Composite controls assemble primitives:

- ButtonGroup contains Buttons and owns grouping/selection rules.
- ButtonToolbar contains ButtonGroups and owns toolbar layout only.
- Accordion owns AccordionItems; each item combines a focusable header with Collapse behavior.
- Sidebar reuses Collapse/animation for expandable sections.
- DataGridView may compose a Spinner overlay for loading.

Composite controls should not reach into private rendering internals of primitives.

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

## 8. Animation lifecycle

A consumer starts animation only when it can render useful frames. On hide/dispose, it pauses/stops or releases the animation according to component semantics.

Rapid state changes must be deterministic. For example, calling Collapse while an Expand animation is active should transition from the current visual progress rather than jumping through an invalid state.

Reduced motion should shorten or skip nonessential transitions while preserving final state changes.

Finite `Stop()` freezes current progress and `Start()` resumes it; `Restart()` always begins from zero. Natural finite completion stops scheduling before publishing `Completed`, allowing event handlers to stop, restart, or dispose safely. Starting a previously completed finite animation begins a new run from zero.

Loop `Stop()` and `Start()` freeze/resume the current cycle position; `Restart()` returns to zero. Loop animation does not expose a finite completion event.

## 9. Resource ownership

The owner that creates a disposable resource is responsible for disposing it unless ownership is explicitly transferred.

For paint-time resources, prefer scoped `using` lifetime. For cached resources, recreate only when the cache key changes and dispose the previous instance.

Event subscriptions crossing object lifetimes must be explicitly removed.

Animation objects own and deterministically dispose their frame scheduler and owner-lifecycle subscriptions. They never own or dispose the optional WinForms control supplied as lifecycle owner.

## 10. Designer architecture

The framework must not require a service locator, DI container, or application bootstrap merely to instantiate a control in the Designer.

Use parameterless constructors with safe defaults. Runtime services should have defaults or be attached lazily.

Designer-specific code should be isolated and must not leak into runtime rendering behavior.

## 11. Error handling philosophy

Invalid public property values should be normalized or rejected consistently. Do not allow negative sizes, invalid Min/Max ranges, or impossible animation duration states to produce painting exceptions.

Recoverable rendering failures should fail gracefully rather than crash the host form. Programmer-contract violations may throw argument exceptions where that improves diagnosis.

Animation durations must be greater than zero. Easing delegates must be non-null, and published eased values are normalized before reaching consumers.

## 12. Evolution rules

Before the first stable release, public APIs may change deliberately to improve consistency. Every such change must update `docs/COMPONENTS.md`, relevant examples, and `docs/DECISIONS.md` when architectural.

After a stable compatibility baseline is declared, breaking public changes require an explicit compatibility policy.
