using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired themed <see cref="DataGridView"/> while retaining native WinForms grid behavior.
/// </summary>
public class BootstrapDataGridView : DataGridView
{
    private readonly Panel _loadingOverlay = new Panel();
    private readonly BootstrapSpinner _loadingSpinner = new BootstrapSpinner();
    private readonly Label _loadingLabel = new Label();
    private string _emptyStateText = "No data to display.";
    private string _loadingText = "Loading...";
    private bool _loading;
    private bool _themeSubscribed;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private bool _initialized;
    private Font? _themeFont;

    /// <summary>
    /// Initializes a designer-safe Bootstrap-inspired data grid.
    /// </summary>
    public BootstrapDataGridView()
    {
        DoubleBuffered = true;
        EnableHeadersVisualStyles = false;
        AccessibleRole = AccessibleRole.Table;
        AccessibleDescription = "Bootstrap-inspired data grid.";

        _loadingOverlay.Visible = false;
        _loadingOverlay.TabStop = false;
        _loadingOverlay.AccessibleRole = AccessibleRole.Grouping;
        _loadingOverlay.AccessibleName = "Loading";

        _loadingSpinner.AutoSize = true;
        _loadingSpinner.SpinnerSize = BootstrapSpinnerSize.Default;
        _loadingSpinner.Variant = BootstrapVariant.Primary;
        _loadingSpinner.Spinning = false;
        _loadingSpinner.TabStop = false;

        _loadingLabel.AutoSize = true;
        _loadingLabel.Text = _loadingText;
        _loadingLabel.TextAlign = ContentAlignment.MiddleLeft;
        _loadingLabel.UseMnemonic = false;
        _loadingLabel.TabStop = false;
        _loadingLabel.AccessibleRole = AccessibleRole.StaticText;

        _loadingOverlay.Controls.Add(_loadingSpinner);
        _loadingOverlay.Controls.Add(_loadingLabel);
        Controls.Add(_loadingOverlay);

        _initialized = true;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        ApplyThemeFont();
        ApplyTheme();
        LayoutLoadingOverlay();
    }

