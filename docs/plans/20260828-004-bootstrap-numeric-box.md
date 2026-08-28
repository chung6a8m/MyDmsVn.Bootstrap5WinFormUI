# BootstrapNumericBox Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Stage 5 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md` by adding a Bootstrap-inspired `BootstrapNumericBox` that delegates numeric parsing, formatting, range enforcement, increment/decrement behavior, keyboard arrows, mouse wheel behavior, and value state to one native WinForms `NumericUpDown`, while the framework owns the themed input shell, validation/focus presentation, font lifecycle, DPI-aware geometry, demo coverage, documentation, and public API review.

**Architecture:** `BootstrapNumericBox : UserControl` composes exactly one borderless native `NumericUpDown`. The native control is the sole authority for `Value`, `Minimum`, `Maximum`, `Increment`, `DecimalPlaces`, `ThousandsSeparator`, `ReadOnly`, culture-sensitive numeric text, and `ValueChanged`; wrapper properties forward directly and never mirror numeric state. The outer control owns the single public tab stop, redirects focus into the native editor, paints the rounded Bootstrap surface/border, applies theme/font/DPI state, and reuses the existing `BootstrapValidationState` presentation rules established by `BootstrapTextBox`.

**Tech Stack:** C#, native Windows Forms, existing Theme / Rendering / Compatibility infrastructure, `BootstrapValidationState`, `BootstrapThemeManager`, `DpiScaler`, `RoundedPath`, `CornerRadius`, NUnit 4, SDK-style multi-targeting (`net48;net8.0-windows`). No new external dependency.

**Spec:** Stage 5 of `docs/plans/20260828-002-bootstrap-component-expansion-roadmap.md`, plus repository-wide constraints in `AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPONENTS.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/DEVELOPMENT_PLAN.md`, and `docs/PUBLIC_API_BASELINE.md`.

## Global Constraints

- Root namespace remains `MyDmsVn.Bootstrap5WinFormUI`; the public control remains under `MyDmsVn.Bootstrap5WinFormUI.Controls`.
- Product code must compile from one shared implementation for both `net48` and `net8.0-windows` wherever practical.
- Stage 4 (`BootstrapTabControl`) must be complete and green before Stage 5 implementation begins, preserving roadmap order. `BootstrapNumericBox` itself must not depend on Tabs.
- `BootstrapNumericBox` must contain exactly one native `NumericUpDown`; do not create a custom parser, range model, spin-button implementation, or duplicate numeric backing fields.
- The inner `NumericUpDown` must use `BorderStyle.None`, remain owned by the wrapper, and remain outside the parent tab sequence with `TabStop = false`.
- The outer `BootstrapNumericBox` owns the public tab stop with `TabStop = true` and redirects focus into the native control, matching the established `BootstrapTextBox` composition pattern.
- `Value`, `Minimum`, `Maximum`, `Increment`, `DecimalPlaces`, `ThousandsSeparator`, and `ReadOnly` forward directly to the native control. Native exceptions, clamping, formatting, and effective-change behavior remain authoritative.
- `ValueChanged` is raised from the native `NumericUpDown.ValueChanged` path exactly once for an effective native value change; do not raise a second event from the wrapper property setter.
- The wrapper reuses `BootstrapValidationState`. Do not add a NumericBox-specific validation enum.
- Validation/focus priority follows the established TextBox rule: disabled presentation wins; then valid/invalid semantic border; then focused border; then neutral border.
- `ReadOnly` preserves native WinForms semantics. It prevents typed text editing but still permits changing the value through the native up/down mechanisms. Do not reinterpret `ReadOnly` as `Enabled = false`.
- `BorderRadius = -1` means current theme radius. Values below `-1` throw `ArgumentOutOfRangeException`.
- Theme metrics and colors must come from `BootstrapThemeManager.CurrentTheme`; do not hard-code repeated spacing, border width, focus width, radius, or semantic colors.
- Reuse `DpiScaler`, `RoundedPath`, and `CornerRadius`. Do not add a second DPI scaler or geometry helper.
- Treat `NumericUpDown` as a public WinForms control boundary. Do not depend on undocumented internal child types such as `UpDownEdit` or `UpDownButtons`, and do not use reflection to style their implementation-specific internals.
- Designer construction must work without application bootstrap, DI, service locators, or initialized global state beyond existing safe theme defaults.
- The control must unsubscribe from `BootstrapThemeManager.ThemeChanged` and dispose only fonts it created. A caller-assigned `Font` remains caller-owned.
- The component adds public/protected API after the frozen RC baseline. `Phase16PublicApiBaselineTests` must intentionally fail first, the reconstructed API must be reviewed, and only then may the approved fingerprint and `docs/PUBLIC_API_BASELINE.md` be updated.
- `Hexadecimal`, `Accelerations`, custom spin-button glyphs, custom culture properties, custom parse events, custom text alignment, and custom mouse-wheel policy are outside Stage 5.
- No timer, animation, async scheduler, P/Invoke input hook, top-level popup, or external package belongs in this stage.

---

## Platform Behavior Resolved During Planning

This section records native WinForms behavior that Stage 5 deliberately preserves rather than reimplementing.

Microsoft documents that `NumericUpDown` owns the single numeric value, supports changing it with the up/down buttons, keyboard arrows, or typed input, and formats it with `DecimalPlaces` and `ThousandsSeparator`. Microsoft also documents that `ReadOnly = true` prevents typed editing while the up/down mechanism remains the allowed change path.

Relevant references:

- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.numericupdown?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/numericupdown-control-overview-windows-forms>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.numericupdown.value?view=windowsdesktop-9.0>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.numericupdown.minimum?view=netframework-4.8.1>
- <https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.numericupdown.maximum?view=windowsdesktop-9.0>

Native range behavior is part of the contract:

- Default `Minimum` is `0m`.
- Default `Maximum` is `100m`.
- Default `Value` is `0m`.
- Default `Increment` is `1m`.
- Default `DecimalPlaces` is `0`.
- Default `ThousandsSeparator` is `false`.
- Setting `Value` outside `[Minimum, Maximum]` throws the native `ArgumentOutOfRangeException`.
- Raising `Minimum` above the current `Value` raises the current value to the new minimum; if the new minimum exceeds `Maximum`, native WinForms also moves `Maximum` to that minimum.
- Lowering `Maximum` below the current `Value` lowers the current value to the new maximum; if the new maximum is below `Minimum`, native WinForms also moves `Minimum` to that maximum.
- `DecimalPlaces` validation, regional decimal/group separators, wheel behavior, arrow-key behavior, and spin-button repeat behavior remain native.

Implementation tests should therefore compare wrapper outcomes with a plain `NumericUpDown` when behavior is target/runtime-sensitive instead of copying native algorithms into test helpers.

---

## Stage 5 Public Contract

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

[DefaultProperty(nameof(Value))]
[DefaultEvent(nameof(ValueChanged))]
public class BootstrapNumericBox : UserControl
{
    public decimal Value { get; set; }                 // default 0
    public decimal Minimum { get; set; }               // default 0
    public decimal Maximum { get; set; }               // default 100
    public decimal Increment { get; set; }             // default 1
    public int DecimalPlaces { get; set; }             // default 0
    public bool ThousandsSeparator { get; set; }       // default false
    public bool ReadOnly { get; set; }                 // default false
    public BootstrapValidationState ValidationState { get; set; } // default None
    public int BorderRadius { get; set; }              // default -1

