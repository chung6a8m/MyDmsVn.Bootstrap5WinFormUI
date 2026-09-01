using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides separate primary-command and dropdown-menu button regions with connected Bootstrap styling.
/// The child regions are framework-owned implementation details even though WinForms exposes them through
/// the inherited <see cref="Control.Controls"/> collection.
/// </summary>
[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Click))]
public class BootstrapSplitButton : Control, IBootstrapConnectedControl
{
    private static readonly IIconRenderer DefaultIconRenderer = BootstrapIconRenderer.CreateDefault();

    private readonly SplitRegionButton _primaryButton;
    private readonly SplitRegionButton _dropDownButton;
    private readonly BootstrapDropdown _dropdown;
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private BootstrapButtonSize _buttonSize = BootstrapButtonSize.Default;
    private BootstrapIconPosition _iconPosition = BootstrapIconPosition.Left;
    private IIconRenderer _iconRenderer = DefaultIconRenderer;
    private IconDescriptor? _icon;
    private bool _outline;
    private bool _loading;
    private string _loadingText = string.Empty;
    private int _borderRadius = -1;
    private CornerRadius? _connectedCornerRadius;
    private BootstrapConnectedControlSize? _connectedSizeOverride;
    private int _minimumWidth;
    private bool _initialized;
    private bool _performingLayout;
    private bool _synchronizingFont;
    private bool _callerCustomFont;
    private bool _dropDownOpen;
    private bool _disposed;

    /// <summary>
    /// Initializes a designer-safe split button with separate focusable command and menu regions.
    /// </summary>
    public BootstrapSplitButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        AutoSize = true;
        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleDescription = "Split command button with an additional commands menu.";

        _dropdown = new BootstrapDropdown();
        _primaryButton = new SplitRegionButton(this, isMenuRegion: false)
        {
            AutoSize = false,
            TabStop = true
        };
        _dropDownButton = new SplitRegionButton(this, isMenuRegion: true)
        {
            AutoSize = false,
            TabStop = true,
            Text = string.Empty,
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.ChevronDown)
        };

        _primaryButton.Click += OnPrimaryButtonClick;
        _primaryButton.FontChanged += OnPrimaryButtonFontChanged;
        _dropDownButton.Click += OnDropDownButtonClick;
        _dropdown.Opened += OnDropdownOpened;
        _dropdown.Closed += OnDropdownClosed;

        Controls.Add(_primaryButton);
        Controls.Add(_dropDownButton);
        SynchronizeAppearance();
        _initialized = true;
        MirrorPrimaryFont();
        Size = GetPreferredSize(Size.Empty);
    }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.AllowNull]
