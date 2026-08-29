# BootstrapSelect

`BootstrapSelect` is the framework's Select2-style WinForms selector for searchable single/multiple selection, grouped results, custom values, asynchronous providers, and paged result sets.

It is intentionally separate from `BootstrapComboBox`. `BootstrapComboBox` preserves native WinForms `ComboBox` binding/editing/popup semantics, while `BootstrapSelect` owns a dedicated selection model and managed overlay result surface.

## Local single selection

Use the caller-owned `Items` collection for local mode. `BootstrapSelectItem.Value` is the logical identity; `Text` is presentation.

```csharp
var customerSelect = new BootstrapSelect
{
    Placeholder = "Choose a customer...",
    SearchEnabled = true
};

customerSelect.Items.Add(new BootstrapSelectItem(1, "Contoso"));
customerSelect.Items.Add(new BootstrapSelectItem(2, "Fabrikam"));
customerSelect.Items.Add(new BootstrapSelectItem(3, "Northwind"));

customerSelect.SelectionChanged += (_, _) =>
{
    var customerId = customerSelect.SelectedValue;
    var customer = customerSelect.SelectedItem;
};
```

The built-in local matcher performs case-insensitive text matching. Assign `Matcher` to replace matching/ranking behavior. Local matching is never run over asynchronous provider results.

## Multiple selection and grouping

Set `SelectionMode = BootstrapSelectMode.Multiple` for chip-based multiple selection. Duplicate logical values are prevented using `ValueComparer`.

```csharp
var productSelect = new BootstrapSelect
{
    SelectionMode = BootstrapSelectMode.Multiple,
    Placeholder = "Choose products...",
    MaximumSelectionRows = 3
};

productSelect.Items.Add(new BootstrapSelectItem("crm", "CRM Suite")
{
    Group = "Business Apps"
});
productSelect.Items.Add(new BootstrapSelectItem("erp", "ERP Core")
{
    Group = "Business Apps"
});
productSelect.Items.Add(new BootstrapSelectItem("mail", "Mail Gateway")
{
    Group = "Infrastructure"
});
```

`Group` is nullable metadata on each item. Group headers are non-selectable. Local filtering removes empty groups, and paged provider results reconcile adjacent group boundaries without creating duplicate headers.

Single mode closes the popup after selection by default. Multiple mode remains open by default. `CloseOnSelect` can override the mode-sensitive default.

## Custom values

Custom values are opt-in.

```csharp
productSelect.AllowCustomValues = true;
productSelect.CustomValueFactory = text =>
{
    var normalized = text.Trim();
    if (normalized.Length == 0)
        return null;

    return new BootstrapSelectItem(
        "custom:" + normalized.ToLowerInvariant(),
        normalized);
};
```

When enabled, a non-empty search with no exact text match can expose a `Create '…'` action. Partial/fuzzy matches do not suppress creation. Returning `null` rejects the custom value. A successful item uses the normal cancellable selection pipeline with `BootstrapSelectChangeReason.CustomValue`.

## Asynchronous providers

`IBootstrapSelectDataProvider` is transport-agnostic. The UI library does not own `HttpClient`, REST, database, cache, or service dependencies.

```csharp
public sealed class CustomerSelectProvider : IBootstrapSelectDataProvider
{
    public async Task<BootstrapSelectPage> SearchAsync(
        BootstrapSelectQuery query,
        CancellationToken cancellationToken)
    {
        var result = await customerService.SearchAsync(
            query.SearchText,
            query.Page,
            query.PageSize,
            cancellationToken);

        return new BootstrapSelectPage(
            result.Items.Select(x => new BootstrapSelectItem(x.Id, x.Name)),
            result.HasMore);
    }
}

var remote = new BootstrapSelect
{
    DataProvider = new CustomerSelectProvider(),
    MinimumSearchLength = 2,
    SearchDebounce = TimeSpan.FromMilliseconds(250),
    PageSize = 20
};
```

Local `Items` and `DataProvider` are mutually exclusive result modes. While `DataProvider` is set, local items are not merged into remote pages.

The control owns query mechanics: debounce, cancellation, generation/race protection, page state, load-more serialization, loading/error presentation, retry, and result deduplication. The provider only retrieves a page.

A provider may honor cancellation, but correctness does not depend on it. If an older provider request completes after a newer logical query, generation checks prevent stale results from overwriting current state.

## Paging and retry

