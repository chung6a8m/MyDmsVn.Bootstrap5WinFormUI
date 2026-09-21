# Phase 4 — Shared Animation Infrastructure Design

Date: 2026-08-27

## 1. Purpose

Phase 4 introduces the shared animation foundation used by Spinner, Collapse, Progress, Sidebar, Button loading, and later animated controls. The goal is to provide one finite animation primitive and one looping primitive with consistent timing, easing, lifecycle, reduced-motion behavior, and deterministic disposal across `net48` and `net8.0-windows`.

This phase does not implement control-specific animation behavior.

## 2. Architectural constraints

The implementation must follow the existing project rules:

- Namespace: `MyDmsVn.Bootstrap5WinFormUI.Animation`.
- Shared code must compile for both `net48` and `net8.0-windows`.
- Animation runs on the WinForms UI thread.
- Controls must consume shared animation primitives instead of creating ad-hoc timers.
- `Thread.Sleep` and `Task.Delay` are not frame schedulers.
- Reduced motion comes from `BootstrapThemeManager.CurrentTheme.ReducedMotion`.
- Hidden or disposed consumers must not continue useful animation work.
- Public APIs must not expose test-only timing infrastructure.
- Initial implementation may use per-animation WinForms timers behind the abstraction; a central scheduler may replace the internal implementation later without changing consumer APIs.

## 3. Selected approach

Use `System.Windows.Forms.Timer` for UI-thread frame scheduling and `System.Diagnostics.Stopwatch`-style monotonic elapsed time for progress calculation.

The timer is only a wake-up mechanism. Progress is always derived from elapsed time, never by counting timer ticks. This prevents accumulated timer jitter from changing total animation duration.

Timing and scheduling are isolated behind internal abstractions so tests can advance time deterministically without arbitrary sleeps.

## 4. Public API direction

### 4.1 BootstrapEasing

`BootstrapEasing` exposes normalized easing functions operating on input progress in `[0, 1]`.

Required functions:

- `Linear`
- `EaseIn`
- `EaseOut`
- `EaseInOut`

All easing functions clamp input to `[0, 1]` and return values in `[0, 1]`.

The exact mathematical curves should be simple, stable, dependency-free, and identical across both target frameworks.

### 4.2 BootstrapAnimation

`BootstrapAnimation` represents one finite transition.

Expected public concepts:

- `Duration`
- `Easing`
- `Progress`
- `IsRunning`
- `Start()`
- `Stop()`
- `Restart()`
- `Dispose()`
- progress notification
- completion notification

Behavior:

- `Progress` is normalized to `[0, 1]`.
- `Start()` begins from the current progress when stopped before completion.
- `Restart()` always begins again from zero.
- `Stop()` freezes current progress and stops frame scheduling without reporting completion.
- Natural completion publishes final progress `1.0`, stops scheduling, and raises completion exactly once for that run.
- Starting a completed animation without restart begins a new run from zero rather than creating an inert running state.
- Calls after disposal throw `ObjectDisposedException` except `Dispose()`, which is idempotent.

### 4.3 BootstrapLoopAnimation

`BootstrapLoopAnimation` represents repeating normalized progress.

Expected public concepts:

- `Duration` for one cycle
- `Easing`
- `Progress`
- `IsRunning`
- `Start()`
- `Stop()`
- `Restart()`
- `Dispose()`
- progress notification

Behavior:

- Raw cycle progress wraps from values approaching `1.0` back to `0.0`.
- The published value is the eased cycle progress.
- Loop animation never raises a finite-completion event.
- `Stop()` freezes the current cycle position.
- `Start()` resumes from the frozen cycle position.
- `Restart()` begins the loop at zero.
- Calls after disposal follow the same disposal contract as finite animation.

## 5. Internal timing model

Introduce small internal abstractions for deterministic testing and future scheduler replacement.

Conceptually:

```text
IAnimationClock
  Elapsed / timestamp access

IAnimationFrameScheduler
  Start(callback)
  Stop()
  IsRunning
  Dispose()
```

Production implementations use a monotonic clock and WinForms timer. Tests use manually controlled clock/scheduler implementations.

The internal frame interval should target approximately 60 Hz, but correctness must not depend on exact delivery frequency.

## 6. Owner lifecycle integration

Animations may optionally be associated with a WinForms `Control` owner.

When an owner is present:

- If the owner is disposed, animation scheduling stops and owned lifecycle subscriptions are removed.
- If the owner becomes hidden, scheduling stops while retaining logical progress.
- When the owner becomes visible again, a previously running animation resumes from the retained logical position rather than including hidden wall-clock time.
- If the owner is already disposed when starting, animation must not start.