    /// <summary>
    /// Gets or sets the message shown when the grid has no data rows.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies the message shown when the grid contains no data rows.")]
    [DefaultValue("No data to display.")]
    public string EmptyStateText
    {
        get => _emptyStateText;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_emptyStateText, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _emptyStateText = normalized;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the loading overlay is visible.
    /// </summary>
    [Category("Behavior")]
    [Description("Shows the framework loading overlay without changing the grid data source or enabled state.")]
    [DefaultValue(false)]
    public bool Loading
    {
        get => _loading;
        set
        {
            if (_loading == value)
            {
                return;
            }

            _loading = value;
            _loadingOverlay.Visible = value;
            _loadingSpinner.Spinning = value;
            if (value)
            {
                LayoutLoadingOverlay();
                _loadingOverlay.BringToFront();
            }

            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the text shown beside the loading spinner.
    /// </summary>
    [Category("Appearance")]
    [Description("Specifies the text shown beside the loading spinner.")]
    [DefaultValue("Loading...")]
    public string LoadingText
    {
        get => _loadingText;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_loadingText, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _loadingText = normalized;
            _loadingLabel.Text = normalized;
            LayoutLoadingOverlay();
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!_initialized)
        {
            return;
        }

        if (!_settingThemeFont)
        {
            _useThemeFont = false;
            DisposeThemeFont();
        }

        _loadingLabel.Font = Font;
        LayoutLoadingOverlay();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        LayoutLoadingOverlay();
        if (_loading)
        {
            _loadingOverlay.BringToFront();
        }
    }

    /// <inheritdoc />
    protected override void OnRowsAdded(DataGridViewRowsAddedEventArgs e)
    {
        base.OnRowsAdded(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnRowsRemoved(DataGridViewRowsRemovedEventArgs e)
    {
        base.OnRowsRemoved(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDataSourceChanged(EventArgs e)
    {
        base.OnDataSourceChanged(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        LayoutLoadingOverlay();
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_loading || _emptyStateText.Length == 0 || HasDataRows())
        {
            return;
        }

        var bounds = GetEmptyStateBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        TextRenderer.DrawText(
            e.Graphics,
            _emptyStateText,
            Font,
            bounds,
            BootstrapThemeManager.CurrentTheme.Colors.MutedText,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.WordBreak |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            DisposeThemeFont();
        }

        base.Dispose(disposing);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (_useThemeFont)
        {
            ApplyThemeFont();
        }

        ApplyTheme();
        LayoutLoadingOverlay();
        Invalidate();
    }

    private void ApplyTheme()
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var colors = theme.Colors;
        var selectionText = ColorUtil.GetContrastingTextColor(colors.Primary, colors.Light, colors.Dark);

        BackgroundColor = colors.Surface;
        BackColor = colors.Surface;
        ForeColor = colors.Text;
        GridColor = colors.Border;
        BorderStyle = BorderStyle.FixedSingle;
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

        ApplyCellStyle(DefaultCellStyle, colors.Surface, colors.Text, colors.Primary, selectionText);
        ApplyCellStyle(RowsDefaultCellStyle, colors.Surface, colors.Text, colors.Primary, selectionText);
        ApplyCellStyle(AlternatingRowsDefaultCellStyle, colors.SurfaceSecondary, colors.Text, colors.Primary, selectionText);
        ApplyCellStyle(ColumnHeadersDefaultCellStyle, colors.SurfaceSecondary, colors.Text, colors.SurfaceSecondary, colors.Text);
        ApplyCellStyle(RowHeadersDefaultCellStyle, colors.SurfaceSecondary, colors.Text, colors.SurfaceSecondary, colors.Text);

        _loadingOverlay.BackColor = colors.Surface;
        _loadingOverlay.ForeColor = colors.Text;
        _loadingLabel.BackColor = colors.Surface;
        _loadingLabel.ForeColor = colors.MutedText;
        _loadingSpinner.BackColor = Color.Transparent;
        _loadingSpinner.Variant = BootstrapVariant.Primary;
        _loadingSpinner.Invalidate();
    }

    private static void ApplyCellStyle(
        DataGridViewCellStyle style,
        Color backColor,
        Color foreColor,
        Color selectionBackColor,
        Color selectionForeColor)
    {
        style.BackColor = backColor;
        style.ForeColor = foreColor;
        style.SelectionBackColor = selectionBackColor;
        style.SelectionForeColor = selectionForeColor;
    }

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        var nextFont = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _themeFont = nextFont;
        _settingThemeFont = true;
        try
        {
            Font = nextFont;
        }
        finally
        {
            _settingThemeFont = false;
        }

        previous?.Dispose();
        _loadingLabel.Font = Font;
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private bool HasDataRows()
    {
        var count = Rows.Count;
        if (NewRowIndex >= 0 && NewRowIndex < count)
        {
            count--;
        }

        return count > 0;
    }

    private Rectangle GetEmptyStateBounds()
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var inset = DpiScaler.Scale(theme.Metrics.SpacingSM, dpi);
        var left = RowHeadersVisible ? RowHeadersWidth : 0;
        var top = ColumnHeadersVisible ? ColumnHeadersHeight : 0;
        return new Rectangle(
            left + inset,
            top + inset,
            Math.Max(0, ClientSize.Width - left - (inset * 2)),
            Math.Max(0, ClientSize.Height - top - (inset * 2)));
    }

    private void LayoutLoadingOverlay()
    {
        if (_loadingOverlay.IsDisposed)
        {
            return;
        }

        _loadingOverlay.Bounds = ClientRectangle;
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        var spacing = _loadingText.Length > 0 ? DpiScaler.Scale(theme.Metrics.SpacingSM, dpi) : 0;
        var spinnerSize = _loadingSpinner.GetPreferredSize(Size.Empty);
        _loadingSpinner.Size = spinnerSize;
        var labelSize = _loadingText.Length > 0
            ? _loadingLabel.GetPreferredSize(Size.Empty)
            : Size.Empty;
        _loadingLabel.Visible = _loadingText.Length > 0;

        var totalWidth = spinnerSize.Width + spacing + labelSize.Width;
        var contentHeight = Math.Max(spinnerSize.Height, labelSize.Height);
        var left = Math.Max(0, (ClientSize.Width - totalWidth) / 2);
        var top = Math.Max(0, (ClientSize.Height - contentHeight) / 2);

        _loadingSpinner.Location = new Point(left, top + Math.Max(0, (contentHeight - spinnerSize.Height) / 2));
        _loadingLabel.Location = new Point(
            left + spinnerSize.Width + spacing,
            top + Math.Max(0, (contentHeight - labelSize.Height) / 2));
    }
}
