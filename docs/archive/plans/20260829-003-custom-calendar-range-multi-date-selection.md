# Custom Calendar Rendering, Date Range, and Multi-Date Selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a fully framework-rendered Bootstrap-inspired calendar plus date-range and multi-date selection without breaking the existing native-backed `BootstrapDatePicker`, then expose the same calendar through an advanced popup picker that reuses the repository's hosted-control Dropdown infrastructure.

**Architecture:** Preserve `BootstrapDatePicker` exactly as the native single-date/editor option. Add a new owner-drawn `BootstrapCalendar : Control` with no per-day child controls; it owns month/header/week/day rendering, hit testing, keyboard focus, theme/DPI behavior, accessibility, and delegates all selection rules to one internal `BootstrapCalendarSelectionModel` shared by `Single`, `Range`, and `Multiple` modes. Add `BootstrapCalendarPicker : Control` as a compact summary/trigger surface that hosts a fresh `BootstrapCalendar` inside `BootstrapDropdown` using the hosted-control support from `docs/plans/20260829-002-dropdown-submenus-hosted-controls-split-button.md`. Widen only the **internal** Dropdown opening primitive so non-button controls can supply a presentation `Control` plus `IIconRenderer`; classic Dropdown and SplitButton public APIs remain unchanged. Native ToolStrip infrastructure continues to own popup working-area placement, outside-click/Escape dismissal, and hosted-control lifetime.

**Tech Stack:** C#, native Windows Forms owner drawing (`Control`, `Graphics`, `TextRenderer`), existing Theme / Rendering / Icons / Compatibility infrastructure, `BootstrapDropdown` + `ToolStripControlHost` hosted-control support from plan `20260829-002`, `BootstrapValidationState`, `BootstrapThemeManager`, `BootstrapThemeMetrics`, `DpiScaler`, `RoundedPath`, `CornerRadius`, `BootstrapIconRenderer`, `IIconRenderer`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** Advanced extension of `docs/plans/20260828-009-bootstrap-date-picker.md`. Stage 9 deliberately deferred custom calendar rendering, date ranges, and multi-date selection; this plan makes those capabilities explicit while preserving the Stage 9 contract. Popup composition depends on the hosted-control and internal anchored-show work in `docs/plans/20260829-002-dropdown-submenus-hosted-controls-split-button.md`. Repository-wide constraints in `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, and `docs/PUBLIC_API_BASELINE.md` remain authoritative.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; new public types live under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code compiles from one shared implementation for both `net48` and `net8.0-windows` wherever practical.
- Do **not** replace, subclass around, or silently change the existing native-backed `BootstrapDatePicker`. Its current public members, native `DateTimePicker` behavior, tests, and compatibility guarantees remain intact.
- `BootstrapDatePicker` remains the choice for native typed date/time segment editing, native locale formatting, `ShowCheckBox`, and the OS-owned calendar popup.
- `BootstrapCalendar` is the new fully custom-rendered date-selection surface. The framework owns every visible calendar pixel: month header, navigation buttons, weekday labels, 42 day cells, hover, focus, disabled, today, selected, committed-range, and preview-range states.
- `BootstrapCalendar` is one owner-drawn focusable control. Do not create 42 `Button`, `Label`, or other child controls for day cells.
- Do not use native `MonthCalendar` or a hidden/native `DateTimePicker` inside `BootstrapCalendar`.
- One internal selection state machine powers `Single`, `Range`, and `Multiple`; do not duplicate click/keyboard selection algorithms per mode.
- All public/internal selections are calendar dates, not instants. Normalize accepted values using `value.Date`; time-of-day is intentionally discarded.
- The supported calendar date domain is **exactly** `DateTimePicker.MinimumDateTime.Date` through `DateTimePicker.MaximumDateTime.Date`, inclusive. This is an invariant, not merely a default. `MinDate`, `MaxDate`, every programmatic selection, keyboard/mouse/accessibility activation, and internal model bound must remain inside this domain.
- Default bounds are the entire supported domain. Assigning a bound below `DateTimePicker.MinimumDateTime.Date`, above `DateTimePicker.MaximumDateTime.Date`, or producing `MinDate > MaxDate` throws `ArgumentOutOfRangeException` before mutation.
- The supported-domain invariant exists partly to make six-week projection safe: leading/trailing adjacent-month cells can be computed without underflowing `DateTime.MinValue` or overflowing `DateTime.MaxValue`.
- Programmatic selection outside `[MinDate, MaxDate]` throws `ArgumentOutOfRangeException`; out-of-range UI cells render disabled and never activate.
- Bound changes may invalidate selection: Single clears if selected date is outside; incomplete/complete Range clears if either stored endpoint is outside; Multiple removes only newly invalid dates. Raise `SelectionChanged` once if effective public selection changes.
- `DisplayMonth` is normalized to the first day of its month and clamped to a month intersecting the allowed bounds. `DisplayMonthChanged` fires only on effective change.
- Default `DisplayMonth` is the first day of the current local month, clamped to `[MinDate, MaxDate]`.
- Culture is read from `CultureInfo.CurrentCulture` at layout/render/format time. Week order begins at `CurrentCulture.DateTimeFormat.FirstDayOfWeek`. No separate public Culture property is added.
- The visual grid is always 6 rows x 7 columns = 42 date cells. Leading/trailing adjacent-month days remain visible, muted, and selectable when in range.
- Selecting an enabled adjacent-month day applies selection first, then moves `DisplayMonth` to that date's month.
- `Single` stores zero or one `SelectedDate`.
- `Range` stores `RangeStart` and optional `RangeEnd`. First activation starts/restarts an incomplete range. Second activation completes it and normalizes endpoint order. Third activation after completion starts a new incomplete range.
- Range hover preview is presentation-only and never mutates range state or raises `SelectionChanged`.
- `Multiple` toggles each activated date. `SelectedDates` is a sorted, deduplicated ascending snapshot.
- Switching `SelectionMode` clears prior-mode selection. Raise `SelectionChanged` once if anything was selected; never reinterpret state across modes implicitly.
- Keyboard focus date is private state distinct from public selection. Its lifecycle is deterministic and defined below; focus movement alone never raises `SelectionChanged`.
- Left/Right move keyboard focus +/-1 day; Up/Down +/-7 days; PageUp/PageDown +/-1 month with day clamping; Home/End move to culture-week start/end; Enter/Space activates the focused date. Crossing a month boundary updates `DisplayMonth`.
- Month arithmetic clamps day safely and must not use `Math.Clamp`, unavailable on `net48`.
- Navigation buttons are disabled/no-op when the adjacent month does not intersect allowed bounds.
- Theme colors/metrics come from `BootstrapThemeManager.CurrentTheme`; do not add calendar-only hard-coded palette constants where existing `Surface`, `SurfaceSecondary`, `Border`, `Text`, `MutedText`, `Disabled`, `Focus`, `Hover`, `Active`, and `Primary` suffice.
- Selected endpoint/multiple cells use `Active` surface plus `Primary` outline; committed range interiors use `SurfaceSecondary`; preview range uses `Hover`; focused cell adds the `Focus` outline. Text uses `Text`/`MutedText` according to enabled/current-month state.
- Derive geometry from `BootstrapThemeMetrics` and `DpiScaler`; do not extend the theme constructor only for calendar sizing.
- 96-DPI baseline uses `SpacingSM` outer padding, `ControlHeight` header, `ControlHeightSmall` weekday row, `ControlHeight` day rows/cell preferred width, `SpacingXS` gaps, and theme radius for `BorderRadius = -1`.
- `BootstrapCalendar.GetPreferredSize(...)` is deterministic. Preferred width is `2*OuterPadding + 7*PreferredDayCellWidth + 6*CellGap`; preferred height is `2*OuterPadding + HeaderHeight + WeekdayHeight + 6*DayRowHeight + 7*CellGap`, where `PreferredDayCellWidth` uses the DPI-scaled `ControlHeight` metric. The popup factory uses the same pure preferred-size helper with the picker DPI.
- Cache resolved layout/cell geometry when size, DPI, display month, culture week start, bounds, or border radius changes. Do not allocate a new 42-cell list on every `OnPaint`.
- Dispose temporary GDI objects deterministically. Do not retain persistent `Pen`, `Brush`, `GraphicsPath`, `Bitmap`, or `Region` unless measurements prove it necessary.
- Theme subscription occurs at most once and is removed on disposal. Dispose only framework-created fonts; never dispose caller-assigned fonts.
- Designer construction must work without app bootstrap, DI, a running message loop, assigned parent, or initialized service locator.
- `BootstrapCalendarPicker` reuses hosted-control `BootstrapDropdown`. Do not create a custom top-level `Form`, second popup engine, global hook, or parallel placement/dismissal implementation.
- Plan `20260829-002` introduces `BootstrapDropdown.ShowFrom(BootstrapButton presentationSource, Control anchor, Point location)`. This plan keeps that overload for `BootstrapSplitButton` and adds an **internal-only** generic overload: `ShowFrom(Control presentationSource, IIconRenderer iconRenderer, Control anchor, Point location)`. The existing Button overload delegates to the generic overload after preserving Button-specific guards such as loading state.
- The generic Dropdown opening overload uses `presentationSource.DeviceDpi`, `presentationSource.Font`, control enabled/disposed state, and the explicit `IIconRenderer`; it never requires an invisible fake `BootstrapButton` merely to open a calendar popup.
- The generic internal overload does not change `BootstrapDropdown.Target`, public `Show()`, public `Close()`, or `BootstrapSplitButton` public behavior.
- The popup calendar is factory-created for each effective Dropdown snapshot because `ToolStripControlHost` owns hosted-control disposal. The picker may keep one **non-owning** `_activeCalendar` reference only while the popup is visible; it unsubscribes and clears that reference in `Closed` before the next snapshot can replace/dispose the hosted control.
- While the popup is open, programmatic picker changes (`SelectionMode`, bounds, selected state, `ClearSelection`) synchronize immediately into `_activeCalendar` under one private `_synchronizingCalendar` guard. Do not silently close the popup solely because programmatic state changed.
- `BootstrapCalendar` exposes an **internal-only activation signal** for valid user/accessibility activations. It fires even when Single mode re-activates the already selected date. Public `SelectionChanged` remains change-only. The picker uses the internal signal's `Changed` bit for its public event and `Completed` bit for close policy.
- Single-mode picker activation commits and closes immediately, including same-date reactivation with no duplicate public `SelectionChanged`. Range stays open after first endpoint and closes after second. Multiple remains open after toggles and closes only through Escape, outside click, trigger toggle, or `CloseDropDown()`.
- Closing with an incomplete range preserves its `RangeStart`; there is no hidden rollback buffer.
- Picker text is summary-only. No typed date parser/editor is added; `BootstrapDatePicker` remains the typed/native option.
- `DateFormat` defaults to `"d"`; setter validates before mutation by formatting a known in-range date with `CultureInfo.CurrentCulture`, preserving normal `FormatException` for invalid formats.
- Empty selection renders `PlaceholderText`; default is `string.Empty` so library code does not embed visible English placeholder copy.
- Range summary uses the same format for both endpoints. Incomplete: `<start> – …`; complete: `<start> – <end>`.
- Multiple summary renders one formatted date when count=1; count>=2 renders `<first> (+N)` where N is remaining count.
- Picker validation priority matches established inputs: disabled -> Valid/Invalid -> focus -> neutral.
- Accessibility is interactive, not read-only metadata: calendar nav/day logical children expose default actions, disabled cells cannot activate, parent/children support hit testing/navigation, and picker accessibility exposes DropList semantics, current summary, expanded/collapsed state, and an Open/Close default action.
- All new public/protected members receive XML docs. Warning-as-error/XML-doc policy remains green.
- This extends the frozen v1 public API. `Phase16PublicApiBaselineTests` must intentionally fail before baseline approval, then be updated only after exported surface review.
- Completion requires both target builds, focused/full tests, Light/Dark, real Windows 100/125/150/175/200% DPI, keyboard/mouse/popup manual verification, accessibility checks, and GDI/event/host ownership review.

