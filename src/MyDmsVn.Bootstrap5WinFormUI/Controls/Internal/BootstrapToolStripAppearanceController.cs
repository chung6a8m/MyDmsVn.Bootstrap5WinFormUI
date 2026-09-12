using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal sealed class BootstrapToolStripAppearanceController : IDisposable
{
    private readonly ToolStrip _owner;
    private readonly BootstrapToolStripRenderer _renderer;
    private readonly HashSet<ToolStripDropDownItem> _wiredItems = new HashSet<ToolStripDropDownItem>();
    private readonly HashSet<ToolStripDropDown> _trackedDropDowns = new HashSet<ToolStripDropDown>();
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private Font? _themeFont;
    private bool _settingThemeFont;
    private bool _useThemeFont = true;
    private bool _disposed;

    public BootstrapToolStripAppearanceController(ToolStrip owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _renderer = new BootstrapToolStripRenderer();
        _owner.Renderer = _renderer;
        _owner.FontChanged += OnFontChanged;
        _owner.ItemAdded += OnItemAdded;
        _owner.ItemRemoved += OnItemRemoved;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyThemeFont();
        ApplyThemeColors();
        WireItems(_owner.Items);
    }

    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapVariantColorResolver.Resolve(BootstrapThemeManager.CurrentTheme.Colors, value);
            if (_variant == value) return;
            _variant = value;
            _renderer.Variant = value;
            InvalidateBootstrapSurfaces();
        }
    }

    internal BootstrapToolStripRenderer Renderer => _renderer;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        _owner.FontChanged -= OnFontChanged;
        _owner.ItemAdded -= OnItemAdded;
        _owner.ItemRemoved -= OnItemRemoved;
        foreach (var item in new List<ToolStripDropDownItem>(_wiredItems)) UnwireItem(item);
        foreach (var surface in new List<ToolStripDropDown>(_trackedDropDowns)) UntrackDropDown(surface);
        DisposeThemeFont();
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (_disposed || _owner.IsDisposed || _owner.Disposing) return;
        if (_useThemeFont) ApplyThemeFont();
        ApplyThemeColors();
        InvalidateBootstrapSurfaces();
    }

    private void OnFontChanged(object? sender, EventArgs e)
    {
        if (_settingThemeFont || _disposed) return;
        _useThemeFont = false;
        DisposeThemeFont();
    }

    private void OnItemAdded(object? sender, ToolStripItemEventArgs e)
    {
        if (e.Item is ToolStripDropDownItem item) WireItem(item);
    }

    private void OnItemRemoved(object? sender, ToolStripItemEventArgs e)
    {
        if (e.Item is ToolStripDropDownItem item) UnwireItem(item);
    }

    private void WireItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            if (item is ToolStripDropDownItem dropDownItem) WireItem(dropDownItem);
        }
    }

    private void WireItem(ToolStripDropDownItem item)
    {
        if (!_wiredItems.Add(item)) return;
        item.DropDownOpening += OnDropDownOpening;
        item.DropDownClosed += OnDropDownClosed;
        item.Disposed += OnDropDownItemDisposed;
    }

    private void UnwireItem(ToolStripDropDownItem item)
    {
        if (!_wiredItems.Remove(item)) return;
        item.DropDownOpening -= OnDropDownOpening;
        item.DropDownClosed -= OnDropDownClosed;
        item.Disposed -= OnDropDownItemDisposed;
    }

    private void OnDropDownOpening(object? sender, EventArgs e)
    {
        if (_disposed || sender is not ToolStripDropDownItem item) return;
        var dropDown = item.DropDown;
        TrackDropDown(dropDown);
        WireItems(dropDown.Items);
        if (ReferenceEquals(_owner.Renderer, _renderer) && ReferenceEquals(dropDown.Renderer, _renderer))
        {
            dropDown.Invalidate();
        }
    }

    private void OnDropDownClosed(object? sender, EventArgs e)
    {
        if (sender is ToolStripDropDownItem item && item.HasDropDownItems)
        {
            UntrackDropDown(item.DropDown);
        }
    }

    private void OnDropDownItemDisposed(object? sender, EventArgs e)
    {
        if (sender is ToolStripDropDownItem item) UnwireItem(item);
    }

    private void TrackDropDown(ToolStripDropDown surface)
    {
        if (!_trackedDropDowns.Add(surface)) return;
        surface.ItemAdded += OnTrackedItemAdded;
        surface.ItemRemoved += OnTrackedItemRemoved;
        surface.Closed += OnTrackedDropDownClosed;
        surface.Disposed += OnTrackedDropDownDisposed;
    }

    private void UntrackDropDown(ToolStripDropDown surface)
    {
        if (!_trackedDropDowns.Remove(surface)) return;
        surface.ItemAdded -= OnTrackedItemAdded;
        surface.ItemRemoved -= OnTrackedItemRemoved;
        surface.Closed -= OnTrackedDropDownClosed;
        surface.Disposed -= OnTrackedDropDownDisposed;
        foreach (ToolStripItem item in surface.Items)
        {
            if (item is ToolStripDropDownItem child) UnwireItem(child);
        }
    }

    private void OnTrackedItemAdded(object? sender, ToolStripItemEventArgs e)
    {
        if (e.Item is ToolStripDropDownItem item) WireItem(item);
    }

    private void OnTrackedItemRemoved(object? sender, ToolStripItemEventArgs e)
    {
        if (e.Item is ToolStripDropDownItem item) UnwireItem(item);
    }

    private void OnTrackedDropDownClosed(object? sender, ToolStripDropDownClosedEventArgs e)
    {
        if (sender is ToolStripDropDown dropDown) UntrackDropDown(dropDown);
    }

    private void OnTrackedDropDownDisposed(object? sender, EventArgs e)
    {
        if (sender is ToolStripDropDown dropDown) UntrackDropDown(dropDown);
    }

    private void ApplyThemeColors()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        _owner.BackColor = GetSurfaceKind() == BootstrapToolStripSurfaceKind.ToolBar || GetSurfaceKind() == BootstrapToolStripSurfaceKind.StatusBar
            ? colors.SurfaceSecondary
            : colors.Surface;
        _owner.ForeColor = colors.Text;
    }

    private BootstrapToolStripSurfaceKind GetSurfaceKind() => BootstrapToolStripRendererBase.GetSurfaceKind(_owner);

    private void ApplyThemeFont()
    {
        var token = BootstrapThemeManager.CurrentTheme.Typography.Body;
        if (_themeFont is not null && string.Equals(_themeFont.Name, token.FontFamilyName, StringComparison.OrdinalIgnoreCase) && Math.Abs(_themeFont.SizeInPoints - token.SizeInPoints) < 0.01f && _themeFont.Style == token.Style) return;
        var next = new Font(token.FontFamilyName, token.SizeInPoints, token.Style);
        var previous = _themeFont;
        _themeFont = next;
        _settingThemeFont = true;
        try { _owner.Font = next; }
        catch { _themeFont = previous; next.Dispose(); throw; }
        finally { _settingThemeFont = false; }
        previous?.Dispose();
    }

    private void DisposeThemeFont()
    {
        var font = _themeFont;
        _themeFont = null;
        font?.Dispose();
    }

    private void InvalidateBootstrapSurfaces()
    {
        if (!ReferenceEquals(_owner.Renderer, _renderer)) return;
        _owner.Invalidate();
        foreach (var surface in new List<ToolStripDropDown>(_trackedDropDowns))
        {
            if (surface.IsDisposed) UntrackDropDown(surface);
            else if (ReferenceEquals(surface.Renderer, _renderer)) surface.Invalidate();
        }
    }
}
