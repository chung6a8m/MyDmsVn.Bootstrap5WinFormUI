# Phase 9 — BootstrapCollapse

Phase 9 finalizes `BootstrapCollapse` as the reusable vertical expand/collapse primitive that later Accordion and Sidebar work must compose rather than reimplement.

## Public contract

```text
BootstrapCollapseHeightMode: Auto | Fixed

BootstrapCollapse.Expanded
BootstrapCollapse.ExpandedHeightMode
BootstrapCollapse.ExpandedHeight
BootstrapCollapse.AnimationDuration
BootstrapCollapse.AnimationProgress
BootstrapCollapse.IsAnimating
BootstrapCollapse.ExpandedChanged
BootstrapCollapse.AnimationProgressChanged
BootstrapCollapse.Expand()
BootstrapCollapse.Collapse()
BootstrapCollapse.Toggle()
```

### Defaults

- `Expanded = true` so a control dropped in the Designer exposes its content immediately.
- `ExpandedHeightMode = Auto`.
- `ExpandedHeight = 0`; this value is used only in Fixed mode.
- `AnimationDuration = 200 ms`, within the design-system collapse range.
- `AnimationProgress = 1.0` while initially expanded.
- The container itself is not a tab stop.

## Height behavior

### Auto

Auto mode measures visible child content plus the Collapse padding. Child size/visibility and normal WinForms layout changes trigger remeasurement.

When stable and expanded, a new measured height is applied immediately so ordinary content/layout edits are not treated as an application state transition. When an expand transition is already active, a changed measured target restarts the remaining shared transition from the current visual height.

A collapsed Auto instance retains the latest expanded measurement so collapse to zero does not destroy the height required for the next expansion.

### Fixed

Fixed mode uses `ExpandedHeight` exactly. The property accepts zero or a positive value and rejects negative values.

Changing `ExpandedHeight` while stably expanded updates the control to the new exact height. If an expansion is active, the new value becomes the new transition target.

## Animation and reversal

`BootstrapCollapse` does not own a `Timer`. Every motion segment uses the shared finite `BootstrapAnimation` with `BootstrapEasing.EaseInOut`.

Before a running transition is reversed or retargeted, the current shared animation is stopped. `BootstrapAnimation.Stop()` publishes elapsed progress, allowing Collapse to capture the current visual height before disposing that animation. The replacement segment therefore starts from the current rendered height instead of jumping to the previous start/end height.

A partial segment scales the configured full `AnimationDuration` by the remaining height distance. This keeps repeated rapid reversals responsive without extending every partial transition to another full duration.

`AnimationProgress` is semantic expansion progress, not animation-timeline progress:

- `0.0` = fully collapsed.
- `1.0` = fully expanded.
- It rises while expanding and falls while collapsing.

That contract is intentionally suitable for Phase 10 AccordionHeader chevron animation without giving Accordion a second timing engine.

`IsAnimating` remains true while a transition is pending/running, including a transition paused by owner visibility lifecycle.

## Reduced motion and lifecycle

Reduced motion is inherited from the current application theme through `BootstrapAnimation`. Starting/restarting a transition with reduced motion enabled applies its exact final height immediately and schedules no continuous animation.

A runtime Reduced-motion change during an active transition retargets the remaining segment using the new preference.

The shared animation owner lifecycle pauses scheduling while Collapse is hidden and resumes without counting hidden wall-clock time. Disposing Collapse disposes any active animation and detaches theme/child event subscriptions.

## State events

`ExpandedChanged` represents requested logical state. It is raised once when `Expanded` actually changes and is not raised for repeated `Expand()`/`Collapse()` calls that request the current state.

`AnimationProgressChanged` represents visual expansion progress and is intended for composed controls such as the Phase 10 AccordionHeader.

## Automated coverage

Phase 9 control tests cover:

- Public defaults.
- Auto versus Fixed height modes.
- Auto content measurement and content resize.
- Exact Fixed final height and runtime Fixed target changes.
- `Expanded`, `Expand()`, `Collapse()`, and `Toggle()` state behavior.
- `ExpandedChanged` idempotence.
- Reduced-motion exact final states.
- Rapid repeated state requests ending in the last requested state.
- Invalid duration and negative expanded-height rejection.

Shared Phase 4 animation tests continue to cover elapsed-time progress, stop/resume, owner hide/show, reduced motion, disposal, and reentrancy. Collapse relies on that shared implementation instead of duplicating those tests with sleeps.

## Demo and manual verification

Launch the demo application and choose **Collapse**.

The page contains:

- An Auto-height instance with variable rows that can be added and removed.
- A Fixed-height instance with `ExpandedHeight = 180`.
- Live `Expanded`, `Height`, `AnimationProgress`, and `IsAnimating` status.

Manual checks:

1. Repeatedly click **Toggle auto** while motion is active; height must reverse from its current position without jumping.
2. Repeat for **Toggle fixed** and verify the exact expanded height returns to 180.
3. Add/remove Auto rows while expanded, then while collapsed, and verify the next expansion uses the current content measurement.
4. Toggle Reduced motion from the main demo while a transition is active; the remaining transition must settle immediately to the requested final state.
5. Hide/show or cover/reveal normal host layouts and confirm animation lifecycle remains stable.
6. Switch Light/Dark while the page is open.
7. Repeat at Windows display scaling 100%, 125%, 150%, 175%, and 200%; content must not clip and Auto measurement must settle to the correct content height.
8. Rapidly resize the demo while Auto content is expanded and collapsed.

## Phase 10 dependency rule

Accordion must compose this control. It may consume `Expanded`, `ExpandedChanged`, `AnimationProgress`, and `AnimationProgressChanged`, but it must not copy height interpolation, add another timer, or implement a parallel reduced-motion path.