    public event EventHandler? ValueChanged;
}
```

### Public behavior

- `Value`, `Minimum`, `Maximum`, `Increment`, `DecimalPlaces`, `ThousandsSeparator`, and `ReadOnly` are direct forwarding properties; wrapper setters must not implement parallel validation.
- `ValueChanged` uses the wrapper as `sender` but is triggered only by the single native `NumericUpDown.ValueChanged` path.
- Setting `Value` to its current value does not produce a wrapper-generated event.
- Native value changes caused indirectly by `Minimum`, `Maximum`, keyboard/spin actions, wheel actions, or typed edits flow through the same one event path.
- `ValidationState = None` uses focus/neutral border tokens. `Valid` uses `theme.Colors.Success`; `Invalid` uses `theme.Colors.Danger`.
- Disabled state overrides validation/focus border presentation with the disabled token.
- `ReadOnly` uses the secondary surface treatment but remains enabled/focusable and keeps native spinner behavior.
- `BorderRadius = -1` resolves to `theme.Metrics.Radius`; non-negative values are logical 96-DPI values scaled through `DpiScaler`.
- The inner native control remains visually borderless; the outer control paints the only framework border.
- The outer shell uses one public tab stop. Tabbing into the shell redirects focus to the native `NumericUpDown`; normal Tab/Shift+Tab traversal leaves the composite without exposing the inner control as a second stop.
- Keyboard events raised by the inner native control should be forwarded through the wrapper using the same pattern already used by `BootstrapTextBox` for `KeyDown`, `KeyPress`, `KeyUp`, and `PreviewKeyDown` so applications can observe the composite input rather than discovering the implementation child.
- The framework does not replace native accessibility for the spin editor. The wrapper exposes `AccessibleRole.SpinButton` and a concise framework description while the native child remains the actual interactive editor.

### Explicitly unsupported/new scope not added here

Do not add any of the following during Stage 5:

- `Hexadecimal` forwarding.
- `Accelerations` forwarding or a framework acceleration collection.
- `InterceptArrowKeys` or a framework-specific arrow-key policy.
- Custom parse/format callbacks.
- A `Culture` property or custom numeric format string.
- Leading/trailing icons or clear buttons.
- Prefix/suffix text such as currency/unit labels.
- A custom spinner-button renderer or replacement button controls.
- Mouse-wheel suppression or custom wheel increments.
- Nullable/empty numeric state.
- Validation messages/tooltips; only border state is in scope.
- Animation on focus, validation, or value changes.

These can be evaluated later as separate API additions if concrete requirements justify them.

---

## Internal Rendering and Layout Contract

Keep deterministic calculations in `BootstrapNumericBoxRenderLogic.cs` so most visual policy is testable without a WinForms handle.

### Metrics

```csharp
internal readonly struct BootstrapNumericBoxMetrics
{
    public BootstrapNumericBoxMetrics(
        int horizontalPadding,
        float borderWidth,
        float focusBorderWidth,
        float radius)
    {
        HorizontalPadding = horizontalPadding;
        BorderWidth = borderWidth;
        FocusBorderWidth = focusBorderWidth;
        Radius = radius;
    }

    public int HorizontalPadding { get; }
    public float BorderWidth { get; }
    public float FocusBorderWidth { get; }
    public float Radius { get; }
}
```

Token mapping before DPI scaling:

```text
HorizontalPadding = Metrics.SpacingSM
BorderWidth       = Metrics.BorderWidth
FocusBorderWidth  = Metrics.FocusBorderWidth
Radius            = BorderRadius >= 0 ? BorderRadius : Metrics.Radius
```

Pure helper:

```csharp
internal static BootstrapNumericBoxMetrics ResolveMetrics(
    BootstrapThemeMetrics metrics,
    int dpi,
    int borderRadius)
```

Rules:

- `metrics == null` throws `ArgumentNullException`.
- `dpi <= 0` throws `ArgumentOutOfRangeException`.
- `borderRadius < -1` throws `ArgumentOutOfRangeException`.
- All logical values scale through existing `DpiScaler`.

### Palette

```csharp
internal readonly struct BootstrapNumericBoxPalette
{
    public BootstrapNumericBoxPalette(Color background, Color foreground, Color border)
    {
        Background = background;
        Foreground = foreground;
        Border = border;
    }

    public Color Background { get; }
    public Color Foreground { get; }
    public Color Border { get; }
}
```

Pure helper:

```csharp
internal static BootstrapNumericBoxPalette ResolvePalette(
    BootstrapThemeColors colors,
    BootstrapValidationState validationState,
    bool containsFocus,
    bool enabled,
    bool readOnly)
```

Rules:

```text
background: enabled && !readOnly ? colors.Surface : colors.SurfaceSecondary
foreground: enabled ? colors.Text : colors.MutedText
border:
  !enabled                         => colors.Disabled
  validationState == Valid         => colors.Success
  validationState == Invalid       => colors.Danger
  containsFocus                    => colors.Focus
  otherwise                        => colors.Border
```

Reuse `BootstrapTextBoxRenderLogic.ValidateState` / border semantics internally rather than maintaining two divergent validation-priority tables.

### Native editor layout

```csharp
internal static Rectangle CalculateNativeBounds(
    Size clientSize,
    int nativePreferredHeight,
    BootstrapNumericBoxMetrics metrics)
