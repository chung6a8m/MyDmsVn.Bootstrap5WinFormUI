using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapLookupFooter : Panel
{
    private readonly Label _status = new Label();
    private readonly Button _refresh = new Button();
    private readonly Button _addNew = new Button();

    internal BootstrapLookupFooter()
    {
        Dock = DockStyle.Bottom;
        TabStop = false;
        Height = 32;
        _status.Dock = DockStyle.Fill;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.TabStop = false;
        _status.AccessibleRole = AccessibleRole.StaticText;
        ConfigureButton(_refresh, "Refresh");
        ConfigureButton(_addNew, "Add New");
        _addNew.Dock = DockStyle.Right;
        _refresh.Dock = DockStyle.Right;
        _refresh.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        _addNew.Click += (_, _) => AddNewRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(_status);
        Controls.Add(_refresh);
        Controls.Add(_addNew);
        ApplyTheme();
    }

    internal event EventHandler? RefreshRequested;
    internal event EventHandler? AddNewRequested;

    internal void Configure(bool showRefresh, bool showAddNew)
    {
        _refresh.Visible = showRefresh;
        _addNew.Visible = showAddNew;
    }

    internal void UpdateStatus(int position, int total, bool waiting, int minimumLength)
    {
        _status.Text = waiting ? $"Type at least {minimumLength} characters" : $"{Math.Max(0, position)} / {Math.Max(0, total)}";
    }

    private static void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.TabStop = false;
        button.AutoSize = true;
        button.Dock = DockStyle.Right;
        button.FlatStyle = FlatStyle.Flat;
        button.AccessibleName = text;
    }

    private void ApplyTheme()
    {
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        BackColor = colors.SurfaceSecondary;
        _status.BackColor = colors.SurfaceSecondary;
        _status.ForeColor = colors.MutedText;
        _refresh.BackColor = colors.SurfaceSecondary;
        _refresh.ForeColor = colors.Text;
        _addNew.BackColor = colors.SurfaceSecondary;
        _addNew.ForeColor = colors.Text;
    }
}