---

## Prerequisite Gate

Before Task 1, the existing Stage 9 DatePicker must be green and plan `20260829-002` must have implemented hosted controls plus the Button-oriented internal anchored-show path.

Expected artifacts:

```text
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePicker.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePickerRenderLogic.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItem.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemKind.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
```

Required plan-002 contract:

```csharp
BootstrapDropdownItemKind.HostedControl
BootstrapDropdownItem.DropDownItems
BootstrapDropdownItem.HostedControlFactory
BootstrapDropdown.ShowFrom(BootstrapButton presentationSource, Control anchor, Point location) // internal
```

Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapDatePicker|BootstrapDropdown"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapDatePicker|BootstrapDropdown"
```

Expected: PASS. If plan `20260829-002` is not implemented, standalone `BootstrapCalendar` Tasks 2-6 may proceed, but finish the Dropdown plan before Task 7; never embed a calendar-specific popup workaround.

---

## Public Contract Added by This Plan

### Selection mode

```csharp
public enum BootstrapCalendarSelectionMode
{
    Single = 0,
    Range = 1,
    Multiple = 2
}
```

Undefined values throw `ArgumentOutOfRangeException` before mutation.

### Standalone calendar

```csharp
[DefaultEvent(nameof(SelectionChanged))]
public class BootstrapCalendar : Control
{
    public BootstrapCalendar();

    public BootstrapCalendarSelectionMode SelectionMode { get; set; }
    public DateTime DisplayMonth { get; set; }
    public DateTime MinDate { get; set; }
    public DateTime MaxDate { get; set; }
    public DateTime? SelectedDate { get; set; }
    public DateTime? RangeStart { get; }
    public DateTime? RangeEnd { get; }

    [Browsable(false)]
    public IReadOnlyList<DateTime> SelectedDates { get; }

    public int BorderRadius { get; set; }

    public event EventHandler? SelectionChanged;
    public event EventHandler? DisplayMonthChanged;

    public void SetRange(DateTime? start, DateTime? end);
    public void SetSelectedDates(IEnumerable<DateTime> dates);
    public void ClearSelection();
    public void ShowPreviousMonth();
    public void ShowNextMonth();
}
```

Mode-specific behavior:

- `SelectedDate` returns data only in Single; setter outside Single throws `InvalidOperationException`. `null` clears Single.
- `RangeStart`/`RangeEnd` are populated only in Range.
- `SetRange(null, null)` clears. `SetRange(start, null)` creates incomplete range. `SetRange(null, end)` throws `ArgumentException`. Two endpoints normalize ascending.
- `SetRange` outside Range throws `InvalidOperationException`.
- `SelectedDates` is an ascending snapshot in Multiple; outside Multiple it is empty.
- `SetSelectedDates` outside Multiple throws `InvalidOperationException`; null collection throws `ArgumentNullException`; duplicates dedupe after `.Date`; any invalid date rejects the whole replacement before mutation.
- `ClearSelection()` is valid in every mode and raises only on effective change.
- `DisplayMonth` defaults to current month first-day, normalizes to day 1, and clamps to a month intersecting bounds.
- `MinDate`/`MaxDate` must remain inside the supported `DateTimePicker` domain described above.
- `BorderRadius=-1` means current theme radius; nonnegative is explicit logical radius; `< -1` throws before mutation.
- `GetPreferredSize` follows the deterministic metric formula in the Rendering/Layout Contract below.

### Advanced popup picker

```csharp
[DefaultEvent(nameof(SelectionChanged))]
public class BootstrapCalendarPicker : Control
{
    public BootstrapCalendarPicker();

