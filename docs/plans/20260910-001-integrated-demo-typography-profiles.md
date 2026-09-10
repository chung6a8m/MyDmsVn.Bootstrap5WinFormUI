# Integrated Demo Typography Profiles Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an Integrated Demo `Base font` selector beside the existing Light/Dark theme selector so the demo can switch at runtime among `Default`, `Base 14px`, and `Base 16px` typography profiles while preserving the Base 16px startup behavior introduced by PR #62.

**Architecture:** Treat theme mode, typography profile, and reduced-motion preference as three independent inputs that are composed into one immutable `BootstrapTheme` and published only through `BootstrapThemeManager.CurrentTheme`. Keep all profile definitions in demo-scoped typography infrastructure, reuse `BootstrapThemeTypography.Default` for the framework-default profile, and make every demo-owned native/cached font react to `ThemeChanged` without recreating demo pages. Do not change the framework's core default typography or add a second global typography manager/event channel.

**Tech Stack:** C#, native WinForms, `net48;net8.0-windows`, existing `BootstrapTheme`, `BootstrapThemeManager`, `BootstrapThemeTypography`, `BootstrapFontToken`, Integrated Demo project, NUnit 4, STA WinForms tests.

**Spec:** Approved post-PR #62 analysis for Integrated Demo typography profiles, together with `docs/DESIGN_SYSTEM.md`, repository rules in `AGENTS.md`, and the PR #62 baseline currently on `main`.

## Global Constraints

- Read `README.md`, `AI_CONTEXT.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPMENT_PLAN.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/WINFORMS_TEST_EXECUTION.md`, and relevant design-system/demo documentation before changing product/demo code, as required by `AGENTS.md`.
- Keep root namespace `MyDmsVn.Bootstrap5WinFormUI` and project targets `net48;net8.0-windows` unchanged.
- This feature belongs to the Integrated Demo. Do **not** change `BootstrapThemeTypography.Default`, `BootstrapTheme.CreateDefault(...)`, or the default typography of the core package.
- Keep Light/Dark mode, typography profile, and reduced motion independent. The selected typography profile must survive Light/Dark and reduced-motion changes, and changing typography must preserve the current mode and reduced-motion value.
- Publish all combinations as a complete immutable `BootstrapTheme` through `BootstrapThemeManager.CurrentTheme`. Do **not** add `DemoTypographyManager`, a second global event, static mutable font-size state, or per-control font-size settings.
- The normal Integrated Demo startup must remain **Base 16px** so this change does not silently revert PR #62 behavior.
- The selector label is `Base font`; its items, in order, are exactly `Default`, `Base 14px`, and `Base 16px`.
- `Default` must reuse the exact framework typography object `BootstrapThemeTypography.Default`; do not copy its current numeric values into demo constants.
- `Base 16px` must preserve the typography hierarchy introduced by PR #62: Segoe UI; Body `12f`; BodySmall `10.5f`; Label `12f` Bold; HeadingSmall `15f` Bold; HeadingMedium `18f` Bold.
- `Base 14px` is the initial 14/16 proportional version of the PR #62 profile: Segoe UI; Body `10.5f`; BodySmall `9.1875f`; Label `10.5f` Bold; HeadingSmall `13.125f` Bold; HeadingMedium `15.75f` Bold. Keep these values centralized in the profile definition; controls must never calculate `14f / 16f` themselves.
- Profile switching must update already-created Integrated Demo forms and controls. Do not dispose/recreate the active demo page merely to apply a different typography profile.
- Native controls that inherit their form font must follow the active `Typography.Body`. Bootstrap controls must continue to follow their semantic theme typography through existing framework behavior.
- Explicit semantic fonts owned by the demo, such as the MainForm page-title font and Pagination section-heading font, must refresh from the corresponding active typography token.
- Every demo-owned `Font` must have deterministic ownership and disposal. Replace a cached font by assigning the new font to all consumers first, then dispose the old owned font.
- Avoid allocating a new native form font when a theme change only changes Light/Dark colors or reduced motion and the typography token is unchanged.
- Keep the existing fixed framework metrics/density tokens unchanged in this feature. Typography switching must not implicitly create 14px/16px metric profiles.
- Keep the Integrated Demo header usable at its existing minimum window size. Do not solve selector width pressure by removing the title/description block or increasing `MinimumSize` unless an automated/manual layout check proves there is no viable compact layout.
- Tests that mutate global theme state or create WinForms controls must be `[Apartment(ApartmentState.STA)]` and `[NonParallelizable]`, restore `BootstrapThemeManager.CurrentTheme` in teardown, and follow `docs/WINFORMS_TEST_EXECUTION.md`.
- Never permit modal WinForms error dialogs during automated tests. Focused raw `dotnet test` commands must use bounded hang detection; the final full suite must run through `./test.ps1`.
- Build and test both target frameworks before completion.

---

## Current Baseline After PR #62

The implementation starts from these facts on `main`:

1. `BootstrapThemeTypography.Default` is still the compact framework default: Body `9f`, BodySmall `8.25f`, Label `9f` Bold, HeadingSmall `11f` Bold, HeadingMedium `14f` Bold.
2. `DemoTypography` currently represents one hard-coded Integrated Demo profile: the PR #62 Base 16px profile with Body `12f`.
3. `DemoThemeFactory.Create(mode, reducedMotion)` always injects that one demo typography profile.
4. `Program.Main` installs `DemoThemeFactory.Create(Light)` before creating `MainForm`, which makes the normal Integrated Demo startup use the Base 16px profile.
5. `DemoFormBase` currently creates one 12pt native body font in its constructor and never changes it when `BootstrapThemeManager.CurrentTheme` changes.
6. `MainForm` already republishes complete themes for Light/Dark and reduced motion, but has no typography selector and its cached page-title font does not refresh when typography changes.
7. `ThemeDemoForm` already rebuilds its summary from `theme.Typography`, so it should update automatically once profile changes are published correctly.
8. `PaginationDemoForm` currently snapshots `HeadingSmall` into `_sectionTitleFont` in its constructor. That cached font would become stale after a runtime typography-profile switch and therefore needs explicit refresh logic.
9. PR #62 added `IntegratedDemoTypographyTests` and `IntegratedDemoTypographyLayoutTests`; extend these tests instead of creating a competing test architecture.

---

## UX and Typography Contract

### Header layout

Keep the settings in one left-to-right group, adjacent as requested:

```text
Theme  [ Light ▼ ]   Base font  [ Base 16px ▼ ]   ☐ Reduced motion
```

The typography selector must be a native `ComboBox` with `DropDownStyle = ComboBoxStyle.DropDownList`. Give it `AccessibleName = "Integrated demo base font profile"` so tests and accessibility tooling do not have to identify it only from item contents.

### Profile matrix

| Selector item | `Typography.Body` | `BodySmall` | `Label` | `HeadingSmall` | `HeadingMedium` | Meaning |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| `Default` | 9pt | 8.25pt | 9pt Bold | 11pt Bold | 14pt Bold | Exact framework default via `BootstrapThemeTypography.Default` |
| `Base 14px` | 10.5pt | 9.1875pt | 10.5pt Bold | 13.125pt Bold | 14/16 proportional profile for compact desktop evaluation |
| `Base 16px` | 12pt | 10.5pt | 12pt Bold | 15pt Bold | PR #62 browser/Bootstrap-equivalent profile |

### Combination matrix

All six Light/Dark + typography combinations must be valid, with reduced motion independently on or off:

```text
Light + Default
Light + Base 14px
Light + Base 16px
Dark  + Default
Dark  + Base 14px
Dark  + Base 16px
```

Changing one selector must not reset either of the other two settings.

---

## File Map

