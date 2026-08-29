# Global Toast Service and Notification Center Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the existing BootstrapToast implementation with an application-level `BootstrapToastService` that can show notifications without an application-placed container by creating its own non-activating top-level host windows, while also adding bounded in-memory notification history and an interactive notification-center window.

**Architecture:** Preserve `BootstrapToast` and `BootstrapToastContainer` as the rendering, auto-hide, animation, queueing, and ownership primitives. `BootstrapToastService` is a UI-thread-affine orchestrator that creates one internal host `Form` per target screen, places an existing `BootstrapToastContainer` inside each host, and routes service-created Toasts to the appropriate screen. The service separately owns an in-memory history store and one interactive notification-center `Form`; the center reads snapshots from the store, never owns live Toast controls, and can be opened independently of the transient host windows. Existing application-placed Toast containers continue to behave exactly as before.

**Tech Stack:** C#, native Windows Forms `Form` / `ListBox` / `Control` / `Screen`, existing `BootstrapToast`, `BootstrapToastContainer`, Theme / Rendering / Icons / Animation / Compatibility infrastructure, `BootstrapVariant`, `IconDescriptor`, `IIconRenderer`, `DpiScaler`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** User request dated 2026-08-29. Compatibility baseline: `docs/plans/20260828-008-bootstrap-toast.md` and the implemented Stage 8 Toast sources/tests. Repository-wide constraints: `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`.

## Global Constraints

- Keep the root namespace `MyDmsVn.Bootstrap5WinFormUI`; new public Toast-service types remain under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must continue to compile from one shared implementation for both `net48` and `net8.0-windows` wherever practical.
- This plan intentionally supersedes only the Stage 8 exclusions for a global/application-level Toast service, top-level Toast windows, and notification history. It does **not** supersede the existing `BootstrapToast` or `BootstrapToastContainer` public contracts, ownership rules, dismissal timing, animation behavior, or application-placed hosting model.
- Existing applications that instantiate `BootstrapToast` and `BootstrapToastContainer` directly must not be required to use the new service.
- `BootstrapToastService` is an application-process service, not a Windows notification-platform bridge. Do not use WinRT Toast notifications, Windows Action Center APIs, tray-balloon APIs, COM activation, AppUserModelID registration, registry integration, or operating-system notification persistence.
- History is in-memory for the lifetime of one service instance. Do not add JSON/XML/database/registry persistence, cross-process synchronization, roaming, cloud synchronization, or migration/versioning in this change.
- Do not add a service locator, dependency-injection package, background worker, hidden worker thread, second UI thread, custom message pump, `Application.DoEvents`, or polling loop.
- Service construction and all service operations are UI-thread-affine. The creating thread must be STA. Calls from another thread throw `InvalidOperationException` with a clear message instead of silently marshaling to an arbitrary UI context.
- Do not use `Task.Delay`, `Thread.Sleep`, thread-pool timers, or a second animation scheduler. Transient Toast animation and auto-hide remain owned by the existing Toast/ToastContainer implementation.
- The service must reuse `BootstrapToastContainer` for stacking, FIFO queueing, visible-slot limits, enter/exit animation, reflow, auto-hide handoff, and deterministic Toast disposal. Do not implement a parallel service-specific Toast state machine.
- The service owns every `BootstrapToast` instance it creates. After creation, ownership transfers immediately to a service-owned `BootstrapToastContainer`; application code never receives the live Toast control.
- The service may create top-level Forms, but transient host windows must be borderless, omitted from the taskbar, non-activating, and absent from Alt+Tab. The notification-center window is intentionally interactive and may activate when explicitly opened by the application/user.
- Global Toast host windows must not steal keyboard focus from the current application or another application merely because a Toast appears.
- `TopMost` defaults to `false`. The service is application-global, not system-authoritative. Applications that deliberately need always-on-top transient feedback may opt in.
- Resolve the target screen from a live `relativeTo` control when supplied; otherwise prefer `Form.ActiveForm`; otherwise use the primary screen. Do not invent a global cursor-following placement policy.
- Maintain at most one active transient host window per resolved `Screen.DeviceName`. Reuse the host for subsequent notifications on that screen.
- Multi-monitor coordinates use `Screen.WorkingArea` in screen pixels. Public spacing, margin, and Toast-width settings are logical 96-DPI values and are scaled by the host/target DPI before use.
- Do not make this plan depend on `docs/plans/20260829-001-interactive-tooltip-popover-placement-engine.md`. Toasts are corner-anchored to a screen working area, not anchored floating surfaces that need Popper-style collision selection.
- Host windows must never reserve the Windows taskbar area; use the screen working area rather than full monitor bounds.
- A top-level host may cover the target working area geometrically to let the existing container calculate corner stacks, but its native window region must be reduced to the active Toast stack/animation envelope so blank host space does not consume pointer input. Region refreshes must be coalesced and old `Region` instances must be disposed deterministically.
- The existing `BootstrapToastContainer.MaximumVisibleToasts` count remains authoritative. Add only internal height-aware hosting support needed to prevent a top-level stack from extending beyond the available working-area height; application-placed containers retain their current unlimited-by-height behavior unless that internal limit is explicitly set by the service host.
- Global-host Toasts remain mouse-dismissible through their existing close affordance. Because the transient top-level host is intentionally non-activating, keyboard review/action belongs to the persistent notification center rather than to the transient overlay.
- Notification history records semantic data only. Do not retain a live `BootstrapToast`, `Control`, `Image`, `Icon`, renderer, or other caller/framework UI object after a notification has been added to history.
- Notification history snapshots include notification id, UTC creation time, title, text, semantic variant, and read state. The center uses semantic variant presentation rather than retaining arbitrary icon resources.
- New history entries are unread. Toast auto-hide, close-button dismissal, `DismissAll()`, application deactivation, or host disposal do not mark them read.
- Merely opening the notification center does not mark all entries read. Explicit item activation marks that item read; `MarkAllAsRead()` is a separate operation.
- `ClearHistory()` changes history only. It never dismisses live Toasts. `DismissAll()` changes transient Toasts only. It never clears history.
- History capacity defaults to `100`, must be greater than zero, and trims the oldest entries immediately when capacity is reduced or exceeded.
- `GetHistory()` returns a newest-first snapshot. Returned `BootstrapToastHistoryItem` objects are immutable snapshots; later read-state changes do not mutate objects previously returned by a caller.
- `HistoryChanged` fires once for each effective history mutation batch: add, successful single-item read, successful mark-all, non-empty clear, or capacity trim. No-op operations do not raise it.
- Runtime Light/Dark theme changes repaint transient Toasts through their existing contract and repaint the notification center without changing history order/read state or restarting transient Toast timers.
- DPI/display changes reposition active top-level windows and recompute logical margins/sizes without replacing history or silently moving caller-owned application windows.
- Subscribe to `SystemEvents.DisplaySettingsChanged` only if required for live screen-topology refresh, and always unsubscribe during service disposal. No static event handler may keep a disposed service alive.
- Caller-assigned `IIconRenderer` instances are never disposed by the service. A renderer setting is snapshotted onto newly created Toast controls; changing it does not rewrite already-owned live Toasts.
- Designer construction of existing Toast controls must remain unchanged and safe. The new service is a runtime API and must not create a top-level window merely because the assembly is loaded or the static `Default` property has not been accessed.
- All new public/protected members receive XML documentation. `TreatWarningsAsErrors` and `CS1591` remain green.
- Every new public/protected type/member changes the frozen public API. `Phase16PublicApiBaselineTests` must intentionally fail before the approved fingerprint and `docs/PUBLIC_API_BASELINE.md` are updated.
- Final completion requires both target builds, focused and full tests, Feedback demo/manual verification, multi-monitor/DPI checks where hardware is available, focus-stealing checks, history-capacity/read-state checks, resource-lifetime checks, documentation updates, and deliberate public API review.

---

## Compatibility Baseline and Superseded Exclusions

The existing Stage 8 plan deliberately chose an application-placed `BootstrapToastContainer` and declared these capabilities out of scope:

```text
- global/static Toast manager
- top-level Toast Form/window
- screen/monitor-aware overlay hosting
- notification history
- global notification service
```

Those exclusions were appropriate for the first Toast implementation. The new request explicitly adds those capabilities, so this plan supersedes only that exclusion list.

The following existing behavior remains the compatibility baseline:

```text
BootstrapToast
  -> owns visual content, dismissibility, auto-hide delay, theme/DPI rendering

BootstrapToastContainer
  -> owns transferred Toast controls
  -> FIFO queue
  -> MaximumVisibleToasts
  -> corner stack placement
  -> enter/exit/reflow animation
  -> semantic DismissAll
  -> disposes owned Toast after exit
```

