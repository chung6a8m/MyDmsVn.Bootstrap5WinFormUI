# Integrated Demo Browser-Equivalent Typography Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Integrated Demo use a normal body font visually equivalent to the browser default `16px` (`12pt` at the 96-DPI CSS reference) across native WinForms controls and Bootstrap controls, while preserving DPI scaling, theme switching, both target frameworks, unattended test safety, and the production library's existing default typography contract.

**Architecture:** Keep the policy entirely in the demo assembly. `DemoFormBase` owns only form-local concerns (`Segoe UI 12pt`, `AutoScaleMode.Dpi`, deterministic `Font` disposal) and must never mutate `BootstrapThemeManager.CurrentTheme`. `DemoThemeFactory` creates demo-specific themes, and global theme publication happens explicitly at application/test composition boundaries before theme-font Bootstrap controls are constructed. `MainForm` continues to publish a fresh demo theme when Light/Dark or Reduced Motion changes so typography never falls back to the framework `9pt` default.

**Tech Stack:** C#, WinForms, `System.Drawing.Font`, `GraphicsUnit.Point`, existing `BootstrapTheme`, `BootstrapThemeTypography`, `BootstrapFontToken`, `BootstrapThemeManager`, `net48;net8.0-windows`, NUnit, existing Integrated Demo and WinForms test infrastructure.

**Spec:** User requirement from the 2026-09-07 Integrated Demo typography discussion, bounded by `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/WINFORMS_TEST_EXECUTION.md`, and repository rules in `AGENTS.md`.

## Global Constraints

- Scope is the Integrated Demo only. Do not change the production framework's default typography merely to make the demo larger.
- Keep `BootstrapThemeTypography.Default.Body` at its current `Segoe UI 9pt` contract unless a separate framework-wide typography change is explicitly approved.
- Treat browser-default `16 CSS px` as `12pt` at the 96-DPI CSS reference: `16 × 72 / 96 = 12`.
- Use `GraphicsUnit.Point`; do not hard-code `16` physical pixels with `GraphicsUnit.Pixel`.
- Do not set a WinForms font to `16f` expecting CSS `16px`; the normal `Font` constructor interprets that as `16pt`.
- Use `AutoScaleMode.Dpi` for demo forms. Do not introduce font-based autoscaling as a second scaling model.
- Do not use `Application.SetDefaultFont()` because the demo must continue targeting `net48;net8.0-windows` with one coherent implementation.
- Keep runtime targets exactly `net48;net8.0-windows`.
- Do not introduce a new package or font dependency. Use `Segoe UI`, which is already the framework's default font family.
- Preserve Light/Dark switching and Reduced Motion switching. Changing either must not reset demo typography to framework-default `9pt`.
- `DemoFormBase` must not publish, normalize, replace, or otherwise mutate `BootstrapThemeManager.CurrentTheme` from its constructor or lifecycle methods.
- Global demo theme publication must be explicit at composition boundaries: application startup, the Integrated Demo theme controls, and test setup/scopes that intentionally exercise demo typography.
- Any `Font` instance created by demo infrastructure is owned by that infrastructure and must be disposed deterministically.
- Do not set fonts recursively on every child control. Native WinForms controls should inherit the form font; Bootstrap controls should continue using their existing theme-font mechanism.
- Do not weaken a Bootstrap control's current `UseThemeFont`/theme ownership behavior just to make the demo match `12pt`.
- `DemoFormBase` is allowed to be public because existing public demo forms must be able to inherit from it; this is a demo-assembly API only, not a public API addition to the production package.
- Do not add demo typography settings to the public production package API.
- Use existing unattended WinForms test rules: STA for handle/UI tests, no modal UI, bounded message pumping only, and `--blame-hang --blame-hang-timeout 5m` for focused raw `dotnet test` runs.
- Run the complete suite through `./test.ps1` before considering implementation complete.

---

## Why Two Explicit Typography Paths Are Required

The Integrated Demo mixes two font acquisition paths:

1. Standard WinForms controls such as `Label`, `GroupBox`, `FlowLayoutPanel`, and ordinary `Button` normally inherit `Font` from their parent/form unless explicitly overridden.
2. Bootstrap controls may own a theme font. For example, `BootstrapButton` constructs its font from `BootstrapThemeManager.CurrentTheme.Typography.Body` when theme-font mode is enabled.

