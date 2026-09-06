# Integrated Demo Browser-Equivalent Typography Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Integrated Demo use a normal body font visually equivalent to the browser default `16px` (`12pt` at the 96-DPI CSS reference) across native WinForms controls and Bootstrap controls, while preserving DPI scaling, theme switching, both target frameworks, and the library's existing default typography contract.

**Architecture:** Keep this change entirely inside the demo layer. Add demo-specific typography tokens and a demo theme factory, plus a shared `DemoFormBase` that establishes `Segoe UI 12pt` and `AutoScaleMode.Dpi` before derived demo forms create their child controls. Because framework controls such as `BootstrapButton` source their font from `BootstrapThemeManager.CurrentTheme.Typography.Body` rather than inheriting the parent `Form.Font`, the Integrated Demo must also publish a demo-specific `BootstrapTheme` whose body token is `12pt`; do **not** change `BootstrapThemeTypography.Default` in the production library.

**Tech Stack:** C#, WinForms, `System.Drawing.Font`, `GraphicsUnit.Point`, existing `BootstrapTheme`, `BootstrapThemeTypography`, `BootstrapFontToken`, `BootstrapThemeManager`, `net48;net8.0-windows`, NUnit, existing Integrated Demo and WinForms test infrastructure.

**Spec:** User requirement from the 2026-09-07 Integrated Demo typography discussion, bounded by `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/WINFORMS_TEST_EXECUTION.md`, and repository rules in `AGENTS.md`.

## Global Constraints

- Scope is the Integrated Demo only. Do not change the production framework's default typography merely to make the demo larger.
- Keep `BootstrapThemeTypography.Default.Body` at its current `Segoe UI 9pt` contract unless a separate framework-wide typography change is explicitly approved.
- Treat browser-default `16 CSS px` as `12pt` at the 96-DPI CSS reference: `16 × 72 / 96 = 12`.
- Use `GraphicsUnit.Point`; do not hard-code `16` physical pixels with `GraphicsUnit.Pixel`.
- Do not set a WinForms font to `16f` expecting CSS `16px`; `16f` with the normal `Font` constructor means `16pt`, approximately `21.33px` at 96 DPI.
- Use `AutoScaleMode.Dpi` for demo forms. Do not introduce font-based autoscaling as a second scaling model.
- Do not use `Application.SetDefaultFont()` because the demo must continue targeting `net48;net8.0-windows` with one coherent implementation.
- Keep runtime targets exactly `net48;net8.0-windows`.
- Keep the root namespace and production namespaces unchanged.
- Do not introduce a new package or font dependency. Use the Windows system font family already used by the framework: `Segoe UI`.
- Preserve Light/Dark switching and Reduced Motion switching. Changing either must not reset the demo typography to framework-default `9pt`.
- Preserve caller/custom theme colors and metrics when merely normalizing an already-active theme for demo typography.
- Demo-specific theme creation may continue to use the framework's default colors and metrics when the user explicitly switches Light/Dark from the Integrated Demo header, matching current `MainForm.PublishSelectedTheme()` behavior.
- Any `Font` instance created by demo infrastructure is owned by that infrastructure and must be disposed deterministically.
- Do not set fonts recursively on every child control. Native WinForms controls should inherit the form font; Bootstrap controls should continue using their existing theme-font mechanism.
- Do not weaken a Bootstrap control's current `UseThemeFont`/theme ownership behavior just to make the demo match `12pt`.
- Do not add demo typography settings to the public production package API.
- Use the existing unattended WinForms test rules: STA for handle/UI tests, no modal UI, bounded message pumping only, and `--blame-hang --blame-hang-timeout 5m` for focused raw `dotnet test` runs.
- Run the complete suite through `./test.ps1` before considering implementation complete.

---

## Why a Form-Level Font Alone Is Insufficient

The Integrated Demo currently mixes two font acquisition paths:

1. Standard WinForms controls such as `Label`, `GroupBox`, `FlowLayoutPanel`, and ordinary `Button` normally inherit `Font` from their parent/form unless explicitly overridden.
2. Bootstrap controls may own a theme font. For example, `BootstrapButton` initializes with theme-font mode enabled and constructs its font from `BootstrapThemeManager.CurrentTheme.Typography.Body`.

Therefore this change needs both layers:

```text
Integrated Demo Form.Font = Segoe UI 12pt
                +
BootstrapThemeManager.CurrentTheme.Typography.Body = Segoe UI 12pt
```

Changing only `MainForm.Font` would leave Bootstrap controls at the current theme body size (`9pt`). Changing only the global theme typography would leave ordinary WinForms demo controls at their inherited/default WinForms font unless all forms participate consistently.