```

Rules:

- Empty/non-positive client dimensions return `Rectangle.Empty`.
- `nativePreferredHeight <= 0` throws `ArgumentOutOfRangeException`.
- Horizontal padding uses `metrics.HorizontalPadding`, reduced only when a very narrow control would otherwise produce a negative width.
- Native width is always inside the shell and positive when the client has usable area.
- Native height is `min(nativePreferredHeight, clientHeight)` and vertically centered.
- The layout helper never assumes or manipulates the native control's internal edit/button rectangles.

### Shell painting

`BootstrapNumericBox.OnPaint` paints only the framework shell:

1. Resolve current theme and current DPI.
2. Resolve metrics and palette.
3. Select `FocusBorderWidth` when `ContainsFocus`, otherwise `BorderWidth`.
4. Inset the paint rectangle by half the stroke so the border remains inside the client bounds.
5. Fill a rounded path using `palette.Background`.
6. Stroke the same path using `palette.Border`.
7. Restore `Graphics.SmoothingMode` and dispose the scoped `GraphicsPath`, `Brush`, and `Pen`.

The native `NumericUpDown` paints its text and spin buttons. The shell must not paint over or replace those native child pixels.

---

## Focus, Event, Theme, and Resource Ownership

### Construction

The constructor should follow the established composed-input pattern:

```csharp
private readonly NumericUpDown _editor = new NumericUpDown();
private BootstrapValidationState _validationState = BootstrapValidationState.None;
private int _borderRadius = -1;
private bool _themeSubscribed;
private bool _settingThemeFont;
private bool _useThemeFont = true;
private Font? _themeFont;
```

Configure the native editor once:

```csharp
_editor.BorderStyle = BorderStyle.None;
_editor.TabStop = false;
_editor.Margin = Padding.Empty;
_editor.ValueChanged += OnEditorValueChanged;
_editor.GotFocus += OnEditorGotFocus;
_editor.LostFocus += OnEditorLostFocus;
_editor.KeyDown += OnEditorKeyDown;
_editor.KeyPress += OnEditorKeyPress;
_editor.KeyUp += OnEditorKeyUp;
_editor.PreviewKeyDown += OnEditorPreviewKeyDown;
Controls.Add(_editor);
```

The wrapper owns the child by normal WinForms containment and must never expose it as a public property merely for tests.

### Value event path

```csharp
private void OnEditorValueChanged(object? sender, EventArgs e)
{
    ValueChanged?.Invoke(this, e);
}
```

Do not call `ValueChanged` from `Value`, `Minimum`, or `Maximum` setters. Native WinForms determines whether those operations caused an effective value transition.

### Focus path

- Constructor sets `TabStop = true` and `ControlStyles.Selectable`.
- `OnEnter` calls a private `FocusEditor()` helper.
- `OnMouseDown` focuses the native editor when enabled.
- Native `GotFocus`/`LostFocus` handlers only invalidate the shell; `ContainsFocus` remains the source of truth for focus painting.
- Key events are forwarded through inherited `OnKeyDown`, `OnKeyPress`, `OnKeyUp`, and `OnPreviewKeyDown`, mirroring `BootstrapTextBox`.

### Theme/font lifecycle

- Subscribe once to `BootstrapThemeManager.ThemeChanged` in the constructor and record ownership with `_themeSubscribed`.
- Create the theme body font from `BootstrapThemeManager.CurrentTheme.Typography.Body` using the same ownership pattern as `BootstrapTextBox`.
- Assign the wrapper `Font` and the native editor `Font` together.
- If the caller assigns `Font`, `OnFontChanged` marks `_useThemeFont = false`, disposes only the previous theme-owned font, applies the caller font to the child, relayouts, and repaints.
- Runtime theme switching recreates/disposes only theme-owned fonts and updates native `BackColor`/`ForeColor`, layout, and shell painting.
- `Dispose(bool)` unsubscribes the theme handler and disposes `_themeFont`; normal WinForms containment disposes `_editor`.

---

## File Structure

**Create product files**

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBoxRenderLogic.cs` — pure DPI metrics, palette, validation, and native-editor layout calculations.
- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBox.cs` — public `UserControl` wrapper, one native `NumericUpDown`, property/event forwarding, focus path, theme/font/DPI lifecycle, and shell painting.

**Modify shared product metadata**

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapValidationState.cs` — broaden XML documentation from TextBox-specific wording to reusable Bootstrap input validation presentation; do not change enum names or values.

**Create tests**

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxRenderLogicTests.cs` — pure metrics/palette/layout/validation tests.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxTests.cs` — STA defaults, native delegation, range semantics, event forwarding, keyboard/focus, wheel characterization, theme/font/DPI/lifecycle, and paint smoke tests.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs` — shared Advanced Inputs demo integration for NumericBox; Stage 6 ComboBox and Stage 9 DatePicker extend this test later.

**Create demo**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs` — first shared Advanced Inputs page containing NumericBox scenarios; later Stage 6 and Stage 9 extend this same form.

**Modify integrated demo**

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs` — add exactly one `Advanced Inputs` navigation entry because Stage 5 is the first control in that roadmap demo group.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoApplicationTests.cs` — add `Advanced Inputs` to required pages and verify selecting it embeds the new form.

**Modify docs/release baseline**

- `docs/COMPONENTS.md` — add finalized `BootstrapNumericBox` contract, native authority, validation/focus rules, and explicit deferred features.
- `docs/TESTING.md` — add Stage 5 pure/STA/manual/DPI/theme/designer coverage.
- `docs/ARCHITECTURE.md` — add `NumericBox -> native NumericUpDown` to the native-backed input dependency description without introducing a dependency on Tabs, ComboBox, or Dropdown.
- `README.md` — list NumericBox support and point users to the integrated Advanced Inputs demo.
- `docs/PACKAGE_README.md` — add package-facing NumericBox capability/usage notes.
- `CHANGELOG.md` — record the compatible NumericBox API addition under `Unreleased` without rewriting release history.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs` — update the approved fingerprint only after deliberate API review.
- `docs/PUBLIC_API_BASELINE.md` — record the reviewed NumericBox exported additions and new fingerprint after the failing gate has been inspected.

---

### Task 1: Freeze deterministic NumericBox shell rendering rules

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBoxRenderLogic.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxRenderLogicTests.cs`

**Interfaces:**
- Consumes: `BootstrapThemeMetrics`, `BootstrapThemeColors`, `BootstrapValidationState`, `BootstrapTextBoxRenderLogic`, and `DpiScaler`.
- Produces: `BootstrapNumericBoxMetrics`, `BootstrapNumericBoxPalette`, `BootstrapNumericBoxRenderLogic.ResolveMetrics(...)`, `ResolvePalette(...)`, and `CalculateNativeBounds(...)` as defined above.

- [ ] **Step 1: Write failing metric/radius tests.**

Add tests that verify the 96/120/144/168/192 DPI matrix and the theme-radius sentinel:

```csharp
[TestCase(96)]
[TestCase(120)]
[TestCase(144)]
[TestCase(168)]
[TestCase(192)]
public void ResolveMetricsScalesThemeTokens(int dpi)
{
    var metrics = BootstrapThemeMetrics.CreateDefault();

    var actual = BootstrapNumericBoxRenderLogic.ResolveMetrics(metrics, dpi, borderRadius: -1);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(actual.HorizontalPadding, Is.EqualTo(DpiScaler.Scale(metrics.SpacingSM, dpi)));
        Assert.That(actual.BorderWidth, Is.EqualTo(DpiScaler.Scale((float)metrics.BorderWidth, dpi)));
        Assert.That(actual.FocusBorderWidth, Is.EqualTo(DpiScaler.Scale((float)metrics.FocusBorderWidth, dpi)));
        Assert.That(actual.Radius, Is.EqualTo(DpiScaler.Scale((float)metrics.Radius, dpi)));
    }));
}

[Test]
public void ResolveMetricsUsesExplicitRadiusAndRejectsInvalidInputs()
{
    var metrics = BootstrapThemeMetrics.CreateDefault();

    Assert.That(
        BootstrapNumericBoxRenderLogic.ResolveMetrics(metrics, 192, 6).Radius,
        Is.EqualTo(DpiScaler.Scale(6f, 192)));
    Assert.Throws<ArgumentOutOfRangeException>(() => BootstrapNumericBoxRenderLogic.ResolveMetrics(metrics, 96, -2));
    Assert.Throws<ArgumentOutOfRangeException>(() => BootstrapNumericBoxRenderLogic.ResolveMetrics(metrics, 0, -1));
    Assert.Throws<ArgumentNullException>(() => BootstrapNumericBoxRenderLogic.ResolveMetrics(null!, 96, -1));
}
```

- [ ] **Step 2: Run the pure test file and verify it fails because the new helper does not exist.**

