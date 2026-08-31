# BootstrapFormattedTextBox Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `BootstrapFormattedTextBox` that formats text while the user types, inspired by the DOM-decoupled formatter model of `cleave-zen`, with reusable pure-C# General, Numeral, Date, Time, and CreditCard formatters, stable caret/selection behavior, a raw/unformatted value API, custom formatter extensibility, native WinForms focus/clipboard behavior, designer-friendly options, demo coverage, and deliberate public API review.

**Architecture:** Keep `BootstrapTextBox` as the themed native-editing shell and add the smallest protected extension seam needed for a derived formatted editor. Put all formatting algorithms under a new `Formatting` namespace with no WinForms dependency. `BootstrapFormattedTextBox` owns only input integration: it converts the native editor candidate text to canonical raw text, formats it through an `IInputFormatter`, maps caret/selection through raw positions, handles separator-aware Backspace/Delete and deterministic undo/redo, and then raises the normal wrapper events once for the final stable value. Built-in formatter options remain mutable/designer-friendly and notify the control internally so option changes reformat in place.

**Tech Stack:** C#, Windows Forms, existing `BootstrapTextBox` / Theme / Rendering / Compatibility infrastructure, pure string formatting helpers, NUnit 4, STA WinForms interaction tests, SDK-style multi-targeting (`net48;net8.0-windows`). No JavaScript runtime, WebView, external formatting package, global hook, or new NuGet dependency.

**Spec:** This plan formalizes the control analysis from 2026-08-31 and must remain consistent with `docs/COMPONENTS.md`, `docs/ARCHITECTURE.md`, `docs/COMPATIBILITY.md`, `docs/TESTING.md`, `docs/PUBLIC_API_BASELINE.md`, and the existing `BootstrapTextBox`/`BootstrapNumericBox` contracts. Behavioral inspiration/reference only: `https://github.com/nosir/cleave-zen` (`formatGeneral`, `formatNumeral`, `formatDate`, `formatTime`, `formatCreditCard`, raw/unformat helpers, credit-card type detection, and cursor tracking). Do not add a runtime dependency on cleave-zen and do not copy TypeScript implementation code verbatim.

## Global Constraints

- Keep the root namespace `MyDmsVn.Bootstrap5WinFormUI`; the control remains under `MyDmsVn.Bootstrap5WinFormUI.Controls`; reusable formatter APIs live under `MyDmsVn.Bootstrap5WinFormUI.Formatting`.
- Product and test projects must continue to compile from shared source for both `net48` and `net8.0-windows`.
- `BootstrapFormattedTextBox` is additive and derives from `BootstrapTextBox`; it does not replace or change the documented behavior of ordinary `BootstrapTextBox` instances.
- Preserve the native inner WinForms `TextBox` as the actual editor. Do not implement a custom caret, selection model, clipboard stack, IME window, text renderer, or replacement input window.
- Keep the outer composite as the single public tab stop. The inner editor remains `TabStop = false`, and `Tab` / `Shift+Tab` must continue normal WinForms focus traversal.
- Pressing `Alt` must not alter formatted-input state or be intercepted by formatting code.
- Do not intercept ordinary character input, Ctrl+A, Ctrl+C, Ctrl+X, or Ctrl+V before the native editor. Native text mutation occurs first; the formatted control normalizes the resulting candidate text.
- `Text` is always the final formatted/display value. `RawValue` is always the canonical unformatted value.
- `RawValue == string.Empty` implies `Text == string.Empty`, even when a formatter has a prefix configured. This preserves placeholder and clear-button behavior.
- Built-in formatters must be pure C#: no `Control`, handle, theme, DPI, timer, async, thread-affinity, or static application state.
- `IInputFormatter.Format` and `IInputFormatter.Unformat` must be deterministic and null-safe through wrapper normalization (`null` becomes `string.Empty` before calling the formatter).
- Built-in formatters must satisfy canonical stability: `Unformat(Format(raw))` returns the canonical raw form, and `Format(Unformat(formatted))` returns the canonical display form.
- Formatting is not business validation. The control may shape structurally impossible date/time components when required by the formatter contract, but it does not perform Luhn validation, real calendar validation such as rejecting February 31, business min/max date validation, tax-code validation, or server-side sanitization.
- `BootstrapNumericBox` remains the recommended typed numeric value editor with `decimal Value`, range, increment, spinner buttons, and native numeric semantics. Numeral mode in `BootstrapFormattedTextBox` is for formatted string input, nullable/empty/partial states, prefixes, alternative grouping, and raw string extraction; do not merge these controls.
- No Phone formatter in v1. Phone/libphonenumber behavior requires a separate requirement and data/dependency decision.
- No `decimal? DecimalValue`, `DateTime? Value`, mask placeholder character, validation-provider API, or arbitrary regex-mask DSL in v1.
- No global keyboard hook, `IMessageFilter`, clipboard polling, or top-level popup is permitted.
- Formatting code must use `StringBuilder`/simple loops where practical and avoid repeated regular-expression compilation on every keystroke. Precompiled static regexes are acceptable only where they materially simplify stable credit-card IIN detection and remain compatible with both targets.
- Every formatting algorithm requires pure unit tests. Every caret/history/focus behavior requires STA control tests.
- New public/protected API intentionally changes the frozen API fingerprint. `Phase16PublicApiBaselineTests.ExportedApiMatchesApprovedV1Baseline` must fail before approval, the exported surface must be reviewed, and the fingerprint/docs updated only in the final API-review task.

---

## Reference Behavior and Deliberate Deviations

The implementation is behavior-inspired by `cleave-zen`, not a line-by-line port.

### Behaviors intentionally adopted

- Pure formatter/unformatter functions are independent from the input control.
- General formatting supports blocks, one delimiter or per-boundary delimiters, lazy delimiter display, prefix, numeric-only filtering, uppercase, and lowercase.
- Numeral formatting supports delimiter, group style, integer scale, decimal mark, decimal scale, positive-only, tail prefix, sign-before-prefix, strip-leading-zeroes, and prefix.
- Numeral `RawValue` uses `.` as the invariant canonical decimal separator even when the display decimal mark is configured as `,`.
- Credit-card formatting supports delimiter, lazy delimiter display, strict mode up to 19 digits, type detection, and type-dependent blocks.
- Credit-card type names cover UATP, AmEx, Diners, Discover, Mastercard, Dankort, Instapayment, JCB15, JCB, Maestro, Visa, MIR, UnionPay, and General.
- Cursor movement is solved as a separate integration problem rather than embedded in each formatting algorithm.