Therefore the running Integrated Demo needs both:

```text
DemoFormBase.Font = Segoe UI 12pt
                +
BootstrapThemeManager.CurrentTheme.Typography.Body = Segoe UI 12pt
```

These responsibilities must remain separate:

```text
DemoFormBase
  -> local Form.Font + AutoScaleMode only

Program.Main / MainForm theme controls / explicit test setup
  -> publish DemoThemeFactory.Create(...)
```

Do not rely on a base-form constructor to establish the application-global theme. Besides causing hidden global state changes in tests, derived instance field initializers can construct Bootstrap controls before the base constructor body runs. The application composition root must therefore publish the demo theme before constructing `MainForm`.

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

The key acceptance requirement is `Body = 12pt`. The other roles keep a consistent hierarchy after increasing the demo body size.

Do not copy these values into `BootstrapThemeTypography.Default`; they belong to the Integrated Demo.

---

## File Structure

### New demo infrastructure

- Create `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoTypography.cs`
  - Owns browser-equivalent point-size constants.
  - Creates immutable `BootstrapThemeTypography` tokens.
  - Creates a caller-owned `Font` for native WinForms inheritance.
- Create `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoThemeFactory.cs`
  - Creates Light/Dark demo themes with demo typography.
  - Has no method that mutates `BootstrapThemeManager.CurrentTheme` implicitly.
- Create `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoFormBase.cs`
  - Is `public abstract` because public demo forms inherit from it.
  - Sets `AutoScaleMode.Dpi`.
  - Owns/disposes the `Segoe UI 12pt` form font.
  - Does not read or write application-global theme state.

### Existing demo shell/theme files

- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/Program.cs`.
- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`.
- Modify `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoPageHostForm.cs`.
- Modify every current Integrated Demo `*DemoForm` listed in Tasks 4-6 so it derives from `DemoFormBase` rather than directly from `Form`.

### Tests

- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`.
- Create `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyLayoutTests.cs`.

The existing test project already references the demo project, so do not add another project reference solely for these tests.

---

## Interfaces

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

Keep the factory side-effect free and make it public in the demo assembly so the separate test project can explicitly create the same demo theme without duplicating token values. This does **not** add API to the production package.

Contract:

```csharp
public static class DemoThemeFactory
{
    public static BootstrapTheme Create(
        BootstrapThemeMode mode,
        bool reducedMotion = false);
}
```

`Create()` returns a new theme but never assigns `BootstrapThemeManager.CurrentTheme`:

```csharp
return new BootstrapTheme(
    mode,
    BootstrapThemeColors.CreateDefault(mode),
    BootstrapThemeMetrics.Default,
    DemoTypography.CreateThemeTypography(),
    reducedMotion);
```

There is deliberately no `EnsureCurrentThemeUsesDemoTypography()` method. Hidden normalization from form construction is not part of the design.

### `DemoFormBase`

```csharp
public abstract class DemoFormBase : Form
{
    private Font? _demoBodyFont;

