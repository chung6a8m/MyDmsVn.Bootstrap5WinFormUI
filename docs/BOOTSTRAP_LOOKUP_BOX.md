# BootstrapLookupBox and BootstrapLookupColumn

`BootstrapLookupBox` is a native WinForms, single-selection lookup for local application data. It keeps committed `SelectedItem`/`SelectedValue`, pending editor `Text`, and transient `HighlightedItem` separate. Searching or moving the highlight never commits a value and never changes `BindingSource.Position`.

## Standalone lookup

```csharp
var products = new BindingList<Product>
{
    new Product(1, "CF-A", "Cà phê rang xay"),
    new Product(2, "TRA-O", "Trà ô long")
};
var lookup = new BootstrapLookupBox
{
    DataSource = products,
    DisplayMember = nameof(Product.Name),
    ValueMember = nameof(Product.Id),
    SearchDebounceMilliseconds = 150,
    ShowRefreshButton = true,
    ShowAddNewButton = true
};
lookup.SearchMembers.Add(nameof(Product.Name));
lookup.SearchMembers.Add(nameof(Product.Code));
lookup.Columns.Add(new BootstrapLookupColumnDefinition { DataPropertyName = nameof(Product.Code), HeaderText = "Code" });
lookup.Columns.Add(new BootstrapLookupColumnDefinition { DataPropertyName = nameof(Product.Name), HeaderText = "Product", Width = 240 });
```

Search is Vietnamese-diacritic-insensitive by default. Multiple normalized tokens use AND semantics and may match different members. Results rank by worst token quality, total quality, `DisplayMember` best-match count, member priority, then source order. `SearchTextNormalizer` customizes search; `TextNormalizer` and `TextComparer` customize exact commit resolution.

An exact display match commits only when all matching rows have one distinct logical value. Duplicate display text with different values is ambiguous and blocks silent selection; duplicate rows with the same value resolve to the first source row. Empty text clears. Unmatched text follows `RestorePreviousSelection`, `KeepFocusWithValidationError`, or `CommitAndAdd`. The latter requires an add-capable local list and may use `CreateItemFromText` for typed models.

`RefreshResults()` raises `RefreshRequested`, reconciles the local source, and reapplies the query. Add New raises `AddNewRequested`; a valid returned `NewItem` is committed. Application exceptions are not swallowed.

Keyboard focus stays in the editor: Up/Down/Home/End/PageUp/PageDown move only highlight; Enter commits/resolves; Tab resolves before native traversal; Escape and `CancelPendingEdit()` discard pending text; F4, Down, and Alt+Down open. Application deactivation closes presentation only and preserves committed and pending state.

## DataGridView lookup column

```csharp
var lines = new BindingList<OrderLine>();
var binding = new BindingSource { DataSource = lines };
var productColumn = new BootstrapLookupColumn
{
    HeaderText = "Product",
    DataPropertyName = nameof(OrderLine.ProductId), // raw row value
    DataSource = products,
    ValueMember = nameof(Product.Id),
    DisplayMember = nameof(Product.Name)
};
productColumn.SearchMembers.Add(nameof(Product.Name));
productColumn.SelectionCommitted += (_, e) =>
{
    var line = (OrderLine)e.DataGridView.Rows[e.RowIndex].DataBoundItem;
    line.Unit = ((Product)e.Item!).Unit;
};
grid.AutoGenerateColumns = false;
grid.AllowUserToAddRows = true;
grid.Columns.Add(productColumn);
grid.DataSource = binding;
```

The cell stores raw `ValueMember` values and formats through `DisplayMember`. The reused internal editor closes old presentation, cancels debounce, detaches the old source/event context, clones mutable configuration, and initializes the next raw value. Native DataGridView traversal, validation, currency, and new-row `AddNew` remain authoritative.

## V1 boundaries

V1 is local and single-select. It does not provide remote paging, multiple selection, `DataTable` filtering, `BindingSource.Filter`, arbitrary cell templates, or public lookup cell/editing-control types. Use `BootstrapSelect` for managed multiple selection or remote-provider scenarios.
