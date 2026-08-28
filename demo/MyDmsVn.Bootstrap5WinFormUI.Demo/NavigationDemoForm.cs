using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class NavigationDemoForm : Form
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly Label _selectionStatus = new Label();
    private readonly ImageList _images = new ImageList();

    public NavigationDemoForm()
    {
        Text = "BootstrapTabControl Demo";
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
        _selectionStatus.Text = "Select a tab to see native SelectedIndexChanged events.";
        _selectionStatus.Margin = new Padding(0, 0, 0, 12);
        _content.Controls.Add(_selectionStatus);

        AddStyleScenario("Tabs", BootstrapTabStyle.Tabs, BootstrapVariant.Primary, fill: false);
        AddStyleScenario("Pills", BootstrapTabStyle.Pills, BootstrapVariant.Success, fill: false);
        AddStyleScenario("Underline", BootstrapTabStyle.Underline, BootstrapVariant.Info, fill: false);
        AddFillScenario();
        AddContentScenario();
        AddVariantScenario();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _images.Dispose();
        }

        base.Dispose(disposing);
    }

    private void AddStyleScenario(string title, BootstrapTabStyle style, BootstrapVariant variant, bool fill)
    {
        var tabs = CreateTabs(style, variant, fill);
        tabs.TabPages.Add(CreatePage("Overview", "Native TabPage content for Overview."));
        tabs.TabPages.Add(CreatePage("Details", "Native TabPage content for Details."));
        tabs.TabPages.Add(CreatePage("Settings", "Native TabPage content for Settings."));
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

        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            Text = title,
            Margin = new Padding(0, 0, 0, 2)
        }, 0, 0);
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