The shared demo base class and demo theme factory deliberately solve the two paths without changing the production theme defaults.

---

## Demo Typography Contract

Use these demo-only semantic mappings:

| Role | Demo token | Web-equivalent intent |
| --- | ---: | ---: |
| Body | `Segoe UI 12pt` regular | `16px` |
| BodySmall | `Segoe UI 10.5pt` regular | `14px` |
| Label | `Segoe UI 12pt` bold | `16px` emphasized |
| HeadingSmall | `Segoe UI 15pt` bold | `20px` |
| HeadingMedium | `Segoe UI 18pt` bold | `24px` |

The important acceptance requirement is the `Body = 12pt` contract. The other roles keep a sensible hierarchy so increasing body text does not make headings smaller than body text.

Do not copy these values into `BootstrapThemeTypography.Default`; they belong to the Integrated Demo.

---

## File Structure

### New demo infrastructure

- Create `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoTypography.cs`
  - Owns the browser-equivalent point-size constants.
  - Creates immutable `BootstrapThemeTypography` tokens.
  - Creates a caller-owned `Font` for native WinForms inheritance.
- Create `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoThemeFactory.cs`
  - Creates Light/Dark demo themes with demo typography.
  - Replaces only typography when normalizing an already-active theme.
  - Detects whether the active theme already uses the complete demo typography contract so normalization is idempotent.
- Create `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoFormBase.cs`
  - Establishes the demo theme early enough for derived-field Bootstrap controls.
  - Sets `AutoScaleMode.Dpi`.
  - Owns/disposes the `Segoe UI 12pt` form font.

### Existing demo shell/theme files

- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/Program.cs`
- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoPageHostForm.cs`
- Modify every concrete `*DemoForm` listed in Tasks 4-6 so it derives from `DemoFormBase` rather than directly from `Form`.

### Tests

- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyLayoutTests.cs`

The existing test project already references the demo project, so do not add another project reference solely for these tests.

---

## Internal Interfaces

### `DemoTypography`

```csharp
internal static class DemoTypography
{
    internal const string FontFamilyName = "Segoe UI";
    internal const float BodySizeInPoints = 12f;
    internal const float BodySmallSizeInPoints = 10.5f;
    internal const float LabelSizeInPoints = 12f;
    internal const float HeadingSmallSizeInPoints = 15f;
    internal const float HeadingMediumSizeInPoints = 18f;

    internal static BootstrapThemeTypography CreateThemeTypography();

    // Caller owns the returned GDI Font.
    internal static Font CreateBodyFont();
}
```

Implementation contract:

```csharp
internal static BootstrapThemeTypography CreateThemeTypography()
{
    return new BootstrapThemeTypography(
        new BootstrapFontToken(FontFamilyName, BodySizeInPoints),
        new BootstrapFontToken(FontFamilyName, BodySmallSizeInPoints),
        new BootstrapFontToken(FontFamilyName, LabelSizeInPoints, FontStyle.Bold),
        new BootstrapFontToken(FontFamilyName, HeadingSmallSizeInPoints, FontStyle.Bold),
        new BootstrapFontToken(FontFamilyName, HeadingMediumSizeInPoints, FontStyle.Bold));
}

internal static Font CreateBodyFont()
{
    return new Font(
        FontFamilyName,
        BodySizeInPoints,
        FontStyle.Regular,
        GraphicsUnit.Point);
}
```

### `DemoThemeFactory`

```csharp
internal static class DemoThemeFactory
{
    internal static BootstrapTheme Create(
        BootstrapThemeMode mode,
        bool reducedMotion = false);

    internal static void EnsureCurrentThemeUsesDemoTypography();
}
```

`Create()` must build a fresh theme with:

```csharp
return new BootstrapTheme(
    mode,
    BootstrapThemeColors.CreateDefault(mode),
    BootstrapThemeMetrics.Default,
    DemoTypography.CreateThemeTypography(),
    reducedMotion);
```

`EnsureCurrentThemeUsesDemoTypography()` must be idempotent. If all five typography roles already match the demo contract, return without publishing another theme. Otherwise preserve the current mode, colors, metrics, and reduced-motion value and replace only typography:

```csharp
var current = BootstrapThemeManager.CurrentTheme;
BootstrapThemeManager.CurrentTheme = new BootstrapTheme(
    current.Mode,
    current.Colors,
    current.Metrics,
    DemoTypography.CreateThemeTypography(),
    current.ReducedMotion);
