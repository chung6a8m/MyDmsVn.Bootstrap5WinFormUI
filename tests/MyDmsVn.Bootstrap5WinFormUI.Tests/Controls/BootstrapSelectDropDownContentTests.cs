using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapSelectDropDownContentTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void SearchFieldUsesThemedWrapperWithBorderlessNativeEditor()
    {
        using var content = CreatePresentedContent(96);

        var search = Descendants(content).OfType<BootstrapTextBox>().Single();
        var native = Descendants(search).OfType<TextBox>().Single();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.BorderStyle, Is.EqualTo(BorderStyle.None));
            Assert.That(search.Left, Is.GreaterThan(0));
            Assert.That(search.Top, Is.GreaterThan(0));
            Assert.That(search.Right, Is.LessThan(content.ClientSize.Width));
        }));
    }

    [TestCase(96)]
    [TestCase(120)]
    [TestCase(144)]
    [TestCase(192)]
    public void SearchHostScalesOnlyItsOwnedInsetAndHeightMetrics(int dpi)
    {
        using var content = CreatePresentedContent(dpi);
        var theme = BootstrapThemeManager.CurrentTheme;
        var search = Descendants(content).OfType<BootstrapTextBox>().Single();
        var searchHost = search.Parent!;
        var results = Descendants(content).OfType<BootstrapSelectResultsView>().Single();
        var expectedInset = DpiScaler.Scale(theme.Metrics.SpacingXS, dpi);
        var expectedFieldHeight = DpiScaler.Scale(theme.Metrics.ControlHeightSmall, dpi);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(searchHost.Padding, Is.EqualTo(new Padding(expectedInset)));
            Assert.That(searchHost.Height, Is.EqualTo(expectedFieldHeight + (expectedInset * 2)));
            Assert.That(search.Height, Is.EqualTo(expectedFieldHeight));
            Assert.That(results.Dock, Is.EqualTo(DockStyle.Fill));
            Assert.That(results.Left, Is.EqualTo(0));
            Assert.That(results.Width, Is.EqualTo(content.ClientSize.Width));
        }));
    }

    [Test]
    public void DisablingSearchRemovesTheBandAndRestoresTheSameWrapperWhenReenabled()
    {
        using var content = CreatePresentedContent(96);
        var search = Descendants(content).OfType<BootstrapTextBox>().Single();
        var searchHost = search.Parent!;
        var results = Descendants(content).OfType<BootstrapSelectResultsView>().Single();

        content.SearchEnabled = false;
        content.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(searchHost.Visible, Is.False);
            Assert.That(results.Bounds, Is.EqualTo(content.ClientRectangle));
        }));

        content.SearchEnabled = true;
        content.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(searchHost.Visible, Is.True);
            Assert.That(Descendants(content).OfType<BootstrapTextBox>().Single(), Is.SameAs(search));
        }));
    }

    [Test]
    public void SearchTextAndSilentClearPreserveLogicalEventSemantics()
    {
        using var content = CreatePresentedContent(96);
        var events = new List<string>();
        content.SearchTextChanged += text => events.Add(text);

        content.SearchText = "Northwind";
        content.ClearSearchSilently();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(content.SearchText, Is.Empty);
            Assert.That(events, Is.EqualTo(new[] { "Northwind" }));
        }));
    }

    [Test]
    public void RealThemeManagerSwitchRethemesSameSearchCompositionWithoutLosingState()
    {
        using var content = new BootstrapSelectDropDownContent
        {
            Size = new Size(340, 180)
        };

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
        content.ApplyPresentation(
            new BootstrapSelectRenderer(),
            BootstrapThemeManager.CurrentTheme,
            96);
        content.SearchText = "Northwind";
        content.PerformLayout();

        var search = Descendants(content).OfType<BootstrapTextBox>().Single();
        var native = Descendants(search).OfType<TextBox>().Single();
        var results = Descendants(content).OfType<BootstrapSelectResultsView>().Single();
        var lightHostColor = search.Parent!.BackColor;

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        content.ApplyPresentation(
            new BootstrapSelectRenderer(),
            BootstrapThemeManager.CurrentTheme,
            96);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Descendants(content).OfType<BootstrapTextBox>().Single(), Is.SameAs(search));
            Assert.That(Descendants(content).OfType<BootstrapSelectResultsView>().Single(), Is.SameAs(results));
            Assert.That(content.SearchText, Is.EqualTo("Northwind"));
            Assert.That(native.BorderStyle, Is.EqualTo(BorderStyle.None));
            Assert.That(search.Parent!.BackColor, Is.EqualTo(BootstrapThemeManager.CurrentTheme.Colors.Surface));
            Assert.That(search.Parent!.BackColor, Is.Not.EqualTo(lightHostColor));
            Assert.That(results.Left, Is.EqualTo(0));
            Assert.That(results.Width, Is.EqualTo(content.ClientSize.Width));
        }));
    }

    private static BootstrapSelectDropDownContent CreatePresentedContent(int dpi)
    {
        var content = new BootstrapSelectDropDownContent
        {
            Size = new Size(340, 180)
        };
        content.ApplyPresentation(
            new BootstrapSelectRenderer(),
            BootstrapThemeManager.CurrentTheme,
            dpi);
        content.PerformLayout();
        return content;
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
}
