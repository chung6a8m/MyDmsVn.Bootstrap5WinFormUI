# BootstrapDatePicker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Stage 9 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md` by adding a Bootstrap-inspired `BootstrapDatePicker` that delegates date/time value state, locale-aware formatting, native range validation, checkbox semantics, keyboard editing, and the OS calendar popup to exactly one native WinForms `DateTimePicker`, while the framework owns the outer input shell, focus/validation presentation, theme font, DPI-aware layout, lifecycle handling, Advanced Inputs demo coverage, documentation, final cross-control regression, and frozen-public-API review.

**Architecture:** `BootstrapDatePicker : UserControl` composes exactly one native `DateTimePicker`. The native picker is the sole authority for `Value`, `MinDate`, `MaxDate`, `Format`, `CustomFormat`, `ShowCheckBox`, `Checked`, locale-sensitive text, keyboard segment editing, and calendar popup behavior; wrapper properties forward directly and never maintain parallel date state. The outer control owns one public tab stop, redirects focus to the native picker, paints a rounded Bootstrap shell around the native control, resolves focus/validation colors through the established input rules, and applies only supported theme/font/DPI changes. The calendar popup and native picker internals remain OS-owned and are never replaced, subclassed through private implementation details, or force-themed with unsupported Win32 hacks.

**Tech Stack:** C#, native Windows Forms `UserControl` + `DateTimePicker`, existing Theme / Rendering / Compatibility infrastructure, `BootstrapValidationState`, `BootstrapThemeManager`, `BootstrapThemeMetrics`, `DpiScaler`, `RoundedPath`, `CornerRadius`, `BootstrapTextBoxRenderLogic`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** Stage 9 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md`, plus repository-wide constraints in `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`. The Stage 5 NumericBox plan is the closest wrapper-input precedent; the existing `BootstrapTextBox` implementation is the current source of truth for focus redirection, validation priority, theme-owned font lifetime, and shell painting.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; the public control remains under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile from one shared implementation for both `net48` and `net8.0-windows` wherever practical.
- Preserve roadmap order. Stage 8 (`BootstrapToast`) and all earlier stages must be complete and green before Stage 9 implementation begins.
- `BootstrapDatePicker` contains exactly one native `DateTimePicker`. Do not build a custom calendar, `MonthCalendar` popup, `BootstrapDropdown`-based calendar, top-level picker `Form`, or second hidden picker.
- Native WinForms is the sole authority for date/time parsing, locale/regional formatting, range validation, `Value`, `MinDate`, `MaxDate`, checkbox state, keyboard segment editing, and calendar open/select/close semantics.
- Wrapper properties forward directly. Do not mirror `Value`, `MinDate`, `MaxDate`, `Format`, `CustomFormat`, `ShowCheckBox`, or `Checked` into independent backing fields.
- `ValueChanged` is raised from exactly one native `DateTimePicker.ValueChanged` path. Wrapper property setters must never raise a second event.
- `BootstrapValidationState` is reused. Do not add a DatePicker-specific validation enum.
- Validation/focus priority matches TextBox/NumericBox/ComboBox: disabled presentation wins; then `Valid`/`Invalid`; then focused; then neutral.
- `BorderRadius = -1` means the current theme radius. Values below `-1` throw `ArgumentOutOfRangeException` before mutating state.
- The wrapper owns the single public tab stop with `TabStop = true`; the native picker remains `TabStop = false` so the composite does not appear twice in tab order.
- `OnEnter` and shell mouse activation focus the native picker. Native key events are forwarded through the wrapper's inherited `KeyDown`, `KeyPress`, `KeyUp`, and `PreviewKeyDown` events following the existing `BootstrapTextBox` composition pattern.
- The framework may paint only the outer shell it owns. Do not suppress native `DateTimePicker` painting, replace its dropdown button, alter undocumented child windows, intercept private messages to repaint the calendar, or depend on WinForms private fields/types.
- The native picker may retain OS-drawn border/button/text chrome inside the framework shell. Stage 9 does not promise pixel-identical Bootstrap rendering for those OS-owned pixels.
- The calendar popup remains OS-owned. Do not promise rounded popup corners, Bootstrap palette, custom hover cells, month/year templates, week numbers, or multi-date/range selection.
- `ShowUpDown` remains out of the public Stage 9 contract and must not be added as a wrapper property. The contained native picker should retain its normal calendar-dropdown mode.
- Theme colors/metrics come from `BootstrapThemeManager.CurrentTheme`; do not hard-code repeated spacing, border width, focus width, radius, or semantic validation colors when tokens already exist.
- Reuse `DpiScaler`, `RoundedPath`, `CornerRadius`, and `BootstrapTextBoxRenderLogic.ResolveBorderColor(...)`. Do not create another DPI scaler, geometry helper, validation color table, focus engine, or theme manager.
- Date text width is native/culture-dependent. Do not create a second formatter merely to predict or paint the native text. Long/custom formats may clip if the application chooses a width that is too small; the demo must show a sufficiently wide scenario and documentation must state that width remains application-owned.
- `CustomFormat` follows native semantics: it matters only when `Format = DateTimePickerFormat.Custom`; the native default is `null` and assigning `null` must not be normalized into a separate framework state.
- `ShowCheckBox`/`Checked` are the only Stage 9 optional-value mechanism. Do not add `DateTime? Value`, `Nullable`, `HasValue`, placeholder text, clear button, or a second null-state model.
- Native `MinDate`/`MaxDate`/`Value` exceptions and effective state changes are authoritative. Do not duplicate range algorithms in the wrapper.
- Designer construction must work without application bootstrap, DI, service locators, initialized adapters, an assigned parent, or a running message loop.
- Subscribe to `BootstrapThemeManager.ThemeChanged` at most once and unsubscribe on disposal. Dispose only framework-created fonts; never dispose a caller-assigned `Font`.
- Runtime theme switching updates shell palette/theme-owned font/layout without changing native value, range, format, checkbox state, or opening/closing the calendar.
- DPI changes relayout the native picker and repaint the outer shell through existing `DpiScaler`; do not scale a font height or native device-pixel measurement twice.
- Temporary GDI resources are scoped with `using`; no persistent `Pen`, `Brush`, `GraphicsPath`, `Bitmap`, or `Region` is required for Stage 9.
- No timer, animation scheduler, background worker, `Task.Delay`, thread-pool callback, global hook, P/Invoke calendar theming layer, or external package belongs in this stage.
- All new public/protected members receive XML documentation. `TreatWarningsAsErrors` and the repository XML-doc policy remain green.
- Stage 9 changes the frozen v1 public API. `Phase16PublicApiBaselineTests` must intentionally fail first, the reconstructed public surface must be reviewed, and only then may the approved fingerprint and `docs/PUBLIC_API_BASELINE.md` be updated.
- Final completion requires both target builds, focused and full tests, Advanced Inputs demo/manual checks, real-Windows DPI checks, final cross-control regression, documentation updates, and deliberate public API baseline review.