#endif
    public override string Text
    {
        get => base.Text;
        set => base.Text = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the semantic Bootstrap-inspired color variant for both regions and the dropdown.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            _primaryButton.Variant = value;
            _dropDownButton.Variant = value;
            _dropdown.Variant = value;
            _variant = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether both regions use outline presentation.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(false)]
    public bool Outline
    {
        get => _outline;
        set
        {
            if (_outline == value)
            {
                return;
            }

            _outline = value;
            _primaryButton.Outline = value;
            _dropDownButton.Outline = value;
        }
    }

    /// <summary>
    /// Gets or sets the standard size applied to both button regions.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(BootstrapButtonSize.Default)]
    public BootstrapButtonSize ButtonSize
    {
        get => _buttonSize;
        set
        {
            _primaryButton.ButtonSize = value;
            _dropDownButton.ButtonSize = value;
            _buttonSize = value;
            ApplyPreferredSize();
            PerformLayout();
        }
    }

    /// <summary>
    /// Gets or sets the optional content icon displayed only in the primary region.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(null)]
    public IconDescriptor? Icon
    {
        get => _icon;
        set
        {
            if (ReferenceEquals(_icon, value))
            {
                return;
            }

            _icon = value;
            _primaryButton.Icon = value;
            ApplyPreferredSize();
        }
    }

    /// <summary>
    /// Gets or sets primary-region icon placement.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(BootstrapIconPosition.Left)]
    public BootstrapIconPosition IconPosition
    {
        get => _iconPosition;
        set
        {
            _primaryButton.IconPosition = value;
            _iconPosition = value;
        }
    }

    /// <summary>
    /// Gets or sets the source-neutral icon renderer used by both regions and dropdown items.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IIconRenderer IconRenderer
    {
        get => _iconRenderer;
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _iconRenderer = value;
            _primaryButton.IconRenderer = value;
            _dropDownButton.IconRenderer = value;
        }
    }

    /// <summary>
    /// Gets or sets the logical radius for the connected outer corners, or -1 for theme radius.
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(-1)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Border radius must be -1 or non-negative.");
            }

            if (_borderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            _primaryButton.BorderRadius = value;
            _dropDownButton.BorderRadius = value;
            ApplyConnectedCorners();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets whether the primary region shows spinner-backed loading and all activation is suppressed.
    /// </summary>
    [Category("Behavior")]
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

            if (value)
            {
                CloseDropDown();
            }

            _loading = value;
            _primaryButton.Loading = value;
            UpdateRegionEnabledState();
            ApplyPreferredSize();
        }
    }

    /// <summary>
    /// Gets or sets optional primary-region text displayed while loading.
    /// </summary>
    [Category("Behavior")]
    [DefaultValue("")]
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
            _primaryButton.LoadingText = normalized;
            ApplyPreferredSize();
        }
    }

    /// <summary>
    /// Gets the stable caller-owned dropdown item collection.
    /// </summary>
    [Category("Data")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public BootstrapDropdownItemCollection Items => _dropdown.Items;

    /// <summary>
    /// Gets or sets the logical 96-DPI minimum dropdown width.
    /// </summary>
    [Category("Layout")]
    [DefaultValue(0)]
    public int MinimumWidth
    {
        get => _minimumWidth;
        set
        {
            _dropdown.MinimumWidth = value;
            _minimumWidth = value;
        }
    }

    /// <summary>
    /// Occurs after the owned native dropdown opens.
    /// </summary>
    public event EventHandler? Opened;

    /// <summary>
    /// Occurs after the owned native dropdown closes.
    /// </summary>
    public event EventHandler? Closed;

    CornerRadius? IBootstrapConnectedControl.ConnectedCornerRadius
    {
        get => _connectedCornerRadius;
        set
        {
            _connectedCornerRadius = value;
            ApplyConnectedCorners();
            Invalidate();
        }
    }

    BootstrapConnectedControlSize? IBootstrapConnectedControl.ConnectedSizeOverride
    {
        get => _connectedSizeOverride;
        set
        {
            _connectedSizeOverride = value;
            ((IBootstrapConnectedControl)_primaryButton).ConnectedSizeOverride = value;
            ((IBootstrapConnectedControl)_dropDownButton).ConnectedSizeOverride = value;
            ApplyPreferredSize();
            PerformLayout();
        }
    }

    int IBootstrapConnectedControl.GetConnectedSafeMinimumHeight(BootstrapConnectedControlSize size, int dpi)
    {
        var primary = ((IBootstrapConnectedControl)_primaryButton).GetConnectedSafeMinimumHeight(size, dpi);
        var menu = ((IBootstrapConnectedControl)_dropDownButton).GetConnectedSafeMinimumHeight(size, dpi);
        return Math.Max(primary, menu);
    }

    /// <summary>
    /// Requests opening the dropdown when current state permits it.
    /// </summary>
    public void ShowDropDown()
    {
        if (_disposed || IsDisposed || Disposing || !Enabled || _loading || Items.Count == 0 || _dropDownOpen)
        {
            return;
        }

        _dropdown.ShowFrom(_primaryButton, ResolveDropDownAnchor(), ResolveDropDownLocation());
    }

    /// <summary>
    /// Closes the dropdown when it is open.
    /// </summary>
    public void CloseDropDown()
    {
        _dropdown.Close();
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        if (!_initialized)
        {
            return base.GetPreferredSize(proposedSize);
        }

        var primarySize = _primaryButton.GetPreferredSize(Size.Empty);
        var dropDownSize = _dropDownButton.GetPreferredSize(Size.Empty);
        var overlap = ResolveSeamOverlap();
        return new Size(
            Math.Max(1, primarySize.Width + dropDownSize.Width - overlap),
            Math.Max(1, Math.Max(primarySize.Height, dropDownSize.Height)));
    }

    /// <inheritdoc />
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if (_initialized)
        {
            _primaryButton.Text = Text;
            ApplyPreferredSize();
        }
    }

    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!_initialized || _synchronizingFont)
        {
            return;
        }

        _callerCustomFont = true;
        _synchronizingFont = true;
        try
        {
            _primaryButton.Font = Font;
            _dropDownButton.Font = Font;
        }
        finally
        {
            _synchronizingFont = false;
        }

        ApplyPreferredSize();
        PerformLayout();
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (_initialized)
        {
            if (!Enabled)
            {
                CloseDropDown();
            }

            UpdateRegionEnabledState();
        }
    }

    /// <inheritdoc />
    protected override void OnAutoSizeChanged(EventArgs e)
    {
        base.OnAutoSizeChanged(e);
        ApplyPreferredSize();
    }

    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ApplyConnectedCorners();
        ApplyPreferredSize();
        PerformLayout();
    }

    /// <inheritdoc />
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        if (!_initialized || _performingLayout || IsDisposed)
        {
            return;
        }

        _performingLayout = true;
        try
        {
            LayoutRegions();
        }
        finally
        {
            _performingLayout = false;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            CloseDropDown();
            _primaryButton.Click -= OnPrimaryButtonClick;
            _primaryButton.FontChanged -= OnPrimaryButtonFontChanged;
            _dropDownButton.Click -= OnDropDownButtonClick;
            _dropdown.Opened -= OnDropdownOpened;
            _dropdown.Closed -= OnDropdownClosed;
            _dropdown.Dispose();
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    private void SynchronizeAppearance()
    {
        _primaryButton.Text = Text;
        _primaryButton.Variant = _variant;
        _dropDownButton.Variant = _variant;
        _dropdown.Variant = _variant;
        _primaryButton.Outline = _outline;
        _dropDownButton.Outline = _outline;
        _primaryButton.ButtonSize = _buttonSize;
        _dropDownButton.ButtonSize = _buttonSize;
        _primaryButton.Icon = _icon;
        _primaryButton.IconPosition = _iconPosition;
        _primaryButton.IconRenderer = _iconRenderer;
        _dropDownButton.IconRenderer = _iconRenderer;
        _primaryButton.BorderRadius = _borderRadius;
        _dropDownButton.BorderRadius = _borderRadius;
        _primaryButton.Loading = _loading;
        _primaryButton.LoadingText = _loadingText;
        _dropdown.MinimumWidth = _minimumWidth;
        UpdateRegionEnabledState();
        ApplyConnectedCorners();
    }

    private void LayoutRegions()
    {
        ApplyConnectedCorners();
        var overlap = ResolveSeamOverlap();
        var preferredDropDownWidth = _dropDownButton.GetPreferredSize(Size.Empty).Width;
        var dropDownWidth = Math.Min(Math.Max(0, ClientSize.Width), Math.Max(0, preferredDropDownWidth));
        var primaryWidth = Math.Max(0, ClientSize.Width - dropDownWidth + Math.Min(overlap, dropDownWidth));
        var dropDownLeft = Math.Max(0, primaryWidth - Math.Min(overlap, dropDownWidth));
        var height = Math.Max(0, ClientSize.Height);
        _primaryButton.SetBounds(0, 0, primaryWidth, height);
        _dropDownButton.SetBounds(dropDownLeft, 0, dropDownWidth, height);
    }

    private void ApplyConnectedCorners()
    {
        if (_connectedCornerRadius.HasValue)
        {
            var outer = _connectedCornerRadius.Value;
            _primaryButton.GroupCornerRadius = new CornerRadius(outer.TopLeft, 0f, 0f, outer.BottomLeft);
            _dropDownButton.GroupCornerRadius = new CornerRadius(0f, outer.TopRight, outer.BottomRight, 0f);
            return;
        }

        var radius = _borderRadius >= 0
            ? _borderRadius
            : BootstrapButtonRenderLogic.GetThemeBorderRadius(
                BootstrapThemeManager.CurrentTheme.Metrics,
                _buttonSize);
        _primaryButton.GroupCornerRadius = BootstrapConnectedControlLayoutLogic.ResolveCornerRadius(
            Orientation.Horizontal,
            0,
            2,
            radius);
        _dropDownButton.GroupCornerRadius = BootstrapConnectedControlLayoutLogic.ResolveCornerRadius(
            Orientation.Horizontal,
            1,
            2,
            radius);
    }

    private int ResolveSeamOverlap()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : DpiScaler.DefaultDpi;
        return BootstrapConnectedControlLayoutLogic.ResolveSeamOverlap(
            BootstrapThemeManager.CurrentTheme.Metrics,
            dpi);
    }

    private void ApplyPreferredSize()
    {
        if (!_initialized || !AutoSize || IsDisposed)
        {
            return;
        }

        var preferred = GetPreferredSize(Size.Empty);
        if (Size != preferred)
        {
            Size = preferred;
        }
    }

    private void UpdateRegionEnabledState()
    {
        _primaryButton.Enabled = Enabled;
        _dropDownButton.Enabled = Enabled && !_loading;
    }

    private Control ResolveDropDownAnchor()
    {
        return this;
    }

    private Point ResolveDropDownLocation()
    {
        return new Point(0, Height);
    }

    private void MirrorPrimaryFont()
    {
        if (!_initialized || _callerCustomFont || _synchronizingFont || _primaryButton.IsDisposed)
        {
            return;
        }

        _synchronizingFont = true;
        try
        {
            Font = _primaryButton.Font;
        }
        finally
        {
            _synchronizingFont = false;
        }

        ApplyPreferredSize();
        PerformLayout();
    }

    private string ResolvePrimaryAccessibleName()
    {
        return !string.IsNullOrWhiteSpace(AccessibleName)
            ? AccessibleName
            : (Text ?? string.Empty);
    }

    private void OnPrimaryButtonClick(object? sender, EventArgs e)
    {
        if (Enabled && !_loading)
        {
            OnClick(EventArgs.Empty);
        }
    }

    private void OnDropDownButtonClick(object? sender, EventArgs e)
    {
        if (_dropdown.ConsumePendingAppClickedDismissal())
        {
            return;
        }

        if (_dropDownOpen)
        {
            CloseDropDown();
            return;
        }

        ShowDropDown();
    }

    private void OnPrimaryButtonFontChanged(object? sender, EventArgs e)
    {
        MirrorPrimaryFont();
    }

    private void OnDropdownOpened(object? sender, EventArgs e)
    {
        _dropDownOpen = true;
        _dropDownButton.Selected = true;
        Opened?.Invoke(this, EventArgs.Empty);
    }

    private void OnDropdownClosed(object? sender, EventArgs e)
    {
        _dropDownOpen = false;
        _dropDownButton.Selected = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class SplitRegionButton : BootstrapButton
    {
        private readonly BootstrapSplitButton _owner;
        private readonly bool _isMenuRegion;

        public SplitRegionButton(BootstrapSplitButton owner, bool isMenuRegion)
        {
            _owner = owner;
            _isMenuRegion = isMenuRegion;
        }

        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new SplitRegionAccessibleObject(this, _owner, _isMenuRegion);
        }
    }

    private sealed class SplitRegionAccessibleObject : Control.ControlAccessibleObject
    {
        private readonly BootstrapSplitButton _owner;
        private readonly bool _isMenuRegion;

        public SplitRegionAccessibleObject(
            SplitRegionButton ownerButton,
            BootstrapSplitButton owner,
            bool isMenuRegion)
            : base(ownerButton)
        {
            _owner = owner;
            _isMenuRegion = isMenuRegion;
        }

        public override AccessibleRole Role => AccessibleRole.PushButton;

        public override string? Name
        {
            get
            {
                var primaryName = _owner.ResolvePrimaryAccessibleName();
                if (!_isMenuRegion)
                {
                    return primaryName;
                }

                return string.IsNullOrEmpty(primaryName) ? "Menu" : primaryName + " menu";
            }
            set => base.Name = value;
        }

        public override string? Description => _isMenuRegion
            ? "Opens additional commands."
            : base.Description;
    }
}
