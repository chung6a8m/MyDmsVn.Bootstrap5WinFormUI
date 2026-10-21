using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapLookupAffordanceRenderingTests
{
    [TestCase(BootstrapThemeMode.Light, false, true)]
    [TestCase(BootstrapThemeMode.Light, true, true)]
    [TestCase(BootstrapThemeMode.Light, false, false)]
    [TestCase(BootstrapThemeMode.Dark, false, true)]
    [TestCase(BootstrapThemeMode.Dark, true, true)]
    [TestCase(BootstrapThemeMode.Dark, false, false)]
    public void LookupAccessoryPaintsChevronOnOwnerSurfaceAcrossThemeAndLockedStates(BootstrapThemeMode mode, bool readOnly, bool enabled)
    {
        var previous = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            using var form = new Form { ShowInTaskbar = false, ClientSize = new Size(400, 160) };
            using var lookup = new BootstrapLookupBox { Width = 300, ReadOnly = readOnly, Enabled = enabled };
            DataGridViewTestGuard.FailOnDataError(lookup.ResultsGrid);
            form.Controls.Add(lookup);
            form.Show();
            Application.DoEvents();
            var accessory = lookup.Controls.Cast<Control>().Single(control => control.AccessibleName == "Open lookup results");
            Assert.That(accessory.TabStop, Is.False);
            Assert.That(accessory.Enabled, Is.EqualTo(enabled && !readOnly));
            var editor = lookup.Controls.OfType<TextBoxBase>().Single();
            if (enabled && !readOnly) editor.Focus();
            var editorHandle = editor.Handle;
            AssertPainted();
            var invalidated = 0;
            accessory.Invalidated += (_, _) => invalidated++;
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode == BootstrapThemeMode.Light ? BootstrapThemeMode.Dark : BootstrapThemeMode.Light);
            Application.DoEvents();
            Assert.That(invalidated, Is.GreaterThan(0), "Theme changes must invalidate the accessory.");
            AssertPainted();
            Assert.That(editor.Handle, Is.EqualTo(editorHandle));
            if (enabled && !readOnly) Assert.That(editor.Focused, Is.True);

            void AssertPainted()
            {
                var colors = BootstrapThemeManager.CurrentTheme.Colors;
                var surface = enabled && !readOnly ? colors.Surface : colors.SurfaceSecondary;
                using var bitmap = new Bitmap(accessory.Width, accessory.Height);
                accessory.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                Assert.That(bitmap.GetPixel(1, 1).ToArgb(), Is.EqualTo(surface.ToArgb()), "Accessory must share the lookup surface.");
                var glyphPixels = 0;
                for (var y = 2; y < bitmap.Height - 2; y++)
                    for (var x = 2; x < bitmap.Width - 2; x++)
                        if (bitmap.GetPixel(x, y).ToArgb() != surface.ToArgb()) glyphPixels++;
                Assert.That(glyphPixels, Is.GreaterThan(5), "Native WM_PRINT must include the chevron, not a blank square.");
            }
        }
        finally { BootstrapThemeManager.CurrentTheme = previous; }
    }
}