---

## Stage 8 Prerequisite Gate

Stage 9 is the final component-expansion stage. Do not use DatePicker work to compensate for an unfinished earlier stage.

Before Task 1, verify these predecessor artifacts exist after Stages 5, 6, and 8 have been implemented:

```text
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBox.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs
src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToast.cs
demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs
tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs
```

Run from the repository root:

```powershell
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBox.cs
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapComboBox.cs
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDropdown.cs
Test-Path src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapToast.cs
Test-Path demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs
Test-Path tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs
```

Expected: every command returns `True`. If an artifact is missing, stop and finish the earlier roadmap stage instead of inventing substitute infrastructure inside DatePicker.

Then run the predecessor regression gate:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapNumericBox|BootstrapComboBox|BootstrapDropdown|BootstrapToast|AdvancedInputsDemoForm"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapNumericBox|BootstrapComboBox|BootstrapDropdown|BootstrapToast|AdvancedInputsDemoForm"
```

Expected: both targets pass before DatePicker product code begins.

---

## Native DateTimePicker Behavior Locked by This Plan

Stage 9 deliberately preserves native behavior instead of copying WinForms algorithms into framework code.

Relevant native references for implementers:

- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datetimepicker?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datetimepicker.value?view=windowsdesktop-10.0>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datetimepicker.mindate?view=windowsdesktop-10.0>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datetimepicker.maxdate?view=windowsdesktop-10.0>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datetimepicker.format?view=windowsdesktop-10.0>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datetimepicker.customformat?view=windowsdesktop-10.0>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datetimepicker.showcheckbox?view=windowsdesktop-10.0>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datetimepicker.checked?view=windowsdesktop-10.0>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datetimepicker.valuechanged?view=windowsdesktop-10.0>

Native behavior to characterize on both repository target frameworks before styling:

- `Value` defaults to the current date/time when the native picker is constructed; tests must use a time window rather than an exact hard-coded instant.
- `MinDate` defaults to `1753-01-01 00:00:00`.
- `MaxDate` defaults to the earlier of `9998-12-31 00:00:00` and the maximum supported date of the current culture's calendar; tests should compare wrapper state with a plain native picker under the same culture instead of hard-coding one universal max.
- `Format` defaults to `DateTimePickerFormat.Long`.
- Long/Short/Time formatting follows the user's Windows regional settings.
- `CustomFormat` defaults to `null` and affects display only while `Format == DateTimePickerFormat.Custom`.
- `ShowCheckBox` defaults to `false`.
- `Checked` defaults to `true`.
- With `ShowCheckBox = true`, clearing `Checked` provides native optional-value behavior while `Value` remains a `DateTime`; Stage 9 does not add a nullable value API.
- Setting `Value` outside `[MinDate, MaxDate]` follows native exception behavior.
- Invalid `Format` enum values follow native `InvalidEnumArgumentException` behavior.
- Keyboard date-segment editing, arrow keys, calendar dropdown interaction, Escape behavior, and locale-specific text remain native.
- Runtime differences between `net48` and `net8.0-windows` are handled by parity tests against a plain `DateTimePicker`, not by target-specific wrapper algorithms.

If a characterization test reveals a real target difference, record the difference in the test name/comments and preserve each target's native result unless the roadmap explicitly requires a framework override.

---

## Stage 9 Public Contract

The roadmap contract is retained. Do not add convenience aliases during implementation.

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

[DefaultProperty(nameof(Value))]
[DefaultEvent(nameof(ValueChanged))]
public class BootstrapDatePicker : UserControl
{
    public DateTime Value { get; set; }
    public DateTime MinDate { get; set; }
    public DateTime MaxDate { get; set; }
    public DateTimePickerFormat Format { get; set; }
    public string CustomFormat { get; set; }
    public bool ShowCheckBox { get; set; }
    public bool Checked { get; set; }
    public BootstrapValidationState ValidationState { get; set; }
    public int BorderRadius { get; set; }

    public event EventHandler? ValueChanged;
}
```

`CustomFormat` retains the roadmap CLR type `System.String`. Native WinForms permits a `null` value. If nullable analysis on `net8.0-windows` requires an annotation, use a code-analysis annotation/nullable annotation that does not create an additional property or normalize native null into framework state.

### Defaults and validation

| Member | Default / rule |
| --- | --- |
| `Value` | native `DateTimePicker` construction value (`DateTime.Now`-based) |
| `MinDate` | native default (`1753-01-01`) |
| `MaxDate` | native culture-aware default |
| `Format` | `DateTimePickerFormat.Long` |
| `CustomFormat` | native default `null` |
| `ShowCheckBox` | `false` |
| `Checked` | `true` |
| `ValidationState` | `BootstrapValidationState.None`; undefined values rejected before mutation |
| `BorderRadius` | `-1`; values below `-1` rejected |
| wrapper `TabStop` | `true` |
| native child `TabStop` | `false` |
| wrapper accessibility | `AccessibleRole.DropList` with a concise date-picker description |

### Public behavior

