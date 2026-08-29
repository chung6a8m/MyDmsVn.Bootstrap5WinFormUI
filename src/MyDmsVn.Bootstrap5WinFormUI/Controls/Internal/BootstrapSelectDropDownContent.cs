using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapSelectDropDownContent : UserControl
{
    private readonly TextBox _searchEditor;
    private readonly BootstrapSelectResultsView _resultsView;
    private bool _suppressSearchChanged;
    private int _dpi = DpiScaler.DefaultDpi;

    internal BootstrapSelectDropDownContent()
    {
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        _searchEditor = new TextBox
        {
            Dock = DockStyle.Top,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = Padding.Empty
        };
        _resultsView = new BootstrapSelectResultsView { Dock = DockStyle.Fill, Margin = Padding.Empty };
        Controls.Add(_resultsView);
        Controls.Add(_searchEditor);
        _searchEditor.TextChanged += OnSearchTextChanged;
        _searchEditor.KeyDown += OnSearchKeyDown;
        _resultsView.RowActivated += (row, reason) => RowActivated?.Invoke(row, reason);
        _resultsView.NearEndReached += () => NearEndRequested?.Invoke();
    }

    internal event Action<string>? SearchTextChanged;
    internal event Action<BootstrapSelectResultRow, BootstrapSelectChangeReason>? RowActivated;
    internal event Action? EscapeRequested;
    internal event Action? TabRequested;
    internal event Action? NearEndRequested;

    internal bool SearchEnabled
    {
        get => _searchEditor.Visible;
        set
        {
            _searchEditor.Visible = value;
            PerformLayout();
        }
    }

    internal string SearchText
    {
        get => _searchEditor.Text;
        set => _searchEditor.Text = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal IReadOnlyList<BootstrapSelectResultRow> Rows => _resultsView.Rows;
    internal BootstrapSelectResultRow? HighlightedRow => _resultsView.HighlightedRow;

    internal void ApplyPresentation(IBootstrapSelectRenderer renderer, BootstrapTheme theme, int dpi)
    {
        if (theme is null) throw new ArgumentNullException(nameof(theme));
        if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
        _dpi = dpi;
        BackColor = theme.Colors.Surface;
        ForeColor = theme.Colors.Text;
        _searchEditor.BackColor = theme.Colors.Surface;
        _searchEditor.ForeColor = theme.Colors.Text;
        _searchEditor.Font = Font;
        _resultsView.Font = Font;
        _resultsView.ApplyPresentation(renderer, theme, dpi);
        var height = DpiScaler.Scale(30, dpi);
        _searchEditor.MinimumSize = new Size(0, height);
        _searchEditor.Height = height;
    }

    internal void SetResults(BootstrapSelectResultSet results)
    {
        _resultsView.SetResults(results);
    }

    internal bool ActivateHighlighted(BootstrapSelectChangeReason reason)
    {
        return _resultsView.ActivateHighlighted(reason);
    }

    internal void FocusSearch()
    {
        if (_searchEditor.Visible)
        {
            _searchEditor.Focus();
            _searchEditor.SelectionStart = _searchEditor.TextLength;
        }
        else
        {
            _resultsView.Focus();
        }
    }

    internal void ForwardCharacter(char character)
    {
        if (!_searchEditor.Visible || char.IsControl(character)) return;
        _searchEditor.AppendText(character.ToString());
        _searchEditor.Focus();
    }

    internal void ClearSearchSilently()
    {
        _suppressSearchChanged = true;
        try
        {
            _searchEditor.Clear();
        }
        finally
        {
            _suppressSearchChanged = false;
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var results = _resultsView.GetPreferredSize(proposedSize);
        var searchHeight = _searchEditor.Visible ? Math.Max(_searchEditor.Height, DpiScaler.Scale(30, _dpi)) : 0;
        return new Size(Math.Max(160, proposedSize.Width), searchHeight + results.Height);
    }

    private void OnSearchTextChanged(object? sender, EventArgs e)
    {
        if (!_suppressSearchChanged) SearchTextChanged?.Invoke(_searchEditor.Text);
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && (e.KeyCode == Keys.A || e.KeyCode == Keys.C || e.KeyCode == Keys.V || e.KeyCode == Keys.X)) return;

        var handled = true;
        switch (e.KeyCode)
        {
            case Keys.Down:
                _resultsView.MoveHighlight(1);
                break;
            case Keys.Up:
                _resultsView.MoveHighlight(-1);
                break;
            case Keys.Home:
                _resultsView.MoveToFirst();
                break;
            case Keys.End:
                _resultsView.MoveToLast();
                break;
            case Keys.PageDown:
                _resultsView.Page(1);
                break;
            case Keys.PageUp:
                _resultsView.Page(-1);
                break;
            case Keys.Enter:
                _resultsView.ActivateHighlighted(BootstrapSelectChangeReason.Keyboard);
                break;
            case Keys.Escape:
                EscapeRequested?.Invoke();
                break;
            case Keys.Tab:
                TabRequested?.Invoke();
                break;
            default:
                handled = false;
                break;
        }

        if (handled)
        {
            e.Handled = true;
            if (e.KeyCode != Keys.Tab) e.SuppressKeyPress = true;
        }
    }
}