Run:

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapNumericBoxRenderLogicTests
```

Expected: compile/test failure referencing missing `BootstrapNumericBoxRenderLogic` / related structs.

- [ ] **Step 3: Add failing palette-priority tests.**

```csharp
[Test]
public void ResolvePalettePreservesValidationFocusAndReadOnlyPriority()
{
    var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light);

    var normal = BootstrapNumericBoxRenderLogic.ResolvePalette(
        colors, BootstrapValidationState.None, containsFocus: false, enabled: true, readOnly: false);
    var focused = BootstrapNumericBoxRenderLogic.ResolvePalette(
        colors, BootstrapValidationState.None, containsFocus: true, enabled: true, readOnly: false);
    var valid = BootstrapNumericBoxRenderLogic.ResolvePalette(
        colors, BootstrapValidationState.Valid, containsFocus: true, enabled: true, readOnly: false);
    var invalid = BootstrapNumericBoxRenderLogic.ResolvePalette(
        colors, BootstrapValidationState.Invalid, containsFocus: true, enabled: true, readOnly: false);
    var readOnly = BootstrapNumericBoxRenderLogic.ResolvePalette(
        colors, BootstrapValidationState.None, containsFocus: true, enabled: true, readOnly: true);
    var disabled = BootstrapNumericBoxRenderLogic.ResolvePalette(
        colors, BootstrapValidationState.Invalid, containsFocus: true, enabled: false, readOnly: false);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(normal.Background, Is.EqualTo(colors.Surface));
        Assert.That(normal.Foreground, Is.EqualTo(colors.Text));
        Assert.That(normal.Border, Is.EqualTo(colors.Border));
        Assert.That(focused.Border, Is.EqualTo(colors.Focus));
        Assert.That(valid.Border, Is.EqualTo(colors.Success));
        Assert.That(invalid.Border, Is.EqualTo(colors.Danger));
        Assert.That(readOnly.Background, Is.EqualTo(colors.SurfaceSecondary));
        Assert.That(readOnly.Foreground, Is.EqualTo(colors.Text));
        Assert.That(disabled.Background, Is.EqualTo(colors.SurfaceSecondary));
        Assert.That(disabled.Foreground, Is.EqualTo(colors.MutedText));
        Assert.That(disabled.Border, Is.EqualTo(colors.Disabled));
    }));
}
```

Also assert an undefined `BootstrapValidationState` value is rejected through the shared validation path.

- [ ] **Step 4: Add failing layout tests.**

```csharp
[Test]
public void CalculateNativeBoundsCentersEditorAndKeepsItInsideShell()
{
    var metrics = new BootstrapNumericBoxMetrics(
        horizontalPadding: 8,
        borderWidth: 1f,
        focusBorderWidth: 2f,
        radius: 4f);

    var bounds = BootstrapNumericBoxRenderLogic.CalculateNativeBounds(
        new Size(160, 32),
        nativePreferredHeight: 20,
        metrics);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(bounds.Left, Is.EqualTo(8));
        Assert.That(bounds.Right, Is.EqualTo(152));
        Assert.That(bounds.Height, Is.EqualTo(20));
        Assert.That(bounds.Top, Is.EqualTo(6));
        Assert.That(new Rectangle(Point.Empty, new Size(160, 32)).Contains(bounds), Is.True);
    }));
}

[Test]
public void CalculateNativeBoundsHandlesTinyAndEmptyClients()
{
    var metrics = new BootstrapNumericBoxMetrics(8, 1f, 2f, 4f);

    Assert.That(
        BootstrapNumericBoxRenderLogic.CalculateNativeBounds(Size.Empty, 20, metrics),
        Is.EqualTo(Rectangle.Empty));

    var tiny = BootstrapNumericBoxRenderLogic.CalculateNativeBounds(new Size(5, 10), 20, metrics);
    Assert.That(tiny.Width, Is.GreaterThan(0));
    Assert.That(tiny.Height, Is.EqualTo(10));
    Assert.That(new Rectangle(0, 0, 5, 10).Contains(tiny), Is.True);
    Assert.Throws<ArgumentOutOfRangeException>(() =>
        BootstrapNumericBoxRenderLogic.CalculateNativeBounds(new Size(160, 32), 0, metrics));
}
```

- [ ] **Step 5: Implement the minimal pure render logic.**

Implement the contracts in this plan. `ResolvePalette` should reuse TextBox validation semantics rather than duplicate a divergent enum validator:

```csharp
var border = BootstrapTextBoxRenderLogic.ResolveBorderColor(
    colors,
    validationState,
    containsFocus,
    enabled);
```

`ResolveMetrics` and `CalculateNativeBounds` remain free of WinForms handle access, theme subscriptions, or mutable numeric state.

- [ ] **Step 6: Run the pure tests until green.**

Run the same filtered test command. Expected: all `BootstrapNumericBoxRenderLogicTests` pass on the currently selected target.

- [ ] **Step 7: Commit the deterministic shell logic.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBoxRenderLogic.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxRenderLogicTests.cs
git commit -m "test: define BootstrapNumericBox shell rendering"
```

---

### Task 2: Add the public native-backed NumericBox contract

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBox.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxTests.cs`
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapValidationState.cs`

**Interfaces:**
- Consumes: Task 1 render logic plus one owned native `NumericUpDown`.
- Produces: the Stage 5 public contract shown above and no additional public helper types.

- [ ] **Step 1: Write failing default/composition tests.**

```csharp
[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapNumericBoxTests
{
    [Test]
    public void DefaultsMatchStage5Contract()
    {
        using var input = new BootstrapNumericBox();
        var native = input.Controls.OfType<NumericUpDown>().Single();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(input.Value, Is.EqualTo(0m));
            Assert.That(input.Minimum, Is.EqualTo(0m));
            Assert.That(input.Maximum, Is.EqualTo(100m));
            Assert.That(input.Increment, Is.EqualTo(1m));
            Assert.That(input.DecimalPlaces, Is.EqualTo(0));
            Assert.That(input.ThousandsSeparator, Is.False);
            Assert.That(input.ReadOnly, Is.False);
            Assert.That(input.ValidationState, Is.EqualTo(BootstrapValidationState.None));
            Assert.That(input.BorderRadius, Is.EqualTo(-1));
            Assert.That(input.TabStop, Is.True);
            Assert.That(native.BorderStyle, Is.EqualTo(BorderStyle.None));
            Assert.That(native.TabStop, Is.False);
            Assert.That(input.Controls.OfType<NumericUpDown>().Count(), Is.EqualTo(1));
        }));
    }
}
```

- [ ] **Step 2: Write failing direct-forwarding tests.**

```csharp
[Test]
public void CoreNumericPropertiesForwardDirectlyToNativeEditor()
{
    using var input = new BootstrapNumericBox();
    var native = input.Controls.OfType<NumericUpDown>().Single();

    input.Minimum = -100m;
    input.Maximum = 1000m;
    input.Increment = 0.25m;
    input.DecimalPlaces = 2;
    input.ThousandsSeparator = true;
    input.ReadOnly = true;
    input.Value = 123.50m;

    Assert.Multiple((Action)(() =>
    {
        Assert.That(native.Minimum, Is.EqualTo(-100m));
        Assert.That(native.Maximum, Is.EqualTo(1000m));
        Assert.That(native.Increment, Is.EqualTo(0.25m));
        Assert.That(native.DecimalPlaces, Is.EqualTo(2));
        Assert.That(native.ThousandsSeparator, Is.True);
        Assert.That(native.ReadOnly, Is.True);
        Assert.That(native.Value, Is.EqualTo(123.50m));
        Assert.That(input.Value, Is.EqualTo(native.Value));
    }));
}
```