- `Value`, `MinDate`, `MaxDate`, `Format`, `CustomFormat`, `ShowCheckBox`, and `Checked` are direct forwarding properties.
- Native property exceptions remain visible to callers. The wrapper must not catch and translate them into different exception types.
- `ValueChanged` is emitted with the `BootstrapDatePicker` wrapper as `sender`, but only in response to the single native `ValueChanged` event path.
- A no-op native assignment must not receive an extra wrapper-generated event.
- If changing native range or checkbox state causes native `ValueChanged`, the wrapper forwards that event exactly as the native control reports it; tests compare with a plain native picker when behavior is subtle.
- `ValidationState` affects only framework shell presentation and never changes native value validity/range rules.
- `BorderRadius` affects only the framework-owned outer shell. The native picker/calendar may retain OS geometry inside/above that shell.
- The outer control owns one focus path. Tabbing to the wrapper focuses the native picker; normal Tab/Shift+Tab leaves the composite without exposing the child as a second tab stop.
- Native key events are forwarded through inherited wrapper events so application code can observe the composite without depending on its private child.
- `Enabled = false` uses the disabled shell treatment and naturally disables the contained picker through WinForms containment.
- The control does not expose the native child as a public property merely for tests.

### Explicitly unsupported/new scope not added here

Do not add any of the following during Stage 9:

- `ShowUpDown` wrapper property.
- `DateTime? Value`, `Nullable`, `HasValue`, or placeholder-based null modeling.
- Date range / two-ended picker.
- Multi-date selection.
- Week-number display.
- Custom day/month/year cell templates.
- Custom calendar header/navigation buttons.
- Custom popup `Form`, `MonthCalendar`, `ToolStripDropDown`, or `BootstrapDropdown` composition.
- Calendar popup animation.
- A custom date parser or culture property.
- Framework-specific date format enum.
- Leading/trailing icons or clear button.
- Text editing API that bypasses the native picker.
- Rounded or themed OS popup-window chrome.
- Win32 hooks, private message interception, reflection into native child windows, or undocumented `DateTimePicker` internals.

These require a separate API/design decision after Stage 9.

---

## Internal Rendering and Layout Contract

Keep deterministic shell calculations in `BootstrapDatePickerRenderLogic.cs` so visual policy is testable without opening a native calendar.

### Metrics

```csharp
internal readonly struct BootstrapDatePickerMetrics
{
    public BootstrapDatePickerMetrics(
        int shellPadding,
        float borderWidth,
        float focusBorderWidth,
        float radius)
    {
        ShellPadding = shellPadding;
        BorderWidth = borderWidth;
        FocusBorderWidth = focusBorderWidth;
        Radius = radius;
    }

    public int ShellPadding { get; }
    public float BorderWidth { get; }
    public float FocusBorderWidth { get; }
    public float Radius { get; }
}
```

Pure helper:

```csharp
internal static BootstrapDatePickerMetrics ResolveMetrics(
    BootstrapThemeMetrics metrics,
    int dpi,
    int borderRadius)
```

Rules:

```text
ShellPadding     = Metrics.SpacingXS
BorderWidth      = Metrics.BorderWidth
FocusBorderWidth = Metrics.FocusBorderWidth
Radius           = BorderRadius >= 0 ? BorderRadius : Metrics.Radius
```

- `metrics == null` throws `ArgumentNullException`.
- `dpi <= 0` throws `ArgumentOutOfRangeException`.
- `borderRadius < -1` throws `ArgumentOutOfRangeException`.
- Logical theme metrics scale exactly once through existing `DpiScaler`.

### Palette

```csharp
internal readonly struct BootstrapDatePickerPalette
{
    public BootstrapDatePickerPalette(Color surface, Color foreground, Color border)
    {
        Surface = surface;
        Foreground = foreground;
        Border = border;
    }

    public Color Surface { get; }
    public Color Foreground { get; }
    public Color Border { get; }
}
```

Pure helper:

```csharp
internal static BootstrapDatePickerPalette ResolvePalette(
    BootstrapThemeColors colors,
    BootstrapValidationState validationState,
    bool containsFocus,
    bool enabled)
```

Rules:

```text
surface: enabled ? colors.Surface : colors.SurfaceSecondary
foreground: enabled ? colors.Text : colors.MutedText
border: BootstrapTextBoxRenderLogic.ResolveBorderColor(
    colors,
    validationState,
    containsFocus,
    enabled)
```

Do not copy the validation-priority switch into DatePicker.

### Native picker bounds

```csharp
internal static Rectangle CalculateNativeBounds(
    Size clientSize,
    int nativePreferredHeight,
    BootstrapDatePickerMetrics metrics)
```

Rules:

- Non-positive client width/height returns `Rectangle.Empty`.
- `nativePreferredHeight <= 0` throws `ArgumentOutOfRangeException`.
- The native picker fills the available inner width after `ShellPadding` on each side.
- Native height is `min(nativePreferredHeight, clientSize.Height - 2 * ShellPadding)` when usable area is positive.
- The native picker is vertically centered in the wrapper.
- Very narrow/small controls clamp to non-negative contained rectangles and never throw merely because the caller resized aggressively.
- Do not assume any internal textbox/dropdown-button rectangle inside `DateTimePicker`.

### Shell painting

`BootstrapDatePicker.OnPaint` owns only the outer framework surface:

1. Resolve current theme and effective DPI.
2. Resolve metrics and palette.
3. Choose `FocusBorderWidth` when `ContainsFocus`, otherwise `BorderWidth`.
4. Inset the paint rectangle by half the stroke so the border stays inside client bounds.
5. Fill the rounded shell with `palette.Surface`.
6. Stroke it with `palette.Border`.
7. Restore `Graphics.SmoothingMode` and dispose the scoped `GraphicsPath`, `Brush`, and `Pen`.
8. Never paint over the native child rectangle and never paint the calendar popup.

The native child receives the current wrapper font. Assigning native `BackColor`/`ForeColor` is permitted as best-effort supported WinForms configuration, but tests must not require OS visual-style implementations to honor every color pixel.

---

## Focus, Event, Theme, DPI, and Resource Ownership

The constructor follows the composed-input pattern already used by `BootstrapTextBox`/planned NumericBox:

```csharp
private readonly DateTimePicker _picker = new DateTimePicker();
private BootstrapValidationState _validationState = BootstrapValidationState.None;
private int _borderRadius = -1;
private bool _themeSubscribed;
private bool _settingThemeFont;
private bool _useThemeFont = true;
private Font? _themeFont;
```

Configure the native picker once:

```csharp
_picker.TabStop = false;
_picker.Margin = Padding.Empty;
_picker.ShowUpDown = false;
_picker.ValueChanged += OnPickerValueChanged;
_picker.GotFocus += OnPickerGotFocus;
_picker.LostFocus += OnPickerLostFocus;
_picker.KeyDown += OnPickerKeyDown;
_picker.KeyPress += OnPickerKeyPress;
_picker.KeyUp += OnPickerKeyUp;
_picker.PreviewKeyDown += OnPickerPreviewKeyDown;
Controls.Add(_picker);
```

The native child is owned by normal WinForms containment and is not exposed publicly.

### Event path

```csharp
private void OnPickerValueChanged(object? sender, EventArgs e)
{
    ValueChanged?.Invoke(this, e);
}
```

Do not call `ValueChanged` from any forwarding property setter.

### Focus path

- Constructor enables `ControlStyles.Selectable` and sets wrapper `TabStop = true`.
- `OnEnter` calls a private `FocusPicker()` helper.
- `OnMouseDown` focuses the native picker when enabled.
- Native `GotFocus`/`LostFocus` only invalidate the shell; `ContainsFocus` remains the source of truth for border presentation.
- `KeyDown`, `KeyPress`, `KeyUp`, and `PreviewKeyDown` from the native picker route through the wrapper's protected `On...` methods.
- Do not intercept native arrow keys, F4/Alt+Down, Escape, segment navigation, or date typing.

### Theme/font lifecycle

Use the same ownership pattern as `BootstrapTextBox`:

- Subscribe once to `BootstrapThemeManager.ThemeChanged`.
- Create a theme body font from `BootstrapThemeManager.CurrentTheme.Typography.Body`.
- Assign the wrapper font and native picker font together.
- On caller `Font` assignment, set `_useThemeFont = false`, dispose only the old framework-owned font, keep the caller font alive, relayout, and repaint.
- Runtime theme changes recreate/dispose only framework-owned fonts and call `ApplyTheme()`, `PerformLayout()`, and `Invalidate()`.
- `ApplyTheme()` updates the wrapper shell and best-effort native foreground/background without touching native date state.
- `Dispose(bool)` unsubscribes from the theme manager and disposes `_themeFont`; normal control containment disposes `_picker`.

### DPI/layout lifecycle

- Initial wrapper width should follow the existing composed-input convention (`240` logical pixels is acceptable); height must be at least the theme `ControlHeight` and large enough for the native picker plus scaled shell padding.
- `OnLayout` uses `CalculateNativeBounds(...)` with the actual native preferred/current height rather than assuming one hard-coded pixel height.
- `OnDpiChangedAfterParent` relayouts and invalidates.
- Theme/font changes relayout because native `DateTimePicker` preferred height can change with font/system metrics.
- Long/custom text does not automatically widen the public control; the application owns Width.

---

## File Map

**Create product files**

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePicker.cs` — public wrapper, native forwarding, focus/event routing, theme/font/DPI/lifecycle, shell paint.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePickerRenderLogic.cs` — pure metrics, palette, and child-bounds calculations.

**Create tests**

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerRenderLogicTests.cs` — pure validation/palette/metrics/layout tests.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs` — native characterization, public contract, forwarding, event/focus/keyboard, range, checkbox, theme/font/DPI/lifecycle, paint smoke, and interactive-calendar characterization.

**Modify shared demo/tests**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs` — append DatePicker scenarios to the existing NumericBox/ComboBox page.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs` — assert DatePicker scenarios without adding another top-level demo page.

**Modify documentation/public baseline**

- `docs/COMPONENTS.md`
- `docs/TESTING.md`
- `docs/COMPATIBILITY.md`
- `README.md`
- `docs/PACKAGE_README.md`
- `CHANGELOG.md`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- `docs/PUBLIC_API_BASELINE.md`

No project-file edit should be necessary for SDK-style default `Compile` inclusion unless implementation discovers an existing explicit include policy; do not edit a `.csproj` speculatively.

---

### Task 1: Characterize native DateTimePicker behavior before styling

**Files:**
- Modify/Create test content in: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs`

**Interfaces:**
- Consumes: native `System.Windows.Forms.DateTimePicker` only.
- Produces: executable parity expectations that later wrapper tests use as the behavior oracle.

- [ ] **Step 1: Add an STA fixture and native-default characterization.**

```csharp
using System;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapDatePickerTests
{
    [Test]
    public void NativeDefaultsAreCharacterizedForStage9()
    {
        var before = DateTime.Now;
        using var native = new DateTimePicker();
        var after = DateTime.Now;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Value, Is.InRange(before, after));
            Assert.That(native.MinDate, Is.EqualTo(new DateTime(1753, 1, 1)));
            Assert.That(native.Format, Is.EqualTo(DateTimePickerFormat.Long));
            Assert.That(native.CustomFormat, Is.Null);
            Assert.That(native.ShowCheckBox, Is.False);
            Assert.That(native.Checked, Is.True);
            Assert.That(native.ShowUpDown, Is.False);
        }));
    }
}
```

- [ ] **Step 2: Characterize range/format/checkbox semantics on native WinForms.** Add tests that verify: a known in-range `Value`; out-of-range assignment throws; `Format = Custom` plus a concrete `CustomFormat`; `ShowCheckBox = true`; `Checked` toggles false/true; undefined `DateTimePickerFormat` throws the native exception.

Use a fixed in-range sample such as `new DateTime(2026, 8, 28, 10, 30, 0)` after setting `MinDate = new DateTime(2020, 1, 1)` and `MaxDate = new DateTime(2030, 12, 31)` so the test is independent of today's clock after construction.

- [ ] **Step 3: Run characterization on both targets before product code exists.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapDatePickerTests&Name~Native"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapDatePickerTests&Name~Native"
```

Expected: native characterization passes on both targets. If a target differs, refine the test to compare/document the actual native difference; do not alter WinForms behavior in advance.

