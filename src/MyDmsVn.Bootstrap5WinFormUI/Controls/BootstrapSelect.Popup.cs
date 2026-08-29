using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

public partial class BootstrapSelect
{
    private BootstrapSelectDropDownController? _dropDownController;
    private bool _popupInputHandlersAttached;

    /// <summary>Occurs after the Select popup becomes visible.</summary>
    public event EventHandler? DropDownOpened;

    /// <summary>Occurs after the Select popup closes.</summary>
    public event EventHandler? DropDownClosed;

    internal bool IsDropDownCreatedForTest => _dropDownController?.IsCreated == true;
    internal bool IsDropDownOpenForTest => _dropDownController?.IsOpen == true;
    internal int DropDownCreationCountForTest => _dropDownController?.CreationCount ?? 0;
    internal Rectangle DropDownBoundsForTest => _dropDownController?.CurrentBounds ?? Rectangle.Empty;
    internal string? HighlightedResultTextForTest => _dropDownController?.Content?.HighlightedRow?.Text;

    internal IReadOnlyList<string> VisibleResultItemTextsForTest
    {
        get
        {
            var values = new List<string>();
            var rows = _dropDownController?.Content?.Rows;
            if (rows is null) return values.AsReadOnly();
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Kind == BootstrapSelectResultRowKind.Item && rows[i].Item is not null) values.Add(rows[i].Item!.Text);
            }
            return values.AsReadOnly();
        }
    }

    internal void OpenDropDownInternal()
    {
        if (IsDisposed || !Enabled || !Visible) return;
        _dropDownController ??= new BootstrapSelectDropDownController(this);
        _dropDownController.Open();
    }

    internal void CloseDropDownInternal(bool restoreFocus)
    {
        _dropDownController?.Close(restoreFocus);
    }

    internal void SetSearchTextForTest(string text)
    {
        _dropDownController ??= new BootstrapSelectDropDownController(this);
        _dropDownController.SetSearchText(text);
    }

    internal bool ActivateHighlightedResultForTest()
    {
        return _dropDownController?.ActivateHighlighted(BootstrapSelectChangeReason.Keyboard) == true;
    }

    internal BootstrapSelectResultSet BuildCurrentLocalResultSet(string searchText)
    {
        var effectiveText = SearchEnabled ? searchText : string.Empty;
        if (effectiveText.Length < MinimumSearchLength)
        {
            return BootstrapSelectResultSet.SingleMessage(BootstrapSelectResultRowKind.Instruction,
                "Type at least " + MinimumSearchLength + " character(s) to search.");
        }

        var result = BootstrapSelectResultBuilder.BuildLocal(Items, effectiveText, Matcher, IsItemSelected);
        result = BootstrapSelectResultBuilder.AppendCreateValue(result, Items, effectiveText, AllowCustomValues);
        return result.Rows.Count == 0
            ? BootstrapSelectResultSet.SingleMessage(BootstrapSelectResultRowKind.Empty, "No results found.")
            : result;
    }

    internal bool ActivateResultRow(BootstrapSelectResultRow row, BootstrapSelectChangeReason reason)
    {
        if (row is null) throw new ArgumentNullException(nameof(row));
        if (row.Kind == BootstrapSelectResultRowKind.Error || row.Kind == BootstrapSelectResultRowKind.LoadMoreError)
        {
            RetryRemoteLastFailure();
            return true;
        }
        if (row.Kind == BootstrapSelectResultRowKind.CreateValue)
        {
            if (!AllowCustomValues || row.CustomValueText is null || CustomValueFactory is null) return false;
            var item = CustomValueFactory(row.CustomValueText);
            return item is not null && SelectCore(item, BootstrapSelectChangeReason.CustomValue);
        }
        if (row.Kind != BootstrapSelectResultRowKind.Item || row.Item is null || row.Item.Disabled) return false;
        if (SelectionMode == BootstrapSelectMode.Multiple && row.IsSelected)
        {
            return DeselectCore(row.Item.Value, reason);
        }
        return SelectCore(row.Item, reason);
    }

    internal void NotifyDropDownOpened()
    {
        DropDownOpened?.Invoke(this, EventArgs.Empty);
    }

    internal void NotifyDropDownClosed()
    {
        InvalidateRemoteSearchOnClose();
        DropDownClosed?.Invoke(this, EventArgs.Empty);
    }

    internal void NotifyNearEndRequested()
    {
        RequestRemoteNextPage();
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (_popupInputHandlersAttached) return;
        _popupInputHandlersAttached = true;
        MouseDown += OnPopupSurfaceMouseDown;
        KeyDown += OnPopupSurfaceKeyDown;
        KeyPress += OnPopupSurfaceKeyPress;
        Disposed += OnPopupOwnerDisposed;
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        CloseDropDownInternal(false);
        base.OnHandleDestroyed(e);
    }

    /// <inheritdoc />
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) CloseDropDownInternal(false);
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (!Enabled) CloseDropDownInternal(false);
    }

    private bool IsItemSelected(BootstrapSelectItem item)
    {
        for (var i = 0; i < SelectedItems.Count; i++)
        {
            if (ValueComparer.Equals(SelectedItems[i].Value, item.Value)) return true;
        }
        return false;
    }

    private void OnPopupSurfaceMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var hit = HitTestSelectionSurface(e.Location);
        if (hit.Target == BootstrapSelectHitTarget.Content || hit.Target == BootstrapSelectHitTarget.Arrow || hit.Target == BootstrapSelectHitTarget.Chip)
        {
            OpenDropDownInternal();
        }
    }

    private void OnPopupSurfaceKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || !Enabled) return;
        var open = (e.Alt && e.KeyCode == Keys.Down) || e.KeyCode == Keys.F4 || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space;
        if (!open) return;
        OpenDropDownInternal();
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void OnPopupSurfaceKeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!Enabled || e.Handled || char.IsControl(e.KeyChar)) return;
        OpenDropDownInternal();
        _dropDownController?.ForwardCharacter(e.KeyChar);
        e.Handled = true;
    }

    private void OnPopupOwnerDisposed(object? sender, EventArgs e)
    {
        DisposeSearchInfrastructure();
        _dropDownController?.Dispose();
        _dropDownController = null;
    }
}
