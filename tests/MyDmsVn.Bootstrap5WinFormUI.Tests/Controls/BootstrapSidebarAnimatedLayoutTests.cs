using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSidebarAnimatedLayoutTests
{
    [Test]
    public void AnimatedCollapseAndExpandKeepNavigationRowsInVisibleLayout()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light, reducedMotion: false);
        try
        {
            using var form = new Form
            {
                ClientSize = new System.Drawing.Size(900, 600),
                ShowInTaskbar = false
            };
            using var sidebar = new BootstrapSidebar
            {
                Dock = DockStyle.Left,
                ExpandedWidth = 260,
                CollapsedWidth = 72,
                AnimationDuration = TimeSpan.FromMilliseconds(40)
            };

            var home = new BootstrapSidebarItem
            {
                Text = "Home",
                Icon = IconDescriptor.SegoeMdl2('\uE80F')
            };
            var reports = new BootstrapSidebarItem
            {
                Text = "Reports",
                Icon = IconDescriptor.SegoeMdl2('\uE9D2'),
                Expanded = true
            };
            reports.Items.Add(new BootstrapSidebarItem { Text = "Sales" });
            sidebar.Items.Add(home);
            sidebar.Items.Add(reports);
            sidebar.SelectedItem = home;
            form.Controls.Add(sidebar);

            form.Show();
            PumpMessagesUntil(() => sidebar.IsHandleCreated && sidebar.Width == sidebar.ExpandedWidth, TimeSpan.FromSeconds(2));
            AssertRowsVisible(sidebar, "initial expanded state");

            sidebar.Collapse();
            PumpMessagesUntil(() => sidebar.Width == sidebar.CollapsedWidth, TimeSpan.FromSeconds(2));
            AssertRowsVisible(sidebar, "after animated collapse");

            sidebar.Expand();
            PumpMessagesUntil(() => sidebar.Width == sidebar.ExpandedWidth, TimeSpan.FromSeconds(2));
            AssertRowsVisible(sidebar, "after animated re-expand");
            Assert.That(FindButton(sidebar, home).Text, Is.EqualTo("Home"));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    private static void AssertRowsVisible(BootstrapSidebar sidebar, string state)
    {
        var host = sidebar.Controls.OfType<FlowLayoutPanel>().Single();
        var rootButtons = host.Controls.OfType<BootstrapButton>().ToArray();
        var widths = string.Join("; ", rootButtons.Select(button => $"{button.Text}:{button.Width}"));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.Visible, Is.True, $"Navigation host became hidden {state}.");
            Assert.That(host.Height, Is.GreaterThan(0), $"Navigation host collapsed to zero height {state}.");
            Assert.That(host.Width, Is.GreaterThan(1), $"Navigation host collapsed to unusable width {state}. Bounds={host.Bounds}, min={host.MinimumSize}, max={host.MaximumSize}.");
            Assert.That(rootButtons, Is.Not.Empty, $"Navigation rows disappeared {state}.");
            Assert.That(rootButtons.All(button => button.Visible), Is.True, $"A root navigation row became hidden {state}.");
            Assert.That(
                rootButtons.All(button => host.ClientRectangle.IntersectsWith(button.Bounds)),
                Is.True,
                $"A root navigation row moved outside the visible host bounds {state}. Host={host.Bounds}; rows={string.Join("; ", rootButtons.Select(button => button.Bounds.ToString()))}");
            Assert.That(
                rootButtons.All(button => button.Width == Math.Max(1, host.ClientSize.Width - host.Padding.Horizontal - button.Margin.Horizontal)),
                Is.True,
                $"Root navigation row width stopped tracking the host {state}. Host={host.Bounds}, min={host.MinimumSize}, max={host.MaximumSize}; row widths={widths}.");
        }));
    }

    private static BootstrapButton FindButton(Control root, BootstrapSidebarItem item)
    {
        return Descendants(root)
            .OfType<BootstrapButton>()
            .Single(button => ReferenceEquals(button.Tag, item));
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static void PumpMessagesUntil(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition() && stopwatch.Elapsed < timeout)
        {
            Application.DoEvents();
            Thread.Sleep(5);
        }

        Application.DoEvents();
        Assert.That(condition(), Is.True, $"Condition was not reached within {timeout.TotalSeconds:0.##} seconds.");
    }
}
