using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

public abstract class DemoFormBase : Form
{
    private Font? _demoBodyFont;

    protected DemoFormBase()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        _demoBodyFont = DemoTypography.CreateBodyFont();
        Font = _demoBodyFont;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _demoBodyFont?.Dispose();
            _demoBodyFont = null;
        }
    }
}