- [ ] **Step 4: Commit the native behavior lock.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs
git commit -m "test: characterize native DateTimePicker behavior"
```

---

### Task 2: Add pure DatePicker shell metrics, palette, and layout logic

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePickerRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerRenderLogicTests.cs`

**Interfaces:**
- Consumes: `BootstrapThemeMetrics`, `BootstrapThemeColors`, `BootstrapValidationState`, `BootstrapTextBoxRenderLogic`, `DpiScaler`.
- Produces: `BootstrapDatePickerMetrics`, `BootstrapDatePickerPalette`, `ResolveMetrics(...)`, `ResolvePalette(...)`, `CalculateNativeBounds(...)`.

- [ ] **Step 1: Write failing metric validation/scaling tests.** Cover DPI `96/120/144/168/192`, theme-radius sentinel, explicit radius, invalid DPI, and radius below `-1`.

Representative assertion:

```csharp
[TestCase(96)]
[TestCase(120)]
[TestCase(144)]
[TestCase(168)]
[TestCase(192)]
public void ResolveMetricsScalesThemeTokens(int dpi)
{
    var theme = BootstrapThemeManager.CurrentTheme;

    var actual = BootstrapDatePickerRenderLogic.ResolveMetrics(theme.Metrics, dpi, -1);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(actual.ShellPadding, Is.EqualTo(DpiScaler.Scale(theme.Metrics.SpacingXS, dpi)));
        Assert.That(actual.Radius, Is.EqualTo(DpiScaler.Scale((float)theme.Metrics.Radius, dpi)));
    }));
}
```

- [ ] **Step 2: Run the focused pure tests and confirm they fail because the render helper does not exist.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapDatePickerRenderLogicTests"
```

Expected: compile/test failure referencing missing DatePicker render types.

- [ ] **Step 3: Implement the minimal metric structs/helpers exactly as defined in the Internal Rendering and Layout Contract.** Reuse `DpiScaler`; validate arguments before returning state.

- [ ] **Step 4: Add failing palette priority tests.** Assert neutral, focused, valid, invalid, and disabled cases under both Light and Dark theme color sets. The test must prove disabled beats validation and validation beats focus.

- [ ] **Step 5: Implement `ResolvePalette(...)` by delegating border resolution to `BootstrapTextBoxRenderLogic.ResolveBorderColor(...)`.** Do not copy the state switch.

- [ ] **Step 6: Add failing child-layout tests.** Cover normal `240x32`, narrow width, tiny height, non-positive client size, invalid preferred height, and a larger 200% DPI metric set. Assert all non-empty native bounds remain inside the wrapper client rectangle.

- [ ] **Step 7: Implement `CalculateNativeBounds(...)` and rerun pure tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapDatePickerRenderLogicTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapDatePickerRenderLogicTests"
```

Expected: PASS on both targets.

- [ ] **Step 8: Commit pure DatePicker rendering policy.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePickerRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerRenderLogicTests.cs
git commit -m "test: define BootstrapDatePicker shell policy"
```

---

### Task 3: Implement native state forwarding and the public contract

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePicker.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs`

**Interfaces:**
- Consumes: native `DateTimePicker`; render logic from Task 2.
- Produces: the exact Stage 9 public API and exactly one private native picker.

- [ ] **Step 1: Write failing contract/default tests.** Assert `DefaultProperty(Value)`, `DefaultEvent(ValueChanged)`, exactly one child `DateTimePicker`, wrapper/child tab-stop policy, `ValidationState.None`, `BorderRadius=-1`, `ShowCheckBox=false`, `Checked=true`, `Format=Long`, and native-aligned min/max/default custom format.

Use `input.Controls.OfType<DateTimePicker>().Single()` only in tests; do not expose the child publicly.

- [ ] **Step 2: Write failing forwarding/parity tests.** Configure wrapper and a plain native peer with identical min/max/value/format/custom-format/checkbox state and assert public wrapper state matches the native peer after each operation.

Representative state test:

```csharp
var sample = new DateTime(2026, 8, 28, 10, 30, 0);
using var input = new BootstrapDatePicker();

input.MinDate = new DateTime(2020, 1, 1);
input.MaxDate = new DateTime(2030, 12, 31);
input.Value = sample;
input.Format = DateTimePickerFormat.Custom;
input.CustomFormat = "yyyy-MM-dd HH:mm";
input.ShowCheckBox = true;
input.Checked = false;

Assert.Multiple((Action)(() =>
{
    Assert.That(input.Value, Is.EqualTo(sample));
    Assert.That(input.CustomFormat, Is.EqualTo("yyyy-MM-dd HH:mm"));
    Assert.That(input.ShowCheckBox, Is.True);
    Assert.That(input.Checked, Is.False);
}));
```

- [ ] **Step 3: Write failing validation/radius tests.** Undefined `BootstrapValidationState` and `BorderRadius=-2` must throw before mutation; valid state/radius updates invalidate the shell without altering native date state.

- [ ] **Step 4: Run focused tests and verify failure before implementation.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapDatePickerTests"
```

Expected: compile/test failure because `BootstrapDatePicker` is not yet implemented.

- [ ] **Step 5: Implement the constructor and direct forwarding properties.** The setter pattern must stay thin:

```csharp
public DateTime Value
{
    get => _picker.Value;
    set => _picker.Value = value;
}

public DateTimePickerFormat Format
{
    get => _picker.Format;
    set => _picker.Format = value;
}

public bool Checked
{
    get => _picker.Checked;
    set => _picker.Checked = value;
}
```

Apply the same direct-forwarding rule to `MinDate`, `MaxDate`, `CustomFormat`, and `ShowCheckBox`.

- [ ] **Step 6: Implement `ValidationState` and `BorderRadius` without touching native date state.** Reuse `BootstrapTextBoxRenderLogic.ValidateState(...)`; reject invalid radius before assignment.

- [ ] **Step 7: Run focused tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapDatePickerTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapDatePickerTests"
```

Expected: contract and native forwarding pass on both targets.