Lifecycle handling belongs inside shared animation infrastructure rather than every consumer reimplementing visibility/disposal subscriptions.

The animation object does not own or dispose the control.

## 7. Reduced-motion behavior

Reduced motion is evaluated when a run starts or restarts.

For `BootstrapAnimation`:

- If reduced motion is enabled, immediately publish the final progress value and completion without creating frame scheduling work.
- The final state remains identical to a normally completed transition.

For `BootstrapLoopAnimation`:

- If reduced motion is enabled, do not start continuous frame scheduling.
- Publish a stable normalized progress value of zero and remain logically stopped.

If the application changes themes while an animation is already running, the current run is not rewritten mid-frame. The next explicit start/restart uses the current reduced-motion preference. Controls that need immediate reaction to a theme change may stop/restart through the shared API.

This avoids coupling animation objects directly to the global theme event and therefore avoids global event subscriptions retaining animations.

## 8. State and reentrancy rules

State transitions must be deterministic under rapid calls.

Examples:

- `Start(); Start();` is idempotent while already running.
- `Stop(); Stop();` is idempotent.
- `Restart()` while running resets elapsed state and continues with a new run.
- `Stop(); Start();` resumes from the frozen position.
- Event handlers may call `Stop()`, `Restart()`, or `Dispose()` safely without corrupting scheduler state.

Frame processing must update internal state before invoking consumer callbacks where needed so callback reentrancy observes a coherent object state.

## 9. Error handling and validation

- Duration must be greater than zero for normal animated operation.
- Invalid duration values should throw `ArgumentOutOfRangeException` rather than producing divide-by-zero or undefined animation behavior.
- Easing delegates/functions must not be null.
- Eased output is normalized before publication to protect consumers from accidental out-of-range results if a custom easing delegate is allowed.

## 10. Testing strategy

Tests must be deterministic and avoid waiting on real wall-clock timing.

### BootstrapEasing

Cover:

- boundary values `0` and `1`
- representative midpoint values
- clamping below zero and above one
- monotonic normalized output for built-in curves

### BootstrapAnimation

Cover:

- initial state
- start and intermediate progress
- natural completion
- completion raised exactly once per run
- stop freezes progress
- resume continues from frozen progress
- restart resets to zero
- restart while running
- repeated start/stop calls
- reduced-motion immediate final state
- hidden owner pauses scheduling and show resumes
- disposed owner stops work
- animation disposal is idempotent
- public operation after disposal throws where specified
- event-handler reentrancy for stop/restart/dispose

### BootstrapLoopAnimation

Cover:

- start and cycle progress
- wrap at cycle boundary
- stop/resume
- restart
- repeated lifecycle calls
- reduced-motion no-loop behavior
- hidden/show owner behavior
- owner disposal
- animation disposal

Tests should run for both project target frameworks through the existing test project.

## 11. Demo/manual verification

Add an Animation demo entry showing at least:

- one finite progress transition
- restart and stop/resume controls
- one loop animation
- current normalized progress values
- a reduced-motion toggle implemented by switching to a theme instance whose `ReducedMotion` value changes
- hide/show behavior of an animated preview area

The demo is diagnostic, not a new product control.

## 12. Documentation updates

Update the testing documentation with the concrete Phase 4 automated/manual coverage and document the finalized shared animation lifecycle contract where needed.

Do not add control-specific animation APIs during this phase.

## 13. Non-goals

Phase 4 does not include:

- Spinner rendering
- Collapse height measurement
- ProgressBar animation semantics
- Sidebar transitions
- Button loading
- a global central scheduler
- background-thread animation
- physics/spring animation
- arbitrary keyframe timelines

These remain later-phase concerns or deferred extensions.

## 14. Acceptance criteria

Phase 4 is complete when:

- `BootstrapAnimation`, `BootstrapLoopAnimation`, and `BootstrapEasing` exist with XML documentation.
- Finite progress, completion, stop/resume, and restart behavior are deterministic.
- Loop progress wraps correctly and supports stop/resume/restart.
- Reduced-motion behavior creates no unnecessary scheduled animation work.
- Hidden/disposed owner lifecycle behavior is implemented and tested.
- Owned timer/event resources are disposed deterministically.
- Tests cover progress, completion, restart, cancellation/stop, disposal, reduced motion, and lifecycle behavior.
- The Animation demo provides a manual verification path.
- Both `net48` and `net8.0-windows` builds succeed.
- Relevant tests pass for both targets.
- No control-specific timer implementation is introduced.
