using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

internal sealed class DemoPageSection
{
    public DemoPageSection(string title, Func<Form> createForm)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        CreateForm = createForm ?? throw new ArgumentNullException(nameof(createForm));
    }

    public string Title { get; }

    public Func<Form> CreateForm { get; }
}

internal sealed class DemoPageHostForm : Form
{
    private readonly TabControl _tabs = new TabControl();

    public DemoPageHostForm(params DemoPageSection[] sections)
    {
        if (sections is null)
        {
            throw new ArgumentNullException(nameof(sections));
        }

        if (sections.Length == 0)
        {
            throw new ArgumentException("At least one demo section is required.", nameof(sections));
        }

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;

        _tabs.Dock = DockStyle.Fill;
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.Padding = new Point(18, 5);
        _tabs.DrawItem += DrawTab;
        Controls.Add(_tabs);

        foreach (var section in sections)
        {
            AddSection(section);
        }

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme(BootstrapThemeManager.CurrentTheme);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _tabs.DrawItem -= DrawTab;
        }

        base.Dispose(disposing);
    }

    private void AddSection(DemoPageSection section)
    {
        var page = new TabPage(section.Title)
        {
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            UseVisualStyleBackColor = false
        };
        _tabs.TabPages.Add(page);

        var form = section.CreateForm();
        EmbedForm(page, form);
    }

    private static void EmbedForm(Control host, Form form)
    {
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        form.ShowInTaskbar = false;
        host.Controls.Add(form);
        form.Show();
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _tabs.BackColor = theme.Colors.Body;
        _tabs.ForeColor = theme.Colors.Text;

        foreach (TabPage page in _tabs.TabPages)
        {
            page.BackColor = theme.Colors.Body;
            page.ForeColor = theme.Colors.Text;
        }

        _tabs.Invalidate();
    }

    private void DrawTab(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _tabs.TabPages.Count)
        {
            return;
        }

        var theme = BootstrapThemeManager.CurrentTheme;
        var selected = e.Index == _tabs.SelectedIndex;
        var background = selected ? theme.Colors.Surface : theme.Colors.SurfaceSecondary;
        var foreground = selected ? theme.Colors.Text : theme.Colors.MutedText;

        using (var backgroundBrush = new SolidBrush(background))
        {
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
        }

        TextRenderer.DrawText(
            e.Graphics,
            _tabs.TabPages[e.Index].Text,
            _tabs.Font,
            e.Bounds,
            foreground,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }
}
