# Custom Calendar Rendering, Date Range, and Multi-Date Selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a fully framework-rendered Bootstrap-inspired calendar plus date-range and multi-date selection without breaking the existing native-backed `BootstrapDatePicker`, then expose the same calendar through an advanced popup picker that reuses the repository's hosted-control Dropdown infrastructure.

**Architecture:** Preserve `BootstrapDatePicker` exactly as the native single-date/editor option. Add a new owner-drawn `BootstrapCalendar : Control` with no per-day child controls; it owns month/header/week/day rendering, hit testing, keyboard focus, theme/DPI behavior, and delegates all selection rules to one internal `BootstrapCalendarSelectionModel` shared by `Single`, `Range`, and `Multiple` modes. Add `BootstrapCalendarPicker : Control` as a compact summary/trigger surface that hosts a fresh `BootstrapCalendar` inside `BootstrapDropdown` using the hosted-control support from `docs/plans/20260829-002-dropdown-submenus-hosted-controls-split-button.md`. Widen only the **internal** Dropdown opening primitive so non-button controls can supply a presentation `Control` plus `IIconRenderer`; classic Dropdown and SplitButton public APIs remain unchanged. Native ToolStrip infrastructure continues to own popup focus, working-area placement, Escape/outside-click dismissal, and popup lifetime.

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
- Default bounds are `DateTimePicker.MinimumDateTime.Date` and `DateTimePicker.MaximumDateTime.Date`, matching the broad safe domain of the existing native picker.
- `MinDate <= MaxDate` is required. Invalid bound assignments throw `ArgumentOutOfRangeException` before mutation.
- Programmatic selection outside `[MinDate, MaxDate]` throws `ArgumentOutOfRangeException`; out-of-range UI cells render disabled and never activate.
- Bound changes may invalidate selection: Single clears if selected date is outside; incomplete/complete Range clears if either stored endpoint is outside; Multiple removes only newly invalid dates. Raise `SelectionChanged` once if effective public selection changes.
- `DisplayMonth` is normalized to the first day of its month and clamped to a month intersecting the allowed bounds. `DisplayMonthChanged` fires only on effective change.
- Culture is read from `CultureInfo.CurrentCulture` at layout/render/format time. Week order begins at `CurrentCulture.DateTimeFormat.FirstDayOfWeek`. No separate public Culture property is added.
- The visual grid is always 6 rows x 7 columns = 42 date cells. Leading/trailing adjacent-month days remain visible, muted, and selectable when in range.
- Selecting an enabled adjacent-month day applies selection first, then moves `DisplayMonth` to that date's month.
- `Single` stores zero or one `SelectedDate`.
- `Range` stores `RangeStart` and optional `RangeEnd`. First activation starts/restarts an incomplete range. Second activation completes it and normalizes endpoint order. Third activation after completion starts a new incomplete range.
- Range hover preview is presentation-only and never mutates range state or raises `SelectionChanged`.
- `Multiple` toggles each activated date. `SelectedDates` is a sorted, deduplicated ascending snapshot.
- Switching `SelectionMode` clears prior-mode selection. Raise `SelectionChanged` once if anything was selected; never reinterpret state across modes implicitly.
- Keyboard focus date is separate from selection. Left/Right move +/-1 day; Up/Down +/-7 days; PageUp/PageDown +/-1 month; Home/End go to culture-week start/end; Enter/Space activates. Crossing a month boundary updates `DisplayMonth`.
- Month arithmetic clamps day safely (for example January 31 -> February 28/29) and must not use `Math.Clamp`, unavailable on `net48`.
- Navigation buttons are disabled/no-op when the adjacent month does not intersect allowed bounds.
- Theme colors/metrics come from `BootstrapThemeManager.CurrentTheme`; do not add calendar-only hard-coded palette constants where existing `Surface`, `SurfaceSecondary`, `Border`, `Text`, `MutedText`, `Disabled`, `Focus`, `Hover`, `Active`, and `Primary` suffice.
- Selected endpoint/multiple cells use `Active` surface plus `Primary` outline; committed range interiors use `SurfaceSecondary`; preview range uses `Hover`; focused cell adds the `Focus` outline. Text uses `Text`/`MutedText` according to enabled/current-month state. This avoids inventing a new contrast engine.
- Derive geometry from `BootstrapThemeMetrics` and `DpiScaler`; do not extend the theme constructor only for calendar sizing.
- 96-DPI baseline uses `SpacingSM` outer padding, `ControlHeight` header, `ControlHeightSmall` weekday row, `ControlHeight` day rows, `SpacingXS` cell gap, and theme radius for `BorderRadius = -1`.
- Cache resolved layout/cell geometry when size, DPI, display month, culture, bounds, or border radius changes. Do not allocate a new 42-cell list on every `OnPaint`.
- Dispose temporary GDI objects deterministically. Do not retain persistent `Pen`, `Brush`, `GraphicsPath`, `Bitmap`, or `Region` unless measurements prove it necessary.
- Theme subscription occurs at most once and is removed on disposal. Dispose only framework-created fonts; never dispose caller-assigned fonts.
- Designer construction must work without app bootstrap, DI, a running message loop, assigned parent, or initialized service locator.
- `BootstrapCalendarPicker` reuses hosted-control `BootstrapDropdown`. Do not create a custom top-level `Form`, second `ToolStripDropDown`, global hook, or second placement/focus/dismissal engine.
- Plan `20260829-002` introduces `BootstrapDropdown.ShowFrom(BootstrapButton presentationSource, Control anchor, Point location)`. This plan keeps that overload for compatibility with `BootstrapSplitButton` and adds an **internal-only** generic overload: `ShowFrom(Control presentationSource, IIconRenderer iconRenderer, Control anchor, Point location)`. The existing button overload delegates to the generic overload after preserving Button-specific open guards such as loading state.
- The generic Dropdown opening overload uses `presentationSource.DeviceDpi`/control state and the explicit `IIconRenderer`; it never requires an invisible fake `BootstrapButton` merely to open a calendar popup.
- The generic internal overload does not change `BootstrapDropdown.Target`, public `Show()`, public `Close()`, or `BootstrapSplitButton` public behavior.
- The popup calendar is factory-created for each effective Dropdown snapshot because `ToolStripControlHost` owns hosted-control disposal. Picker owns logical selection/display state only, never a reusable popup-control reference.
- Single-mode picker selection commits and closes immediately. Range stays open after first endpoint and closes after second. Multiple remains open after toggles and closes only through Escape, outside click, trigger toggle, or `CloseDropDown()`.
- Closing with an incomplete range preserves its `RangeStart`; there is no hidden rollback buffer.
- Picker text is summary-only. No typed date parser/editor is added; `BootstrapDatePicker` remains the typed/native option.
- `DateFormat` defaults to `"d"`; setter validates before mutation by formatting a known in-range date with `CultureInfo.CurrentCulture`, preserving normal `FormatException` for invalid formats.
- Empty selection renders `PlaceholderText`; default is `string.Empty` so library code does not embed English UI copy.
- Range summary uses the same format for both endpoints. Incomplete: `<start> – …`; complete: `<start> – <end>`.
- Multiple summary renders one formatted date when count=1; count>=2 renders `<first> (+N)` where N is remaining count.
- Picker validation priority matches established inputs: disabled -> Valid/Invalid -> focus -> neutral.
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
- `DisplayMonth` normalizes to day 1 and clamps to a month intersecting bounds.
- `BorderRadius=-1` means current theme radius; nonnegative is explicit logical radius; `< -1` throws before mutation.

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