### Deliberate v1 deviations

- Date min/max options are not copied from cleave-zen. Range policy belongs to validation/typed date controls and can be planned separately.
- Date formatter performs only component-level shaping; it does not promise full Gregorian validity.
- Phone formatting is out of scope.
- The WinForms control provides deterministic Ctrl+Z/Ctrl+Y history because replacing formatted text can invalidate the native TextBox undo stack. The formatted control therefore owns history snapshots for its own transformed edits while ordinary `BootstrapTextBox` continues to use native undo semantics.
- Prefix is not displayed for empty raw input so `BootstrapTextBox.PlaceholderText` continues to work naturally.

Reference documentation used to freeze this plan:

- `https://github.com/nosir/cleave-zen/blob/main/docs/interfaces/FormatGeneralOptions.md`
- `https://github.com/nosir/cleave-zen/blob/main/docs/interfaces/FormatNumeralOptions.md`
- `https://github.com/nosir/cleave-zen/blob/main/docs/interfaces/FormatDateOptions.md`
- `https://github.com/nosir/cleave-zen/blob/main/docs/interfaces/FormatTimeOptions.md`
- `https://github.com/nosir/cleave-zen/blob/main/docs/interfaces/FormatCreditCardOptions.md`
- `https://github.com/nosir/cleave-zen/blob/main/src/credit-card/constants.ts`
- `https://github.com/nosir/cleave-zen/blob/main/src/credit-card/index.ts`
- `https://github.com/nosir/cleave-zen/blob/main/src/numeral/index.ts`

---

## Public Contract to Implement

