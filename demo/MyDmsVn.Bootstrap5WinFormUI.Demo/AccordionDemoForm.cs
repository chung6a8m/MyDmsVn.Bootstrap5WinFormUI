using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class AccordionDemoForm : Form
{
    private readonly FlowLayoutPanel _root = new FlowLayoutPanel();
    private readonly FlowLayoutPanel _commands = new FlowLayoutPanel();
    private readonly Button _addDynamicItem = new Button();
    private readonly Button _collapseAll = new Button();
    private readonly Button _expandAll = new Button();
    private readonly BootstrapAccordion _singleAccordion = new BootstrapAccordion();
    private readonly BootstrapAccordion _multipleFlushAccordion = new BootstrapAccordion();
    private int _dynamicItemNumber = 2;

    public AccordionDemoForm()
    {
        Text = "Accordion and AccordionHeader Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(860, 760);
        MinimumSize = new Size(700, 560);

        ConfigureLayout();
        ConfigureSingleOpenExample();
        ConfigureMultipleFlushExample();

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    private void ConfigureLayout()
    {
        _root.Dock = DockStyle.Fill;
        _root.AutoScroll = true;
        _root.FlowDirection = FlowDirection.TopDown;
        _root.WrapContents = false;
        _root.Padding = new Padding(16);

        var title = new Label
        {
            AutoSize = true,
            Text = "Accordion — single/multiple open, flush, icons, keyboard and nested content",
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };

        var instructions = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(800, 0),
            Text = "Click anywhere on a header, then repeat with Tab + Enter/Space. The chevron is vector-rendered and follows BootstrapCollapse.AnimationProgress. The first group is single-open; the second is multiple-open + flush and supports dynamic items.",
            Margin = new Padding(0, 0, 0, 12)
        };

        _commands.AutoSize = true;
        _commands.WrapContents = true;
        _commands.Margin = new Padding(0, 0, 0, 12);
        ConfigureButton(_addDynamicItem, "Add dynamic item", (_, _) => AddDynamicItem());
        ConfigureButton(_collapseAll, "Collapse all", (_, _) =>
        {
            _singleAccordion.CollapseAll();
            _multipleFlushAccordion.CollapseAll();
        });
        ConfigureButton(_expandAll, "Expand all", (_, _) =>
        {
            _singleAccordion.ExpandAll();
            _multipleFlushAccordion.ExpandAll();
        });
        _commands.Controls.Add(_addDynamicItem);
        _commands.Controls.Add(_collapseAll);
        _commands.Controls.Add(_expandAll);

        _root.Controls.Add(title);
        _root.Controls.Add(instructions);
        _root.Controls.Add(_commands);
        _root.Controls.Add(CreateSectionLabel("Single-open composition (normal bordered style)"));
        _root.Controls.Add(_singleAccordion);
        _root.Controls.Add(CreateSectionLabel("Multiple-open composition (flush style + dynamic content)"));
        _root.Controls.Add(_multipleFlushAccordion);
        Controls.Add(_root);
    }

    private void ConfigureSingleOpenExample()
    {
        ConfigureAccordion(_singleAccordion, allowMultipleOpen: false, flush: false);

        var account = _singleAccordion.AddItem("Account settings");
        account.Header.Icon = IconDescriptor.Framework(FrameworkIconGlyph.Check);
        AddBodyLabel(account, "Only one sibling remains open in this group. Try opening Notifications or Advanced while this section is expanded.");

        var notifications = _singleAccordion.AddItem("Notifications");
        notifications.Header.Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus);
        AddBodyLabel(notifications, "Header icons use the shared source-neutral icon infrastructure. The trailing chevron is framework-owned vector geometry.");

        var advanced = _singleAccordion.AddItem("Advanced — contains a nested accordion");
        AddBodyLabel(advanced, "Nested accordion below:");

        var nested = new BootstrapAccordion
        {
            AllowMultipleOpen = true,
            Flush = false,
            AnimationDuration = TimeSpan.FromMilliseconds(200),
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0),
            MinimumSize = new Size(620, 0),
            Width = 700
        };
        var nestedOne = nested.AddItem("Nested option A");
        AddBodyLabel(nestedOne, "Nested composition reuses the same AccordionItem → AccordionHeader + BootstrapCollapse structure.");
        var nestedTwo = nested.AddItem("Nested option B");
        AddBodyLabel(nestedTwo, "Nested sections have independent multiple-open policy without another animation engine.");
        advanced.Body.Controls.Add(nested);
        advanced.Body.Controls.SetChildIndex(nested, 0);

        account.Expanded = true;
    }

    private void ConfigureMultipleFlushExample()
    {
        ConfigureAccordion(_multipleFlushAccordion, allowMultipleOpen: true, flush: true);

        var first = _multipleFlushAccordion.AddItem("Flush item 1");
        first.Header.Icon = IconDescriptor.Framework(FrameworkIconGlyph.Minus);
        AddBodyLabel(first, "Flush removes rounded outer item borders and uses separators while retaining full-header focus and activation behavior.");

        var second = _multipleFlushAccordion.AddItem("Flush item 2");
        AddBodyLabel(second, "Multiple-open mode allows this section to stay expanded while another section opens.");

        first.Expanded = true;
        second.Expanded = true;
    }

    private static void ConfigureAccordion(BootstrapAccordion accordion, bool allowMultipleOpen, bool flush)
    {
        accordion.AllowMultipleOpen = allowMultipleOpen;
        accordion.Flush = flush;
        accordion.AnimationDuration = TimeSpan.FromMilliseconds(200);
        accordion.AutoSize = true;
        accordion.Margin = new Padding(0, 0, 0, 16);
        accordion.MinimumSize = new Size(780, 0);
        accordion.Width = 800;
    }

    private void AddDynamicItem()
    {
        _dynamicItemNumber++;
        var item = _multipleFlushAccordion.AddItem($"Dynamic item {_dynamicItemNumber}");
        item.Header.Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus);

        var content = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        content.Controls.Add(CreateBodyLabel("This item was added at runtime. Add more rows below to exercise auto-height remeasurement."));
        content.Controls.Add(CreateBodyLabel("The Accordion coordinates policy only; BootstrapCollapse still owns all height animation."));
        item.Body.Controls.Add(content);
        item.Expanded = true;
        _multipleFlushAccordion.PerformLayout();
    }

    private static void AddBodyLabel(BootstrapAccordionItem item, string text)
    {
        var label = CreateBodyLabel(text);
        label.Dock = DockStyle.Top;
        item.Body.Controls.Add(label);
        item.Body.Controls.SetChildIndex(label, 0);
    }

    private static Label CreateBodyLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            MaximumSize = new Size(720, 0),
            Text = text,
            Margin = new Padding(0, 3, 0, 3),
            BackColor = Color.Transparent
        };
    }

    private static Label CreateSectionLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 6)
        };
    }

    private static void ConfigureButton(Button button, string text, EventHandler click)
    {
        button.AutoSize = true;
        button.Text = text;
        button.Margin = new Padding(0, 0, 8, 0);
        button.UseVisualStyleBackColor = false;
        button.Click += click;
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _root.BackColor = theme.Colors.Body;
        _root.ForeColor = theme.Colors.Text;
        _commands.BackColor = theme.Colors.Body;
        _commands.ForeColor = theme.Colors.Text;

        foreach (Control control in _commands.Controls)
        {
            control.BackColor = theme.Colors.Surface;
            control.ForeColor = theme.Colors.Text;
        }

        ApplyThemeRecursively(_root, theme);
    }

    private static void ApplyThemeRecursively(Control root, BootstrapTheme theme)
    {
        foreach (Control child in root.Controls)
        {
            if (child is Label)
            {
                child.ForeColor = theme.Colors.Text;
            }

            ApplyThemeRecursively(child, theme);
        }
    }
}