The new dependency direction is:

```text
BootstrapToastService
    |
    +--> BootstrapToastOptions -> creates BootstrapToast
    |
    +--> per-screen BootstrapToastHostWindow
    |       |
    |       +--> existing BootstrapToastContainer
    |               |
    |               +--> existing BootstrapToast
    |
    +--> BootstrapToastHistoryStore
    |       |
    |       +--> immutable BootstrapToastHistoryItem snapshots
    |
    +--> one BootstrapNotificationCenterWindow
            |
            +--> reads history snapshots only
```

There is deliberately no dependency from `BootstrapToast`, `BootstrapToastContainer`, Theme, Rendering, Animation, or Icons back to the service.

---

## Scope

### In scope

1. Public `BootstrapToastOptions` for service-created transient Toast content/behavior.
2. Public immutable `BootstrapToastHistoryItem` snapshots.
3. Public `BootstrapToastService`, including a lazily created application-wide `Default` instance.
4. UI-thread-affine service semantics with explicit wrong-thread failures.
5. One reusable internal non-activating top-level Toast host window per target screen.
6. Existing `BootstrapToastContainer` inside each host, preserving FIFO, auto-hide, animation, and disposal semantics.
7. Per-screen working-area placement with TopLeft/TopRight/BottomLeft/BottomRight alignment.
8. Logical screen margin, Toast width, spacing, maximum-visible count, and optional `TopMost` behavior.
9. Internal height-aware top-level-host constraint so a service-created stack cannot grow beyond its current screen working area.
10. Bounded in-memory history, read/unread state, unread count, capacity trimming, clear, and change notification.
11. One internal interactive notification-center window with newest-first history, read/unread presentation, item activation, Mark all read, Clear, Close, Escape close, keyboard navigation, theme, and DPI support.
12. Feedback demo scenarios for global service, multi-screen anchoring, queueing, notification center, history-disabled notifications, unread state, and clearing.
13. Documentation and public API baseline updates.

### Explicitly deferred

- Persistence across application restarts.
- Windows Action Center / WinRT native Toast notifications.
- Tray icon integration.
- Cross-process notification aggregation.
- A background-thread-safe `ShowAsync`/dispatcher API.
- A dedicated Toast UI thread or custom application context.
- Explicit `Screen`-parameter public overloads; `relativeTo` + active-form + primary-screen routing is the v1 screen-selection contract.
- Per-notification custom screen coordinates.
- Center docking into an application-owned Form.
- Search/filter/grouping/categories in notification history.
- Per-item delete/archive/pin/snooze.
- History persistence adapters/providers.
- Notification actions/buttons, callbacks, deep links, or arbitrary controls inside history rows.
- Sound, vibration, system attention requests, flash-window behavior.
- Hover-to-pause or mouse-enter timer semantics.
- Deduplication/coalescing/throttling/rate limiting.
- Updating/replacing an already-shown notification by id.
- Dismissing a live Toast by notification id.
- Retaining custom icon/image objects in history.
- Global keyboard shortcuts or mouse/keyboard hooks.
- Depending on the separate Popper-like overlay placement plan.

---

## Public Contract to Add

### 1. BootstrapToastOptions

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastOptions.cs`:

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public sealed class BootstrapToastOptions
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public BootstrapVariant Variant { get; set; } = BootstrapVariant.Primary;
    public IconDescriptor? Icon { get; set; }
    public bool Dismissible { get; set; } = true;
    public bool AutoHide { get; set; } = true;
    public int AutoHideDelay { get; set; } = 5000;
    public int AnimationDuration { get; set; } = 200;
    public bool IncludeInHistory { get; set; } = true;
}
```

Rules:

- `Title` and `Text` normalize `null` to `string.Empty`.
- `Variant` rejects undefined enum values using the same validation convention as `BootstrapToast`.
- `AutoHideDelay` must be greater than zero even when `AutoHide == false`; this keeps options directly transferable to a Toast without order-dependent validation.
- `AnimationDuration` must be greater than zero.
- `Icon` is not copied into history. It is used only while constructing the live transient Toast.
- `IncludeInHistory = false` suppresses only history/unread creation. It does not change display, auto-hide, dismissal, or the returned notification id.
- The options object is caller-owned and can be reused. `Show()` snapshots its values before creating the Toast; later caller mutations do not mutate an already-shown notification.

### 2. BootstrapToastHistoryItem

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryItem.cs`:

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public sealed class BootstrapToastHistoryItem
{
    internal BootstrapToastHistoryItem(
        Guid id,
        DateTimeOffset createdAtUtc,
        string title,
        string text,
        BootstrapVariant variant,
        bool isRead)
    {
        Id = id;
        CreatedAtUtc = createdAtUtc;
        Title = title;
        Text = text;
        Variant = variant;
        IsRead = isRead;
    }

    public Guid Id { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public string Title { get; }
    public string Text { get; }
    public BootstrapVariant Variant { get; }
    public bool IsRead { get; }
}
```

Snapshot semantics:

- Public construction is intentionally absent; history entries come from the service.
- `CreatedAtUtc` is always UTC (`Offset == TimeSpan.Zero`).
- All string values are non-null.
- All public properties are immutable.
- Marking an item read replaces the store entry with a new immutable snapshot rather than mutating an object previously returned from `GetHistory()`.

### 3. BootstrapToastService

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastService.cs`:

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public sealed class BootstrapToastService : IDisposable
{
    public BootstrapToastService();

    public static BootstrapToastService Default { get; }

    public BootstrapToastPlacement Placement { get; set; } = BootstrapToastPlacement.TopRight;
    public int ToastSpacing { get; set; } = 8;
    public int MaximumVisibleToasts { get; set; } = 5;
    public int ToastWidth { get; set; } = 320;
    public Padding ScreenMargin { get; set; } = new Padding(16);
    public bool TopMost { get; set; }
    public int HistoryCapacity { get; set; } = 100;
    public IIconRenderer IconRenderer { get; set; }

    public int UnreadCount { get; }
    public bool IsNotificationCenterVisible { get; }

    public event EventHandler? HistoryChanged;

    public Guid Show(string text, Control? relativeTo = null);
    public Guid Show(BootstrapToastOptions options, Control? relativeTo = null);

    public void DismissAll();

    public IReadOnlyList<BootstrapToastHistoryItem> GetHistory();
    public bool MarkAsRead(Guid notificationId);
    public void MarkAllAsRead();
    public void ClearHistory();

    public void ShowNotificationCenter(Control? relativeTo = null);
    public void HideNotificationCenter();
    public void ToggleNotificationCenter(Control? relativeTo = null);

    public void Dispose();
}
```

Defaults and validation:

| Member | Default / rule |
| --- | --- |
| `Placement` | `TopRight`; undefined enum rejected before mutation |
| `ToastSpacing` | `8` logical px; values `< 0` rejected |
| `MaximumVisibleToasts` | `5`; values `<= 0` rejected |
| `ToastWidth` | `320` logical px; values `<= 0` rejected |
| `ScreenMargin` | `16,16,16,16` logical px; any negative edge rejected |
| `TopMost` | `false` |
| `HistoryCapacity` | `100`; values `<= 0` rejected; lowering trims oldest immediately |
| `IconRenderer` | `BootstrapIconRenderer.CreateDefault()`; `null` rejected; caller-owned renderer is never disposed |
| `UnreadCount` | number of retained history snapshots with `IsRead == false` |
| `IsNotificationCenterVisible` | actual current visibility of the service-owned center window |

Service configuration timing:

- `Placement`, `ToastSpacing`, `MaximumVisibleToasts`, `ScreenMargin`, and `TopMost` apply immediately to existing service-owned host windows and to future hosts.
- `ToastWidth` applies when each new Toast is created. Existing/queued Toast controls keep the width they were assigned at `Show()` time.
- `IconRenderer` applies when each new Toast is created. Existing Toast controls keep their current renderer reference.
- `HistoryCapacity` applies immediately and may trim history.

### 4. Show semantics

`Show(string text, Control? relativeTo = null)` is exactly the convenience form:

```csharp
return Show(
    new BootstrapToastOptions
    {
        Text = text ?? string.Empty
    },
    relativeTo);
```

`Show(BootstrapToastOptions options, Control? relativeTo = null)` performs this sequence on the service UI thread:

```text
1. Verify service is not disposed and call is on the creating STA thread.
2. Validate and snapshot BootstrapToastOptions.
3. Resolve target Screen from relativeTo -> Form.ActiveForm -> Screen.PrimaryScreen.
4. Generate one Guid notification id.
5. If IncludeInHistory, add an unread immutable history snapshot with DateTimeOffset.UtcNow.
6. Get/create the per-screen top-level host.
7. Create one BootstrapToast, copy the snapshotted Toast options, service IconRenderer, and DPI-scaled width.
8. Transfer ownership to the host's existing BootstrapToastContainer.
9. Return the Guid.
```

Failure atomicity:

- Validation failures happen before history or host mutation.
- If host/Toast creation fails after a history item was tentatively prepared, do not leave a history-only entry for a notification that never successfully transferred to a host. Add history only after host existence is established and immediately before the transfer, with rollback if transfer throws.
- If `HistoryChanged` application event handlers throw, the Toast/history state remains committed. Do not roll back already-valid framework state because external observer code failed.

Notification ids:

- A new `Guid` is generated for every successful `Show()` call, including `IncludeInHistory = false` notifications.
- The id is correlation metadata only in this version. It does not expose a live Toast or add a `Dismiss(id)` API.

### 5. Default service lifetime

`BootstrapToastService.Default` is lazy:

```text
Assembly load
  -> no service
  -> no Form
  -> no SystemEvents subscription

first Default access on STA UI thread
  -> create default service
  -> still no top-level host Form until Show()/ShowNotificationCenter()

Application.ApplicationExit
  -> dispose default service
  -> dispose host/center windows
  -> detach static/system events
  -> clear static default reference
```

Additional rules:

- First `Default` access from a non-STA thread throws `InvalidOperationException`; it does not bind the singleton to the wrong thread.
- Explicitly disposing the current default instance is supported. The static reference is cleared, and a later `Default` access on a valid STA UI thread creates a fresh service with empty history.
- A manually constructed service is caller-owned and is not placed into the `Default` slot.
- No default service instance is created during designer construction of unrelated controls.

---

## Internal Service Boundaries

Keep service orchestration testable without exposing implementation details publicly.

### Screen resolution

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastScreenResolver.cs`:

```csharp
internal readonly struct BootstrapToastScreenInfo
{
    public BootstrapToastScreenInfo(string deviceName, Rectangle workingArea)
    {
        DeviceName = deviceName;
        WorkingArea = workingArea;
    }

    public string DeviceName { get; }
    public Rectangle WorkingArea { get; }
}

internal interface IBootstrapToastScreenResolver
{
    BootstrapToastScreenInfo Resolve(Control? relativeTo);
    IReadOnlyList<BootstrapToastScreenInfo> GetCurrentScreens();
}
```

Production resolution:

```text
live relativeTo with usable bounds
    -> Screen.FromControl(relativeTo)
else live Form.ActiveForm
    -> Screen.FromControl(Form.ActiveForm)
else
    -> Screen.PrimaryScreen
```

If `Screen.PrimaryScreen` is unexpectedly unavailable, throw `InvalidOperationException` before creating a host rather than inventing `(0,0)` monitor geometry.

### Layout logic

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceLayoutLogic.cs` as pure internal geometry helpers.

Required operations:

```csharp
internal static Rectangle InsetWorkingArea(
    Rectangle workingArea,
    Padding logicalMargin,
    int dpi);

internal static int ResolveToastWidth(
    int logicalToastWidth,
    int availablePixelWidth,
    int dpi);

internal static Rectangle CalculateNotificationCenterBounds(
    Rectangle availableWorkingArea,
    Size desiredPixelSize,
    BootstrapToastPlacement placement);
```

Rules:

- Scale each logical margin edge independently through `DpiScaler`.
- Never produce negative available width/height; clamp to at least one pixel after validated margins.
- Toast width is the DPI-scaled requested width clamped to the available working-area width.
- Notification center uses the same corner selected by `Placement`; TopLeft/BottomLeft align left and TopRight/BottomRight align right. Top placements align top; Bottom placements align bottom.
- Layout logic is pure: no `Screen`, `Control`, `Form`, theme, handle, or static mutable state.

### Host contract

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceHostContracts.cs`:

```csharp
internal readonly struct BootstrapToastHostSettings
{
    public BootstrapToastHostSettings(
        BootstrapToastPlacement placement,
        int toastSpacing,
        int maximumVisibleToasts,
        Padding screenMargin,
        bool topMost)
    {
        Placement = placement;
        ToastSpacing = toastSpacing;
        MaximumVisibleToasts = maximumVisibleToasts;
        ScreenMargin = screenMargin;
        TopMost = topMost;
    }

    public BootstrapToastPlacement Placement { get; }
    public int ToastSpacing { get; }
    public int MaximumVisibleToasts { get; }
    public Padding ScreenMargin { get; }
    public bool TopMost { get; }
}

internal interface IBootstrapToastHostWindow : IDisposable
{
    string ScreenDeviceName { get; }
    bool HasOwnedToasts { get; }
    event EventHandler? BecameEmpty;

    void ApplySettings(BootstrapToastScreenInfo screen, BootstrapToastHostSettings settings);
    void ShowToast(BootstrapToast toast);
    void DismissAll();
}
```

The public service depends on this internal contract; production creates `BootstrapToastHostWindow`, while service orchestration tests can inject a deterministic fake.

---

## Top-Level Toast Host Window Design

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHostWindow.cs`.

`BootstrapToastHostWindow : Form, IBootstrapToastHostWindow` owns exactly one `BootstrapToastContainer`.

### Native Form contract

Configure the transient host as:

```text
FormBorderStyle = None
ShowInTaskbar = false
ControlBox = false
MaximizeBox = false
MinimizeBox = false
StartPosition = Manual
TopMost = service setting (default false)
BackColor / TransparencyKey = a private collision-safe host key color
ShowWithoutActivation = true
CreateParams.ExStyle includes WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
```

Do not give the host an application owner Form solely to control z-order; owner relationships can unexpectedly minimize/hide the host with one specific Form and conflict with application-global usage.

### Host geometry

For a resolved screen:

```text
screen.WorkingArea
    -> apply DPI-scaled ScreenMargin
    -> host Bounds = resulting available rectangle
    -> BootstrapToastContainer.Dock = Fill
    -> container Placement/ToastSpacing/MaximumVisibleToasts = service settings
```

The host is geometrically large only so the existing container can reuse its corner stack algorithm. To avoid intercepting pointer input across the blank working area:

1. Keep a native `Region` representing only the current transient Toast stack plus its horizontal slide-animation envelope.
2. Observe owned Toast child `VisibleChanged`, `BoundsChanged`, `SizeChanged`, `ParentChanged`, container `ControlAdded`, and `ControlRemoved` as needed.
3. Coalesce repeated region invalidations with one pending `BeginInvoke` callback instead of rebuilding a native region multiple times inside one message-loop turn.
4. Build the new region from visible Toast rectangles in host-client coordinates. Inflate horizontally by the existing DPI-scaled Toast slide distance so enter/exit animation is not clipped.
5. Include the small spacing gaps inside the stack envelope, but do not include unrelated host-client space.
6. Replace the previous host `Region` atomically and dispose the previous framework-owned region.
7. When there are no owned Toasts, hide the host and clear/dispose its active region.

This keeps the service self-contained without a global mouse hook or a full-screen input-blocking invisible Form.

### Non-activation

Showing a transient notification must use a non-activating show path. Tests must verify that the host's `ShowWithoutActivation` contract and extended styles are present.

Do not call `Activate()`, `Focus()`, `Select()`, or `BringToFront()` in a way that changes the foreground window. If z-order refresh is required, use the non-activating native/window path already implied by `WS_EX_NOACTIVATE` rather than activating the window.

### Height-aware stack bound

A service host has a finite working-area height. Reuse the existing count limit and add one internal constraint to `BootstrapToastContainer`:

```csharp
internal int? MaximumStackHeightPixels { get; set; }
```

Rules:

- Default `null` means current Stage 8 behavior: no extra height limit.
- The top-level host sets it to its current client height.
- A Toast may move from queued to entering only if:
  - the occupied count remains within `MaximumVisibleToasts`; and
  - current occupied stack height + candidate preferred height + required spacing fits `MaximumStackHeightPixels`.
- Recompute a queued Toast's preferred height at its assigned width before evaluating fit.
- If resize/DPI/settings changes make the current active stack exceed the limit, move the newest excess non-exiting Toasts back to `Queued` without firing `Dismissed`, matching the existing `MaximumVisibleToasts` reconciliation model.
- When an exit frees enough vertical space, promote the oldest queued Toast that fits.
- Application-placed containers never set `MaximumStackHeightPixels`; all existing tests and semantics remain unchanged.

