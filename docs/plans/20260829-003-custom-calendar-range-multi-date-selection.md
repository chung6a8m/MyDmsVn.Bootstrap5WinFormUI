# Custom Calendar Rendering, Date Range, and Multi-Date Selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a fully framework-rendered Bootstrap-inspired calendar plus range and multi-date selection without breaking the existing native-backed `BootstrapDatePicker`, and expose the same calendar through an advanced popup picker that reuses the repository's hosted-control Dropdown infrastructure.

**Architecture:** Preserve `BootstrapDatePicker` exactly as the native single-date/editor option. Add a new owner-drawn `BootstrapCalendar : Control` with no per-day child controls; it owns month/header/week/day rendering, hit testing, keyboard focus, theme/DPI behavior, and delegates all selection rules to one internal `BootstrapCalendarSelectionModel` shared by `Single`, `Range`, and `Multiple` modes. Add `BootstrapCalendarPicker : Control` as a compact summary/trigger surface that hosts a fresh `BootstrapCalendar` inside `BootstrapDropdown` using the hosted-control support from `docs/plans/20260829-002-dropdown-submenus-hosted-controls-split-button.md`; native ToolStrip infrastructure continues to own popup focus, working-area placement, Escape/outside-click dismissal, and popup lifetime, so this feature does not introduce a second top-level popup engine.

**Tech Stack:** C#, native Windows Forms owner drawing (`Control`, `Graphics`, `TextRenderer`), existing Theme / Rendering / Compatibility infrastructure, `BootstrapDropdown` + `ToolStripControlHost` hosted-control support from plan `20260829-002`, `BootstrapValidationState`, `BootstrapThemeManager`, `BootstrapThemeMetrics`, `DpiScaler`, `RoundedPath`, `CornerRadius`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** This plan is an advanced extension of `docs/plans/20260828-009-bootstrap-date-picker.md`. That Stage 9 plan deliberately deferred custom calendar rendering, date ranges, and multi-date selection; this plan makes those deferred capabilities explicit while preserving the Stage 9 public contract. Popup composition depends on the hosted-control Dropdown contract planned in `docs/plans/20260829-002-dropdown-submenus-hosted-controls-split-button.md`. Repository-wide constraints in `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, and `docs/PUBLIC_API_BASELINE.md` remain authoritative.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; new public types live under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile from one shared implementation for both `net48` and `net8.0-windows` wherever practical.
- Do **not** replace, subclass around, or silently change the existing native-backed `BootstrapDatePicker`. Its existing public members, native `DateTimePicker` behavior, tests, and compatibility guarantees remain intact.
- `BootstrapDatePicker` remains the recommended choice when an application needs native typed date/time segment editing, native locale formatting, `ShowCheckBox`, or the OS-owned calendar popup.
- `BootstrapCalendar` is the new fully custom-rendered date-selection surface. The framework owns every visible calendar pixel inside this control: month header, navigation buttons, weekday labels, 42 day cells, hover, focus, disabled, today, selected, committed-range, and preview-range presentation.
- `BootstrapCalendar` must be one owner-drawn focusable control. Do not create 42 `Button`, `Label`, or other child controls for day cells.
- Do not use native `MonthCalendar` or a hidden/native `DateTimePicker` as the calendar implementation. This scope exists specifically to make calendar rendering framework-owned.
- One internal selection state machine must power `Single`, `Range`, and `Multiple`; do not implement three independent click/keyboard selection algorithms in the control.
- All public and internal date-selection values represent calendar dates, not instants. Normalize accepted values through `value.Date`; time-of-day is discarded intentionally.
- Default allowed bounds are `DateTimePicker.MinimumDateTime.Date` and `DateTimePicker.MaximumDateTime.Date`, keeping the advanced calendar inside the same broad date domain as the existing native picker.
- `MinDate <= MaxDate` is required. An invalid bound assignment throws `ArgumentOutOfRangeException` before mutating state.
- Programmatic selection outside `[MinDate, MaxDate]` throws `ArgumentOutOfRangeException`; UI cells outside the range are rendered disabled and do not activate.
- Changing bounds may invalidate an existing selection. Single selection is cleared if outside the new range; an incomplete or complete range is cleared if either endpoint falls outside; Multiple mode removes only dates that are now out of range. Raise `SelectionChanged` once if effective public selection changes.
- `DisplayMonth` is normalized to the first day of its month and is clamped to a month that intersects `[MinDate, MaxDate]`. `DisplayMonthChanged` fires only for an effective month change.
- Culture is read from `CultureInfo.CurrentCulture` at layout/render/format time. Weekday order starts at `CurrentCulture.DateTimeFormat.FirstDayOfWeek`. This scope adds no separate public culture property.
- The visual month grid is always six rows by seven columns (42 date cells). Leading/trailing adjacent-month days remain visible, use muted presentation, and are selectable when inside the allowed date range.
- Selecting an enabled adjacent-month day moves `DisplayMonth` to that date's month after applying the selection.
- `Single` mode stores zero or one `SelectedDate`.
- `Range` mode stores `RangeStart` and optional `RangeEnd`. First activation starts/restarts a range with only `RangeStart`; second activation completes the range and normalizes endpoint order. A third activation after a complete range begins a new incomplete range.
- Range hover preview is presentation-only. Moving the mouse or keyboard focus must never mutate `RangeStart`/`RangeEnd` or raise `SelectionChanged` until an activation occurs.
- `Multiple` mode toggles each activated date. Public `SelectedDates` is always a sorted, deduplicated snapshot in ascending date order.
- Switching `SelectionMode` clears the previous mode's selection. Raise `SelectionChanged` exactly once if anything was selected; do not reinterpret a range as a multiple selection or a multiple selection as a single date implicitly.
- Keyboard focus date is distinct from public selection. Arrow keys move it by one/seven days, PageUp/PageDown by one month, Home/End to the first/last day in the current culture's week, and Enter/Space activates it. Moving keyboard focus across month boundaries updates `DisplayMonth`.
- Month arithmetic must clamp the day number safely (for example January 31 PageDown -> February 28/29) and must not use `Math.Clamp`, which is unavailable on `net48`.
- Navigation buttons are disabled/no-op when no date in the previous/next month intersects the allowed range.
- Theme colors/metrics come from `BootstrapThemeManager.CurrentTheme`; do not add calendar-only hard-coded palette values when existing `Surface`, `SurfaceSecondary`, `Border`, `Text`, `MutedText`, `Disabled`, `Focus`, `Hover`, `Active`, and `Primary` tokens are sufficient.
- Selected cells use a theme-token composition that preserves text readability without inventing a new contrast engine: selected endpoint/multiple cells use `Active` surface plus `Primary` outline; committed range interiors use `SurfaceSecondary`; preview range uses `Hover`; focused cells add the `Focus` outline. Text remains `Text` or `MutedText` according to cell/month/enabled state.
- Derive all geometry from existing `BootstrapThemeMetrics` and `DpiScaler`. Do not extend the theme constructor only for this feature.
- The 96-DPI baseline uses existing metrics: outer padding `SpacingSM`, header height `ControlHeight`, weekday row height `ControlHeightSmall`, day-row height `ControlHeight`, cell gap `SpacingXS`, and theme radius for `BorderRadius = -1`.
- Cache resolved layout/cell geometry when size, DPI, display month, culture, or bounds change. Do not allocate a new list/array of 42 cells on every `OnPaint` call.
- All temporary GDI objects are disposed with `using`; no persistent `Pen`, `Brush`, `GraphicsPath`, `Bitmap`, or `Region` is required.
- `BootstrapCalendar` subscribes to `BootstrapThemeManager.ThemeChanged` at most once and unsubscribes on disposal. Dispose only framework-created fonts; never dispose caller-assigned fonts.
- Designer construction must work without application startup, DI, a running message loop, assigned parent, or initialized adapter.
- `BootstrapCalendarPicker` must reuse hosted-control `BootstrapDropdown` support from plan `20260829-002`. Do not create a custom top-level `Form`, `ToolStripDropDown` clone, global mouse/keyboard hook, or second placement/focus/dismissal engine.
- The popup calendar is factory-created for each effective Dropdown snapshot because `ToolStripControlHost` owns/disposes hosted controls according to the Dropdown snapshot lifecycle. The picker owns only logical selection/display state, not a reusable popup `Control` instance.
- Single-mode picker activation commits and closes immediately. Range mode stays open after the first endpoint and closes after the second. Multiple mode stays open after each toggle and closes only through Escape, outside click, a second trigger activation, or `CloseDropDown()`.
- Popup close without a second range endpoint keeps the incomplete `RangeStart`; no hidden rollback state is maintained.
- Picker text is a summary only; this plan does not add typed date parsing/editing. `BootstrapDatePicker` remains the native typed/date-time editor.
- `DateFormat` defaults to `"d"`. Its setter validates the format before mutation by formatting a known in-range date with `CultureInfo.CurrentCulture`; invalid format strings preserve normal `FormatException` behavior.
- Empty picker selection renders `PlaceholderText`; the default placeholder is `string.Empty` so the library does not embed English UI copy.
- Range summary uses the same `DateFormat` for both endpoints. Incomplete range renders `<start> – …`. Complete range renders `<start> – <end>`.
- Multiple summary renders one formatted date when one date is selected; for two or more it renders `<first> (+N)`, where `N` is the remaining count. This is intentionally compact and avoids a localization-heavy count sentence.
- Picker validation priority matches other inputs: disabled presentation wins; then `Valid`/`Invalid`; then focus; then neutral.
- All public/protected members receive XML documentation. `TreatWarningsAsErrors` and the repository XML-doc policy remain green.
- This work changes the frozen v1 public API. `Phase16PublicApiBaselineTests` must fail intentionally after the new types/members appear; review the exported surface before updating the approved fingerprint and `docs/PUBLIC_API_BASELINE.md`.
- Final completion requires both target builds, focused and full test suites, Light/Dark checks, real Windows 100/125/150/175/200% DPI checks, keyboard/mouse/popup manual verification, accessibility checks, and GDI/event/host ownership review.

