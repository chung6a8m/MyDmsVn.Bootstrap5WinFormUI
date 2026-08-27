using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class CollapseDemoForm : Form
{
    private readonly FlowLayoutPanel _root = new FlowLayoutPanel();
    private readonly FlowLayoutPanel _commands = new FlowLayoutPanel();
    private readonly Button _toggleAuto = new Button();
    private readonly Button _addRow = new Button();
    private readonly Button _removeRow = new Button();
    private readonly Button _toggleFixed = new Button();
    private readonly Label _autoStatus = new Label();
    private readonly Label _fixedStatus = new Label();
    private readonly BootstrapCollapse _autoCollapse = new BootstrapCollapse();
    private readonly BootstrapCollapse _fixedCollapse = new BootstrapCollapse();
    private readonly FlowLayoutPanel _variableContent = new FlowLayoutPanel();
    private readonly Panel _fixedContent = new Panel();
    private int _dynamicRowNumber = 3;

    public CollapseDemoForm()
    {
        Text = "BootstrapCollapse Demo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(820, 660);
        MinimumSize = new Size(680, 520);

        ConfigureLayout();
        ConfigureAutoExample();
        ConfigureFixedExample();

        BootstrapThemeManager.ThemeChanged += OnThemeChanged;
        _autoCollapse.ExpandedChanged += OnCollapseStateChanged;
        _autoCollapse.AnimationProgressChanged += OnCollapseProgressChanged;
        _fixedCollapse.ExpandedChanged += OnCollapseStateChanged;
        _fixedCollapse.AnimationProgressChanged += OnCollapseProgressChanged;

        ApplyTheme(BootstrapThemeManager.CurrentTheme);
        UpdateStatus();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BootstrapThemeManager.ThemeChanged -= OnThemeChanged;
            _autoCollapse.ExpandedChanged -= OnCollapseStateChanged;
            _autoCollapse.AnimationProgressChanged -= OnCollapseProgressChanged;
            _fixedCollapse.ExpandedChanged -= OnCollapseStateChanged;
            _fixedCollapse.AnimationProgressChanged -= OnCollapseProgressChanged;
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
            Text = "BootstrapCollapse — auto measurement, fixed height, reversal and reduced motion",
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };

        var instructions = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            Text = "Click either Toggle repeatedly while it is moving to verify reversal from the current visual height. Add/remove variable rows while expanded or collapsed, and use the main window Reduced motion option to verify immediate final states.",
            Margin = new Padding(0, 0, 0, 12)
        };

        _commands.AutoSize = true;
        _commands.WrapContents = true;
        _commands.Margin = new Padding(0, 0, 0, 12);

        ConfigureButton(_toggleAuto, "Toggle auto", (_, _) => _autoCollapse.Toggle());
        ConfigureButton(_addRow, "Add row", (_, _) => AddVariableRow());
        ConfigureButton(_removeRow, "Remove row", (_, _) => RemoveVariableRow());
        ConfigureButton(_toggleFixed, "Toggle fixed", (_, _) => _fixedCollapse.Toggle());

        _commands.Controls.Add(_toggleAuto);
        _commands.Controls.Add(_addRow);
        _commands.Controls.Add(_removeRow);
        _commands.Controls.Add(_toggleFixed);

        _autoStatus.AutoSize = true;
        _autoStatus.Margin = new Padding(0, 4, 0, 4);
        _fixedStatus.AutoSize = true;
        _fixedStatus.Margin = new Padding(0, 16, 0, 4);

        _root.Controls.Add(title);
        _root.Controls.Add(instructions);
        _root.Controls.Add(_commands);
        _root.Controls.Add(_autoStatus);
        _root.Controls.Add(_autoCollapse);
        _root.Controls.Add(_fixedStatus);
        _root.Controls.Add(_fixedCollapse);
        Controls.Add(_root);
    }

    private void ConfigureAutoExample()
    {
        _autoCollapse.Width = 750;
        _autoCollapse.Padding = new Padding(12);
        _autoCollapse.Margin = Padding.Empty;
        _autoCollapse.ExpandedHeightMode = BootstrapCollapseHeightMode.Auto;
        _autoCollapse.AnimationDuration = TimeSpan.FromMilliseconds(200);

        _variableContent.Dock = DockStyle.Top;
        _variableContent.AutoSize = true;
        _variableContent.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _variableContent.FlowDirection = FlowDirection.TopDown;
        _variableContent.WrapContents = false;
        _variableContent.Padding = new Padding(8);
        _variableContent.Margin = Padding.Empty;

        AddVariableRow("Variable content row 1 — measured automatically.");
        AddVariableRow("Variable content row 2 — resize the form and keep toggling.");
        AddVariableRow("Variable content row 3 — add or remove rows while collapsed.");

        _autoCollapse.Controls.Add(_variableContent);
        _autoCollapse.PerformLayout();
    }

    private void ConfigureFixedExample()
    {
        _fixedCollapse.Width = 750;
        _fixedCollapse.Margin = Padding.Empty;
        _fixedCollapse.ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed;
        _fixedCollapse.ExpandedHeight = 180;
        _fixedCollapse.AnimationDuration = TimeSpan.FromMilliseconds(200);

        _fixedContent.Dock = DockStyle.Fill;
        _fixedContent.Padding = new Padding(20);
        _fixedContent.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Fixed-height content\r\n\r\nExpandedHeight = 180. The final expanded height remains exact even when the child content would prefer a different size.",
            TextAlign = ContentAlignment.MiddleLeft
        });
        _fixedCollapse.Controls.Add(_fixedContent);
    }

    private static void ConfigureButton(Button button, string text, EventHandler click)
    {
        button.AutoSize = true;
        button.Text = text;
        button.Margin = new Padding(0, 0, 8, 0);
        button.UseVisualStyleBackColor = false;
        button.Click += click;
    }

    private void AddVariableRow()
    {
        _dynamicRowNumber++;
        AddVariableRow($"Dynamic row {_dynamicRowNumber} — Auto mode remeasures content changes.");
        _autoCollapse.PerformLayout();
        UpdateStatus();
    }

    private void AddVariableRow(string text)
    {
        _variableContent.Controls.Add(new Label
        {
            AutoSize = true,
            Text = text,
            Margin = new Padding(0, 3, 0, 3)
        });
    }

    private void RemoveVariableRow()
    {
        if (_variableContent.Controls.Count <= 1)
        {
            return;
        }

        var control = _variableContent.Controls[_variableContent.Controls.Count - 1];
        _variableContent.Controls.Remove(control);
        control.Dispose();
        _autoCollapse.PerformLayout();
        UpdateStatus();
    }

    private void OnCollapseStateChanged(object? sender, EventArgs e)
    {
        UpdateStatus();
    }

    private void OnCollapseProgressChanged(object? sender, EventArgs e)
    {
        UpdateStatus();
    }

    private void OnThemeChanged(object? sender, BootstrapThemeChangedEventArgs e)
    {
        ApplyTheme(e.NewTheme);
    }

    private void UpdateStatus()
    {
        _autoStatus.Text = $"AUTO · Expanded={_autoCollapse.Expanded} · Height={_autoCollapse.Height} · Progress={_autoCollapse.AnimationProgress:0.00} · IsAnimating={_autoCollapse.IsAnimating}";
        _fixedStatus.Text = $"FIXED 180px · Expanded={_fixedCollapse.Expanded} · Height={_fixedCollapse.Height} · Progress={_fixedCollapse.AnimationProgress:0.00} · IsAnimating={_fixedCollapse.IsAnimating}";
    }

    private void ApplyTheme(BootstrapTheme theme)
    {
        BackColor = theme.Colors.Body;
        ForeColor = theme.Colors.Text;
        _root.BackColor = theme.Colors.Body;
        _root.ForeColor = theme.Colors.Text;
        _commands.BackColor = theme.Colors.Body;
        _autoStatus.ForeColor = theme.Colors.Text;
        _fixedStatus.ForeColor = theme.Colors.Text;
        _autoCollapse.BackColor = theme.Colors.Surface;
        _fixedCollapse.BackColor = theme.Colors.Surface;
        _variableContent.BackColor = theme.Colors.SurfaceSecondary;
        _variableContent.ForeColor = theme.Colors.Text;
        _fixedContent.BackColor = theme.Colors.SurfaceSecondary;
        _fixedContent.ForeColor = theme.Colors.Text;

        foreach (Control control in _variableContent.Controls)
        {
            control.BackColor = theme.Colors.SurfaceSecondary;
            control.ForeColor = theme.Colors.Text;
        }

        foreach (Control control in _fixedContent.Controls)
        {
            control.BackColor = theme.Colors.SurfaceSecondary;
            control.ForeColor = theme.Colors.Text;
        }

        foreach (Control control in _commands.Controls)
        {
            control.BackColor = theme.Colors.Surface;
            control.ForeColor = theme.Colors.Text;
        }
    }
}
