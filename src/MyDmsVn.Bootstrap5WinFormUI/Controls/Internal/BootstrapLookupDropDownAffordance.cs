using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Theme;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapLookupDropDownAffordance : Control
{
    internal BootstrapLookupDropDownAffordance()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, false);
        TabStop = false;
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = "Open lookup results";
        Cursor = Cursors.Default;
    }

    internal event EventHandler? Activated;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && Enabled) Activated?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var color = Enabled ? BootstrapThemeManager.CurrentTheme.Colors.MutedText : BootstrapThemeManager.CurrentTheme.Colors.Disabled;
        var centerX = ClientSize.Width / 2;
        var centerY = ClientSize.Height / 2;
        using var pen = new Pen(color, 1.5f);
        e.Graphics.DrawLines(pen, new[]
        {
            new Point(centerX - 4, centerY - 2),
            new Point(centerX, centerY + 2),
            new Point(centerX + 4, centerY - 2)
        });
    }
}
