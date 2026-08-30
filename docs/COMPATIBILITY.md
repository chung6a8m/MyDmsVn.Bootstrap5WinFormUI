# Target Framework and Compatibility Policy

## 1. Supported targets

The project requirement is .NET Framework 4.8 plus .NET 8 for Windows Forms.

The intended project TFMs are:

```xml
<TargetFrameworks>net48;net8.0-windows</TargetFrameworks>
<UseWindowsForms>true</UseWindowsForms>
```

The `-windows` suffix is required for the .NET 8 WinForms target.

## 2. Compatibility philosophy

Maintain one source tree and one public API wherever practical. Target-specific code is a last-mile implementation detail, not a reason to expose different control APIs on each runtime.

The `net48` target is not a degraded compatibility afterthought. Every release gate builds and verifies both targets.

## 3. Runtime API restrictions

Code shared by both targets may only call APIs available on both targets, unless the call is wrapped in a compatibility abstraction or guarded with target-specific compilation.

Examples of APIs commonly found in prototypes that require attention on `net48`:

- `Math.Clamp`
- Newer convenience overloads added after .NET Framework
- Runtime/platform detection APIs introduced in modern .NET
- Newer `System.Drawing` APIs not present in .NET Framework 4.8

Prefer a small helper such as an internal `NumericUtil.Clamp` over scattering `#if` blocks through controls.

## 4. C# language vs runtime APIs

A modern C# compiler may target .NET Framework 4.8, so language syntax and runtime API availability are different concerns.

The repository may use the C# language version supported by the pinned SDK/Visual Studio toolchain, but contributors must ensure generated IL only references APIs available on the target framework.

Do not assume a newer syntax feature implies the corresponding newer runtime helper exists on `net48`.

## 5. Nullable reference types

Nullable annotations are a compile-time quality feature and can be used with `net48` when the build toolchain supports them. They must not lead to separate public APIs across targets.

If enabled, use them consistently. Do not copy prototype nullable annotations blindly without resolving real ownership/nullability semantics.

## 6. WinForms behavior differences

Where WinForms behavior differs between .NET Framework and modern .NET, isolate the difference behind an internal helper or a minimal target-specific implementation.

Areas to verify on both targets include:

- DPI scaling
- Text rendering and preferred size
- Designer serialization
- Accessibility behavior
- DataGridView painting
- Font availability/fallback
- Native `DateTimePicker` localized text, checkbox state, range normalization/exceptions, keyboard navigation, and calendar-popup behavior

`BootstrapDatePicker` deliberately keeps one native `DateTimePicker` authoritative on both TFMs. Tests that characterize date/range/format/checkbox behavior should compare against a fresh native peer instead of freezing culture- or runtime-specific rendered strings. The framework may theme the wrapper shell but does not promise to recolor, round, replace, or otherwise normalize the OS-owned calendar popup across Windows versions, cultures, or target frameworks.

Stage 9 therefore introduces no target-specific `MonthCalendar`, popup `Form`, P/Invoke hook, private WinForms reflection, parsing engine, or culture abstraction to force visual/behavioral parity. Differences that are genuinely native remain native unless a later explicit compatibility contract says otherwise.

`BootstrapCalendar` and `BootstrapCalendarPicker` are separate, framework-owned custom-calendar controls; they do not alter the native `BootstrapDatePicker` distinction above. Both target frameworks use the safe inclusive date domain supported by WinForms `DateTimePicker` (`DateTimePicker.MinimumDateTime.Date` through `DateTimePicker.MaximumDateTime.Date`), normalize public selection and bounds inputs to date-only values, and use `CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek` for the 42-cell month projection. The calendar paints its own shell, header, weekday names, and day states from framework theme/DPI tokens, so its visuals are framework-owned rather than OS calendar chrome.

`BootstrapCalendarPicker` presents a fresh custom calendar through the existing native `ToolStripDropDownMenu` infrastructure. Native ToolStrip remains responsible for screen working-area placement, focus transfer, Escape/outside-click dismissal, and disposal of the hosted snapshot. When the popup opens, the hosted calendar must receive focus before its arrow, PageUp/PageDown, Home/End, Enter, and Space keys can participate in the normal WinForms key route; the picker restores its collapsed keyboard entry points without exposing the hosted control or ToolStrip types. Both behaviors are verified on `net48` and `net8.0-windows`; Windows/culture-specific text measurement and ToolStrip placement may still differ naturally between runtimes and OS versions.

## 7. System.Drawing

This is a Windows-only WinForms framework, so `System.Drawing` is an appropriate rendering foundation. All drawing code still needs deterministic GDI resource disposal.

Do not add cross-platform abstraction complexity merely because modern `System.Drawing.Common` has restrictions outside Windows; cross-platform UI is a non-goal.

## 8. Icon dependencies

### Segoe MDL2 Assets

Treat the font as a Windows glyph source and provide fallback behavior when a glyph/font cannot be rendered as expected.

### FontAwesome.Sharp

Integration must be optional. The core assembly must not require applications to install FontAwesome.Sharp if they only use SVG, MDL2, or application-provided icons.

### SVG

WinForms/System.Drawing has no built-in complete SVG renderer. Therefore generic SVG support should be implemented behind the icon abstraction using a compatible renderer/adapter. The chosen implementation must support both target frameworks or be isolated into an optional adapter with a stable core contract.

## 9. Conditional compilation

Use conditional compilation only for real target differences, for example:

```csharp
#if NET8_0_OR_GREATER
    // Modern implementation
#else
    // .NET Framework-compatible implementation
#endif
```

Do not use target directives merely to avoid writing a small shared compatibility helper.

## 10. Build matrix

At minimum, CI/local verification must include:

```text
net48            Windows
net8.0-windows   Windows
```

Release verification also includes the UI/manual matrix defined in `TESTING.md`.

## 11. Package compatibility rule

Before adding a runtime dependency, verify that its supported target frameworks are compatible with both project targets or isolate it into an optional adapter package. Core functionality must not become unavailable on `net48` because of a convenience dependency chosen for `net8.0-windows`.