Picker mode-specific selection semantics exactly match `BootstrapCalendar`. Do not add ambiguous aliases such as `Value`, `StartDate`, `EndDate`, `Dates`, `SelectedRange`, or `IsOpen`.

### Deliberately not added

- No changes to `BootstrapDatePicker`.
- No time-of-day selection or `DateTimeOffset` API.
- No consumer per-cell paint/template callback yet; “fully custom rendering” means framework-owned rendering rather than OS-owned rendering.
- No week numbers, year/decade zoom, multi-month view, drag range, blackout predicate/collection, holiday/event badges, recurrence, or month animation.
- No typed parser/editor in `BootstrapCalendarPicker`.
- No public native ToolStrip/popup/host handles or hosted calendar reference.
- No second popup-placement engine or top-level popup Form.

---

## Internal Selection Contract

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

The model raises no events. Its bool results report effective selection change. `Activate.Completed` is true for Single, false after first Range endpoint, true after second Range endpoint, and false for Multiple. A same-date Single activation may return `Changed=false, Completed=true`, allowing the picker to close without emitting a duplicate event.

---

## Rendering/Layout Contract

Create pure/internal types in `BootstrapCalendarRenderLogic.cs`:

```csharp
internal readonly struct BootstrapCalendarMetrics
{
    public int OuterPadding { get; }
    public int CellGap { get; }
    public int HeaderHeight { get; }
    public int WeekdayHeight { get; }
    public int DayRowHeight { get; }
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

The helper returns 42 logical cells even for tiny/zero client sizes. Rectangle dimensions never become negative. Culture-localized strings are resolved during render, not cached in layout objects.

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

### Task 1: Characterize hosted-control popup interaction

**Files:**
- Create/Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs`

