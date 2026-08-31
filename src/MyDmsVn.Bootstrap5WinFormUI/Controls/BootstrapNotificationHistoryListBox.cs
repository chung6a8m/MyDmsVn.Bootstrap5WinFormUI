using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapNotificationHistoryItemActivatedEventArgs : EventArgs
{
    public BootstrapNotificationHistoryItemActivatedEventArgs(BootstrapToastHistoryItem item)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
    }

    public BootstrapToastHistoryItem Item { get; }
}

internal sealed class BootstrapNotificationHistoryListBox : ListBox
{
    private Font? _titleFont;
    private bool _themeSubscribed;

    public BootstrapNotificationHistoryListBox()
    {
        DrawMode = DrawMode.OwnerDrawVariable;
        IntegralHeight = false;
        BorderStyle = BorderStyle.None;
        HorizontalScrollbar = false;
        AccessibleRole = AccessibleRole.List;
        AccessibleName = "Notification history";
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
        RebuildTitleFont();
        ApplyTheme();
    }

    public event EventHandler<BootstrapNotificationHistoryItemActivatedEventArgs>? ItemActivated;

    public void SetHistory(IReadOnlyList<BootstrapToastHistoryItem> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        BeginUpdate();
        try
        {
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
        finally
        {
            EndUpdate();
        }

        RefreshItems();
        Invalidate();
    }

    internal void ProcessActivationKeyForTests(Keys key)
    {
        OnKeyDown(new KeyEventArgs(key));
    }

    protected override void OnMeasureItem(MeasureItemEventArgs e)
    {
        base.OnMeasureItem(e);
        if (e.Index < 0 || e.Index >= Items.Count || Items[e.Index] is not BootstrapToastHistoryItem item)
        {
            e.ItemHeight = Math.Max(1, Font.Height);
            return;
        }

        var metrics = CurrentMetrics();
        var textWidth = Math.Max(1, ClientSize.Width - (metrics.Padding * 2) - metrics.UnreadMarkerSize - metrics.ContentSpacing);
        var titleSize = string.IsNullOrEmpty(item.Title)
            ? Size.Empty
            : Measure(item.Title, _titleFont ?? Font, textWidth, singleLine: true);
        var bodySize = Measure(item.Text, Font, textWidth, singleLine: false);
        var timestampSize = Measure(FormatTimestamp(item), Font, textWidth, singleLine: true);
        e.ItemHeight = BootstrapNotificationCenterRenderLogic.CalculateRowHeight(
            Math.Max(1, ClientSize.Width),
            metrics,
            titleSize,
            bodySize,
            timestampSize,
            !string.IsNullOrEmpty(item.Title));
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count || Items[e.Index] is not BootstrapToastHistoryItem item)
        {
            return;
        }

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var palette = BootstrapNotificationCenterRenderLogic.ResolvePalette(
            BootstrapThemeManager.CurrentTheme.Colors,
            item.Variant,
            selected,
            item.IsRead);
        var metrics = CurrentMetrics();
        var textWidth = Math.Max(1, e.Bounds.Width - (metrics.Padding * 2) - metrics.UnreadMarkerSize - metrics.ContentSpacing);
        var titleSize = string.IsNullOrEmpty(item.Title)
            ? Size.Empty
            : Measure(item.Title, _titleFont ?? Font, textWidth, singleLine: true);
        var bodySize = Measure(item.Text, Font, textWidth, singleLine: false);
        var timestamp = FormatTimestamp(item);
        var timestampSize = Measure(timestamp, Font, textWidth, singleLine: true);
        var layout = BootstrapNotificationCenterRenderLogic.CalculateRowLayout(
            e.Bounds,
            metrics,
            titleSize,
            bodySize,
            timestampSize,
            !string.IsNullOrEmpty(item.Title));

        using (var background = new SolidBrush(palette.Surface))
        {
            e.Graphics.FillRectangle(background, e.Bounds);
        }

        if (!item.IsRead && layout.UnreadMarkerBounds.Width > 0)
        {
            using var marker = new SolidBrush(palette.Marker);
            e.Graphics.FillEllipse(marker, layout.UnreadMarkerBounds);
        }

        if (!layout.TitleBounds.IsEmpty)
        {
            TextRenderer.DrawText(e.Graphics, item.Title, _titleFont ?? Font, layout.TitleBounds, palette.Foreground,
                TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        if (!layout.BodyBounds.IsEmpty)
        {
            TextRenderer.DrawText(e.Graphics, item.Text, Font, layout.BodyBounds, palette.Foreground,
                TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        if (!layout.TimestampBounds.IsEmpty)
        {
            TextRenderer.DrawText(e.Graphics, timestamp, Font, layout.TimestampBounds, palette.Muted,
                TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        using (var border = new Pen(palette.Border))
        {
            e.Graphics.DrawLine(border, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        e.DrawFocusRectangle();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var index = IndexFromPoint(e.Location);
        if (index >= 0)
        {
            SelectedIndex = index;
            ActivateSelectedItem();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
        {
            ActivateSelectedItem();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        RebuildTitleFont();
        RefreshItems();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        RefreshItems();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            _titleFont?.Dispose();
            _titleFont = null;
        }

        base.Dispose(disposing);
    }

    private void ActivateSelectedItem()
    {
        if (SelectedItem is BootstrapToastHistoryItem item)
        {
            ItemActivated?.Invoke(this, new BootstrapNotificationHistoryItemActivatedEventArgs(item));
        }
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        ApplyTheme();
        RefreshItems();
        Invalidate();
    }

    private void ApplyTheme()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        BackColor = colors.Surface;
        ForeColor = colors.Text;
    }

    private void RebuildTitleFont()
    {
        _titleFont?.Dispose();
        _titleFont = new Font(Font, FontStyle.Bold);
    }

    private BootstrapNotificationCenterMetrics CurrentMetrics()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        return BootstrapNotificationCenterRenderLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, dpi);
    }

    private static Size Measure(string text, Font font, int width, bool singleLine)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Size.Empty;
        }

        var flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        flags |= singleLine ? TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis : TextFormatFlags.WordBreak;
        return TextRenderer.MeasureText(text, font, new Size(Math.Max(1, width), int.MaxValue), flags);
    }

    private static string FormatTimestamp(BootstrapToastHistoryItem item)
    {
        return item.CreatedAtUtc.ToLocalTime().ToString("g");
    }
}