Add a pure helper to `BootstrapToastLayoutLogic` if needed:

```csharp
internal static int CalculateRequiredStackHeight(
    IReadOnlyList<Size> toastSizes,
    int logicalSpacing,
    int dpi);
```

Do not expose the height limit publicly in this feature.

### Host disposal

Host disposal:

- unsubscribes every child/container event;
- cancels any pending coalesced region refresh safely;
- disposes the active native region;
- disposes its owned `BootstrapToastContainer`, which already disposes owned Toasts without manufacturing public dismissals;
- never writes history;
- raises no fake `Dismissed` events merely because the application is shutting down.

---

## Notification History Store

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryStore.cs` as an internal non-UI unit.

Suggested internal contract:

```csharp
internal sealed class BootstrapToastHistoryStore
{
    public BootstrapToastHistoryStore(int capacity);

    public int Capacity { get; set; }
    public int UnreadCount { get; }
    public int Count { get; }

    public bool Add(BootstrapToastHistoryItem item);
    public bool Remove(Guid id);
    public bool MarkAsRead(Guid id);
    public bool MarkAllAsRead();
    public bool Clear();
    public IReadOnlyList<BootstrapToastHistoryItem> SnapshotNewestFirst();
}
```

Rules:

- Internally store oldest -> newest for O(1)-style append/oldest trim using an appropriate bounded collection structure; snapshot reverses to newest-first.
- `Add` rejects duplicate ids with `InvalidOperationException`; service-generated Guids should make this defensive path exceptional.
- `Add` trims oldest entries until `Count <= Capacity`.
- `Capacity` rejects values `<= 0`; reducing capacity trims immediately.
- `MarkAsRead` returns `true` only if a matching unread item changed to read.
- `MarkAllAsRead` returns `true` only if at least one item changed.
- `Clear` returns `true` only if at least one item was removed.
- `Remove` supports `Show()` rollback if Toast transfer fails; it is internal and does not become a public delete API.
- `SnapshotNewestFirst()` returns a new collection and immutable item objects; callers cannot mutate store order or read state.
- The store has no `Control`, `Screen`, timer, thread, event, theme, icon renderer, or persistence dependency.

Service owns the public `HistoryChanged` event rather than putting event policy into the store. This allows one event to represent a complete effective mutation batch.

---

## Notification Center Design

The notification center is an internal interactive window created on demand. It is part of `BootstrapToastService`; it is not a second public control hierarchy that applications must host.

### Files

Create:

```text
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationCenterWindow.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationHistoryListBox.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationCenterRenderLogic.cs
```

### Window behavior

`BootstrapNotificationCenterWindow : Form`:

```text
FormBorderStyle = None
ShowInTaskbar = false
StartPosition = Manual
KeyPreview = true
TopMost = service TopMost
logical preferred size = 420 x 560
minimum useful logical width = 280
```

Unlike transient host windows, the center is intentionally activatable because the user must be able to navigate history with the keyboard.

When opening:

1. Resolve screen with the same `relativeTo -> active Form -> primary screen` policy.
2. Inset `Screen.WorkingArea` by the current service `ScreenMargin`.
3. Scale preferred center size for the center's current DPI.
4. Clamp width/height to available working area.
5. Anchor the window to the corner implied by service `Placement`.
6. Refresh from `GetHistory()`.
7. Show/activate the center because opening it is an explicit interaction request.

When already visible, a repeated `ShowNotificationCenter()` repositions/reuses the same window rather than creating another center.

### Center composition

Use native/established WinForms primitives rather than a new mini-framework:

```text
BootstrapNotificationCenterWindow
    +-- header panel
    |     +-- title label: "Notifications"
    |     +-- unread BootstrapBadge (existing control)
    |     +-- close BootstrapButton (existing control)
    |
    +-- BootstrapNotificationHistoryListBox
    |
    +-- footer panel
          +-- BootstrapButton: "Mark all read"
          +-- BootstrapButton: "Clear"
```

Reuse existing `BootstrapButton` and `BootstrapBadge`; do not custom-paint duplicate button/badge behavior.

### History list

`BootstrapNotificationHistoryListBox : ListBox` uses native selection/scrolling/keyboard/accessibility semantics with variable-height owner drawing:

```text
DrawMode = OwnerDrawVariable
IntegralHeight = false
BorderStyle = None
```

Each row renders:

- unread indicator/semantic variant marker;
- title when non-empty;
- body text, wrapped to available width;
- local-time timestamp derived from `CreatedAtUtc` at render time;
- subtle read/unread background/text emphasis from current theme tokens.

Do not retain `Brush`, `Pen`, `Font`, `StringFormat`, `Bitmap`, or other per-row GDI objects beyond their proper scoped/theme-owned lifetime.

`BootstrapNotificationCenterRenderLogic` contains pure row measurement/layout helpers so wrapped text height, marker rectangle, title/body/timestamp rectangles, spacing, and DPI behavior can be tested without showing a Form.

### Read interaction

- Mouse activation of a row marks that notification read.
- Enter or Space on the selected row marks it read.
- Arrow-key selection alone does not mark an item read.
- Double-click may share the same activation path but must not fire history mutation twice.
- Marking an already-read row is a no-op.
- `Mark all read` calls service `MarkAllAsRead()`.
- `Clear` calls service `ClearHistory()` with no implicit transient Toast dismissal.
- Empty history shows a centered themed empty-state message such as `No notifications yet.` and disables Mark all/Clear actions.
- Escape hides the center.
- Close hides rather than disposes the center so the service can reuse it until service disposal.

### History refresh

The service subscribes its center to the service-level history change path while the center exists. A history mutation refreshes the center snapshot and unread badge without replacing the window or stealing focus.

Do not expose a live mutable collection or data-binding object from the service solely for the center.

---

## UI Thread and Error Contract

`BootstrapToastService` records the creating WinForms thread:

```csharp
private readonly int _uiThreadId = Thread.CurrentThread.ManagedThreadId;
```

Construction validates:

```csharp
if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
{
    throw new InvalidOperationException(
        "BootstrapToastService must be created on an STA Windows Forms UI thread.");
}
```

Every stateful public instance member calls an internal guard equivalent to:

```csharp
private void VerifyAccess()
{
    if (_disposed)
    {
        throw new ObjectDisposedException(nameof(BootstrapToastService));
    }

    if (Thread.CurrentThread.ManagedThreadId != _uiThreadId)
    {
        throw new InvalidOperationException(
            "BootstrapToastService can only be used from the UI thread that created it.");
    }
}
```

Property getters that report immutable configuration may still use the same guard for consistency; do not create a partially thread-safe API whose mutation semantics are ambiguous.

Do not catch a wrong-thread call and invoke/BeginInvoke internally. A future explicit asynchronous dispatcher API can be planned separately if required.

---

## Event and State Semantics

### HistoryChanged

`HistoryChanged` sender is always the `BootstrapToastService` instance.

Raise exactly once after each effective operation:

```text
successful Show(... IncludeInHistory=true)
  -> add unread item
  -> HistoryChanged once

MarkAsRead(unread known id)
  -> replace snapshot with read version
  -> HistoryChanged once

MarkAsRead(unknown/already-read id)
  -> false
  -> no event

MarkAllAsRead(any unread)
  -> batch replacement
  -> one HistoryChanged

MarkAllAsRead(no unread)
  -> no event

ClearHistory(non-empty)
  -> clear
  -> one HistoryChanged

ClearHistory(empty)
  -> no event

HistoryCapacity reduction trims one or more
  -> one HistoryChanged
```

If a `HistoryChanged` subscriber throws, propagate the subscriber exception as normal .NET event behavior, but do not roll back the already-committed history mutation.

### Dismissal/history independence

A transient Toast and a history entry share only semantic snapshot data and the generated notification id at creation time; the live Toast is not stored in history.

Therefore:

```text
Toast dismissed/auto-hidden
    != mark history read

DismissAll()
    != ClearHistory()

ClearHistory()
    != dismiss transient Toast

service/host disposal
    != manufacture read state
