using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired command dropdown anchored to a caller-owned <see cref="BootstrapButton"/>
/// while delegating popup focus, keyboard, dismissal, and placement behavior to native WinForms.
/// </summary>
[DefaultEvent(nameof(Opened))]
public class BootstrapDropdown : Component
{
    private readonly ToolStripDropDownMenu _dropDown;
    private readonly BootstrapDropdownRenderer _renderer;
    private readonly BootstrapDropdownItemCollection _items;
    private readonly List<Image> _ownedImages = new List<Image>();
    private BootstrapButton? _target;
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private int _minimumWidth;
    private bool _themeSubscribed;
    private bool _disposed;

    /// <summary>
    /// Initializes a designer-safe dropdown with an empty item collection and no target.
    /// </summary>
    public BootstrapDropdown()
    {
        _items = new BootstrapDropdownItemCollection();
        _renderer = new BootstrapDropdownRenderer();
        _dropDown = new ToolStripDropDownMenu
        {
            AutoClose = true,
            AutoSize = true,
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Renderer = _renderer
        };

        _dropDown.Opened += OnNativeOpened;
        _dropDown.Closed += OnNativeClosed;
        _dropDown.ItemClicked += OnNativeItemClicked;
        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _themeSubscribed = true;
    }

    /// <summary>
    /// Gets or sets the caller-owned button used to anchor and toggle the dropdown.
    /// </summary>
    [Category("Behavior")]
    [Description("Specifies the caller-owned BootstrapButton that anchors and toggles this command dropdown.")]
    [DefaultValue(null)]
    public BootstrapButton? Target
    {
        get => _target;
        set
        {
            ThrowIfDisposed();
            if (ReferenceEquals(_target, value))
            {
                return;
            }

            if (_dropDown.Visible)
            {
                _dropDown.Close();
            }

            if (_target is not null)
            {
                DetachTarget(_target);
            }

            _target = value;
            if (_target is not null && !_target.IsDisposed)
            {
                AttachTarget(_target);
            }
            else if (_target is not null && _target.IsDisposed)
            {
                _target = null;
            }
        }
    }

