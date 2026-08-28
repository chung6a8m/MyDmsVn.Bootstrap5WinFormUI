using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapDropdownTests
{
    [Test]
    public void NativeDropDownUsesAutoCloseAndForwardsOneOpenCloseTransition()
    {
        var button = new Button { Text = "Open" };
        using var form = CreateHost(button);
        using var menu = new ToolStripDropDownMenu();
        menu.Items.Add(new ToolStripMenuItem("Action"));

        var opened = 0;
        var closed = 0;
        menu.Opened += (_, _) => opened++;
        menu.Closed += (_, _) => closed++;

        Assert.That(menu.AutoClose, Is.True);

        menu.Show(button, new Point(0, button.Height));
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(menu.Visible, Is.True);
            Assert.That(opened, Is.EqualTo(1));
        }));

        menu.Close();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(menu.Visible, Is.False);
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NativeMenuItemsPreserveCheckedEnabledSeparatorAndNonTogglePolicy()
    {
        using var menu = new ToolStripDropDownMenu();
        var checkedItem = new ToolStripMenuItem("Checked")
        {
            Checked = true,
            Enabled = true,
            CheckOnClick = false
        };
        var disabledItem = new ToolStripMenuItem("Disabled") { Enabled = false };
        var separator = new ToolStripSeparator();
        var clickCount = 0;
        checkedItem.Click += (_, _) => clickCount++;

        menu.Items.Add(checkedItem);
        menu.Items.Add(disabledItem);
        menu.Items.Add(separator);

        checkedItem.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(menu.Items[0], Is.SameAs(checkedItem));
            Assert.That(menu.Items[1], Is.SameAs(disabledItem));
            Assert.That(menu.Items[2], Is.SameAs(separator));
            Assert.That(checkedItem.Checked, Is.True);
            Assert.That(checkedItem.CheckOnClick, Is.False);
            Assert.That(checkedItem.Enabled, Is.True);
            Assert.That(disabledItem.Enabled, Is.False);
            Assert.That(menu.Items[2], Is.TypeOf<ToolStripSeparator>());
            Assert.That(clickCount, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NativeCloseIsIdempotent()
    {
        var button = new Button { Text = "Open" };
        using var form = CreateHost(button);
        using var menu = new ToolStripDropDownMenu();
        menu.Items.Add(new ToolStripMenuItem("Action"));

        var closed = 0;
        menu.Closed += (_, _) => closed++;
        menu.Show(button, new Point(0, button.Height));
        Application.DoEvents();

        menu.Close();
        Application.DoEvents();
        menu.Close();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(menu.Visible, Is.False);
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    private static Form CreateHost(Control target)
    {
        var form = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(100, 100),
            Size = new Size(500, 300)
        };
        target.Location = new Point(24, 24);
        form.Controls.Add(target);
        form.Show();
        Application.DoEvents();
        return form;
    }
}