- [ ] **Step 3: Write failing native-range-authority tests.**

```csharp
[Test]
public void MinimumAndMaximumPreserveNativeNormalizationRules()
{
    using var input = new BootstrapNumericBox();
    var changed = 0;
    input.ValueChanged += (_, _) => changed++;

    input.Minimum = 10m;
    Assert.Multiple((Action)(() =>
    {
        Assert.That(input.Minimum, Is.EqualTo(10m));
        Assert.That(input.Maximum, Is.EqualTo(100m));
        Assert.That(input.Value, Is.EqualTo(10m));
        Assert.That(changed, Is.EqualTo(1));
    }));

    input.Maximum = 5m;
    Assert.Multiple((Action)(() =>
    {
        Assert.That(input.Minimum, Is.EqualTo(5m));
        Assert.That(input.Maximum, Is.EqualTo(5m));
        Assert.That(input.Value, Is.EqualTo(5m));
        Assert.That(changed, Is.EqualTo(2));
    }));
}

[Test]
public void NativeValidationExceptionsAreNotReimplementedOrSwallowed()
{
    using var input = new BootstrapNumericBox();

    Assert.Throws<ArgumentOutOfRangeException>(() => input.Value = 101m);
    Assert.Throws<ArgumentOutOfRangeException>(() => input.DecimalPlaces = -1);
    Assert.Throws<ArgumentOutOfRangeException>(() => input.DecimalPlaces = 100);
    Assert.Throws<ArgumentOutOfRangeException>(() => input.BorderRadius = -2);
    Assert.Throws<ArgumentOutOfRangeException>(() => input.ValidationState = (BootstrapValidationState)999);
}
```

- [ ] **Step 4: Write failing single-event-path tests.**

```csharp
[Test]
public void ValueChangedOccursOncePerEffectiveNativeValueChange()
{
    using var input = new BootstrapNumericBox { Maximum = 10m };
    var native = input.Controls.OfType<NumericUpDown>().Single();
    var senders = new List<object?>();
    input.ValueChanged += (sender, _) => senders.Add(sender);

    input.Value = 1m;
    input.Value = 1m;
    native.UpButton();

    Assert.Multiple((Action)(() =>
    {
        Assert.That(input.Value, Is.EqualTo(2m));
        Assert.That(senders.Count, Is.EqualTo(2));
        Assert.That(senders.All(sender => ReferenceEquals(sender, input)), Is.True);
    }));
}
```

- [ ] **Step 5: Run the filtered STA tests and verify they fail for the missing public control.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~BootstrapNumericBoxTests
```

Expected: compile/test failure because `BootstrapNumericBox` is not implemented.

- [ ] **Step 6: Implement the minimal public control and forwarding properties.**

Use direct forwarding properties with WinForms metadata, for example:

```csharp
[Category("Data")]
[Description("Gets or sets the current native numeric value.")]
[DefaultValue(typeof(decimal), "0")]
public decimal Value
{
    get => _editor.Value;
    set => _editor.Value = value;
}

[Category("Data")]
[Description("Gets or sets the minimum native numeric value.")]
[DefaultValue(typeof(decimal), "0")]
public decimal Minimum
{
    get => _editor.Minimum;
    set => _editor.Minimum = value;
}

[Category("Data")]
[Description("Gets or sets the maximum native numeric value.")]
[DefaultValue(typeof(decimal), "100")]
public decimal Maximum
{
    get => _editor.Maximum;
    set => _editor.Maximum = value;
}
```

Apply the same forwarding-only rule to `Increment`, `DecimalPlaces`, `ThousandsSeparator`, and `ReadOnly`. `ValidationState` and `BorderRadius` are the only wrapper-owned public state.

- [ ] **Step 7: Implement the one native event path and generalized validation-state XML comment.**

`BootstrapValidationState` keeps values `None = 0`, `Valid = 1`, `Invalid = 2`; only change its summary from TextBox-specific wording to input-validation wording. In `BootstrapNumericBox`, subscribe exactly once to `_editor.ValueChanged` and raise the wrapper event with `this` as sender.

- [ ] **Step 8: Run contract/delegation tests until green.**

Run the Task 2 filtered command. Expected: all default, delegation, range, validation, and event-path tests pass.

- [ ] **Step 9: Commit the public contract.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBox.cs src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapValidationState.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxTests.cs
git commit -m "feat: add native-backed BootstrapNumericBox"
```

---

### Task 3: Add shell painting, theme/font ownership, and DPI layout

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBox.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxTests.cs`

**Interfaces:**
- Consumes: Task 1 metrics/palette/layout helpers and existing `BootstrapThemeManager`, `RoundedPath`, `CornerRadius`, `DpiScaler`.
- Produces: theme-reactive shell behavior with no new public members.

- [ ] **Step 1: Write failing theme/surface/layout tests.**

Cover these observable invariants:

```csharp
[Test]
public void NativeEditorUsesShellPaletteAndRemainsInsideClientBounds()
{
    using var input = new BootstrapNumericBox { Size = new Size(180, 32) };
    var native = input.Controls.OfType<NumericUpDown>().Single();

    input.PerformLayout();

    Assert.Multiple((Action)(() =>
    {
        Assert.That(input.ClientRectangle.Contains(native.Bounds), Is.True);
        Assert.That(native.BackColor, Is.EqualTo(BootstrapThemeManager.CurrentTheme.Colors.Surface));
        Assert.That(native.ForeColor, Is.EqualTo(BootstrapThemeManager.CurrentTheme.Colors.Text));
    }));

    input.ReadOnly = true;
    Assert.That(native.BackColor, Is.EqualTo(BootstrapThemeManager.CurrentTheme.Colors.SurfaceSecondary));

    input.Enabled = false;
    Assert.That(native.ForeColor, Is.EqualTo(BootstrapThemeManager.CurrentTheme.Colors.MutedText));
}
```

Also add a `DrawToBitmap` smoke test for normal, focused/validation where practical, read-only, disabled, and custom radius states. The assertion is that drawing succeeds and the native child remains contained; do not introduce pixel-perfect golden-image tests.

- [ ] **Step 2: Write failing runtime-theme/font-ownership tests.**

```csharp
[Test]
public void CallerAssignedFontRemainsCallerOwnedAcrossThemeChangesAndDispose()
{
    var originalTheme = BootstrapThemeManager.CurrentTheme;
    using var callerFont = new Font("Segoe UI", 11f, FontStyle.Bold);

    try
    {
        var input = new BootstrapNumericBox { Font = callerFont };
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        Assert.That(ReferenceEquals(input.Font, callerFont), Is.True);
        input.Dispose();
        Assert.That(callerFont.Size, Is.EqualTo(11f));
    }
    finally
    {
        BootstrapThemeManager.CurrentTheme = originalTheme;
    }
}
```

Add a complementary test that a theme-owned font changes when the theme typography changes and that the native editor receives the same effective `Font` as the wrapper.

- [ ] **Step 3: Implement control styles, theme application, and layout.**

Follow the `BootstrapTextBox` lifecycle pattern:

```csharp
SetStyle(
    ControlStyles.UserPaint |
    ControlStyles.AllPaintingInWmPaint |
    ControlStyles.OptimizedDoubleBuffer |
    ControlStyles.ResizeRedraw |
    ControlStyles.SupportsTransparentBackColor |
    ControlStyles.Selectable,
    true);