    public BootstrapCalendarSelectionMode SelectionMode { get; set; }
    public DateTime MinDate { get; set; }
    public DateTime MaxDate { get; set; }
    public DateTime? SelectedDate { get; set; }
    public DateTime? RangeStart { get; }
    public DateTime? RangeEnd { get; }

    [Browsable(false)]
    public IReadOnlyList<DateTime> SelectedDates { get; }

    public string DateFormat { get; set; }
    public string PlaceholderText { get; set; }
    public BootstrapValidationState ValidationState { get; set; }
    public int BorderRadius { get; set; }

    public event EventHandler? SelectionChanged;
    public event EventHandler? Opened;
    public event EventHandler? Closed;

    public void SetRange(DateTime? start, DateTime? end);
    public void SetSelectedDates(IEnumerable<DateTime> dates);
    public void ClearSelection();
    public void ShowDropDown();
    public void CloseDropDown();
}
```

Picker mode-specific selection semantics exactly match `BootstrapCalendar`. Do not add aliases such as `Value`, `StartDate`, `EndDate`, `Dates`, `SelectedRange`, or public `IsOpen`.

### Deliberately not added

- No changes to `BootstrapDatePicker`.
- No time-of-day selection or `DateTimeOffset` API.
- No consumer per-cell paint/template callback yet; “fully custom rendering” means framework-owned rendering rather than OS-owned rendering.
- No week numbers, year/decade zoom, multi-month view, drag range, blackout predicate/collection, holiday/event badges, recurrence, or month animation.
- No typed parser/editor in `BootstrapCalendarPicker`.
- No public native ToolStrip/popup/host handles or hosted calendar reference.
- No second popup-placement engine or top-level popup Form.

---

## Internal Selection and Activation Contract

Create one model shared by calendar and picker:

```csharp
internal readonly struct BootstrapCalendarSelectionChange
{
    public BootstrapCalendarSelectionChange(bool changed, bool completed);
    public bool Changed { get; }
    public bool Completed { get; }
}

internal sealed class BootstrapCalendarSelectionModel
{
    internal static readonly DateTime MinimumSupportedDate = DateTimePicker.MinimumDateTime.Date;
    internal static readonly DateTime MaximumSupportedDate = DateTimePicker.MaximumDateTime.Date;

    public BootstrapCalendarSelectionModel(DateTime minDate, DateTime maxDate);

    public BootstrapCalendarSelectionMode Mode { get; }
    public DateTime MinDate { get; }
    public DateTime MaxDate { get; }
    public DateTime? SelectedDate { get; }
    public DateTime? RangeStart { get; }
    public DateTime? RangeEnd { get; }
    public IReadOnlyList<DateTime> SelectedDates { get; }

    public bool SetMode(BootstrapCalendarSelectionMode mode);
    public bool SetBounds(DateTime minDate, DateTime maxDate);
    public bool SetSelectedDate(DateTime? date);
    public bool SetRange(DateTime? start, DateTime? end);
    public bool SetSelectedDates(IEnumerable<DateTime> dates);
    public bool Clear();
    public BootstrapCalendarSelectionChange Activate(DateTime date);
}
```

The model raises no events. Bool results report effective selection change. `Activate.Completed` is true for Single, false after first Range endpoint, true after second Range endpoint, and false for Multiple. A same-date Single activation returns `Changed=false, Completed=true`, allowing a picker to close without emitting a duplicate public change event.

`SetBounds` validation order is atomic:

```text
1. normalize min/max with .Date
2. reject min < MinimumSupportedDate
3. reject max > MaximumSupportedDate
4. reject min > max
5. only then mutate bounds and reconcile selection
```

### Internal user-activation signal

`BootstrapCalendar` adds no public event for activation completion. Instead it exposes this internal-only contract inside the product assembly:

```csharp
internal sealed class BootstrapCalendarSelectionActivatedEventArgs : EventArgs
{
    public BootstrapCalendarSelectionActivatedEventArgs(
        DateTime date,
        bool changed,
        bool completed);

    public DateTime Date { get; }
    public bool Changed { get; }
    public bool Completed { get; }
}

internal event EventHandler<BootstrapCalendarSelectionActivatedEventArgs>? SelectionActivated;
```

All valid user-equivalent activation paths funnel through one private method:

```csharp
private void ActivateDate(DateTime date)
{
    var change = _selection.Activate(date);
    _focusedDate = date;

    if (change.Changed)
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    SelectionActivated?.Invoke(
        this,
        new BootstrapCalendarSelectionActivatedEventArgs(
            date,
            change.Changed,
            change.Completed));
}
```

Mouse, Enter/Space, and accessibility `DoDefaultAction()` all use this path. Programmatic setters **do not** raise `SelectionActivated`; they only raise normal public state events when effective state changes.

---

## Rendering, Preferred Size, and Keyboard-Focus Contract

Create pure/internal types in `BootstrapCalendarRenderLogic.cs`:

```csharp
internal readonly struct BootstrapCalendarMetrics
{
    public int OuterPadding { get; }
    public int CellGap { get; }
    public int HeaderHeight { get; }
    public int WeekdayHeight { get; }
    public int DayRowHeight { get; }
    public int PreferredDayCellWidth { get; }
    public float BorderWidth { get; }
    public float FocusBorderWidth { get; }
    public float Radius { get; }
}

internal readonly struct BootstrapCalendarDayCell
{
    public int Index { get; }
    public DateTime Date { get; }
    public Rectangle Bounds { get; }
    public bool IsCurrentMonth { get; }
    public bool IsEnabled { get; }
    public bool IsToday { get; }
}

internal sealed class BootstrapCalendarLayout
{
    public Rectangle HeaderBounds { get; }
    public Rectangle PreviousButtonBounds { get; }
    public Rectangle MonthTitleBounds { get; }
    public Rectangle NextButtonBounds { get; }
    public IReadOnlyList<Rectangle> WeekdayBounds { get; }
    public IReadOnlyList<BootstrapCalendarDayCell> DayCells { get; }
}
```

Pure helper surface:

```csharp
internal static BootstrapCalendarMetrics ResolveMetrics(
    BootstrapThemeMetrics themeMetrics, int dpi, int borderRadius);

internal static Size CalculatePreferredSize(BootstrapCalendarMetrics metrics);

internal static BootstrapCalendarLayout CalculateLayout(
    Size clientSize,
    BootstrapCalendarMetrics metrics,
    DateTime displayMonth,
    DayOfWeek firstDayOfWeek,
    DateTime minDate,
    DateTime maxDate,
    DateTime today);

internal static int HitTestDay(Point location, BootstrapCalendarLayout layout);
internal static DateTime MoveByMonth(DateTime date, int months);
internal static DateTime MoveToWeekBoundary(
    DateTime date, DayOfWeek firstDayOfWeek, bool endOfWeek);
```

Month projection:

```text
monthStart = new DateTime(displayMonth.Year, displayMonth.Month, 1)
offset = ((int)monthStart.DayOfWeek - (int)firstDayOfWeek + 7) % 7
gridStart = monthStart.AddDays(-offset)
cell[i].Date = gridStart.AddDays(i), i = 0..41
```

Because `displayMonth` is always clamped to a month intersecting the safe supported domain, this projection must be tested at both minimum and maximum supported months to prove no underflow/overflow.

Preferred size:

```text
width  = 2*OuterPadding
       + 7*PreferredDayCellWidth
       + 6*CellGap