---

## Prerequisite Gate

This plan assumes the existing Stage 9 DatePicker is already complete and that plan `20260829-002` has implemented hosted controls in `BootstrapDropdown`.

Before Task 1, verify these existing/new prerequisite artifacts:

```text
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePicker.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePickerRenderLogic.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItem.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdownItemKind.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDropdownTests.cs
```

After implementing plan `20260829-002`, the following contract must also exist before popup-picker work begins:

```csharp
BootstrapDropdownItemKind.HostedControl
BootstrapDropdownItem.DropDownItems
BootstrapDropdownItem.HostedControlFactory
```

Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapDatePicker|BootstrapDropdown"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapDatePicker|BootstrapDropdown"
```

Expected: both targets pass. If hosted-control support has not yet been implemented, stop after the standalone `BootstrapCalendar` tasks and finish `docs/plans/20260829-002-dropdown-submenus-hosted-controls-split-button.md` before starting `BootstrapCalendarPicker`; do not embed a competing popup implementation in this plan.

---

## Public Contract Added by This Plan

### Selection mode

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public enum BootstrapCalendarSelectionMode
{
    Single = 0,
    Range = 1,
    Multiple = 2
}
```

Undefined values throw `ArgumentOutOfRangeException` before state mutation.

### Standalone fully custom calendar

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

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

Mode-specific rules:

- `SelectedDate` get returns the selected date only in `Single`; otherwise `null`. Setting it outside `Single` throws `InvalidOperationException` before mutation. Setting `null` clears Single selection.
- `RangeStart`/`RangeEnd` are non-null only in `Range`.
- `SetRange(null, null)` clears Range selection. `SetRange(start, null)` creates an incomplete range. `SetRange(null, end)` throws `ArgumentException`. Two non-null endpoints are normalized to ascending order.
- Calling `SetRange` outside `Range` throws `InvalidOperationException`.
- `SelectedDates` returns a new read-only/snapshot view of the ascending Multiple selection; outside `Multiple` it is empty.
- `SetSelectedDates(...)` is valid only in `Multiple`; `null` throws `ArgumentNullException`; duplicate inputs are deduplicated after `.Date` normalization; any out-of-range value rejects the whole call before mutation.
- `ClearSelection()` works in every mode and raises `SelectionChanged` only if effective selection existed.
- `DisplayMonth` ignores time/day portions after normalizing to the first day of the requested month and clamps to the nearest month that intersects allowed bounds.
- `BorderRadius = -1` uses the current theme radius; non-negative values are explicit logical radii; values below `-1` throw before mutation.

### Advanced popup picker

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

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

The picker uses the same mode-specific selection rules as `BootstrapCalendar`. Do not add aliases such as `Value`, `StartDate`, `EndDate`, `Dates`, `SelectedRange`, or `IsOpen`; the explicit API above avoids ambiguity across modes.

### Public surface deliberately not added

- No changes to `BootstrapDatePicker`.
- No time-of-day selection, time picker, or `DateTimeOffset` API.
- No consumer-supplied per-cell `Paint` callback/template in this scope. “Fully custom rendering” means the framework owns the calendar rendering rather than the OS; it does not yet mean arbitrary user templating.
- No week-number column.
- No month/year/decade zoom view.
- No multiple-month side-by-side view.
- No drag-to-select or Shift-drag range selection.
- No arbitrary disabled-date predicate, holiday provider, appointment/event badges, recurrence engine, or blackout collection.
- No animated month transitions.
- No typed text parser/editor in `BootstrapCalendarPicker`.
- No direct public access to `ToolStripDropDownMenu`, `ToolStripControlHost`, the hosted calendar instance, or native popup handles.
- No new popup-placement engine, top-level popup Form, global input hook, or message-loop replacement.

---

## Internal State Contract

Create one model used by both public controls:

```csharp
internal readonly struct BootstrapCalendarSelectionChange
{
    public BootstrapCalendarSelectionChange(bool changed, bool completed)
    {
        Changed = changed;
        Completed = completed;
    }

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

`bool` return values report whether effective selection changed. `Activate(...).Completed` means the interaction can complete a popup selection session: always `true` for Single, `false` on the first Range endpoint, `true` on the second Range endpoint, and `false` for Multiple so the popup remains open.

The model does not raise events. `BootstrapCalendar` and `BootstrapCalendarPicker` own public event emission and can therefore guarantee one `SelectionChanged` per effective transition.

---

## Calendar Rendering and Layout Contract

Create pure/internal layout types in `BootstrapCalendarRenderLogic.cs`:

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

Required pure helpers:

```csharp
internal static BootstrapCalendarMetrics ResolveMetrics(
    BootstrapThemeMetrics themeMetrics,
    int dpi,
    int borderRadius);

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
    DateTime date,
    DayOfWeek firstDayOfWeek,
    bool endOfWeek);
```

`CalculateLayout` returns 42 day cells even when client dimensions are constrained; cell bounds may collapse to zero size but never become negative or escape the client rectangle. Non-positive client size returns empty rectangles with the same 42 logical date entries so keyboard/date projection can remain deterministic.

Month projection formula:

```text
monthStart = new DateTime(displayMonth.Year, displayMonth.Month, 1)
offset = ((int)monthStart.DayOfWeek - (int)firstDayOfWeek + 7) % 7
gridStart = monthStart.AddDays(-offset)
cell[i].Date = gridStart.AddDays(i), i = 0..41
```

No localized month/day strings are stored in layout objects; text is resolved at render time from `CultureInfo.CurrentCulture` so a culture change followed by invalidation/layout rebuild is reflected without stale cached labels.

---

## File Map

### Create product files

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionMode.cs` — public three-mode enum.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionModel.cs` — internal date-only normalization, bounds, mode, range, and multiple-selection state machine.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs` — pure DPI metrics, six-week projection, layout, hit testing, and safe keyboard date arithmetic.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs` — fully owner-drawn standalone calendar, interaction, theme/font/DPI/lifecycle/accessibility.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarPicker.cs` — compact themed trigger/summary control, shared logical selection state, hosted-calendar Dropdown synchronization and popup lifecycle.

### Create tests

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarSelectionModelTests.cs` — all mode/state/bounds normalization and activation semantics.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs` — month projection, first-day-of-week, leap years, DPI/layout, hit testing, safe month/week keyboard arithmetic.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs` — public contract, rendering smoke, mouse/keyboard navigation/selection, theme/DPI, accessibility, lifecycle.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs` — hosted-control characterization, picker public contract, summary formatting, selection synchronization, open/close policy, keyboard, theme/DPI, disposal.

### Modify demo/tests

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs` — add standalone custom calendar and Single/Range/Multiple popup-picker scenarios to the existing Advanced Inputs page.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs` — assert the new scenarios and labels without creating another top-level demo page.

### Modify documentation/public baseline

- `docs/COMPONENTS.md`
- `docs/TESTING.md`
- `docs/COMPATIBILITY.md`
- `README.md`
- `docs/PACKAGE_README.md`
- `CHANGELOG.md`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- `docs/PUBLIC_API_BASELINE.md`

No project-file edit should be necessary under SDK-style default `Compile` inclusion. Do not edit a `.csproj` unless the current project explicitly excludes one of the new paths.

---