### Create

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoTypographyPreset.cs` — demo-only enum defining the three selectable profiles.

### Modify

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoTypography.cs` — central immutable profile definitions, profile lookup, and demo-owned `Font` creation/comparison helpers.
- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoThemeFactory.cs` — compose requested mode + typography preset + reduced motion while preserving the existing Base 16px overload behavior.
- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoFormBase.cs` — synchronize the native inherited body font with `theme.Typography.Body` on runtime theme changes and dispose it safely.
- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs` — add the `Base font` selector, compose all three settings, synchronize selector state, theme the new header controls, and refresh the cached page-title semantic font.
- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/PaginationDemoForm.cs` — refresh the cached HeadingSmall font and all section-title labels when the typography profile changes.
- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/Program.cs` — make Base 16px startup selection explicit rather than depending only on an overload default.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs` — profile/factory/selector/runtime font regression coverage.
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyLayoutTests.cs` — parameterized containment/clipping checks for all profiles and shell sizes.
- `docs/DESIGN_SYSTEM.md` — document that Integrated Demo offers three evaluation profiles while core framework defaults remain unchanged.

### Audit, change only if the audit identifies a stale cached semantic font

- Other files under `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/*.cs` that construct or cache `Font` instances from `BootstrapThemeManager.CurrentTheme.Typography` outside a paint method. The known required fixes are `MainForm.cs` and `PaginationDemoForm.cs`; do not perform unrelated typography refactoring.

---

### Task 1: Introduce the three demo typography profiles and theme-factory contract

**Files:**
- Create: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoTypographyPreset.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoTypography.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoThemeFactory.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`

**Interfaces:**
- Produces: `public enum DemoTypographyPreset { Default, Base14Px, Base16Px }` in the demo assembly.
- Produces: `DemoTypography.CreateThemeTypography(DemoTypographyPreset preset)` returning the immutable typography object for the requested preset.
- Produces: `DemoTypography.TryGetPreset(BootstrapThemeTypography typography, out DemoTypographyPreset preset)` for MainForm synchronization of known demo profiles.
- Produces: `DemoTypography.CreateFont(BootstrapFontToken token)` and `DemoTypography.FontMatchesToken(Font font, BootstrapFontToken token)` for deterministic demo-owned GDI font management.
- Preserves: existing `DemoThemeFactory.Create(BootstrapThemeMode mode, bool reducedMotion = false)` behavior as Base 16px.
- Adds: `DemoThemeFactory.Create(BootstrapThemeMode mode, DemoTypographyPreset typographyPreset, bool reducedMotion = false)`.

- [ ] **Step 1: Add failing factory/profile tests before changing the demo implementation**

Extend `IntegratedDemoTypographyTests` with focused tests equivalent to:

```csharp
[Test]
public void DefaultPresetReusesExactFrameworkDefaultTypography()
{
    var theme = DemoThemeFactory.Create(
        BootstrapThemeMode.Light,
        DemoTypographyPreset.Default);

    Assert.That(theme.Typography, Is.SameAs(BootstrapThemeTypography.Default));
}

[TestCase(DemoTypographyPreset.Base14Px, 10.5f, 9.1875f, 10.5f, 13.125f, 15.75f)]
[TestCase(DemoTypographyPreset.Base16Px, 12f, 10.5f, 12f, 15f, 18f)]
public void DemoThemeFactoryCreatesRequestedTypographyProfile(
    DemoTypographyPreset preset,
    float body,
    float bodySmall,
    float label,
    float headingSmall,
    float headingMedium)
{
    var theme = DemoThemeFactory.Create(BootstrapThemeMode.Light, preset);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(theme.Typography.Body.FontFamilyName, Is.EqualTo("Segoe UI"));
        Assert.That(theme.Typography.Body.SizeInPoints, Is.EqualTo(body).Within(0.001f));
        Assert.That(theme.Typography.BodySmall.SizeInPoints, Is.EqualTo(bodySmall).Within(0.001f));
        Assert.That(theme.Typography.Label.SizeInPoints, Is.EqualTo(label).Within(0.001f));
        Assert.That(theme.Typography.Label.Style, Is.EqualTo(FontStyle.Bold));
        Assert.That(theme.Typography.HeadingSmall.SizeInPoints, Is.EqualTo(headingSmall).Within(0.001f));
        Assert.That(theme.Typography.HeadingMedium.SizeInPoints, Is.EqualTo(headingMedium).Within(0.001f));
    }));
}

[Test]
public void ExistingFactoryOverloadStillMeansBase16Px()
{
    var theme = DemoThemeFactory.Create(BootstrapThemeMode.Dark, reducedMotion: true);

    Assert.Multiple((Action)(() =>
    {
        Assert.That(theme.Mode, Is.EqualTo(BootstrapThemeMode.Dark));
        Assert.That(theme.ReducedMotion, Is.True);
        Assert.That(theme.Typography.Body.SizeInPoints, Is.EqualTo(12f).Within(0.001f));
    }));
}
```

Also add a test that an out-of-range `DemoTypographyPreset` passed to the new overload throws `ArgumentOutOfRangeException` rather than silently falling back.

- [ ] **Step 2: Run the focused tests and confirm they fail for the missing enum/overload**

Run on Windows:

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --filter "FullyQualifiedName~IntegratedDemoTypographyTests" `
  --blame-hang --blame-hang-timeout 5m
```

Expected: compilation/test failure because `DemoTypographyPreset` and the new factory overload do not exist yet.

- [ ] **Step 3: Add `DemoTypographyPreset`**

Create:

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

/// <summary>
/// Identifies the typography profiles that can be previewed by the Integrated Demo.
/// </summary>
public enum DemoTypographyPreset
{
    Default = 0,
    Base14Px = 1,
    Base16Px = 2
}
```

This type is public only because the demo project is referenced by the test project and `DemoThemeFactory` is already public. It is **not** a public API of the core `MyDmsVn.Bootstrap5WinFormUI` package.

- [ ] **Step 4: Refactor `DemoTypography` into centralized immutable profiles**

Replace the single-profile constants with two demo-owned immutable objects while reusing the framework default by reference. The implementation should have this shape:

```csharp
internal static class DemoTypography
{
    private const string FontFamilyName = "Segoe UI";

    private static readonly BootstrapThemeTypography Base14Typography =
        new BootstrapThemeTypography(
            new BootstrapFontToken(FontFamilyName, 10.5f),
            new BootstrapFontToken(FontFamilyName, 9.1875f),
            new BootstrapFontToken(FontFamilyName, 10.5f, FontStyle.Bold),
            new BootstrapFontToken(FontFamilyName, 13.125f, FontStyle.Bold),
            new BootstrapFontToken(FontFamilyName, 15.75f, FontStyle.Bold));

    private static readonly BootstrapThemeTypography Base16Typography =
        new BootstrapThemeTypography(
            new BootstrapFontToken(FontFamilyName, 12f),
            new BootstrapFontToken(FontFamilyName, 10.5f),
            new BootstrapFontToken(FontFamilyName, 12f, FontStyle.Bold),
            new BootstrapFontToken(FontFamilyName, 15f, FontStyle.Bold),
            new BootstrapFontToken(FontFamilyName, 18f, FontStyle.Bold));

    internal static BootstrapThemeTypography CreateThemeTypography(DemoTypographyPreset preset)
    {
        switch (preset)
        {
            case DemoTypographyPreset.Default:
                return BootstrapThemeTypography.Default;
            case DemoTypographyPreset.Base14Px:
                return Base14Typography;
            case DemoTypographyPreset.Base16Px:
                return Base16Typography;
            default:
                throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unsupported demo typography preset.");
        }
    }
}
```

Add `TryGetPreset(...)` using the known immutable profile objects. Recognize `BootstrapThemeTypography.Default`, Base14, and Base16. If an arbitrary application-defined typography object does not match a known demo profile, return `false` rather than guessing from body size alone.

Add centralized font helpers:

```csharp
internal static Font CreateFont(BootstrapFontToken token)
{
    return new Font(
        token.FontFamilyName,
        token.SizeInPoints,
        token.Style,
        GraphicsUnit.Point);
}

internal static bool FontMatchesToken(Font font, BootstrapFontToken token)
{
    return string.Equals(font.Name, token.FontFamilyName, StringComparison.OrdinalIgnoreCase) &&
        Math.Abs(font.SizeInPoints - token.SizeInPoints) <= 0.001f &&
        font.Style == token.Style;
}
```

Do not keep `CreateBodyFont()` hard-coded to 12pt; all font creation must receive the active token.

- [ ] **Step 5: Add the new `DemoThemeFactory` overload without breaking the existing one**

Keep the existing signature and make it delegate to Base16:

```csharp
public static BootstrapTheme Create(
    BootstrapThemeMode mode,
    bool reducedMotion = false)
{
    return Create(mode, DemoTypographyPreset.Base16Px, reducedMotion);
}
```

Add:

```csharp
public static BootstrapTheme Create(
    BootstrapThemeMode mode,
    DemoTypographyPreset typographyPreset,
    bool reducedMotion = false)
{
    return new BootstrapTheme(
        mode,
        BootstrapThemeColors.CreateDefault(mode),
        BootstrapThemeMetrics.Default,
        DemoTypography.CreateThemeTypography(typographyPreset),
        reducedMotion);
}
```

Do not add typography state to `DemoThemeFactory`; it remains a pure composition factory.

- [ ] **Step 6: Run the focused profile tests on both TFMs**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --filter "FullyQualifiedName~IntegratedDemoTypographyTests" `
  --blame-hang --blame-hang-timeout 5m

dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net48 `
  --filter "FullyQualifiedName~IntegratedDemoTypographyTests" `
  --blame-hang --blame-hang-timeout 5m
```

Expected: PASS.

- [ ] **Step 7: Commit the profile/factory slice**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoTypographyPreset.cs \
        demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoTypography.cs \
        demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoThemeFactory.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs
git commit -m "feat: add integrated demo typography profiles"
```

---

### Task 2: Make `DemoFormBase` native body typography react to runtime profile changes

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoFormBase.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`

**Interfaces:**
- Consumes: `DemoTypography.CreateFont(BootstrapFontToken)` and `DemoTypography.FontMatchesToken(...)` from Task 1.
- Produces: every existing `DemoFormBase` instance tracks `BootstrapThemeManager.CurrentTheme.Typography.Body` for its inherited native `Form.Font`.
- Preserves: constructing a demo form must not assign a new application theme.

- [ ] **Step 1: Replace the old fixed-12pt test with runtime profile-transition coverage**

Update the PR #62 test so it explicitly installs Base16 before construction and then verifies profile changes on the same form instance:

```csharp
[Test]
public void DemoFormBaseTracksBodyTypographyAcrossRuntimeProfileChanges()
{
    BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
        BootstrapThemeMode.Light,
        DemoTypographyPreset.Base16Px);

    using var form = new ButtonDemoForm();
    var nativeLabel = FindControls<Label>(form).First();

    Assert.That(form.Font.SizeInPoints, Is.EqualTo(12f).Within(0.001f));
    Assert.That(nativeLabel.Font.SizeInPoints, Is.EqualTo(12f).Within(0.001f));

    BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
        BootstrapThemeMode.Light,
        DemoTypographyPreset.Base14Px);

    Assert.That(form.Font.SizeInPoints, Is.EqualTo(10.5f).Within(0.001f));
    Assert.That(nativeLabel.Font.SizeInPoints, Is.EqualTo(10.5f).Within(0.001f));

    BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
        BootstrapThemeMode.Light,
        DemoTypographyPreset.Default);

    Assert.That(form.Font.SizeInPoints, Is.EqualTo(9f).Within(0.001f));
    Assert.That(nativeLabel.Font.SizeInPoints, Is.EqualTo(9f).Within(0.001f));
}
```

Add a no-churn test for color/reduced-motion changes with the same typography profile:

```csharp
[Test]
public void DemoFormBaseDoesNotReplaceOwnedFontWhenTypographyTokenIsUnchanged()
{
    BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
        BootstrapThemeMode.Light,
        DemoTypographyPreset.Base14Px);

    using var form = new ButtonDemoForm();
    var originalFont = form.Font;

    BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
        BootstrapThemeMode.Dark,
        DemoTypographyPreset.Base14Px,
        reducedMotion: true);

    Assert.That(form.Font, Is.SameAs(originalFont));
}
```

- [ ] **Step 2: Run the two new tests and confirm the runtime transition currently fails**

Use the same bounded focused test command from Task 1. Expected: the fixed constructor-created font remains 12pt after the theme changes.

- [ ] **Step 3: Subscribe `DemoFormBase` to `BootstrapThemeManager.ThemeChanged` and apply `Typography.Body`**

Refactor the base class along these lines:

```csharp
public abstract class DemoFormBase : Form
{
    private Font? _demoBodyFont;

    protected DemoFormBase()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        ApplyBodyTypography(BootstrapThemeManager.CurrentTheme);
        BootstrapThemeManager.ThemeChanged += OnDemoThemeChanged;
    }

    private void OnDemoThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyBodyTypography(e.NewTheme);
    }

    private void ApplyBodyTypography(BootstrapTheme theme)
    {
        var token = theme.Typography.Body;
        if (_demoBodyFont is not null && DemoTypography.FontMatchesToken(_demoBodyFont, token))
        {
            return;
        }

        var replacement = DemoTypography.CreateFont(token);
        var previous = _demoBodyFont;
        _demoBodyFont = replacement;
        Font = replacement;
        previous?.Dispose();
    }
}
```

In `Dispose(bool)`:

1. unsubscribe `BootstrapThemeManager.ThemeChanged` while disposing;
2. call `base.Dispose(disposing)`;
3. dispose `_demoBodyFont` exactly once and set it to `null`.

Do not publish a theme from this base class. It only consumes the current theme.

- [ ] **Step 4: Verify native inherited controls and existing Bootstrap controls after a profile change**

Extend the runtime test with a representative `BootstrapButton` from `ButtonDemoForm` and assert its font follows the same body sizes. This proves native inheritance and framework theme consumption stay synchronized without introducing demo-specific logic into the control.

- [ ] **Step 5: Run `IntegratedDemoTypographyTests` on both targets**

Expected: PASS with no modal dialogs or hangs.

- [ ] **Step 6: Commit runtime body-font synchronization**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/DemoFormBase.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs
git commit -m "feat: update demo body font on theme changes"
```

---

### Task 3: Add the `Base font` selector and compose it with Theme/Reduced motion

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs`
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/Program.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`

**Interfaces:**
- Consumes: new `DemoThemeFactory.Create(mode, preset, reducedMotion)` overload from Task 1.
- Consumes: `DemoTypography.TryGetPreset(...)` and semantic-font helpers from Task 1.
- Produces: a `Base font` `ComboBox` adjacent to the existing Theme selector with indices `0=Default`, `1=Base14Px`, `2=Base16Px`.
- Produces: MainForm page-title font follows `theme.Typography.Label` and refreshes without unnecessary font allocation.

- [ ] **Step 1: Add failing UI-contract tests for the new selector**

In `IntegratedDemoTypographyTests`, add a helper that finds the ComboBox by `AccessibleName == "Integrated demo base font profile"`, then test:

```csharp
[Test]
public void MainFormExposesBaseFontSelectorBesideThemeSelector()
{
    BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
        BootstrapThemeMode.Light,
        DemoTypographyPreset.Base16Px);

    using var form = new MainForm();
    var settings = FindControls<FlowLayoutPanel>(form).Single(panel =>
        panel.Controls.OfType<Label>().Any(label => label.Text == "Theme"));

    var labels = settings.Controls.OfType<Label>().ToArray();
    var combos = settings.Controls.OfType<ComboBox>().ToArray();
    var baseFont = combos.Single(combo =>
        combo.AccessibleName == "Integrated demo base font profile");

    Assert.Multiple((Action)(() =>
    {
        Assert.That(labels.Any(label => label.Text == "Base font"), Is.True);
        Assert.That(baseFont.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDownList));
        Assert.That(baseFont.Items.Cast<string>(), Is.EqualTo(new[]
        {
            "Default",
            "Base 14px",
            "Base 16px"
        }));
        Assert.That(baseFont.SelectedIndex, Is.EqualTo(2));
    }));
}
```

Also assert the relative settings order is Theme label → Theme ComboBox → Base font label → Base font ComboBox → Reduced motion CheckBox. Do not rely on screen coordinates for this contract; use `settings.Controls.GetChildIndex(...)` or the collection order established by `Controls.Add(...)`.

- [ ] **Step 2: Add failing interaction tests for all three dimensions**

Create tests that start at Light + Base16 + reduced motion false and then perform these transitions on the same `MainForm`:

1. select Base14 → theme remains Light, reduced motion remains false, Body becomes 10.5pt;
2. select Dark → Base14 remains selected and Body remains 10.5pt;
3. check Reduced motion → Dark and Base14 remain unchanged;
4. select Default → Dark/reduced-motion remain unchanged and `theme.Typography` is the same object as `BootstrapThemeTypography.Default`;
5. select Base16 → mode/reduced-motion remain unchanged and Body returns to 12pt.

Use `SelectedIndex` and `Checked` exactly as a user interaction would drive the existing event handlers.

- [ ] **Step 3: Add the header fields and configure the selector**

Add fields:

```csharp
private readonly Label _baseFontLabel = new Label();
private readonly ComboBox _baseFontPreset = new ComboBox();
```

In `ConfigureHeader()` place them immediately after `_themeMode` and before `_reducedMotion`:

```csharp
_baseFontLabel.AutoSize = true;
_baseFontLabel.Text = "Base font";
_baseFontLabel.Margin = new Padding(12, 14, 6, 0);

_baseFontPreset.DropDownStyle = ComboBoxStyle.DropDownList;
_baseFontPreset.Width = 120;
_baseFontPreset.Margin = new Padding(0, 8, 0, 0);
_baseFontPreset.AccessibleName = "Integrated demo base font profile";
_baseFontPreset.Items.Add("Default");
_baseFontPreset.Items.Add("Base 14px");
_baseFontPreset.Items.Add("Base 16px");
_baseFontPreset.SelectedIndexChanged += (_, _) => PublishSelectedTheme();
```

If the exact width needs a small adjustment during the minimum-size layout test, keep it compact enough to preserve the existing 900x600 shell and do not change item text.

- [ ] **Step 4: Compose all settings in `PublishSelectedTheme()`**

Require both ComboBoxes to have valid selections, then map the typography selection explicitly:

```csharp
private static DemoTypographyPreset GetTypographyPreset(int selectedIndex)
{
    switch (selectedIndex)
    {
        case 0:
            return DemoTypographyPreset.Default;
        case 1:
            return DemoTypographyPreset.Base14Px;
        case 2:
            return DemoTypographyPreset.Base16Px;
        default:
            throw new ArgumentOutOfRangeException(nameof(selectedIndex));
    }
}
```

Publish:

```csharp
BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
    mode,
    GetTypographyPreset(_baseFontPreset.SelectedIndex),
    _reducedMotion.Checked);
```

Do not assign fonts directly from the selector event. The theme event is the single propagation mechanism.

- [ ] **Step 5: Extend `SyncSelection(BootstrapTheme theme)`**

Within the existing `_updatingSelection` guard:

- synchronize Light/Dark exactly as today;
- synchronize Reduced motion exactly as today;
- call `DemoTypography.TryGetPreset(theme.Typography, out var preset)`;
- for known demo profiles set `_baseFontPreset.SelectedIndex = (int)preset`;
- if an arbitrary unknown typography object is installed externally, leave the previous selector value unchanged rather than guessing.

This prevents event recursion while keeping factory-created/default themes accurately reflected in the shell.

- [ ] **Step 6: Refresh the MainForm cached page-title font from `Typography.Label`**

PR #62's current page-title font is 12pt Bold at Base16. Preserve that visual result by using the `Label` semantic role, not `HeadingSmall`.

Add a helper:

```csharp
private void UpdatePageTitleFont(BootstrapTheme theme)
{
    var token = theme.Typography.Label;
    if (_pageTitleFont is not null && DemoTypography.FontMatchesToken(_pageTitleFont, token))
    {
        return;
    }

    var replacement = DemoTypography.CreateFont(token);
    var previous = _pageTitleFont;
    _pageTitleFont = replacement;
    _pageTitle.Font = replacement;
    previous?.Dispose();
}
```

Call it from `ApplyTheme(theme)`. Continue disposing `_pageTitleFont` in `Dispose(bool)`.

Extend `ApplyTheme` to set the new label/combo colors from `theme.Colors.SurfaceSecondary`, `theme.Colors.Surface`, and `theme.Colors.Text` in the same pattern as the existing Theme controls.

- [ ] **Step 7: Make Base16 startup explicit in `Program.Main`**

Replace the implicit overload call with:

```csharp
BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
    BootstrapThemeMode.Light,
    DemoTypographyPreset.Base16Px);
```

This records the compatibility decision directly at the Integrated Demo entry point.

- [ ] **Step 8: Verify interaction tests on both TFMs**

Run the focused typography suite with bounded hang detection on `net8.0-windows` and `net48`. Expected: PASS.

- [ ] **Step 9: Commit the shell selector slice**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MainForm.cs \
        demo/MyDmsVn.Bootstrap5WinFormUI.Demo/Program.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs
git commit -m "feat: add base font selector to integrated demo"
```

---

### Task 4: Make cached semantic fonts inside demo pages profile-aware

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/PaginationDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`
- Audit: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/*.cs`

**Interfaces:**
- Consumes: `DemoTypography.CreateFont(...)` and `FontMatchesToken(...)`.
- Produces: Pagination section headings track `theme.Typography.HeadingSmall` at runtime on the already-created form.
- Preserves: intentionally per-paint, immediately disposed fonts that read the current theme are not converted into unrelated caching infrastructure.

- [ ] **Step 1: Add a failing Pagination semantic-heading transition test**

Add a test equivalent to:

```csharp
[Test]
public void PaginationSectionHeadingTracksActiveHeadingSmallTypography()
{
    BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
        BootstrapThemeMode.Light,
        DemoTypographyPreset.Base16Px);

    using var form = new PaginationDemoForm();
    var heading = FindControls<Label>(form)
        .Single(label => label.Text == "Button sizes");

    Assert.That(heading.Font.SizeInPoints, Is.EqualTo(15f).Within(0.001f));

    BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
        BootstrapThemeMode.Light,
        DemoTypographyPreset.Base14Px);
    Assert.That(heading.Font.SizeInPoints, Is.EqualTo(13.125f).Within(0.001f));

    BootstrapThemeManager.CurrentTheme = DemoThemeFactory.Create(
        BootstrapThemeMode.Dark,
        DemoTypographyPreset.Default);
    Assert.That(heading.Font.SizeInPoints, Is.EqualTo(11f).Within(0.001f));
}
```

Expected before implementation: first assertion passes, later assertions fail because `_sectionTitleFont` was captured in the constructor.

- [ ] **Step 2: Replace readonly one-shot Pagination heading font ownership with refreshable ownership**

Change:

```csharp
private readonly Font _sectionTitleFont;
```

to nullable owned state plus a label registry:

```csharp
private readonly List<Label> _sectionTitleLabels = new List<Label>();
private Font? _sectionTitleFont;
```

Add `using System.Collections.Generic;` if not already present.

Create/update the heading font from `BootstrapThemeManager.CurrentTheme.Typography.HeadingSmall` before building sections, and register each title label in `CreateSection(...)`.

- [ ] **Step 3: Subscribe Pagination to theme changes only for its semantic heading role**

Add a local `ThemeChanged` handler that calls `UpdateSectionTitleFont(e.NewTheme)`. The helper must:

1. return early when the current owned font already matches `HeadingSmall`;
2. create the replacement font;
3. assign the replacement to every label in `_sectionTitleLabels`;
4. swap `_sectionTitleFont`;
5. dispose the previous owned font after consumers no longer reference it.

Unsubscribe in `Dispose(bool)` and dispose the final owned font once.

Do not duplicate `DemoFormBase` body-font logic here; Pagination's handler exists only because its section titles opt into a non-body semantic role.

- [ ] **Step 4: Audit the rest of the demo for stale cached typography-derived fonts**

Run:

```powershell
git grep -n "new Font(" -- demo/MyDmsVn.Bootstrap5WinFormUI.Demo
git grep -n "Typography\." -- demo/MyDmsVn.Bootstrap5WinFormUI.Demo
```

Classify every result:

- `DemoTypography.cs`: central font factory/profile definitions — expected.
- `MainForm.cs`: owned page-title font — must be profile-aware after Task 3.
- `PaginationDemoForm.cs`: owned section-heading font — must be profile-aware in this task.
- Paint/render code that creates a font from the **current** theme in a `using` scope for one paint operation — may remain as-is if it cannot become stale.
- Any other field/property initialized once from `BootstrapThemeManager.CurrentTheme.Typography` and retained after construction — add the same explicit ThemeChanged refresh + deterministic disposal pattern and a focused test naming that form/role.

The audit is complete only when no retained semantic font can stay on the previous profile after `ThemeChanged`.

- [ ] **Step 5: Run focused typography tests on both TFMs**

Expected: Pagination heading changes correctly and the full existing typography suite remains green.

- [ ] **Step 6: Commit the cached-font hardening slice**

```bash
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/PaginationDemoForm.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs
# Add only additional demo/test files that the explicit audit proved necessary.
git commit -m "fix: refresh demo semantic fonts across profiles"
```

---

### Task 5: Expand layout and live-page regression coverage across all profiles

**Files:**
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyLayoutTests.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs`

**Interfaces:**
- Consumes: all three working typography profiles and MainForm selector behavior.
- Produces: automated evidence that shell chrome, representative native/Bootstrap controls, and the Theme summary remain coherent while switching profiles.

- [ ] **Step 1: Parameterize shell containment tests for all three profiles**

Replace the fixed `MainShellChromeRemainsContainedAtTwelvePointBodyTypography` assumption with a profile-aware test. For each profile, install the theme **before** constructing `MainForm`, then verify containment for:

- navigation toggle;
- title block;
- page title;
- page description;
- settings panel;
- Theme label and combo;
- Base font label and combo;
- Reduced motion checkbox.

Use expected body/title sizes:

```text
Default   -> Body 9pt,    page title Label 9pt Bold
Base14Px  -> Body 10.5pt, page title Label 10.5pt Bold
Base16Px  -> Body 12pt,   page title Label 12pt Bold
```

Run each profile at both logical shell sizes:

```text
900 x 600
1280 x 800
```

Keep `AssertContained(...)` as the core geometry assertion. The page-title/description already use ellipsis, so the test should assert containment and non-zero bounds rather than require a fixed title width.

- [ ] **Step 2: Parameterize representative native + Bootstrap control sizing**

Replace the fixed 12pt test with three cases that create `ButtonDemoForm` and assert:

- first representative native `Label` follows the expected Body size;
- representative `BootstrapButton` follows the expected Body size;
- `button.GetPreferredSize(Size.Empty).Height >= TextRenderer.MeasureText(button.Text, button.Font).Height`;
- `button.AutoSize` remains true where that existing scenario expects it.

This guards against a font token changing while the control remains clipped at old text measurements.

- [ ] **Step 3: Make the Theme page summary test profile-aware**

For each profile, construct MainForm on the Theme page and assert the summary contains the active body value:

```text
Default   -> "Body Segoe UI 9pt"
Base14Px  -> "Body Segoe UI 10.5pt"
Base16Px  -> "Body Segoe UI 12pt"
```

Continue asserting usable non-zero bounds.

- [ ] **Step 4: Prove that switching the selector updates the already-created Theme page**

Create `MainForm`, locate the Theme summary label, capture the label instance, then change the Base font selector from Base16 → Base14 → Default. After each selection:

- assert the same summary `Label` instance is still in the control tree;
- assert its text changes to the new body size;
- assert the active page has not been disposed;
- assert MainForm/native typography also changed.

This explicitly prevents an implementation that solves typography switching by destroying and recreating the current demo page.

- [ ] **Step 5: Run both Integrated Demo typography test fixtures with bounded hang detection**

```powershell
dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net8.0-windows `
  --filter "FullyQualifiedName~IntegratedDemoTypography" `
  --blame-hang --blame-hang-timeout 5m

dotnet test tests/MyDmsVn.Bootstrap5WinFormUI.Tests/MyDmsVn.Bootstrap5WinFormUI.Tests.csproj `
  -f net48 `
  --filter "FullyQualifiedName~IntegratedDemoTypography" `
  --blame-hang --blame-hang-timeout 5m
```

Expected: PASS.

- [ ] **Step 6: Commit the expanded regression coverage**

```bash
git add tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyTests.cs \
        tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/IntegratedDemoTypographyLayoutTests.cs
git commit -m "test: cover integrated demo typography switching"
```

---

### Task 6: Document, build, run the full suite, and perform the visual matrix

**Files:**
- Modify: `docs/DESIGN_SYSTEM.md`
- Verify: entire solution/demo/test surface affected by the change.

**Interfaces:**
- Produces: documented distinction between core framework default typography and Integrated Demo evaluation presets.
- Produces: final evidence across both target frameworks and manual Windows DPI checks.

- [ ] **Step 1: Document the Integrated Demo typography-profile behavior**

Add a concise subsection under typography/design-system demo guidance that states:

- core framework default typography remains `BootstrapThemeTypography.Default`;
- Integrated Demo starts at Base 16px for continuity with PR #62;
- the `Base font` selector exposes `Default`, `Base 14px`, and `Base 16px` for visual/regression comparison;
- typography profile is independent from Light/Dark and reduced motion;
- profile changes are published as a complete `BootstrapTheme` through `BootstrapThemeManager`.

Do not describe Base14/Base16 as new core-framework defaults or public theme presets.

- [ ] **Step 2: Build the complete solution in Release configuration**

```powershell
dotnet build MyDmsVn.Bootstrap5WinFormUI.sln -c Release
```

Expected: successful builds for both `net48` and `net8.0-windows` projects with no new warnings introduced by this change.

- [ ] **Step 3: Run the repository's bounded full test suite**

```powershell
./test.ps1
```

Expected: both target-framework suites pass; no hang dump, unexpected dialog, or DataError wait is produced.

- [ ] **Step 4: Launch the Integrated Demo and execute the profile/theme interaction matrix**

Run the demo using the normal repository-supported launch path, for example:

```powershell
dotnet run --project demo/MyDmsVn.Bootstrap5WinFormUI.Demo/MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -f net8.0-windows
```

Verify:

1. startup selector is `Base 16px`;
2. switch Base16 → Base14 → Default → Base16 without changing page;
3. repeat in Light and Dark;
4. toggle Reduced motion while on Base14 and confirm the typography remains Base14;
5. change Light/Dark while on Default and confirm typography remains Default;
6. return to Base16 and confirm the visual baseline matches PR #62.

- [ ] **Step 5: Perform the minimum-size and DPI visual matrix**

At minimum test:

```text
Window sizes:
- 900 x 600
- 1280 x 800

Windows display scaling:
- 100%
- 125%
- 150%
- 200%

Typography profiles:
- Default
- Base 14px
- Base 16px
```

Prioritize these pages because they expose density/font regressions most clearly:

- Theme
- Buttons / Groups / Toolbar
- Advanced Inputs
- Select
- Input Groups
- TreeView
- ListView
- DataGrid
- Pagination
- Sidebar

Check for:

- clipped header controls or title text;
- settings group overflowing the header;
- stale 16px native text after selecting 14px/Default;
- stale Pagination headings;
- Bootstrap controls and native labels disagreeing on body size;
- DataGrid/ListView/TreeView rows becoming visibly clipped;
- focus rectangles/text baselines becoming incorrect after runtime switching;
- popup/select content retaining an old typography profile after the owning control changes.

If a visual issue is found, fix the owning layout/font-refresh logic and add a focused regression test before completing the task. Do not globally increase metrics as a shortcut unless the evidence shows a framework metric itself is incorrect for all profiles.

- [ ] **Step 6: Re-run focused typography tests after any visual-matrix fix**

Run both Integrated Demo typography fixtures on both TFMs with bounded hang detection, then run `./test.ps1` again if code changed after Step 3.

- [ ] **Step 7: Commit documentation/final hardening**

```bash
git add docs/DESIGN_SYSTEM.md
# Add only implementation/test files changed as a result of verified visual findings.
git commit -m "docs: document integrated demo typography profiles"
```

---

## Definition of Done

The implementation is complete only when all of the following are true:

- Integrated Demo header contains `Theme [Light/Dark]`, `Base font [Default/Base 14px/Base 16px]`, and Reduced motion in that order.
- Normal startup remains Light + Base 16px + reduced motion off unless another existing startup setting explicitly says otherwise.
- `Default` uses the exact `BootstrapThemeTypography.Default` object and does not mutate core defaults.
- Base14 and Base16 typography values match the profile matrix in this plan.
- Theme mode, typography profile, and reduced motion preserve one another across all selector changes.
- A profile change publishes exactly one complete replacement `BootstrapTheme` through `BootstrapThemeManager.CurrentTheme`; there is no second typography state/event channel.
- Existing `DemoFormBase` instances update their native inherited body font at runtime.
- A Light/Dark/reduced-motion-only change does not allocate a replacement native body font when its typography token did not change.
- MainForm page-title semantic font updates with the active profile and remains Bold.
- Pagination section-title semantic fonts update with the active profile on the same form instance.
- Audit finds no other retained typography-derived demo font that can remain stale after a profile switch.
- Current Theme page summary updates on the same page instance and reports 9pt / 10.5pt / 12pt Body values correctly.
- Shell containment tests pass for all three profiles at 900x600 and 1280x800 logical sizes.
- Representative native labels and Bootstrap controls agree on active body typography and do not clip text.
- `net48` and `net8.0-windows` builds pass.
- Focused typography tests pass on both TFMs with bounded hang detection.
- `./test.ps1` passes without hangs or modal UI.
- Manual checks pass at 100%, 125%, 150%, and 200% Windows display scaling for the identified high-density demo pages.
- `docs/DESIGN_SYSTEM.md` clearly states that the three choices are Integrated Demo evaluation profiles, not a change to the core framework default.

---

## Non-Goals

Do not expand this plan into any of the following without separate approval:

- changing `BootstrapThemeTypography.Default` from 9pt;
- adding public Base14/Base16 presets to the core framework package;
- adding user persistence/settings storage for the selected Integrated Demo profile;
- scaling `BootstrapThemeMetrics` automatically from typography size;
- changing all component default heights or density tokens;
- adding arbitrary/custom font-size input;
- adding font-family selection;
- redesigning the Integrated Demo header/navigation;
- changing component typography semantics unrelated to stale runtime profile propagation;
- introducing a global font cache or new dependency.

The purpose of this feature is to make the Integrated Demo a reliable comparison harness for three typography profiles while preserving the architecture and behavior established by PR #62.