BackColor = Color.Transparent;
TabStop = true;
AccessibleRole = AccessibleRole.SpinButton;
AccessibleDescription = "Bootstrap-inspired numeric input.";
```

At construction, apply the theme font and choose an initial size from native/default metrics rather than a hard-coded themed height:

```csharp
var theme = BootstrapThemeManager.CurrentTheme;
var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
var metrics = BootstrapNumericBoxRenderLogic.ResolveMetrics(theme.Metrics, dpi, _borderRadius);
Size = new Size(
    _editor.Width + (metrics.HorizontalPadding * 2),
    DpiScaler.Scale(theme.Metrics.ControlHeight, dpi));
```

`OnLayout` calculates `_editor.Bounds` through `CalculateNativeBounds(ClientSize, _editor.PreferredHeight, metrics)`.

- [ ] **Step 4: Implement rounded shell painting with scoped GDI resources.**

Use the paint contract from this plan. Keep the native spin buttons untouched and do not render any extra arrows.

- [ ] **Step 5: Implement theme/font/DPI/disposal hooks.**

Required hooks:

```text
OnEnabledChanged -> ApplyTheme, PerformLayout, Invalidate
OnFontChanged -> preserve caller ownership, ApplyChildFont, PerformLayout, Invalidate
OnLayout -> CalculateNativeBounds
OnDpiChangedAfterParent -> PerformLayout, Invalidate
ThemeChanged -> optional theme font refresh, ApplyTheme, PerformLayout, Invalidate
Dispose -> unsubscribe theme + dispose theme-owned font
```

- [ ] **Step 6: Run NumericBox pure + STA tests.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter "FullyQualifiedName~BootstrapNumericBox"
```

Expected: all NumericBox-specific tests pass.

- [ ] **Step 7: Commit shell rendering/lifecycle.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxTests.cs
git commit -m "feat: theme BootstrapNumericBox shell"
```

---

### Task 4: Lock keyboard, focus, read-only, boundary, and mouse-wheel semantics

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBox.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxTests.cs`

**Interfaces:**
- Consumes: one owned native editor from Task 2.
- Produces: one composite focus stop and native interaction behavior, without a second focus engine or custom numeric input path.

- [ ] **Step 1: Write failing Tab-entry/exit tests using a real Form.**

```csharp
[Test]
public void CompositeOwnsOneTabStopAndRedirectsFocusToNativeEditor()
{
    using var form = new Form();
    using var before = new TextBox { TabIndex = 0 };
    using var input = new BootstrapNumericBox { TabIndex = 1 };
    using var after = new TextBox { TabIndex = 2 };
    form.Controls.AddRange(new Control[] { before, input, after });
    form.Show();

    var native = input.Controls.OfType<NumericUpDown>().Single();
    before.Focus();

    Assert.That(form.SelectNextControl(before, true, true, true, true), Is.True);
    Assert.That(input.ContainsFocus, Is.True);
    Assert.That(native.Focused, Is.True);
    Assert.That(native.TabStop, Is.False);

    Assert.That(form.SelectNextControl(input, true, true, true, true), Is.True);
    Assert.That(after.Focused, Is.True);
}
```

Add the reverse traversal assertion with `forward: false` so Shift+Tab semantics are covered without using timing-sensitive `SendKeys`.

- [ ] **Step 2: Write failing native spin/boundary/read-only tests.**

```csharp
[Test]
public void NativeSpinButtonsRespectIncrementBoundariesAndReadOnlySemantics()
{
    using var input = new BootstrapNumericBox
    {
        Minimum = 0m,
        Maximum = 2m,
        Value = 1m,
        Increment = 1m,
        ReadOnly = true
    };
    var native = input.Controls.OfType<NumericUpDown>().Single();

    native.UpButton();
    Assert.That(input.Value, Is.EqualTo(2m));

    native.UpButton();
    Assert.That(input.Value, Is.EqualTo(2m));

    native.DownButton();
    Assert.That(input.Value, Is.EqualTo(1m));
    Assert.That(native.ReadOnly, Is.True);
}
```

This deliberately verifies that the wrapper does not reinterpret `ReadOnly` as disabled.

- [ ] **Step 3: Write failing keyboard-event forwarding tests.**

Use the same reflection helper pattern already established by `BootstrapTextBoxTests` to raise protected native key events and assert wrapper subscribers see exactly one `KeyDown`, `KeyPress`, `KeyUp`, and `PreviewKeyDown`, including propagation of `Handled`, `SuppressKeyPress`, and `IsInputKey` changes.

- [ ] **Step 4: Add a mouse-wheel native-characterization test.**

Do not duplicate wheel mathematics. Place the wrapper's native editor and a plain configured `NumericUpDown` in the same STA test form, raise one equal wheel input on each, and assert both end at the same value:

```csharp
private static void RaiseMouseWheel(Control control, int delta)
{
    var method = control.GetType().GetMethod(
        "OnMouseWheel",
        BindingFlags.Instance | BindingFlags.NonPublic);
    Assert.That(method, Is.Not.Null);
    method!.Invoke(control, new object[]
    {
        new MouseEventArgs(MouseButtons.None, 0, 0, 0, delta)
    });
}
```

If the target runtime/system wheel setting produces no change for both controls, equality still proves the wrapper did not replace native policy. The integrated demo provides the real-device manual wheel check.

- [ ] **Step 5: Implement focus and key forwarding only as needed to make the tests pass.**

Use these private handlers:

```csharp
private void OnEditorGotFocus(object? sender, EventArgs e) => Invalidate();
private void OnEditorLostFocus(object? sender, EventArgs e) => Invalidate();
private void OnEditorKeyDown(object? sender, KeyEventArgs e) => OnKeyDown(e);
private void OnEditorKeyPress(object? sender, KeyPressEventArgs e) => OnKeyPress(e);
private void OnEditorKeyUp(object? sender, KeyEventArgs e) => OnKeyUp(e);
private void OnEditorPreviewKeyDown(object? sender, PreviewKeyDownEventArgs e) => OnPreviewKeyDown(e);
```

`OnEnter` / `OnMouseDown` call a private `FocusEditor()` only when the control is enabled. Do not intercept Up/Down, wheel messages, or numeric text.

- [ ] **Step 6: Run all NumericBox tests.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter "FullyQualifiedName~BootstrapNumericBox"
```

Expected: pure and STA NumericBox tests pass with native interaction semantics intact.

- [ ] **Step 7: Commit the interaction contract.**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapNumericBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapNumericBoxTests.cs
git commit -m "test: lock BootstrapNumericBox interaction semantics"
```

---