### Task 1: Characterize hosted-control popup behavior before calendar-picker integration

**Files:**
- Modify/Create test content in: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs`

**Interfaces:**
- Consumes: hosted-control `BootstrapDropdown` contract from plan `20260829-002`, native ToolStrip popup behavior.
- Produces: an executable guard that proves a calendar-like hosted control can receive focus/clicks without closing the popup after every interaction.

- [ ] **Step 1: Write the STA fixture and hosted-control focus/click characterization.** Create a hidden/real `Form`, a `BootstrapButton` target, and a `BootstrapDropdown` with one `HostedControl` item whose factory returns a small focusable `Panel` containing a `Button`. Open the Dropdown, focus/click the hosted button, call `Application.DoEvents()`, and assert `Opened == 1`, `Closed == 0`, and the hosted button click occurred.

- [ ] **Step 2: Characterize Escape/programmatic close separately.** After reopening the same Dropdown, call the existing public `Close()` path and assert `Closed` raises exactly once. Keep full Escape/outside-click behavior in the native Dropdown/manual verification path; do not simulate private WinForms popup messages through reflection.

- [ ] **Step 3: Run the characterization on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapCalendarPickerTests&Name~HostedControl"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapCalendarPickerTests&Name~HostedControl"
```

Expected: hosted-control interaction does not auto-close the root popup; explicit close does. If the implemented plan `20260829-002` closes a hosted-control row on ordinary child interaction, fix that hosted-control contract first. Do not work around it with a calendar-specific popup engine.

- [ ] **Step 4: Commit the popup behavior lock.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs
git commit -m "test: characterize hosted calendar popup behavior"
```

---

### Task 2: Implement the shared Single/Range/Multiple selection model

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionMode.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionModel.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarSelectionModelTests.cs`

**Interfaces:**
- Consumes: `DateTime`, `IEnumerable<DateTime>`, `IReadOnlyList<DateTime>`.
- Produces: exact internal `BootstrapCalendarSelectionModel` and `BootstrapCalendarSelectionChange` signatures defined above.

- [ ] **Step 1: Write failing construction and validation tests.** Assert defaults are `Single`, empty selection, normalized date-only min/max, and that `min > max` plus undefined selection modes throw before mutation.

Representative test:

```csharp
[Test]
public void ConstructionNormalizesBoundsAndStartsSingleEmpty()
{
    var model = new BootstrapCalendarSelectionModel(
        new DateTime(2020, 1, 1, 10, 30, 0),
        new DateTime(2030, 12, 31, 23, 59, 59));

    Assert.Multiple((Action)(() =>
    {
        Assert.That(model.Mode, Is.EqualTo(BootstrapCalendarSelectionMode.Single));
        Assert.That(model.MinDate, Is.EqualTo(new DateTime(2020, 1, 1)));
        Assert.That(model.MaxDate, Is.EqualTo(new DateTime(2030, 12, 31)));
        Assert.That(model.SelectedDate, Is.Null);
        Assert.That(model.SelectedDates, Is.Empty);
    }));
}
```

- [ ] **Step 2: Run focused tests and verify they fail because the model/types do not exist.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapCalendarSelectionModelTests"
```

- [ ] **Step 3: Implement enum, normalization, mode validation, and empty state only.** Keep `SortedSet<DateTime>` internal for Multiple mode and expose snapshots through `ToArray()`/`Array.AsReadOnly(...)` compatible with `net48`; do not leak the mutable set.

- [ ] **Step 4: Add failing Single activation tests.** Cover first activation, same-date reactivation (`Changed=false`, `Completed=true`), different date, explicit `null` clear, `.Date` normalization, and out-of-range rejection.

- [ ] **Step 5: Implement Single behavior minimally.** `Activate()` validates bounds, sets the normalized date, clears other mode storage, and returns completion `true` even for a no-op same-date confirmation.

- [ ] **Step 6: Add failing Range tests.** Cover first endpoint (`Completed=false`), second endpoint (`Completed=true`), reverse-order normalization, third activation restarting the range, `SetRange(start, null)`, `SetRange(null, null)`, invalid `null/end`, and out-of-range rejection without partial mutation.

- [ ] **Step 7: Implement Range behavior and rerun focused tests.** Store incomplete range explicitly as `RangeStart != null`, `RangeEnd == null`; do not synthesize a second endpoint.

- [ ] **Step 8: Add failing Multiple tests.** Cover toggling on/off, sorted output, duplicate normalization, atomic rejection of an input set containing one out-of-range date, and `Completed=false` for every activation.

- [ ] **Step 9: Implement Multiple behavior.** Normalize to `.Date`, validate the full incoming set before replacing internal state, then de-duplicate/sort.

- [ ] **Step 10: Add bounds/mode-transition tests.** Verify Single/Range selections clear when invalidated, Multiple removes only invalid dates, valid selections survive compatible bound changes, and changing mode clears old mode state with the correct returned `changed` flag.

- [ ] **Step 11: Run selection-model tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapCalendarSelectionModelTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapCalendarSelectionModelTests"
```

Expected: PASS on both targets.

- [ ] **Step 12: Commit the shared selection state machine.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionMode.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarSelectionModel.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarSelectionModelTests.cs
git commit -m "feat: add calendar selection state model"
```

---

### Task 3: Add pure six-week projection, layout, hit testing, and safe date navigation

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs`

**Interfaces:**
- Consumes: `BootstrapThemeMetrics`, `DpiScaler`, `Size`, `DayOfWeek`, date bounds.
- Produces: `BootstrapCalendarMetrics`, `BootstrapCalendarDayCell`, `BootstrapCalendarLayout`, `ResolveMetrics`, `CalculateLayout`, `HitTestDay`, `MoveByMonth`, `MoveToWeekBoundary`.