### Control API

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(TextChanged))]
public class BootstrapFormattedTextBox : BootstrapTextBox
{
    public BootstrapInputFormatMode FormatMode { get; set; }
    public string RawValue { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IInputFormatter? Formatter { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapGeneralFormatOptions GeneralOptions { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapNumeralFormatOptions NumeralOptions { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapDateFormatOptions DateOptions { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapTimeFormatOptions TimeOptions { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapCreditCardFormatOptions CreditCardOptions { get; }

    [Browsable(false)]
    public BootstrapCreditCardType CreditCardType { get; }

    public event EventHandler? RawValueChanged;
    public event EventHandler? CreditCardTypeChanged;

    public void Reformat();
}
```

Rules:

- Default `FormatMode` is `None`.
- Default `RawValue` and `Text` are empty.
- `FormatMode = None` uses identity formatting; `RawValue == Text`.
- `FormatMode = Custom` uses `Formatter`. A null custom formatter acts as identity formatting so WinForms Designer property assignment order cannot throw during initialization.
- For built-in modes, `Formatter` is ignored; it is not overwritten or disposed when switching modes.
- Setting `RawValue` normalizes through the effective formatter and updates `Text` without recording an undo entry.
- Setting `Text` treats the assigned string as a candidate display value, canonicalizes through `Unformat -> Format`, updates `RawValue`, and clears edit history. This makes `Text = "123456"` convenient even when the active formatter displays `"123 456"`.
- Setting the same canonical raw/display pair is a no-op.
- `RawValueChanged` fires exactly once when canonical raw value changes.
- `TextChanged` fires exactly once when final display text changes; callers never observe an intermediate unformatted candidate through the wrapper event.
- When both change, final `TextChanged` is raised first through the inherited path, then `RawValueChanged` is raised. Event handlers reading either property see the final stable pair.
- `CreditCardType` is `General` outside CreditCard mode. In CreditCard mode it reflects the detected canonical raw value; `CreditCardTypeChanged` fires once on an effective type transition.
- Changing `FormatMode`, built-in options, or `Formatter` reformats the existing canonical raw value in place, preserves the raw value where the new formatter can represent it, and does not add an undo entry.
- `Reformat()` recomputes `RawValue`, display text, caret, and credit-card type using the current effective formatter/options without adding an undo entry.

### Formatting namespace API

```csharp
namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

public enum BootstrapInputFormatMode
{
    None,
    General,
    Numeral,
    Date,
    Time,
    CreditCard,
    Custom
}

public interface IInputFormatter
{
    string Format(string rawValue);
    string Unformat(string formattedValue);
}

public enum BootstrapNumeralGroupStyle
{
    None,
    Thousand,
    Lakh,
    Wan
}

public enum BootstrapTimeFormat
{
    TwentyFourHour,
    TwelveHour
}

public enum BootstrapCreditCardType
{
    General,
    Uatp,
    AmericanExpress,
    Diners,
    Discover,
    Mastercard,
    Dankort,
    Instapayment,
    Jcb15,
    Jcb,
    Maestro,
    Visa,
    Mir,
    UnionPay
}
```

Built-in formatter classes are public and reusable without WinForms:

```csharp
public sealed class BootstrapGeneralInputFormatter : IInputFormatter
{
    public BootstrapGeneralInputFormatter(BootstrapGeneralFormatOptions options);
    public string Format(string rawValue);
    public string Unformat(string formattedValue);
}

public sealed class BootstrapNumeralInputFormatter : IInputFormatter
{
    public BootstrapNumeralInputFormatter(BootstrapNumeralFormatOptions options);
    public string Format(string rawValue);
    public string Unformat(string formattedValue);
}

public sealed class BootstrapDateInputFormatter : IInputFormatter
{
    public BootstrapDateInputFormatter(BootstrapDateFormatOptions options);
    public string Format(string rawValue);
    public string Unformat(string formattedValue);
}

public sealed class BootstrapTimeInputFormatter : IInputFormatter
{
    public BootstrapTimeInputFormatter(BootstrapTimeFormatOptions options);
    public string Format(string rawValue);
    public string Unformat(string formattedValue);
}

public sealed class BootstrapCreditCardInputFormatter : IInputFormatter
{
    public BootstrapCreditCardInputFormatter(BootstrapCreditCardFormatOptions options);
    public BootstrapCreditCardType GetCardType(string value);
    public string Format(string rawValue);
    public string Unformat(string formattedValue);
}
```

### Designer option contracts

`BootstrapGeneralFormatOptions` defaults:

```text
Blocks            = empty array
Delimiter         = " "
Delimiters        = empty array
DelimiterLazyShow = false
Prefix            = ""
NumericOnly       = false
Uppercase         = false
Lowercase         = false
```

Validation:

- Every block length must be `> 0`.
- Empty `Blocks` means no block truncation/delimiter insertion; filtering/case/prefix can still apply.
- `Delimiters` is copied on assignment. Boundary `i` uses `Delimiters[i]` when present, otherwise `Delimiter`.
- `Uppercase` and `Lowercase` cannot both be true. Setting one to true automatically sets the other to false and raises one internal options-change notification.
- Prefix is display decoration and is excluded from `RawValue`.

`BootstrapNumeralFormatOptions` defaults:

```text
Delimiter           = ","
ThousandsGroupStyle = Thousand
IntegerScale        = 0      // unlimited
DecimalMark         = "."
DecimalScale        = 2
PositiveOnly        = false
TailPrefix          = false
SignBeforePrefix    = false
StripLeadingZeroes  = true
Prefix              = ""
```

Validation:

- `IntegerScale >= 0` and `DecimalScale >= 0`.
- `Delimiter` and `DecimalMark` may be multi-character only if tests prove caret mapping; v1 public setters therefore restrict both to zero or one character. Empty delimiter means no group separator. Empty decimal mark is rejected when `DecimalScale > 0`.
- Non-empty `Delimiter` must differ from `DecimalMark`.
- Canonical raw decimal separator is always `.`.
- Canonical raw sign is a single leading `-`; all other sign characters are removed.

`BootstrapDateFormatOptions` defaults:

```text
Pattern           = "dmY"
Delimiter         = "/"
DelimiterLazyShow = false
```

Pattern grammar:

- `d` = two-digit day.
- `m` = two-digit month.
- `y` = two-digit year.
- `Y` = four-digit year.
- Pattern must contain 1-3 unique components, cannot contain both `y` and `Y`, and rejects every other character.
- Structural shaping constrains month to 01-12 and day to 01-31 when a complete component is present. It does not validate day against month/year.

`BootstrapTimeFormatOptions` defaults:

```text
Pattern           = "hm"
Delimiter         = ":"
DelimiterLazyShow = false
TimeFormat        = TwentyFourHour
```

Pattern grammar:

- `h`, `m`, `s` are two-digit hour/minute/second components.
- Pattern must contain 1-3 unique components and rejects every other character.
- Completed hour is constrained to 00-23 for `TwentyFourHour` and 01-12 for `TwelveHour`.
- Completed minute/second is constrained to 00-59.
- No AM/PM suffix is generated in v1.

`BootstrapCreditCardFormatOptions` defaults:

```text
Delimiter         = " "
DelimiterLazyShow = false
StrictMode        = false
```

Rules:

- Input/raw value is digits only.
- Normal mode uses the detected type's documented block maximum, normally 14-16 digits.
- `StrictMode = true` extends the detected block layout to a total maximum of 19 digits.
- Block layouts are frozen as: UATP `4-5-6`; AmericanExpress `4-6-5`; Diners `4-6-4`; JCB15 `4-6-5`; all other detected/general types `4-4-4-4`, plus a final strict-mode block to total 19.
- IIN detection order and ranges follow the cleave-zen reference table from `src/credit-card/constants.ts`, including the 2221-2720 Mastercard family and the specific JCB/Discover/Maestro/MIR/UnionPay ranges. Detection is formatting metadata only; do not implement Luhn validation.

---

## Internal Editing Contract

### Caret mapping

Never restore caret using only `newText.Length - oldText.Length`.

Mapping uses canonical raw positions:

```text
candidate formatted position
        -> unformat candidate prefix
        -> raw character index
        -> format raw prefix under the effective formatter
        -> formatted position in final display
```

Internal API:

```csharp
internal static class InputCaretMapper
{
    public static int ToRawPosition(
        IInputFormatter formatter,
        string formattedText,
        int formattedPosition);

    public static int ToFormattedPosition(
        IInputFormatter formatter,
        string rawValue,
        int rawPosition);
}
```

Rules:

- Clamp incoming positions to valid string bounds.
- Mapping must work for collapsed caret and selection start/end independently.
- Prefix positions map to raw position 0.
- A caret at end maps to canonical raw length and then back to final text end.
- Mapping must tolerate formatting that removes invalid characters.

### Separator-aware deletion

Native Backspace/Delete on a formatting-only separator can otherwise remove only the separator, which is immediately reinserted. Detect decoration boundaries by comparing raw positions on both sides of the caret.

Forward rules:

- Backspace with no selection: if stepping one display character left does not reduce raw position, delete the previous raw character instead and reformat.
- Delete with no selection: if stepping one display character right does not increase raw position, delete the raw character at the current raw index instead and reformat.
- When a real raw character is adjacent, allow native Backspace/Delete and normalize through the ordinary TextChanged path.
- With a non-empty selection, always allow native deletion/replacement and normalize afterward.

### Undo/redo snapshots

Because programmatically replacing the inner TextBox text can invalidate native undo history, formatted input owns a small deterministic history stack:

```csharp
internal readonly struct FormattedTextSnapshot
{
    public FormattedTextSnapshot(string rawValue, int rawSelectionStart, int rawSelectionLength);
    public string RawValue { get; }
    public int RawSelectionStart { get; }
    public int RawSelectionLength { get; }
}

internal sealed class FormattedTextHistory
{
    public void Record(FormattedTextSnapshot snapshot);
    public bool TryUndo(FormattedTextSnapshot current, out FormattedTextSnapshot snapshot);
    public bool TryRedo(FormattedTextSnapshot current, out FormattedTextSnapshot snapshot);
    public void Clear();
}
```

Rules:

- Record the previous stable snapshot once per effective user edit.
- Consecutive duplicate snapshots are not stored.
- A new user edit clears redo history.
- Programmatic `Text`, `RawValue`, `FormatMode`, options, `Formatter`, and `Reformat()` changes clear history rather than creating entries.
- Ctrl+Z uses formatted history and suppresses the native undo command.
- Ctrl+Y uses formatted history and suppresses the native redo command.
- Restoring history maps raw selection back through the current formatter, so undo/redo remains valid after grouping/prefix insertion.
- Cap each stack at 100 snapshots to bound memory; discard the oldest undo entry when exceeding the cap.

---

## File Structure and Responsibilities

### Existing files to modify

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTextBox.cs`
  - Add the minimal protected editor/TextChanged/KeyDown extension seam.
  - Preserve all existing ordinary TextBox semantics.

- `docs/COMPONENTS.md`
  - Add the finalized `BootstrapFormattedTextBox` contract and distinguish it from `BootstrapNumericBox`.

- `docs/TESTING.md`
  - Add formatter and formatted-input keyboard/caret regression matrix.

- `docs/PUBLIC_API_BASELINE.md`
  - Record approved additive formatted-input API after review.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`
  - Add a focused API contract test and update fingerprint only after deliberate review.

- `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs`
  - Add a dedicated formatted-input section with live Text/RawValue/type output.

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs`
  - Verify demo registration/contract.

### New production files

- `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapFormattedTextBox.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/IInputFormatter.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapInputFormatMode.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapNumeralGroupStyle.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapTimeFormat.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapCreditCardType.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapGeneralFormatOptions.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapNumeralFormatOptions.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapDateFormatOptions.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapTimeFormatOptions.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapCreditCardFormatOptions.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapGeneralInputFormatter.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapNumeralInputFormatter.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapDateInputFormatter.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapTimeInputFormatter.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapCreditCardInputFormatter.cs`
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/InputFormatOptionValidation.cs` (internal)
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/InputCaretMapper.cs` (internal)
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/FormattedTextSnapshot.cs` (internal)
- `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/FormattedTextHistory.cs` (internal)

### New tests

- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapGeneralInputFormatterTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapNumeralInputFormatterTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapDateInputFormatterTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapTimeInputFormatterTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapCreditCardInputFormatterTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/InputCaretMapperTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/FormattedTextHistoryTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapFormattedTextBoxTests.cs`
- `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapFormattedTextBoxInteractionTests.cs`

---

### Task 1: Add a Safe `BootstrapTextBox` Inheritance Seam

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTextBox.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTextBoxTests.cs`

**Interfaces:**
- Consumes: existing private native `_editor` and current TextChanged/KeyDown forwarding paths.
- Produces: `protected TextBox Editor`, `protected virtual void OnEditorTextChanged(EventArgs e)`, and `protected virtual void OnEditorKeyDown(KeyEventArgs e)` without changing ordinary `BootstrapTextBox` behavior.

- [ ] **Step 1: Add regression tests proving the base control still forwards one final TextChanged and KeyDown event**

Add a small test subclass:

```csharp
private sealed class BootstrapTextBoxProbe : BootstrapTextBox
{
    public TextBox NativeEditor => Editor;

    public int EditorTextChangedCount { get; private set; }
    public int EditorKeyDownCount { get; private set; }

    protected override void OnEditorTextChanged(EventArgs e)
    {
        EditorTextChangedCount++;
        base.OnEditorTextChanged(e);
    }

    protected override void OnEditorKeyDown(KeyEventArgs e)
    {
        EditorKeyDownCount++;
        base.OnEditorKeyDown(e);
    }
}
```

Assert native typing/text mutation still produces one wrapper `TextChanged`, and a native KeyDown still produces one wrapper `KeyDown`.

- [ ] **Step 2: Run focused base tests before changing production code**

Run:

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapTextBoxTests"
```

Expected: the new probe test fails to compile because the protected seam does not exist.

- [ ] **Step 3: Refactor event wiring without changing behavior**

Use handler wrappers so WinForms delegate signatures stay private while derived classes receive clean virtual hooks:

```csharp
protected TextBox Editor => _editor;

protected virtual void OnEditorTextChanged(EventArgs e)
{
    UpdatePlaceholderVisibility();
    UpdateClearButtonVisibility();
    PerformLayout();
    OnTextChanged(e);
}

protected virtual void OnEditorKeyDown(KeyEventArgs e)
{
    OnKeyDown(e);
}

private void HandleEditorTextChanged(object? sender, EventArgs e)
{
    OnEditorTextChanged(e);
}

private void HandleEditorKeyDown(object? sender, KeyEventArgs e)
{
    OnEditorKeyDown(e);
}
```

Change constructor subscriptions from the old private handlers to `HandleEditorTextChanged` / `HandleEditorKeyDown`. Keep KeyPress, KeyUp, PreviewKeyDown, focus, clear-button, theme, and layout paths unchanged.

- [ ] **Step 4: Run all existing TextBox tests on both targets**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~BootstrapTextBoxTests"
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48 --filter "FullyQualifiedName~BootstrapTextBoxTests"
```

Expected: PASS on supported Windows development environment.

- [ ] **Step 5: Commit the inheritance seam**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapTextBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapTextBoxTests.cs
git commit -m "refactor: add formatted text box extension seam"
```

---

### Task 2: Define Formatting Contracts and Designer Options

**Files:**
- Create: all enum/interface/options files listed under `Formatting/`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/InputFormatOptionValidation.cs`
- Test through formatter test files created in later tasks; add `FormattingOptionsTests.cs` if keeping option validation separate is clearer.

**Interfaces:**
- Produces exactly the public formatting enums/interface/options specified above.
- Each options class exposes an `internal event EventHandler? Changed` raised once after an effective property change.

- [ ] **Step 1: Write failing tests for defaults, validation, defensive array copies, and option-change notification behavior**

Representative assertions:

```csharp
var general = new BootstrapGeneralFormatOptions();
Assert.That(general.Delimiter, Is.EqualTo(" "));
Assert.That(general.Blocks, Is.Empty);

var blocks = new[] { 4, 4, 4, 4 };
general.Blocks = blocks;
blocks[0] = 99;
Assert.That(general.Blocks, Is.EqualTo(new[] { 4, 4, 4, 4 }));

Assert.Throws<ArgumentException>(() => general.Blocks = new[] { 4, 0, 4 });

var numeral = new BootstrapNumeralFormatOptions();
Assert.That(numeral.ThousandsGroupStyle, Is.EqualTo(BootstrapNumeralGroupStyle.Thousand));
Assert.That(numeral.DecimalScale, Is.EqualTo(2));
Assert.Throws<ArgumentOutOfRangeException>(() => numeral.DecimalScale = -1);
```

Also test `Uppercase = true` clears `Lowercase`, and vice versa.

- [ ] **Step 2: Run tests and verify failure before implementation**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~FormattingOptionsTests"
```

Expected: FAIL/compile failure because contracts are absent.

- [ ] **Step 3: Implement contracts and option validation**

Use ordinary mutable properties with equality guards. Arrays return clones. All strings normalize null to empty. Validation happens before mutation so failed assignments preserve prior state. `Changed` remains internal and therefore does not expand consumer event surface.

- [ ] **Step 4: Run option tests on both targets**

Use the same command for `net8.0-windows` and `net48`; expected PASS.

- [ ] **Step 5: Commit formatting contracts**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Formatting tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting
git commit -m "feat: define formatted input contracts"
```

---

### Task 3: Implement General Block/Delimiter Formatting

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapGeneralInputFormatter.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapGeneralInputFormatterTests.cs`

**Interfaces:**
- Consumes: `IInputFormatter`, `BootstrapGeneralFormatOptions`.
- Produces: pure General `Format`/`Unformat` behavior.

- [ ] **Step 1: Write table-driven failing tests**

Required cases:

```text
Blocks [4,4,4,4], delimiter " ":
  raw  "1234567890123456" -> "1234 5678 9012 3456"

Blocks [4,3,3,4], delimiter "-":
  raw  "12345678901234"   -> "1234-567-890-1234"

Blocks [3,3,3,2], delimiters [".", ".", "-"]:
  raw  "12345678901"      -> "123.456.789-01"

NumericOnly:
  "AB12-C3" -> raw "123"

Uppercase:
  "ab-cd" -> raw/display "ABCD" when blocks are empty

Prefix "VN", blocks [4,4]:
  raw "12345678" -> display "VN1234 5678"
  empty raw       -> empty display
```

Add round-trip assertions for every case:

```csharp
var formatted = formatter.Format(raw);
Assert.That(formatter.Unformat(formatted), Is.EqualTo(expectedCanonicalRaw));
Assert.That(formatter.Format(formatter.Unformat(formatted)), Is.EqualTo(formatted));
```

Cover eager versus lazy delimiter behavior at exact block boundaries.

- [ ] **Step 2: Run tests and verify failure**

Expected: compile failure because formatter is absent.

- [ ] **Step 3: Implement with one normalization pass and one formatting pass**

Implementation sequence:

1. Normalize candidate by removing a leading configured prefix.
2. Remove configured delimiter strings.
3. Apply numeric-only filter when enabled.
4. Apply case conversion with `ToUpperInvariant` / `ToLowerInvariant`.
5. Truncate to total block capacity when blocks exist.
6. Build formatted output from blocks and boundary delimiters.
7. Add prefix only when canonical raw is non-empty.

Do not build a regex-based generic mask engine.

- [ ] **Step 4: Run General formatter tests on both targets**

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapGeneralInputFormatter.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapGeneralInputFormatterTests.cs
git commit -m "feat: add general input formatter"
```

---

### Task 4: Implement Numeral Formatting with Invariant Raw Value

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapNumeralInputFormatter.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapNumeralInputFormatterTests.cs`

**Interfaces:**
- Consumes: numeral options/group style.
- Produces: free-form numeric string formatter distinct from `BootstrapNumericBox`.

- [ ] **Step 1: Write failing tests for canonicalization and grouping**

Required examples:

```text
Default:       "1234567.89" -> "1,234,567.89"
Display mark:  options DecimalMark="," Delimiter="."; raw "1234567.89" -> "1.234.567,89"
Unformat:      "1.234.567,89" -> "1234567.89"
Negative:      "-1234.5" -> "-1,234.5"
PositiveOnly:  "-1234" -> "1,234"
Prefix:        raw "1234" + Prefix="$" -> "$1,234"
SignBefore:    raw "-1234" + Prefix="$" -> "-$1,234"
TailPrefix:    raw "-1234" + Prefix=" kg" -> "-1,234 kg"
IntegerScale:  4 => "123456" canonicalizes to "1234"
DecimalScale:  2 => "12.3456" canonicalizes to "12.34"
Strip zeros:   "000123.4" -> "123.4"
Group Lakh:    "12345678" -> "1,23,45,678"
Group Wan:     "12345678" -> "1234,5678"
Group None:    "12345678" -> "12345678"
Empty raw:     "" -> ""
Partial minus: "-" remains a valid partial raw state when PositiveOnly=false
```

- [ ] **Step 2: Add tests proving numeral formatting does not expose `decimal` semantics**

Verify very long digit strings up to configured `IntegerScale` are handled as strings without decimal overflow. This protects the intended distinction from `BootstrapNumericBox`.

- [ ] **Step 3: Implement canonical raw normalization**

`Unformat` converts the configured display decimal mark to reserved internal marker, removes all characters except digits/leading minus/marker, restores marker as `.`, and enforces one leading sign and one decimal point. `Format` consumes canonical raw `.` and emits configured decimal mark/grouping/prefix.

- [ ] **Step 4: Run tests on both targets**

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapNumeralInputFormatter.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapNumeralInputFormatterTests.cs
git commit -m "feat: add numeral input formatter"
```

---

### Task 5: Implement Date and Time Formatters

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapDateInputFormatter.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapTimeInputFormatter.cs`
- Create: corresponding two test files.

**Interfaces:**
- Consumes: Date/Time options.
- Produces: structural partial-input formatters.

- [ ] **Step 1: Write Date formatter tests**

Required cases:

```text
Pattern dmY, delimiter /:
  "31082026" -> "31/08/2026"

Pattern Ymd, delimiter -:
  "20260831" -> "2026-08-31"

Pattern my:
  "0826" -> "08/26"

Partial input:
  "3" -> "3"
  "31" with eager delimiter -> "31/"
  "31" with lazy delimiter  -> "31"
  "310" -> "31/0"

Sanitize:
  "31a08b2026" -> canonical raw "31082026"
```

Also test component shaping for complete `m` and `d` groups and explicitly prove a cross-component invalid value such as `31/02/2026` remains representable; validation is outside the formatter.

- [ ] **Step 2: Write Time formatter tests**

Required cases:

```text
Pattern hm:   "1230" -> "12:30"
Pattern hms:  "123045" -> "12:30:45"
Partial:      "12" eager -> "12:"
Partial:      "12" lazy  -> "12"
24-hour:      completed hour remains within 00-23
12-hour:      completed hour remains within 01-12
minute/second completed groups remain within 00-59
```

- [ ] **Step 3: Implement a shared internal block formatter only if it removes actual duplication**

A small internal helper may map component widths and delimiters, but Date and Time retain their own component shaping rules. Do not expose a generic mask parser publicly.

- [ ] **Step 4: Run Date/Time tests on both targets**

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapDateInputFormatter.cs src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapTimeInputFormatter.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapDateInputFormatterTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapTimeInputFormatterTests.cs
git commit -m "feat: add date and time input formatters"
```

---

### Task 6: Implement Credit-Card Formatting and Type Detection

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapCreditCardInputFormatter.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapCreditCardInputFormatterTests.cs`

**Interfaces:**
- Consumes: credit-card options/type enum.
- Produces: digits-only raw value, type-dependent blocks, `GetCardType`.

- [ ] **Step 1: Freeze the IIN table in tests before implementation**

Use representative prefixes for every public type and assert detection:

```text
1...       -> Uatp except 1800...
34/37...   -> AmericanExpress
300-305, 309, 36, 38, 39 -> Diners
6011, 65, 644-649 -> Discover
51-55 and 2221-2720 -> Mastercard
5019, 4175, 4571 -> Dankort
637-639 -> Instapayment
2131/1800 -> Jcb15
35 -> Jcb
50, 56-58, 6304, 67 -> Maestro
4 -> Visa
2200-2204 -> Mir
62/81 -> UnionPay
otherwise -> General
```

Add boundary tests for Mastercard 2220/2221/2720/2721 so numeric-range logic is correct rather than a broad `2[2-7]` approximation.

- [ ] **Step 2: Add formatting tests**

```text
Visa/general 16 digits -> 4-4-4-4
AmEx 15 digits         -> 4-6-5
Diners 14 digits       -> 4-6-4
Strict 19 digits       -> original blocks plus final block to total 19
Delimiter "-"          -> block separator changes without changing raw value
Unformat                -> digits only
```

- [ ] **Step 3: Implement ordered type detection and block selection**

Preserve specific-pattern priority before broad patterns. Use simple prefix/range helpers where practical; if static regexes are used, construct them once and make the order explicit in one readonly table.

- [ ] **Step 4: Run CreditCard tests on both targets**

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Formatting/BootstrapCreditCardInputFormatter.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/BootstrapCreditCardInputFormatterTests.cs
git commit -m "feat: add credit card input formatter"
```

---

### Task 7: Build Caret Mapping and Bounded Edit History as Pure Helpers

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/InputCaretMapper.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/FormattedTextSnapshot.cs`
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Formatting/FormattedTextHistory.cs`
- Create: corresponding two test files.

**Interfaces:**
- Produces caret raw/display mapping and bounded undo/redo snapshots used by the control.

- [ ] **Step 1: Write caret mapping tests using real built-in formatters**

Required scenarios:

```text
"12|34 5678" under 4-digit blocks maps through raw index correctly.
"1,23|4,567" under numeral formatting maps without jumping to end.
Prefix positions before/inside "$" map to raw index 0.
End-of-text maps to raw length and back to display end.
Selection start/end across a delimiter preserve selected raw characters.
Invalid inserted characters removed by formatter do not produce out-of-range selection.
```

Test mapping at every position `0..Text.Length` for representative General, Numeral, Date, and CreditCard values and assert returned positions are always within final display bounds.

- [ ] **Step 2: Write history tests**

Verify:

- first edit can undo to initial snapshot;
- redo restores undone snapshot;
- new edit after undo clears redo;
- duplicate record is ignored;
- stack never exceeds 100 undo entries;
- selection is stored in raw coordinates.

- [ ] **Step 3: Implement helpers with no WinForms dependency**

`InputCaretMapper.ToRawPosition` unformats the display prefix up to the requested position. `ToFormattedPosition` formats the raw prefix up to the requested raw index and clamps the result against formatting of the complete raw value.

- [ ] **Step 4: Run pure helper tests on both targets**

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Formatting/InputCaretMapper.cs src/MyDmsVn.Bootstrap5WinFormUI/Formatting/FormattedTextSnapshot.cs src/MyDmsVn.Bootstrap5WinFormUI/Formatting/FormattedTextHistory.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/InputCaretMapperTests.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Formatting/FormattedTextHistoryTests.cs
git commit -m "feat: add formatted input caret and history helpers"
```

---

### Task 8: Implement `BootstrapFormattedTextBox` Core Value Pipeline

**Files:**
- Create: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapFormattedTextBox.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapFormattedTextBoxTests.cs`

**Interfaces:**
- Consumes: protected `Editor`/TextChanged seam, all formatter contracts/options, caret mapper/history.
- Produces: the public control API defined above.

- [ ] **Step 1: Write failing default/API behavior tests**

Assert:

```csharp
using var input = new BootstrapFormattedTextBox();
Assert.That(input.FormatMode, Is.EqualTo(BootstrapInputFormatMode.None));
Assert.That(input.Text, Is.Empty);
Assert.That(input.RawValue, Is.Empty);
Assert.That(input.CreditCardType, Is.EqualTo(BootstrapCreditCardType.General));
Assert.That(input.TabStop, Is.True);
Assert.That(input.Controls.OfType<TextBox>().Single().TabStop, Is.False);
```

Add tests for General/Numeral/Date/Time/CreditCard mode assignment, custom formatter, option change reformatting, `Reformat()`, and identity behavior for `Custom` + null formatter.

- [ ] **Step 2: Add exact event-order/count tests**

For an edit changing both values, collect event names:

```csharp
var events = new List<string>();
input.TextChanged += (_, _) => events.Add("TextChanged");
input.RawValueChanged += (_, _) => events.Add("RawValueChanged");
```

Expected sequence is exactly:

```text
TextChanged
RawValueChanged
```

No event should expose the transient candidate value.

- [ ] **Step 3: Implement effective formatter selection**

Construct one built-in formatter per options object in the control constructor and reuse it:

```text
None       -> identity formatter
General    -> BootstrapGeneralInputFormatter
Numeral    -> BootstrapNumeralInputFormatter
Date       -> BootstrapDateInputFormatter
Time       -> BootstrapTimeInputFormatter
CreditCard -> BootstrapCreditCardInputFormatter
Custom     -> Formatter ?? identity formatter
```

Subscribe once to each options object's internal `Changed` event and detach in `Dispose(bool)`.

- [ ] **Step 4: Implement re-entrant-safe native TextChanged normalization**

Core shape:

```csharp
protected override void OnEditorTextChanged(EventArgs e)
{
    if (_applyingFormattedText)
    {
        return;
    }

    ApplyCandidateText(
        Editor.Text,
        Editor.SelectionStart,
        Editor.SelectionLength,
        recordHistory: true,
        raiseEvents: true);
}
```

`ApplyCandidateText` must:

1. capture previous stable raw/display pair;
2. map candidate selection start/end to raw positions;
3. unformat candidate to canonical raw;
4. format canonical raw to final display;
5. set inner editor text under `_applyingFormattedText` only when display differs;
6. restore mapped final selection;
7. update `_rawValue` and card type;
8. call `base.OnEditorTextChanged(e)` exactly once only when final display changed;
9. raise `RawValueChanged` only when canonical raw changed;
10. raise `CreditCardTypeChanged` only on effective type change.

- [ ] **Step 5: Override `Text` and implement `RawValue` programmatic semantics**

Programmatic assignment clears history, never records an undo entry, and leaves the control with a canonical stable pair before returning to the caller.

- [ ] **Step 6: Run control unit tests on both targets**

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapFormattedTextBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapFormattedTextBoxTests.cs
git commit -m "feat: add BootstrapFormattedTextBox core"
```

---

### Task 9: Implement Natural Keyboard Editing, Separator Deletion, Undo/Redo, and Focus Regression Coverage

**Files:**
- Modify: `src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapFormattedTextBox.cs`
- Create: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapFormattedTextBoxInteractionTests.cs`

**Interfaces:**
- Consumes: protected KeyDown seam, caret mapper, history.
- Produces: predictable desktop editing behavior.

- [ ] **Step 1: Add STA interaction tests for insertion in the middle**

Start with a formatted value such as `1,234,567.89`, set `SelectionStart` in the middle through the native editor, mutate native text as a user edit would, invoke the TextChanged path, and assert both final text and caret remain adjacent to the edited raw digit rather than moving to the end.

Repeat for General blocks and CreditCard.

- [ ] **Step 2: Add selection replacement/paste-like tests**

Cover replacing a selection spanning a delimiter, pasting already-formatted text, pasting raw text, and pasting text containing invalid characters. Assert final `RawValue`, `Text`, `SelectionStart`, and event counts.

- [ ] **Step 3: Add Backspace/Delete separator tests**

Required behavior examples:

```text
General "1234-5678", caret immediately after '-' + Backspace
  -> delete raw '4', not merely '-'
  -> final display "1235-678" according to remaining raw sequence

General "1234-5678", caret immediately before '-' + Delete
  -> delete raw '5'
  -> delimiter is recomputed naturally
```

Use raw-position comparisons rather than checking hard-coded separator characters so prefix/numeral/date modes share the logic.

- [ ] **Step 4: Implement KeyDown flow without swallowing ordinary keys**

Override:

```csharp
protected override void OnEditorKeyDown(KeyEventArgs e)
{
    base.OnEditorKeyDown(e);
    if (e.Handled || e.SuppressKeyPress)
    {
        return;
    }

    if (TryHandleUndoRedo(e) || TryHandleFormattingSeparatorDeletion(e))
    {
        e.Handled = true;
        e.SuppressKeyPress = true;
    }
}
```

Only Ctrl+Z, Ctrl+Y, and the specific zero-selection formatting-decoration Backspace/Delete cases are consumed by the formatted control.

- [ ] **Step 5: Add undo/redo interaction tests**

Exercise at least three user edits, two undo operations, one redo, then a new edit. Assert raw/display/selection at each step and redo clearing after the branch edit.

- [ ] **Step 6: Add focus/key regression tests**

Host `Button -> BootstrapFormattedTextBox -> Button` in a real `Form` and assert:

- entering the outer control focuses the native editor;
- `TabStop` remains one composite stop;
- `SelectNextControl` forward/backward can leave the control normally;
- forwarding an Alt KeyDown does not modify Text/RawValue and formatting code does not mark it handled;
- Ctrl+A/C/X/V remain native paths; X/V resulting text changes are formatted once.

Do not use a global hook or application-wide message filter in tests.

- [ ] **Step 7: Run interaction tests on both targets**

Expected: PASS.

- [ ] **Step 8: Commit**

```powershell
git add src/MyDmsVn.Bootstrap5WinFormUI/Controls/BootstrapFormattedTextBox.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Controls/BootstrapFormattedTextBoxInteractionTests.cs
git commit -m "feat: harden formatted input editing behavior"
```

---

### Task 10: Add Advanced Inputs Demo Scenarios

**Files:**
- Modify: `demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs`
- Modify layout regression test only if the additional section changes an asserted minimum size/scroll contract.

**Interfaces:**
- Produces a manual acceptance surface without demo-only formatting/key handlers.

- [ ] **Step 1: Add `_formattedSection` to the existing scrollable Advanced Inputs page**

Place it before NumericBox so users see the distinction clearly. Keep existing sections intact.

- [ ] **Step 2: Add these scenarios**

1. General blocks: `1234-567-890-1234` pattern with live RawValue label.
2. Numeral: `1,234,567.89` with live RawValue label.
3. Numeral Vietnamese-style separators: display `1.234.567,89`, raw `1234567.89`.
4. Date: `dd/MM/yyyy`-style pattern.
5. Time: `HH:mm:ss`-style pattern.
6. Credit card: live formatted value, RawValue, and detected type.
7. Custom formatter: a tiny demo-local `IInputFormatter` that groups uppercase characters as `AAA-BBB` to prove extensibility without modifying framework code.
8. Existing inherited appearance behavior: placeholder, validation state, icon/clear button on at least one formatted example.

- [ ] **Step 3: Add demo contract tests**

Verify the form contains formatted controls for all built-in modes, that live status labels update when RawValue changes, and existing Numeric/Combo/Date/Calendar sections still exist.

- [ ] **Step 4: Run demo tests**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~AdvancedInputsDemo"
```

Expected: PASS.

- [ ] **Step 5: Manual acceptance**

Launch the demo and manually verify:

```powershell
dotnet run --project .\demo\MyDmsVn.Bootstrap5WinFormUI.Demo\MyDmsVn.Bootstrap5WinFormUI.Demo.csproj -c Release
```

For every formatted example:

- type at beginning/middle/end;
- select across a delimiter and replace;
- Backspace/Delete adjacent to delimiter;
- Ctrl+A/C/X/V/Z/Y;
- Tab/Shift+Tab into and out of the control;
- press/release Alt;
- use clear button;
- switch Light/Dark;
- resize the form repeatedly;
- verify at Windows 100%, 125%, 150%, 175%, and 200% DPI where available.

Also type Vietnamese text with a normal system IME into the General/custom examples and confirm formatting occurs after committed text without corruption. If a reproducible IME composition defect appears, do not add a global message filter as a shortcut; capture it as a blocking regression and implement the smallest native-editor composition seam in a follow-up commit before considering the feature complete.

- [ ] **Step 6: Commit demo**

```powershell
git add demo/MyDmsVn.Bootstrap5WinFormUI.Demo/AdvancedInputsDemoForm.cs tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Demo/AdvancedInputsDemoFormTests.cs
git commit -m "demo: add formatted input scenarios"
```

---

### Task 11: Document the Contract and Update Public API Baseline Deliberately

**Files:**
- Modify: `docs/COMPONENTS.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/PUBLIC_API_BASELINE.md`
- Modify: `tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs`

**Interfaces:**
- Produces final reviewed public/protected API and test documentation.

- [ ] **Step 1: Add `BootstrapFormattedTextBox` to `docs/COMPONENTS.md`**

Document:

- formatted Text vs RawValue;
- pure formatter architecture;
- built-in modes/options;
- custom `IInputFormatter`;
- caret/selection/history semantics;
- validation boundary;
- distinction from NumericBox;
- v1 exclusions: Phone, typed DecimalValue/DateTime, regex-mask DSL.

- [ ] **Step 2: Add the regression matrix to `docs/TESTING.md`**

Include format round trips, partial input, middle edits, selection replacement, paste/cut, separator deletion, undo/redo, Tab/Shift+Tab, Alt, clear, theme, DPI, and manual IME smoke coverage.

- [ ] **Step 3: Add a focused exported API test before changing the fingerprint**

Assert the declared control properties/events/method and formatter exported type names explicitly. Also assert `BootstrapTextBox.Editor`, `OnEditorTextChanged`, and `OnEditorKeyDown` are protected rather than public.

Representative expected declared properties on `BootstrapFormattedTextBox`:

```text
CreditCardOptions
CreditCardType
DateOptions
FormatMode
Formatter
GeneralOptions
NumeralOptions
RawValue
Text
TimeOptions
```

Expected declared events:

```text
CreditCardTypeChanged
RawValueChanged
```

Expected declared method:

```text
Reformat
```

Do not expose the internal caret/history/validation helpers.

- [ ] **Step 4: Run the API baseline and observe intentional failure**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows --filter "FullyQualifiedName~Phase16PublicApiBaselineTests"
```

Expected: the focused contract assertions pass after implementation, while `ExportedApiMatchesApprovedV1Baseline` fails and prints the new fingerprint/API surface.

- [ ] **Step 5: Review the printed API for accidental exports**

Reject and fix the implementation before baseline update if the output contains any of:

```text
InputCaretMapper
FormattedTextSnapshot
FormattedTextHistory
InputFormatOptionValidation
native editor implementation types
additional public TextBox child exposure
```

Also confirm the formatter/options/enums listed in this plan are present and no alias properties were introduced.

- [ ] **Step 6: Update `docs/PUBLIC_API_BASELINE.md` and fingerprint**

Copy the reviewed actual SHA-256 fingerprint printed by the test into `ApprovedV1Fingerprint` only after Step 5 passes review. Keep assembly compatibility version unchanged unless the repository's release policy separately requires a package-version change.

- [ ] **Step 7: Run full tests on both targets**

```powershell
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net8.0-windows
dotnet test .\tests\MyDmsVn.Bootstrap5WinFormUI.Tests\MyDmsVn.Bootstrap5WinFormUI.Tests.csproj -c Release -f net48
```

Expected: PASS.

- [ ] **Step 8: Build the solution in Release**

```powershell
dotnet build .\MyDmsVn.Bootstrap5WinFormUI.sln -c Release
```

Expected: PASS for all supported projects/targets on Windows.

- [ ] **Step 9: Commit docs/API approval**

```powershell
git add docs/COMPONENTS.md docs/TESTING.md docs/PUBLIC_API_BASELINE.md tests/MyDmsVn.Bootstrap5WinFormUI.Tests/Release/Phase16PublicApiBaselineTests.cs
git commit -m "docs: finalize formatted input contract"
```

---

## Final Acceptance Checklist

- [ ] Ordinary `BootstrapTextBox` tests remain green and its runtime behavior is unchanged.
- [ ] `BootstrapFormattedTextBox` derives from `BootstrapTextBox` and reuses its theme, validation, icon, placeholder, clear-button, read-only, password, focus, font, DPI, and disposal behavior rather than duplicating the shell.
- [ ] Format engine has no WinForms dependency.
- [ ] General, Numeral, Date, Time, and CreditCard formatters each pass round-trip/canonicalization tests on both targets.
- [ ] `Text` always exposes final formatted text; `RawValue` always exposes canonical unformatted text.
- [ ] Empty RawValue produces empty Text even when prefix is configured.
- [ ] Numeral RawValue uses invariant `.` decimal separator.
- [ ] Credit-card type detection matches the frozen IIN boundary tests and no Luhn/business validation is implied.
- [ ] Custom formatter works through `IInputFormatter` without framework modification.
- [ ] Mid-string typing, selection replacement, paste, cut, Backspace/Delete near delimiters, and caret restoration remain predictable.
- [ ] Ctrl+Z/Ctrl+Y operate on bounded formatted-input history and do not depend on invalidated native undo state.
- [ ] Ctrl+A/C/X/V remain native editing paths.
- [ ] Tab/Shift+Tab leave the composite normally; Alt is not intercepted and does not mutate value.
- [ ] No global hook/message filter/polling loop/new package is introduced.
- [ ] Demo clearly distinguishes formatted Numeral mode from `BootstrapNumericBox`.
- [ ] Light/Dark and DPI behavior remain inherited from `BootstrapTextBox`.
- [ ] Manual Vietnamese IME smoke test does not corrupt committed text.
- [ ] `Phase16PublicApiBaselineTests` contains an explicit formatted-input contract and the approved fingerprint was updated only after review.
- [ ] Full `net48` and `net8.0-windows` test suites pass.
- [ ] Release solution build passes.

## Recommended Implementation Order

The dependency order is intentional:

```text
BootstrapTextBox extension seam
        -> formatting contracts/options
        -> pure General/Numeral/Date/Time/CreditCard formatters
        -> caret/history helpers
        -> BootstrapFormattedTextBox value pipeline
        -> keyboard/editing hardening
        -> demo/manual verification
        -> docs/public API review/full regression
```

Do not start by writing all formatting logic inside `BootstrapFormattedTextBox.TextChanged`. The formatter layer must be independently testable before the WinForms integration is added.
