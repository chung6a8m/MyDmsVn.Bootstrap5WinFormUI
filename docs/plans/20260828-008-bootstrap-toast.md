# BootstrapToast Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Stage 8 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md` by adding a Bootstrap-inspired transient `BootstrapToast` notification and an application-placed `BootstrapToastContainer` that owns stacking, queueing, auto-hide, enter/exit transitions, deterministic disposal, theme/DPI integration, demo coverage, documentation, and frozen-public-API review without introducing a global toast service or top-level notification window.

**Architecture:** `BootstrapToast : UserControl` owns the visual notification surface, title/text/icon/close affordance, dismissal request semantics, and one semantic auto-hide delay timer while it is fully visible. `BootstrapToastContainer : Panel` explicitly takes ownership through `ShowToast`, keeps insertion-order ownership state, exposes at most `MaximumVisibleToasts` at once, queues overflow toasts without starting their timers, and owns enter/exit/reflow positioning. Visual transitions use the existing finite `BootstrapAnimation`; auto-hide uses only a short-lived WinForms timer abstraction and never schedules animation frames. Stage 8 also performs a targeted internal refactor of the Alert palette formula into one shared feedback palette helper so Alert and Toast use identical semantic surface/border/foreground rules without creating a second hard-coded feedback palette.

**Tech Stack:** C#, native Windows Forms `UserControl` / `Panel` / `Button` / `Timer`, existing Theme / Rendering / Icons / Animation / Compatibility infrastructure, `BootstrapVariant`, `BootstrapVariantColorResolver`, `BootstrapThemeManager`, `BootstrapThemeMetrics`, `DpiScaler`, `RoundedPath`, `CornerRadius`, `ColorUtil`, `IconDescriptor`, `IIconRenderer`, `BootstrapAnimation`, `BootstrapEasing`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** Stage 8 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md`, plus repository-wide constraints in `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`. The Stage 2 Alert plan is the source for the feedback palette formula and close-affordance conventions.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; public Toast types remain under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile from one shared implementation for both `net48` and `net8.0-windows` wherever practical.
- Preserve roadmap order: Stage 7 (`BootstrapDropdown`) must be complete and green before Stage 8 implementation starts even though Toast's direct functional dependencies are Alert visual language and shared Animation infrastructure.
- `BootstrapToast` is one transient notification. `BootstrapToastContainer` is the only Stage 8 host/owner abstraction.
- Do not add a global/static toast manager, service locator, hidden application singleton, top-level Toast `Form`, layered window, notify-icon integration, OS notification bridge, message hook, or global keyboard/mouse hook.
- `ShowToast(BootstrapToast toast)` explicitly transfers ownership to the container. The XML documentation must say that the caller must not dispose, reparent, remove, or manually manage `Visible` after a successful transfer.
- One toast instance can be owned by at most one container and can be transferred only once. Passing an already-owned or disposed toast is an error, not a silent no-op.
- Owned toasts are disposed by the container after exit animation completes. Queued toasts dismissed before ever becoming visible are disposed immediately after logical dismissal because there is no exit surface to animate.
- Container disposal is cleanup, not a semantic dismissal. It cancels transitions/timers and disposes all owned toasts without manufacturing `Dismissed` events.
- `DismissAll()` is semantic dismissal and therefore routes every owned toast through the same exactly-once dismissal path; visible toasts exit, queued toasts are removed immediately, and no queued toast is promoted while the bulk dismissal is in progress.
- `Dismissed` is raised exactly once when a logical dismissal request is accepted. For an owned visible toast, the event occurs when exit starts; disposal happens later after exit completion. Repeated dismiss requests while already dismissing/dismissed are no-ops.
- A detached toast may be dismissed without a container. In that case `Dismiss()` immediately hides it and raises `Dismissed` once if it was visible; direct caller assignment `Visible = false` does not synthesize `Dismissed`. Setting a detached toast visible again enables a later independent dismissal.
- `AutoHideDelay` is expressed in milliseconds, defaults to `5000`, and rejects values `<= 0` with `ArgumentOutOfRangeException` before mutating state.
- `AnimationDuration` is expressed in milliseconds, defaults to `200`, and rejects values `<= 0` with `ArgumentOutOfRangeException`. This matches the repository's existing 200 ms finite-control transition convention while retaining the roadmap's `int` API.
- `AutoHide` defaults to `true`. The auto-hide timer starts only after enter transition completion and only while the toast is actually visible in a container.
- Queued, entering, exiting, hidden-container, detached, and disposed toasts do not have a running auto-hide countdown.
- Changing `AutoHide` from `true` to `false` while fully shown stops and disposes the current timer. Changing it back to `true` starts a fresh full delay. Changing `AutoHideDelay` while fully shown restarts the countdown from the new full delay.
- Auto-hide timer callbacks must be generation-guarded so a stopped/disposed timer or queued stale tick cannot dismiss a later visible cycle or a different ownership state.
- Auto-hide uses no `Task.Delay`, thread-pool timer, background thread, or animation scheduler. It is a semantic UI-thread delay only.
- Every visual transition frame comes from `BootstrapAnimation`. Do not create a second frame timer, transition loop, `Application.DoEvents` loop, `Thread.Sleep`, or `Task.Delay` animation.
- Reduced motion is inherited from `BootstrapThemeManager.CurrentTheme.ReducedMotion` through `BootstrapAnimation`. Enter/exit/reflow complete immediately, but auto-hide still waits `AutoHideDelay` once the toast is fully shown.
- WinForms child controls cannot be uniformly alpha-composited as one translucent subtree. Stage 8 therefore uses clipped slide/reflow geometry for visual transitions and does not invent fake per-child opacity or a layered-window workaround.
- Rapid show/dismiss paths must never leave overlapping enter/exit animations on the same toast, stale timer callbacks, duplicate `Dismissed` events, orphaned queue entries, or animation scheduling after disposal.
- `BootstrapToastContainer` keeps insertion order. For top placements, the oldest visible toast is nearest the top anchor and later visible toasts stack downward. For bottom placements, the oldest visible toast is nearest the bottom anchor and later visible toasts stack upward.
- `MaximumVisibleToasts` defaults to `5` and must be greater than zero. Additional owned toasts remain queued in insertion order and are promoted only when a visible slot becomes free.
- `ToastSpacing` defaults to `8` logical 96-DPI pixels and rejects negative values. The value is DPI-scaled through `DpiScaler` before layout.
- `Placement` defaults to `BootstrapToastPlacement.TopRight`; undefined enum values throw `InvalidEnumArgumentException` before mutating state.
- Container resize and placement/spacing changes recompute stack targets from pure layout logic. Stable visible toasts may reflow; queued state and ownership order never change merely because layout changes.
- The default Toast size is `320 x 96` logical pixels. Normal WinForms `Width` remains caller-configurable before ownership; preferred height is recalculated from current width/title/text/icon/dismiss affordance when shown. No new public width API is added.
- `Title` defaults to `string.Empty` and normalizes `null` to `string.Empty`. `Text` remains inherited from `Control` and follows native null-to-empty normalization.
- `Variant` defaults to `BootstrapVariant.Primary`; undefined values are rejected with the repository's existing enum-validation convention.
- `Icon` defaults to `null`. `IconRenderer` defaults to `BootstrapIconRenderer.CreateDefault()` and rejects `null`.
- `Dismissible` defaults to `true`. Toast itself is not a tab stop. The private native close button participates in keyboard focus only while dismissible and effectively enabled.
- The close glyph uses `IconDescriptor.Framework(FrameworkIconGlyph.Close)` through the configured `IIconRenderer`; do not hand-draw another X glyph.
- Reuse the Stage 2 Alert semantic tint/contrast formula by extracting it into an internal shared feedback helper. Do not copy the formula into Toast and do not introduce a new eight-row Toast color table.
- Runtime Light/Dark theme changes repaint owned toasts and re-resolve palette/fonts without resetting queue order, restart-entering toasts, or restarting auto-hide countdowns merely because colors changed.
- DPI changes recompute Toast content metrics, preferred height, slide distance, stack spacing, and stack target rectangles through `DpiScaler`.
- Temporary GDI objects are scoped with `using`; any owned derived title font is disposed/rebuilt on font/theme lifecycle changes; caller-owned fonts and caller-owned icon sources are never disposed by Toast.
- Designer construction for both Toast and ToastContainer must work without application bootstrap, DI, service locators, initialized adapters, an assigned parent, or a configured global service.
- All new public/protected members receive XML documentation. `TreatWarningsAsErrors` and `CS1591` remain green.
- Stage 8 changes the frozen public API. `Phase16PublicApiBaselineTests` must intentionally fail first, the reconstructed exported surface must be reviewed, and only then may the approved fingerprint and `docs/PUBLIC_API_BASELINE.md` be updated.
- Actions, arbitrary child content, progress bars inside Toast, custom placement coordinates, center placement, drag/swipe dismissal, hover-to-pause, close reasons, async toast providers, persistence, deduplication, notification history, screen/monitor-aware top-level overlays, and a global notification service are outside Stage 8.
- Final completion requires both target builds, relevant and full tests, Feedback demo/manual verification, resource stress, documentation updates, and deliberate public API baseline approval.