- [ ] **Step 1: Write failing metric tests for 96/120/144/168/192 DPI.** Assert existing tokens are scaled exactly, `BorderRadius=-1` maps to theme `Radius`, explicit radius scales as a logical value, DPI <= 0 throws, and radius below `-1` throws.

- [ ] **Step 2: Add failing month-projection tests independent of current culture.** Supply explicit `firstDayOfWeek` values and verify:
  - September 2026 with Sunday-first and Monday-first starts on the expected grid date.
  - February 2028 contains February 29.
  - December -> January and January -> December leading/trailing cells preserve correct year boundaries.
  - exactly 42 cells are produced and dates are strictly consecutive.

- [ ] **Step 3: Implement metrics and projection/layout sufficiently to pass those tests.** Use the formula in the Rendering Contract; do not call `CultureInfo.CurrentCulture` inside the pure helper when `firstDayOfWeek` is already supplied.

- [ ] **Step 4: Add failing bounds/layout tests.** Cover a normal `280x300` surface, very narrow/tiny surfaces, zero size, header nav/title containment, seven weekday columns, six equal day rows, 42 non-negative day bounds, and every non-empty rectangle contained by the client rectangle.

- [ ] **Step 5: Implement deterministic integer layout.** Distribute any remainder pixels across the earliest columns/rows so the final right/bottom edge exactly meets the intended inner content boundary instead of accumulating rounding gaps.

- [ ] **Step 6: Add failing `HitTestDay` tests.** Verify centers of cells 0, 20, and 41 map correctly; points in header, gaps, and outside client return `-1`.

- [ ] **Step 7: Implement hit testing against cached cell bounds.** Iterate 42 cells; no spatial index is needed for this bounded grid.

- [ ] **Step 8: Add failing safe navigation tests.** Verify:

```text
2025-01-31 +1 month = 2025-02-28
2028-01-31 +1 month = 2028-02-29
2026-03-31 -1 month = 2026-02-28
Sunday-first Home/End produce Sunday/Saturday
Monday-first Home/End produce Monday/Sunday
```

Also test crossing year boundaries.

- [ ] **Step 9: Implement safe date navigation without `Math.Clamp`.** Compute target month/year first, then `Math.Min(originalDay, DateTime.DaysInMonth(targetYear, targetMonth))`.

- [ ] **Step 10: Run pure tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapCalendarRenderLogicTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapCalendarRenderLogicTests"
```

Expected: PASS on both targets.

- [ ] **Step 11: Commit the pure calendar geometry/navigation engine.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs
git commit -m "feat: add calendar rendering geometry"
```

---

### Task 4: Implement the fully owner-drawn BootstrapCalendar shell and public contract

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs`

**Interfaces:**
- Consumes: Tasks 2-3 state/layout logic, theme/render infrastructure.
- Produces: exact public `BootstrapCalendar` contract defined above, cached layout, theme-owned rendering, and no per-day child controls.

- [ ] **Step 1: Write failing metadata/default/public-surface tests.** Assert `DefaultEvent(SelectionChanged)`, no child controls, `TabStop=true`, default mode Single, min/max equal native safe date domain, current display month normalized to day 1, empty selection, `BorderRadius=-1`, and exactly the declared public members from this plan.

- [ ] **Step 2: Write failing mode-specific forwarding tests.** Exercise `SelectedDate`, `SetRange`, `SetSelectedDates`, `ClearSelection`, bound changes, and `SelectionChanged` counts. Assert invalid mode-specific calls throw before mutation.

- [ ] **Step 3: Run focused tests and verify compile/test failure before the control exists.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapCalendarTests"
```

- [ ] **Step 4: Implement construction, state forwarding, mode/bound/display-month validation, and public events without painting interaction yet.** Configure:

```csharp
SetStyle(
    ControlStyles.UserPaint |
    ControlStyles.AllPaintingInWmPaint |
    ControlStyles.OptimizedDoubleBuffer |
    ControlStyles.ResizeRedraw |
    ControlStyles.Selectable,
    true);
TabStop = true;
AccessibleRole = AccessibleRole.Table;
```

Initialize a private `BootstrapCalendarSelectionModel`, normalized current month, focus date, theme subscription, and theme body font. Preferred/default size must derive from metrics rather than a magic device-pixel height.

- [ ] **Step 5: Add failing `DrawToBitmap` smoke tests for Light/Dark, enabled/disabled, Single/Range/Multiple selections, incomplete range, explicit radius, adjacent-month days, today, and tiny sizes.** These are smoke/ownership tests, not pixel-golden tests.

- [ ] **Step 6: Implement cached layout invalidation.** Rebuild cached `BootstrapCalendarLayout` only when client size, effective DPI, display month, first day of week, min/max, or border radius changes. Theme color-only changes invalidate painting without rebuilding date projection unless metrics changed.

- [ ] **Step 7: Implement owner drawing in this order:**

```text
1. rounded outer Surface + Border
2. previous/next navigation button hover/disabled states
3. centered culture-aware month title ("Y")
4. seven abbreviated weekday names in culture order
5. committed range interior surfaces
6. preview range surfaces
7. selected endpoint/multiple surfaces + Primary outline
8. today indicator
9. day number text
10. keyboard-focus outline
```

Use `TextRenderer.DrawText` for labels/numbers; use `RoundedPath` only where rounded geometry is visible; restore any changed `Graphics` state.

- [ ] **Step 8: Add render-state tests through internal/test-visible helper assertions rather than exact colors at individual bitmap pixels.** Verify cell state classification for selected endpoint, range middle, preview, adjacent month, disabled, and today. If classification needs a helper, keep it internal in `BootstrapCalendarRenderLogic` rather than adding public appearance state.

- [ ] **Step 9: Run focused calendar tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapCalendarTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapCalendarTests"
```

- [ ] **Step 10: Commit the owner-drawn calendar shell.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs
git commit -m "feat: add fully rendered BootstrapCalendar"
```