- [ ] **Step 8: Commit the public/native delegation layer.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePicker.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs
git commit -m "feat: add BootstrapDatePicker native delegation"
```

---

### Task 4: Add single-path focus, keyboard forwarding, and exactly-once ValueChanged

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePicker.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs`

**Interfaces:**
- Consumes: the private native picker from Task 3.
- Produces: wrapper-observable `ValueChanged` and inherited keyboard events without duplicate focus/tab stops.

- [ ] **Step 1: Write failing `ValueChanged` tests.** Verify one effective `Value` change produces one event with wrapper sender, repeating the same value produces no wrapper-added duplicate, and range-driven/native changes match a plain peer's event count when native behavior changes the value.

- [ ] **Step 2: Implement the single event route.**

```csharp
private void OnPickerValueChanged(object? sender, EventArgs e)
{
    ValueChanged?.Invoke(this, e);
}
```

No forwarding property setter may invoke the event.

- [ ] **Step 3: Write failing keyboard-forwarding tests** using the same protected-event reflection pattern already present in `BootstrapTextBoxTests`: raise native `OnKeyDown`, `OnKeyPress`, `OnKeyUp`, and `OnPreviewKeyDown`, then assert wrapper subscribers receive each event once and handled/input-key flags propagate.

- [ ] **Step 4: Implement native keyboard forwarding through wrapper `On...` methods.** Do not special-case date keys.

- [ ] **Step 5: Write failing focus/tab tests.** Host the wrapper in a small Form with a control before and after it. Assert the wrapper is the only DatePicker tab stop, entering/focusing the wrapper transfers focus to the native picker, and `ContainsFocus` represents the composite state.

- [ ] **Step 6: Implement `OnEnter`, `OnMouseDown`, native focus invalidation, and `FocusPicker()`.** Do not change the native child's `TabStop=false` rule.

- [ ] **Step 7: Add accessibility assertions.** Wrapper role is `AccessibleRole.DropList`; set a concise default `AccessibleDescription` such as `"Bootstrap-inspired date picker."`; do not replace the native child's own accessibility object.

- [ ] **Step 8: Run focused tests on both targets and commit.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapDatePickerTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapDatePickerTests"
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePicker.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs
git commit -m "feat: add BootstrapDatePicker interaction routing"
```

---

### Task 5: Implement shell painting, theme font, DPI layout, and lifecycle ownership

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePicker.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs`

**Interfaces:**
- Consumes: `BootstrapDatePickerRenderLogic`, current theme manager/tokens, existing rendering helpers.
- Produces: themed outer shell and deterministic resource lifecycle without altering native calendar semantics.

- [ ] **Step 1: Write failing layout tests.** Set representative wrapper sizes, call `PerformLayout()`, and assert the one native child matches `CalculateNativeBounds(...)`, stays inside client bounds, and remains usable after font changes and narrow resize.

- [ ] **Step 2: Implement `OnLayout` using the pure helper.** Use native preferred/current height; do not inspect internal child windows.

- [ ] **Step 3: Write failing palette/paint smoke tests.** Exercise Light/Dark, `None`/`Valid`/`Invalid`, focused/unfocused, enabled/disabled, theme/explicit radius, and small/normal sizes using `DrawToBitmap` only to prove painting does not throw; do not pixel-assert OS-owned native text/button chrome.

- [ ] **Step 4: Implement `OnPaint` for the framework shell.** Use `RoundedPath.Create`, `CornerRadius`, scoped `SolidBrush`/`Pen`, and restore `SmoothingMode` in `finally`.

- [ ] **Step 5: Write failing theme/font ownership tests.** Cover runtime Light -> Dark -> Light, framework-created font replacement, caller-assigned font preservation, and disposal after theme switches.

- [ ] **Step 6: Implement theme subscription/font ownership following `BootstrapTextBox`.** `ApplyTheme()` may set native foreground/background best-effort but must never depend on those pixels for correctness.

- [ ] **Step 7: Write/implement DPI reaction.** `OnDpiChangedAfterParent` calls `PerformLayout()` + `Invalidate()`; pure logic already covers 96/120/144/168/192. Add a testable subclass only if protected DPI triggering is already an established repository test pattern; do not expose a public DPI API for tests.

- [ ] **Step 8: Add disposal/resource stress.** Repeatedly create, theme-switch, lay out, draw, and dispose DatePickers; assert no duplicate native children/events and no use-after-dispose exception from theme callbacks. Resource-count checks may reuse any existing hardening helper; do not introduce a DatePicker-specific global counter.

- [ ] **Step 9: Run focused tests and build product on both targets.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapDatePicker"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapDatePicker"
```

Expected: both builds and focused tests pass.

- [ ] **Step 10: Commit themed shell/lifecycle work.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapDatePicker.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs
git commit -m "feat: theme BootstrapDatePicker shell"
```

---

### Task 6: Add STA native-interaction parity and calendar-popup coverage

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs`

**Interfaces:**
- Consumes: complete DatePicker wrapper from Tasks 3-5.
- Produces: proof that the wrapper does not intercept native date/checkbox/calendar behavior.

- [ ] **Step 1: Add deterministic native-parity tests for min/max boundaries.** Configure a wrapper and plain native peer with the same range/value, then perform the same boundary assignments and compare resulting value/range or exception types.

- [ ] **Step 2: Add checkbox parity tests.** With `ShowCheckBox=true`, toggle `Checked=false/true` on wrapper and native peer and compare visible public state plus `ValueChanged` event counts rather than assuming checkbox toggles always produce the same event sequence on every runtime.

- [ ] **Step 3: Add culture-sensitive format characterization without hard-coded localized strings.** Temporarily select a known installed culture only if the test process supports it, configure wrapper/native peers identically, and compare their `Text`; restore the original culture in `finally`. If a specific culture is unavailable on a build agent, use the current culture and assert wrapper/native equality rather than skipping all formatting coverage.

- [ ] **Step 4: Add a shown-Form focus/keyboard smoke test.** Verify Tab entry focuses the native picker and ordinary keyboard changes remain native. Do not use arbitrary sleeps; use deterministic control/event triggering where possible.

- [ ] **Step 5: Add calendar open/close characterization at the correct reliability level.** Automated CI tests should attach to the native child's `DropDown`/`CloseUp` events and verify the wrapper does not suppress them. If the repository has an interactive-desktop test convention, add an explicit interactive STA smoke that shows a Form, focuses the native picker, opens the calendar with the native keyboard path, closes with Escape, and confirms the control remains focused/usable. Mark any test that genuinely requires an interactive desktop as explicit/manual rather than making headless CI flaky.

- [ ] **Step 6: Run both target suites.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapDatePicker"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapDatePicker"
```