---

## Stage 7 Prerequisite Gate

Stage 8 is independently testable but it is not allowed to bypass earlier roadmap stages. Before Task 1, verify the expected predecessor artifacts exist:

```text
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlert.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlertRenderLogic.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs
demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs
```

Run from the repository root:

```powershell
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlert.cs
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs
Test-Path demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs
Test-Path tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs
```

Expected: every command returns `True`. If any predecessor is missing, stop and finish the earlier roadmap stage rather than creating alternate infrastructure inside Stage 8.

Then run the direct regression gate:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapAlert|BootstrapDropdown|FeedbackDemoForm|BootstrapAnimation"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapAlert|BootstrapDropdown|FeedbackDemoForm|BootstrapAnimation"
```

Expected: both targets pass before Toast work begins.

---

## Stage 8 Public Contract

The roadmap contract is retained. Planning resolves defaults, validation, ownership timing, and queue policy without adding convenience API.

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public enum BootstrapToastPlacement
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Dismissed))]
public class BootstrapToast : UserControl
{
    public string Title { get; set; }
    public BootstrapVariant Variant { get; set; }
    public IconDescriptor? Icon { get; set; }
    public IIconRenderer IconRenderer { get; set; }
    public bool Dismissible { get; set; }
    public bool AutoHide { get; set; }
    public int AutoHideDelay { get; set; }
    public int AnimationDuration { get; set; }

    public event EventHandler? Dismissed;

    public void Dismiss();
}

public class BootstrapToastContainer : Panel
{
    public BootstrapToastPlacement Placement { get; set; }
    public int ToastSpacing { get; set; }
    public int MaximumVisibleToasts { get; set; }

    public void ShowToast(BootstrapToast toast);
    public void DismissAll();
}
```

### Defaults and validation

| Member | Default / rule |
| --- | --- |
| `BootstrapToast.Title` | `string.Empty`; assigning `null` normalizes to empty |
| inherited `Text` | native `Control` null-to-empty semantics |
| `Variant` | `Primary`; undefined values rejected |
| `Icon` | `null` |
| `IconRenderer` | `BootstrapIconRenderer.CreateDefault()`; `null` rejected |
| `Dismissible` | `true` |
| `AutoHide` | `true` |
| `AutoHideDelay` | `5000`; values `<= 0` rejected |
| `AnimationDuration` | `200`; values `<= 0` rejected |
| Toast `TabStop` | `false` |
| Toast `AccessibleRole` | `AccessibleRole.Alert` |
| default Toast size | `320 x 96` logical pixels |
| `Placement` | `TopRight`; undefined values rejected |
| `ToastSpacing` | `8` logical pixels; negative values rejected |
| `MaximumVisibleToasts` | `5`; values `<= 0` rejected |
| Container `TabStop` | `false` |

### Ownership and dismissal contract

`ShowToast` is a transfer-of-ownership method, not a display hint:

```csharp
container.ShowToast(toast);
// Success means container now owns toast.
// Caller may keep a reference for read-only observation/events,
// but must not dispose, reparent, remove, or manually toggle Visible.
```

Rules:

- `ShowToast(null)` throws `ArgumentNullException`.
- Passing a disposed toast throws `ObjectDisposedException`.
- Passing a toast already owned by this or any other container throws `InvalidOperationException`.
- Successful transfer attaches one internal owner reference, subscribes to required internal lifecycle hooks once, and adds the toast to the container's ownership list.
- If fewer than `MaximumVisibleToasts` are active, the toast begins enter transition immediately when the container can render. Otherwise it is queued with `Visible = false` and no active timer/animation.
- Queued toasts preserve insertion order.
- A visible/entering toast remains counted against the maximum until its exit completes. This prevents overlap while it is still visually present.
- When an exit completes, the container disposes/removes that toast, then promotes the oldest queued toast if a slot is available.
- `Dismiss()` routes through one internal owner request when owned. The owner marks the toast logically dismissed, stops auto-hide, raises `Dismissed` exactly once, and starts exit if the toast has entered/started entering.
- A queued toast can be dismissed before display. It raises `Dismissed` once, is removed from the ownership queue, and is disposed immediately; it never flashes on screen and never starts a timer.
- `DismissAll()` snapshots the ownership list, suppresses queue promotion for the duration of the operation, and routes every not-yet-dismissed toast through the same internal dismissal path.
- Calling `DismissAll()` again while exits are in progress is idempotent.
- Container disposal cancels transitions and disposes children directly; it does not raise public `Dismissed` merely because the host is shutting down.

