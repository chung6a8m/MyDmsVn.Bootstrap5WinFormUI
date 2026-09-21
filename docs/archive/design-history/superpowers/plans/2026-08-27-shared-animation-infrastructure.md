# Shared Animation Infrastructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the Phase 4 finite and looping WinForms animation primitives, deterministic timing seams, owner lifecycle behavior, reduced-motion handling, tests, demo coverage, and documentation.

**Architecture:** Public `BootstrapAnimation`, `BootstrapLoopAnimation`, and `BootstrapEasing` live in `MyDmsVn.Bootstrap5WinFormUI.Animation`. Production scheduling uses a WinForms timer as a UI-thread wake-up source while progress is calculated from a monotonic clock; both are hidden behind internal interfaces so tests can drive time and frames synchronously. Owner visibility/disposal is centralized in a small internal lifecycle helper shared by both animation primitives.

**Tech Stack:** C#, WinForms, `System.Diagnostics.Stopwatch`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`).

**Spec:** `docs/superpowers/specs/2026-08-27-shared-animation-infrastructure-design.md`

## Global Constraints

- Namespace: `MyDmsVn.Bootstrap5WinFormUI.Animation`.
- Shared code must compile for both `net48` and `net8.0-windows`.
- Animation runs on the WinForms UI thread.
- Controls consume shared animation primitives instead of creating ad-hoc timers.
- `Thread.Sleep` and `Task.Delay` are not frame schedulers.
- Reduced motion comes from `BootstrapThemeManager.CurrentTheme.ReducedMotion`.
- Hidden or disposed consumers must not continue useful animation work.
- Public APIs do not expose test-only timing infrastructure.
- A central scheduler is a non-goal for Phase 4.

---

## File Structure

**Create product files**

- `src/MyDmsVn.Bootstrap5WinFormUI/Animation/BootstrapEasing.cs` — built-in normalized easing functions.
- `src/MyDmsVn.Bootstrap5WinFormUI/Animation/BootstrapAnimation.cs` — finite transition public API/state machine.
- `src/MyDmsVn.Bootstrap5WinFormUI/Animation/BootstrapLoopAnimation.cs` — repeating transition public API/state machine.
- `src/MyDmsVn.Bootstrap5WinFormUI/Animation/IAnimationClock.cs` — internal elapsed-time contract.
- `src/MyDmsVn.Bootstrap5WinFormUI/Animation/StopwatchAnimationClock.cs` — production monotonic clock.
- `src/MyDmsVn.Bootstrap5WinFormUI/Animation/IAnimationFrameScheduler.cs` — internal frame callback contract.
- `src/MyDmsVn.Bootstrap5WinFormUI/Animation/WinFormsAnimationFrameScheduler.cs` — production WinForms timer scheduler.
- `src/MyDmsVn.Bootstrap5WinFormUI/Animation/AnimationOwnerLifecycle.cs` — shared hidden/show/disposal coordination.

**Create test files**

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Animation/BootstrapEasingTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Animation/AnimationTestDoubles.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Animation/BootstrapAnimationTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Animation/BootstrapLoopAnimationTests.cs`

**Demo/docs**

- Create `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AnimationDemoForm.cs`.
- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs` to expose the Animation demo.
- Modify `docs/ARCHITECTURE.md` with the finalized Phase 4 API/lifecycle contract.
- Modify `docs/TESTING.md` with concrete Phase 4 automated/manual verification.

---

### Task 1: Built-in easing functions

