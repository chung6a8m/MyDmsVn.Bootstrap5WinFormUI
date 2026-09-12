using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapContextMenuStripInteractionTests
{
    [Test]
    public void ShowUsesCurrentNativeSourceControlForEachOwner()
    {
        using var host = new WinFormsMessageLoopTestHost();
        host.Run(() =>
        {
            using var form = new Form { Size = new Size(400, 200) };
            using var first = new Panel { Size = new Size(100, 50) };
            using var second = new Panel { Location = new Point(120, 0), Size = new Size(100, 50) };
            using var menu = new BootstrapContextMenuStrip();
            menu.Items.Add("Action");
            first.ContextMenuStrip = menu;
            second.ContextMenuStrip = menu;
            form.Controls.AddRange(new Control[] { first, second });
            form.Show();

            menu.Show(first, Point.Empty);
            Application.DoEvents();
            Assert.That(menu.SourceControl, Is.SameAs(first));
            menu.Close();
            menu.Show(second, Point.Empty);
            Application.DoEvents();
            Assert.That(menu.SourceControl, Is.SameAs(second));
            menu.Close();
            form.Close();
        });
    }

    [Test]
    public void NativeOpeningAndClosingCancellationAndEventOrderRemainIntact()
    {
        using var host = new WinFormsMessageLoopTestHost();
        host.Run(() =>
        {
            using var form = new Form { Size = new Size(300, 160) };
            using var panel = new Panel { Dock = DockStyle.Fill };
            using var menu = new BootstrapContextMenuStrip();
            menu.Items.Add("Action");
            form.Controls.Add(panel);
            form.Show();
            var events = new List<string>();
            var cancelOpening = true;
            menu.Opening += (_, e) => { events.Add("Opening"); e.Cancel = cancelOpening; };
            menu.Opened += (_, _) => events.Add("Opened");
            menu.Closing += (_, _) => events.Add("Closing");
            menu.Closed += (_, _) => events.Add("Closed");

            menu.Show(panel, Point.Empty);
            Application.DoEvents();
            Assert.That(events, Is.EqualTo(new[] { "Opening" }));
            cancelOpening = false;
            events.Clear();
            menu.Show(panel, Point.Empty);
            Application.DoEvents();
            menu.Close();
            Application.DoEvents();

            Assert.That(events, Is.EqualTo(new[] { "Opening", "Opened", "Closing", "Closed" }));
            form.Close();
        });
    }

    [Test]
    public void ThemeAndVariantChangeKeepSameOpenNativeMenuAndItems()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var host = new WinFormsMessageLoopTestHost();
            host.Run(() =>
            {
                using var form = new Form { Size = new Size(300, 160) };
                using var panel = new Panel { Dock = DockStyle.Fill };
                using var menu = new BootstrapContextMenuStrip();
                var item = new ToolStripMenuItem("Checked") { Checked = true };
                var nested = new ToolStripMenuItem("Nested");
                nested.DropDownItems.Add("Child");
                menu.Items.AddRange(new ToolStripItem[] { item, new ToolStripSeparator(), nested });
                form.Controls.Add(panel);
                form.Show();
                menu.Show(panel, Point.Empty);
                Application.DoEvents();

                menu.Variant = BootstrapVariant.Danger;
                BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
                Application.DoEvents();

                Assert.Multiple((Action)(() =>
                {
                    Assert.That(menu.Visible, Is.True);
                    Assert.That(menu.Items[0], Is.SameAs(item));
                    Assert.That(menu.Renderer, Is.TypeOf<BootstrapToolStripRenderer>());
                }));
                menu.Close();
                form.Close();
            });
        }
        finally { BootstrapThemeManager.CurrentTheme = originalTheme; }
    }
}