**Interfaces:** consumes plan-002 hosted-control Dropdown behavior; produces a test guard proving a calendar-like hosted control can interact without auto-closing.

- [ ] **Step 1:** Create an STA fixture with a visible/minimized-offscreen `Form`, a `BootstrapButton` Dropdown presentation source, and one HostedControl factory returning a focusable panel/button. Open, focus/click the hosted button, pump `Application.DoEvents()`, and assert `Opened=1`, `Closed=0`, hosted click=1.
- [ ] **Step 2:** Characterize explicit `Close()` separately and assert `Closed` exactly once. Keep real Escape/outside-click verification manual; do not invoke private native popup messages by reflection.
- [ ] **Step 3:** Run on both targets:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapCalendarPickerTests&Name~HostedControl"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapCalendarPickerTests&Name~HostedControl"
```

- [ ] **Step 4:** If hosted child interaction closes the root popup, fix plan-002 hosted-control semantics first; do not create a custom calendar popup.
- [ ] **Step 5:** Commit:

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs
git commit -m "test: characterize hosted calendar popup behavior"
```

---

### Task 2: Implement shared Single/Range/Multiple selection state

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionMode.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionModel.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarSelectionModelTests.cs`

- [ ] **Step 1:** Write failing construction/validation tests: default Single/empty, `.Date` normalization, native-safe bounds, min>max rejection, undefined mode rejection.
- [ ] **Step 2:** Run focused tests and verify compile failure because types do not exist.
- [ ] **Step 3:** Implement enum/model skeleton, `SortedSet<DateTime>` for Multiple, immutable snapshots, validation helpers.
- [ ] **Step 4:** Add failing Single tests for first activation, same-date confirmation (`Changed=false, Completed=true`), different date, null clear, out-of-range atomic rejection.
- [ ] **Step 5:** Implement Single minimally.
- [ ] **Step 6:** Add failing Range tests for first endpoint, reverse second endpoint normalization, completed third-click restart, `SetRange(start,null)`, clear, null/end rejection, out-of-range atomic rejection.
- [ ] **Step 7:** Implement Range.
- [ ] **Step 8:** Add failing Multiple tests for toggle, sort, dedupe, atomic invalid input rejection, `Completed=false`.
- [ ] **Step 9:** Implement Multiple.
- [ ] **Step 10:** Add bound/mode transition tests: clear invalid Single/Range, filter Multiple, preserve compatible state, clear on mode switch with exact changed flag.
- [ ] **Step 11:** Run both targets:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapCalendarSelectionModelTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapCalendarSelectionModelTests
```

- [ ] **Step 12:** Commit:

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionMode.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionModel.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarSelectionModelTests.cs
git commit -m "feat: add calendar selection state model"
```

---

### Task 3: Implement pure six-week projection, DPI layout, hit testing, and safe date navigation

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs`

- [ ] **Step 1:** Write failing metric tests at DPI 96/120/144/168/192, theme/default vs explicit radius, invalid DPI/radius.
- [ ] **Step 2:** Write failing month projection tests using explicit first-day-of-week: September 2026 Sunday/Monday first, leap February 2028, Dec/Jan boundaries, exactly 42 consecutive dates.
- [ ] **Step 3:** Implement metrics/projection only enough to pass.
- [ ] **Step 4:** Add normal/tiny/zero layout tests: contained nonnegative rectangles, seven weekday columns, six day rows, deterministic remainder-pixel distribution.
- [ ] **Step 5:** Implement integer layout so accumulated rounding never leaves unexplained right/bottom gaps.
- [ ] **Step 6:** Add/implement hit-test tests for representative cells and header/gap/outside points (`-1`).
- [ ] **Step 7:** Add navigation tests:

```text
2025-01-31 +1 month = 2025-02-28
2028-01-31 +1 month = 2028-02-29
2026-03-31 -1 month = 2026-02-28
Sunday-first Home/End = Sunday/Saturday
Monday-first Home/End = Monday/Sunday
```

