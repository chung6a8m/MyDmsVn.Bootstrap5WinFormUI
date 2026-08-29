# Global Toast Service and Notification Center Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the existing `BootstrapToast` implementation with an application-level `BootstrapToastService` that can show notifications without an application-placed container by creating its own non-activating top-level host windows, while also adding bounded in-memory notification history and an interactive notification-center window.

**Architecture:** Preserve `BootstrapToast` and `BootstrapToastContainer` as the rendering, auto-hide, animation, queueing, and ownership primitives. `BootstrapToastService` remains UI-thread-affine for its public API, creates one internal transient host `Form` per live target screen, and routes service-created Toasts using explicit per-screen DPI metadata. Framework callbacks such as display-topology notifications are marshaled back to the creating UI thread through one service-owned dispatcher control; the service refreshes its internal notification center before publishing the public `HistoryChanged` event. Existing application-placed Toast containers continue to behave exactly as before.

**Tech Stack:** C#, native Windows Forms `Form` / `ListBox` / `Control` / `Screen`, existing `BootstrapToast`, `BootstrapToastContainer`, Theme / Rendering / Icons / Animation / Compatibility infrastructure, `BootstrapVariant`, `IconDescriptor`, `IIconRenderer`, `DpiScaler`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** User request dated 2026-08-29. Compatibility baseline: `docs/plans/20260828-008-bootstrap-toast.md` and the implemented Stage 8 Toast sources/tests. Repository-wide constraints: `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`.

## Review Corrections Integrated

This revision incorporates the design-review findings identified after the first version of the plan:

1. `SystemEvents.DisplaySettingsChanged` and other framework-originated callbacks are never allowed to touch WinForms state directly from their source thread; they marshal through an internal UI dispatcher owned by the service. Public wrong-thread calls still fail instead of marshaling.
2. `BootstrapToastScreenInfo` carries explicit target-monitor DPI so Toast width, margins, host geometry, and notification-center sizing use the same deterministic per-screen scale.
3. A monitor that disappears is not rebound onto another screen while retaining a live stack. Its host is retired and its transient Toasts are dismissed while history remains intact, preventing two independent stacks from overlapping on the fallback monitor.
4. Internal notification-center refresh is not implemented as a subscriber to the public `HistoryChanged` event. The service commits state, refreshes framework-owned UI, then raises the public event last.
5. Height-aware queueing preserves the existing strict FIFO contract. The service-host height bound clamps an individual Toast height to the host height, ensuring the head item can eventually enter when the stack is empty instead of starving forever; later queued items never bypass the head item just because they are smaller.
6. The transient host does not use `TransparencyKey`. A restricted WinForms/native `Region` is the sole mechanism used to keep blank host space non-interactive and visually absent.
7. The reusable notification-center Form intercepts user close/Alt+F4 and hides while the service is alive; only service disposal permits real Form close/disposal.

---

## Global Constraints

- Keep the root namespace `MyDmsVn.Bootstrap5WinFormUI`; new public Toast-service types remain under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must continue to compile from one shared implementation for both `net48` and `net8.0-windows` wherever practical.
- This plan supersedes only the Stage 8 exclusions for a global/application-level Toast service, top-level Toast windows, and notification history. It does **not** supersede the existing `BootstrapToast` or `BootstrapToastContainer` public contracts, ownership rules, dismissal timing, animation behavior, or application-placed hosting model.
- Existing applications that instantiate `BootstrapToast` and `BootstrapToastContainer` directly must not be required to use the new service.
- `BootstrapToastService` is an application-process service, not a Windows notification-platform bridge. Do not use WinRT Toast notifications, Windows Action Center APIs, tray-balloon APIs, COM activation, AppUserModelID registration, registry integration, or operating-system notification persistence.
- History is in-memory for the lifetime of one service instance. Do not add JSON/XML/database/registry persistence, cross-process synchronization, roaming, cloud synchronization, or migration/versioning in this change.
- Do not add a service locator, dependency-injection package, background worker, hidden worker thread, second UI thread, custom message pump, `Application.DoEvents`, or polling loop.
- Service construction and all public service operations are UI-thread-affine. The creating thread must be STA. Calls from another thread throw `InvalidOperationException` instead of silently marshaling.
- The public wrong-thread rule does **not** prohibit internal marshaling of framework callbacks such as `SystemEvents.DisplaySettingsChanged` or `Application.ApplicationExit`. Such callbacks must be posted to the service's creating UI thread before touching Forms, Controls, host dictionaries, history UI, or service lifecycle state.
- The service owns one private WinForms dispatcher `Control` created on the service UI thread. It exists only to marshal framework callbacks; it is not top-level, visible, public, or a second UI thread. The service disposes it after event unsubscription.
- Do not use `Task.Delay`, `Thread.Sleep`, thread-pool timers, or a second animation scheduler. Transient Toast animation and auto-hide remain owned by the existing Toast/ToastContainer implementation.
- The service must reuse `BootstrapToastContainer` for stacking, strict FIFO queueing, visible-slot limits, enter/exit animation, reflow, auto-hide handoff, and deterministic Toast disposal. Do not implement a parallel service-specific Toast state machine.
- The service owns every `BootstrapToast` instance it creates. After creation, ownership transfers immediately to a service-owned `BootstrapToastContainer`; application code never receives the live Toast control.
- The service may create top-level Forms, but transient host windows must be borderless, omitted from the taskbar, non-activating, and absent from Alt+Tab. The notification-center window is intentionally interactive and may activate when explicitly opened.
- Global transient hosts must not steal keyboard focus from the current application or another application merely because a Toast appears.
- `TopMost` defaults to `false`. Applications may opt in explicitly.
- Resolve the target screen from a live `relativeTo` control when supplied; otherwise prefer `Form.ActiveForm`; otherwise use the primary screen. Do not invent cursor-following placement.
- Maintain at most one canonical transient host per **currently live** screen device name. Hosts for removed monitors are retired rather than rebound over a canonical host on another monitor.
- Multi-monitor coordinates use `Screen.WorkingArea` in screen pixels. `BootstrapToastScreenInfo` carries the target screen DPI. Public spacing, margin, Toast width, and notification-center preferred size are logical 96-DPI values and are scaled using that DPI.
- Do not make this feature depend on `docs/plans/20260829-001-interactive-tooltip-popover-placement-engine.md`. Toasts are screen-corner anchored, not anchor-relative floating surfaces.
- Host windows must never reserve the Windows taskbar area; use `Screen.WorkingArea` rather than monitor bounds.
- A top-level transient host may geometrically fill the target usable working area to reuse the existing container corner-layout algorithm, but its `Region` must be reduced to the active Toast stack plus animation envelope so blank host space does not consume pointer input. Old `Region` instances must be disposed deterministically.
- Do not use `TransparencyKey` for the transient host. Theme/content colors must never become accidentally transparent because they match a chroma-key color.
- The existing `BootstrapToastContainer.MaximumVisibleToasts` count remains authoritative. Add only internal height-aware support required by service-owned hosts. Application-placed containers retain existing unlimited-by-height behavior unless the internal host limit is set.
- Height-aware service hosting preserves strict FIFO: only the oldest queued Toast is eligible for promotion. Later queued Toasts never bypass it because they happen to fit.
- Under a finite host height, an individual Toast's rendered control height is clamped to `MaximumStackHeightPixels`. This guarantees that the queue head can enter when no other Toast occupies the stack. The notification history keeps the complete text even if the transient surface clips/ellipsizes content.
- Global-host Toasts remain mouse-dismissible through the existing close affordance. Keyboard review/action belongs to the notification center.
- Notification history records semantic data only. Do not retain a live `BootstrapToast`, `Control`, `Image`, `Icon`, renderer, or other caller/framework UI object after a notification is added to history.
- Notification history snapshots include notification id, UTC creation time, title, text, semantic variant, and read state. The center uses semantic presentation rather than retaining arbitrary icon resources.
- New history entries are unread. Toast auto-hide, close-button dismissal, `DismissAll()`, application deactivation, host retirement, monitor removal, or host disposal do not mark history read.
- Merely opening the notification center does not mark all entries read. Explicit item activation marks that item read; `MarkAllAsRead()` is separate.
- `ClearHistory()` changes history only. It never dismisses live Toasts. `DismissAll()` changes transient Toasts only. It never clears history.
- History capacity defaults to `100`, must be greater than zero, and trims oldest entries immediately when reduced or exceeded.
- `GetHistory()` returns a newest-first snapshot. Returned `BootstrapToastHistoryItem` objects are immutable; later read-state changes do not mutate previously returned snapshots.
- A history mutation is published in this order: commit store state -> refresh framework-owned notification-center UI synchronously -> raise public `HistoryChanged` last. The center must never subscribe to the public event to keep itself synchronized.
- `HistoryChanged` fires once for each effective history mutation batch: successful Show with history, successful single-item read, successful mark-all, non-empty clear, or capacity trim. No-op operations do not raise it.
- If a public `HistoryChanged` subscriber throws, propagate the exception as normal .NET event behavior, but do not roll back committed history, a successfully transferred Toast, or the already-refreshed notification center.
- Runtime Light/Dark theme changes repaint transient Toasts through their existing contract and repaint the notification center without changing history order/read state or restarting transient Toast timers.
- DPI/display changes reposition active top-level windows and recompute logical margins/sizes without replacing history or moving caller-owned application windows.
- Subscribe to `SystemEvents.DisplaySettingsChanged` only while the service needs live top-level display tracking, and always unsubscribe during service disposal. No static event handler may keep a disposed service alive.
- When a monitor disappears, hide/retire its transient host and semantically dismiss its live/queued transient Toasts; do not transfer those Toast controls into another container and do not rebind that live stack on top of another screen's canonical host. History remains unchanged.
- Caller-assigned `IIconRenderer` instances are never disposed by the service. A renderer setting is snapshotted onto newly created Toast controls; changing it does not rewrite existing live Toasts.
- Designer construction of existing Toast controls must remain unchanged and safe. The new service is runtime-only and must not create a top-level window merely because the assembly is loaded or `Default` has not been accessed.
- All new public/protected members receive XML documentation. `TreatWarningsAsErrors` and `CS1591` remain green.
- Every new public/protected type/member changes the frozen public API. `Phase16PublicApiBaselineTests` must intentionally fail before the approved fingerprint and `docs/PUBLIC_API_BASELINE.md` are updated.
- Final completion requires both target builds, focused and full tests, Feedback demo/manual verification, multi-monitor/DPI checks where hardware is available, focus-stealing checks, topology-retirement checks, history-capacity/read-state checks, resource-lifetime checks, documentation updates, and deliberate public API review.

