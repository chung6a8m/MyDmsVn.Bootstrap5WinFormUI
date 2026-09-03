using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class MainForm : Form
{
    private readonly BootstrapSidebar _navigation = new BootstrapSidebar();
    private readonly Panel _workspace = new Panel();
    private readonly TableLayoutPanel _header = new TableLayoutPanel();
    private readonly TableLayoutPanel _titleBlock = new TableLayoutPanel();
    private readonly FlowLayoutPanel _settings = new FlowLayoutPanel();
    private readonly Panel _contentHost = new Panel();
    private readonly Button _navigationToggle = new Button();
    private readonly Label _pageTitle = new Label();
    private readonly Label _pageDescription = new Label();
    private readonly Label _themeLabel = new Label();
    private readonly ComboBox _themeMode = new ComboBox();
    private readonly CheckBox _reducedMotion = new CheckBox();
    private Form? _currentPage;
    private bool _updatingSelection;

    public MainForm()
    {
        Text = "MyDmsVn.Bootstrap5WinFormUI — Integrated Demo";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1280, 800);
        MinimumSize = new Size(900, 600);

        ConfigureWorkspace();
        ConfigureHeader();
        ConfigureNavigation();
        ConfigurePages();

        Controls.Add(_workspace);
        Controls.Add(_navigation);

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        SyncSelection(BootstrapThemeManager.CurrentTheme);
        ApplyTheme(BootstrapThemeManager.CurrentTheme);

        if (_navigation.Items.Count > 0)
        {
            _navigation.SelectedItem = _navigation.Items[0];
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    private void ConfigureWorkspace()
    {
        _workspace.Dock = DockStyle.Fill;
        _contentHost.Dock = DockStyle.Fill;
        _contentHost.Padding = Padding.Empty;

        _workspace.Controls.Add(_contentHost);
        _workspace.Controls.Add(_header);
    }

    private void ConfigureHeader()
    {
        _header.Dock = DockStyle.Top;
        _header.Height = 82;
        _header.Padding = new Padding(12, 10, 12, 8);
        _header.ColumnCount = 3;
        _header.RowCount = 1;
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _navigationToggle.AutoSize = true;
        _navigationToggle.Text = "Menu";
        _navigationToggle.Margin = new Padding(0, 8, 12, 8);
        _navigationToggle.UseVisualStyleBackColor = false;
        _navigationToggle.AccessibleName = "Toggle demo navigation";
        _navigationToggle.Click += (_, _) => _navigation.Toggle();

        _titleBlock.Dock = DockStyle.Fill;
        _titleBlock.Margin = Padding.Empty;
        _titleBlock.ColumnCount = 1;
        _titleBlock.RowCount = 2;
        _titleBlock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _titleBlock.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        _titleBlock.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

        _pageTitle.Dock = DockStyle.Fill;
        _pageTitle.AutoEllipsis = true;
        _pageTitle.TextAlign = ContentAlignment.BottomLeft;
        _pageTitle.AccessibleName = "Current demo page title";
        _pageTitle.Font = new Font(_pageTitle.Font, FontStyle.Bold);

        _pageDescription.Dock = DockStyle.Fill;
        _pageDescription.AutoEllipsis = true;
        _pageDescription.TextAlign = ContentAlignment.TopLeft;
        _pageDescription.AccessibleName = "Current demo page description";

        _titleBlock.Controls.Add(_pageTitle, 0, 0);
        _titleBlock.Controls.Add(_pageDescription, 0, 1);

        _settings.AutoSize = true;
        _settings.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _settings.Dock = DockStyle.Fill;
        _settings.FlowDirection = FlowDirection.LeftToRight;
        _settings.WrapContents = false;
        _settings.Margin = Padding.Empty;
        _settings.Padding = Padding.Empty;

        _themeLabel.AutoSize = true;
        _themeLabel.Text = "Theme";
        _themeLabel.Margin = new Padding(0, 14, 6, 0);

        _themeMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeMode.Width = 112;
        _themeMode.Margin = new Padding(0, 8, 0, 0);
        _themeMode.Items.Add("Light");
        _themeMode.Items.Add("Dark");
        _themeMode.SelectedIndexChanged += (_, _) => PublishSelectedTheme();

        _reducedMotion.AutoSize = true;
        _reducedMotion.Text = "Reduced motion";
        _reducedMotion.Margin = new Padding(16, 13, 0, 0);
        _reducedMotion.CheckedChanged += (_, _) => PublishSelectedTheme();

        _settings.Controls.Add(_themeLabel);
        _settings.Controls.Add(_themeMode);
        _settings.Controls.Add(_reducedMotion);

        _header.Controls.Add(_navigationToggle, 0, 0);
        _header.Controls.Add(_titleBlock, 1, 0);
        _header.Controls.Add(_settings, 2, 0);
    }

    private void ConfigureNavigation()
    {
        _navigation.Dock = DockStyle.Left;
        _navigation.ExpandedWidth = 250;
        _navigation.CollapsedWidth = 68;
        _navigation.AccessibleName = "Integrated demo navigation";
        _navigation.SelectedItemChanged += (_, _) =>
        {
            if (_navigation.SelectedItem?.Tag is DemoPageDefinition page)
            {
                ShowPage(page);
            }
        };
    }

    private void ConfigurePages()
    {
        var themeItem = AddPage(
            "Theme",
            "Semantic colors, typography, metrics, runtime Light/Dark switching, and reduced motion.",
            () => new ThemeDemoForm());
        themeItem.Expanded = true;
        AddChildPage(
            themeItem,
            "Rendering / DPI",
            "Shared rendering primitives and virtual 96–192 DPI diagnostics.",
            () => new RenderingDemoForm());
        AddChildPage(
            themeItem,
            "Icons",
            "Source-neutral Segoe MDL2 and framework-vector icon diagnostics.",
            () => new IconDemoForm());
        AddChildPage(
            themeItem,
            "Animation",
            "Finite and loop animation lifecycle, hide/show, restart, and reduced-motion diagnostics.",
            () => new AnimationDemoForm());

        AddPage(
            "Buttons / Groups / Toolbar",
            "Button variants, loading, connected groups, selection modes, and toolbar layouts.",
            () => new DemoPageHostForm(
                new DemoPageSection("Buttons", () => new ButtonDemoForm()),
                new DemoPageSection("Groups / Toolbar", () => new ButtonGroupToolbarDemoForm())));

        AddPage(
            "Inputs",
            "Text input placeholder, validation, icons, clear, disabled, read-only, password, and focus states.",
            () => new TextBoxCardDemoForm());

        AddPage(
            "Checks / Radios / Switches",
            "Native-backed checks, radios, and switches with variants, validation, RTL, grouping, fallback, and event feedback.",
            () => new ChecksDemoForm());

        AddPage(
            "Advanced Inputs",
            "Native-backed NumericBox, ComboBox, and DatePicker scenarios with validation, formatting, keyboard, and DPI checks.",
            () => new AdvancedInputsDemoForm());

        AddPage(
            "Select",
            "Select2-style single/multiple selection, grouping, custom values, async providers, paging, retry, keyboard, and accessibility scenarios.",
            () => new BootstrapSelectDemoForm());

        AddPage(
            "Input Groups",
            "Connected addons, inputs, buttons, Single Select, split commands, sizing, validation, reorder, RTL, and constrained-width behavior.",
            () => new InputGroupDemoForm());

        AddPage(
            "Cards",
            "Themed surfaces with Header/Body/Footer composition, borders, radius, and shadow states.",
            () => new TextBoxCardDemoForm());

        AddPage(
            "Feedback",
            "Badge indicators across semantic variants, pill/custom/disabled states, runtime theme switching, and DPI verification.",
            () => new FeedbackDemoForm());

        AddPage(
            "Collapse / Accordion",
            "Variable and fixed collapse content plus keyboard-friendly single/multiple accordion scenarios.",
            () => new DemoPageHostForm(
                new DemoPageSection("Collapse", () => new CollapseDemoForm()),
                new DemoPageSection("Accordion", () => new AccordionDemoForm())));

        AddPage(
            "Loading / Spinner",
            "Border/Grow spinners, semantic variants, reduced motion, and Button loading behavior.",
            () => new DemoPageHostForm(
                new DemoPageSection("Spinner", () => new SpinnerDemoForm()),
                new DemoPageSection("Button loading", () => new ButtonDemoForm())));

        AddPage(
            "Progress",
            "Determinate, striped, animated, indeterminate, custom-color, and AnimateTo scenarios.",
            () => new ProgressDemoForm());

        AddPage(
            "Sidebar",
            "Expanded/collapsed navigation, selection, icons, badges, disabled items, nested sections, and keyboard use.",
            () => new SidebarDemoForm());

        AddPage(
            "DataGrid",
            "Bound data, states, large-row performance, and editable product cells using BootstrapSelect through the native editing lifecycle.",
            () => new DemoPageHostForm(
                new DemoPageSection("Basic / states", () => new DataGridDemoForm()),
                new DemoPageSection("Editable + BootstrapLookup", () => new DataGridSelectEditingDemoForm())));

        AddPage(
            "Pagination",
            "Bounded page windows, ellipses, navigation visibility, sizes, and application-owned DataGrid paging.",
            () => new PaginationDemoForm());

        AddPage(
            "Navigation / Tabs",
            "Native-backed tab pages with Tabs/Pills/Underline headers, fill sizing, images, tooltips, disabled pages, variants, and selection events.",
            () => new NavigationDemoForm());
    }

    private BootstrapSidebarItem AddPage(string title, string description, Func<Form> createForm)
    {
        var item = CreateNavigationItem(title, description, createForm);
        _navigation.Items.Add(item);
        return item;
    }

    private static void AddChildPage(
        BootstrapSidebarItem parent,
        string title,
        string description,
        Func<Form> createForm)
    {
        parent.Items.Add(CreateNavigationItem(title, description, createForm));
    }

    private static BootstrapSidebarItem CreateNavigationItem(string title, string description, Func<Form> createForm)
    {
        var page = new DemoPageDefinition(title, description, createForm);
        return new BootstrapSidebarItem
        {
            Text = title,
            Tag = page
        };
    }

    private void ShowPage(DemoPageDefinition page)
    {
        _contentHost.SuspendLayout();
        try
        {
            if (_currentPage is not null)
            {
                _contentHost.Controls.Remove(_currentPage);
                _currentPage.Dispose();
                _currentPage = null;
            }

            _pageTitle.Text = page.Title;
            _pageDescription.Text = page.Description;

            var form = page.CreateForm();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            form.ShowInTaskbar = false;
            _contentHost.Controls.Add(form);
            _currentPage = form;
            form.Show();
        }
        finally
        {
            _contentHost.ResumeLayout(true);
        }
    }

    private void PublishSelectedTheme()
    {
        if (_updatingSelection || _themeMode.SelectedIndex < 0)
        {
            return;
        }

        var mode = _themeMode.SelectedIndex == 1
            ? BootstrapThemeMode.Dark
            : BootstrapThemeMode.Light;

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode, _reducedMotion.Checked);
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        SyncSelection(e.NewTheme);
        ApplyTheme(e.NewTheme);
    }

    private void SyncSelection(BootstrapTheme theme)
    {
        _updatingSelection = true;
        try
        {
            _themeMode.SelectedIndex = theme.Mode == BootstrapThemeMode.Dark ? 1 : 0;
            _reducedMotion.Checked = theme.ReducedMotion;
        }
        finally
        {
            _updatingSelection = false;
        }
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _workspace.BackColor = theme.Colors.Body;
        _workspace.ForeColor = theme.Colors.Text;
        _contentHost.BackColor = theme.Colors.Body;
        _header.BackColor = theme.Colors.SurfaceSecondary;
        _titleBlock.BackColor = theme.Colors.SurfaceSecondary;
        _settings.BackColor = theme.Colors.SurfaceSecondary;
        _pageTitle.BackColor = theme.Colors.SurfaceSecondary;
        _pageTitle.ForeColor = theme.Colors.Text;
        _pageDescription.BackColor = theme.Colors.SurfaceSecondary;
        _pageDescription.ForeColor = theme.Colors.MutedText;
        _themeLabel.BackColor = theme.Colors.SurfaceSecondary;
        _themeLabel.ForeColor = theme.Colors.Text;
        _themeMode.BackColor = theme.Colors.Surface;
        _themeMode.ForeColor = theme.Colors.Text;
        _reducedMotion.BackColor = theme.Colors.SurfaceSecondary;
        _reducedMotion.ForeColor = theme.Colors.Text;
        _navigationToggle.BackColor = theme.Colors.Surface;
        _navigationToggle.ForeColor = theme.Colors.Text;
    }

    private sealed class DemoPageDefinition
    {
        public DemoPageDefinition(string title, string description, Func<Form> createForm)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            CreateForm = createForm ?? throw new ArgumentNullException(nameof(createForm));
        }

        public string Title { get; }

        public string Description { get; }

        public Func<Form> CreateForm { get; }
    }
}