**Files:**
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Animation/BootstrapEasingTests.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Animation/BootstrapEasing.cs`

**Interfaces:**
- Produces: `public static double Linear(double progress)`, `EaseIn(double)`, `EaseOut(double)`, `EaseInOut(double)`.

- [ ] **Step 1: Write failing easing tests** covering boundaries, clamping, midpoint values, and monotonic normalized output. Use expected quadratic curves: `EaseIn(t)=t*t`, `EaseOut(t)=1-(1-t)^2`, and piecewise quadratic `EaseInOut`.
- [ ] **Step 2: Run `dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapEasingTests` and verify RED because `BootstrapEasing` does not exist.**
- [ ] **Step 3: Implement `BootstrapEasing`**, using the existing compatibility clamp helper rather than `Math.Clamp`.
- [ ] **Step 4: Run the filtered easing tests for `net8.0-windows` and `net48`; verify GREEN.**
- [ ] **Step 5: Commit `test/feat: add Bootstrap easing functions`.**

### Task 2: Deterministic timing and frame scheduler seams

**Files:**
- Create product internals: `IAnimationClock.cs`, `StopwatchAnimationClock.cs`, `IAnimationFrameScheduler.cs`, `WinFormsAnimationFrameScheduler.cs`
- Test support: `AnimationTestDoubles.cs`
- Test: extend `BootstrapAnimationTests.cs` with construction/timing contract tests that require the internal seams.

**Interfaces:**
- `internal interface IAnimationClock { TimeSpan Elapsed { get; } void Restart(); }`
- `internal interface IAnimationFrameScheduler : IDisposable { bool IsRunning { get; } void Start(Action callback); void Stop(); }`
- `internal sealed class StopwatchAnimationClock : IAnimationClock`
- `internal sealed class WinFormsAnimationFrameScheduler : IAnimationFrameScheduler`

- [ ] **Step 1: Write failing tests** with `ManualAnimationClock` and `ManualAnimationFrameScheduler`; the scheduler exposes `FireFrame()` only in test code and records whether scheduling is active.
- [ ] **Step 2: Run the focused tests and verify RED because the internal interfaces/animation constructor do not exist.**
- [ ] **Step 3: Add the internal contracts and production implementations.** `WinFormsAnimationFrameScheduler` uses one `System.Windows.Forms.Timer` with a 16 ms interval, stores one callback, starts/stops idempotently, and clears callback on dispose. `StopwatchAnimationClock` wraps `Stopwatch` and reports monotonic elapsed time since `Restart()`.
- [ ] **Step 4: Run focused tests for both targets and verify GREEN.**
- [ ] **Step 5: Commit `feat: add deterministic animation timing seams`.**

### Task 3: Finite `BootstrapAnimation`

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Animation/BootstrapAnimation.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Animation/BootstrapAnimationTests.cs`

**Interfaces:**
- `public BootstrapAnimation(TimeSpan duration, Func<double,double>? easing = null, Control? owner = null)`
- Internal test constructor additionally accepts `IAnimationClock` and `IAnimationFrameScheduler`.
- Properties: `TimeSpan Duration`, `Func<double,double> Easing`, `double Progress`, `bool IsRunning`.
- Events: `event EventHandler? ProgressChanged`, `event EventHandler? Completed`.
- Methods: `Start()`, `Stop()`, `Restart()`, `Dispose()`.

- [ ] **Step 1: Write failing tests** for initial state, intermediate elapsed-time progress, easing publication, natural completion exactly once, repeated `Start`, stop/freeze, resume, restart from zero, restart while running, custom easing output clamping, duration/easing validation, post-dispose exceptions, idempotent dispose, and event-handler reentrancy.
- [ ] **Step 2: Run `BootstrapAnimationTests` on `net8.0-windows`; verify RED because `BootstrapAnimation` is missing.**
- [ ] **Step 3: Implement the minimal finite state machine.** Track raw progress separately from eased published progress; on each frame derive raw progress from frozen offset plus elapsed/duration. Stop scheduler before raising completion. `Start()` resumes stopped progress and resets a completed run to zero; `Restart()` always resets to zero. Update state before firing events so reentrant calls see coherent state.
- [ ] **Step 4: Run finite tests on both targets; verify GREEN and no regressions.**
- [ ] **Step 5: Commit `feat: add finite BootstrapAnimation`.**