`BootstrapSelectQuery.Page` is one-based and `PageSize` is the configured requested size. `BootstrapSelectPage.HasMore` controls infinite-load continuation.

Only one load-more request is active for the current logical query. The page number advances only after success. Later-page failure keeps already loaded rows and adds an actionable retry row for the same failed page. First-page failure exposes a retry action for page 1.

Results are deduplicated using `ValueComparer`. If a later page returns a new item instance with the same logical value, the loaded metadata and any selected snapshot may be refreshed without raising a false `SelectionChanged`.

## Selection identity and lifetime

`BootstrapSelectItem.Value` is the sole logical identity. Object reference identity is not used for selection reconciliation.

`SelectedItem`, `SelectedItems`, `SelectedValue`, and `SelectedValues` represent logical selection independently from the currently displayed result page. Filtering, opening/closing the popup, paging, and provider result replacement do not make an already-selected value disappear.

`ValueComparer` defaults to `EqualityComparer<object>.Default` and may be replaced when application values need another equality policy.

## Selection events

Selection mutations use the following primary events:

- `Selecting` — cancellable before add.
- `Selected` — after add.
- `Deselecting` — cancellable before removal.
- `Deselected` — after removal.
- `SelectionChanged` — once after the logical mutation/batch completes.

Async query diagnostics use `SearchStarted`, `SearchCompleted`, and `SearchFailed`. Expected cancellation does not raise `SearchFailed`.

## Rendering extension

`Renderer` accepts an `IBootstrapSelectRenderer`. The renderer receives item/state/theme/DPI context for result rows, group headers, the single selected value, and multiple-selection chips.

The control remains responsible for layout, hit testing, scrolling, keyboard behavior, selection, paging, and popup lifecycle. Renderer implementations should present supplied state rather than own behavior.

## Keyboard and focus behavior

Closed control:

- `Alt+Down`, `F4`, `Enter`, or `Space` opens the popup.
- Printable input opens and enters search.
- `Delete` clears a clearable single selection.
- `Backspace` removes the last selected chip in multiple mode when appropriate.

Open popup:

- `Up` / `Down` navigate selectable rows.
- `Home` / `End` move to first/last selectable loaded row.
- `PageUp` / `PageDown` page the result viewport.
- `Enter` activates the highlighted item, create action, or retry action.
- `Esc` closes and restores focus appropriately.
- `Tab` closes without stealing normal WinForms tab traversal.

Search editing stays on a real WinForms text editor so caret, selection, clipboard, IME, and Vietnamese input remain native editing responsibilities.

## Accessibility

The outer control exposes ComboBox-style accessibility semantics. It reports focusability and expanded/collapsed popup state. Single mode exposes the selected text as its accessible value; multiple mode exposes a stable `<count> selected` summary.

Primary mouse operations have keyboard equivalents, including retry and custom-value creation.

## Theme, DPI, RTL, and popup placement

The selection surface and popup consume the existing framework theme/render/DPI infrastructure. Popup placement reuses the shared overlay placement/collision engine with bottom-start preference plus flip/shift behavior within the monitor working area.

`DropDownWidth = 0` uses owner-relative automatic width. `MaxDropDownHeight` caps popup height in logical pixels. Popup geometry repositions with owner movement and shared overlay tracking.

Selection geometry and result metrics are DPI-scaled. The implementation is validated at 96/120/144/192 DPI (100/125/150/200%). `RightToLeft.Yes` mirrors major horizontal selection-surface affordances.

## Ownership and disposal

Caller-owned objects are not disposed by `BootstrapSelect`:

- `DataProvider`;
- custom `Matcher`;
- custom `Renderer`;
- caller-created `BootstrapSelectItem` instances;
- item `Tag` objects.

The control disposes only infrastructure it creates, including debounce/search state, cancellation resources, overlay/popup content, owned event subscriptions, and owned rendering resources.

Popup close/open cycles reuse popup infrastructure while the control remains alive. Hiding, disabling, disposing, or recreating the owner handle closes the active popup while preserving logical selection.

## Integrated demo

Run the demo and choose **Select**. The page includes local single selection, multiple chips, grouping, custom values, validation, asynchronous delayed providers, infinite paging, later-page failure/retry, and rapid-typing race scenarios.

Use the integrated Light/Dark switch and verify keyboard-only operation. For desktop validation, repeat near monitor edges and at 100%, 125%, 150%, and 200% Windows display scaling.