```

The private comparison used by the idempotence check must compare family name, point size, and `FontStyle` for every role; do not decide based on body size alone.

### `DemoFormBase`

```csharp
internal abstract class DemoFormBase : Form
{
    private Font? _demoBodyFont;

    protected DemoFormBase()
    {
        DemoThemeFactory.EnsureCurrentThemeUsesDemoTypography();
        AutoScaleMode = AutoScaleMode.Dpi;

        _demoBodyFont = DemoTypography.CreateBodyFont();
        Font = _demoBodyFont;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _demoBodyFont?.Dispose();
            _demoBodyFont = null;
        }
    }
}
```

The base constructor intentionally normalizes the theme before the derived demo form finishes constructing its own controls. This makes directly instantiated demo forms behave consistently in tests and standalone diagnostics, not only when launched through `Program.Main()`.

---

### Task 1: Lock the Integrated Demo typography contract with failing tests

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`
- Reference only: `src/MyDmsVn.Bootstrap5WinFormUI/Theme/BootstrapThemeTypography.cs`
- Reference only: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapButton.cs`
- Reference only: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`

**Interfaces:**
- Consumes: existing `BootstrapThemeManager`, `BootstrapTheme.CreateDefault`, `BootstrapThemeTypography.Default`, `MainForm`, `BootstrapButton`.
- Produces: executable regression contract for the `12pt` demo body size, theme-toggle persistence, native-form inheritance, Bootstrap-control theme typography, and unchanged framework defaults.

- [ ] **Step 1: Add an STA fixture that saves/restores the global theme**