```

### Notification-center visibility

- `IsNotificationCenterVisible` reflects actual current center visibility.
- `HideNotificationCenter()` is idempotent.
- `ToggleNotificationCenter()` maps hidden -> show and visible -> hide.
- If the center is closed through its close affordance or Escape, service visibility immediately reflects hidden state.

---

## File Map

### New product files

```text
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastOptions.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryItem.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryStore.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastScreenResolver.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceLayoutLogic.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceHostContracts.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHostWindow.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastService.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationCenterRenderLogic.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationHistoryListBox.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationCenterWindow.cs
```

### Existing product files to modify

```text
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastLayoutLogic.cs
```

Only internal height-bound/host-observation support is added to the existing container. Do not add or change its public members for this feature.

### New tests

```text
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastOptionsTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastHistoryStoreTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceLayoutLogicTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastHostWindowTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNotificationCenterRenderLogicTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNotificationCenterTests.cs
```

### Existing tests to extend/regress

```text
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerAnimationTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastLayoutLogicTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastReviewRegressionTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs
```

### Demo/docs to modify

```text
demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs
README.md
docs/ARCHITECTURE.md
docs/COMPONENTS.md
docs/TESTING.md
docs/PACKAGE_README.md
docs/PUBLIC_API_BASELINE.md
CHANGELOG.md
```

Do not add another top-level demo navigation page; the existing Feedback page already owns Toast examples.

---

## Prerequisite Gate

Before Task 1, verify the implemented Stage 8 artifacts exist:

```powershell
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToast.cs
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastLayoutLogic.cs
Test-Path tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTests.cs
Test-Path tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs
Test-Path tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastAutoHideLifecycleTests.cs
Test-Path demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs
```

Expected: every command returns `True`.

Run the existing Toast regression gate before changing code:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToast|FeedbackDemoForm"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToast|FeedbackDemoForm"
```

Expected: both targets pass. If existing Toast regressions are red, fix/understand that baseline before implementing the service so failures are not misattributed.

---

### Task 1: Define and test the public options/history snapshot contracts

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastOptions.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryItem.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastOptionsTests.cs`

**Interfaces:**
- Produces: `BootstrapToastOptions` and immutable `BootstrapToastHistoryItem` exactly as defined in the Public Contract section.
- Consumes: existing `BootstrapVariant`, `IconDescriptor`, and Toast validation conventions.

- [ ] **Step 1: Write failing defaults and normalization tests.**

```csharp
[Test]
public void Options_DefaultsMatchBootstrapToastContract()
{
    var options = new BootstrapToastOptions();

    Assert.Multiple(() =>
    {
        Assert.That(options.Title, Is.EqualTo(string.Empty));
        Assert.That(options.Text, Is.EqualTo(string.Empty));
        Assert.That(options.Variant, Is.EqualTo(BootstrapVariant.Primary));
        Assert.That(options.Icon, Is.Null);
        Assert.That(options.Dismissible, Is.True);
        Assert.That(options.AutoHide, Is.True);
        Assert.That(options.AutoHideDelay, Is.EqualTo(5000));
        Assert.That(options.AnimationDuration, Is.EqualTo(200));
        Assert.That(options.IncludeInHistory, Is.True);
    });
}

[Test]
public void Options_NullTextValuesNormalizeToEmpty()
{
    var options = new BootstrapToastOptions
    {
        Title = null!,
        Text = null!
    };

    Assert.That(options.Title, Is.Empty);
    Assert.That(options.Text, Is.Empty);
}
```

- [ ] **Step 2: Run the focused tests and confirm failure because the types do not exist.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapToastOptionsTests
```

Expected: compile/test failure identifying the missing new types.

- [ ] **Step 3: Add validation tests before implementation.**

Cover:

```text
undefined Variant -> InvalidEnumArgumentException
AutoHideDelay <= 0 -> ArgumentOutOfRangeException
AnimationDuration <= 0 -> ArgumentOutOfRangeException
failed setter leaves previous value unchanged
history item has get-only public properties
history item constructor is not public
CreatedAtUtc/title/text/variant/read values are exact snapshots
```

- [ ] **Step 4: Implement the two public model types with XML documentation and no UI/lifetime behavior.**

Use private backing fields for validated/normalized option properties rather than relying on uncontrolled auto-property values.

- [ ] **Step 5: Run the focused tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapToastOptionsTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter BootstrapToastOptionsTests
```

Expected: PASS on both targets.

- [ ] **Step 6: Commit the contract slice.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastOptions.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryItem.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastOptionsTests.cs
git commit -m "feat: define toast service notification models"
```

---

### Task 2: Add the internal height-aware ToastContainer hosting constraint

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastLayoutLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastLayoutLogicTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerAnimationTests.cs`

**Interfaces:**
- Produces: internal `BootstrapToastContainer.MaximumStackHeightPixels` (`int?`, default `null`) and pure stack-height calculation.
- Consumes: existing queue states, preferred-height calculation, `MaximumVisibleToasts`, spacing/DPI logic, promotion/reconciliation paths.

- [ ] **Step 1: Add a failing pure stack-height test.**

```csharp
[Test]
public void CalculateRequiredStackHeight_AddsToastHeightsAndScaledGaps()
{
    var sizes = new[]
    {
        new Size(320, 80),
        new Size(320, 100),
        new Size(320, 120)
    };

    var height = BootstrapToastLayoutLogic.CalculateRequiredStackHeight(
        sizes,
        logicalSpacing: 8,
        dpi: 96);

    Assert.That(height, Is.EqualTo(80 + 8 + 100 + 8 + 120));
}
```

- [ ] **Step 2: Add failing container tests for a finite host height.**

Characterize:

```text
MaximumStackHeightPixels == null -> existing count-only behavior unchanged
finite height -> first Toast enters if it fits
next Toast queues when count permits but cumulative height does not fit
queued Toast has measured preferred height before fit decision
exit frees height -> oldest queued fitting Toast promotes
height grows -> queued Toast promotes
height shrinks -> newest excess active Toast returns to queued without Dismissed
MaximumVisibleToasts still applies together with height bound
reduced motion and normal animation both keep the same logical queue outcome
```

- [ ] **Step 3: Implement pure height calculation and internal nullable property.**

Keep public metadata/reflection surface unchanged. The setter must reject non-positive non-null values with `ArgumentOutOfRangeException` and trigger internal reconciliation/promotion when changed.

- [ ] **Step 4: Replace count-only promotion checks with one shared eligibility path.**

Use one internal method conceptually equivalent to:

```csharp
private bool CanOccupyNextVisibleSlot(ToastEntry candidate)
{
    if (CountOccupiedSlots() >= _maximumVisibleToasts)
    {
        return false;
    }

    if (!_maximumStackHeightPixels.HasValue)
    {
        return true;
    }

    RecomputeHeight(candidate.Toast);
    var activeSizes = _entries
        .Where(entry => entry.State != BootstrapToastHostState.Queued)
        .Select(entry => entry.Toast.Size)
        .Concat(new[] { candidate.Toast.Size })
        .ToArray();

    return BootstrapToastLayoutLogic.CalculateRequiredStackHeight(
        activeSizes,
        _toastSpacing,
        GetCurrentDpi()) <= _maximumStackHeightPixels.Value;
}
```

Adjust for exiting entries exactly according to existing occupied-slot semantics rather than changing their lifetime mid-exit.

- [ ] **Step 5: Run focused container/layout/animation regressions on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToastContainer|BootstrapToastLayoutLogic"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToastContainer|BootstrapToastLayoutLogic"
```

Expected: all old count-only tests and new height-bound tests pass.

- [ ] **Step 6: Commit the internal hosting primitive.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastLayoutLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastLayoutLogicTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerAnimationTests.cs
git commit -m "refactor: support bounded toast host height"
```

---

### Task 3: Implement the pure bounded history store

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryStore.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastHistoryStoreTests.cs`

**Interfaces:**
- Produces: internal store contract from the Notification History Store section.
- Consumes: immutable `BootstrapToastHistoryItem` from Task 1.

- [ ] **Step 1: Write failing history-order/capacity tests.**

```csharp
[Test]
public void Add_TrimsOldestAndSnapshotIsNewestFirst()
{
    var store = new BootstrapToastHistoryStore(capacity: 2);
    var first = HistoryItem("first", isRead: false);
    var second = HistoryItem("second", isRead: false);
    var third = HistoryItem("third", isRead: false);

    store.Add(first);
    store.Add(second);
    store.Add(third);

    Assert.That(store.SnapshotNewestFirst().Select(x => x.Text),
        Is.EqualTo(new[] { "third", "second" }));
}
```

- [ ] **Step 2: Add failing unread/read-state tests.**

Cover:

```text
new unread entries increment UnreadCount
MarkAsRead known unread -> true, count decremented
MarkAsRead already read -> false
MarkAsRead unknown -> false
MarkAllAsRead -> one logical batch result
Clear empty -> false
Clear non-empty -> true, unread zero
capacity reduction trims oldest and recalculates unread correctly
duplicate Guid rejected
SnapshotNewestFirst returns a new list each call
previous snapshot object remains unchanged after store replaces item read state
Remove supports successful rollback and is false for unknown id
```

- [ ] **Step 3: Run and observe expected failures.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapToastHistoryStoreTests
```

- [ ] **Step 4: Implement the store with no UI/event/static dependencies.**

Prefer a small ordered list/deque-style implementation appropriate for default capacity 100; avoid premature database/index infrastructure.

- [ ] **Step 5: Run both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapToastHistoryStoreTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter BootstrapToastHistoryStoreTests
```

- [ ] **Step 6: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryStore.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastHistoryStoreTests.cs
git commit -m "feat: add in-memory toast history store"
```

---

### Task 4: Implement deterministic screen and service layout helpers

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastScreenResolver.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceLayoutLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceLayoutLogicTests.cs`

**Interfaces:**
- Produces: `BootstrapToastScreenInfo`, `IBootstrapToastScreenResolver`, production resolver, and pure layout helpers.
- Consumes: `Screen`, `DpiScaler`, `BootstrapToastPlacement` only at the appropriate boundary.

- [ ] **Step 1: Write failing pure DPI/margin tests.**

Cover 96/120/144/168/192 DPI with asymmetric margins such as `new Padding(8, 12, 16, 20)` and negative-origin monitor coordinates.

Example:

```csharp
[Test]
public void InsetWorkingArea_PreservesNegativeMonitorOrigin()
{
    var working = new Rectangle(-1920, 0, 1920, 1040);

    var result = BootstrapToastServiceLayoutLogic.InsetWorkingArea(
        working,
        new Padding(16),
        dpi: 96);

    Assert.That(result, Is.EqualTo(new Rectangle(-1904, 16, 1888, 1008)));
}
```

- [ ] **Step 2: Write failing Toast-width and center-corner tests.**

Cover width scaling/clamping and all four `BootstrapToastPlacement` values for notification-center anchoring.

- [ ] **Step 3: Implement pure geometry helpers with compatibility-safe clamp logic.**

Do not use `Math.Clamp` directly on `net48`; use the repository compatibility helper/convention.

- [ ] **Step 4: Implement production screen resolution.**

Keep `Screen` access confined to the resolver/window boundary. Validate disposed/non-created `relativeTo` controls and fall back rather than throwing native handle errors where practical.

- [ ] **Step 5: Run both target tests.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapToastServiceLayoutLogicTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter BootstrapToastServiceLayoutLogicTests
```

- [ ] **Step 6: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastScreenResolver.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceLayoutLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceLayoutLogicTests.cs
git commit -m "feat: add toast service screen layout logic"
```

---

### Task 5: Build the reusable non-activating top-level Toast host

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceHostContracts.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHostWindow.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastHostWindowTests.cs`

**Interfaces:**
- Produces: `IBootstrapToastHostWindow` and production `BootstrapToastHostWindow`.
- Consumes: existing `BootstrapToastContainer`, Task 2 height bound, Task 4 screen/layout data.

- [ ] **Step 1: Write STA construction/native-style tests.**

Tests must assert:

```text
FormBorderStyle.None
ShowInTaskbar == false
StartPosition.Manual
ShowWithoutActivation == true through an internal test seam/derived accessor
WS_EX_TOOLWINDOW present
WS_EX_NOACTIVATE present
TopMost follows settings
one BootstrapToastContainer child exists
container fills host
container public placement/spacing/max settings are forwarded
container internal MaximumStackHeightPixels tracks host client height
```

- [ ] **Step 2: Add failing ownership/empty-lifecycle tests.**

Show two manual-auto-hide-disabled Toasts through the host, dismiss them through existing Toast semantics, complete animations with the existing animation test seam, and assert `BecameEmpty` only after the container owns zero Toasts.

- [ ] **Step 3: Add failing region-scope/resource tests.**

Characterize a visible Toast stack so the host region:

```text
contains current visible Toast bounds
contains the allowed horizontal slide envelope
excludes a far-away point in otherwise blank host working-area space
is cleared when the host becomes empty
replaces/disposes old Region instances during repeated layout updates
does not rebuild repeatedly after host disposal
```

- [ ] **Step 4: Implement the host Form and coalesced region refresh.**

Use one pending UI callback flag, for example:

```csharp
private bool _regionRefreshPending;

private void RequestRegionRefresh()
{
    if (_regionRefreshPending || IsDisposed || Disposing)
    {
        return;
    }

    _regionRefreshPending = true;
    BeginInvoke((MethodInvoker)(() =>
    {
        _regionRefreshPending = false;
        if (!IsDisposed && !Disposing)
        {
            RefreshWindowRegion();
        }
    }));
}
```

Guard handle-destruction races and never invoke after disposal.

- [ ] **Step 5: Verify non-activating show behavior manually in an STA harness/demo test.**

Open another focusable form/control, record active/focused control, show a service-hosted Toast, and confirm the previous active form/control remains active.

- [ ] **Step 6: Run focused tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToastHostWindow|BootstrapToastContainer"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToastHostWindow|BootstrapToastContainer"
```

- [ ] **Step 7: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceHostContracts.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHostWindow.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastHostWindowTests.cs
git commit -m "feat: add top-level toast host window"
```

---

### Task 6: Implement BootstrapToastService orchestration and global Default

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastService.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceTests.cs`
- Modify if needed for internal test doubles only: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTestDoubles.cs`

**Interfaces:**
- Produces: full public `BootstrapToastService` contract except notification-center behavior, which is wired in Task 8.
- Consumes: Tasks 1/3/4/5 contracts, existing `BootstrapToast`, existing icon renderer infrastructure.

- [ ] **Step 1: Write failing STA/wrong-thread/disposal tests.**

Cover:

```text
constructor on STA succeeds
constructor on MTA/non-STA fails before subscribing/creating windows
all stateful methods reject calls from another managed thread
methods after Dispose throw ObjectDisposedException
Dispose is idempotent
manual service is independent of Default
```

Use explicit STA test helpers already established by the repository rather than relying on the test runner's default apartment.

- [ ] **Step 2: Write failing defaults/validation tests.**

Assert every default and setter rule from the Public Contract table, including no mutation on failed setter and `IconRenderer = null` rejection.

- [ ] **Step 3: Write failing screen-host reuse/routing tests with injected internal fakes.**

Use a fake `IBootstrapToastScreenResolver` and fake host factory so tests can deterministically assert:

```text
relative target resolves screen A -> host A created once
second notification on screen A -> same host reused
notification on screen B -> one additional host
settings change -> both existing hosts receive new settings
host BecameEmpty -> service removes/disposes that host
later screen A notification -> fresh host created
DismissAll -> forwarded once to every current host
```

- [ ] **Step 4: Write failing Show snapshot/history atomicity tests.**

Example behavioral shape:

```csharp
[Test]
public void Show_SnapshotsOptionsAndAddsUnreadHistoryOnce()
{
    using var service = CreateServiceWithFakes();
    var options = new BootstrapToastOptions
    {
        Title = "Saved",
        Text = "Order 1001 saved",
        Variant = BootstrapVariant.Success,
        AutoHide = false
    };

    var id = service.Show(options);
    options.Text = "mutated later";

    var history = service.GetHistory();
    Assert.Multiple(() =>
    {
        Assert.That(history, Has.Count.EqualTo(1));
        Assert.That(history[0].Id, Is.EqualTo(id));
        Assert.That(history[0].Text, Is.EqualTo("Order 1001 saved"));
        Assert.That(history[0].IsRead, Is.False);
        Assert.That(service.UnreadCount, Is.EqualTo(1));
    });
}
```

Also cover `IncludeInHistory=false`, Toast-transfer failure rollback, null options, null text normalization, and renderer/width snapshot timing.

- [ ] **Step 5: Write failing public history/event tests.**

Assert exact return values and `HistoryChanged` counts for add/read/mark-all/clear/capacity-trim/no-op operations.

- [ ] **Step 6: Implement service orchestration with one UI-thread guard and one host dictionary keyed by ordinal screen device name.**

Use `StringComparer.OrdinalIgnoreCase` only if actual `Screen.DeviceName` comparison requires it consistently; choose one comparer and test it rather than mixing key rules.

Create live Toast controls centrally in one method so option-to-control mapping cannot diverge across overloads:

```csharp
private BootstrapToast CreateToast(BootstrapToastOptionsSnapshot options, int widthPixels)
{
    return new BootstrapToast
    {
        Width = widthPixels,
        Title = options.Title,
        Text = options.Text,
        Variant = options.Variant,
        Icon = options.Icon,
        IconRenderer = _iconRenderer,
        Dismissible = options.Dismissible,
        AutoHide = options.AutoHide,
        AutoHideDelay = options.AutoHideDelay,
        AnimationDuration = options.AnimationDuration
    };
}
```

The exact internal snapshot type may be a private readonly struct in the service/options file; do not expose it publicly.

- [ ] **Step 7: Implement Default lifecycle without eager window creation.**

Test:

```text
Default identity stable until disposed
Default access does not create a host Form
first Show creates only the required screen host
Dispose current Default clears the static slot
next Default is a fresh service with empty history
ApplicationExit cleanup is detached/reset safely
```

- [ ] **Step 8: Handle display-settings refresh without retaining disposed services.**

On `DisplaySettingsChanged`:

- re-read current screens;
- reapply settings/working area to hosts whose device still exists;
- if a device disappeared, rebind that existing host to the current primary screen for its remaining owned Toasts without transferring live Toast ownership between containers;
- new notifications route using the resolver's current topology;
- once a stale/rebound host becomes empty, dispose/remove it normally.

Do not attempt live transfer of owned Toast controls between containers because Stage 8 ownership is intentionally single-transfer.

- [ ] **Step 9: Run focused service/history/host tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToastService|BootstrapToastHistoryStore|BootstrapToastHostWindow"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToastService|BootstrapToastHistoryStore|BootstrapToastHostWindow"
```