height = 2*OuterPadding
       + HeaderHeight
       + WeekdayHeight
       + 6*DayRowHeight
       + 7*CellGap
```

The helper returns 42 logical cells even for tiny/zero client sizes. Rectangle dimensions never become negative. Culture-localized strings are resolved during render, not cached in layout objects.

### Private keyboard-focus lifecycle

`BootstrapCalendar` stores one private `_focusedDate` that is always normalized and clamped to `[MinDate, MaxDate]`.

Rules:

- Constructor: `_focusedDate = Clamp(DateTime.Today.Date, MinDate, MaxDate)`; default `DisplayMonth` is `_focusedDate`'s month.
- Mouse/accessibility activation: set `_focusedDate` to the activated date before any adjacent-month display update.
- Keyboard navigation: compute the next focus date, clamp to bounds, then update `DisplayMonth` if its month differs.
- Programmatic non-empty selection replacement reanchors focus to the mode's primary selected date: Single=`SelectedDate`; Range=`RangeEnd ?? RangeStart`; Multiple=first selected date. Clearing selection preserves the current in-range focus date.
- Mode changes clear selection but preserve the current in-range focus date.
- Bound changes clamp `_focusedDate` after selection reconciliation.
- Direct `DisplayMonth` assignment and header previous/next navigation keep the current day-of-month when possible in the target month, using `MoveByMonth`/`DaysInMonth`, then clamp to bounds. If the existing focus is already in the target month, preserve it.
- Adjacent-month selection sets focus to the clicked date first, so the subsequent display-month update preserves that exact focus date.
- Hover never changes `_focusedDate`.
- Accessibility reports `Focused` only for the logical day child whose date equals `_focusedDate` while the calendar itself contains keyboard focus.

Do not implement a second keyboard-selection state path in the picker; once the hosted calendar owns focus, it is authoritative.

---

## Hosted Picker Lifecycle Contract

`BootstrapCalendarPicker` owns logical state and one `BootstrapDropdown`. It does **not** own hosted calendar lifetime.

Private lifecycle fields are expected to include:

```csharp
private BootstrapCalendar? _activeCalendar; // non-owning; visible popup only
private DateTime? _lastDisplayMonth;         // logical state only
private bool _isDropDownOpen;
private bool _synchronizingCalendar;
```

Rules:

- `_lastDisplayMonth` starts `null`.
- First opening seeds the popup month from current selection in this order: `SelectedDate`, `RangeStart`, first Multiple date, `DateTime.Today`; then clamp to bounds.
- After a hosted calendar reports `DisplayMonthChanged`, retain its normalized display month in `_lastDisplayMonth`.
- Subsequent openings prefer `_lastDisplayMonth` before selection/today fallback.
- Bounds changes clamp `_lastDisplayMonth` if non-null.
- The HostedControl factory creates a fresh calendar, configures mode/bounds/state/display month/size, subscribes `SelectionActivated` and `DisplayMonthChanged`, assigns `_activeCalendar` as its final successful setup step, and returns it. If setup fails before return, unsubscribe/dispose the local calendar and rethrow.
- `ShowDropDown()` wraps the generic Dropdown open call. If opening throws after the factory assigned `_activeCalendar`, detach/clear `_activeCalendar` and rethrow; the picker never retains a failed-open control reference.
- `Opened` sets `_isDropDownOpen=true`, raises the picker `Opened` event once, and transfers keyboard focus to `_activeCalendar` so arrows/PageUp/Home/Enter operate on the calendar rather than the ToolStrip host.
- `Closed` first unsubscribes from `_activeCalendar`, clears `_activeCalendar`, sets `_isDropDownOpen=false`, then raises picker `Closed` once. The reference is not retained merely because plan-002 may keep a closed native snapshot alive until rebuild/disposal.
- Programmatic picker state changes while open update the picker model first, raise picker `SelectionChanged` if needed, then synchronize the complete state into `_activeCalendar` under `_synchronizingCalendar=true`. This synchronization must not be interpreted as a user activation and must not auto-close the popup.
- Hosted `SelectionActivated` is the only path used for user activation close policy. Copy the hosted selection snapshot into picker state. If `Changed=true`, raise picker `SelectionChanged` once. If mode is Single and `Completed=true`, close even when `Changed=false`. If mode is Range, close only when `Completed=true`. Multiple never auto-closes.
- A second trigger activation while `_isDropDownOpen` calls `CloseDropDown()`; it does not invoke the generic `ShowFrom` path again.
- Do not expose `_activeCalendar`, `_isDropDownOpen`, or native Dropdown visibility as public API.

---

## Accessibility Contract

### BootstrapCalendar

`CreateAccessibilityInstance()` returns an internal `ControlAccessibleObject` implementation exposing exactly 44 logical children in this order:

```text
0 Previous-month button
1 Next-month button
2..43 Day cells 0..41
```

Required behavior:

- Navigation children: `AccessibleRole.PushButton`, state includes `Unavailable` when that direction cannot navigate, `DefaultAction` describes previous/next month, `DoDefaultAction()` calls the same public/private month navigation path as mouse.
- Day children: `AccessibleRole.Cell`; `Name` is a culture-aware full date string; disabled dates include `Unavailable`; selected endpoints/multiple dates and committed range cells include `Selected`; keyboard focus includes `Focused` only as defined above.
- Enabled day `DefaultAction` is selection/activation; `DoDefaultAction()` funnels through the same `ActivateDate(...)` path as mouse/keyboard, so picker completion semantics remain consistent. Disabled day action is a no-op.
- Parent `GetChildCount()`/`GetChild()` are deterministic. Parent hit testing returns the corresponding nav/day child for points inside those logical bounds; otherwise it returns the normal parent/base result.
- Child `Navigate(...)` supports previous/next sibling traversal so screen readers can move through nav controls and the 42 date cells without native child controls.
- Resizing/DPI/display-month changes update accessible bounds/names from current cached layout; do not create WinForms child controls merely for accessibility.

### BootstrapCalendarPicker

The picker is one custom-drawn focusable control with `AccessibleRole.DropList`.

Its accessibility object exposes:

- `Name`: caller-provided `AccessibleName` when set; otherwise the base accessible name.
- `Value`: current formatted summary, or `PlaceholderText` when empty.
- State: `Expanded` while `_isDropDownOpen`, otherwise `Collapsed`; include `Unavailable` when disabled.
- `DefaultAction`: Open calendar when closed, Close calendar when open.
- `DoDefaultAction()`: invokes the same toggle path as mouse/Enter/F4/Alt+Down and respects disabled state.

No public accessibility-specific API is added.

---

## File Map

### Create product files

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionMode.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionModel.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarPicker.cs`

### Modify shared Dropdown infrastructure in Task 7

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`

The change is internal-only: preserve the plan-002 Button overload and add a generic `Control + IIconRenderer` overload used by the calendar picker.

### Create tests

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarSelectionModelTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs`

### Modify demo/tests

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs`

### Modify docs/public baseline

- `docs/COMPONENTS.md`
- `docs/TESTING.md`
- `docs/COMPATIBILITY.md`
- `README.md`
- `docs/PACKAGE_README.md`
- `CHANGELOG.md`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- `docs/PUBLIC_API_BASELINE.md`

SDK-style default compile inclusion should require no `.csproj` edit unless current project explicitly excludes these paths.

---

### Task 1: Characterize hosted-control popup focus and keyboard interaction

**Files:**
- Create/Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs`