### Event timing

Public event sender is always the `BootstrapToast` instance:

```text
ShowToast accepted
  -> enter starts
  -> enter completes
  -> auto-hide timer may start
  -> dismissal request accepted
  -> timer stops/disposes
  -> Dismissed fires exactly once
  -> exit animation runs (or completes immediately under reduced motion)
  -> container removes/disposes toast
  -> queued toast may be promoted
```

`Dismissed` therefore means "this Toast has logically entered dismissal" rather than "Dispose has completed".

---

## Shared Feedback Palette Refactor

Stage 2 deliberately defines one formula for all Alert variants. Toast must use the same visual language without depending on a type named specifically for Alert.

Create:

`src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapFeedbackRenderLogic.cs`

```csharp
internal readonly struct BootstrapFeedbackPalette
{
    public BootstrapFeedbackPalette(
        Color surface,
        Color border,
        Color foreground,
        Color focus)
    {
        Surface = surface;
        Border = border;
        Foreground = foreground;
        Focus = focus;
    }

    public Color Surface { get; }
    public Color Border { get; }
    public Color Foreground { get; }
    public Color Focus { get; }
}

internal static class BootstrapFeedbackRenderLogic
{
    private const float SurfaceSemanticAmount = 0.12f;
    private const float BorderSemanticAmount = 0.45f;
    private const float ForegroundSemanticAmount = 0.72f;
    private const double MinimumTextContrast = 4.5d;

    public static BootstrapFeedbackPalette ResolvePalette(
        BootstrapThemeColors colors,
        BootstrapVariant variant,
        bool enabled)
    {
        if (!enabled)
        {
            return new BootstrapFeedbackPalette(
                colors.SurfaceSecondary,
                colors.Border,
                colors.MutedText,
                colors.Disabled);
        }

        var semantic = BootstrapVariantColorResolver.Resolve(colors, variant);
        var surface = ColorUtil.Blend(semantic, colors.Surface, SurfaceSemanticAmount);
        var border = ColorUtil.Blend(semantic, colors.Border, BorderSemanticAmount);
        var foregroundCandidate = ColorUtil.Blend(semantic, colors.Text, ForegroundSemanticAmount);
        var foreground = ColorUtil.GetContrastRatio(foregroundCandidate, surface) >= MinimumTextContrast
            ? foregroundCandidate
            : colors.Text;

        return new BootstrapFeedbackPalette(surface, border, foreground, colors.Focus);
    }
}
```

Modify `BootstrapAlertRenderLogic` to consume this helper and delete its duplicate palette constants/type. Keep existing Alert rendering results and public API unchanged. Existing Alert tests become regression coverage for the extraction; add focused shared-helper tests only if the Alert tests do not already cover all semantic/disabled cases.

This is an internal refactor only. Do not add `BootstrapFeedbackPalette` or the resolver to the public API baseline.

---

## Toast Presentation and Content Layout

`BootstrapToast` paints one rounded feedback surface using `BootstrapFeedbackRenderLogic.ResolvePalette`. It owns one private native close `Button`; title/text/icon are painted by the Toast itself.

Create internal metric/layout types in `BootstrapToastLayoutLogic.cs`:

```csharp
internal readonly struct BootstrapToastMetrics
{
    public BootstrapToastMetrics(
        int horizontalPadding,
        int verticalPadding,
        int contentSpacing,
        int iconSize,
        int closeButtonSize,
        int borderWidth,
        int radius,
        int slideDistance)
    {
        HorizontalPadding = horizontalPadding;
        VerticalPadding = verticalPadding;
        ContentSpacing = contentSpacing;
        IconSize = iconSize;
        CloseButtonSize = closeButtonSize;
        BorderWidth = borderWidth;
        Radius = radius;
        SlideDistance = slideDistance;
    }

    public int HorizontalPadding { get; }
    public int VerticalPadding { get; }
    public int ContentSpacing { get; }
    public int IconSize { get; }
    public int CloseButtonSize { get; }
    public int BorderWidth { get; }
    public int Radius { get; }
    public int SlideDistance { get; }
}
```

Resolve logical metrics from current theme:

```text
HorizontalPadding = Metrics.SpacingMD
VerticalPadding   = Metrics.SpacingSM
ContentSpacing    = Metrics.SpacingSM
IconSize          = Metrics.SpacingLG
CloseButtonSize   = Metrics.ControlHeightSmall
BorderWidth       = Metrics.BorderWidth
Radius            = Metrics.Radius
SlideDistance     = Metrics.SpacingLG
```

Every metric is scaled with `DpiScaler.Scale` using current DPI. At default 96 DPI the expected logical values are approximately `12, 8, 8, 16, 28, 1, 6, 16` based on the current default theme metrics.

Content rules:

- Surface bounds are the Toast client rectangle inset only as required for border painting.
- Optional icon occupies the leading content column.
- Title and Text share the center text column. Empty Title removes the title row and its vertical gap.
- Dismissible close button occupies the trailing column and is vertically aligned to the top content row.
- When `Dismissible = false`, close bounds are empty and text receives the released width.
- Text wraps using the current Toast width; title remains single-line with ellipsis rather than forcing unbounded height.
- Preferred height is the maximum of icon height, close-button height, and title/body text stack plus vertical padding.
- Container computes preferred height immediately before first display and after content/font/DPI changes for an owned toast.
- Caller-selected Width before ownership is preserved. Container changes Height only.
- `Title` uses an owned bold derivative of the current control Font. Rebuild/dispose that font when the base Font changes; never dispose the caller's Font.
- Disabled presentation uses the shared feedback disabled palette.
- Close button uses the same resolved foreground color and framework close glyph as Alert.

Accessibility:

```text
Toast AccessibleRole        = Alert
Toast default description   = "Transient notification."
Toast TabStop               = false
Close AccessibleName        = "Dismiss notification"
Close AccessibleDescription = "Dismisses this notification."
```

The Toast does not steal focus on show. Keyboard users may tab to the close button according to ordinary container/form tab order.

---

## Pure Stack Layout Contract

`BootstrapToastLayoutLogic` owns stack geometry independently from WinForms handles.

Use an internal method equivalent to:

```csharp
internal static Rectangle[] CalculateStackBounds(
    Rectangle clientBounds,
    BootstrapToastPlacement placement,
    int logicalSpacing,
    int maximumVisibleToasts,
    IReadOnlyList<Size> toastSizes,
    int dpi)
```

Validation:

- `logicalSpacing < 0` -> `ArgumentOutOfRangeException`.
- `maximumVisibleToasts <= 0` -> `ArgumentOutOfRangeException`.
- `dpi <= 0` -> `ArgumentOutOfRangeException`.
- Undefined `placement` -> `InvalidEnumArgumentException`.
- `toastSizes == null` -> `ArgumentNullException`.
- Negative width/height in any `Size` -> `ArgumentOutOfRangeException`.

Output length is `Math.Min(toastSizes.Count, maximumVisibleToasts)` and corresponds to the first owned/not-dismissing candidates supplied by the container.

Placement equations after scaling spacing:

```text
TopLeft:
  x = client.Left
  y starts client.Top and increases by height + spacing

TopRight:
  x = client.Right - toast.Width
  y starts client.Top and increases by height + spacing

BottomLeft:
  x = client.Left
  y starts client.Bottom - toast.Height and decreases by next height + spacing

BottomRight:
  x = client.Right - toast.Width
  y starts client.Bottom - toast.Height and decreases by next height + spacing
```

Do not silently resize toasts to fit the container and do not overlap/clamp rows when the host is too small. Normal Panel clipping is authoritative; the demo/manual documentation must tell consumers to size the container for the desired maximum stack.

---

## Transition Model

Use internal state owned by `BootstrapToastContainer`, not new public state properties:

```csharp
internal enum BootstrapToastHostState
{
    Queued,
    Entering,
    Visible,
    Exiting
}
```

Each owned toast has one internal entry record containing the Toast reference, state, current transition animation, and the geometry needed for the current transition. Do not expose the entry publicly.

### Enter

- Promotion calculates the Toast's current target stack rectangle.
- Set `Visible = true` immediately before starting the enter transition.
- Enter movement starts one scaled `SlideDistance` outside the horizontal anchor edge:
  - Left placements: `target.X - SlideDistance` -> `target.X`.
  - Right placements: `target.X + SlideDistance` -> `target.X`.
- Y remains the current stack target throughout enter.
- Use `BootstrapAnimation(TimeSpan.FromMilliseconds(toast.AnimationDuration), BootstrapEasing.EaseOut, container)`.
- On progress, interpolate only X and apply the current target Y so a concurrent host resize does not preserve a stale vertical target.
- On completion, snap to current target rectangle, set state `Visible`, and notify Toast that it is fully shown so auto-hide may start.

### Dismiss while entering

- Stop/dispose the current enter animation first.
- Preserve the current on-screen bounds as exit start; do not jump to the final enter position.
- Mark logical dismissal and raise `Dismissed` once before starting exit.
- Start one exit animation toward the outward horizontal edge from the current bounds.
- Never allow enter and exit `BootstrapAnimation` instances for the same toast to run simultaneously.

### Exit

- Stop/dispose auto-hide before exit begins.
- Exit uses `BootstrapEasing.EaseIn` and the Toast's current `AnimationDuration`.
- Move from current visual X to one `SlideDistance` outside the relevant anchor edge.
- On completion, remove and dispose the toast, remove the ownership entry, then reflow survivors and promote queued work.
- Under reduced motion, `BootstrapAnimation` completes immediately and the same completion path performs disposal/promotion synchronously.

### Reflow

Use one container-owned finite `BootstrapAnimation` for survivor movement after removal/promotion/placement changes when animation is useful. It may animate all stable survivor rectangles from their captured current bounds to newly calculated stack bounds using `BootstrapEasing.EaseInOut`. Do not create one extra reflow timer per survivor.

- A new enter/exit request cancels/disposes a stale reflow animation before recomputing start/target rectangles.
- During continuous host resize, prefer immediate target recomputation/snap rather than restarting a 200 ms animation on every `SizeChanged` event.
- Placement or spacing changes initiated as discrete property changes may reflow once.
- Reduced motion snaps all reflow positions immediately.

There is no opacity contract in Stage 8.

---

## Auto-hide Timer Contract

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastAutoHideTimer.cs` containing an internal semantic-delay abstraction and its WinForms implementation. This is not an animation scheduler.

```csharp
internal interface IBootstrapToastAutoHideTimer : IDisposable
{
    int Interval { get; set; }
    bool Enabled { get; }
    event EventHandler? Tick;
    void Start();
    void Stop();
}

internal sealed class WinFormsBootstrapToastAutoHideTimer : IBootstrapToastAutoHideTimer
{
    private readonly Timer _timer = new Timer();

    public int Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    public bool Enabled => _timer.Enabled;