---

### Task 5: Add calendar mouse, range-preview, keyboard, and month-navigation interaction

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs`

**Interfaces:**
- Consumes: cached layout and `BootstrapCalendarSelectionModel.Activate(...)`.
- Produces: deterministic pointer/keyboard date navigation and event paths shared across all modes.

- [ ] **Step 1: Add failing mouse activation tests.** Invoke protected mouse methods against known cell centers and assert:
  - Single click selects once.
  - Range first click creates only `RangeStart`; second click completes and normalizes order.
  - Multiple click toggles on/off.
  - disabled out-of-range cell is a no-op.
  - clicking an enabled adjacent-month date selects it and changes `DisplayMonth` once.

- [ ] **Step 2: Implement `OnMouseDown` using `HitTestDay`.** Set focus to the control, ignore non-left button/disabled cells, call the single state-model activation path, raise `SelectionChanged` only when `Changed`, then update display month if needed.

- [ ] **Step 3: Add failing hover/preview tests.** In Range mode with only `RangeStart`, moving over another enabled day marks the preview interval for rendering but leaves public range state/event count unchanged. Mouse leave clears preview. Preview is absent in Single/Multiple and after a completed range.

- [ ] **Step 4: Implement `_hotDayIndex` and `_rangePreviewDate` as presentation-only state.** Invalidate the calendar control when the effective preview changes; never write preview into the selection model.

- [ ] **Step 5: Add failing navigation-button tests.** Click previous/next header rectangles, verify effective month changes once, and verify buttons are no-op when the adjacent month lies wholly outside min/max.

- [ ] **Step 6: Implement `ShowPreviousMonth()`/`ShowNextMonth()` through one private `TrySetDisplayMonth(...)` path used by both public methods and mouse buttons.**

- [ ] **Step 7: Add failing keyboard tests.** Cover Left/Right, Up/Down, PageUp/PageDown, Home/End under Sunday-first and Monday-first culture settings, Enter/Space activation, focus date clamping at Min/Max, and crossing into adjacent months.

- [ ] **Step 8: Implement `IsInputKey`/`OnKeyDown` routing.** Mark navigation keys as input keys; move only the private focus date until Enter/Space activates. Use Task 3 helpers for PageUp/PageDown/Home/End.

- [ ] **Step 9: Add no-duplicate-event tests.** Mouse or keyboard reactivation of the same Single date must not raise `SelectionChanged` twice even though an activation can be considered “complete”; changing only display month/focus/hover must never raise selection events.

- [ ] **Step 10: Run interaction tests on both targets and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapCalendarTests|BootstrapCalendarRenderLogicTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapCalendarTests|BootstrapCalendarRenderLogicTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarRenderLogicTests.cs
git commit -m "feat: add calendar selection interaction"
```

---

### Task 6: Complete theme/font/DPI lifecycle and custom-calendar accessibility

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs`

**Interfaces:**
- Consumes: existing theme manager/font ownership patterns used by `BootstrapTextBox`/`BootstrapDatePicker`.
- Produces: leak-safe live theme/DPI behavior and accessible header/day-cell semantics for the owner-drawn control.

- [ ] **Step 1: Add failing theme/font lifecycle tests.** Verify Light -> Dark -> Light invalidates without changing selection/display month; caller-assigned font survives theme switches and is not disposed by the calendar; framework theme font is replaced/disposed only when framework-owned; disposing the calendar detaches the static theme subscription.

- [ ] **Step 2: Implement the established `_useThemeFont`, `_settingThemeFont`, `_themeFont`, `_themeSubscribed` ownership pattern.** On theme change, recreate only framework-owned font, recompute cached metrics/layout if typography/metrics affect size, and invalidate.

- [ ] **Step 3: Add failing DPI-layout tests.** Exercise `ResolveMetrics` at the supported DPI matrix and invoke the parent-DPI lifecycle path so cached layout is rebuilt while selected dates/display month remain unchanged.

- [ ] **Step 4: Implement `OnDpiChangedAfterParent`, `OnFontChanged`, `OnSizeChanged`, and disposal paths.** Do not double-scale font metrics or device-pixel measurements.

- [ ] **Step 5: Add an internal custom accessibility object.** `CreateAccessibilityInstance()` returns a `ControlAccessibleObject` implementation with 44 logical children in this order: Previous button, Next button, then 42 day cells. Navigation children use `AccessibleRole.PushButton`; day children use `AccessibleRole.Cell`; names use culture-aware full date strings; disabled day cells expose `Unavailable`; selected endpoints/multiple dates and committed range cells expose `Selected`; the private keyboard-focus cell exposes `Focused`.

- [ ] **Step 6: Add accessibility tests.** Assert child count, roles, representative date names, selected/unavailable/focused states, and that accessibility bounds update after resize without creating WinForms child controls.

- [ ] **Step 7: Run lifecycle/accessibility tests on both targets and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapCalendarTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapCalendarTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendar.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarTests.cs
git commit -m "feat: harden calendar theme dpi accessibility"
```

---

