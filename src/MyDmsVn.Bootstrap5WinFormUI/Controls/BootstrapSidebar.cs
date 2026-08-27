using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Icons;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides a Bootstrap-inspired application navigation sidebar.
/// </summary>
[DefaultProperty(nameof(Items))]
public class BootstrapSidebar : Panel
{
    private static readonly TimeSpan DefaultAnimationDuration = TimeSpan.FromMilliseconds(200);
    private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();

    private readonly BindingList<BootstrapSidebarItem> _items = new BindingList<BootstrapSidebarItem>();
    private IIconRenderer _iconRenderer = DefaultIconRenderer;
    private int _expandedWidth = 260;
    private int _collapsedWidth = 72;
    private bool _expanded = true;
    private TimeSpan _animationDuration = DefaultAnimationDuration;
    private BootstrapSidebarItem? _selectedItem;

    /// <summary>
    /// Initializes a designer-safe expanded sidebar.
    /// </summary>
    public BootstrapSidebar()
    {
        TabStop = false;
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleDescription = "Application navigation sidebar.";
        Size = new Size(_expandedWidth, 480);
    }

    /// <summary>
    /// Gets or sets the logical width used while expanded.
    /// </summary>
    [Category("Layout")]
    [DefaultValue(260)]
    public int ExpandedWidth
    {
        get => _expandedWidth;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Expanded width must be positive.");
            }

            _expandedWidth = value;
            if (_expanded)
            {
                Width = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the logical width used while collapsed.
    /// </summary>
    [Category("Layout")]
    [DefaultValue(72)]
    public int CollapsedWidth
    {
        get => _collapsedWidth;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Collapsed width must be positive.");
            }

            _collapsedWidth = value;
            if (!_expanded)
            {
                Width = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the sidebar is expanded.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(true)]
    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value)
            {
                return;
            }

            _expanded = value;
            Width = _expanded ? _expandedWidth : _collapsedWidth;
            ExpandedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets or sets the currently selected navigation item.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BootstrapSidebarItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (ReferenceEquals(_selectedItem, value))
            {
                return;
            }

            if (_selectedItem is not null)
            {
                _selectedItem.Selected = false;
            }

            _selectedItem = value;
            if (_selectedItem is not null)
            {
                _selectedItem.Selected = true;
            }

            SelectedItemChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the root navigation items.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BindingList<BootstrapSidebarItem> Items => _items;

    /// <summary>
    /// Gets or sets the full sidebar width transition duration.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue(typeof(TimeSpan), "00:00:00.2000000")]
    public TimeSpan AnimationDuration
    {
        get => _animationDuration;
        set
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Animation duration must be greater than zero.");
            }

            _animationDuration = value;
        }
    }

    /// <summary>
    /// Gets or sets the source-neutral icon renderer used by navigation items.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IIconRenderer IconRenderer
    {
        get => _iconRenderer;
        set => _iconRenderer = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Occurs when the requested expanded state changes.
    /// </summary>
    public event EventHandler? ExpandedChanged;

    /// <summary>
    /// Occurs when the selected navigation item changes.
    /// </summary>
    public event EventHandler? SelectedItemChanged;

    /// <summary>
    /// Expands the sidebar.
    /// </summary>
    public void Expand()
    {
        Expanded = true;
    }

    /// <summary>
    /// Collapses the sidebar.
    /// </summary>
    public void Collapse()
    {
        Expanded = false;
    }

    /// <summary>
    /// Toggles the expanded state.
    /// </summary>
    public void Toggle()
    {
        Expanded = !Expanded;
    }
}