### Task 5: Add the shared Advanced Inputs demo and integration tests

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoApplicationTests.cs`

**Interfaces:**
- Consumes: `BootstrapNumericBox`.
- Produces: one reusable `Advanced Inputs` integrated-demo page that Stage 6 ComboBox and Stage 9 DatePicker extend rather than creating new top-level pages.

- [ ] **Step 1: Write the failing Advanced Inputs demo test.**

The form must expose these concrete NumericBox scenarios:

```text
Integer/default        Value=12, Minimum=0, Maximum=100, Increment=1
Decimal                Value=12.50, Minimum=0, Maximum=100, Increment=0.25, DecimalPlaces=2
Thousands              Value=123456, Minimum=0, Maximum=1000000, Increment=1000, ThousandsSeparator=true
Signed/large step      Value=0, Minimum=-100, Maximum=100, Increment=10
Valid                  ValidationState=Valid
Invalid                ValidationState=Invalid
Read-only              ReadOnly=true, Enabled=true
Disabled               Enabled=false
```

Test at least the following:

```csharp
[Test]
public void AdvancedInputsDemoContainsStage5NumericScenarios()
{
    using var form = new AdvancedInputsDemoForm();
    form.CreateControl();
    form.PerformLayout();

    var numericBoxes = FindControls<BootstrapNumericBox>(form).ToArray();

    Assert.Multiple((Action)(() =>
    {
        Assert.That(numericBoxes.Length, Is.GreaterThanOrEqualTo(8));
        Assert.That(numericBoxes.Any(box => box.DecimalPlaces == 2 && box.Increment == 0.25m), Is.True);
        Assert.That(numericBoxes.Any(box => box.ThousandsSeparator && box.Value == 123456m), Is.True);
        Assert.That(numericBoxes.Any(box => box.ValidationState == BootstrapValidationState.Valid), Is.True);
        Assert.That(numericBoxes.Any(box => box.ValidationState == BootstrapValidationState.Invalid), Is.True);
        Assert.That(numericBoxes.Any(box => box.ReadOnly && box.Enabled), Is.True);
        Assert.That(numericBoxes.Any(box => !box.Enabled), Is.True);
    }));
}
```

- [ ] **Step 2: Add failing integrated-navigation assertions.**

Update `RequiredPages` in `IntegratedDemoApplicationTests` by inserting `"Advanced Inputs"` immediately after `"Inputs"`. Add a navigation test equivalent to the existing DataGrid test that selects the page and verifies an embedded non-top-level `AdvancedInputsDemoForm` is created.

- [ ] **Step 3: Run the demo tests and verify they fail before implementation.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter "FullyQualifiedName~AdvancedInputsDemoFormTests|FullyQualifiedName~IntegratedDemoApplicationTests"
```

Expected: failure because the form/navigation entry does not yet exist.

- [ ] **Step 4: Implement `AdvancedInputsDemoForm`.**

Use programmatic WinForms layout consistent with the existing demo. Prefer a scrollable `TableLayoutPanel`/`FlowLayoutPanel`, labels next to each input, and a small status label connected to the default integer control's `ValueChanged` event so event behavior is manually visible. Do not introduce a second theme selector; runtime Light/Dark switching remains owned by `MainForm`.

The form must be reusable: keep NumericBox scenarios in their own section so Stage 6 can append ComboBox scenarios and Stage 9 can append DatePicker scenarios without replacing this form.

- [ ] **Step 5: Add exactly one MainForm page.**

Insert after the existing `Inputs` entry:

```csharp
AddPage(
    "Advanced Inputs",
    "Native-backed NumericBox, ComboBox, and DatePicker scenarios with validation, formatting, keyboard, and DPI checks.",
    () => new AdvancedInputsDemoForm());
```

The description intentionally names future members of the shared roadmap page; Stage 5 initially displays only NumericBox scenarios.

- [ ] **Step 6: Run demo/integration tests until green.**

Run the Task 5 filtered command. Expected: Advanced Inputs scenarios and integrated navigation pass.

- [ ] **Step 7: Commit demo integration.**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoApplicationTests.cs
git commit -m "demo: add BootstrapNumericBox scenarios"
```

---

### Task 6: Update component/testing architecture documentation

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `README.md`
- Modify: `docs/PACKAGE_README.md`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: finalized code/test behavior from Tasks 1–5.
- Produces: user/developer documentation matching the implemented contract exactly.

- [ ] **Step 1: Add the finalized component contract to `docs/COMPONENTS.md`.**

Document all nine public properties plus `ValueChanged`, the one-native-editor architecture, direct forwarding, single tab stop, validation priority, read-only semantics, radius sentinel, and explicitly deferred Hexadecimal/Accelerations/custom parsing/icons/prefix/suffix behavior.

- [ ] **Step 2: Extend `docs/TESTING.md`.**

Add Stage 5 coverage under pure, STA, manual, DPI, theme, lifecycle, interaction, and Designer sections:

```text
Pure: palette priority, validation rejection, DPI metrics, radius, native bounds.
STA: defaults, exactly one native NumericUpDown, direct forwarding, range normalization,
     ValueChanged once, tab entry/exit, key forwarding, spin boundaries, read-only,
     wheel characterization, theme/font/disposal, drawing smoke.
Manual: typed culture-sensitive values, Up/Down, spin buttons, wheel, Tab/Shift+Tab,
        read-only vs disabled, valid/invalid, Light/Dark, resize, 100/125/150/175/200% DPI.
Designer: default construction plus serialization of Value/Minimum/Maximum/Increment/
          DecimalPlaces/ThousandsSeparator/ReadOnly/ValidationState/BorderRadius.
```

- [ ] **Step 3: Update architecture/dependency docs.**

Add NumericBox to the native-backed input description with this explicit edge:

```text
NumericBox -> native NumericUpDown + Theme / Rendering
```

State that NumericBox does not depend on Tabs, ComboBox, Dropdown, or a custom popup/parser subsystem.

- [ ] **Step 4: Update package-facing docs and changelog.**

Add one concise usage example:

```csharp
var quantity = new BootstrapNumericBox
{
    Minimum = 0,
    Maximum = 1000,
    Value = 10,
    Increment = 5,
    ThousandsSeparator = true,
    ValidationState = BootstrapValidationState.None
};

quantity.ValueChanged += (_, _) =>
{
    Console.WriteLine(quantity.Value);
};
```

Record the compatible addition under `Unreleased` in `CHANGELOG.md`; do not change assembly version or rewrite historical release entries.

- [ ] **Step 5: Commit documentation.**

```powershell
git add docs/COMPONENTS.md docs/TESTING.md docs/ARCHITECTURE.md README.md docs/PACKAGE_README.md CHANGELOG.md
git commit -m "docs: document BootstrapNumericBox"
```

---

### Task 7: Deliberately review and update the frozen public API baseline

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
- Modify: `docs/PUBLIC_API_BASELINE.md`

**Interfaces:**
- Consumes: the complete exported assembly after Tasks 1–6.
- Produces: one intentionally approved new RC fingerprint with `AssemblyVersion` still `1.0.0.0`.

- [ ] **Step 1: Run the public API baseline test before changing the approved fingerprint.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline
```

Expected: FAIL with `Public API baseline changed`, an `Actual fingerprint`, and the reconstructed exported surface.

- [ ] **Step 2: Review the failure output before accepting it.**

The deliberate Stage 5 public additions must be limited to `MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapNumericBox`, its parameterless constructor, the public properties `Value`, `Minimum`, `Maximum`, `Increment`, `DecimalPlaces`, `ThousandsSeparator`, `ReadOnly`, `ValidationState`, `BorderRadius`, the `ValueChanged` event, and only the protected overrides actually required by the composed-control lifecycle.

Verify that:

- `BootstrapNumericBoxRenderLogic`, metrics, and palette/layout structs remain internal.
- No native `NumericUpDown` child accessor became public/protected.
- `BootstrapValidationState` names and numeric values did not change.
- No existing type/member disappeared or changed signature.
- `AssemblyVersion` remains `1.0.0.0`.