---

## Compatibility Baseline and Superseded Exclusions

The existing Stage 8 plan deliberately excluded:

```text
- global/static Toast manager
- top-level Toast Form/window
- screen/monitor-aware overlay hosting
- notification history
- global notification service
```

This plan supersedes only those exclusions. Existing behavior remains the compatibility baseline:

```text
BootstrapToast
  -> owns visual content, dismissibility, auto-hide delay, theme/DPI rendering

BootstrapToastContainer
  -> owns transferred Toast controls
  -> strict FIFO queue
  -> MaximumVisibleToasts
  -> corner stack placement
  -> enter/exit/reflow animation
  -> semantic DismissAll
  -> disposes owned Toast after exit
```

The dependency direction remains:

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

There is no dependency from `BootstrapToast`, `BootstrapToastContainer`, Theme, Rendering, Animation, or Icons back to the service.

---

## Scope

### In scope

1. Public `BootstrapToastOptions` for service-created transient Toast content/behavior.
2. Public immutable `BootstrapToastHistoryItem` snapshots.
3. Public `BootstrapToastService`, including a lazy application-wide `Default`.
4. Public UI-thread-affine service semantics with explicit wrong-thread failures.
5. Internal UI-thread dispatch for framework-originated callbacks only.
6. One reusable internal non-activating top-level Toast host per live target screen.
7. Existing `BootstrapToastContainer` inside each host, preserving strict FIFO, auto-hide, animation, and disposal semantics.
8. Explicit per-screen DPI metadata and screen working-area placement.
9. Logical screen margin, Toast width, spacing, maximum-visible count, and optional `TopMost` behavior.
10. Internal height-aware host constraint with oversized-single-Toast clamping and no FIFO bypass.
11. Deterministic retirement of hosts whose monitor disappears.
12. Bounded in-memory history, read/unread state, unread count, capacity trimming, clear, and change notification.
13. One internal interactive notification-center window with newest-first history, read/unread presentation, item activation, Mark all read, Clear, Close/Escape/Alt+F4 hide semantics, keyboard navigation, theme, and DPI support.
14. Feedback demo scenarios for global service, multi-screen anchoring, queueing, notification center, history-disabled notifications, unread state, clearing, and topology changes.
15. Documentation and public API baseline updates.

### Explicitly deferred

- Persistence across application restarts.
- Windows Action Center / WinRT native Toast notifications.
- Tray icon integration.
- Cross-process notification aggregation.
- A public background-thread-safe `ShowAsync`/dispatcher API.
- A dedicated Toast UI thread or custom application context.
- Explicit public `Screen` overloads.
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
- `AutoHideDelay` must be greater than zero even when `AutoHide == false`.
- `AnimationDuration` must be greater than zero.
- `Icon` is used only by the live transient Toast and is not copied into history.
- `IncludeInHistory = false` suppresses only history/unread creation.
- The options object remains caller-owned. `Show()` snapshots it before any framework state mutation.

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

- No public constructor.
- `CreatedAtUtc.Offset == TimeSpan.Zero`.
- Strings are non-null.
- Public properties are immutable.
- Marking read replaces the store entry with a new immutable snapshot.

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
| `UnreadCount` | retained history snapshots where `IsRead == false` |
| `IsNotificationCenterVisible` | actual current visibility of the service-owned center |

Configuration timing:

- `Placement`, `ToastSpacing`, `MaximumVisibleToasts`, `ScreenMargin`, and `TopMost` apply immediately to existing live hosts and future hosts.
- `ToastWidth` applies when each new Toast is created and is scaled from logical pixels using the resolved `BootstrapToastScreenInfo.Dpi`.
- `IconRenderer` applies when each new Toast is created.
- `HistoryCapacity` applies immediately, refreshes the center if trimming occurs, then raises `HistoryChanged` once.

---

## Show Semantics and Failure Atomicity

`Show(string text, Control? relativeTo = null)` is the convenience form:

```csharp
return Show(
    new BootstrapToastOptions
    {
        Text = text ?? string.Empty
    },
    relativeTo);
```

`Show(BootstrapToastOptions options, Control? relativeTo = null)` executes on the service UI thread in this order:

```text
1. Verify service access/disposal and validate/snapshot options.
2. Resolve BootstrapToastScreenInfo, including DeviceName, WorkingArea, and Dpi.
3. Get/create the canonical host for that live screen.
4. Resolve Toast width using the screen's explicit Dpi and available pixel width.
5. Create the BootstrapToast and copy the snapshotted options/renderer/width.
6. Generate the notification Guid and, when IncludeInHistory=true, add one tentative unread history snapshot.
7. Transfer the live Toast to the host's existing BootstrapToastContainer.
8. If transfer fails, remove the tentative history item and dispose any untransferred framework-created Toast; do not publish HistoryChanged.
9. After successful transfer, if history changed, synchronously refresh the framework-owned notification center from the committed store snapshot.
10. Raise public HistoryChanged last when history changed.
11. Return the Guid.
```

Failure rules:

- Validation failures happen before host/history mutation.
- Host creation failure happens before tentative history mutation.
- Toast transfer failure rolls history back before any public event.
- A throwing `HistoryChanged` subscriber does **not** roll back the already-transferred Toast or committed history. Therefore a caller that catches an exception thrown from its own event subscriber must not assume the notification was not shown.
- `IncludeInHistory=false` still generates and returns a new Guid but performs no history refresh/event.

---

## Default Service Lifetime

`BootstrapToastService.Default` is lazy:

```text
assembly load
  -> no service
  -> no Form
  -> no SystemEvents subscription

first Default access on STA UI thread
  -> create service and its private dispatcher Control
  -> no top-level host/center Form yet

first top-level display use
  -> subscribe display-topology event if required

Application.ApplicationExit
  -> callback is marshaled to the service UI thread when necessary
  -> dispose default service
  -> detach static/system events
  -> dispose host/center/dispatcher Controls
  -> clear static default reference
```

Rules:

- First `Default` access from a non-STA thread throws and does not initialize the singleton.
- Explicit disposal of the current default clears the static slot; later valid access creates a fresh service with empty history.
- A manually constructed service is caller-owned and independent of `Default`.
- No top-level Form is created by assembly load, unrelated control construction, or merely obtaining `Default`.

---

## Internal Service Boundaries

### Screen and DPI resolution

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastScreenResolver.cs`:

```csharp
internal readonly struct BootstrapToastScreenInfo
{
    public BootstrapToastScreenInfo(
        string deviceName,
        Rectangle workingArea,
        int dpi)
    {
        DeviceName = deviceName;
        WorkingArea = workingArea;
        Dpi = dpi;
    }

    public string DeviceName { get; }
    public Rectangle WorkingArea { get; }
    public int Dpi { get; }
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
    -> use the control/window DPI when available for that screen
else live Form.ActiveForm
    -> Screen.FromControl(Form.ActiveForm)
    -> use the active Form/window DPI when available
else
    -> Screen.PrimaryScreen
    -> resolve monitor DPI through the internal monitor-DPI compatibility provider
```

For `GetCurrentScreens()`, obtain each screen's DPI through one internal monitor-DPI provider compatible with both target frameworks. Prefer the repository's existing DPI/window compatibility convention; where a monitor-specific native API is unavailable on an older Windows version, use a documented 96-DPI fallback. Tests inject exact DPI values and do not depend on workstation scaling.

Validation:

- `deviceName` is non-empty.
- `workingArea` may have negative coordinates but must have positive width/height.
- `dpi > 0`; production fallback is `96`, never `0`.
- If `Screen.PrimaryScreen` is unexpectedly unavailable, throw `InvalidOperationException` before host mutation.

### Layout logic

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceLayoutLogic.cs`:

```csharp
internal static Rectangle InsetWorkingArea(
    Rectangle workingArea,
    Padding logicalMargin,
    int dpi);

internal static int ResolveToastWidth(
    int logicalToastWidth,
    int availablePixelWidth,
    int dpi);

internal static Size ResolveNotificationCenterSize(
    Size logicalPreferredSize,
    Size availablePixelSize,
    int dpi);

internal static Rectangle CalculateNotificationCenterBounds(
    Rectangle availableWorkingArea,
    Size desiredPixelSize,
    BootstrapToastPlacement placement);
```

Rules:

- Scale logical values with the supplied screen DPI; never read static/global DPI inside the pure helpers.
- Preserve negative monitor origins.
- Clamp available dimensions to at least one pixel after validated margins.
- Clamp Toast width to available pixel width.
- Notification center uses the same corner selected by `Placement`.
- No `Screen`, `Control`, `Form`, theme, handle, or mutable static state in these helpers.

### Host contracts

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
    void RetireForScreenRemoval();
}
```

`RetireForScreenRemoval()` is internal-only. Production behavior is: hide the top-level host immediately, dismiss all owned/queued Toasts through existing container semantics, never transfer Toast controls to another container, and allow `BecameEmpty` to drive final disposal.

---

## Top-Level Toast Host Window Design

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHostWindow.cs`.

`BootstrapToastHostWindow : Form, IBootstrapToastHostWindow` owns exactly one `BootstrapToastContainer`.

### Native Form contract

Configure:

```text
FormBorderStyle = None
ShowInTaskbar = false
ControlBox = false
MaximizeBox = false
MinimizeBox = false
StartPosition = Manual
TopMost = service setting (default false)
ShowWithoutActivation = true
CreateParams.ExStyle includes WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE
TransparencyKey is not used
```

Do not assign an application owner Form solely for z-order.

### Host geometry and Region

For a resolved screen:

```text
BootstrapToastScreenInfo.WorkingArea
    -> apply DPI-scaled ScreenMargin using BootstrapToastScreenInfo.Dpi
    -> host Bounds = resulting available rectangle
    -> BootstrapToastContainer.Dock = Fill
    -> container Placement/ToastSpacing/MaximumVisibleToasts = service settings
    -> container.MaximumStackHeightPixels = host client height
```

Pointer/visibility rules:

1. Maintain one `Region` representing visible Toast rectangles plus horizontal slide envelopes and stack spacing gaps.
2. Observe child/container geometry/visibility additions/removals required to refresh the region.
3. Coalesce refresh requests with one pending `BeginInvoke` callback.
4. Build rectangles in host-client coordinates.
5. Inflate horizontally by the existing DPI-scaled Toast slide distance.
6. Do not include blank host-client space.
7. Replace and dispose the previous Region deterministically.
8. When no Toasts remain, hide the host and clear/dispose the Region.
9. Never add a chroma-key/`TransparencyKey` workaround.

