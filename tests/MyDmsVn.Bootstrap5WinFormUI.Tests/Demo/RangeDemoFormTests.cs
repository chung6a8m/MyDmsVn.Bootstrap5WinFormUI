using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class RangeDemoFormTests
{
    private BootstrapTheme _originalTheme = null!;

    [SetUp]
    public void SetUp() => _originalTheme = BootstrapThemeManager.CurrentTheme;

    [TearDown]
    public void TearDown() => BootstrapThemeManager.CurrentTheme = _originalTheme;

    [Test]
    public void DemoConstructsAllRangeScenariosWithoutModalUi()
    {
        using var form = new RangeDemoForm();
        form.Show();
        Application.DoEvents();
        form.PerformLayout();
        var ranges = FindControls<BootstrapRange>(form).ToArray();
        var labels = FindControls<Label>(form).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(ranges.Any(range => range.TickStyle == TickStyle.None && range.Orientation == Orientation.Horizontal), Is.True);
            Assert.That(ranges.Any(range => range.TickStyle != TickStyle.None && range.Orientation == Orientation.Horizontal), Is.True);
            Assert.That(ranges.Any(range => range.Orientation == Orientation.Vertical), Is.True);
            Assert.That(ranges.Any(range => !range.Enabled), Is.True);
            Assert.That(ranges.Any(range => range.RightToLeft == RightToLeft.Yes && range.RightToLeftLayout), Is.True);
            Assert.That(ranges.Select(range => range.Variant).Distinct().Count(), Is.EqualTo(8));
            Assert.That(labels.Any(label => label.Text.Contains("PageUp/PageDown")), Is.True);
            Assert.That(labels.Any(label => label.Text.Contains("100%") && label.Text.Contains("200%")), Is.True);
        }));
    }

    [Test]
    public void DemoProvidesLiveValueDiagnosticsAndThemeSwitching()
    {
        using var form = new RangeDemoForm();
        form.Show();
        Application.DoEvents();
        var range = FindControls<BootstrapRange>(form)
            .Single(control => control.AccessibleName == "Interactive range diagnostics");
        var status = FindControls<Label>(form)
            .Single(label => label.AccessibleName == "Range event diagnostics");

        range.Value++;
        Assert.That(status.Text, Does.Contain($"Value: {range.Value}"));
        Assert.That(status.Text, Does.Contain("ValueChanged: 1"));

        FindControls<Button>(form).Single(button => button.Text == "Use Dark theme").PerformClick();
        Assert.That(BootstrapThemeManager.CurrentTheme.Mode, Is.EqualTo(BootstrapThemeMode.Dark));
        FindControls<Button>(form).Single(button => button.Text == "Use Light theme").PerformClick();
        Assert.That(BootstrapThemeManager.CurrentTheme.Mode, Is.EqualTo(BootstrapThemeMode.Light));
    }

    private static IEnumerable<T> FindControls<T>(Control root)
        where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindControls<T>(child))
            {
                yield return nested;
            }
        }
    }
}