- [ ] **Step 10: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastService.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTestDoubles.cs
git commit -m "feat: add global toast service"
```

---

### Task 7: Implement notification-center row rendering and keyboard-capable history list

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationCenterRenderLogic.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationHistoryListBox.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNotificationCenterRenderLogicTests.cs`

**Interfaces:**
- Produces: pure row measurement/layout helpers and internal owner-drawn native list control.
- Consumes: immutable `BootstrapToastHistoryItem`, current theme, DPI/rendering helpers.

- [ ] **Step 1: Write failing pure row measurement/layout tests.**

Cover:

```text
empty title -> body moves to title position without blank gap
non-empty title -> title/body/timestamp do not overlap
long multiline body increases measured row height
unread indicator reserves width
read/unread uses same geometry
96/120/144/168/192 DPI produces scaled padding/marker/gaps
narrow width clamps text area to non-negative geometry
```

- [ ] **Step 2: Add palette/theme-resolution tests.**

Verify row surfaces/foregrounds derive from existing theme/semantic helpers, including readable Light/Dark contrast. Do not add a separate hard-coded notification-center semantic color table.

- [ ] **Step 3: Implement the pure render logic.**

Keep it free from `ListBox` handles and static mutable state. Passing `Graphics`/Font for text measurement is acceptable only at a dedicated render-measure boundary; geometry/padding calculations remain pure where practical.

- [ ] **Step 4: Implement `BootstrapNotificationHistoryListBox`.**

Requirements:

```text
native ListBox selection/scroll/focus retained
OwnerDrawVariable
MeasureItem delegates to shared render logic
DrawItem delegates to shared render logic
Items contain immutable history snapshots or internal view models only
Enter/Space raises one internal ItemActivated notification
mouse activation raises the same internal path
arrow-key selection does not activate
runtime theme change invalidates/repaints without changing selection
DPI change remeasures rows
Dispose detaches theme events and disposes framework-created font resources
```

- [ ] **Step 5: Run focused tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapNotificationCenterRenderLogicTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter BootstrapNotificationCenterRenderLogicTests
```

- [ ] **Step 6: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationCenterRenderLogic.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationHistoryListBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNotificationCenterRenderLogicTests.cs
git commit -m "feat: add notification history list presentation"
```

---

### Task 8: Build and wire the notification-center window

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationCenterWindow.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastService.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNotificationCenterTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceTests.cs`

**Interfaces:**
- Produces: `ShowNotificationCenter`, `HideNotificationCenter`, `ToggleNotificationCenter`, and `IsNotificationCenterVisible` behavior.
- Consumes: service history APIs, Task 4 center bounds, Task 7 history list, existing Button/Badge controls.

- [ ] **Step 1: Write failing STA center construction/composition tests.**

Assert one history list, one existing `BootstrapBadge` unread indicator, Mark all/Clear/Close existing `BootstrapButton` controls, borderless/taskbar-hidden center Form, and no transient host/container inside the center.

- [ ] **Step 2: Write failing center snapshot/read interaction tests.**

Cover:

```text
open center displays newest-first snapshot
opening alone leaves unread state unchanged
mouse item activation marks exactly one unread item read
Enter/Space activation does the same
arrow-key selection alone does not mark read
Mark all read updates all rows and badge once
Clear empties rows and disables Mark all/Clear
history changed while center open refreshes rows without recreating Form
Escape hides center
close button hides center
reopening reuses same window while service lives
```

- [ ] **Step 3: Write failing placement/DPI tests.**

Inject/resolve deterministic screen working areas and assert each service `Placement` anchors the center to the corresponding corner after logical margin and DPI scaling.

- [ ] **Step 4: Implement center composition using existing controls.**

Keep header/footer layout focused and deterministic; do not introduce a general-purpose notification-center panel public API.

- [ ] **Step 5: Wire center visibility methods into the service.**

The service owns one nullable center instance. Create it lazily on first `ShowNotificationCenter()`. History mutation refresh should be synchronous on the owning UI thread and safe if the center is hidden.

- [ ] **Step 6: Add disposal tests.**

Service disposal while center is visible must:

```text
hide/dispose center
unsubscribe center/service/theme handlers
leave no top-level framework Form alive
not mutate read state
not raise HistoryChanged solely because of disposal
make IsNotificationCenterVisible inaccessible through disposed instance according to VerifyAccess contract
```

- [ ] **Step 7: Run focused tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapNotificationCenter|BootstrapToastService"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapNotificationCenter|BootstrapToastService"
```

- [ ] **Step 8: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationCenterWindow.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastService.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNotificationCenterTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceTests.cs
git commit -m "feat: add toast notification center"
```

---

### Task 9: Add Feedback demo coverage and manual verification controls

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs`

**Interfaces:**
- Consumes: public service/history/center APIs only. Demo must not access internal host Forms or stores.

- [ ] **Step 1: Add failing demo-contract tests.**

Require the Feedback page to expose discoverable demo actions for:

```text
Show global Toast
Show non-auto-hide global Toast
Burst 7 notifications
Show notification-center/history
Mark all read
Clear history
IncludeInHistory=false sample
TopMost toggle
Placement selector for all four corners
Unread-count display
```

Do not test button captions only; where practical invoke handlers/public demo seams and assert service state/history outcomes.

- [ ] **Step 2: Extend the existing Feedback page instead of creating a new navigation page.**

Use a demo-owned `BootstrapToastService` instance or `BootstrapToastService.Default` consistently. If using `Default`, reset/cleanup test state so repeated demo tests are independent.

- [ ] **Step 3: Add a target-control routing example.**

Pass the Feedback form or a child control as `relativeTo` so moving the demo Form to another monitor demonstrates screen routing without a new public `Screen` overload.

- [ ] **Step 4: Add visible history state.**

Show `UnreadCount` in the demo and update it from `HistoryChanged` without polling.

- [ ] **Step 5: Run demo tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FeedbackDemoForm
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FeedbackDemoForm
```

- [ ] **Step 6: Perform the manual matrix.**

At minimum:

```text
Light and Dark
Reduced motion on/off
100%, 125%, 150%, 175%, 200% Windows scaling
TopLeft / TopRight / BottomLeft / BottomRight
TopMost false / true
single Toast / 7-Toast burst / long wrapped Toast
AutoHide true / false
close-button dismissal
open center while Toasts are live
mark one read / mark all / clear
history capacity reduced below current count
history-disabled Toast
move Feedback form between monitors, then show relativeTo it
unplug/reconfigure secondary monitor where hardware permits
confirm Toast appearance never steals keyboard focus
confirm center explicitly opened can receive keyboard focus
rapid show/dismiss/center-open-close stress
```

- [ ] **Step 7: Commit demo coverage.**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs
git commit -m "demo: showcase global toast notifications"
```

---