### Non-activation

Showing transient notifications must not call `Activate()`, `Focus()`, `Select()`, or an activating `BringToFront()`. If z-order refresh is needed, use a no-activate native/window path consistent with `WS_EX_NOACTIVATE`.

### Height-aware strict FIFO bound

Add internal-only support to `BootstrapToastContainer`:

```csharp
internal int? MaximumStackHeightPixels { get; set; }
```

Rules:

- `null` preserves existing Stage 8 count-only behavior exactly.
- A non-null value must be `> 0`.
- Service hosts set it to current client height.
- While finite, the container computes each owned Toast height as:

```csharp
var preferred = toast.CalculatePreferredHeightForCurrentWidth();
var resolved = _maximumStackHeightPixels.HasValue
    ? Math.Min(preferred, _maximumStackHeightPixels.Value)
    : preferred;
```

Use the repository's compatibility-safe min/clamp conventions as needed. This clamp applies only when the internal host limit is set.

- Full text remains in the live Toast model/history snapshot even when transient rendering is vertically constrained. The transient surface may clip/ellipsis according to its existing paint/layout behavior; do not mutate `Text` to fit.
- Promotion always examines exactly the first queued entry. If that head item cannot fit beside currently occupied entries, promotion stops. Do **not** scan forward for a smaller item.
- Because one Toast's height is clamped to the total host height, the FIFO head can enter once the stack becomes empty; permanent head-of-line starvation from an oversized single Toast is therefore impossible.
- Exiting entries continue to count as occupied exactly according to existing Stage 8 semantics.
- If host height shrinks, move newest excess non-exiting active Toasts back to `Queued` without `Dismissed` until the remaining stack fits. Preserve relative queue order so those returned entries do not jump ahead of older queued items.
- If host height grows or an exit completes, retry promotion from the queue head.
- Application-placed containers never set this property and remain unaffected.

Add pure helper:

```csharp
internal static int CalculateRequiredStackHeight(
    IReadOnlyList<Size> toastSizes,
    int logicalSpacing,
    int dpi);
```

### Host retirement and disposal

`RetireForScreenRemoval()`:

```text
mark host retiring
hide host immediately
clear active Region
call existing container.DismissAll()
reject new ShowToast calls
wait for existing exit/disposal semantics
raise BecameEmpty when container owns zero Toasts
```

Disposal:

- unsubscribes child/container events;
- cancels pending region refresh safely;
- disposes active Region;
- disposes the owned container/Toasts;
- never changes history/read state;
- raises no synthetic history events.

---

## Notification History Store

Create `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryStore.cs`:

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

- Store oldest -> newest; snapshot reverses to newest-first.
- `Add` rejects duplicate ids.
- Capacity `<= 0` is rejected.
- Add/capacity reduction trims oldest immediately.
- Read operations replace immutable item snapshots rather than mutate them.
- `Remove` is internal rollback support only.
- No UI, event, Screen, timer, thread, theme, icon renderer, or persistence dependency.

---

## Notification Center Design

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

Opening:

1. Resolve `BootstrapToastScreenInfo` using the same screen policy as Toasts.
2. Inset its WorkingArea using its explicit Dpi and current `ScreenMargin`.
3. Resolve 420x560 logical preferred size using that same Dpi.
4. Clamp to available working area.
5. Anchor to the service `Placement` corner.
6. Refresh from current history snapshot.
7. Show/activate because opening the center is explicit user/application interaction.

Repeated show reuses/repositions the same center.

### Close/reuse lifecycle

The center is reusable while the service lives. Implement an internal disposal flag, for example:

```csharp
private bool _allowCloseForServiceDisposal;
```

`OnFormClosing` contract:

```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
    if (!_allowCloseForServiceDisposal)
    {
        e.Cancel = true;
        Hide();
        return;
    }

    base.OnFormClosing(e);
}
```

Equivalent implementation is acceptable, but behavior is fixed:

- close button hides;
- Escape hides;
- Alt+F4 / `Close()` while service lives hides and does not dispose;
- service disposal sets the allow-close flag, then closes/disposes exactly once;
- reopening after user close reuses the same Form instance.

### Composition

```text
BootstrapNotificationCenterWindow
    +-- header panel
    |     +-- title label: "Notifications"
    |     +-- unread BootstrapBadge
    |     +-- close BootstrapButton
    |
    +-- BootstrapNotificationHistoryListBox
    |
    +-- footer panel
          +-- BootstrapButton: "Mark all read"
          +-- BootstrapButton: "Clear"
```

Use existing `BootstrapButton` and `BootstrapBadge`.

### History list

`BootstrapNotificationHistoryListBox : ListBox`:

```text
DrawMode = OwnerDrawVariable
IntegralHeight = false
BorderStyle = None
```

Rows render semantic unread marker, optional title, wrapped body, local-time timestamp, and read/unread themed emphasis. Use scoped/theme-owned GDI objects only.

Read interaction:

- Mouse activation marks one item read.
- Enter/Space marks selected item read.
- Arrow selection alone does not mark read.
- Double-click shares the same activation path and cannot double-mutate.
- Mark all and Clear call service operations.
- Empty history shows `No notifications yet.` and disables Mark all/Clear.

### Internal history refresh pipeline

The notification center does **not** subscribe to public `HistoryChanged`.

The service owns one internal method equivalent to:

```csharp
private void PublishCommittedHistoryMutation()
{
    RefreshNotificationCenterFromStore();
    HistoryChanged?.Invoke(this, EventArgs.Empty);
}
```

Rules:

- Call it only after a history mutation is committed and any associated Toast transfer succeeded.
- `RefreshNotificationCenterFromStore()` synchronously updates rows/unread badge if the center exists, visible or hidden.
- A throwing application `HistoryChanged` subscriber cannot prevent the internal center refresh because public event invocation is last.
- No-op mutations never call this method.

---

## UI Thread and Framework Callback Contract

The service records:

```csharp
private readonly int _uiThreadId = Thread.CurrentThread.ManagedThreadId;
private readonly Control _uiDispatcher;
```

Construction:

```csharp
if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
{
    throw new InvalidOperationException(
        "BootstrapToastService must be created on an STA Windows Forms UI thread.");
}

_uiDispatcher = new Control();
_uiDispatcher.CreateControl();
```

Follow the repository's safe handle-creation convention if a specialized internal dispatcher control is preferable.

Public guard:

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

Internal framework callback posting:

```csharp
private void PostFrameworkCallbackToUi(Action callback)
{
    if (_disposed)
    {
        return;
    }

    if (Thread.CurrentThread.ManagedThreadId == _uiThreadId)
    {
        callback();
        return;
    }

    if (_uiDispatcher.IsDisposed || !_uiDispatcher.IsHandleCreated)
    {
        return;
    }

    try
    {
        _uiDispatcher.BeginInvoke((MethodInvoker)(() =>
        {
            if (!_disposed)
            {
                callback();
            }
        }));
    }
    catch (InvalidOperationException)
    {
        // Handle destruction/disposal race: no framework state is touched.
    }
}
```