### Task 4: Shared owner lifecycle and reduced motion

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Animation/AnimationOwnerLifecycle.cs`
- Modify: `BootstrapAnimation.cs`
- Test: extend `BootstrapAnimationTests.cs`

**Interfaces:**
- `AnimationOwnerLifecycle` subscribes to `Control.VisibleChanged` and `Control.Disposed` and reports pause/resume/dispose callbacks without owning the control.

- [ ] **Step 1: Write failing STA tests** using a real `Control` plus manual clock/scheduler for hide pauses, hidden wall-clock exclusion, show resumes, disposed owner stops work, starting with disposed owner does nothing, and finite reduced-motion immediate progress/completion without scheduler start.
- [ ] **Step 2: Run the focused tests; verify RED on missing lifecycle/reduced-motion behavior.**
- [ ] **Step 3: Implement `AnimationOwnerLifecycle` and integrate it into `BootstrapAnimation`.** On hide, snapshot progress and stop scheduling; on show, restart elapsed-time origin only if the animation was logically running. On owner disposal, stop permanently and unsubscribe. Read `BootstrapThemeManager.CurrentTheme.ReducedMotion` only when `Start()`/`Restart()` begins a run.
- [ ] **Step 4: Run finite/lifecycle tests for both targets; verify GREEN.**
- [ ] **Step 5: Commit `feat: add animation lifecycle and reduced motion`.**

### Task 5: `BootstrapLoopAnimation`

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Animation/BootstrapLoopAnimation.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Animation/BootstrapLoopAnimationTests.cs`

**Interfaces:**
- Public constructor/properties/events mirror the finite primitive except there is no `Completed` event.
- Methods: `Start()`, `Stop()`, `Restart()`, `Dispose()`.

- [ ] **Step 1: Write failing tests** for initial state, cycle progress, easing, boundary wrap (`elapsed == duration` yields cycle progress zero), multi-cycle modulo behavior, stop/freeze/resume, restart, repeated calls, custom easing clamping, validation/disposal, hidden/show lifecycle, owner disposal, and reduced-motion stable zero/no scheduling.
- [ ] **Step 2: Run `BootstrapLoopAnimationTests` for `net8.0-windows`; verify RED.**
- [ ] **Step 3: Implement loop state using elapsed modulo cycle duration**, reusing the same scheduler/clock/lifecycle contracts. No finite completion event is introduced.
- [ ] **Step 4: Run loop plus all Animation tests on both targets; verify GREEN.**
- [ ] **Step 5: Commit `feat: add BootstrapLoopAnimation`.**

### Task 6: Animation diagnostic demo

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AnimationDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`

**Interfaces:**
- Demo consumes public animation APIs only; no internal/test timing interfaces.

- [ ] **Step 1: Add the Animation demo form** with finite and loop preview surfaces, numeric progress labels, Start/Stop/Restart buttons, a Hide/Show preview action, and a reduced-motion checkbox that replaces `BootstrapThemeManager.CurrentTheme` with a new theme carrying the selected `ReducedMotion` value.
- [ ] **Step 2: Add an `Animation` command to `MainForm` alongside Rendering/DPI and Icons, applying current theme colors to the new button.**
- [ ] **Step 3: Build the demo for `net8.0-windows`; correct compile issues without introducing control-specific timers.**
- [ ] **Step 4: Commit `demo: add animation infrastructure diagnostics`.**

### Task 7: Documentation and complete verification

**Files:**
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/TESTING.md`

- [ ] **Step 1: Document the finalized public animation API, elapsed-time scheduling model, lifecycle behavior, reduced-motion rule, and the explicit Phase 4 decision not to add a central scheduler.**
- [ ] **Step 2: Document Phase 4 automated coverage and manual Animation demo checks.**
- [ ] **Step 3: Run `dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release -f net48`. Expected: zero errors.**
- [ ] **Step 4: Run `dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release -f net8.0-windows`. Expected: zero errors.**
- [ ] **Step 5: Run `dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48` and the equivalent `net8.0-windows` command. Expected: all tests pass.**
- [ ] **Step 6: Search product code for newly introduced `Timer`, `Task.Delay`, and `Thread.Sleep`. Confirm the only Phase 4 frame timer is `WinFormsAnimationFrameScheduler` and no sleep/delay scheduler exists.**
- [ ] **Step 7: Commit `docs: complete Phase 4 animation infrastructure`.**

## Self-review

- Spec coverage: finite animation, loop animation, easing, deterministic timing, owner hidden/disposed lifecycle, reduced motion, reentrancy, validation, demo, docs, dual-target verification, and no ad-hoc control timers are all mapped to tasks above.
- Placeholder scan: no deferred implementation placeholders are part of the plan.
- Type consistency: both animation primitives use `TimeSpan Duration`, `Func<double,double> Easing`, normalized `double Progress`, the same internal clock/scheduler contracts, and the same owner lifecycle semantics.