### Task 10: Update documentation and deliberately review the frozen public API

**Files:**
- Modify: `README.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

**Interfaces:**
- Consumes: final public surface from Tasks 1/6/8.
- Produces: reviewed package/user documentation and frozen-v1 API baseline.

- [ ] **Step 1: Document architecture boundaries before changing the API fingerprint.**

`docs/ARCHITECTURE.md` must explain:

```text
application-placed Toast path remains supported
BootstrapToastService is a higher-level composition layer
per-screen top-level hosts are framework-owned and non-activating
one existing BootstrapToastContainer per host
history is in-memory semantic snapshots, not live controls
notification center is service-owned/interactive
service is UI-thread-affine
no OS notification/persistence integration
```

- [ ] **Step 2: Document the exact public contracts and examples.**

Add examples such as:

```csharp
BootstrapToastService.Default.Show(
    new BootstrapToastOptions
    {
        Title = "Saved",
        Text = "The order was saved successfully.",
        Variant = BootstrapVariant.Success
    },
    this);

BootstrapToastService.Default.ShowNotificationCenter(this);
```

Also document ownership, history capacity/read behavior, `IncludeInHistory`, TopMost default, multi-monitor routing, UI-thread requirement, and disposal/default lifetime.

- [ ] **Step 3: Run the public API baseline test and require the expected failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter Phase16PublicApiBaselineTests
```

Expected: FAIL because the newly added public types/members are not yet in the approved fingerprint.

If it passes before the baseline is updated, investigate whether the API test is failing to capture the new exported surface.

- [ ] **Step 4: Review the reconstructed public surface before approval.**

The expected new exported concepts are only:

```text
BootstrapToastOptions
BootstrapToastHistoryItem
BootstrapToastService
```

with members exactly listed in this plan. There must be no public host Form, history store, screen resolver, notification-center Form/ListBox/render logic, host interface/settings struct, height-bound property, internal snapshot type, test seam, Win32 helper, or service factory.

- [ ] **Step 5: Update `Phase16PublicApiBaselineTests` and `docs/PUBLIC_API_BASELINE.md` only after the surface is accepted.**

Re-run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter Phase16PublicApiBaselineTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter Phase16PublicApiBaselineTests
```

Expected: PASS on both targets.

- [ ] **Step 6: Commit docs/API baseline together.**

```powershell
git add README.md docs/ARCHITECTURE.md docs/COMPONENTS.md docs/TESTING.md docs/PACKAGE_README.md docs/PUBLIC_API_BASELINE.md CHANGELOG.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs
git commit -m "docs: document global toast service"
```

---

### Task 11: Full verification, lifecycle stress, and final regression gate

**Files:**
- Modify only files required to fix findings discovered by this verification task.

**Interfaces:**
- Verifies the complete plan and existing Toast compatibility surface.

- [ ] **Step 1: Build the library for both target frameworks.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
```

Expected: zero warnings/errors under the repository's warnings-as-errors policy.

- [ ] **Step 2: Run all focused Toast/service/center tests.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToast|BootstrapNotificationCenter|FeedbackDemoForm|Phase16PublicApiBaselineTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToast|BootstrapNotificationCenter|FeedbackDemoForm|Phase16PublicApiBaselineTests"
```

Expected: PASS.

- [ ] **Step 3: Run the complete automated suite for both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
```

Expected: PASS.

- [ ] **Step 4: Stress service lifetime and resource cleanup.**

Exercise at least these loops in a diagnostic/manual test build:

```text
create/dispose 100 manual BootstrapToastService instances without showing
create/show/dismiss/dispose hosts repeatedly
show 500 short-lived Toasts through one service with bounded history capacity
open/hide center 200 times without recreating it each time
switch Light/Dark repeatedly while center and Toasts are active
change placement and MaximumVisibleToasts during active queueing
reduce/increase history capacity repeatedly
```

Inspect that:

```text
host Form count returns to baseline after service disposal
timers/animations stop after owned Toast disposal
no SystemEvents/ApplicationExit handler retains disposed service
GDI object count remains stable after warm-up/GC boundaries
old Region objects are disposed
history contains no Control/IconRenderer/live Toast references
no duplicate HistoryChanged or Dismissed events appear
```

- [ ] **Step 5: Re-run the original Stage 8 Toast tests as a compatibility gate.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToastTests|BootstrapToastAutoHideLifecycleTests|BootstrapToastContainerTests|BootstrapToastContainerAnimationTests|BootstrapToastLayoutLogicTests|BootstrapToastReviewRegressionTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToastTests|BootstrapToastAutoHideLifecycleTests|BootstrapToastContainerTests|BootstrapToastContainerAnimationTests|BootstrapToastLayoutLogicTests|BootstrapToastReviewRegressionTests"
```

Expected: all Stage 8 behavior remains green.

- [ ] **Step 6: Confirm no accidental forbidden infrastructure was added.**

Review the diff for:

```text
new external package references
WinRT/Action Center integration
new background thread/message pump
Task.Delay/Thread.Sleep animation logic
global input hooks
history persistence
duplicate Toast queue/animation state machine
public internal-host types
unreviewed public aliases/convenience overloads
caller-owned renderer disposal
```

Expected: none present.

- [ ] **Step 7: Commit any verification-only fixes, if needed, with a focused message.**

Example only when a concrete finding was fixed:

```powershell
git add <files-fixed-by-verification>
git commit -m "fix: harden global toast service lifecycle"
```

If verification required no code changes, do not create an empty commit.

---

## Acceptance Criteria

The implementation is complete only when all of the following are true:

- `BootstrapToastService.Default.Show(...)` can display a transient Toast without any application-placed `BootstrapToastContainer`.
- A manually constructed `BootstrapToastService` provides the same behavior with caller-controlled lifetime.
- Service access is explicitly bound to one STA UI thread and wrong-thread usage fails deterministically.
- Transient service Toasts are hosted in framework-owned borderless/non-activating top-level Forms and do not steal keyboard focus when appearing.
- The service reuses one host per screen and routes using `relativeTo`, then active Form, then primary-screen fallback.
- Host windows use screen working area plus logical margin/DPI scaling and do not cover the taskbar area.
- Blank top-level host space does not block normal pointer interaction because the native host region is restricted to the active Toast stack/animation envelope.
- Existing `BootstrapToastContainer` remains the single queue/animation/ownership engine.
- Existing application-placed Toast behavior and public API remain unchanged.
- Count and internal height bounds prevent top-level service stacks from overflowing the usable working-area height; overflow remains FIFO queued.
- `TopMost` defaults to false and opt-in changes existing/future hosts without activation.
- `ToastWidth`, Toast options, and service renderer are snapshotted for each newly created live Toast.
- History is bounded, in-memory, newest-first on retrieval, and contains immutable semantic snapshots only.
- New retained history items start unread and update `UnreadCount` correctly.
- Dismissal, clear-history, read state, and transient lifetime remain intentionally independent.
- `HistoryChanged` event counts follow the effective-mutation rules and no-op operations do not raise it.
- Notification center is one reusable interactive service-owned window with keyboard navigation, explicit item read activation, Mark all read, Clear, Close/Escape behavior, empty state, Light/Dark, and DPI support.
- Opening the center does not silently mark everything read.
- No live Toast/control/icon-renderer resource is retained by history.
- `Default` is lazy, creates no top-level window before actual use, cleans up on application exit, and can be recreated after explicit disposal.
- Display/topology changes do not transfer already-owned Toasts between containers or leak stale hosts.
- Both `net48` and `net8.0-windows` builds pass.
- Focused and full tests pass on both target frameworks.
- Feedback demo demonstrates the new service/history center and has a manual multi-monitor/DPI/focus verification path.
- Documentation clearly distinguishes application-placed Toast containers from the new application-global service.
- Public API baseline contains only the intended three new public concepts and has been deliberately reviewed.
- No new external dependency, OS-notification integration, persistence layer, background UI thread, duplicate animation scheduler, or global input hook is introduced.

---

## Recommended Implementation Order

```text
1. Public options/history snapshot models
2. Internal ToastContainer height bound
3. Pure history store
4. Screen/layout helpers
5. Non-activating top-level host Form
6. BootstrapToastService + Default + history/event orchestration
7. Notification-center row/list presentation
8. Notification-center window + service wiring
9. Feedback demo/manual verification
10. Docs + frozen public API review
11. Full regression/lifecycle stress
```

This order keeps each layer testable before introducing the next one and preserves the existing Stage 8 Toast primitives as the single source of transient Toast behavior.