This helper is only for framework-owned callbacks. Public methods never call it to make wrong-thread usage appear supported.

Disposal order:

```text
Verify UI-thread disposal when called explicitly
mark disposing/disposed state as required to block new callbacks
unsubscribe SystemEvents/ApplicationExit handlers
close/dispose hosts and center
remove queued framework callbacks through disposed guard
finally dispose _uiDispatcher
clear Default slot when applicable
```

---

## Display Topology Refresh Contract

If live topology tracking is enabled, `SystemEvents.DisplaySettingsChanged` handler does no WinForms work directly. It only posts `RefreshDisplayTopology()` through `PostFrameworkCallbackToUi`.

`RefreshDisplayTopology()` on the service UI thread:

1. Read `IBootstrapToastScreenResolver.GetCurrentScreens()` including current DPI.
2. Build a lookup of live device names.
3. For every canonical host whose device still exists, call `ApplySettings()` with the new WorkingArea/Dpi/current service settings.
4. For every host whose device disappeared:
   - remove it from the canonical host dictionary immediately so it cannot conflict with future routing;
   - place it in an internal retiring-host set/list;
   - call `RetireForScreenRemoval()`; this hides the window immediately and dismisses its transient queue without modifying history;
   - dispose/remove it when `BecameEmpty` fires.
5. Do **not** rebind the removed monitor's live stack to primary or another monitor.
6. New notifications resolve only against current screens and may create/reuse the canonical host for their actual target screen.
7. If the notification center is visible on a monitor that disappeared, resolve a current screen using its normal fallback policy and reposition the center there using that screen's DPI. Do not change history/read state.

This contract guarantees one canonical live stack per live screen and eliminates overlap between a rebound stale stack and an existing primary-screen stack.

---

## Event and State Semantics

`HistoryChanged` sender is always the service instance.

Effective operations:

```text
successful Show(... IncludeInHistory=true)
  -> transfer Toast successfully
  -> committed unread history item
  -> internal center refresh
  -> HistoryChanged once

MarkAsRead(unread known id)
  -> replace snapshot
  -> internal center refresh
  -> HistoryChanged once

MarkAsRead(unknown/already-read id)
  -> false
  -> no refresh/event

MarkAllAsRead(any unread)
  -> batch replace
  -> one internal center refresh
  -> one HistoryChanged

MarkAllAsRead(no unread)
  -> no refresh/event

ClearHistory(non-empty)
  -> clear
  -> one internal center refresh
  -> one HistoryChanged

ClearHistory(empty)
  -> no refresh/event

HistoryCapacity reduction that trims
  -> trim
  -> one internal center refresh
  -> one HistoryChanged
```

A public subscriber exception propagates only after framework state/UI are committed/refreshed.

Dismissal/history independence:

```text
Toast dismissed/auto-hidden/monitor-retired
    != mark history read

DismissAll()
    != ClearHistory()

ClearHistory()
    != dismiss transient Toast

service/host disposal
    != manufacture read state
```

Notification-center visibility:

- `IsNotificationCenterVisible` reflects actual visibility.
- `HideNotificationCenter()` is idempotent.
- `ToggleNotificationCenter()` maps hidden -> show and visible -> hide.
- close button, Escape, Alt+F4, and normal user `Close()` all result in hidden/reusable while service is alive.

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

Only internal host-height/host-observation support is added to the existing container; its public API remains unchanged.

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

---

## Prerequisite Gate

Before Task 1:

```powershell
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToast.cs
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastLayoutLogic.cs
Test-Path tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTests.cs
Test-Path tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs
Test-Path tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastAutoHideLifecycleTests.cs
Test-Path demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs
```

Expected: all `True`.

Run baseline regression:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToast|FeedbackDemoForm"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToast|FeedbackDemoForm"
```

Expected: PASS on both targets before implementation.

---

### Task 1: Define and test public options/history snapshot contracts

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastOptions.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryItem.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastOptionsTests.cs`

**Interfaces:**
- Produces: `BootstrapToastOptions`, `BootstrapToastHistoryItem` exactly as defined above.
- Consumes: existing `BootstrapVariant`, `IconDescriptor`, Toast validation conventions.

- [ ] **Step 1: Write failing defaults/normalization tests.**

```csharp
[Test]
public void Options_DefaultsMatchToastContract()
{
    var options = new BootstrapToastOptions();

    Assert.Multiple(() =>
    {
        Assert.That(options.Title, Is.Empty);
        Assert.That(options.Text, Is.Empty);
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
public void Options_NullStringsNormalizeToEmpty()
{
    var options = new BootstrapToastOptions { Title = null!, Text = null! };
    Assert.That(options.Title, Is.Empty);
    Assert.That(options.Text, Is.Empty);
}
```

- [ ] **Step 2: Run focused test and confirm missing-type failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapToastOptionsTests
```

- [ ] **Step 3: Add validation/immutability tests.**

Cover exact exception/no-mutation behavior for undefined Variant, delay/duration <= 0, internal-only history constructor, get-only history properties, UTC timestamp, and immutable read-state snapshots.

- [ ] **Step 4: Implement both model types with XML docs and private backing fields for normalized/validated properties.**

- [ ] **Step 5: Run both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapToastOptionsTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter BootstrapToastOptionsTests
```

- [ ] **Step 6: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastOptions.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryItem.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastOptionsTests.cs
git commit -m "feat: define toast service notification models"
```

---

### Task 2: Add strict-FIFO height-aware ToastContainer hosting

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastLayoutLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastLayoutLogicTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerAnimationTests.cs`

**Interfaces:**
- Produces: internal `MaximumStackHeightPixels` and stack-height helper.
- Preserves: public API and strict FIFO queue semantics.

- [ ] **Step 1: Add failing stack-height helper test.**

```csharp
[Test]
public void CalculateRequiredStackHeight_AddsHeightsAndScaledGaps()
{
    var sizes = new[] { new Size(320, 80), new Size(320, 100), new Size(320, 120) };

    var height = BootstrapToastLayoutLogic.CalculateRequiredStackHeight(
        sizes,
        logicalSpacing: 8,
        dpi: 96);

    Assert.That(height, Is.EqualTo(80 + 8 + 100 + 8 + 120));
}
```

- [ ] **Step 2: Add finite-height tests that lock strict FIFO and oversized behavior.**

Cover:

```text
null height limit -> exact existing count-only behavior
single preferred-height Toast taller than host -> control height is clamped to host height and enters
second Toast that does not fit -> queues
queue head that does not fit blocks later smaller Toast; later Toast never bypasses FIFO head
exit frees enough room -> original queue head promotes first
height grows -> queue head promotes first
height shrinks -> newest excess active items return to queued without Dismissed and without jumping ahead of older queued items
MaximumVisibleToasts and height bound both apply
normal/reduced-motion paths produce same logical queue order
```

- [ ] **Step 3: Implement `MaximumStackHeightPixels` validation and finite-height clamping in the existing height recomputation path.**