**Interfaces:** consumes plan-002 hosted-control Dropdown behavior; produces a prerequisite guard proving a calendar-like hosted control can keep the popup open and receive navigation/activation keys.

- [ ] **Step 1:** Create an STA fixture with a visible off-screen `Form`, a `BootstrapButton` presentation source, and one HostedControl factory returning a focusable probe control. Open the dropdown, focus/click the hosted control, pump `Application.DoEvents()`, and assert `Opened=1`, `Closed=0`, hosted click=1.
- [ ] **Step 2:** Extend the probe so `IsInputKey`/`OnKeyDown` record Left, Right, Up, Down, PageUp, PageDown, Home, End, Enter, and Space. Focus the hosted control and verify each key reaches it while the dropdown remains open unless the probe itself explicitly requests close.
- [ ] **Step 3:** Characterize focus immediately after native `Opened`: calling `Focus()` on the hosted control must succeed after the popup is shown. Keep real Escape/outside-click verification manual; do not reflect private ToolStrip message plumbing.
- [ ] **Step 4:** Characterize explicit `Close()` separately and assert `Closed` exactly once.
- [ ] **Step 5:** Run on both targets:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapCalendarPickerTests&Name~HostedControl"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapCalendarPickerTests&Name~HostedControl"
```

- [ ] **Step 6:** If hosted interaction auto-closes or navigation keys never reach the focused hosted control, fix plan-002 hosted-control semantics first and add the reproducing Dropdown test there. Do not create a calendar-specific popup workaround.
- [ ] **Step 7:** Commit:

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs
git commit -m "test: characterize hosted calendar popup input"
```

---

### Task 2: Implement shared Single/Range/Multiple selection state and safe date domain

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionMode.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionModel.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarSelectionModelTests.cs`

**Interfaces:** produces `BootstrapCalendarSelectionMode`, `BootstrapCalendarSelectionChange`, and one atomic model used by both standalone calendar and picker.

- [ ] **Step 1:** Write failing construction/domain tests: default Single/empty, `.Date` normalization, constants equal `DateTimePicker.MinimumDateTime.Date`/`MaximumDateTime.Date`, min>max rejection, min below supported domain rejection, max above supported domain rejection, and undefined mode rejection.
- [ ] **Step 2:** Run focused tests and verify compile failure because types do not exist.
- [ ] **Step 3:** Implement enum/model skeleton, supported-domain constants, validation helpers, `SortedSet<DateTime>` for Multiple, and immutable snapshots.
- [ ] **Step 4:** Add failing Single tests for first activation, same-date confirmation (`Changed=false, Completed=true`), different date, null clear, out-of-range atomic rejection.
- [ ] **Step 5:** Implement Single minimally.
- [ ] **Step 6:** Add failing Range tests for first endpoint, reverse second endpoint normalization, completed third-click restart, `SetRange(start,null)`, clear, null/end rejection, and out-of-range atomic rejection.
- [ ] **Step 7:** Implement Range.
- [ ] **Step 8:** Add failing Multiple tests for toggle, sort, dedupe, atomic invalid input rejection, and `Completed=false`.
- [ ] **Step 9:** Implement Multiple.
- [ ] **Step 10:** Add bound/mode transition tests: clear invalid Single/Range, filter Multiple, preserve compatible state, clear on mode switch with exact changed flag. Include minimum/maximum supported bound values and verify an attempted invalid bound assignment leaves all prior bounds/selection unchanged.
- [ ] **Step 11:** Run both targets:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapCalendarSelectionModelTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapCalendarSelectionModelTests
```

- [ ] **Step 12:** Commit:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionMode.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionModel.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarSelectionModelTests.cs
git commit -m "feat: add safe calendar selection state model"
```

---

### Task 3: Implement pure six-week projection, preferred size, DPI layout, hit testing, and safe date navigation

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs`

**Interfaces:** consumes safe supported dates from Task 2; produces all non-UI calendar geometry and date navigation helpers.

- [ ] **Step 1:** Write failing metric tests at DPI 96/120/144/168/192, theme/default vs explicit radius, preferred day-cell width, invalid DPI/radius.
- [ ] **Step 2:** Write failing preferred-size tests using the exact formula above and assert monotonic scaling across DPI values.
- [ ] **Step 3:** Write failing month projection tests using explicit first-day-of-week: September 2026 Sunday/Monday first, leap February 2028, Dec/Jan boundaries, exactly 42 consecutive dates.
- [ ] **Step 4:** Add explicit safe-boundary projection tests for the month containing `DateTimePicker.MinimumDateTime` and the month containing `DateTimePicker.MaximumDateTime`. Assert 42 cells are produced without exception and every generated `DateTime` remains representable.
- [ ] **Step 5:** Implement metrics/projection/preferred-size only enough to pass.
- [ ] **Step 6:** Add normal/tiny/zero layout tests: contained nonnegative rectangles, seven weekday columns, six day rows, deterministic remainder-pixel distribution.
- [ ] **Step 7:** Implement integer layout so accumulated rounding never leaves unexplained right/bottom gaps.
- [ ] **Step 8:** Add/implement hit-test tests for representative cells and header/gap/outside points (`-1`).
- [ ] **Step 9:** Add navigation tests:

```text
2025-01-31 +1 month = 2025-02-28
2028-01-31 +1 month = 2028-02-29
2026-03-31 -1 month = 2026-02-28
Sunday-first Home/End = Sunday/Saturday
Monday-first Home/End = Monday/Sunday
```

- [ ] **Step 10:** Implement month navigation with `Math.Min(originalDay, DateTime.DaysInMonth(...))`; no `Math.Clamp`.
- [ ] **Step 11:** Run both targets and commit:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapCalendarRenderLogicTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapCalendarRenderLogicTests
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs
git commit -m "feat: add calendar rendering geometry"
```

---

### Task 4: Implement fully owner-drawn BootstrapCalendar shell, public state, defaults, and focus anchoring

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs`

**Interfaces:** consumes Tasks 2-3; produces public calendar API, cached layout/rendering, deterministic `DisplayMonth`, `GetPreferredSize`, and private focus-date lifecycle.

- [ ] **Step 1:** Write failing defaults/metadata/public-surface tests: `DefaultEvent`, zero child controls, one tab stop, mode/safe bounds, default current/clamped `DisplayMonth`, empty state, radius=-1, `AccessibleRole.Table`, exact declared public members.
- [ ] **Step 2:** Write failing preferred-size tests against `BootstrapCalendarRenderLogic.CalculatePreferredSize(...)`; verify constructor/default size is usable and positive without a created handle.
- [ ] **Step 3:** Write failing mode-specific public API/event tests for `SelectedDate`, `SetRange`, `SetSelectedDates`, clear, safe-domain bound changes, invalid call atomicity.
- [ ] **Step 4:** Write failing private-focus seam tests through internal observations/reflection-safe helpers: initial focus=today clamped; non-empty programmatic selection reanchors focus; clear/mode change preserves focus; bound change clamps focus; direct display-month change keeps/clamps day-of-month deterministically.
- [ ] **Step 5:** Implement constructor/state forwarding. Configure `UserPaint | AllPaintingInWmPaint | OptimizedDoubleBuffer | ResizeRedraw | Selectable`, `TabStop=true`, `AccessibleRole.Table`, shared model, `_focusedDate`, theme font/subscription, and default month/size.
- [ ] **Step 6:** Add `DrawToBitmap` smoke tests for Light/Dark, enabled/disabled, Single/Range/Multiple, incomplete range, explicit radius, adjacent days, today, tiny sizes. Avoid pixel-golden tests.
- [ ] **Step 7:** Implement cached layout rebuild only when size/DPI/month/culture-week-start/bounds/radius changes.
- [ ] **Step 8:** Implement drawing order:

```text
1. outer Surface + Border
2. previous/next button state
3. culture month title ("Y")
4. seven abbreviated weekday names
5. committed range interior
6. preview range
7. selected endpoint/multiple surface + Primary outline
8. today indicator
9. day number text
10. keyboard-focus outline
```

Use `TextRenderer.DrawText`; dispose all temporary GDI resources.
- [ ] **Step 9:** Add internal render-state classification tests for selected endpoint, range middle, preview, adjacent month, disabled, today; keep classifier internal.
- [ ] **Step 10:** Run both targets and commit:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapCalendarTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapCalendarTests
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs
git commit -m "feat: add fully rendered BootstrapCalendar"
```

---

### Task 5: Add mouse, internal activation signal, range preview, keyboard, and month interaction

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs`

**Interfaces:** produces one internal `SelectionActivated` path later consumed by the picker.

- [ ] **Step 1:** Add failing mouse tests: Single select once; Range first/second; Multiple toggle; disabled no-op; adjacent-month select + one month-change event. Assert mouse activation updates private focused date to the clicked date.
- [ ] **Step 2:** Add failing internal activation-signal tests. Verify a changed Single activation emits `SelectionChanged=1` and `SelectionActivated(Changed=true,Completed=true)` once; reactivating the same Single date emits no second public change but emits `SelectionActivated(Changed=false,Completed=true)` once.
- [ ] **Step 3:** Implement one `ActivateDate(DateTime)` method and route mouse through it. Focus the calendar, reject non-left/disabled, set focused date, raise public event only when changed, then raise internal activation signal for every valid activation.
- [ ] **Step 4:** Add failing Range activation-signal tests: first endpoint `Completed=false`, second endpoint `Completed=true`, third activation restarts with `Completed=false`. Multiple always returns `Completed=false`.
- [ ] **Step 5:** Add failing Range hover-preview tests proving state/event immutability; mouse leave clears preview; no preview in other/completed states.
- [ ] **Step 6:** Implement `_hotDayIndex`/`_rangePreviewDate` as presentation-only fields.
- [ ] **Step 7:** Add previous/next header hit tests including min/max no-op and focused-day anchoring into the target month.
- [ ] **Step 8:** Implement `ShowPreviousMonth`/`ShowNextMonth` through one private `TrySetDisplayMonth` path shared by mouse/public calls.
- [ ] **Step 9:** Add keyboard tests for arrows, PageUp/PageDown, Home/End under Sunday-first and Monday-first, Enter/Space, min/max clamp, adjacent-month focus. Assert navigation alone never changes selection; Enter/Space uses the same internal activation signal as mouse.
- [ ] **Step 10:** Implement `IsInputKey`/`OnKeyDown` and private focus movement exactly per the focus lifecycle contract.
- [ ] **Step 11:** Add no-duplicate-event tests for same Single activation and non-selection month/focus/hover transitions.
- [ ] **Step 12:** Run and commit:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapCalendarTests|BootstrapCalendarRenderLogicTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapCalendarTests|BootstrapCalendarRenderLogicTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs
git commit -m "feat: add calendar selection interaction"
```

---

### Task 6: Complete theme/font/DPI lifecycle and interactive accessibility

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs`

**Interfaces:** consumes Task-5 activation/month paths so accessibility performs the same actions as mouse/keyboard.

- [ ] **Step 1:** Add theme/font lifecycle tests: Light->Dark->Light preserves selection/month/focus; caller font survives; framework font replacement/disposal correct; static theme subscription removed on dispose.
- [ ] **Step 2:** Implement established `_useThemeFont`, `_settingThemeFont`, `_themeFont`, `_themeSubscribed` ownership pattern.
- [ ] **Step 3:** Add DPI lifecycle tests proving cached layout/preferred size rebuild while logical state persists.
- [ ] **Step 4:** Implement `OnDpiChangedAfterParent`, `OnFontChanged`, `OnSizeChanged`, disposal without double scaling.
- [ ] **Step 5:** Override `CreateAccessibilityInstance()` with an internal `ControlAccessibleObject` exposing 44 logical children: Previous, Next, then 42 days.
- [ ] **Step 6:** Add failing accessibility metadata tests for child count/roles/full-date names/selected/unavailable/focused states and bounds after resize while `Controls.Count` remains zero.
- [ ] **Step 7:** Add failing accessibility interaction tests:
  - Previous/Next `DoDefaultAction()` changes month through the same navigation path and respects disabled bounds.
  - Enabled day `DoDefaultAction()` updates focused date and emits the same `SelectionChanged`/internal `SelectionActivated` semantics as mouse.
  - Disabled day action is a no-op.
  - Parent hit test returns nav/day accessible objects for representative points.
  - `Navigate(Next/Previous)` walks the logical child order without creating controls.
- [ ] **Step 8:** Implement accessible child default actions, hit testing, and sibling navigation without introducing public API.
- [ ] **Step 9:** Run and commit:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapCalendarTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapCalendarTests
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs
git commit -m "feat: harden calendar lifecycle accessibility"
```

---

### Task 7: Generalize internal Dropdown presentation source and implement BootstrapCalendarPicker lifecycle

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarPicker.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs`

**Interfaces:** consumes plan-002 `ShowFrom(BootstrapButton, Control, Point)`, hosted-control Dropdown, calendar/model/internal `SelectionActivated`; produces an internal generic anchored-show overload and the public picker.

#### 7A — Generalize the existing internal opening primitive without public API change

- [ ] **Step 1:** Add failing Dropdown tests for a non-button presentation source. Use a plain focusable `Control` anchor/presentation source plus `BootstrapIconRenderer.CreateDefault()`. One HostedControl-only model must open/close normally, use the anchor's DPI/font, and leave public `Target` unchanged.
- [ ] **Step 2:** Keep the plan-002 overload as the Button-specific guard/convenience path:

```csharp
internal void ShowFrom(
    BootstrapButton presentationSource,
    Control anchor,
    Point location)
{
    ThrowIfDisposed();
    if (presentationSource is null)
    {
        throw new ArgumentNullException(nameof(presentationSource));
    }

    if (!CanOpen(presentationSource))
    {
        return;
    }

    ShowFrom(
        presentationSource,
        presentationSource.IconRenderer,
        anchor,
        location);
}
```

- [ ] **Step 3:** Add the generic internal overload with explicit argument validation, no-op policy, snapshot build, active presentation tracking, and failed-open cleanup:

```csharp
internal void ShowFrom(
    Control presentationSource,
    IIconRenderer iconRenderer,
    Control anchor,
    Point location)
{
    ThrowIfDisposed();

    if (presentationSource is null)
    {
        throw new ArgumentNullException(nameof(presentationSource));
    }
    if (iconRenderer is null)
    {
        throw new ArgumentNullException(nameof(iconRenderer));
    }
    if (anchor is null)
    {
        throw new ArgumentNullException(nameof(anchor));
    }
    if (presentationSource.IsDisposed)
    {
        throw new ObjectDisposedException(nameof(presentationSource));
    }
    if (anchor.IsDisposed)
    {
        throw new ObjectDisposedException(nameof(anchor));
    }
    if (_dropDown.Visible || !presentationSource.Enabled || !anchor.Enabled || _items.Count == 0)
    {
        return;
    }

    ValidateItemTree(_items);
    RebuildNativeItems(presentationSource, iconRenderer);
    _activePresentationSource = presentationSource;
    _activeIconRenderer = iconRenderer;

    try
    {
        _dropDown.Show(anchor, location);
    }
    catch
    {
        _activePresentationSource = null;
        _activeIconRenderer = null;
        ClearNativeItems();
        throw;
    }
}
```

