using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapSelectDropDownContent : UserControl
{
    private readonly Panel _searchHost;
    private readonly BootstrapSelectSearchTextBox _searchEditor;
    private readonly BootstrapSelectResultsView _resultsView;
    private bool _searchEnabled = true;
    private bool _suppressSearchChanged;

    internal BootstrapSelectDropDownContent()
    {
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        _searchHost = new Panel
        {
            Dock = DockStyle.Top,
            Height = 0,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _searchEditor = new BootstrapSelectSearchTextBox
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            ShowClearButton = false,
            BorderRadius = -1
        };
        _resultsView = new BootstrapSelectResultsView { Dock = DockStyle.Fill, Margin = Padding.Empty };
        _searchHost.Controls.Add(_searchEditor);
        Controls.Add(_resultsView);
        Controls.Add(_searchHost);
        _searchEditor.TextChanged += OnSearchTextChanged;
        _searchEditor.KeyDown += OnSearchKeyDown;
        _searchEditor.TabNavigationRequested += reverse => TabRequested?.Invoke(reverse);
        _resultsView.RowActivated += (row, reason) => RowActivated?.Invoke(row, reason);
        _resultsView.NearEndReached += () => NearEndRequested?.Invoke();
    }

    internal event Action<string>? SearchTextChanged;
    internal event Action<BootstrapSelectResultRow, BootstrapSelectChangeReason>? RowActivated;
    internal event Action? EscapeRequested;
    internal event Action<bool>? TabRequested;
    internal event Action? NearEndRequested;

    internal bool SearchEnabled
    {
        get => _searchEnabled;
        set
        {
            if (_searchEnabled == value)
            {
                return;
            }

            _searchEnabled = value;
            _searchHost.Visible = value;
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
    internal int ScrollOffset => _resultsView.ScrollOffset;

    internal void ApplyPresentation(IBootstrapSelectRenderer renderer, BootstrapTheme theme, int dpi)
    {
        if (theme is null) throw new ArgumentNullException(nameof(theme));
        if (dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi));
        BackColor = theme.Colors.Surface;
        ForeColor = theme.Colors.Text;
        var inset = DpiScaler.Scale(theme.Metrics.SpacingXS, dpi);
        var fieldHeight = DpiScaler.Scale(theme.Metrics.ControlHeightSmall, dpi);
        _searchHost.Padding = new Padding(inset);
        _searchHost.Height = fieldHeight + (inset * 2);
        _searchHost.BackColor = theme.Colors.Surface;
        _searchEditor.Font = Font;
        _searchEditor.Height = fieldHeight;
        _resultsView.Font = Font;
        _resultsView.ApplyPresentation(renderer, theme, dpi);
    }

    internal void SetResults(BootstrapSelectResultSet results)
    {
        _resultsView.SetResults(results);
    }

    internal void SetResults(
        BootstrapSelectResultSet results,
        BootstrapSelectResultsUpdateMode updateMode,
        IEqualityComparer<object> valueComparer)
    {
        _resultsView.SetResults(results, updateMode, valueComparer);
    }

    internal bool MoveHighlight(int delta)
    {
        return _resultsView.MoveHighlight(delta);
    }

    internal bool Page(int direction)
    {
        return _resultsView.Page(direction);
    }

    internal bool ActivateHighlighted(BootstrapSelectChangeReason reason)
    {
        return _resultsView.ActivateHighlighted(reason);
    }

    internal void FocusSearch()
    {
        if (_searchEnabled)
        {
            _searchEditor.FocusEditorAtEnd();
        }
        else
        {
            _resultsView.Focus();
        }
    }

    internal void ForwardCharacter(char character)
    {
        if (!_searchEnabled || char.IsControl(character)) return;
        _searchEditor.AppendCharacter(character);
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
        var searchHeight = _searchEnabled ? _searchHost.Height : 0;
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
            default:
                handled = false;
                break;
        }

        if (handled)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }
}