- [ ] **Step 4: Replace count-only promotion with strict-head eligibility.**

Conceptual shape:

```csharp
private void PromoteQueuedToasts()
{
    while (CountOccupiedSlots() < _maximumVisibleToasts)
    {
        var next = _entries.FirstOrDefault(x => x.State == BootstrapToastHostState.Queued);
        if (next is null || !CanOccupyNextVisibleSlot(next))
        {
            break;
        }

        BeginEnter(next);
    }
}
```

`CanOccupyNextVisibleSlot` measures/clamps only the head candidate and uses existing occupied/exiting semantics. It never scans later queued entries.

- [ ] **Step 5: Run both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToastContainer|BootstrapToastLayoutLogic"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToastContainer|BootstrapToastLayoutLogic"
```

- [ ] **Step 6: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastContainer.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastLayoutLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastLayoutLogicTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastContainerAnimationTests.cs
git commit -m "refactor: bound toast host height without breaking fifo"
```

---

### Task 3: Implement pure bounded history store

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryStore.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastHistoryStoreTests.cs`

- [ ] **Step 1: Write failing order/capacity/read/rollback tests.**

```csharp
[Test]
public void Add_TrimsOldestAndSnapshotIsNewestFirst()
{
    var store = new BootstrapToastHistoryStore(2);
    var first = HistoryItem("first", false);
    var second = HistoryItem("second", false);
    var third = HistoryItem("third", false);

    store.Add(first);
    store.Add(second);
    store.Add(third);

    Assert.That(store.SnapshotNewestFirst().Select(x => x.Text),
        Is.EqualTo(new[] { "third", "second" }));
}
```

Also cover duplicate id, unread count, no-op reads, immutable previous snapshot, capacity reduction, Clear, and `Remove` rollback.

- [ ] **Step 2: Run and confirm failures.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapToastHistoryStoreTests
```

- [ ] **Step 3: Implement store with no UI/event/static dependency.**

- [ ] **Step 4: Run both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapToastHistoryStoreTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter BootstrapToastHistoryStoreTests
```

- [ ] **Step 5: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHistoryStore.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastHistoryStoreTests.cs
git commit -m "feat: add in-memory toast history store"
```

---

### Task 4: Implement deterministic screen, DPI, and layout helpers

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastScreenResolver.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceLayoutLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceLayoutLogicTests.cs`

**Interfaces:**
- Produces: screen info with `Dpi`, production resolver/DPI provider, pure layout functions.

- [ ] **Step 1: Write pure geometry tests for 96/120/144/168/192 DPI and negative-origin screens.**

```csharp
[Test]
public void InsetWorkingArea_UsesExplicitDpiAndPreservesNegativeOrigin()
{
    var working = new Rectangle(-1920, 0, 1920, 1040);

    var result = BootstrapToastServiceLayoutLogic.InsetWorkingArea(
        working,
        new Padding(16),
        dpi: 96);

    Assert.That(result, Is.EqualTo(new Rectangle(-1904, 16, 1888, 1008)));
}
```

- [ ] **Step 2: Add explicit-DPI width/center-size tests.**

For example, 320 logical px resolves to 320 at 96 DPI and 480 at 144 DPI before available-width clamp. Test all placement corners.

- [ ] **Step 3: Add screen-info validation/provider tests.**

Cover positive DPI, 96 fallback when the native monitor-DPI path is unavailable, and control/active-form DPI preference when a live target handle exists.

- [ ] **Step 4: Implement production resolver and compatibility-safe monitor DPI provider.**

Keep `Screen`/native monitor access at this boundary; do not leak native handles into public API.

- [ ] **Step 5: Run both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter BootstrapToastServiceLayoutLogicTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter BootstrapToastServiceLayoutLogicTests
```

- [ ] **Step 6: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastScreenResolver.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceLayoutLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceLayoutLogicTests.cs
git commit -m "feat: add per-screen toast dpi layout logic"
```

---

### Task 5: Build reusable non-activating top-level Toast host

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceHostContracts.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHostWindow.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastHostWindowTests.cs`

- [ ] **Step 1: Write STA/native-style tests.**

Assert borderless/taskbar-hidden/manual/no-activate/toolwindow behavior, `TopMost`, one fill-docked `BootstrapToastContainer`, and internal height limit tracking host client height.

- [ ] **Step 2: Add tests proving `TransparencyKey` is not used.**

```csharp
Assert.That(host.TransparencyKey, Is.EqualTo(Color.Empty));
```

Use the actual WinForms default expectation appropriate to the test seam; the required behavior is no application-defined transparency key.

- [ ] **Step 3: Add ownership/empty/Region tests.**

Verify Region includes Toast + slide envelope, excludes far blank space, is replaced/disposed on refresh, and is cleared when empty.

- [ ] **Step 4: Add retirement tests.**

```text
RetireForScreenRemoval hides immediately
new ShowToast is rejected after retirement
all owned/queued Toasts are dismissed through container semantics
history is not involved
BecameEmpty fires after ownership reaches zero
```

- [ ] **Step 5: Implement host and coalesced Region refresh.**

Use one pending callback flag and guard handle-destruction/disposal races.

- [ ] **Step 6: Run both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToastHostWindow|BootstrapToastContainer"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToastHostWindow|BootstrapToastContainer"
```

- [ ] **Step 7: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastServiceHostContracts.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastHostWindow.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastHostWindowTests.cs
git commit -m "feat: add non-activating toast host window"
```

---

### Task 6: Implement BootstrapToastService, event pipeline, topology refresh, and Default

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastService.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceTests.cs`
- Modify if required for internal test doubles: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTestDoubles.cs`

**Interfaces:**
- Consumes: Tasks 1/3/4/5.
- Produces: public service excluding center Form implementation, plus internal dispatcher/topology/event orchestration.

- [ ] **Step 1: Write STA/wrong-thread/disposal/dispatcher tests.**

Cover:

```text
constructor STA succeeds
constructor non-STA fails before subscription/window creation
public wrong-thread methods throw and never marshal
private/internal simulated DisplaySettings callback raised from worker thread posts work to creating UI thread
callback after dispose performs no work
Dispose idempotent
manual service independent of Default
```

Use an STA test harness with a real message pump for the framework-callback marshal test.

- [ ] **Step 2: Write defaults/validation tests.**

Assert all public defaults and no mutation after failed setters.

- [ ] **Step 3: Write per-screen DPI routing tests with injected fakes.**

```text
screen A dpi 96 -> 320 logical Toast width becomes 320 px
screen B dpi 144 -> 320 logical Toast width becomes 480 px unless available width clamps it
same device -> host reused
new device -> second host
screen-info DPI update -> future Toast width uses new DPI
```

- [ ] **Step 4: Write Show atomicity/event-order tests.**

Test exact order using fakes/logging:

```text
host exists
history tentative add
Toast transfer succeeds
center-refresh callback runs
public HistoryChanged runs last
```

Also test transfer failure removes history and raises no public event.

Add a throwing public subscriber test proving:

```text
Show throws subscriber exception
Toast was nevertheless transferred successfully
history remains committed
internal center refresh already occurred
```

- [ ] **Step 5: Write history mutation ordering tests.**

For MarkAsRead/MarkAll/Clear/capacity trim, internal center refresh must precede the public event and no-op operations do neither.

- [ ] **Step 6: Write topology-removal tests.**

With fake screens/hosts:

```text
A and B live -> canonical hosts A/B
B disappears -> B removed from canonical dictionary immediately
B RetireForScreenRemoval called once
B is never ApplySettings-rebound to A
new notification on A reuses canonical A only
when retired B becomes empty -> disposed/removed from retiring set
history/read state unchanged by retirement
```

- [ ] **Step 7: Implement service orchestration and central Toast creation.**

```csharp
private BootstrapToast CreateToast(
    BootstrapToastOptionsSnapshot options,
    int widthPixels)
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

Use one canonical-host dictionary plus a separate retiring-host collection.

- [ ] **Step 8: Implement framework callback dispatcher and topology subscription.**

`SystemEvents.DisplaySettingsChanged` handler may only call `PostFrameworkCallbackToUi(RefreshDisplayTopology)`; it may not access Forms/dictionaries directly.

- [ ] **Step 9: Implement lazy Default/ApplicationExit lifecycle.**

ApplicationExit callback uses the same internal UI-posting rule when source thread differs. Explicit public disposal remains UI-thread-affine.

- [ ] **Step 10: Run both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToastService|BootstrapToastHistoryStore|BootstrapToastHostWindow"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToastService|BootstrapToastHistoryStore|BootstrapToastHostWindow"
```

- [ ] **Step 11: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastService.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastTestDoubles.cs
git commit -m "feat: add global toast service orchestration"
```

---

### Task 7: Implement notification-center row rendering and history list

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationCenterRenderLogic.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationHistoryListBox.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNotificationCenterRenderLogicTests.cs`

- [ ] **Step 1: Write row geometry tests.**

Cover empty/non-empty title, long body height, unread marker width, read/unread identical geometry, narrow widths, and 96/120/144/168/192 DPI.

- [ ] **Step 2: Add theme palette tests.**

Use existing theme/semantic helpers, not a new hard-coded notification color system.

- [ ] **Step 3: Implement pure render/measurement logic.**

- [ ] **Step 4: Implement owner-drawn `ListBox`.**

Requirements:

```text
native selection/scroll/focus preserved
OwnerDrawVariable
Enter/Space -> one ItemActivated
mouse activation -> same path
arrow selection -> no activation
theme change invalidates only
DPI change remeasures
Dispose detaches theme events/resources
```

- [ ] **Step 5: Run both targets.**

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

### Task 8: Build and wire reusable notification-center window

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationCenterWindow.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastService.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNotificationCenterTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceTests.cs`

- [ ] **Step 1: Write construction/composition tests.**

Assert borderless/taskbar-hidden center, history list, existing Badge, and existing Buttons.

- [ ] **Step 2: Write explicit-DPI placement tests.**

Injected `BootstrapToastScreenInfo.Dpi` must drive preferred-size scaling and all four corner bounds.

- [ ] **Step 3: Write read interaction and internal-refresh tests.**

Cover newest-first rows, open-without-read, mouse/Enter/Space read, arrow no-read, mark-all, clear, and history mutation while visible/hidden.

- [ ] **Step 4: Write reusable-close lifecycle tests.**

```text
close button -> hidden, not disposed
Escape -> hidden, not disposed
Alt+F4/UserClosing -> hidden, not disposed
calling Close() while service lives -> hidden, not disposed
reopen -> same Form instance
service Dispose -> allow-close flag permits real dispose exactly once
```

- [ ] **Step 5: Implement center and `OnFormClosing` interception.**

Only service disposal bypasses hide-on-close.

- [ ] **Step 6: Wire center through direct internal service refresh, not `HistoryChanged` subscription.**

The center must have zero subscription to the public event. Service history methods invoke internal refresh synchronously, then public event last.

- [ ] **Step 7: Add display-removal center test.**

When the visible center's screen disappears, topology refresh repositions the same center on a current fallback screen using that screen's new DPI without changing history/read state.

- [ ] **Step 8: Run both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapNotificationCenter|BootstrapToastService"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapNotificationCenter|BootstrapToastService"
```

- [ ] **Step 9: Commit.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNotificationCenterWindow.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToastService.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNotificationCenterTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapToastServiceTests.cs
git commit -m "feat: add reusable toast notification center"
```

---

### Task 9: Add Feedback demo and manual verification paths

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs`

- [ ] **Step 1: Add demo-contract tests for:**

```text
Show global Toast
Show non-auto-hide Toast
Burst 7 notifications
Open notification center
Mark all read
Clear history
IncludeInHistory=false
TopMost toggle
all four placements
UnreadCount display
```

- [ ] **Step 2: Extend existing Feedback page, using one consistent service lifetime.**

- [ ] **Step 3: Add `relativeTo` monitor-routing example and unread display updated from `HistoryChanged` without polling.**

- [ ] **Step 4: Run both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FeedbackDemoForm
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FeedbackDemoForm
```

- [ ] **Step 5: Perform manual matrix.**

```text
Light/Dark
Reduced motion on/off
100/125/150/175/200% scaling
all four placements
TopMost false/true
single/burst/long transient content
oversized transient content constrained to screen height while full text remains in center history
auto-hide true/false
close-button dismissal
center read/mark-all/clear
Alt+F4 on center hides and reopening reuses it
history capacity reduction
history-disabled Toast
move demo between monitors
unplug/reconfigure secondary monitor: removed-screen transient host disappears without overlapping primary host; history remains
confirm Toast appearance never steals keyboard focus
confirm explicitly opened center can receive focus
rapid show/dismiss/open/hide stress
```

- [ ] **Step 6: Commit.**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/FeedbackDemoFormTests.cs
git commit -m "demo: showcase global toast notifications"
```

---

### Task 10: Update documentation and frozen public API baseline

**Files:**
- Modify: `README.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

- [ ] **Step 1: Document architecture boundaries.**

Must explicitly document:

```text
application-placed Toast path remains supported
service is higher-level composition
public API is UI-thread-affine
framework callbacks internally marshal to UI thread
per-screen DPI is explicit
removed-screen hosts retire/dismiss instead of rebinding/overlapping
host uses Region, not TransparencyKey
height-bound queue stays strict FIFO
history is semantic/in-memory
internal center refresh occurs before public HistoryChanged
center user-close/Alt+F4 hides; service disposal really disposes
no OS notification/persistence integration
```

- [ ] **Step 2: Document public examples and lifecycle/error semantics.**

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

Also explain that an exception thrown by an application's own `HistoryChanged` handler occurs after the framework state is committed.

- [ ] **Step 3: Run public API baseline and require pre-approval failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter Phase16PublicApiBaselineTests
```

- [ ] **Step 4: Review exported surface.**

Only these new public concepts are accepted:

```text
BootstrapToastOptions
BootstrapToastHistoryItem
BootstrapToastService
```