- [ ] **Step 8:** Implement month navigation with `Math.Min(originalDay, DateTime.DaysInMonth(...))`; no `Math.Clamp`.
- [ ] **Step 9:** Run both targets and commit:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapCalendarRenderLogicTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapCalendarRenderLogicTests
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs
git commit -m "feat: add calendar rendering geometry"
```

---

### Task 4: Implement the fully owner-drawn BootstrapCalendar shell

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs`

- [ ] **Step 1:** Write failing defaults/metadata/public-surface tests: `DefaultEvent`, zero child controls, one tab stop, mode/bounds/display month/empty state/radius defaults, exact declared members.
- [ ] **Step 2:** Write failing mode-specific public API/event tests for `SelectedDate`, `SetRange`, `SetSelectedDates`, clear, bound changes, invalid call atomicity.
- [ ] **Step 3:** Implement constructor/state forwarding. Configure `UserPaint | AllPaintingInWmPaint | OptimizedDoubleBuffer | ResizeRedraw | Selectable`, `TabStop=true`, `AccessibleRole.Table`, shared model, theme font/subscription.
- [ ] **Step 4:** Add `DrawToBitmap` smoke tests for Light/Dark, enabled/disabled, Single/Range/Multiple, incomplete range, explicit radius, adjacent days, today, tiny sizes. Avoid pixel-golden tests.
- [ ] **Step 5:** Implement cached layout rebuild only when size/DPI/month/culture/bounds/radius changes.
- [ ] **Step 6:** Implement drawing order:

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
- [ ] **Step 7:** Add internal render-state classification tests for selected endpoint, range middle, preview, adjacent month, disabled, today; keep classifier internal.
- [ ] **Step 8:** Run both targets and commit:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapCalendarTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapCalendarTests
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs
git commit -m "feat: add fully rendered BootstrapCalendar"
```

---

### Task 5: Add mouse, range-preview, keyboard, and month interaction

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs`

- [ ] **Step 1:** Add failing mouse tests: Single select once; Range first/second; Multiple toggle; disabled no-op; adjacent-month select + one month-change event.
- [ ] **Step 2:** Implement `OnMouseDown` through `HitTestDay` and one model `Activate` path; focus control, reject non-left/disabled, emit event only on `Changed`.
- [ ] **Step 3:** Add failing Range hover-preview tests proving state/event immutability; mouse leave clears preview; no preview in other/completed states.
- [ ] **Step 4:** Implement `_hotDayIndex`/`_rangePreviewDate` as presentation-only fields.
- [ ] **Step 5:** Add previous/next header hit tests including min/max no-op.
- [ ] **Step 6:** Implement `ShowPreviousMonth`/`ShowNextMonth` through one private `TrySetDisplayMonth` path shared by mouse/public calls.
- [ ] **Step 7:** Add keyboard tests for arrows, PageUp/PageDown, Home/End under Sunday-first and Monday-first, Enter/Space, min/max clamp, adjacent-month focus.
- [ ] **Step 8:** Implement `IsInputKey`/`OnKeyDown`; focus movement alone never changes public selection.
- [ ] **Step 9:** Add no-duplicate-event tests for same Single activation and non-selection month/focus/hover transitions.
- [ ] **Step 10:** Run and commit:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapCalendarTests|BootstrapCalendarRenderLogicTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapCalendarTests|BootstrapCalendarRenderLogicTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs
git commit -m "feat: add calendar selection interaction"
```

---

### Task 6: Complete theme/font/DPI lifecycle and accessibility

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs`

- [ ] **Step 1:** Add theme/font lifecycle tests: Light->Dark->Light preserves selection/month; caller font survives; framework font replacement/disposal correct; static theme subscription removed on dispose.
- [ ] **Step 2:** Implement established `_useThemeFont`, `_settingThemeFont`, `_themeFont`, `_themeSubscribed` ownership pattern.
- [ ] **Step 3:** Add DPI lifecycle tests proving cached layout rebuild while logical state persists.
- [ ] **Step 4:** Implement `OnDpiChangedAfterParent`, `OnFontChanged`, `OnSizeChanged`, disposal without double scaling.
- [ ] **Step 5:** Override `CreateAccessibilityInstance()` with an internal `ControlAccessibleObject` exposing 44 logical children: Previous, Next, then 42 days. Navigation = `AccessibleRole.PushButton`; days = `AccessibleRole.Cell`; full culture date names; disabled = `Unavailable`; selected/range = `Selected`; keyboard focus = `Focused`.
- [ ] **Step 6:** Add accessibility tests for child count/roles/names/states/bounds after resize while `Controls.Count` remains zero.
- [ ] **Step 7:** Run and commit:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapCalendarTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapCalendarTests
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs
git commit -m "feat: harden calendar theme dpi accessibility"
```

---

### Task 7: Generalize internal Dropdown presentation source and implement BootstrapCalendarPicker

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarPicker.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs`