Expected: deterministic tests pass on both targets; explicit interactive calendar smoke is run manually on an interactive Windows desktop before Stage 9 completion.

- [ ] **Step 7: Commit native-interaction coverage.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapDatePickerTests.cs
git commit -m "test: cover BootstrapDatePicker native interaction"
```

---

### Task 7: Extend the shared Advanced Inputs demo with DatePicker scenarios

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs`

**Interfaces:**
- Consumes: `BootstrapDatePicker` plus the existing Advanced Inputs page created by NumericBox and extended by ComboBox.
- Produces: one shared manual/visual page covering the roadmap's complete advanced-input group.

- [ ] **Step 1: Write failing demo tests before adding UI.** Assert the form contains DatePicker examples for: Long, Short, Custom, min/max constrained, checkbox checked/unchecked, valid, invalid, disabled, and a runtime-observable current-value example. Do not add a new MainForm navigation entry because Advanced Inputs already exists.

- [ ] **Step 2: Add DatePicker demo sections with stable labels.** Use a sufficiently wide control for Long/custom formats. Recommended scenarios:

```text
DatePicker / Long
DatePicker / Short
DatePicker / Custom yyyy-MM-dd
DatePicker / Limited Range
DatePicker / Optional Checked
DatePicker / Optional Unchecked
DatePicker / Valid
DatePicker / Invalid
DatePicker / Disabled
```

The limited-range sample uses fixed deterministic bounds and an in-range value rather than `DateTime.Today` so screenshots/tests remain stable.

- [ ] **Step 3: Add one small event-observation label** updated from `ValueChanged` so manual testing can confirm keyboard/calendar selection flows through the public wrapper event. Do not turn the demo into business logic.

- [ ] **Step 4: Preserve shared page behavior.** NumericBox and ComboBox demos remain intact; layout must not create a second Advanced Inputs page or duplicate global theme controls.

- [ ] **Step 5: Run demo tests and demo build on both targets supported by the demo project.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "AdvancedInputsDemoForm|BootstrapDatePicker"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "AdvancedInputsDemoForm|BootstrapDatePicker"
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release
```

Expected: demo tests/build pass and no existing Advanced Inputs scenario is removed.

- [ ] **Step 6: Run manual Advanced Inputs matrix on Windows.** Verify:

```text
Format: Long / Short / Custom
Range: min / middle / max
Optional: ShowCheckBox checked / unchecked
State: neutral / valid / invalid / disabled
Theme: Light / Dark / Light->Dark / Dark->Light
DPI: 100 / 125 / 150 / 175 / 200%
Keyboard: Tab / Shift+Tab / arrow or segment editing / calendar-open key / Escape
Calendar: open / select / close
Locale: current Windows regional setting plus at least one different regional setting when available
Resize: normal width / narrow width / wide long-format width
```

Expected: native text/calendar behavior remains usable, shell border stays aligned, no duplicate tab stop appears, long/custom format clipping occurs only when the demo/application intentionally makes the control too narrow, and switching theme does not reset date state.

- [ ] **Step 7: Commit demo integration.**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs
git commit -m "demo: add BootstrapDatePicker scenarios"
```

---

### Task 8: Update documentation and deliberately review the public API baseline

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
- Consumes: completed and tested public DatePicker contract.
- Produces: user-facing component guidance, explicit OS-owned calendar limitations, test matrix, changelog entry, and approved frozen API fingerprint.

- [ ] **Step 1: Update `docs/COMPONENTS.md`.** Document the exact public contract, forwarding semantics, validation/radius behavior, one-tab-stop composition, checkbox optional-value pattern, and unsupported custom-calendar/range-picker scope.

- [ ] **Step 2: Update `docs/TESTING.md`.** Add DatePicker pure/STA/manual coverage and the 100/125/150/175/200% real-Windows DPI matrix. Explicitly state that pixel assertions do not cover OS-owned calendar/native text-button chrome.

- [ ] **Step 3: Update `docs/COMPATIBILITY.md`.** Record that the wrapper preserves native `DateTimePicker` behavior on both target frameworks, locale/regional output is OS/native, the popup is OS-owned, and target differences are characterized through parity tests rather than wrapper forks.

- [ ] **Step 4: Update README/package README/changelog.** Add DatePicker to supported controls and summarize native-backed behavior without claiming custom calendar theming.

- [ ] **Step 5: Run the frozen baseline test before changing the baseline.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

Expected: FAIL because the approved fingerprint does not yet contain `BootstrapDatePicker`.

- [ ] **Step 6: Review the reconstructed exported API.** Confirm it contains only the intended Stage 9 public surface and no leaked private helper/native-child type. The intended new declared surface is:

```text
MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapDatePicker
  Value : System.DateTime
  MinDate : System.DateTime
  MaxDate : System.DateTime
  Format : System.Windows.Forms.DateTimePickerFormat
  CustomFormat : System.String
  ShowCheckBox : System.Boolean
  Checked : System.Boolean
  ValidationState : BootstrapValidationState
  BorderRadius : System.Int32
  ValueChanged : System.EventHandler
```

If reflection shows any extra public/protected member introduced solely for testing/native access, remove it before updating the baseline.

- [ ] **Step 7: Update the approved fingerprint and `docs/PUBLIC_API_BASELINE.md` only after the surface review.** Do not mask unrelated public API drift.