No public host Form, history store, screen/DPI resolver, dispatcher, center Form/ListBox, host contract, height property, topology-retirement API, render helper, or test seam.

- [ ] **Step 5: Update baseline/docs and re-run both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter Phase16PublicApiBaselineTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter Phase16PublicApiBaselineTests
```

- [ ] **Step 6: Commit.**

```powershell
git add README.md docs/ARCHITECTURE.md docs/COMPONENTS.md docs/TESTING.md docs/PACKAGE_README.md docs/PUBLIC_API_BASELINE.md CHANGELOG.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs
git commit -m "docs: document global toast service"
```

---

### Task 11: Full verification, lifecycle stress, and Stage 8 regression gate

**Files:**
- Modify only concrete files implicated by a discovered verification failure; do not create empty verification commits.

- [ ] **Step 1: Build both targets.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
```

Expected: zero warnings/errors.

- [ ] **Step 2: Run focused tests.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToast|BootstrapNotificationCenter|FeedbackDemoForm|Phase16PublicApiBaselineTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToast|BootstrapNotificationCenter|FeedbackDemoForm|Phase16PublicApiBaselineTests"
```

- [ ] **Step 3: Run full suite.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
```

- [ ] **Step 4: Stress lifecycle/resources.**

Exercise:

```text
100 create/dispose manual services
500 short-lived Toasts with bounded history
200 center open/hide/Alt+F4/reopen cycles proving the same center is reused until disposal
repeated Light/Dark switches
placement/max-visible/height changes during active queueing
repeated history-capacity changes
worker-thread simulated display callbacks marshaled to UI thread
repeated monitor remove/add topology simulations: removed hosts retire, canonical live hosts never overlap/rebind
```

Inspect:

```text
Form count returns to baseline
no disposed service retained by SystemEvents/ApplicationExit
private dispatcher Control disposed
no callback touches UI after disposal
GDI Region count stable after warm-up/GC boundaries
no TransparencyKey configured on host
no timer/animation leak
history contains no Control/IconRenderer/live Toast
strict FIFO preserved under height pressure
no duplicate HistoryChanged or Dismissed events
center refresh is complete before every public HistoryChanged callback observes state
```

- [ ] **Step 5: Re-run original Stage 8 Toast compatibility gate.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapToastTests|BootstrapToastAutoHideLifecycleTests|BootstrapToastContainerTests|BootstrapToastContainerAnimationTests|BootstrapToastLayoutLogicTests|BootstrapToastReviewRegressionTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapToastTests|BootstrapToastAutoHideLifecycleTests|BootstrapToastContainerTests|BootstrapToastContainerAnimationTests|BootstrapToastLayoutLogicTests|BootstrapToastReviewRegressionTests"
```

Expected: PASS.

- [ ] **Step 6: Review diff for forbidden infrastructure.**

Confirm absence of:

```text
new external package
WinRT/Action Center integration
second/background UI thread
public wrong-thread auto-marshaling
Task.Delay/Thread.Sleep animation logic
global input hooks
history persistence
duplicate Toast queue/animation state machine
TransparencyKey-based transient host
live-stack rebinding from removed monitor onto another canonical host
public internal-host/dispatcher/DPI-provider types
caller-owned renderer disposal
```

- [ ] **Step 7: If verification found a concrete bug, fix that bug, rerun the directly affected focused test plus both-target regression gate, and commit the exact changed files with a focused message. If no bug was found, create no commit.**

---

## Acceptance Criteria

The implementation is complete only when all are true:

- `BootstrapToastService.Default.Show(...)` displays transient Toasts without an application-placed container.
- Manual service instances provide the same behavior with caller-controlled lifetime.
- Public service access is bound to one STA UI thread; wrong-thread calls fail deterministically and are never silently marshaled.
- Framework callbacks such as display-topology notifications marshal to the service UI thread before touching WinForms/service state.
- Transient Toasts use framework-owned borderless/non-activating top-level Forms and do not steal focus.
- One canonical host is reused per live screen.
- `BootstrapToastScreenInfo` supplies explicit target DPI; Toast width, margins, host geometry, and notification-center sizing use it deterministically.
- Removed-screen hosts are retired/hidden/dismissed and never rebound on top of another screen's canonical host; history/read state remains intact.
- Hosts use working area, not taskbar-covered bounds.
- Blank host space does not block pointer interaction because host Region is restricted to Toast stack/animation envelope.
- Transient hosts do not use `TransparencyKey`.
- Existing `BootstrapToastContainer` remains the only queue/animation/ownership engine.
- Existing application-placed Toast public API/behavior remains unchanged.
- Height-aware hosting preserves strict FIFO; later small Toasts cannot bypass an older large head item.
- A single oversized Toast is height-clamped under service hosting so it can enter when the stack is empty rather than remain queued forever.
- `TopMost` defaults to false.
- Toast options, width, and renderer are snapshotted at creation.
- History is bounded, in-memory, newest-first, immutable, semantic-only, and contains the full text even when transient rendering is height-constrained.
- New retained history items start unread and `UnreadCount` remains correct.
- Dismissal, monitor retirement, history clear, and read state remain independent.
- Every committed history mutation refreshes the internal center before public `HistoryChanged`; the center does not subscribe to the public event.
- Throwing public event subscribers cannot roll back already-transferred Toasts/history or prevent the center from having been refreshed first.
- Notification center is one reusable interactive service-owned window with keyboard navigation, explicit read activation, Mark all read, Clear, empty state, Light/Dark, and DPI support.
- Close button, Escape, Alt+F4, and user `Close()` hide/reuse the center while service lives; service disposal actually disposes it.
- No live Toast/control/icon-renderer resource is retained by history.
- `Default` is lazy, creates no top-level window before use, cleans static/system subscriptions on exit/disposal, and can be recreated after explicit disposal.
- Both `net48` and `net8.0-windows` builds pass.
- Focused/full tests pass on both targets.
- Feedback demo covers service/history/center/multi-monitor/DPI/focus/topology behavior.
- Documentation distinguishes application-placed Toast containers from the application-global service and documents thread/event semantics.
- Public API baseline contains only the intended three new public concepts.
- No new external dependency, OS-notification integration, persistence layer, second UI thread, duplicate animation scheduler, global input hook, chroma-key host, or stale-host overlap policy is introduced.

---

## Recommended Implementation Order

```text
1. Public options/history models
2. Strict-FIFO internal ToastContainer height bound
3. Pure history store
4. Screen/DPI/layout helpers
5. Non-activating Region-based top-level host + retirement
6. BootstrapToastService + dispatcher + topology + atomic history/event pipeline + Default
7. Notification-center row/list presentation
8. Reusable notification-center window + hide-on-close + direct internal refresh wiring
9. Feedback demo/manual verification
10. Docs + frozen public API review
11. Full regression/lifecycle stress
```

This order keeps each layer independently testable, resolves the reviewed lifecycle/thread/DPI ambiguities before implementation, and preserves the existing Stage 8 Toast primitives as the single source of transient Toast behavior.