    /// <summary>
    /// Gets the stable caller-owned model collection snapshotted into native menu rows at each opening.
    /// </summary>
    [Category("Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapDropdownItemCollection Items => _items;

    /// <summary>
    /// Gets or sets the semantic accent used for selected and checked menu presentation.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects the semantic Bootstrap-inspired accent for selected and checked dropdown rows.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            BootstrapVariantColorResolver.Resolve(BootstrapThemeManager.CurrentTheme.Colors, value);
            if (_variant == value)
            {
                return;
            }

            _variant = value;
            _renderer.Variant = value;
            if (_dropDown.Visible)
            {
                _dropDown.Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the logical 96-DPI minimum popup width. Zero leaves native content sizing authoritative.
    /// </summary>
    [Category("Layout")]
    [Description("Sets a logical 96-DPI minimum popup width. Zero keeps native content measurement authoritative.")]
    [DefaultValue(0)]
    public int MinimumWidth
    {
        get => _minimumWidth;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Minimum width cannot be negative.");
            }

            if (_minimumWidth == value)
            {
                return;
            }

            _minimumWidth = value;
            if (_dropDown.Visible && _target is not null && !_target.IsDisposed)
            {
                _dropDown.MinimumSize = new Size(ResolveMinimumWidth(value, GetTargetDpi(_target)), 0);
            }
        }
    }

    /// <summary>
    /// Occurs after the owned native popup actually opens.
    /// </summary>
    public event EventHandler? Opened;

    /// <summary>
    /// Occurs after the owned native popup actually closes.
    /// </summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Opens the dropdown below <see cref="Target"/> when the target and item state permit it.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no target is assigned.</exception>
    public void Show()
    {
        ThrowIfDisposed();
        var target = _target ?? throw new InvalidOperationException("A BootstrapDropdown Target must be assigned before Show is called.");
        if (_dropDown.Visible || !CanOpen(target))
        {
            return;
        }

        ValidateItemTree(_items);
        RebuildNativeItems(target);
        _dropDown.Show(target, new Point(0, target.Height));
    }

    /// <summary>
    /// Closes the dropdown when it is currently visible. Calling this while closed is a no-op.
    /// </summary>
    public void Close()
    {
        if (_disposed)
        {
            return;
        }

        if (_dropDown.Visible)
        {
            _dropDown.Close();
        }
    }

    internal static bool CanActivate(BootstrapDropdownItem item)
    {
        return item.Kind == BootstrapDropdownItemKind.Item && item.Enabled;
    }

    internal static void ValidateItemTree(BootstrapDropdownItemCollection items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var visited = new HashSet<BootstrapDropdownItem>();
        ValidateItemLevel(items, visited);
    }

    internal void ActivateItem(BootstrapDropdownItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (CanActivate(item))
        {
            item.RaiseClick();
        }
    }

    internal static int ResolveMinimumWidth(int logicalMinimumWidth, int dpi)
    {
        if (logicalMinimumWidth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalMinimumWidth), logicalMinimumWidth, "Minimum width cannot be negative.");
        }

        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be greater than zero.");
        }

        return logicalMinimumWidth == 0 ? 0 : DpiScaler.Scale(logicalMinimumWidth, dpi);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            if (_dropDown.Visible)
            {
                _dropDown.Close();
            }

            if (_target is not null)
            {
                DetachTarget(_target);
                _target = null;
            }

            if (_themeSubscribed)
            {
                BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
                _themeSubscribed = false;
            }

            _dropDown.Opened -= OnNativeOpened;
            _dropDown.Closed -= OnNativeClosed;
            _dropDown.ItemClicked -= OnNativeItemClicked;
            ClearNativeItems();
            _dropDown.Dispose();
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    private bool CanOpen(BootstrapButton target)
    {
        return !target.IsDisposed && target.Enabled && !target.Loading && _items.Count > 0;
    }

    private static void ValidateItemLevel(
        BootstrapDropdownItemCollection items,
        HashSet<BootstrapDropdownItem> visited)
    {
        foreach (var item in items)
        {
            if (!visited.Add(item))
            {
                throw new InvalidOperationException(
                    "A dropdown item instance may appear only once in a dropdown tree.");
            }

            switch (item.Kind)
            {
                case BootstrapDropdownItemKind.Item:
                    if (item.HostedControlFactory is not null)
                    {
                        throw new InvalidOperationException(
                            "A normal dropdown item cannot define a hosted-control factory.");
                    }

                    ValidateItemLevel(item.DropDownItems, visited);
                    break;

                case BootstrapDropdownItemKind.Separator:
                    if (item.DropDownItems.Count > 0 || item.HostedControlFactory is not null)
                    {
                        throw new InvalidOperationException(
                            "A dropdown separator cannot contain child items or a hosted-control factory.");
                    }

                    break;

                case BootstrapDropdownItemKind.HostedControl:
                    if (item.DropDownItems.Count > 0 || item.HostedControlFactory is null)
                    {
                        throw new InvalidOperationException(
                            "A hosted-control item must define a factory and cannot contain child items.");
                    }

                    break;

                default:
                    throw new InvalidOperationException("Unsupported dropdown item kind.");
            }
        }
    }

    private void AttachTarget(BootstrapButton target)
    {
        target.Click += OnTargetClick;
        target.Disposed += OnTargetDisposed;
    }

    private void DetachTarget(BootstrapButton target)
    {
        target.Click -= OnTargetClick;
        target.Disposed -= OnTargetDisposed;
    }

    private void OnTargetClick(object? sender, EventArgs e)
    {
        var target = _target;
        if (target is null || !ReferenceEquals(sender, target) || target.IsDisposed || !target.Enabled || target.Loading)
        {
            return;
        }

        if (_dropDown.Visible)
        {
            Close();
        }
        else
        {
            Show();
        }
    }

    private void OnTargetDisposed(object? sender, EventArgs e)
    {
        var target = _target;
        if (target is null || !ReferenceEquals(sender, target))
        {
            return;
        }

        if (_dropDown.Visible)
        {
            _dropDown.Close();
        }

        DetachTarget(target);
        _target = null;
    }

    private void OnNativeOpened(object? sender, EventArgs e)
    {
        Opened?.Invoke(this, EventArgs.Empty);
    }

    private void OnNativeClosed(object? sender, ToolStripDropDownClosedEventArgs e)
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void OnNativeItemClicked(object? sender, ToolStripItemClickedEventArgs e)
    {
        if (e.ClickedItem?.Tag is BootstrapDropdownItem model)
        {
            ActivateItem(model);
        }
    }

    private void RebuildNativeItems(BootstrapButton target)
    {
        ClearNativeItems();

        _dropDown.ShowImageMargin = _items.Any(item =>
            item.Kind == BootstrapDropdownItemKind.Item && item.Icon is not null);
        _dropDown.ShowCheckMargin = _items.Any(item =>
            item.Kind == BootstrapDropdownItemKind.Item && item.Checked);

        foreach (var model in _items)
        {
            if (model.Kind == BootstrapDropdownItemKind.Separator)
            {
                _dropDown.Items.Add(new ToolStripSeparator());
                continue;
            }

            _dropDown.Items.Add(new ToolStripMenuItem(model.Text)
            {
                Enabled = model.Enabled,
                Checked = model.Checked,
                CheckOnClick = false,
                Tag = model,
                AutoSize = true
            });
        }

        ApplyPresentation(target, refreshImages: true);
    }

    private void ApplyPresentation(BootstrapButton target, bool refreshImages)
    {
        var dpi = GetTargetDpi(target);
        var theme = BootstrapThemeManager.CurrentTheme;
        var metrics = BootstrapDropdownRenderer.ResolveMetrics(theme.Metrics, dpi);

        _renderer.Variant = _variant;
        _dropDown.Font = target.Font;
        _dropDown.BackColor = theme.Colors.Surface;
        _dropDown.ForeColor = theme.Colors.Text;
        _dropDown.MinimumSize = new Size(ResolveMinimumWidth(_minimumWidth, dpi), 0);

        foreach (ToolStripItem nativeItem in _dropDown.Items)
        {
            if (nativeItem is ToolStripSeparator separator)
            {
                separator.Margin = new Padding(
                    metrics.SeparatorInset,
                    metrics.ItemVerticalPadding,
                    metrics.SeparatorInset,
                    metrics.ItemVerticalPadding);
                continue;
            }

            nativeItem.Padding = new Padding(
                metrics.ItemHorizontalPadding,
                metrics.ItemVerticalPadding,
                metrics.ItemHorizontalPadding,
                metrics.ItemVerticalPadding);
        }

        if (refreshImages)
        {
            RefreshOwnedImages(target, dpi, theme, metrics.ImageSize);
        }
    }

    private void RefreshOwnedImages(BootstrapButton target, int dpi, BootstrapTheme theme, int imageSize)
    {
        ReleaseOwnedImages();

        foreach (ToolStripItem nativeItem in _dropDown.Items)
        {
            if (nativeItem.Tag is not BootstrapDropdownItem model || model.Icon is null)
            {
                continue;
            }

            var color = model.Enabled ? theme.Colors.Text : theme.Colors.MutedText;
            var image = CreateMenuImage(target, model, dpi, imageSize, color);
            if (image is null)
            {
                continue;
            }

            nativeItem.Image = image;
            nativeItem.ImageScaling = ToolStripItemImageScaling.None;
        }
    }

    private Image? CreateMenuImage(BootstrapButton target, BootstrapDropdownItem model, int dpi, int imageSize, Color color)
    {
        if (model.Icon is null || imageSize <= 0)
        {
            return null;
        }

        var bitmap = new Bitmap(imageSize, imageSize);
        bitmap.SetResolution(dpi, dpi);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            if (!target.IconRenderer.TryRender(
                    graphics,
                    model.Icon,
                    new Rectangle(0, 0, imageSize, imageSize),
                    color))
            {
                bitmap.Dispose();
                return null;
            }
        }

        _ownedImages.Add(bitmap);
        return bitmap;
    }

    private void ReleaseOwnedImages()
    {
        foreach (ToolStripItem nativeItem in _dropDown.Items)
        {
            nativeItem.Image = null;
        }

        foreach (var image in _ownedImages)
        {
            image.Dispose();
        }

        _ownedImages.Clear();
    }

    private void ClearNativeItems()
    {
        ReleaseOwnedImages();

        while (_dropDown.Items.Count > 0)
        {
            var nativeItem = _dropDown.Items[0];
            _dropDown.Items.RemoveAt(0);
            nativeItem.Dispose();
        }
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        _renderer.Variant = _variant;
        if (_dropDown.Visible && _target is not null && !_target.IsDisposed)
        {
            ApplyPresentation(_target, refreshImages: true);
            _dropDown.Invalidate();
        }
    }

    private static int GetTargetDpi(BootstrapButton target)
    {
        return target.DeviceDpi > 0 ? target.DeviceDpi : DpiScaler.DefaultDpi;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BootstrapDropdown));
        }
    }
}