    public event EventHandler? Tick
    {
        add => _timer.Tick += value;
        remove => _timer.Tick -= value;
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();
    public void Dispose() => _timer.Dispose();
}
```

`BootstrapToast` receives a default factory in the public constructor and an internal injectable factory for deterministic tests:

```csharp
public BootstrapToast()
    : this(() => new WinFormsBootstrapToastAutoHideTimer())
{
}

internal BootstrapToast(Func<IBootstrapToastAutoHideTimer> timerFactory)
{
    _timerFactory = timerFactory ?? throw new ArgumentNullException(nameof(timerFactory));
    // initialize normal designer-safe state
}
```

Lifecycle helper rules:

```text
NotifyEnterCompleted:
  if owned && visible && AutoHide && not dismissed -> StartAutoHideTimer()

StartAutoHideTimer:
  StopAndDisposeAutoHideTimer()
  generation++
  create one timer
  Interval = AutoHideDelay
  subscribe Tick with captured generation
  Start()

Tick:
  if sender/current timer/generation/state do not all match -> ignore
  StopAndDisposeAutoHideTimer() first
  call Dismiss() through the normal owner path

AutoHide=false / delay changed / dismissal / removal / disposal:
  generation++
  stop
  unsubscribe
  dispose
  set field null
```

Tests must retain an old fake timer and deliberately fire it after a new timer has been created. The stale callback must be ignored.

---

## File Structure

### Create

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastPlacement.cs` — public placement enum only.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapFeedbackRenderLogic.cs` — internal shared Alert/Toast semantic feedback palette.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastAutoHideTimer.cs` — internal semantic delay abstraction wrapping one WinForms Timer.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastLayoutLogic.cs` — pure DPI/content/stack geometry.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToast.cs` — public notification surface, content painting, close affordance, detached/owned dismissal and auto-hide lifecycle.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs` — public host/owner, queue, enter/exit/reflow transitions, promotion and deterministic disposal.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastLayoutLogicTests.cs` — pure metrics/content/stack tests.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTests.cs` — public contract, presentation, dismissal, auto-hide, theme/DPI/lifecycle tests.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs` — ownership, max-visible queue, animation, promotion, bulk dismissal and disposal tests.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTestDoubles.cs` — fake semantic timer and small animation factory helper that composes existing `ManualAnimationClock` / `ManualAnimationFrameScheduler` patterns.

### Modify

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlertRenderLogic.cs` — delegate semantic palette resolution to `BootstrapFeedbackRenderLogic`; no public behavior change.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertRenderLogicTests.cs` — preserve Alert regression expectations after internal helper extraction.
- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs` — add Toast host, manual/auto-hide/burst/reduced-motion guidance cases.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs` — verify Toast demo presence and stable navigation/theme behavior.
- `docs/COMPONENTS.md` — document public Toast/Container contracts, ownership, max-visible queue, animation/auto-hide semantics.
- `docs/TESTING.md` — add Toast automated/manual matrix and resource-lifetime checks.
- `README.md` — add Toast to supported components/examples.
- `docs/PACKAGE_README.md` — add package-facing Toast usage and ownership warning.
- `CHANGELOG.md` — add Unreleased BootstrapToast entry.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs` or the repository's current baseline test file — approve the new exported surface only after deliberate failure/review.
- `docs/PUBLIC_API_BASELINE.md` — record the approved Stage 8 compatible public API addition and new fingerprint.

Do not modify project files merely to include the new `.cs` files; SDK-style default compile globbing already includes them.

---

## Task 1: Add Placement and Pure Stack/Content Layout Logic

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastPlacement.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastLayoutLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastLayoutLogicTests.cs`

**Interfaces:**
- Produces: public `BootstrapToastPlacement`.
- Produces: internal `BootstrapToastMetrics`, content layout result, `ResolveMetrics(...)`, content layout method, and `CalculateStackBounds(...)` consumed by Tasks 3-5.
- Consumes: `BootstrapThemeMetrics`, `DpiScaler`, `CornerRadius`, standard `Rectangle`/`Size`.

- [ ] **Step 1: Write failing enum/layout validation tests.**

```csharp
[Test]
public void CalculateStackBounds_rejects_invalid_inputs()
{
    var sizes = new[] { new Size(320, 80) };

    Assert.Throws<ArgumentOutOfRangeException>(() =>
        BootstrapToastLayoutLogic.CalculateStackBounds(
            new Rectangle(0, 0, 400, 400),
            BootstrapToastPlacement.TopRight,
            -1,
            5,
            sizes,
            96));

    Assert.Throws<ArgumentOutOfRangeException>(() =>
        BootstrapToastLayoutLogic.CalculateStackBounds(
            new Rectangle(0, 0, 400, 400),
            BootstrapToastPlacement.TopRight,
            8,
            0,
            sizes,
            96));
}
```

Add explicit tests for undefined placement, `dpi <= 0`, null size list, and negative sizes.

- [ ] **Step 2: Write failing top/bottom and left/right stacking tests.**

Use three distinct heights so ordering cannot accidentally pass:

```csharp
var sizes = new[]
{
    new Size(200, 60),
    new Size(220, 80),
    new Size(180, 70)
};
```

Assert exact rectangles for TopLeft, TopRight, BottomLeft, BottomRight with 8 px spacing at 96 DPI. Assert the first item is nearest the selected top/bottom anchor and that output length is capped by `maximumVisibleToasts`.

- [ ] **Step 3: Write failing DPI spacing tests.**

At 96/120/144/168/192 DPI, assert logical spacing `8` scales through `DpiScaler` and no private scaling formula appears in Toast layout logic.

- [ ] **Step 4: Write failing content-layout tests.**

Cover:

```text
Title empty / body only
Title + body
Icon + title + body
Dismissible on/off
Long wrapped body
Narrow caller-selected width
96/120/144/168/192 DPI
Preferred height increases only when content requires it
```

- [ ] **Step 5: Implement the enum and minimal pure logic.**

Use checked, deterministic arithmetic and no WinForms handle creation. Keep all methods/internal structs internal except the public enum.

- [ ] **Step 6: Run focused tests.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToastLayoutLogic"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToastLayoutLogic"
```

Expected: PASS on both targets.

- [ ] **Step 7: Commit.**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastPlacement.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastLayoutLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastLayoutLogicTests.cs
git commit -m "test: define BootstrapToast layout contract"
```

---

## Task 2: Extract the Shared Feedback Palette from Alert

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapFeedbackRenderLogic.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlertRenderLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertRenderLogicTests.cs`

**Interfaces:**
- Produces: internal `BootstrapFeedbackPalette` and `BootstrapFeedbackRenderLogic.ResolvePalette(...)` consumed by Alert and Toast.
- Preserves: all existing Alert public API and rendered palette results.

- [ ] **Step 1: Add failing shared-palette equivalence tests.**

For every defined `BootstrapVariant`, compare shared resolver output to the existing Alert expected surface/border/foreground/focus values under Light and Dark themes. Also verify disabled output uses `SurfaceSecondary`, `Border`, `MutedText`, and `Disabled`.

- [ ] **Step 2: Move the exact Stage 2 formula into `BootstrapFeedbackRenderLogic`.**

Do not alter the constants or contrast threshold during extraction.

- [ ] **Step 3: Update Alert render logic to delegate palette resolution.**

The Alert-specific layout/metrics code remains in `BootstrapAlertRenderLogic`; only semantic palette logic moves.

- [ ] **Step 4: Run Alert regressions and shared helper tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapAlertRenderLogic|BootstrapFeedback"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapAlertRenderLogic|BootstrapFeedback"
```

Expected: no Alert visual contract changes.

- [ ] **Step 5: Commit.**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapFeedbackRenderLogic.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapAlertRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapAlertRenderLogicTests.cs
git commit -m "refactor: share feedback palette logic"
```

---

## Task 3: Implement BootstrapToast Surface and Detached Dismissal

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastAutoHideTimer.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToast.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTestDoubles.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTests.cs`

**Interfaces:**
- Produces: public `BootstrapToast` contract.
- Produces: internal owner attach/detach and enter-complete hooks used only by `BootstrapToastContainer`.
- Produces: internal semantic timer abstraction for deterministic tests.
- Consumes: Tasks 1-2 layout/palette helpers and existing Icon/Theme infrastructure.

- [ ] **Step 1: Write failing public-default and validation tests.**

Assert the exact defaults table, designer-safe construction, `Title` null normalization, `IconRenderer` null rejection, invalid `Variant`, `AutoHideDelay <= 0`, and `AnimationDuration <= 0`.

- [ ] **Step 2: Write failing detached-dismissal tests.**

```csharp
[Test]
[Apartment(ApartmentState.STA)]
public void Dismiss_detached_visible_toast_hides_and_raises_once()
{
    using var toast = new BootstrapToast();
    var count = 0;
    toast.Dismissed += (_, _) => count++;

    toast.Visible = true;
    toast.Dismiss();
    toast.Dismiss();

    Assert.That(toast.Visible, Is.False);
    Assert.That(count, Is.EqualTo(1));

    toast.Visible = true;
    toast.Dismiss();
    Assert.That(count, Is.EqualTo(2));
}
```

Also assert direct `Visible = false` raises no event.

- [ ] **Step 3: Write failing rendering/accessibility tests.**

Cover shared palette use, disabled palette, title/body/icon rectangles, dismissible close visibility/tab stop, close accessible name/description, and close-button keyboard `PerformClick()` routing to `Dismiss()`.

- [ ] **Step 4: Implement the private close button and owner-neutral surface.**

Use owner painting with `RoundedPath`, scoped brushes/pens, shared feedback palette, and `IconRenderer`. The close button must call only `Dismiss()` and must not raise `Dismissed` separately.

- [ ] **Step 5: Implement title-font ownership.**

Create one owned bold derivative of the current Font when needed, rebuild on `OnFontChanged`, and dispose only the derived font. Add a regression test that assigning a caller-owned Font and disposing Toast does not dispose the caller Font.

- [ ] **Step 6: Add theme/DPI lifecycle tests.**

Verify theme change invalidates/re-resolves palette, DPI layout recomputes preferred height/close bounds, disposal unsubscribes theme handlers, and no timer exists merely from construction/detached visibility.

- [ ] **Step 7: Run focused tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToastTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToastTests"
```

- [ ] **Step 8: Commit.**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastAutoHideTimer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToast.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTestDoubles.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTests.cs
git commit -m "feat: add BootstrapToast surface"
```

---

## Task 4: Implement Container Ownership, Queueing, and Static Stack Placement

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToast.cs` only for the internal host hooks defined in Task 3 if compilation requires final wiring.

**Interfaces:**
- Produces: public `BootstrapToastContainer` contract.
- Consumes: `BootstrapToastLayoutLogic.CalculateStackBounds`, internal Toast owner hooks.
- Defers: animation to Task 5; first make ownership/queue logic correct with immediate placement/removal.

- [ ] **Step 1: Write failing defaults/validation tests.**

Assert `TopRight`, spacing 8, max visible 5, non-tab-stop, invalid placement, negative spacing, and max visible <=0.

- [ ] **Step 2: Write failing transfer-of-ownership tests.**

Cover null, disposed toast, same-container duplicate, different-container duplicate, caller reference retained but ownership held by container, and deterministic child disposal after immediate test-mode dismissal.

- [ ] **Step 3: Write failing max-visible queue tests.**

With `MaximumVisibleToasts = 2`, show four distinct toasts and assert:

```text
Toast 1 visible
Toast 2 visible
Toast 3 queued/hidden
Toast 4 queued/hidden
No queued toast owns a running auto-hide timer
Dismiss Toast 1 -> Toast 3 is next promotion candidate
Dismiss Toast 2 -> Toast 4 is next promotion candidate
```

- [ ] **Step 4: Write failing placement/reflow tests using real container bounds.**

Assert TopLeft/TopRight/BottomLeft/BottomRight consume the pure layout result and update after host resize/spacing/placement changes without changing ownership order.

- [ ] **Step 5: Write failing dismissal/disposal tests.**

Assert exactly-once public `Dismissed`, queued dismissal never becomes visible, `DismissAll()` dismisses all without promotion churn, and container `Dispose()` disposes owned toasts without raising `Dismissed`.

- [ ] **Step 6: Implement minimal ownership list and queue promotion.**

Keep one private ordered entry list. Do not use a second public collection. Direct `Controls` manipulation is not a supported API contract; internal container code is the only code that should add/remove owned Toast children.

- [ ] **Step 7: Run focused container tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToastContainerTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToastContainerTests"
```

- [ ] **Step 8: Commit.**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToast.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs
git commit -m "feat: add BootstrapToast ownership and stacking"
```

---

## Task 5: Add Deterministic Enter, Exit, and Reflow Animation

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTestDoubles.cs`

**Interfaces:**
- Consumes: existing `BootstrapAnimation`, `BootstrapEasing`, existing `ManualAnimationClock` / `ManualAnimationFrameScheduler` pattern.
- Produces: one active transition per Toast entry plus at most one container reflow animation.

Use an internal constructor/factory seam matching existing animated controls rather than exposing animation objects publicly:

```csharp
internal BootstrapToastContainer(
    Func<TimeSpan, Func<double, double>, Control, BootstrapAnimation> animationFactory)
{
    _animationFactory = animationFactory ?? throw new ArgumentNullException(nameof(animationFactory));
    InitializeDefaults();
}
```

The public constructor supplies the normal factory:

```csharp
public BootstrapToastContainer()
    : this((duration, easing, owner) => new BootstrapAnimation(duration, easing, owner))
{
}
```

- [ ] **Step 1: Write failing deterministic enter tests.**

Use manual clock/scheduler to assert start offset, midpoint, completion bounds, `EaseOut`, state transition to Visible, and exactly one enter animation.

- [ ] **Step 2: Write failing dismiss-while-entering tests.**

Advance enter to a non-terminal progress, call `Dismiss()`, assert enter scheduler stops/disposes, exit begins from current visual bounds without snapping, and one `Dismissed` event is raised.

- [ ] **Step 3: Write failing exit/disposal/promotion tests.**

Assert toast is not disposed before exit completion, is disposed exactly at completion, then the oldest queued toast begins enter.

- [ ] **Step 4: Write failing reduced-motion tests.**

Set a reduced-motion theme/provider according to existing animation test conventions and assert enter/exit/reflow complete synchronously while ownership/event/disposal ordering remains identical.

- [ ] **Step 5: Write failing hidden-container and disposal-during-transition tests.**

Verify a hidden container does not continue scheduling visible transition frames; when shown, the shared animation owner lifecycle resumes appropriately. Disposing the container during enter/exit stops all schedulers and no completion callback mutates disposed controls.

- [ ] **Step 6: Write failing reflow tests.**

Dismiss the first of three visible toasts and assert survivors move toward their new stack targets through one container reflow animation. Assert a rapid second dismissal cancels stale reflow before calculating the next geometry.

- [ ] **Step 7: Implement the transition state machine.**

Use `BootstrapToastHostState` internally. Every method starts by rejecting disposed/stale entries. Completion handlers must verify that the completing animation is still the entry/container's current animation before mutating state.

- [ ] **Step 8: Run deterministic animation tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToastContainerTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToastContainerTests"
```

- [ ] **Step 9: Commit.**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTestDoubles.cs
git commit -m "feat: animate BootstrapToast lifecycle"
```

---

## Task 6: Implement Auto-hide Timer Lifecycle and Stale-tick Protection

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToast.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTestDoubles.cs`

**Interfaces:**
- Consumes: internal `IBootstrapToastAutoHideTimer`.
- Container calls Toast internal `NotifyEnterCompleted()` after true visual completion.
- Toast calls ordinary `Dismiss()` from the valid auto-hide tick path.

- [ ] **Step 1: Write failing timer-start timing tests.**

Assert no timer at construction, no timer while queued, no timer while entering, and exactly one timer created/started after enter completion when `AutoHide = true`.

- [ ] **Step 2: Write failing manual-dismiss timer cleanup tests.**

After fully shown, call `Dismiss()` and assert timer `Stop`, event unsubscribe, `Dispose`, and field release happen before public `Dismissed` observation can see an active timer.

- [ ] **Step 3: Write failing property-change tests.**

```text
AutoHide true -> false: current timer disposed
AutoHide false -> true while fully visible: fresh timer created with full delay
AutoHideDelay 5000 -> 1500 while visible: old timer disposed, fresh timer Interval=1500
AnimationDuration change: does not alter current auto-hide interval
```

- [ ] **Step 4: Write failing stale-tick test.**

Retain fake timer A, restart auto-hide so timer B is current, then manually fire A. Assert no dismissal. Fire B and assert one dismissal.

- [ ] **Step 5: Write failing reduced-motion auto-hide test.**

Reduced motion completes enter immediately, but Toast must still wait for the semantic timer tick; it must not auto-dismiss merely because animation was skipped.

- [ ] **Step 6: Write failing disposal tests.**

Disposing Toast or Container while timer is active must stop/dispose timer and a subsequent fake tick must be ignored without exception/event.

- [ ] **Step 7: Implement generation-guarded lifecycle exactly once.**

Use one helper for stop/unsubscribe/dispose and call it from every dismissal/removal/disposal/property-change path. Do not duplicate cleanup code in the container.

- [ ] **Step 8: Run Toast + Container tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToast"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToast"
```

- [ ] **Step 9: Commit.**

```bash
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToast.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTestDoubles.cs
git commit -m "feat: add BootstrapToast auto-hide lifecycle"
```

---

## Task 7: Extend the Feedback Demo and Add Manual/Stress Scenarios

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs`

**Interfaces:**
- Consumes: final public Toast/Container API only. Demo must not reach into internal state.

- [ ] **Step 1: Add failing demo presence/navigation tests.**

Verify `FeedbackDemoForm` contains a `BootstrapToastContainer`, buttons/actions that can produce manual and auto-hide Toasts, and the existing Badge/Alert/Tooltip sections remain present.

- [ ] **Step 2: Add a dedicated Toast section to FeedbackDemoForm.**

Include controls/actions for:

```text
Show manual Toast (AutoHide=false)
Show auto-hide Toast
Show icon + title + multiline body Toast
Burst 8 Toasts to exercise MaximumVisibleToasts queueing
Dismiss All
Cycle TopLeft / TopRight / BottomLeft / BottomRight
Rapid show then dismiss
Disabled Toast presentation
```

Use at least Success, Warning, Danger, and Info variants across examples. Keep the existing integrated Light/Dark switch behavior; do not add a second theme controller.

- [ ] **Step 3: Add reduced-motion and DPI guidance text.**

Explain that the existing theme reduced-motion option should make transitions immediate while auto-hide delay remains observable. Repeat manual verification at 100%, 125%, 150%, 175%, and 200% Windows display scaling.

- [ ] **Step 4: Add a resource-stress demo action.**

A button may create a bounded burst (for example 100 toasts in batches) through the public API and allow `DismissAll()`. Do not add P/Invoke or product diagnostics solely for the demo.

Manual stress procedure:

```text
1. Open Feedback demo and note process handle counts using Task Manager/Process Explorer.
2. Run repeated burst + dismiss-all cycles until several hundred Toast instances have been created/disposed.
3. Return to idle and allow GC naturally; repeat Light/Dark switching.
4. Verify no continually growing timer activity, obvious USER/GDI handle climb, retained visible child controls, or post-disposal exceptions.
5. Repeat with reduced motion and with AutoHide enabled.
```

- [ ] **Step 5: Run demo tests on both targets and launch the demo manually.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FeedbackDemoForm"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FeedbackDemoForm"
```

Manual expected behavior:

- No focus steal when Toast appears.
- Close button works by mouse and keyboard.
- At most configured visible count is shown.
- Overflow appears in FIFO order as slots free.
- DismissAll produces no queued flashes.
- Rapid close during enter does not jump or duplicate events.
- Light/Dark switch recolors existing visible toasts without resetting their lifetimes.
- Reduced motion removes movement but preserves auto-hide timing.
- No clipped title/body/close glyph across the required DPI matrix.

- [ ] **Step 6: Commit.**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs
git commit -m "demo: showcase BootstrapToast"
```

---

## Task 8: Documentation and Public API Baseline Review

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: current `Phase16PublicApiBaselineTests` file
- Modify: `docs/PUBLIC_API_BASELINE.md`

**Interfaces:**
- Documents exactly the public contract above; no undocumented convenience members.

- [ ] **Step 1: Update component documentation.**

Document:

```text
BootstrapToast purpose and public properties
BootstrapToastContainer ownership transfer
Top/Bottom + Left/Right placements
FIFO max-visible queue policy
Dismissed event timing vs disposal timing
AutoHide timer starts only after enter completion
AnimationDuration and reduced-motion behavior
Caller responsibility before vs after ShowToast
No global/top-level notification service in this version
```

Include a package-facing example:

```csharp
var toast = new BootstrapToast
{
    Title = "Saved",
    Text = "Changes were saved successfully.",
    Variant = BootstrapVariant.Success,
    AutoHide = true,
    AutoHideDelay = 5000
};

toastContainer.ShowToast(toast); // ownership transfers here
```

Do not show `using`/manual disposal around a successfully transferred Toast.

- [ ] **Step 2: Update testing documentation and changelog.**

Add automated coverage categories for pure stack layout, ownership, exactly-once dismissal, deterministic animations, reduced motion, stale timer ticks, queue promotion, disposal, and both targets. Add the manual Light/Dark/DPI/resource-stress matrix. Add one Unreleased changelog entry without rewriting prior release history.

- [ ] **Step 3: Run the public API baseline test before updating it.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline"
```

Expected: FAIL because Stage 8 intentionally adds public API.

- [ ] **Step 4: Review the reconstructed exported API.**

Accept only the intended additions:

```text
BootstrapToastPlacement
BootstrapToast
  Title
  Variant
  Icon
  IconRenderer
  Dismissible
  AutoHide
  AutoHideDelay
  AnimationDuration
  Dismissed
  Dismiss()
BootstrapToastContainer
  Placement
  ToastSpacing
  MaximumVisibleToasts
  ShowToast(BootstrapToast)
  DismissAll()
```

Inherited WinForms API is naturally exported by the base types; do not add redundant aliases. Confirm none of the internal palette/timer/layout/host-state types entered the exported baseline.

- [ ] **Step 5: Update the approved baseline and docs deliberately.**

Update the test fingerprint and `docs/PUBLIC_API_BASELINE.md` with a named compatible Stage 8 addition only after the surface above is confirmed.

- [ ] **Step 6: Re-run the baseline test on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline"
```

Expected: PASS.

- [ ] **Step 7: Commit docs and baseline.**

```bash
git add docs/COMPONENTS.md docs/TESTING.md README.md docs/PACKAGE_README.md CHANGELOG.md docs/PUBLIC_API_BASELINE.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests
git commit -m "docs: document BootstrapToast public API"
```

---

## Task 9: Final Cross-target Verification and Stage Gate

**Files:**
- No new files expected; fixes from failures stay within Stage 8 files unless a genuine shared-infrastructure defect is discovered.

- [ ] **Step 1: Build the library for both targets.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
```

Expected: zero warnings/errors.

- [ ] **Step 2: Run the focused Stage 8 suite on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToast|BootstrapFeedback|FeedbackDemoForm"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToast|BootstrapFeedback|FeedbackDemoForm"
```

- [ ] **Step 3: Run animation and Alert regressions.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapAnimation|BootstrapAlert"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapAnimation|BootstrapAlert"
```

Expected: shared palette extraction and Toast usage do not regress Alert or animation primitives.

- [ ] **Step 4: Run the full test suite.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: PASS on both targets.

- [ ] **Step 5: Run final manual matrix.**

Verify Feedback demo at 100%, 125%, 150%, 175%, 200% DPI with:

```text
Light and Dark themes
Reduced motion on/off
Manual and auto-hide
Title/body/icon combinations
Dismissible true/false
All four placements
MaximumVisibleToasts = 1, 2, 5
Burst queueing and FIFO promotion
Dismiss while entering
DismissAll during active exits
Host hide/show during enter
Host resize while populated
Theme switch while timer is active
Container disposal while timer/animation active
Hundreds-of-toasts resource stress
```

- [ ] **Step 6: Inspect git diff and public surface.**

Confirm:

```text
No new package dependency
No top-level Toast Form/global service
No copied animation frame timer
No second semantic feedback palette
No public implementation/testing seam
No generated bin/obj/package files
All public members have XML docs
Ownership transfer is explicit in XML/package docs
```

- [ ] **Step 7: Final Stage 8 commit if verification fixes were needed.**

```bash
git add -A
git commit -m "feat: complete BootstrapToast"
```

If no verification fixes were required, do not create an empty commit.

---

## Acceptance Checklist

Stage 8 is complete only when all of the following are true:

- [ ] `BootstrapToastPlacement`, `BootstrapToast`, and `BootstrapToastContainer` match the approved public contract exactly.
- [ ] `BootstrapAlert` and Toast share one internal semantic feedback palette resolver; no duplicate hard-coded semantic table/formula exists.
- [ ] Toast title/text/icon/close presentation works in Light/Dark and disabled state.
- [ ] Close affordance is keyboard accessible and uses the framework icon renderer.
- [ ] `ShowToast` clearly and deterministically transfers ownership.
- [ ] One Toast cannot be hosted twice.
- [ ] Maximum-visible queueing is FIFO and queued Toasts do not start timers or animations.
- [ ] TopLeft/TopRight/BottomLeft/BottomRight stack geometry is covered by pure tests.
- [ ] Spacing/layout scale correctly at 96/120/144/168/192 DPI.
- [ ] Enter/exit/reflow use only `BootstrapAnimation` for frame progression.
- [ ] Dismiss during enter reverses into a clean exit without visual jump or concurrent animations.
- [ ] Reduced motion skips transition frames but preserves semantic auto-hide delay.
- [ ] Auto-hide timer starts only after enter completion.
- [ ] Auto-hide timer stops/disposes before manual/auto dismissal and on removal/disposal.
- [ ] Stale timer ticks are ignored.
- [ ] `Dismissed` is exactly once per accepted logical dismissal.
- [ ] Visible owned Toasts are disposed only after exit completion; queued dismissed Toasts are disposed immediately.
- [ ] Container disposal cleans all owned Toasts/animations/timers without synthesizing dismissal events.
- [ ] `DismissAll()` does not promote queued Toasts during the bulk dismissal.
- [ ] Runtime theme switching does not reset queue/timer/lifecycle state.
- [ ] Feedback demo covers manual, auto-hide, burst, placement, rapid dismiss, reduced-motion, theme, and DPI cases.
- [ ] Repeated create/show/dismiss stress reveals no unbounded timer/event/GDI/USER-handle growth.
- [ ] `docs/COMPONENTS.md`, `docs/TESTING.md`, `README.md`, `docs/PACKAGE_README.md`, and `CHANGELOG.md` are updated.
- [ ] The frozen public API failure was reviewed intentionally before updating the approved fingerprint.
- [ ] Both target builds pass.
- [ ] Focused Stage 8 tests pass on both targets.
- [ ] Full tests pass on both targets.

---

## Explicit Non-goals for Stage 8

Do not add these while executing this plan:

- Global/static `ToastService`, `ToastManager`, DI service, or application singleton.
- Top-level, borderless, layered, transparent, click-through, monitor-aware Toast windows.
- Native Windows notification center integration.
- Center/TopCenter/BottomCenter/custom-coordinate placement.
- Screen-edge placement independent of an application-provided container.
- Hover-to-pause auto-hide.
- Swipe/drag gestures.
- Toast actions/action-button collections.
- Arbitrary child-content templates.
- Progress Toasts.
- Async Toast factory/provider APIs.
- Persistence, deduplication, history, grouping, channels, priorities, or rate limiting.
- Close-reason public enums.
- Public animation progress/state/queue collection APIs.
- Public access to the semantic timer, animation objects, close button, or container entry objects.
- A second animation frame scheduler or copied timer loop.
- A Toast-specific semantic palette table.

Any of those requires a separate roadmap/design decision after Stage 8 is green.