**Interfaces:** consumes plan-002 `ShowFrom(BootstrapButton, Control, Point)`, hosted-control Dropdown, calendar/model; produces an internal generic anchored-show overload plus the public picker.

#### 7A — Generalize the existing internal opening primitive without public API change

- [ ] **Step 1:** Add failing Dropdown tests for a non-button presentation source. Use a plain focusable `Control` anchor/presentation source plus `BootstrapIconRenderer.CreateDefault()`. One HostedControl-only model must open/close normally, use the anchor's DPI, and leave public `Target` unchanged.
- [ ] **Step 2:** Keep the plan-002 overload exactly as an internal convenience path:

```csharp
internal void ShowFrom(
    BootstrapButton presentationSource,
    Control anchor,
    Point location)
{
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

- [ ] **Step 3:** Add the generic internal overload:

```csharp
internal void ShowFrom(
    Control presentationSource,
    IIconRenderer iconRenderer,
    Control anchor,
    Point location)
{
    // ThrowIfDisposed.
    // Reject null/disposed presentationSource, iconRenderer, or anchor.
    // Require presentationSource/anchor enabled and usable.
    // Validate item tree before snapshot mutation.
    // Build native snapshot using iconRenderer.
    // Resolve DPI from presentationSource (fall back to DpiScaler.DefaultDpi).
    // Record active presentation source + renderer while visible.
    // _dropDown.Show(anchor, location).
}
```

Do not expose this overload publicly.

- [ ] **Step 4:** Refactor active presentation tracking for theme refresh:

```csharp
private Control? _activePresentationSource;
private IIconRenderer? _activeIconRenderer;
```

Public `Show()` still calls the existing Button overload, preserving native Stage-7 `Target` and Button loading/disabled semantics. `BootstrapSplitButton` continues calling the existing Button overload from plan 002; no SplitButton public or behavioral change is required. Recursive icon refresh uses `_activeIconRenderer`; DPI uses `_activePresentationSource`.

- [ ] **Step 5:** Run complete Dropdown tests on both targets before creating the picker:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~BootstrapDropdownTests
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter FullyQualifiedName~BootstrapDropdownTests
```

Expected: classic Target path, SplitButton-oriented internal Button path, hosted controls, theme/DPI all remain green.

#### 7B — Implement the picker shell/state/hosted calendar

- [ ] **Step 6:** Write failing picker defaults/public-surface tests: Single, safe bounds, empty state, `DateFormat="d"`, empty placeholder, Validation=None, radius=-1, one tab stop, exact declared members, no public/native calendar child.
- [ ] **Step 7:** Write failing programmatic state/event tests matching `BootstrapCalendar` mode rules.
- [ ] **Step 8:** Write formatting tests under fixed culture: empty placeholder, Single, incomplete/complete Range, one Multiple, three Multiple (`first (+2)`), invalid format preserves prior value.
- [ ] **Step 9:** Implement shell/state/summary rendering. Reuse `BootstrapTextBoxRenderLogic.ResolveBorderColor(...)`; derive padding/radius from theme. Use one private `BootstrapIconRenderer.CreateDefault()` for the structural trailing calendar/chevron affordance and for the generic internal Dropdown opening call. This renderer is not public state and is not disposable.
- [ ] **Step 10:** Add popup-factory synchronization tests. `ShowDropDown()` must create a fresh hosted `BootstrapCalendar` receiving mode, bounds, logical selection, and initial display month = retained month or selected/range-start/first-multiple/today fallback clamped to bounds.
- [ ] **Step 11:** Construct one internal Dropdown with exactly one `HostedControl` model item. `HostedControlFactory` creates a **fresh** calendar, configures DPI-aware preferred size, synchronizes state, subscribes to Selection/DisplayMonth events, and returns it. Never cache the control beyond current snapshot.
- [ ] **Step 12:** Open with the generic shared Dropdown primitive, never with a fake hidden Button:

```csharp
_dropdown.ShowFrom(
    this,
    _iconRenderer,
    this,
    new Point(0, Height));
```

This aligns below the complete picker and resolves popup DPI from the real picker control.

- [ ] **Step 13:** Add/implement popup commit policy tests:
  - Single: update once, close.
  - Range first: update start, remain open.
  - Range second: update range, close.
  - Multiple: toggle/update, remain open.
  - same Single date: no duplicate event, still close.
