using System.Drawing;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

/// <summary>
/// Provides the shared DPI and native body-font policy for Integrated Demo forms.
/// </summary>
public abstract class DemoFormBase : Form
{
    private Font? _demoBodyFont;

    /// <summary>
    /// Initializes an Integrated Demo form with DPI autoscaling and browser-equivalent body typography.
    /// </summary>
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
