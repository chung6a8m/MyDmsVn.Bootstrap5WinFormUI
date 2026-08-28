using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

/// <summary>
/// Provides Bootstrap-inspired, data-source-agnostic page navigation by composing
/// <see cref="BootstrapButton"/> controls inside a connected <see cref="BootstrapButtonGroup"/>.
/// </summary>
[DefaultEvent(nameof(PageChanged))]
public class BootstrapPagination : Panel
{
    private readonly BootstrapButtonGroup _buttonGroup;
    private int _totalItems;
    private int _pageSize = 20;
    private int _currentPage = 1;
    private int _maxVisiblePages = 5;
    private bool _showFirstLast = true;
    private bool _showPreviousNext = true;
    private BootstrapButtonSize _buttonSize = BootstrapButtonSize.Default;
    private BootstrapVariant _variant = BootstrapVariant.Primary;
    private int _borderRadius = -1;

    /// <summary>
    /// Initializes a designer-safe pagination control using the current shared Button and ButtonGroup presentation.
    /// </summary>
    public BootstrapPagination()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);

        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleDescription = "Pagination navigation.";

        _buttonGroup = new BootstrapButtonGroup
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            BorderRadius = _borderRadius,
            Orientation = Orientation.Horizontal,
            SelectionMode = BootstrapButtonSelectionMode.None,
            TabStop = false
        };

        Controls.Add(_buttonGroup);
        RebuildButtons();
    }

    /// <summary>
    /// Occurs after the effective current page changes.
    /// </summary>
    public event EventHandler? PageChanged;

    /// <summary>
    /// Gets or sets the total number of caller-owned items represented by this pagination control.
    /// </summary>
    [Category("Behavior")]
    [Description("Specifies the total number of caller-owned items represented by this pagination control.")]
    [DefaultValue(0)]
    public int TotalItems
    {
        get => _totalItems;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Total items cannot be negative.");
            }

            if (_totalItems == value)
            {
                return;
            }

            _totalItems = value;
            ApplyRangeChange();
        }
    }

    /// <summary>
    /// Gets or sets the number of caller-owned items represented by one page.
    /// </summary>
    [Category("Behavior")]
    [Description("Specifies the number of caller-owned items represented by one page.")]
    [DefaultValue(20)]
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Page size must be at least one.");
            }

            if (_pageSize == value)
            {
                return;
            }

            _pageSize = value;
            ApplyRangeChange();
        }
    }

    /// <summary>
    /// Gets or sets the one-based current page.
    /// </summary>
    [Category("Behavior")]
    [Description("Specifies the one-based current page.")]
    [DefaultValue(1)]
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            var totalPages = TotalPages;
            if (value < 1 || value > totalPages)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Current page must be within the available page range.");
            }

            if (_currentPage == value)
            {
                return;
            }

            _currentPage = value;
            RebuildButtons();
            PageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the number of pages represented by <see cref="TotalItems"/> and <see cref="PageSize"/>.
    /// The value is always at least one so an empty data set still has a stable page-one state.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int TotalPages => _totalItems == 0 ? 1 : 1 + ((_totalItems - 1) / _pageSize);

    /// <summary>
    /// Gets or sets the maximum number of numeric page buttons used to build the bounded page window.
    /// </summary>
    [Category("Layout")]
    [Description("Specifies the maximum numeric-page window. Values below five are not supported.")]
    [DefaultValue(5)]
    public int MaxVisiblePages
    {
        get => _maxVisiblePages;
        set
        {
            if (value < 5)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum visible pages must be at least five.");
            }

            if (_maxVisiblePages == value)
            {
                return;
            }

            _maxVisiblePages = value;
            RebuildButtons();
        }
    }

    /// <summary>
    /// Gets or sets whether First and Last navigation buttons are displayed.
    /// </summary>
    [Category("Appearance")]
    [Description("Shows or hides the First and Last navigation buttons.")]
    [DefaultValue(true)]
    public bool ShowFirstLast
    {
        get => _showFirstLast;
        set
        {
            if (_showFirstLast == value)
            {
                return;
            }

            _showFirstLast = value;
            RebuildButtons();
        }
    }

    /// <summary>
    /// Gets or sets whether Previous and Next navigation buttons are displayed.
    /// </summary>
    [Category("Appearance")]
    [Description("Shows or hides the Previous and Next navigation buttons.")]
    [DefaultValue(true)]
    public bool ShowPreviousNext
    {
        get => _showPreviousNext;
        set
        {
            if (_showPreviousNext == value)
            {
                return;
            }

            _showPreviousNext = value;
            RebuildButtons();
        }
    }

    /// <summary>
    /// Gets or sets the size preset applied to every composed pagination button.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects the Small, Default, or Large size applied to pagination buttons.")]
    [DefaultValue(BootstrapButtonSize.Default)]
    public BootstrapButtonSize ButtonSize
    {
        get => _buttonSize;
        set
        {
            ValidateButtonSize(value);
            if (_buttonSize == value)
            {
                return;
            }

            _buttonSize = value;
            ApplyButtonPresentation();
        }
    }

    /// <summary>
    /// Gets or sets the semantic variant applied to every composed pagination button.
    /// </summary>
    [Category("Appearance")]
    [Description("Selects the semantic variant applied to pagination buttons.")]
    [DefaultValue(BootstrapVariant.Primary)]
    public BootstrapVariant Variant
    {
        get => _variant;
        set
        {
            ValidateVariant(value);
            if (_variant == value)
            {
                return;
            }

            _variant = value;
            ApplyButtonPresentation();
        }
    }

    /// <summary>
    /// Gets or sets the logical radius forwarded to the owned connected button group.
    /// Use -1 to preserve theme-derived outer radii.
    /// </summary>
    [Category("Appearance")]
    [Description("Sets the connected group's logical outer radius, or -1 to use button/theme radii.")]
    [DefaultValue(-1)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Border radius must be -1 or a non-negative value.");
            }

            if (_borderRadius == value)
            {
                return;
            }

            _borderRadius = value;
            _buttonGroup.BorderRadius = value;
            PerformPaginationLayout();
        }
    }

    /// <inheritdoc />
    public override Size GetPreferredSize(Size proposedSize)
    {
        var groupSize = _buttonGroup.GetPreferredSize(Size.Empty);
        return new Size(
            Math.Max(0, groupSize.Width + Padding.Left + Padding.Right),
            Math.Max(0, groupSize.Height + Padding.Top + Padding.Bottom));
    }

    private void ApplyRangeChange()
    {
        var previousPage = _currentPage;
        var totalPages = TotalPages;
        if (_currentPage > totalPages)
        {
            _currentPage = totalPages;
        }

        RebuildButtons();
        if (_currentPage != previousPage)
        {
            PageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void RebuildButtons()
    {
        _buttonGroup.SuspendLayout();
        try
        {
            DisposeCurrentButtons();

            if (_showFirstLast)
            {
                AddNavigationButton("«", "First page", 1, _currentPage > 1);
            }

            if (_showPreviousNext)
            {
                AddNavigationButton("‹", "Previous page", Math.Max(1, _currentPage - 1), _currentPage > 1);
            }

            var items = BootstrapPaginationLayoutLogic.Build(TotalPages, _currentPage, _maxVisiblePages);
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (item.Kind == BootstrapPaginationItemKind.Ellipsis)
                {
                    AddEllipsisButton();
                }
                else
                {
                    AddPageButton(item.Page);
                }
            }

            if (_showPreviousNext)
            {
                AddNavigationButton("›", "Next page", Math.Min(TotalPages, _currentPage + 1), _currentPage < TotalPages);
            }

            if (_showFirstLast)
            {
                AddNavigationButton("»", "Last page", TotalPages, _currentPage < TotalPages);
            }
        }
        finally
        {
            _buttonGroup.ResumeLayout(true);
        }

        PerformPaginationLayout();
    }

    private void DisposeCurrentButtons()
    {
        while (_buttonGroup.Controls.Count > 0)
        {
            var control = _buttonGroup.Controls[0];
            _buttonGroup.Controls.RemoveAt(0);
            if (control is BootstrapButton button)
            {
                button.Click -= OnNavigationButtonClick;
            }

            control.Dispose();
        }
    }

    private void AddNavigationButton(string text, string accessibleName, int targetPage, bool enabled)
    {
        var button = CreateButton(text, accessibleName, selected: false, enabled: enabled);
        button.Tag = targetPage;
        button.Click += OnNavigationButtonClick;
        _buttonGroup.Controls.Add(button);
    }

    private void AddPageButton(int page)
    {
        var selected = page == _currentPage;
        var accessibleName = selected ? "Current page " + page : "Page " + page;
        var button = CreateButton(page.ToString(), accessibleName, selected, enabled: true);
        button.Tag = page;
        button.Click += OnNavigationButtonClick;
        _buttonGroup.Controls.Add(button);
    }

    private void AddEllipsisButton()
    {
        var button = CreateButton("…", "More pages", selected: false, enabled: false);
        button.TabStop = false;
        _buttonGroup.Controls.Add(button);
    }

    private BootstrapButton CreateButton(string text, string accessibleName, bool selected, bool enabled)
    {
        return new BootstrapButton
        {
            Text = text,
            AccessibleName = accessibleName,
            ButtonSize = _buttonSize,
            Variant = _variant,
            Selected = selected,
            Enabled = enabled,
            TabStop = enabled
        };
    }

    private void OnNavigationButtonClick(object? sender, EventArgs e)
    {
        if (sender is BootstrapButton button && button.Tag is int targetPage && targetPage != _currentPage)
        {
            CurrentPage = targetPage;
        }
    }

    private void ApplyButtonPresentation()
    {
        foreach (Control control in _buttonGroup.Controls)
        {
            if (control is BootstrapButton button)
            {
                button.ButtonSize = _buttonSize;
                button.Variant = _variant;
            }
        }

        PerformPaginationLayout();
    }

    private void PerformPaginationLayout()
    {
        _buttonGroup.Location = new Point(Padding.Left, Padding.Top);
        _buttonGroup.PerformLayout();
        _buttonGroup.Size = _buttonGroup.GetPreferredSize(Size.Empty);
        if (AutoSize)
        {
            Size = GetPreferredSize(Size.Empty);
        }

        PerformLayout();
        Invalidate();
    }

    private static void ValidateButtonSize(BootstrapButtonSize value)
    {
        if (value != BootstrapButtonSize.Small &&
            value != BootstrapButtonSize.Default &&
            value != BootstrapButtonSize.Large)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported pagination button size.");
        }
    }

    private static void ValidateVariant(BootstrapVariant value)
    {
        if (value != BootstrapVariant.Primary &&
            value != BootstrapVariant.Secondary &&
            value != BootstrapVariant.Success &&
            value != BootstrapVariant.Danger &&
            value != BootstrapVariant.Warning &&
            value != BootstrapVariant.Info &&
            value != BootstrapVariant.Light &&
            value != BootstrapVariant.Dark)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported pagination variant.");
        }
    }
}