### Task 7: Implement BootstrapCalendarPicker with hosted BootstrapCalendar popup

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarPicker.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs`

**Interfaces:**
- Consumes: `BootstrapCalendarSelectionModel`, `BootstrapCalendar`, `BootstrapDropdown`, hosted-control factory support from plan `20260829-002`, existing input validation render rules.
- Produces: exact public `BootstrapCalendarPicker` API and one popup-calendar factory path.

- [ ] **Step 1: Write failing picker defaults/public-surface tests.** Assert default Single mode, native-safe bounds, empty selection, `DateFormat="d"`, `PlaceholderText=string.Empty`, `ValidationState=None`, `BorderRadius=-1`, one public tab stop, no public/native calendar child control, and exactly the planned declared public members.

- [ ] **Step 2: Write failing state API tests.** Verify Single/Range/Multiple programmatic APIs match the standalone calendar rules and `SelectionChanged` counts. Use one shared selection model inside the picker; do not duplicate state fields for each mode.

- [ ] **Step 3: Write failing `DateFormat`/summary tests under a fixed `CultureInfo`.** Cover empty placeholder, Single date, incomplete Range, complete Range, one Multiple date, and three Multiple dates (`first (+2)`). Verify invalid format string throws before mutating the previous format.

- [ ] **Step 4: Implement picker shell/state/summary rendering without popup opening yet.** Reuse `BootstrapTextBoxRenderLogic.ResolveBorderColor(...)` for validation/focus priority and existing theme metrics for padding/radius. Draw one structural calendar/dropdown affordance on the trailing edge using framework vector drawing or existing icon infrastructure; do not introduce a new public icon property.

- [ ] **Step 5: Write failing popup-factory synchronization tests.** On `ShowDropDown()`, capture the factory-created `BootstrapCalendar` and assert it receives picker SelectionMode, bounds, selected state, and a display month based on current picker state: selected date/range start/first multiple date when available, otherwise today's month clamped to bounds.

- [ ] **Step 6: Implement the internal Dropdown model.** Create exactly one `BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)` during picker construction and set its `HostedControlFactory` to a private factory method. The factory creates a **fresh** `BootstrapCalendar`, configures a DPI-aware fixed preferred popup size, syncs logical state, subscribes to calendar `SelectionChanged`/`DisplayMonthChanged`, and returns it. Never cache/reuse a disposed hosted calendar across Dropdown snapshots.

- [ ] **Step 7: Write failing popup-to-picker commit tests.** Simulate calendar activation through the hosted calendar and assert:
  - Single: picker selection updates once and Dropdown closes.
  - Range first endpoint: picker `RangeStart` updates once and Dropdown stays open.
  - Range second endpoint: picker range completes and Dropdown closes.
  - Multiple: each toggle updates picker selection once and Dropdown remains open.
  - no-op same Single selection closes without a duplicate `SelectionChanged`.

- [ ] **Step 8: Implement one synchronization path.** On hosted-calendar `SelectionChanged`, copy the complete logical snapshot through mode-specific picker/model methods, then apply close policy from the selection mode/completion state. Guard against feedback loops with a private synchronization flag; never suppress legitimate later user events.

- [ ] **Step 9: Preserve display month across popup instances.** Track only a normalized logical `_lastDisplayMonth` in the picker. Update it from hosted `DisplayMonthChanged`, clamp it when bounds change, and seed the next factory-created calendar from it. This is not a reference to the hosted control.

- [ ] **Step 10: Add failing trigger/keyboard/lifecycle tests.** Mouse activation and Enter/Space/F4/Alt+Down open when enabled; a second trigger while open closes; disabled picker does not open; native popup Escape/outside behavior remains Dropdown-owned; `Opened`/`Closed` each forward exactly once; disposal closes/disposes the internal Dropdown and detaches theme handlers.

- [ ] **Step 11: Implement trigger routing and theme/DPI lifecycle.** The picker remains one focusable public control. Do not forward navigation arrows to a closed calendar; once the hosted calendar has focus, its own keyboard model is authoritative.

- [ ] **Step 12: Run picker tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapCalendarPickerTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapCalendarPickerTests"
```

Expected: PASS on both targets.

- [ ] **Step 13: Commit the advanced popup picker.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapCalendarPicker.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapCalendarPickerTests.cs
git commit -m "feat: add custom calendar picker"
```

---

### Task 8: Add Advanced Inputs demo coverage and manual interaction matrix

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs`

**Interfaces:**
- Consumes: public `BootstrapCalendar` and `BootstrapCalendarPicker` APIs only.
- Produces: discoverable scenarios proving fully custom render states plus all three selection modes.

- [ ] **Step 1: Add failing demo structure tests.** Require the existing Advanced Inputs page to expose these labeled scenarios without creating another navigation page:

```text
Custom Calendar — Range
Calendar Picker — Single
Calendar Picker — Range
Calendar Picker — Multiple
```

- [ ] **Step 2: Add the standalone Range calendar.** Seed a deterministic visible month and completed sample range, then show the current range in a neighboring plain label updated by `SelectionChanged`.

- [ ] **Step 3: Add Single/Range/Multiple picker examples.** Give each a finite 2025-2030 sample bound and `DateFormat = "yyyy-MM-dd"`; set descriptive `PlaceholderText` in the demo only, not in library defaults.

- [ ] **Step 4: Add one invalid/disabled presentation sample without adding another component type.** Use the Range picker with `ValidationState=Invalid` and a disabled Multiple picker or equivalent layout that makes validation/disabled styling visible.

- [ ] **Step 5: Run demo tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~AdvancedInputsDemoFormTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~AdvancedInputsDemoFormTests"
```

- [ ] **Step 6: Run the manual interaction matrix on a real Windows desktop.** Verify:
  - Light/Dark while standalone calendar and popup are visible.
  - 100/125/150/175/200% Windows scaling.
  - Sunday-first and Monday-first Windows regional settings.
  - mouse selection for Single/Range/Multiple.
  - range hover preview does not commit until click.
  - adjacent-month selection changes month.
  - arrow/PageUp/PageDown/Home/End + Enter/Space keyboard flow.
  - F4/Alt+Down/Enter/Space opens picker.
  - Escape/outside click closes through native Dropdown.
  - single and completed range auto-close; multiple remains open.
  - disabled dates never activate.
  - repeated open/close cycles do not leave orphan popup handles or disposed hosted-control references.

- [ ] **Step 7: Commit demo coverage.**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs
git commit -m "demo: showcase custom calendar selection"
```

---

### Task 9: Document the advanced calendar contract and review the frozen public API

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/COMPATIBILITY.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