- [ ] **Step 14:** Use one guarded synchronization path. On hosted calendar selection, copy complete logical state through model APIs, raise picker `SelectionChanged` once when changed, apply close policy. A private `_synchronizingCalendar` flag prevents feedback loops only.
- [ ] **Step 15:** Retain only normalized `_lastDisplayMonth`, updated from hosted `DisplayMonthChanged`; never retain the hosted control. Clamp when bounds change.
- [ ] **Step 16:** Add trigger/lifecycle tests: click and Enter/Space/F4/Alt+Down open; second trigger closes; disabled no-open; `Opened`/`Closed` forward once with picker sender; disposal closes/disposes Dropdown and unsubscribes theme. Native Escape/outside remains Dropdown-owned.
- [ ] **Step 17:** Run picker + Dropdown tests both targets:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapCalendarPickerTests|BootstrapDropdownTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapCalendarPickerTests|BootstrapDropdownTests"
```

- [ ] **Step 18:** Commit the internal Dropdown refactor and picker together so no intermediate commit contains a dead generic path:

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
  - range hover preview remains uncommitted.
  - adjacent-month selection changes month.
  - arrows/PageUp/PageDown/Home/End + Enter/Space.
  - F4/Alt+Down/Enter/Space opens picker.
  - Escape/outside closes via native Dropdown.
  - Single/completed Range auto-close; Multiple stays open.
  - disabled dates do not activate.
  - repeated open/close has no orphan popup handles/disposed-host references.
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

- [ ] **Step 1:** Document `BootstrapCalendar`/`BootstrapCalendarPicker`, modes, date-only normalization, keyboard model, owner-drawn rendering, and unchanged native DatePicker.
- [ ] **Step 2:** Compatibility docs: framework-owned calendar visuals; native ToolStrip popup placement/focus/dismissal; CurrentCulture/FirstDayOfWeek; both TFMs.
- [ ] **Step 3:** Testing docs: 42-cell projection, leap year, culture week-start, range preview, multiple toggles, keyboard, accessibility, hosted ownership, Light/Dark/DPI.
- [ ] **Step 4:** README/package/changelog concise distinction: native DatePicker vs custom Calendar/CalendarPicker.
- [ ] **Step 5:** Run public API baseline and expect intentional failure:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~Phase16PublicApiBaselineTests
```

- [ ] **Step 6:** Review exports: only public enum + two controls/members defined above are new; no internal model/layout/Dropdown overload/ToolStrip/host types escape.
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

- [ ] **Step 4:** Add/run repeated create/open/select/close/dispose STA smoke loop. Assert no `ObjectDisposedException`, duplicate events, or retained hosted-calendar reference. Keep process GDI/USER handle trend as manual Windows verification, not a brittle exact automated count.
- [ ] **Step 5:** Run Task-8 real-desktop matrix plus screen-reader/accessibility-tree inspection and no-focus-trap verification.
- [ ] **Step 6:** Scope review:

```text
BootstrapDatePicker public/native contract unchanged
BootstrapCalendar contains no MonthCalendar/native DateTimePicker
Dropdown generic opening overload remains internal
existing Button ShowFrom overload still services SplitButton unchanged
no fake hidden BootstrapButton used by CalendarPicker
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
- Range first/second endpoint and hover-preview semantics are deterministic.
- Multiple selection is date-normalized, deduplicated, sorted, and mouse/keyboard toggleable.
- Culture-aware month/weekday layout works for differing first-day-of-week settings and leap years.
- Mouse/keyboard focus navigation does not mutate selection until activation.
- Min/max disabled dates, month clamping, and adjacent-month selection are correct.
- Theme/font/DPI lifecycle is leak-safe and functional in Light/Dark at 100/125/150/175/200% Windows scaling.
- Owner-drawn calendar exposes meaningful accessible nav/day children while using no per-day WinForms child controls.
- `BootstrapCalendarPicker` uses hosted-control `BootstrapDropdown` and the shared internal generic anchored-show primitive; it does not create a new popup engine or fake presentation button.
- Existing `BootstrapDropdown.Show()`, plan-002 Button `ShowFrom(...)`, `BootstrapSplitButton`, and native-backed `BootstrapDatePicker` behavior remain green.
- Single and completed Range picker sessions auto-close; incomplete Range and Multiple follow the specified open policy.
- Demo, docs, compatibility/testing guidance, changelog/package docs, and public API baseline are updated.
- Both `net48` and `net8.0-windows` builds and full test suites pass.