Change private presentation helpers to accept `Control presentationSource` plus explicit `IIconRenderer`, so font/DPI no longer depend on `BootstrapButton`. Keep the existing plan-002 recursive tree validation and exception-safe hosted-control snapshot construction.

- [ ] **Step 4:** Track active presentation for runtime theme refresh:

```csharp
private Control? _activePresentationSource;
private IIconRenderer? _activeIconRenderer;
```

Public `Show()` still calls the existing Button overload, preserving Stage-7 `Target` and Button loading/disabled semantics. `BootstrapSplitButton` continues calling the Button overload from plan 002. Native `Closed` clears both active presentation fields after forwarding the normal close lifecycle. Recursive icon refresh uses `_activeIconRenderer`; DPI/font use `_activePresentationSource`.

- [ ] **Step 5:** Run complete Dropdown tests on both targets before creating the picker:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapDropdownTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapDropdownTests
```

Expected: classic Target path, SplitButton-oriented internal Button path, hosted controls, theme/DPI all remain green.

#### 7B — Implement picker shell/state/summary/accessibility before popup wiring

- [ ] **Step 6:** Write failing picker defaults/public-surface tests: Single, safe-domain bounds, empty state, `DateFormat="d"`, empty placeholder, Validation=None, radius=-1, one tab stop, `AccessibleRole.DropList`, exact declared members, no public/native calendar child.
- [ ] **Step 7:** Write failing programmatic state/event tests matching `BootstrapCalendar` mode/domain rules. Include invalid safe-domain bound attempts and atomicity.
- [ ] **Step 8:** Write formatting tests under fixed culture: empty placeholder, Single, incomplete/complete Range, one Multiple, three Multiple (`first (+2)`), invalid format preserves prior value.
- [ ] **Step 9:** Implement shell/state/summary rendering. Reuse `BootstrapTextBoxRenderLogic.ResolveBorderColor(...)`; derive padding/radius from theme. Use one private `BootstrapIconRenderer.CreateDefault()` for the structural trailing calendar/chevron affordance and for generic internal Dropdown opening. This renderer is not public state and is not disposable.
- [ ] **Step 10:** Add picker accessibility tests for `DropList`, summary/placeholder value, collapsed state, disabled unavailable state, and default action routing to the same trigger method. Expanded-state coverage is completed after popup wiring.

#### 7C — Implement hosted-calendar creation, active reference, focus, synchronization, and completion policy

- [ ] **Step 11:** Add popup-factory synchronization tests. On first open with `_lastDisplayMonth=null`, the fresh hosted calendar receives mode/bounds/logical selection and display month chosen by `SelectedDate -> RangeStart -> first Multiple -> today`, clamped to bounds. After user month navigation and close, the next fresh instance starts from retained `_lastDisplayMonth`.
- [ ] **Step 12:** Implement a single HostedControl factory with exception-safe local ownership. Create a local `BootstrapCalendar`, configure mode/bounds/state/display month, set its DPI-aware size from `BootstrapCalendarRenderLogic.CalculatePreferredSize(...)`, subscribe internal `SelectionActivated` and `DisplayMonthChanged`, assign `_activeCalendar` only after all setup succeeds, and return it. If any setup step throws, unsubscribe anything already attached, dispose the local calendar, and rethrow.
- [ ] **Step 13:** Open with the generic shared Dropdown primitive and clean up a failed-open active reference:

```csharp
public void ShowDropDown()
{
    ThrowIfDisposed();
    if (_isDropDownOpen || !Enabled)
    {
        return;
    }

    try
    {
        _dropdown.ShowFrom(
            this,
            _iconRenderer,
            this,
            new Point(0, Height));
    }
    catch
    {
        DetachActiveCalendar();
        throw;
    }
}
```

`DetachActiveCalendar()` only unsubscribes and clears the non-owning reference; Dropdown/ToolStrip remains responsible for disposal of a successfully hosted control.

- [ ] **Step 14:** Add lifecycle/focus tests. `Opened` must set `_isDropDownOpen`, expose accessibility `Expanded`, forward one picker `Opened`, and leave `_activeCalendar.Focused == true` after event pumping. `Closed` must unsubscribe/clear `_activeCalendar`, set collapsed state, and forward one picker `Closed`.
- [ ] **Step 15:** Implement `OnDropDownOpened`/`OnDropDownClosed` in that order. Do not retain a closed hosted-control reference even if the native snapshot is still owned internally by Dropdown.
- [ ] **Step 16:** Add popup completion-policy tests using the **internal activation signal**, not hosted public `SelectionChanged`:
  - Single changed date: picker updates once and closes.
  - Single same date: picker public event count stays unchanged but popup still closes because `Completed=true`.
  - Range first endpoint: picker updates, remains open.
  - Range second endpoint: picker updates, closes.
  - Multiple: picker toggles/updates, remains open.
- [ ] **Step 17:** Implement hosted `SelectionActivated` handler. Copy the complete hosted logical snapshot into picker model; raise picker `SelectionChanged` only when event args report `Changed=true`; apply close policy from `Completed` and mode. Never infer completion from whether `SelectionChanged` fired.
- [ ] **Step 18:** Add programmatic-while-open synchronization tests. For `SelectionMode`, bounds, `SelectedDate`, `SetRange`, `SetSelectedDates`, and `ClearSelection`, assert the active hosted calendar immediately matches picker state, popup remains open, and no internal user-activation close occurs. Bound changes also clamp `_lastDisplayMonth`.
- [ ] **Step 19:** Implement `SynchronizeActiveCalendar()` under `_synchronizingCalendar`. Programmatic synchronization may cause hosted public property/display events but must never invoke picker user-completion policy; only `SelectionActivated` drives that policy. `DisplayMonthChanged` may update the retained logical month during synchronization after bounds clamping, but it must not recurse into state mutation.
- [ ] **Step 20:** Add trigger tests: click and Enter/Space/F4/Alt+Down open; a second trigger while `_isDropDownOpen` closes; disabled picker does not open. Native Escape/outside remains Dropdown-owned and is verified manually.
- [ ] **Step 21:** Complete picker accessibility tests: `Expanded`/`Collapsed`, current summary value after selection, `DoDefaultAction` opens/closes, disabled default action is no-op.
- [ ] **Step 22:** Run picker + Dropdown tests both targets:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapCalendarPickerTests|BootstrapDropdownTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapCalendarPickerTests|BootstrapDropdownTests"
```

- [ ] **Step 23:** Commit the internal Dropdown refactor and picker together so no intermediate commit contains a dead generic path:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarPicker.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs
git commit -m "feat: add custom calendar picker"
```

---

### Task 8: Add Advanced Inputs demo coverage and manual matrix

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs`

- [ ] **Step 1:** Add failing structure tests requiring these existing-page scenarios:

```text
Custom Calendar — Range
Calendar Picker — Single
Calendar Picker — Range
Calendar Picker — Multiple
```