    protected DemoFormBase()
    {
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

`DemoFormBase` must not touch `BootstrapThemeManager.CurrentTheme`. Directly constructing a demo form outside the real application startup path may therefore use whatever application theme the caller intentionally installed; only the form-local native font policy is automatic.

---

### Task 1: Lock the Integrated Demo typography and global-state contracts with failing tests

**Files:**
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`
- Reference only: `src/MyDmsVn.Bootstrap5WinFormUI/Theme/BootstrapThemeTypography.cs`
- Reference only: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapButton.cs`
- Reference only: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`

**Interfaces:**
- Consumes: existing `BootstrapThemeManager`, `BootstrapTheme`, `BootstrapThemeTypography.Default`, `MainForm`, representative demo forms, `BootstrapButton`.
- Produces: regression contracts for `12pt` demo body typography, theme-toggle persistence, unchanged production defaults, and absence of hidden global-theme mutation from form construction.

- [ ] **Step 1: Add an STA fixture that always saves/restores the application-global theme**

Use this skeleton:

```csharp
[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class IntegratedDemoTypographyTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
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

- [ ] **Step 2: Add the failing form-local body-font regression test**

```csharp
[Test]
public void MainFormUsesBrowserEquivalentTwelvePointNativeBodyTypography()
{
    using var form = new MainForm();

    Assert.Multiple((Action)(() =>
    {
        Assert.That(form.AutoScaleMode, Is.EqualTo(AutoScaleMode.Dpi));
        Assert.That(form.Font.Name, Is.EqualTo("Segoe UI"));
        Assert.That(form.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
    }));

    var themeLabel = FindControls<Label>(form).Single(label => label.Text == "Theme");
    Assert.That(themeLabel.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
}
```

Expected before implementation: FAIL because `MainForm` does not yet inherit the demo `12pt` form font policy.

- [ ] **Step 3: Add a regression proving form construction does not mutate global theme state**

Use a representative form that will migrate to `DemoFormBase`:

```csharp
[Test]
public void ConstructingDemoFormDoesNotReplaceApplicationTheme()
{
    var installed = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark, reducedMotion: true);
    BootstrapThemeManager.CurrentTheme = installed;

    using var form = new ButtonDemoForm();

    Assert.That(BootstrapThemeManager.CurrentTheme, Is.SameAs(installed));
}
```

This test must PASS after `DemoFormBase` is introduced. Do not replace it with a test that expects direct form construction to normalize global typography.

- [ ] **Step 4: Add the failing theme-switch persistence test**

Construct `MainForm`, change the Theme selector to Dark and enable Reduced Motion. This exercises `MainForm.PublishSelectedTheme()` and therefore the demo theme factory. Assert:

```csharp
var theme = BootstrapThemeManager.CurrentTheme;
Assert.Multiple((Action)(() =>
{
    Assert.That(theme.Mode, Is.EqualTo(BootstrapThemeMode.Dark));
    Assert.That(theme.ReducedMotion, Is.True);
    Assert.That(theme.Typography.Body.FontFamilyName, Is.EqualTo("Segoe UI"));
    Assert.That(theme.Typography.Body.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
    Assert.That(theme.Typography.BodySmall.SizeInPoints, Is.EqualTo(10.5f).Within(0.01f));
    Assert.That(theme.Typography.Label.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
    Assert.That(theme.Typography.HeadingSmall.SizeInPoints, Is.EqualTo(15f).Within(0.01f));
    Assert.That(theme.Typography.HeadingMedium.SizeInPoints, Is.EqualTo(18f).Within(0.01f));
}));

using var themedButton = new BootstrapButton();
Assert.That(themedButton.Font.SizeInPoints, Is.EqualTo(12f).Within(0.01f));
```

Expected before implementation: FAIL because `MainForm.PublishSelectedTheme()` currently calls `BootstrapTheme.CreateDefault(...)`, which restores the framework typography.

- [ ] **Step 5: Add a guard proving the production default remains unchanged**

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

- [ ] **Step 6: Run the focused tests on both TFMs and capture the intended red state**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter FullyQualifiedName~IntegratedDemoTypographyTests --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter FullyQualifiedName~IntegratedDemoTypographyTests --blame-hang --blame-hang-timeout 5m
```

Expected: the new `12pt` form/theme-switch tests are red on the current implementation; the production-default guard remains green. The no-global-mutation test should remain green both before and after the feature.

- [ ] **Step 7: Commit the red tests**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs
git commit -m "test: define integrated demo typography contract"
```

---

### Task 2: Add demo-specific typography, side-effect-free theme factory, and public base form

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoTypography.cs`
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoThemeFactory.cs`
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoFormBase.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`

**Interfaces:**
- Consumes: `BootstrapFontToken`, `BootstrapThemeTypography`, `BootstrapTheme`, `BootstrapThemeColors`, `BootstrapThemeMetrics`.
- Produces: `DemoTypography.CreateThemeTypography()`, `DemoTypography.CreateBodyFont()`, side-effect-free public demo-assembly `DemoThemeFactory.Create(...)`, and `public abstract DemoFormBase`.

- [ ] **Step 1: Create `DemoTypography` exactly from the contract above**

Use the five constants and explicit `GraphicsUnit.Point` font creation. Do not use `GraphicsUnit.Pixel`.

- [ ] **Step 2: Create public demo-assembly `DemoThemeFactory.Create(...)` as a pure factory**

Implement only theme creation. The public visibility exists so the separate demo test project can install the exact same theme explicitly; it is not part of the production library package. The factory must not assign `BootstrapThemeManager.CurrentTheme`, subscribe to theme events, or normalize an existing theme.

- [ ] **Step 3: Create `public abstract DemoFormBase`**

Use the exact base-class contract above. The `public` accessibility is required because current demo forms such as `MainForm` and `ButtonDemoForm` are public; an `internal` base would cause C# inconsistent-accessibility build errors.

- [ ] **Step 4: Keep `DemoFormBase` free of global theme state**

The constructor must contain only the local DPI/font policy. In particular, do not add any equivalent of:

```csharp
DemoThemeFactory.EnsureCurrentThemeUsesDemoTypography();
BootstrapThemeManager.CurrentTheme = ...;
```

- [ ] **Step 5: Build both demo targets**

```powershell
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net48
```

Expected: both builds PASS. Behavioral tests that require shell wiring may still be red until Task 3.

- [ ] **Step 6: Commit the infrastructure**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoTypography.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoThemeFactory.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoFormBase.cs
git commit -m "feat: add integrated demo typography infrastructure"
```

---

### Task 3: Publish demo theme explicitly at application boundaries

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/Program.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Test: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`

**Interfaces:**
- Consumes: `DemoThemeFactory.Create(...)`, `DemoFormBase`.
- Produces: a running Integrated Demo where startup, Light/Dark switching, and Reduced Motion intentionally publish demo typography without relying on form-constructor side effects.

- [ ] **Step 1: Publish the demo theme before constructing `MainForm` in real startup**

After DPI/visual-style initialization and before `new MainForm()`:

```csharp
BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(BootstrapThemeMode.Light);
Application.Run(new MainForm());
```

This ordering is mandatory. Do not move theme publication into `DemoFormBase`: derived field initializers may create Bootstrap controls before the base constructor body executes.

- [ ] **Step 2: Move `MainForm` onto the public shared base**

Change:

```csharp
public sealed class MainForm : Form
```

to:

```csharp
public sealed class MainForm : DemoFormBase
```

Do not add a second local `Font` assignment.

- [ ] **Step 3: Change runtime theme publishing to use the demo factory**

Replace the current `BootstrapTheme.CreateDefault(mode, _reducedMotion.Checked)` assignment with:

```csharp
BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
    mode,
    _reducedMotion.Checked);
```

- [ ] **Step 4: Update focused tests so application-global theme setup is explicit**

Tests that assert Bootstrap controls use the `12pt` theme must explicitly reach a demo-theme publication boundary before constructing/asserting theme-font controls. Do not make plain `new MainForm()` or `new ButtonDemoForm()` responsible for publishing global theme state.

- [ ] **Step 5: Run focused typography tests on both TFMs**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter FullyQualifiedName~IntegratedDemoTypographyTests --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter FullyQualifiedName~IntegratedDemoTypographyTests --blame-hang --blame-hang-timeout 5m
```

Expected: native form font, theme-toggle typography, no-global-mutation, and production-default guards PASS on both targets.

- [ ] **Step 6: Commit application-shell wiring**

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

**Interfaces:**
- Consumes: `DemoFormBase` and the application-installed demo theme.
- Produces: shared inherited native body typography and DPI policy for host/foundation pages without hidden global theme changes.

- [ ] **Step 1: Change each listed concrete form from direct `Form` inheritance to `DemoFormBase`**

Do not change `DemoPageSection`; it is not a form.

- [ ] **Step 2: Remove only exact duplicate `AutoScaleMode = AutoScaleMode.Dpi` assignments**

Keep unrelated layout, sizing, rendering, and control behavior unchanged.

- [ ] **Step 3: Verify Theme page reports the demo body token when launched under the demo theme**

Its existing summary should contain the equivalent of:

```text
Body Segoe UI 12pt
```

Do not add a duplicate typography label just for this feature.

- [ ] **Step 4: Build both targets and run the relevant demo tests with bounded hang detection**

Use the existing fixture names. If a composite NUnit filter proves adapter-specific, run each fixture by fully qualified name instead of removing hang detection.

- [ ] **Step 5: Commit the foundation migration**

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
- Test: existing matching fixtures under `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/`.

**Interfaces:**
- Consumes: `DemoFormBase`, active demo theme at application/test composition boundaries.
- Produces: consistent native body typography across control/input/feedback pages while preserving existing Bootstrap theme-font ownership.

- [ ] **Step 1: Change inheritance for all listed forms**

Replace only direct `: Form` inheritance with `: DemoFormBase`. Do not recursively assign `Font` to child controls.

- [ ] **Step 2: Remove duplicate local DPI-mode assignments where they exactly duplicate the base**

Do not remove explicit font assignments that intentionally demonstrate a font-specific component feature.

- [ ] **Step 3: Ensure existing tests do not depend on constructor-driven global theme mutation**

Existing fixtures that simply do `new SomeDemoForm()` must remain valid without having to save/restore theme unless they intentionally publish a demo theme. If a fixture needs theme-specific typography, install/restore the theme explicitly in that fixture.

- [ ] **Step 4: Run the complete demo-test namespace on `net8.0-windows`**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter FullyQualifiedName~MyDmsVn.Bootstrap5WinFormUI.Tests.Demo --blame-hang --blame-hang-timeout 5m
```

- [ ] **Step 5: Run the same demo-test namespace on `net48`**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter FullyQualifiedName~MyDmsVn.Bootstrap5WinFormUI.Tests.Demo --blame-hang --blame-hang-timeout 5m
```

- [ ] **Step 6: Commit the control/input/feedback migration**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AccordionDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/BootstrapSelectDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ButtonDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ButtonGroupToolbarDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ChecksDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/CollapseDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/FeedbackDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/InputGroupDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/ProgressDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/SpinnerDemoForm.cs demo/MyDmsVn.Bootstrap5WinFormUI.Demo/TextBoxCardDemoForm.cs
git commit -m "refactor: unify integrated demo control typography"
```

---

### Task 6: Migrate data/navigation demo forms and enforce scoped form coverage

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
- Produces: current Integrated Demo page coverage plus a reflection guard that is type-safe and scoped to Integrated Demo shell/page naming conventions.

- [ ] **Step 1: Change all listed forms to derive from `DemoFormBase`**

Remove only duplicate `AutoScaleMode.Dpi` assignments and preserve all component-specific behavior.

- [ ] **Step 2: Add a scoped reflection guard**

Do not compare `BaseType.Name` to a string. Use `typeof(DemoFormBase).IsAssignableFrom(type)` so the guard is type-safe and permits a future specialized demo base class.

Scope the invariant to the current Integrated Demo naming convention rather than every arbitrary helper/dialog `Form` that may ever exist in the namespace:

```csharp
[Test]
public void IntegratedDemoShellAndPageFormsUseSharedDemoFormBase()
{
    var demoAssembly = typeof(MainForm).Assembly;
    var offenders = demoAssembly
        .GetTypes()
        .Where(type =>
            type.Namespace == "MyDmsVn.Bootstrap5WinFormUI.Demo" &&
            !type.IsAbstract &&
            typeof(Form).IsAssignableFrom(type) &&
            (type.Name == "MainForm" ||
             type.Name == "DemoPageHostForm" ||
             type.Name.EndsWith("DemoForm", StringComparison.Ordinal)))
        .Where(type => !typeof(DemoFormBase).IsAssignableFrom(type))
        .Select(type => type.FullName)
        .OrderBy(name => name)
        .ToArray();

    Assert.That(offenders, Is.Empty);
}
```

This intentionally does not force an unrelated future helper/dialog form to inherit `DemoFormBase` solely because it shares the namespace.

- [ ] **Step 3: Run the full Demo test namespace on both TFMs**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter FullyQualifiedName~MyDmsVn.Bootstrap5WinFormUI.Tests.Demo --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter FullyQualifiedName~MyDmsVn.Bootstrap5WinFormUI.Tests.Demo --blame-hang --blame-hang-timeout 5m
```

Expected: PASS, including existing DataGrid guards and unattended WinForms protections.

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
  - Any individual demo form whose existing fixed logical height/width is proven too small at `12pt`.

**Interfaces:**
- Consumes: completed demo typography/base-form migration and explicit demo-theme setup.
- Produces: deterministic no-clipping checks for shared shell and representative native/themed content without globally inflating spacing metrics.

- [ ] **Step 1: Add an STA layout fixture with explicit demo-theme save/install/restore**

Save `BootstrapThemeManager.CurrentTheme`, explicitly install `DemoThemeFactory.Create(BootstrapThemeMode.Light)` before constructing forms, restore the original theme in teardown, and never call `Application.Run()` or `ShowDialog()`.

- [ ] **Step 2: Add a shell chrome containment check**

Construct `MainForm`, call `CreateControl()` and `PerformLayout()`, locate header/title/description/theme controls, and assert visible children remain within parent client bounds. Prefer containment/preferred-size assertions over machine-specific screenshot dimensions.

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

Do not require every page to fit without scrollbars; several pages intentionally use `AutoScroll`.

- [ ] **Step 3: Add a representative native-vs-themed sizing check**

Under explicit demo-theme setup, assert a native inherited `Label` and a theme-font `BootstrapButton` both use approximately `12pt`. Verify the button has positive preferred height and can auto-size without text clipping.

- [ ] **Step 4: Add a Theme page summary check**

Assert the existing Theme summary contains `Body Segoe UI 12` and has non-empty client bounds. Do not add screenshot-lock tests.

- [ ] **Step 5: Run both TFM layout fixtures with bounded hang detection**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter FullyQualifiedName~IntegratedDemoTypographyLayoutTests --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter FullyQualifiedName~IntegratedDemoTypographyLayoutTests --blame-hang --blame-hang-timeout 5m
```

If a fixed-size container fails, repair only the demonstrated local constraint. Prefer `AutoSize`, a local minimum derived from `PreferredSize`, a targeted width adjustment, or existing `AutoScroll`. Do not multiply every margin/padding/control height/theme metric by `4/3`.

- [ ] **Step 6: Commit layout regression coverage and evidence-driven fixes**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyLayoutTests.cs <specific-demo-files-if-needed>
git commit -m "test: harden integrated demo layout for 12pt typography"
```

---

### Task 8: Full dual-target verification and manual DPI/theme acceptance

**Files:**
- No new production files expected.
- Modify only regression tests or the specific demo layout file if verification finds a reproducible defect.

**Interfaces:**
- Consumes: all previous tasks.
- Produces: evidence that the Integrated Demo typography change is demo-scoped, dual-target compatible, DPI-aware, theme-stable, and free from constructor-driven global-theme side effects.

- [ ] **Step 1: Build the demo on both target frameworks**

```powershell
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net48
dotnet build demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows
```

- [ ] **Step 2: Run all demo tests on both frameworks with bounded hang detection**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net48 --filter FullyQualifiedName~MyDmsVn.Bootstrap5WinFormUI.Tests.Demo --blame-hang --blame-hang-timeout 5m
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -f net8.0-windows --filter FullyQualifiedName~MyDmsVn.Bootstrap5WinFormUI.Tests.Demo --blame-hang --blame-hang-timeout 5m
```

- [ ] **Step 3: Run the repository-authoritative full test script**

```powershell
./test.ps1
```

Expected: both configured target-framework runs complete successfully with no modal-dialog hangs.

- [ ] **Step 4: Manually verify the Integrated Demo at 100%, 125%, 150%, and 200% Windows display scaling**

For each scaling level verify:

- normal body text has the intended browser-like `16px` hierarchy via `12pt` logical font sizing;
- Theme summary reports `Body Segoe UI 12pt`;
- native controls and Bootstrap controls align in body-size hierarchy;
- header controls do not overlap;
- intended scrollable pages remain usable;
- buttons/inputs calculate usable preferred heights;
- TreeView/ListView/DataGrid remain readable and interactive.

- [ ] **Step 5: Verify Light/Dark/Reduced Motion transitions**

Switch Light -> Dark -> Light and toggle Reduced Motion. After each publication verify the five demo typography tokens remain `12 / 10.5 / 12 bold / 15 bold / 18 bold` points.

- [ ] **Step 6: Verify direct demo-form construction does not publish a theme**

Run the focused `ConstructingDemoFormDoesNotReplaceApplicationTheme` test and inspect `DemoFormBase.cs`. There must be no assignment to `BootstrapThemeManager.CurrentTheme` in the base class.

- [ ] **Step 7: Verify the production default contract remains unchanged**

Run the production-default guard and inspect `src/MyDmsVn.Bootstrap5WinFormUI/Theme/BootstrapThemeTypography.cs`. There must be no implementation edit changing the default body to `12pt`.

- [ ] **Step 8: Final diff hygiene check**

```bash
git status --short
git diff --check
git diff --stat
```

Expected: no generated `bin/`/`obj/`, no dependency changes, no production theme-default change, and only demo infrastructure/forms/tests plus plan updates.

- [ ] **Step 9: Commit any final evidence-driven corrections**

Only if verification required a concrete correction. Do not create an empty verification commit.

---

## Acceptance Criteria

Implementation is complete only when all of the following are true:

1. Integrated Demo normal/body typography is `Segoe UI 12pt`, representing browser-default `16px` at the 96-DPI CSS reference.
2. Standard WinForms controls in Integrated Demo forms inherit the `12pt` body font through `DemoFormBase`.
3. Bootstrap controls continue using their existing theme-font path and receive demo typography from an explicitly published demo theme.
4. `DemoFormBase` is `public abstract`, allowing existing public demo forms to inherit without inconsistent-accessibility errors.
5. `DemoFormBase` does not mutate `BootstrapThemeManager.CurrentTheme`; direct construction of a demo form leaves the installed application theme instance unchanged.
6. Real application startup publishes the demo theme before constructing `MainForm`.
7. Light/Dark and Reduced Motion theme publications retain the five demo typography tokens.
8. The scoped reflection guard uses `typeof(DemoFormBase).IsAssignableFrom(type)` and covers Integrated Demo shell/page forms without forcing unrelated future helper/dialog forms in the namespace onto this base.
9. `BootstrapThemeTypography.Default.Body` remains `Segoe UI 9pt` in the production library.
10. Demo font creation uses `GraphicsUnit.Point`, not fixed physical pixels.
11. Integrated Demo forms use `AutoScaleMode.Dpi` through the shared base unless a documented component-specific exception is proven necessary.
12. Owned demo `Font` instances are disposed deterministically.
13. Both `net48` and `net8.0-windows` demo builds succeed.
14. Focused demo tests pass on both TFMs with bounded hang detection.
15. `./test.ps1` passes without modal UI hangs.
16. Manual checks at 100/125/150/200% Windows scaling show readable, non-overlapping Integrated Demo chrome and representative pages.
17. No public production API, dependency, or framework-wide typography default changes are introduced.

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
- normalize or overwrite application-global theme state from a form constructor;
- alter component-specific explicit fonts that are intentionally part of a demo scenario;
- modify rendering, input, focus, popup, keyboard, or accessibility behavior except where a local layout correction is required to prevent text clipping.

---

## Implementation Notes for Reviewers

Review this as a **demo typography policy**, not a production-theme redesign. The key review questions are:

- Is `BootstrapThemeTypography.Default` untouched?
- Is `DemoFormBase` public enough for current public demo forms to inherit?
- Does `DemoFormBase` avoid all hidden application-global theme mutation?
- Does startup publish the demo theme before constructing `MainForm` and therefore before derived field-initialized Bootstrap controls are created?
- Does `MainForm.PublishSelectedTheme()` use the demo factory rather than `BootstrapTheme.CreateDefault()`?
- Do tests that need the demo theme install/restore it explicitly rather than depending on `new DemoForm()` side effects?
- Does the coverage guard use actual type identity/assignability and a deliberate Integrated Demo scope?
- Are native controls getting font inheritance through the form instead of recursive font mutation?
- Is all GDI `Font` ownership explicit and disposed?
- Are layout changes evidence-driven and local rather than a broad spacing rescale?
- Are both TFMs and unattended WinForms test safeguards preserved?