- [ ] **Step 8: Run baseline tests on both targets and commit docs/API review.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
git add docs/COMPONENTS.md docs/TESTING.md docs/COMPATIBILITY.md README.md docs/PACKAGE_README.md CHANGELOG.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs docs/PUBLIC_API_BASELINE.md
git commit -m "docs: document BootstrapDatePicker"
```

Expected: both baseline tests pass after deliberate approval.

---

### Task 9: Run final Stage 9 cross-control regression and completion gate

**Files:**
- No new files expected. Fix only regressions directly caused by Stage 9 and include those files in the final commit if needed.

**Interfaces:**
- Consumes: all Stage 1-9 component-expansion work.
- Produces: a green final expansion roadmap state with DatePicker integrated but no duplicate infrastructure.

- [ ] **Step 1: Run the roadmap's final input/focus regression.** On the Advanced Inputs page, verify tab sequence across `BootstrapTextBox` -> `BootstrapNumericBox` -> `BootstrapComboBox` -> `BootstrapDatePicker` produces exactly one stop per composite/input and Shift+Tab reverses predictably.

- [ ] **Step 2: Run adjacent-component regression.** Verify Tooltip associations still work on/around advanced inputs, Dropdown next to DatePicker opens/dismisses normally, and a Toast triggered from an input/value-change demo action does not affect DatePicker focus/value state.

- [ ] **Step 3: Stress theme/disposal cycles.** Repeated Light/Dark switches with creation/disposal of DatePicker plus earlier input controls must not create duplicate native children, duplicate events, ObjectDisposedException from theme callbacks, or obvious GDI/USER handle growth.

- [ ] **Step 4: Build the library for both target frameworks.**

```powershell
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
dotnet build src/MyDmsVn.Bootstrap5WinFormUI/MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
```

Expected: both builds succeed with zero warnings promoted to errors.

- [ ] **Step 5: Run focused Stage 9 tests on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "BootstrapDatePicker|AdvancedInputsDemoForm|Phase16PublicApiBaselineTests"
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "BootstrapDatePicker|AdvancedInputsDemoForm|Phase16PublicApiBaselineTests"
```

Expected: PASS on both targets.

- [ ] **Step 6: Run the full test suite on both targets.**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
```

Expected: full suite PASS on both targets.

- [ ] **Step 7: Run the integrated demo/manual gate.** Launch the demo, select **Advanced Inputs**, execute the Task 7 matrix, then verify the final Stage 9 cross-control scenarios from Steps 1-3 on an interactive Windows desktop.

- [ ] **Step 8: Inspect repository diff and public API one last time.** Confirm there is no custom calendar implementation, new package, duplicate validation helper, native-child public escape hatch, global hook, or unplanned public API.

```powershell
git status --short
git diff --check
git diff --stat
```

Expected: clean whitespace check and only Stage 9-related changes.

- [ ] **Step 9: Commit any final Stage 9 regression fixes, if required.** If no fixes were needed after Task 8, do not create an empty commit. If fixes were required:

```powershell
git add src tests demo docs README.md CHANGELOG.md
git commit -m "fix: harden BootstrapDatePicker integration"
```

---

## Stage 9 Definition of Done

Stage 9 is complete only when all of the following are true:

- `BootstrapDatePicker` composes exactly one native `DateTimePicker`.
- Native date/range/format/checkbox/calendar behavior remains authoritative on both target frameworks.
- Wrapper `ValueChanged` has exactly one native source path.
- Wrapper owns one tab stop and native child owns zero additional tab stops.
- Validation/focus/disabled priority reuses established input semantics.
- Theme/default and explicit radii are DPI-scaled through existing helpers.
- Runtime theme switching preserves value/range/format/checked state.
- Framework-created fonts/theme subscriptions are cleaned up; caller-owned fonts remain caller-owned.
- OS calendar popup is documented as native and is not force-themed.
- No `ShowUpDown`, nullable `Value`, range picker, custom calendar, timer, animation, external package, or unsupported Win32 theming layer was added.
- Advanced Inputs demo includes all required DatePicker scenarios without adding duplicate top-level navigation.
- Manual Light/Dark, locale, keyboard/calendar, and 100/125/150/175/200% DPI checks pass.
- Final TextBox/NumericBox/ComboBox/DatePicker tab sequence and Tooltip/Dropdown/Toast adjacency regressions pass.
- `docs/COMPONENTS.md`, `docs/TESTING.md`, `docs/COMPATIBILITY.md`, `README.md`, `docs/PACKAGE_README.md`, and `CHANGELOG.md` reflect Stage 9.
- Public API baseline was intentionally failed, reviewed, approved, and regenerated with only the intended DatePicker surface.
- Product builds pass for `net48` and `net8.0-windows`.
- Focused and full NUnit suites pass for both targets.

---

## Self-Review Against Stage 9 Roadmap

- **9.1 Characterize native date behavior:** Task 1 plus Native DateTimePicker Behavior section covers value/min/max, built-in/custom format, checkbox/checked, locale, keyboard/calendar ownership.
- **9.2 Wrapper contract tests:** Tasks 3-4 cover forwarded state/events, one public focus path, validation, radius, disabled/designer-safe behavior, and duplicate-event prevention.
- **9.3 Shell layout/palette tests:** Tasks 2 and 5 cover native inset, focus/validation priority, disabled surface, DPI dimensions, font/layout, and non-negative clipping-safe bounds.
- **9.4 Themed shell without intercepting calendar semantics:** Task 5 owns only outer paint/theme/font/layout and explicitly preserves native popup behavior.
- **9.5 STA interaction tests:** Task 6 covers focus, keyboard, range, checkbox, locale, calendar open/close characterization, and runtime theme behavior at an appropriate deterministic/interactive test level.
- **9.6 Advanced Inputs demo:** Task 7 covers Long/Short/Custom, min/max, checked/unchecked, validation, disabled, locale, theme, DPI, keyboard, and calendar manual checks.
- **9.7 Final cross-control regression:** Task 9 covers TextBox/NumericBox/ComboBox/DatePicker tab sequence plus Tooltip, Dropdown, Toast, repeated theme switching, and disposal.
- **9.8 Both targets/docs/API baseline:** Tasks 8-9 provide the documentation, frozen API review, both target builds, focused tests, and full tests.

No Stage 9 roadmap requirement is intentionally deferred by this plan.