- [ ] **Step 2:** Add a standalone Range calendar seeded to deterministic sample month/range plus a label updated from `SelectionChanged`.
- [ ] **Step 3:** Add Single/Range/Multiple pickers with 2025-2030 demo bounds and `DateFormat="yyyy-MM-dd"`; demo may set descriptive English placeholders, library defaults remain empty.
- [ ] **Step 4:** Include invalid and disabled visual states without adding another component type.
- [ ] **Step 5:** Run demo tests both targets:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~AdvancedInputsDemoFormTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~AdvancedInputsDemoFormTests
```

- [ ] **Step 6:** Manual real-Windows matrix:
  - Light/Dark while standalone/popup visible.
  - 100/125/150/175/200% scaling.
  - Sunday-first and Monday-first regional settings.
  - mouse Single/Range/Multiple.
  - same-date Single reactivation closes the picker with no duplicate public change event.
  - range hover preview remains uncommitted.
  - adjacent-month selection changes month and keyboard focus follows the activated day.
  - arrows/PageUp/PageDown/Home/End + Enter/Space after popup open; verify keys operate on the hosted calendar rather than moving ToolStrip selection.
  - F4/Alt+Down/Enter/Space opens picker; second trigger closes.
  - Escape/outside closes via native Dropdown.
  - Single/completed Range auto-close; Multiple stays open.
  - programmatic state mutation while popup is open immediately updates visible calendar without closing it.
  - disabled dates do not activate.
  - screen reader can invoke Previous/Next/day default actions and picker Open/Close default action.
  - repeated open/close has no orphan popup handles, stale `_activeCalendar`, or disposed-host references.
- [ ] **Step 7:** Commit:

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs
git commit -m "demo: showcase custom calendar selection"
```

---

### Task 9: Document contract and approve frozen public API

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/COMPATIBILITY.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

- [ ] **Step 1:** Document `BootstrapCalendar`/`BootstrapCalendarPicker`, modes, safe `DateTimePicker` date domain, date-only normalization, deterministic default `DisplayMonth`, preferred size, private keyboard-focus model, keyboard behavior, owner-drawn rendering, and unchanged native DatePicker.
- [ ] **Step 2:** Compatibility docs: framework-owned calendar visuals; native ToolStrip popup placement/dismissal; hosted-control focus/key prerequisite; CurrentCulture/FirstDayOfWeek; both TFMs.
- [ ] **Step 3:** Testing docs: 42-cell projection including safe-domain boundaries, preferred-size formula, leap year, culture week-start, range preview, same-date completion signal, multiple toggles, keyboard focus lifecycle, interactive accessibility, active hosted-control synchronization, Light/Dark/DPI.
- [ ] **Step 4:** README/package/changelog concise distinction: native DatePicker vs custom Calendar/CalendarPicker.
- [ ] **Step 5:** Run public API baseline and expect intentional failure:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~Phase16PublicApiBaselineTests
```

- [ ] **Step 6:** Review exports: only public enum + two controls/members defined above are new; no selection model, activation args/event, focus fields, layout types, Dropdown overload, ToolStrip/host types, or active-calendar references escape.
- [ ] **Step 7:** Update approved fingerprint + `docs/PUBLIC_API_BASELINE.md` together.
- [ ] **Step 8:** Rerun both targets and commit:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~Phase16PublicApiBaselineTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~Phase16PublicApiBaselineTests
git add docs/COMPONENTS.md docs/TESTING.md docs/COMPATIBILITY.md README.md docs/PACKAGE_README.md CHANGELOG.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs docs/PUBLIC_API_BASELINE.md
git commit -m "docs: document custom calendar public contract"
```

---

### Task 10: Final cross-control regression and resource verification

**Files:** no expected product edit unless a regression is reproduced and fixed with a test.

- [ ] **Step 1:** Build both:

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
```

- [ ] **Step 2:** Focused regression:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapCalendar|BootstrapCalendarPicker|BootstrapDatePicker|BootstrapDropdown|BootstrapSplitButton|AdvancedInputsDemoForm"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapCalendar|BootstrapCalendarPicker|BootstrapDatePicker|BootstrapDropdown|BootstrapSplitButton|AdvancedInputsDemoForm"
```

- [ ] **Step 3:** Full suite:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
```

- [ ] **Step 4:** Add/run repeated create/open/select/programmatic-sync/close/dispose STA smoke loop. Assert no `ObjectDisposedException`, duplicate events, stale `_activeCalendar`, or retained hosted-calendar reference after `Closed`. Keep process GDI/USER handle trend as manual Windows verification, not a brittle exact automated count.
- [ ] **Step 5:** Run Task-8 real-desktop matrix plus screen-reader/accessibility-tree inspection and no-focus-trap verification.
- [ ] **Step 6:** Scope review:

```text
BootstrapDatePicker public/native contract unchanged
BootstrapCalendar contains no MonthCalendar/native DateTimePicker
safe supported date domain enforced atomically
42-cell projection safe at minimum/maximum supported months
Dropdown generic opening overload remains internal
existing Button ShowFrom overload still services SplitButton unchanged
no fake hidden BootstrapButton used by CalendarPicker
same-date Single completion uses internal activation signal, not duplicate public event
picker holds active calendar only while popup is visible and cleans failed-open references
programmatic open-popup synchronization is guarded and non-closing
keyboard focus lifecycle is deterministic
calendar/picker accessibility default actions are interactive
preferred/default sizing is deterministic and DPI-derived
no second popup/placement engine
no new external package
no duplicated theme/DPI/selection engine
no public ToolStrip/host/internal model types
all public/protected additions XML-documented
```

- [ ] **Step 7:** If fixes were required, rerun both full suites.
- [ ] **Step 8:** Verify repository hygiene:

```powershell
git status --short
git diff --check
```

Do not create an empty final commit when no fixes are needed.

---

## Definition of Done

- `BootstrapCalendar` renders the complete calendar surface itself with zero per-day child controls and no native calendar implementation underneath.
- Single, Range, and Multiple share one tested selection state machine.
- `MinDate`/`MaxDate` and selections are atomically restricted to the `DateTimePicker.MinimumDateTime.Date` .. `DateTimePicker.MaximumDateTime.Date` safe domain.
- Six-week projection is verified at minimum/maximum supported months without `DateTime` underflow/overflow.
- Range first/second endpoint and hover-preview semantics are deterministic.
- Same-date Single activation produces internal `Completed=true` while public `SelectionChanged` remains change-only; picker therefore closes without a duplicate event.
- Multiple selection is date-normalized, deduplicated, sorted, and mouse/keyboard/accessibility toggleable.
- Culture-aware month/weekday layout works for differing first-day-of-week settings and leap years.
- Private keyboard-focus state has deterministic initialization, mouse/programmatic anchoring, bound clamping, month-navigation behavior, and accessibility reporting.
- Mouse/keyboard focus navigation does not mutate selection until activation.
- Min/max disabled dates, month clamping, adjacent-month selection, and deterministic default `DisplayMonth` are correct.
- `GetPreferredSize` and popup hosted-calendar sizing use one tested DPI-aware metric formula.
- Theme/font/DPI lifecycle is leak-safe and functional in Light/Dark at 100/125/150/175/200% Windows scaling.
- Owner-drawn calendar exposes interactive accessible nav/day children with default actions, hit testing, and sibling navigation while using no per-day WinForms child controls.
- `BootstrapCalendarPicker` accessibility exposes DropList summary plus expanded/collapsed and Open/Close default-action semantics.
- `BootstrapCalendarPicker` uses hosted-control `BootstrapDropdown` and the shared internal generic anchored-show primitive; it does not create a new popup engine or fake presentation button.
- Picker retains `_activeCalendar` only while visible, focuses it on open, cleans it on failed open/close, and synchronizes programmatic state while open without triggering user-completion policy.
- Existing `BootstrapDropdown.Show()`, plan-002 Button `ShowFrom(...)`, `BootstrapSplitButton`, and native-backed `BootstrapDatePicker` behavior remain green.
- Single and completed Range picker sessions auto-close; incomplete Range and Multiple follow the specified open policy.
- Demo, docs, compatibility/testing guidance, changelog/package docs, and public API baseline are updated.
- Both `net48` and `net8.0-windows` builds and full test suites pass.