Use this fixture skeleton so tests cannot leak global theme state into the rest of the suite:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class IntegratedDemoTypographyTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    private static IEnumerable<T> FindControls<T>(Control root)
        where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindControls<T>(child))
            {
                yield return nested;
            }
        }
    }
}
```

- [ ] **Step 2: Add the failing body-font regression test**

```csharp
[Test]
public void MainFormUsesBrowserEquivalentTwelvePointBodyTypography()
{
    using var form = new MainForm();

    Assert.Multiple((Action)(() =>
    {
        Assert.That(form.AutoScaleMode, Is.EqualTo(AutoScaleMode.Dpi));
        Assert.That(form.Font.Name, Is.EqualTo("Segoe UI"));
        Assert.That(form.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
        Assert.That(BootstrapThemeManager.CurrentTheme.Typography.Body.FontFamilyName, Is.EqualTo("Segoe UI"));
        Assert.That(BootstrapThemeManager.CurrentTheme.Typography.Body.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
    }));

    var themeLabel = FindControls<Label>(form).Single(label => label.Text == "Theme");
    Assert.That(themeLabel.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));

    using var themedButton = new BootstrapButton();
    Assert.That(themedButton.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
}
```

Expected before implementation: FAIL because the framework theme body is currently `9pt`, and `MainForm` does not establish the demo `12pt` font contract.

- [ ] **Step 3: Add the failing theme-switch persistence test**

```csharp
[Test]
public void ThemeAndReducedMotionSwitchingPreserveDemoTypography()
{
    using var form = new MainForm();

    var mode = FindControls<ComboBox>(form)
        .Single(combo => combo.Items.Cast<object>().Select(item => item?.ToString()).Contains("Dark"));
    var reducedMotion = FindControls<CheckBox>(form)
        .Single(checkBox => checkBox.Text == "Reduced motion");

    mode.SelectedIndex = 1;
    reducedMotion.Checked = true;

    var theme = BootstrapThemeManager.CurrentTheme;
    Assert.Multiple((Action)(() =>
    {
        Assert.That(theme.Mode, Is.EqualTo(BootstrapThemeMode.Dark));
        Assert.That(theme.ReducedMotion, Is.True);
        Assert.That(theme.Typography.Body.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
        Assert.That(theme.Typography.BodySmall.SizeInPoints, Is.EqualTo(10.5f).Within(0.01f));
        Assert.That(theme.Typography.Label.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
        Assert.That(theme.Typography.HeadingSmall.SizeInPoints, Is.EqualTo(15f).Within(0.01f));
        Assert.That(theme.Typography.HeadingMedium.SizeInPoints, Is.EqualTo(18f).Within(0.01f));
    }));
}
```

Expected before implementation: FAIL because `MainForm.PublishSelectedTheme()` currently calls `BootstrapTheme.CreateDefault(...)`, which restores `9pt` body typography.

- [ ] **Step 4: Add a guard proving the production default remains unchanged**

```csharp
[Test]
public void FrameworkDefaultTypographyRemainsCompactAndIsNotRewrittenForTheDemo()
{
    Assert.Multiple((Action)(() =>
    {
        Assert.That(BootstrapThemeTypography.Default.Body.FontFamilyName, Is.EqualTo("Segoe UI"));
        Assert.That(BootstrapThemeTypography.Default.Body.SizeInPoints, Is.EqualTo(9f).Within(0.01f));
    }));
}
```

This test is expected to PASS before and after the feature. It prevents an implementation shortcut that edits the production default token.

- [ ] **Step 5: Run the focused tests on both TFMs and capture the intended red state**

Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter FullyQualifiedName~IntegratedDemoTypographyTests --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter FullyQualifiedName~IntegratedDemoTypographyTests --blame-hang --blame-hang-timeout 5m
```

Expected: the new `12pt` and theme-switch tests FAIL for the current implementation; the production-default guard PASSes.

- [ ] **Step 6: Commit the red tests**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs
git commit -m "test: define integrated demo typography contract"
```

---

### Task 2: Add demo-specific typography, theme factory, and base form

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoTypography.cs`
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoThemeFactory.cs`
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoFormBase.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`

**Interfaces:**
- Consumes: `BootstrapFontToken`, `BootstrapThemeTypography`, `BootstrapTheme`, `BootstrapThemeColors`, `BootstrapThemeMetrics`, `BootstrapThemeManager`.
- Produces: `DemoTypography.CreateThemeTypography()`, `DemoTypography.CreateBodyFont()`, `DemoThemeFactory.Create(...)`, `DemoThemeFactory.EnsureCurrentThemeUsesDemoTypography()`, `DemoFormBase`.

- [ ] **Step 1: Create `DemoTypography` exactly from the contract above**

Use constants for the five point sizes. `CreateBodyFont()` must explicitly use `GraphicsUnit.Point` so the code documents that `12pt` is the logical equivalent of browser `16px`, rather than a request for 12 physical pixels.

- [ ] **Step 2: Create `DemoThemeFactory` with an idempotent typography comparison**

Implement a private token comparison like:

```csharp
private static bool Matches(
    BootstrapFontToken token,
    string family,
    float sizeInPoints,
    FontStyle style)
{
    return string.Equals(token.FontFamilyName, family, StringComparison.OrdinalIgnoreCase) &&
           Math.Abs(token.SizeInPoints - sizeInPoints) < 0.001f &&
           token.Style == style;
}
```

Use it for all five roles before publishing a replacement theme. Do not use `Math.Clamp` or another API with `net48` compatibility concerns.

- [ ] **Step 3: Create `DemoFormBase` and make it own the native form font**

Implement the exact base-class contract shown in the Internal Interfaces section. Keep theme normalization before setting the form font. Dispose the owned `Font` deterministically after base form disposal so child controls are already being torn down before the shared inherited font object is released.

- [ ] **Step 4: Add a focused unit-level test for full token preservation through normalization**

Extend `IntegratedDemoTypographyTests` with a black-box assertion driven through `MainForm` later in Task 3; do not expose `DemoThemeFactory` publicly only for tests. At this task boundary, build both demo targets to validate the new internal infrastructure compiles independently:

```powershell
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net48
```

Expected: both builds PASS; Task 1's behavioral tests remain red because the shell has not been wired yet.

- [ ] **Step 5: Commit the demo typography infrastructure**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoTypography.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoThemeFactory.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoFormBase.cs
git commit -m "feat: add integrated demo typography infrastructure"
```

---

### Task 3: Wire the application shell and preserve typography across theme changes

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/Program.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`

**Interfaces:**
- Consumes: `DemoThemeFactory.Create(...)`, `DemoFormBase`.
- Produces: a self-consistent Integrated Demo shell where startup, direct `MainForm` construction, Light/Dark switching, and Reduced Motion all retain the demo typography.

- [ ] **Step 1: Publish the demo theme before constructing `MainForm` in real application startup**

Update `Program.Main()` after DPI/visual-style initialization and before `Application.Run(new MainForm())`:

```csharp
BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(BootstrapThemeMode.Light);
Application.Run(new MainForm());
```

Add the required theme namespace import. This guarantees that the real Integrated Demo never transiently constructs Bootstrap controls against the `9pt` production theme.

- [ ] **Step 2: Move `MainForm` onto `DemoFormBase`**

Change:

```csharp
public sealed class MainForm : Form
```

to:

```csharp
public sealed class MainForm : DemoFormBase
```

Do not add a second `Font = ...` assignment in `MainForm`; the base class owns this policy.

- [ ] **Step 3: Change theme publishing to the demo theme factory**

Replace the current `BootstrapTheme.CreateDefault(mode, _reducedMotion.Checked)` assignment with:

```csharp
BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
    mode,
    _reducedMotion.Checked);
```

This is the key regression fix for theme toggling: switching modes must update colors without reverting typography to `9pt`.

- [ ] **Step 4: Add a test proving `MainForm` direct construction preserves non-typography theme state**

Before constructing `MainForm`, install a theme that uses current colors/metrics and a non-default reduced-motion value but default typography. After constructing the form, assert mode, color-object identity, metric-object identity, and reduced-motion are preserved while typography becomes the demo contract:

```csharp
[Test]
public void MainFormNormalizationReplacesOnlyTypography()
{
    var colors = BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Dark);
    var metrics = BootstrapThemeMetrics.Default;
    BootstrapThemeManager.CurrentTheme = new BootstrapTheme(
        BootstrapThemeMode.Dark,
        colors,
        metrics,
        BootstrapThemeTypography.Default,
        reducedMotion: true);

    using var form = new MainForm();
    var normalized = BootstrapThemeManager.CurrentTheme;

    Assert.Multiple((Action)(() =>
    {
        Assert.That(normalized.Mode, Is.EqualTo(BootstrapThemeMode.Dark));
        Assert.That(normalized.Colors, Is.SameAs(colors));
        Assert.That(normalized.Metrics, Is.SameAs(metrics));
        Assert.That(normalized.ReducedMotion, Is.True);
        Assert.That(normalized.Typography.Body.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
    }));
}
```

- [ ] **Step 5: Run the Task 1/3 focused tests on both TFMs**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter FullyQualifiedName~IntegratedDemoTypographyTests --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter FullyQualifiedName~IntegratedDemoTypographyTests --blame-hang --blame-hang-timeout 5m
```

Expected: the shell, theme persistence, native font, Bootstrap control font, and unchanged-production-default tests PASS on both targets.

- [ ] **Step 6: Commit the application-shell wiring**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/Program.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs
git commit -m "feat: apply browser-equivalent typography to integrated demo shell"
```

---

### Task 4: Migrate host and foundation demo forms to `DemoFormBase`

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoPageHostForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ThemeDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/RenderingDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/IconDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AnimationDemoForm.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`

**Interfaces:**
- Consumes: `DemoFormBase`.
- Produces: shared inherited body typography and DPI policy for host/foundation pages.

- [ ] **Step 1: Change each listed concrete form from direct `Form` inheritance to `DemoFormBase`**

Do not change `DemoPageSection`; it is not a form.

- [ ] **Step 2: Remove only exact duplicate `AutoScaleMode = AutoScaleMode.Dpi` assignments**

If one of these forms already sets the exact same mode, remove that assignment because the base class is now authoritative. Keep unrelated sizing/layout logic unchanged.

- [ ] **Step 3: Verify the Theme page reports the new body token**

`ThemeDemoForm` already renders the active body token in its summary. Do not add a second typography label. With the demo theme active, its existing summary must contain the equivalent of:

```text
Body Segoe UI 12pt
```

- [ ] **Step 4: Build both targets and run existing foundation/demo tests**

```powershell
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net48
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter "FullyQualifiedName~Demo&FullyQualifiedName~Theme|FullyQualifiedName~Demo&FullyQualifiedName~Rendering|FullyQualifiedName~Demo&FullyQualifiedName~Animation" --blame-hang --blame-hang-timeout 5m
```

If the filter syntax proves adapter-specific, run the corresponding demo fixtures by fully qualified fixture name rather than dropping bounded hang detection.

- [ ] **Step 5: Commit the foundation-form migration**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoPageHostForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ThemeDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/RenderingDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/IconDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AnimationDemoForm.cs
git commit -m "refactor: unify integrated demo foundation typography"
```

---

### Task 5: Migrate control/input/feedback demo forms to `DemoFormBase`

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AccordionDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ButtonDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ButtonGroupToolbarDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ChecksDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/CollapseDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/InputGroupDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ProgressDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/SpinnerDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/TextBoxCardDemoForm.cs`
- Test: existing matching fixtures under `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/`

**Interfaces:**
- Consumes: `DemoFormBase`, active demo theme.
- Produces: consistent body typography across native labels/group boxes and theme-font Bootstrap controls on the main control/input/feedback pages.

- [ ] **Step 1: Change inheritance for all listed forms**

Replace only `: Form` with `: DemoFormBase`. Do not recursively assign `Font` to child controls.

- [ ] **Step 2: Remove duplicate local DPI-mode assignments where they exactly duplicate the base**

For example, `ButtonDemoForm` currently assigns `AutoScaleMode = AutoScaleMode.Dpi;`; remove that duplicate after moving to `DemoFormBase`.

Do not remove explicit font assignments that are intentionally demonstrating a font-specific component feature. Those remain local exceptions to the normal body typography contract.

- [ ] **Step 3: Run the existing demo tests for the migrated forms on `net8.0-windows` with bounded hang detection**

Use fixture-name filters for the existing `AccordionDemoFormTests`, `AdvancedInputsDemoFormTests`, `BootstrapSelectDemoContractTests`, `ButtonGroupToolbarDemoFormTests`, `ChecksDemoFormTests`, `FeedbackDemoFormTests`, and any other matching demo fixtures present at implementation time.

Example:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter FullyQualifiedName~MyDmsVn.Bootstrap5WinFormUI.Tests.Demo --blame-hang --blame-hang-timeout 5m
```

Expected: migrated demo fixtures PASS without modal UI or hangs.

- [ ] **Step 4: Run the same demo-test slice on `net48`**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter FullyQualifiedName~MyDmsVn.Bootstrap5WinFormUI.Tests.Demo --blame-hang --blame-hang-timeout 5m
```

Expected: PASS.

- [ ] **Step 5: Commit the control/input/feedback migration**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AccordionDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ButtonDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ButtonGroupToolbarDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ChecksDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/CollapseDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/InputGroupDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ProgressDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/SpinnerDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/TextBoxCardDemoForm.cs
git commit -m "refactor: unify integrated demo control typography"
```

---

### Task 6: Migrate data/navigation demo forms and enforce complete form coverage

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DataGridDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DataGridSelectEditingDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ListViewDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/PaginationDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/SidebarDemoForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/TreeViewDemoForm.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`

**Interfaces:**
- Consumes: `DemoFormBase`.
- Produces: complete Integrated Demo form coverage, including data/navigation pages.

- [ ] **Step 1: Change all listed forms to derive from `DemoFormBase`**

As in Task 5, remove only duplicate `AutoScaleMode.Dpi` assignments and preserve all component-specific behavior.

- [ ] **Step 2: Add a reflection guard so future demo forms cannot silently bypass the shared base**

Add this test:

```csharp
[Test]
public void AllConcreteIntegratedDemoFormsUseSharedDemoFormBase()
{
    var demoAssembly = typeof(MainForm).Assembly;
    var offenders = demoAssembly
        .GetTypes()
        .Where(type =>
            type.Namespace == "MyDmsVn.Bootstrap5WinFormUI.Demo" &&
            !type.IsAbstract &&
            typeof(Form).IsAssignableFrom(type) &&
            type.BaseType?.Name != "DemoFormBase")
        .Select(type => type.FullName)
        .OrderBy(name => name)
        .ToArray();

    Assert.That(offenders, Is.Empty);
}
```

The test deliberately uses assembly metadata rather than making `DemoFormBase` public.

- [ ] **Step 3: Run the full Demo test namespace on both TFMs**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter FullyQualifiedName~MyDmsVn.Bootstrap5WinFormUI.Tests.Demo --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter FullyQualifiedName~MyDmsVn.Bootstrap5WinFormUI.Tests.Demo --blame-hang --blame-hang-timeout 5m
```

Expected: PASS, including existing DataGrid guards and bounded WinForms execution behavior.

- [ ] **Step 4: Commit the data/navigation migration and coverage guard**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DataGridDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DataGridSelectEditingDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ListViewDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/NavigationDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/PaginationDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/SidebarDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/TreeViewDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs
git commit -m "refactor: complete integrated demo typography migration"
```

---

### Task 7: Add layout regressions for the larger body font and harden only affected local layouts

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyLayoutTests.cs`
- Potentially modify only if a regression test demonstrates clipping:
  - `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
  - `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ThemeDemoForm.cs`
  - Any individual demo form whose existing fixed logical height/width is proven too small at `12pt`

**Interfaces:**
- Consumes: completed demo typography/base-form migration.
- Produces: deterministic no-clipping checks for the shared shell and representative native/themed content, plus a rule against globally inflating every spacing metric.

- [ ] **Step 1: Add an STA layout fixture with theme save/restore**

Use `[Apartment(ApartmentState.STA)]`, save/restore `BootstrapThemeManager.CurrentTheme`, and do not call `Application.Run()` or `ShowDialog()`.

- [ ] **Step 2: Add a shell chrome layout check**

Construct `MainForm`, call `CreateControl()` and `PerformLayout()`, locate the header `TableLayoutPanel`, title/description labels, theme `ComboBox`, and Reduced Motion checkbox. Assert all visible child bounds are contained within the header client rectangle after accounting for header padding and that text-bearing controls have positive client height.

Use a helper that verifies containment rather than asserting one hard-coded pixel height:

```csharp
private static void AssertContained(Control child, Control parent)
{
    var bounds = child.Bounds;
    Assert.Multiple((Action)(() =>
    {
        Assert.That(bounds.Left, Is.GreaterThanOrEqualTo(0), child.Name);
        Assert.That(bounds.Top, Is.GreaterThanOrEqualTo(0), child.Name);
        Assert.That(bounds.Right, Is.LessThanOrEqualTo(parent.ClientSize.Width), child.Name);
        Assert.That(bounds.Bottom, Is.LessThanOrEqualTo(parent.ClientSize.Height), child.Name);
    }));
}
```

Do not require every page's full contents to fit without scrollbars; several demo pages intentionally use `AutoScroll`.

- [ ] **Step 3: Add a representative native-vs-themed sizing check**

After `MainForm` establishes the demo theme, create a normal WinForms `Label` under a `DemoFormBase`-derived form and a `BootstrapButton`. Assert both use approximately `12pt`. For the button, assert `GetPreferredSize(Size.Empty).Height > 0` and that `AutoSize = true` can size it without clipping its text.

- [ ] **Step 4: Add a Theme page summary check through the Integrated Demo control tree**

The first navigation page is Theme. Locate the embedded summary `Label` containing `"Body Segoe UI 12"` after layout and assert its client rectangle is non-empty and its text is not truncated by a zero/negative layout result. Keep the assertion semantic; do not lock the whole page to a screenshot or machine-specific pixel dimensions.

- [ ] **Step 5: Run the new layout tests and fix only demonstrated local constraints**

Run:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter FullyQualifiedName~IntegratedDemoTypographyLayoutTests --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter FullyQualifiedName~IntegratedDemoTypographyLayoutTests --blame-hang --blame-hang-timeout 5m
```

If a fixed-size container fails, repair that container locally using its existing layout model. Prefer one of these concrete patterns, in order:

1. make a text row `AutoSize`/`GrowAndShrink` when the row is purely textual;
2. increase a local minimum height based on `PreferredSize.Height + existing vertical padding`;
3. widen only the specific text-bearing column/control that is clipped;
4. retain intended `AutoScroll` for large demo content rather than making the whole shell arbitrarily larger.

Do **not** multiply every margin, padding, control height, or theme metric by `4/3`; this task changes typography, not the framework's spacing system.

- [ ] **Step 6: Commit layout regression coverage and any evidence-driven local fixes**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyLayoutTests.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo
git commit -m "test: harden integrated demo layout for 12pt typography"
```

Before committing, inspect the staged diff and ensure no unrelated demo files were accidentally staged by the directory-level `git add` command.

---

### Task 8: Full dual-target verification and manual DPI/theme acceptance

**Files:**
- No new production files expected.
- Modify only regression tests or the specific demo layout file if verification finds a reproducible defect.

**Interfaces:**
- Consumes: all previous tasks.
- Produces: evidence that the Integrated Demo typography change is demo-scoped, dual-target compatible, DPI-aware, and stable across themes.

- [ ] **Step 1: Build the demo on both target frameworks**

```powershell
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net48
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows
```

Expected: both builds exit successfully with no new warnings caused by the typography change.

- [ ] **Step 2: Run all demo tests on both frameworks with bounded hang detection**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter FullyQualifiedName~MyDmsVn.Bootstrap5WinFormUI.Tests.Demo --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter FullyQualifiedName~MyDmsVn.Bootstrap5WinFormUI.Tests.Demo --blame-hang --blame-hang-timeout 5m
```

Expected: zero failing demo tests and no modal-dialog hangs.

- [ ] **Step 3: Run the repository-authoritative full test script**

```powershell
./test.ps1
```

Expected: both configured target-framework runs complete successfully under the repository's hang-detection policy.

- [ ] **Step 4: Manually verify the Integrated Demo at 100%, 125%, 150%, and 200% Windows display scaling**

For each scaling level, verify:

- normal body text looks equivalent in hierarchy to a web application's `16px` base font, not like a fixed 16-physical-pixel bitmap font;
- `Theme` summary reports `Body Segoe UI 12pt`;
- native labels/group boxes and Bootstrap controls are visually aligned in body-size hierarchy;
- header title, description, Theme selector, and Reduced Motion checkbox do not overlap;
- no page introduces horizontal clipping solely because of the `12pt` body font when vertical scrolling is the intended overflow model;
- buttons/inputs still calculate usable preferred heights;
- TreeView/ListView/DataGrid text remains readable without breaking native row/item interaction.

- [ ] **Step 5: Verify Light/Dark/Reduced Motion transitions at runtime**

Switch Light → Dark → Light and toggle Reduced Motion in both states. After every transition verify:

```text
Typography.Body = Segoe UI 12pt
Typography.BodySmall = Segoe UI 10.5pt
Typography.Label = Segoe UI 12pt Bold
Typography.HeadingSmall = Segoe UI 15pt Bold
Typography.HeadingMedium = Segoe UI 18pt Bold
```

Colors and motion settings must change as requested; typography must remain stable.

- [ ] **Step 6: Verify the production default contract remains unchanged**

Run the focused production-default guard from `IntegratedDemoTypographyTests` and inspect `src/MyDmsVn.Bootstrap5WinFormUI/Theme/BootstrapThemeTypography.cs` in the final diff. There must be no implementation edit that changes `BootstrapThemeTypography.Default` to `12pt`.

- [ ] **Step 7: Final diff hygiene check**

```bash
git status --short
git diff --check
git diff --stat
```

Expected:

- no generated `bin/` or `obj/` files;
- no external dependency changes;
- no production theme-default change;
- only demo infrastructure/forms, demo tests, and this plan are involved.

- [ ] **Step 8: Commit any final evidence-driven corrections**

Only if verification required a concrete correction:

```bash
git add <specific corrected files>
git commit -m "fix: finish integrated demo typography verification"
```

Do not create an empty verification commit.

---

## Acceptance Criteria

Implementation is complete only when all of the following are true:

1. Integrated Demo normal/body typography is `Segoe UI 12pt`, representing browser-default `16px` at the 96-DPI CSS reference.
2. Standard WinForms controls in demo forms inherit the `12pt` body font through `DemoFormBase`.
3. Bootstrap controls continue using their existing theme-font path and receive `12pt` from the demo-specific active theme.
4. Every concrete form in `MyDmsVn.Bootstrap5WinFormUI.Demo` derives from `DemoFormBase`; a reflection test protects that invariant.
5. `BootstrapThemeTypography.Default.Body` remains `Segoe UI 9pt` in the production library.
6. Light/Dark switching does not reset the Integrated Demo to `9pt`.
7. Reduced Motion switching does not reset the Integrated Demo to `9pt`.
8. Demo theme normalization preserves current colors, metrics, mode, and reduced-motion state when it is only replacing typography.
9. Demo font creation uses `GraphicsUnit.Point`, not fixed physical pixels.
10. All demo forms use `AutoScaleMode.Dpi` through the shared base.
11. Owned demo `Font` instances are disposed deterministically.
12. Both `net48` and `net8.0-windows` demo builds succeed.
13. Focused demo tests pass on both TFMs with bounded hang detection.
14. `./test.ps1` passes without modal UI hangs.
15. Manual checks at 100/125/150/200% Windows scaling show readable, non-overlapping Integrated Demo chrome and representative pages.
16. No public production API, dependency, or framework-wide typography default changes are introduced.

---

## Explicit Non-Goals

This plan does **not**:

- change the framework-wide default body font from `9pt` to `12pt`;
- claim that all WinForms applications using this package should use a browser-like `16px` base size;
- add a user-facing font-size selector to the Integrated Demo;
- add browser CSS units (`px`, `rem`) to the WinForms API;
- convert every existing fixed spacing/control-height metric by the same ratio as the font size;
- change Bootstrap component sizing enums (`Small`, `Default`, `Large`);
- replace DPI autoscaling with font autoscaling;
- use `Application.SetDefaultFont()` or a .NET 8-only startup API;
- alter component-specific explicit fonts that are intentionally part of a demo scenario;
- modify rendering, input, focus, popup, keyboard, or accessibility behavior except where a local layout correction is required to prevent text clipping.

---

## Implementation Notes for Reviewers

Review this change as a **demo typography policy** rather than a production-theme redesign. The most important review questions are:

- Is the production `BootstrapThemeTypography.Default` untouched?
- Does the demo establish its theme before theme-font Bootstrap controls are constructed?
- Does direct construction of a demo form also normalize typography, so tests and standalone diagnostics do not depend on `Program.Main()`?
- Does `MainForm.PublishSelectedTheme()` use the demo factory rather than `BootstrapTheme.CreateDefault()`?
- Are native controls getting font inheritance through the form instead of recursive font mutation?
- Is all GDI `Font` ownership explicit and disposed?
- Are layout changes evidence-driven and local rather than a broad spacing rescale?
- Are both TFMs and unattended WinForms test safeguards preserved?