**Interfaces:**
- Consumes: final public surface from Tasks 2, 4, and 7.
- Produces: user-facing behavioral distinction between native DatePicker and fully custom Calendar/CalendarPicker, plus reviewed API fingerprint.

- [ ] **Step 1: Update `docs/COMPONENTS.md`.** Add explicit sections for `BootstrapCalendar` and `BootstrapCalendarPicker`, document Single/Range/Multiple rules, date-only normalization, keyboard behavior, custom rendering ownership, and the fact that `BootstrapDatePicker` remains native-backed and unchanged.

- [ ] **Step 2: Update `docs/COMPATIBILITY.md`.** Record that custom calendar rendering is framework-owned and therefore visually consistent across supported Windows versions, while popup working-area placement/focus/dismissal remains native ToolStrip behavior. Document CurrentCulture/FirstDayOfWeek dependency and both target frameworks.

- [ ] **Step 3: Update `docs/TESTING.md`.** Add the six-week projection, leap-year, first-day-of-week, range-preview, multi-toggle, keyboard, accessibility, hosted-control ownership, Light/Dark, and DPI matrices from this plan.

- [ ] **Step 4: Update README/package/changelog.** Keep the distinction concise: `BootstrapDatePicker` = native editor/calendar; `BootstrapCalendar`/`BootstrapCalendarPicker` = fully custom calendar + Single/Range/Multiple selection.

- [ ] **Step 5: Run the public API baseline test and expect an intentional failure.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

Expected: FAIL because new public types/members are not yet in the approved frozen fingerprint.

- [ ] **Step 6: Reconstruct and review the public surface before approval.** Confirm the export contains exactly the enum and two public controls defined by this plan and does **not** expose internal model/layout types, hosted calendar instances, ToolStrip types, or aliases rejected above.

- [ ] **Step 7: Update the approved public API fingerprint and `docs/PUBLIC_API_BASELINE.md` in the same change.**

- [ ] **Step 8: Rerun baseline tests on both targets and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
git add docs/COMPONENTS.md docs/TESTING.md docs/COMPATIBILITY.md README.md docs/PACKAGE_README.md CHANGELOG.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs docs/PUBLIC_API_BASELINE.md
git commit -m "docs: document custom calendar public contract"
```

---

### Task 10: Run final cross-control regression and resource/lifecycle verification

**Files:**
- No product file is expected to change unless a regression is found.
- Regression fixes must be made in the smallest responsible file and covered by a reproducing test before final completion.

**Interfaces:**
- Consumes: complete repository after Tasks 1-9.
- Produces: verified two-target release-quality state.

- [ ] **Step 1: Build both target frameworks.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
```

Expected: zero warnings/errors under repository warning policy.

- [ ] **Step 2: Run focused calendar/dropdown/date-input regression.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapCalendar|BootstrapCalendarPicker|BootstrapDatePicker|BootstrapDropdown|AdvancedInputsDemoForm"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapCalendar|BootstrapCalendarPicker|BootstrapDatePicker|BootstrapDropdown|AdvancedInputsDemoForm"
```

Expected: existing native DatePicker/Dropdown tests remain green together with new calendar tests.

- [ ] **Step 3: Run the full automated suite on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
```

- [ ] **Step 4: Run a repeated-open/dispose resource smoke test.** Add or run an STA test loop that creates a picker, opens/closes a hosted calendar, switches modes/selections, and disposes the picker repeatedly. Assert no `ObjectDisposedException`, duplicate event delivery, or retained hosted calendar reference is observable. Keep process-level GDI/USER handle trend verification as a manual Windows check rather than a brittle exact automated count.

- [ ] **Step 5: Run the real-desktop manual matrix from Task 8 after the final Release build.** Include a screen-reader/accessibility-tree inspection for nav buttons/day cells and verify no focus trap occurs when leaving the popup.

- [ ] **Step 6: Review git diff for scope discipline.** Confirm:

```text
BootstrapDatePicker public/native contract unchanged
no MonthCalendar/native DateTimePicker used inside BootstrapCalendar
no second popup/placement engine
no new external package
no duplicate theme/DPI/selection helpers
no public internal ToolStrip/calendar-host implementation types
all new public/protected members XML-documented
```

- [ ] **Step 7: If Step 4-6 required fixes, rerun both full test commands before the final commit.**

- [ ] **Step 8: Commit only verified regression fixes, if any.**

```powershell
git status --short
git diff --check
```

If there are no fixes after verification, do not create an empty commit.

---

## Definition of Done

The feature is complete only when all of the following are true:

- `BootstrapCalendar` renders the entire calendar surface itself; no native calendar/day-cell UI remains underneath it.
- Single, Range, and Multiple modes share one tested state machine.
- Range first/second endpoint and hover-preview semantics are deterministic and covered.
- Multiple selection is date-normalized, deduplicated, sorted, and toggleable by mouse/keyboard.
- Culture-aware month/weekday layout works for different first-day-of-week settings and leap years.
- Mouse and keyboard navigation work without modifying selection until activation.
- Min/max disabled dates, month clamping, and adjacent-month selection are correct.
- Theme/font/DPI lifecycle is leak-safe and visually functional in Light/Dark at the supported Windows scaling matrix.
- The owner-drawn calendar exposes meaningful accessible nav/day children despite using zero per-day WinForms controls.
- `BootstrapCalendarPicker` uses hosted-control `BootstrapDropdown`; it does not introduce a new popup engine.
- Single and completed Range picker sessions auto-close; incomplete Range and Multiple behavior follow the rules in this plan.
- Existing `BootstrapDatePicker` behavior and public API remain unchanged and all existing tests stay green.
- Demo, docs, compatibility/testing guidance, changelog, package docs, and public API baseline are updated.
- Both `net48` and `net8.0-windows` builds and full test suites pass.
