using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public sealed class MainForm : Form
{
    public MainForm()
    {
        Text = "MyDmsVn.Bootstrap5WinFormUI Demo";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(960, 640);
        MinimumSize = new Size(640, 480);

        Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "Phase 0 repository skeleton is ready.\r\nFeature demos begin in Phase 1."
        });
    }
}
