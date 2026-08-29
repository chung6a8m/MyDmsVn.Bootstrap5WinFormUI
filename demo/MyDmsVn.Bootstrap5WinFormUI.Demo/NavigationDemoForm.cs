using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class NavigationDemoForm : Form
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly Label _selectionStatus = new Label();
    private readonly ImageList _images = new ImageList();
    private readonly List<BootstrapDropdown> _dropdowns = new List<BootstrapDropdown>();
    private readonly List<Font> _ownedFonts = new List<Font>();

    public NavigationDemoForm()
    {
        Text = "Navigation Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1080, 760);
        MinimumSize = new Size(760, 520);

        _images.ImageSize = new Size(16, 16);
        _images.Images.Add("info", SystemIcons.Information.ToBitmap());
        _images.Images.Add("warning", SystemIcons.Warning.ToBitmap());

        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(16);
        Controls.Add(_content);

        _selectionStatus.AutoSize = true;
        _selectionStatus.AccessibleName = "Selected tab status";
        _selectionStatus.Text = "Select a tab or activate a dropdown command to observe native lifecycle events.";
        _selectionStatus.Margin = new Padding(0, 0, 0, 12);
        _content.Controls.Add(_selectionStatus);

        AddStyleScenario("Tabs", BootstrapTabStyle.Tabs, BootstrapVariant.Primary, fill: false);
        AddStyleScenario("Pills", BootstrapTabStyle.Pills, BootstrapVariant.Success, fill: false);
        AddStyleScenario("Underline", BootstrapTabStyle.Underline, BootstrapVariant.Info, fill: false);
        AddFillScenario();
        AddContentScenario();
        AddVariantScenario();
        AddDropdownScenarios();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var dropdown in _dropdowns)
            {
                dropdown.Dispose();
            }

            _dropdowns.Clear();
            _images.Dispose();
        }

        base.Dispose(disposing);

        if (disposing)
        {
            foreach (var font in _ownedFonts)
            {
                font.Dispose();
            }

            _ownedFonts.Clear();
        }
    }

    private void AddStyleScenario(string title, BootstrapTabStyle style, BootstrapVariant variant, bool fill)
    {
        var tabs = CreateTabs(style, variant, fill);
        if (style == BootstrapTabStyle.Tabs)
        {
            tabs.TabPages.Add(CreateFocusablePage("Overview", "Use Tab/Shift+Tab to move through native child controls."));
            tabs.TabPages.Add(CreateFocusablePage("Details", "Native focus traversal stays inside the selected TabPage."));
            tabs.TabPages.Add(CreateFocusablePage("Settings", "Arrow/Ctrl+Tab selection remains owned by WinForms TabControl."));
        }
        else
        {
            tabs.TabPages.Add(CreatePage("Overview", "Native TabPage content for Overview."));
            tabs.TabPages.Add(CreatePage("Details", "Native TabPage content for Details."));
            tabs.TabPages.Add(CreatePage("Settings", "Native TabPage content for Settings."));
        }

        _content.Controls.Add(CreateSection(title + " style", "Selection, mouse hit-testing, focus, and keyboard navigation remain native.", tabs));
    }

    private void AddFillScenario()
    {
        var tabs = CreateTabs(BootstrapTabStyle.Pills, BootstrapVariant.Primary, fill: true);
        tabs.TabPages.Add(CreatePage("Summary", "Fill=true uses one uniform width derived from available client width."));
        tabs.TabPages.Add(CreatePage("Activity", "Resize the window to verify equal-width headers."));
        tabs.TabPages.Add(CreatePage("Audit", "Native overflow remains responsible when the minimum header width wins."));
        _content.Controls.Add(CreateSection("Fill sizing", "All fixed owner-draw headers share the available width while respecting the token minimum.", tabs));
    }

    private void AddContentScenario()
    {
        var tabs = CreateTabs(BootstrapTabStyle.Tabs, BootstrapVariant.Warning, fill: false);
        tabs.ImageList = _images;
        tabs.ShowToolTips = true;

        var iconByKey = CreatePage("Icon key", "This page uses native TabPage.ImageKey and ToolTipText.");
        iconByKey.ImageKey = "info";
        iconByKey.ToolTipText = "Native tooltip for the image-key tab";

        var iconByIndex = CreatePage("Icon index", "This page uses native TabPage.ImageIndex.");
        iconByIndex.ImageIndex = 1;
        iconByIndex.ToolTipText = "Native tooltip for the image-index tab";

        var longText = CreatePage(
            "A deliberately long tab title that demonstrates deterministic fixed-width sizing",
            "Long text uses TextRenderer ellipsis inside the native header bounds.");
        longText.ToolTipText = "Long tab title";

        var disabled = CreatePage("Disabled", "Disabled TabPage content is retained, but its header uses disabled theme tokens.");
        disabled.Enabled = false;

        tabs.TabPages.Add(iconByKey);
        tabs.TabPages.Add(iconByIndex);
        tabs.TabPages.Add(longText);
        tabs.TabPages.Add(disabled);
        _content.Controls.Add(CreateSection("Native content semantics", "ImageList, ImageKey/ImageIndex, ToolTipText, long labels, and disabled pages stay native.", tabs));
    }

    private void AddVariantScenario()
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = Padding.Empty
        };

        var variants = new[]
        {
            BootstrapVariant.Primary,
            BootstrapVariant.Secondary,
            BootstrapVariant.Success,
            BootstrapVariant.Danger,
            BootstrapVariant.Warning,
            BootstrapVariant.Info,
            BootstrapVariant.Light,
            BootstrapVariant.Dark
        };

        foreach (var variant in variants)
        {
            var tabs = CreateTabs(BootstrapTabStyle.Underline, variant, fill: false);
            tabs.Width = 920;
            tabs.TabPages.Add(CreatePage(variant.ToString(), variant + " selected accent."));
            tabs.TabPages.Add(CreatePage("Neutral", "Inactive headers remain theme-neutral."));
            panel.Controls.Add(tabs);
        }

        _content.Controls.Add(CreateSection("Semantic variants", "All Bootstrap variants feed the selected header accent through the shared resolver.", panel));
    }

    private void AddDropdownScenarios()
    {
        var basicTarget = CreateDropdownTarget("Basic dropdown", "Dropdown basic");
        var basic = CreateDropdown(basicTarget, BootstrapVariant.Primary);
        AddCommand(basic, "Create", () => SetDropdownStatus("Basic", "Create command activated."));
        AddCommand(basic, "Edit", () => SetDropdownStatus("Basic", "Edit command activated."));
        AddCommand(basic, "Archive", () => SetDropdownStatus("Basic", "Archive command activated."));
        AddDropdownSection("Basic dropdown", "Three text commands. Native ToolStrip owns popup activation, focus, keyboard navigation, outside-click dismissal, and placement.", basicTarget);

        var iconTarget = CreateDropdownTarget("Icons", "Dropdown icons");
        var icons = CreateDropdown(iconTarget, BootstrapVariant.Success);
        AddCommand(icons, "Create item", () => SetDropdownStatus("Icons", "Create item activated."), FrameworkIconGlyph.Plus);
        AddCommand(icons, "Approve", () => SetDropdownStatus("Icons", "Approve activated."), FrameworkIconGlyph.Check);
        AddCommand(icons, "Close item", () => SetDropdownStatus("Icons", "Close item activated."), FrameworkIconGlyph.Close);
        AddDropdownSection("Dropdown icons", "Icons use the target BootstrapButton IconRenderer at the target's current DPI and theme text color.", iconTarget);

        var statesTarget = CreateDropdownTarget("States", "Dropdown states");
        var states = CreateDropdown(statesTarget, BootstrapVariant.Warning);
        var pinned = new BootstrapDropdownItem { Text = "Pinned", Checked = true };
        pinned.Click += (_, _) =>
        {
            pinned.Checked = !pinned.Checked;
            SetDropdownStatus("States", "Application code changed Pinned.Checked for the next opening.");
        };
        states.Items.Add(pinned);
        states.Items.Add(new BootstrapDropdownItem { Text = "Disabled command", Enabled = false });
        states.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator));
        AddCommand(states, "Normal command", () => SetDropdownStatus("States", "Normal command activated."));
        AddDropdownSection("Checked, disabled, separator", "Checked state is presentation-only: the framework never toggles it. Disabled rows and separators never dispatch commands.", statesTarget);

        var longTarget = CreateDropdownTarget("Long menu", "Dropdown long menu");
        var longMenu = CreateDropdown(longTarget, BootstrapVariant.Info);
        longMenu.MinimumWidth = 220;
        AddCommand(
            longMenu,
            "A deliberately long command caption that demonstrates native content auto-sizing beyond MinimumWidth",
            () => SetDropdownStatus("Long menu", "Long command activated."));
        AddDropdownSection("Long menu", "MinimumWidth is a logical DPI-scaled floor only; native content measurement may make the popup wider.", longTarget);

        var stressTarget = CreateDropdownTarget("Stress", "Dropdown stress");
        var stress = CreateDropdown(stressTarget, BootstrapVariant.Danger);
        AddCommand(stress, "Create stress item", () => SetDropdownStatus("Stress", "Command activated."), FrameworkIconGlyph.Plus);
        AddCommand(stress, "Toggle Light / Dark while open", ToggleTheme, FrameworkIconGlyph.Check);
        AddDropdownSection("Stress / runtime theme", "Open and close repeatedly; use the theme command while the popup is open to verify icon refresh and renderer invalidation.", stressTarget);

        var nestedTarget = CreateDropdownTarget("Nested commands", "Dropdown nested");
        var nested = CreateDropdown(nestedTarget, BootstrapVariant.Primary);
        var administration = new BootstrapDropdownItem { Text = "Administration" };
        var exports = new BootstrapDropdownItem { Text = "Exports" };
        AddCommand(exports.DropDownItems, "CSV export", () => SetDropdownStatus("Nested depth 3", "CSV export leaf activated."), FrameworkIconGlyph.Check);
        AddCommand(exports.DropDownItems, "PDF export", () => SetDropdownStatus("Nested depth 3", "PDF export leaf activated."));
        administration.DropDownItems.Add(exports);
        administration.DropDownItems.Add(new BootstrapDropdownItem { Text = "Disabled branch", Enabled = false });
        nested.Items.Add(administration);
        AddDropdownSection("Nested submenus", "Three levels exercise native Right/Left navigation, submenu arrows, independent margins, and visible leaf-depth status updates.", nestedTarget);

        var hostedTarget = CreateDropdownTarget("Hosted controls", "Dropdown hosted controls");
        var hosted = CreateDropdown(hostedTarget, BootstrapVariant.Success);
        hosted.Items.Add(CreateHostedItem(() =>
        {
            var check = new CheckBox
            {
                AutoSize = true,
                Text = "Include archived records",
                AccessibleName = "Hosted include archived option"
            };
            check.CheckedChanged += (_, _) => SetDropdownStatus("Hosted control", "Include archived = " + check.Checked + ".");
            return check;
        }));
        hosted.Items.Add(CreateHostedItem(() =>
        {
            var action = new BootstrapButton
            {
                AutoSize = true,
                Text = "Hosted framework action",
                AccessibleName = "Hosted framework action"
            };
            action.Click += (_, _) => SetDropdownStatus("Hosted control", "Framework button activated.");
            return action;
        }));
        AddDropdownSection("Hosted native and framework controls", "Each factory must return a fresh control. Returned controls become framework-owned snapshot controls and are disposed with that popup snapshot.", hostedTarget);

        var mixedTarget = CreateDropdownTarget("Mixed composition", "Dropdown mixed composition");
        var mixed = CreateDropdown(mixedTarget, BootstrapVariant.Info);
        var workspace = new BootstrapDropdownItem { Text = "Workspace" };
        AddCommand(workspace.DropDownItems, "Open overview", () => SetDropdownStatus("Mixed", "Nested overview leaf activated."));
        workspace.DropDownItems.Add(CreateHostedItem(() =>
        {
            var editor = new TextBox
            {
                AccessibleName = "Hosted workspace note",
                Text = "Snapshot note",
                Width = 180
            };
            editor.TextChanged += (_, _) => SetDropdownStatus("Mixed hosted editor", "Text length = " + editor.TextLength + ".");
            return editor;
        }));
        mixed.Items.Add(workspace);
        AddDropdownSection("Mixed nested and hosted content", "A submenu combines leaf commands and a fresh hosted editor; the returned editor becomes a framework-owned snapshot control.", mixedTarget);

        AddSplitButtonScenarios();

        var instructions = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(980, 0),
            AccessibleName = "Dropdown manual verification matrix",
            Text = "Manual Dropdown and split checks: mouse primary versus chevron, repeated open/close, and outside click. Keyboard Tab/Shift+Tab between split regions; Enter/Space on each; native menu Up/Down/Home/End/Right/Left/Enter/Escape. Focus, edit, and toggle hosted controls, return to menu rows, dismiss outside, then reopen to verify fresh snapshot/factory state. Verify disabled leaf/submenu/host, checked leaf, and split loading. Switch Light/Dark while root/submenu is visible; verify default theme fonts, custom split font persistence, and popup font coherence. Inspect primary/menu accessibility names before and after changing outer Text and AccessibleName. Repeat at 100/125/150/175/200% DPI for arrows, margins, seam, chevron, and custom-font layout. Test bottom/right edges and a secondary monitor. Repeat rebuild/open/close and dispose the form while nested popup is open; check for stale windows, disposed-control exceptions, or increasing GDI artifacts. Inherited Controls may enumerate framework-owned split regions, but application/demo code must not mutate, remove, or dispose them."
        };
        _content.Controls.Add(CreateSection("Dropdown manual verification", "Native-first interaction checks intentionally remain real-desktop verification rather than synthetic SendKeys tests.", instructions));
    }

    private BootstrapDropdown CreateDropdown(BootstrapButton target, BootstrapVariant variant)
    {
        var dropdown = new BootstrapDropdown
        {
            Target = target,
            Variant = variant
        };
        dropdown.Opened += (_, _) => _selectionStatus.Text = target.Text + ": Opened (native popup event).";
        dropdown.Closed += (_, _) => _selectionStatus.Text = target.Text + ": Closed (native popup event).";
        _dropdowns.Add(dropdown);
        return dropdown;
    }

    private static BootstrapButton CreateDropdownTarget(string text, string accessibleName)
    {
        return new BootstrapButton
        {
            Text = text,
            AccessibleName = accessibleName,
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 4)
        };
    }

    private void AddDropdownSection(string title, string description, BootstrapButton target)
    {
        _content.Controls.Add(CreateSection(title, description, target));
    }

    private static void AddCommand(
        BootstrapDropdown dropdown,
        string text,
        Action action,
        FrameworkIconGlyph? icon = null)
    {
        var item = new BootstrapDropdownItem
        {
            Text = text,
            Icon = icon.HasValue ? IconDescriptor.Framework(icon.Value) : null
        };
        item.Click += (_, _) => action();
        dropdown.Items.Add(item);
    }

    private static void AddCommand(
        BootstrapDropdownItemCollection items,
        string text,
        Action action,
        FrameworkIconGlyph? icon = null)
    {
        var item = new BootstrapDropdownItem
        {
            Text = text,
            Icon = icon.HasValue ? IconDescriptor.Framework(icon.Value) : null
        };
        item.Click += (_, _) => action();
        items.Add(item);
    }

    private static BootstrapDropdownItem CreateHostedItem(Func<Control> factory)
    {
        return new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = factory
        };
    }

    private void AddSplitButtonScenarios()
    {
        var split = new BootstrapSplitButton
        {
            Text = "Save",
            AccessibleName = "Split save command",
            Variant = BootstrapVariant.Primary,
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.Check),
            MinimumWidth = 240,
            Margin = new Padding(0, 4, 8, 4)
        };
        split.Click += (_, _) => SetDropdownStatus("Split primary", "Save command activated without opening the menu.");
        var saveAs = new BootstrapDropdownItem { Text = "Save as" };
        AddCommand(saveAs.DropDownItems, "Local file", () => SetDropdownStatus("Split nested", "Local file leaf activated."));
        AddCommand(saveAs.DropDownItems, "Shared workspace", () => SetDropdownStatus("Split nested", "Shared workspace leaf activated."));
        split.Items.Add(saveAs);
        split.Items.Add(CreateHostedItem(() =>
        {
            var check = new CheckBox
            {
                AutoSize = true,
                Text = "Create backup",
                AccessibleName = "Hosted split backup option"
            };
            check.CheckedChanged += (_, _) => SetDropdownStatus("Split hosted", "Create backup = " + check.Checked + ".");
            return check;
        }));

        var loading = new CheckBox
        {
            AutoSize = true,
            Text = "Loading",
            AccessibleName = "Split loading state",
            Margin = new Padding(8, 10, 0, 0)
        };
        loading.CheckedChanged += (_, _) =>
        {
            split.Loading = loading.Checked;
            split.LoadingText = loading.Checked ? "Saving" : string.Empty;
            SetDropdownStatus("Split loading", "Loading = " + loading.Checked + ".");
        };
        var splitRow = CreateHorizontalRow(split, loading);
        _content.Controls.Add(CreateSection("Split primary, nested menu, and hosted control", "Primary activation is independent from the chevron. Loading closes the popup and suppresses both regions; hosted factory output becomes framework-owned snapshot content.", splitRow));

        var customFont = new Font("Segoe UI", 12f, FontStyle.Italic);
        _ownedFonts.Add(customFont);
        var custom = new BootstrapSplitButton
        {
            Text = "Publish",
            AccessibleName = "Custom font split command",
            Variant = BootstrapVariant.Success,
            Font = customFont,
            Margin = new Padding(0, 4, 8, 4)
        };
        custom.Click += (_, _) => SetDropdownStatus("Custom font split", "Publish primary activated.");
        AddCommand(custom.Items, "Toggle Light / Dark", ToggleTheme, FrameworkIconGlyph.Check);
        AddCommand(custom.Items, "Publish preview", () => SetDropdownStatus("Custom font split", "Preview leaf activated."));
        var themeButton = new BootstrapButton
        {
            AutoSize = true,
            Text = "Toggle Light / Dark",
            AccessibleName = "Custom font split theme toggle",
            Margin = new Padding(8, 4, 0, 4)
        };
        themeButton.Click += (_, _) => ToggleTheme();
        _content.Controls.Add(CreateSection("Custom-font split across theme changes", "The caller-assigned font stays authoritative for primary, chevron, and popup presentation after later Light/Dark theme switches.", CreateHorizontalRow(custom, themeButton)));
    }

    private static FlowLayoutPanel CreateHorizontalRow(params Control[] controls)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        row.Controls.AddRange(controls);
        return row;
    }

    private void SetDropdownStatus(string scenario, string message)
    {
        _selectionStatus.Text = scenario + ": " + message;
    }

    private void ToggleTheme()
    {
        var nextMode = BootstrapThemeManager.CurrentTheme.Mode == BootstrapThemeMode.Light
            ? BootstrapThemeMode.Dark
            : BootstrapThemeMode.Light;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(nextMode);
        SetDropdownStatus("Stress", "Theme changed to " + nextMode + ".");
    }

    private BootstrapTabControl CreateTabs(BootstrapTabStyle style, BootstrapVariant variant, bool fill)
    {
        var tabs = new BootstrapTabControl
        {
            Width = 990,
            Height = 150,
            TabStyle = style,
            Variant = variant,
            Fill = fill,
            Margin = new Padding(0, 4, 0, 4)
        };
        tabs.SelectedIndexChanged += (_, _) =>
        {
            var selected = tabs.SelectedTab?.Text ?? "(none)";
            _selectionStatus.Text = $"SelectedIndexChanged: index {tabs.SelectedIndex}, tab '{selected}', style {tabs.TabStyle}.";
        };
        return tabs;
    }

    private static TabPage CreatePage(string title, string body)
    {
        var page = new TabPage(title);
        var label = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Text = body,
            Location = new Point(12, 14)
        };
        page.Controls.Add(label);
        return page;
    }

    private static TabPage CreateFocusablePage(string title, string body)
    {
        var page = CreatePage(title, body);
        page.Controls.Add(new TextBox
        {
            AccessibleName = title + " text input",
            Location = new Point(12, 42),
            Width = 220,
            Text = title + " value"
        });
        page.Controls.Add(new Button
        {
            AccessibleName = title + " action",
            AutoSize = true,
            Location = new Point(244, 40),
            Text = "Action"
        });
        page.Controls.Add(new CheckBox
        {
            AccessibleName = title + " option",
            AutoSize = true,
            Location = new Point(12, 76),
            Text = "Enabled option"
        });
        return page;
    }

    private static TableLayoutPanel CreateSection(string title, string description, Control content)
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0, 0, 0, 18),
            Padding = new Padding(8),
            Width = 1010
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = title,
            Margin = new Padding(0, 0, 0, 2)
        };
        titleLabel.Font = new Font(titleLabel.Font, FontStyle.Bold);
        panel.Controls.Add(titleLabel, 0, 0);
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = description,
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 1);
        panel.Controls.Add(content, 0, 2);
        return panel;
    }
}