- [ ] **Step 3: Replace `ApprovedV1Fingerprint` with the reviewed actual SHA-256 value printed by the failing test.**

Do not compute or guess the value in advance. The failure output from the implemented assembly is the authoritative value to copy after surface review.

- [ ] **Step 4: Update `docs/PUBLIC_API_BASELINE.md`.**

Replace the displayed approved fingerprint with the same reviewed value and append the NumericBox addition summary after the existing compatible-addition history. State that the helper types remain internal and the assembly version is unchanged.

- [ ] **Step 5: Re-run both API baseline tests.**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release --filter FullyQualifiedName~Phase16PublicApiBaselineTests
```

Expected: `ExportedApiMatchesApprovedV1Baseline` PASS and `V1CompatibilityAssemblyVersionIsStable` PASS.

- [ ] **Step 6: Commit the deliberate baseline update.**

```powershell
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs docs/PUBLIC_API_BASELINE.md
git commit -m "chore: approve BootstrapNumericBox API"
```

---

### Task 8: Run the complete Stage 5 verification gate

**Files:**
- No new files unless a verification failure requires a targeted correction.

**Interfaces:**
- Consumes: completed Stage 5 implementation.
- Produces: evidence that the stage is independently shippable before Stage 6 begins.

- [ ] **Step 1: Build the `net48` target.**

```powershell
dotnet build .\src\MyDmsVn.Bootstrap5WinFormUI\MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net48
```

Expected: build succeeds with no new warnings attributable to NumericBox.

- [ ] **Step 2: Build the `net8.0-windows` target.**

```powershell
dotnet build .\src\MyDmsVn.Bootstrap5WinFormUI\MyDmsVn.Bootstrap5WinFormUI.csproj -c Release -f net8.0-windows
```

Expected: build succeeds with no new warnings attributable to NumericBox.

- [ ] **Step 3: Run the complete automated suite.**

```powershell
dotnet test -c Release
```

Then run the repository wrappers used by CI:

```powershell
.\build.ps1
.\test.ps1 -SkipBuild
```

Expected: all tests pass across the repository's configured framework matrix.

- [ ] **Step 4: Run the integrated demo and perform the Stage 5 manual matrix.**

Open **Advanced Inputs** and verify:

```text
Integer/default
DecimalPlaces=2 with Increment=0.25
Thousands separator
Negative/positive range with Increment=10
Valid border
Invalid border
Read-only: typed editing blocked, native spin/arrow change still works
Disabled: no user interaction
Tab and Shift+Tab entry/exit
Up/Down keyboard
Native spin buttons
Mouse wheel
ValueChanged status feedback
Light -> Dark and Dark -> Light
Rapid resize
100%, 125%, 150%, 175%, 200% Windows DPI
```

At every DPI, verify the native text baseline and spin buttons remain usable, the native control stays inside the rounded shell, focus/validation borders are not clipped, and no second tab stop appears.

- [ ] **Step 5: Perform Designer checks in Visual Studio.**

Place `BootstrapNumericBox` on a form without theme bootstrap code. Set and serialize:

```text
Value=12.5
Minimum=-100
Maximum=1000
Increment=0.25
DecimalPlaces=2
ThousandsSeparator=true
ReadOnly=true
ValidationState=Valid
BorderRadius=6
```

Save, close, reopen the Designer, and verify all values restore without exceptions or generated references to internal native child controls.

- [ ] **Step 6: Inspect lifecycle/resource behavior.**

Repeatedly create/dispose NumericBox instances while switching Light/Dark. Confirm there is no unbounded USER/GDI handle growth, no disposed control retained by theme subscriptions, no control-owned timer, and caller fonts remain caller-owned.

- [ ] **Step 7: Verify repository diff scope.**

```powershell
git status --short
git diff --check
```

Expected: no generated `bin/`, `obj/`, package, or IDE files; no whitespace errors; changes are limited to Stage 5 product/test/demo/docs/API-baseline work.

- [ ] **Step 8: Create the stage completion commit if the implementation workflow has not already produced the desired final commit boundary.**

```powershell
git add src tests demo docs README.md CHANGELOG.md
git commit -m "feat: add BootstrapNumericBox"
```

If Tasks 1–7 were intentionally committed separately, do not create an empty duplicate commit. The final repository history must still clearly identify Stage 5 completion.

---

## Stage 5 Acceptance Checklist

Stage 5 is complete only when all statements below are true:

- [ ] `BootstrapNumericBox` derives from `UserControl` and owns exactly one native `NumericUpDown`.
- [ ] Native `NumericUpDown` remains the sole numeric state/parser/range/formatting authority.
- [ ] Public defaults match native NumericUpDown defaults plus `ValidationState=None` and `BorderRadius=-1`.
- [ ] The native editor is borderless and non-tabbable; the outer control owns one tab stop.
- [ ] `ValueChanged` fires exactly once per effective native value transition and reports the wrapper as sender.
- [ ] Native `Minimum`/`Maximum` normalization and native out-of-range exceptions are preserved.
- [ ] `DecimalPlaces`, `ThousandsSeparator`, and regional formatting remain native.
- [ ] `ReadOnly` blocks typed editing without being redefined as disabled; native spin behavior remains available.
- [ ] Validation/disabled/focus border priority matches the established TextBox semantics.
- [ ] Read-only and disabled surfaces use theme tokens rather than hard-coded colors.
- [ ] Border width, focus width, padding, radius, default height, and layout scale through existing theme/DPI infrastructure.
- [ ] The shell uses rounded custom painting with scoped GDI resources and leaves native spin-button painting untouched.
- [ ] Runtime Light/Dark switching updates shell/native colors and theme-owned font state.
- [ ] Caller-assigned fonts are not disposed or replaced by later theme changes.
- [ ] Keyboard events from the native editor are observable through the wrapper.
- [ ] Tab/Shift+Tab exposes no second internal tab stop.
- [ ] Up/Down, native spin buttons, boundaries, and mouse-wheel behavior remain native.
- [ ] No internal `NumericUpDown` implementation child is reflected/styled directly.
- [ ] No Hexadecimal/Accelerations/custom-parser/prefix/suffix/icon scope leaked into Stage 5.
- [ ] `AdvancedInputsDemoForm` exists and MainForm contains exactly one new `Advanced Inputs` navigation entry for the shared Stage 5/6/9 group.
- [ ] NumericBox-specific pure, STA, demo, theme, lifecycle, and API-baseline tests pass.
- [ ] `net48` and `net8.0-windows` builds pass.
- [ ] Full repository tests and CI wrapper scripts pass.
- [ ] Manual Light/Dark and 100/125/150/175/200% DPI checks pass.
- [ ] Visual Studio Designer construction/serialization checks pass.
- [ ] `docs/COMPONENTS.md`, `docs/TESTING.md`, `docs/ARCHITECTURE.md`, `README.md`, `docs/PACKAGE_README.md`, `CHANGELOG.md`, and `docs/PUBLIC_API_BASELINE.md` match the implemented behavior.
- [ ] Public API fingerprint was reviewed deliberately before approval, and `AssemblyVersion` remains `1.0.0.0`.

**Gate:** Do not begin Stage 6 (`BootstrapComboBox`) until every Stage 5 acceptance item is green.
