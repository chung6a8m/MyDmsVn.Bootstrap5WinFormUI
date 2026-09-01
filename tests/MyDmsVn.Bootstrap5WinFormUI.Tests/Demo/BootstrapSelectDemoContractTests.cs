using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSelectDemoContractTests
{
    [Test]
    public void DemoExposesProductSearchWithCustomResultTemplate()
    {
        using var form = new BootstrapSelectDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var productSelect = FindControls<BootstrapSelect>(form)
            .Single(select => select.Name == "productSearchSelect");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(productSelect.ResultRowHeight, Is.EqualTo(48));
            Assert.That(productSelect.Renderer.GetType().Name, Is.EqualTo("BootstrapSelectProductRenderer"));
            Assert.That(productSelect.Items, Has.Count.GreaterThanOrEqualTo(3));
            Assert.That(productSelect.Items.All(item => item.Tag?.GetType().Name == "BootstrapSelectProduct"), Is.True);
            Assert.That(productSelect.Items.All(item =>
                item.Text == (string?)item.Tag?.GetType().GetProperty("Name")?.GetValue(item.Tag)), Is.True);
        }));
    }

    [TestCase(96)]
    [TestCase(144)]
    [TestCase(192)]
    public void ProductResultLayoutKeepsPrimaryAndSecondaryLinesContainedAndSeparate(int dpi)
    {
        var helperType = typeof(BootstrapSelectDemoForm).Assembly.GetType(
            "MyDmsVn.Bootstrap5WinFormUI.Demo.BootstrapSelectProductResultLayout",
            throwOnError: true)!;
        var calculate = helperType.GetMethod("Calculate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        using var bitmap = new Bitmap(640, 200);
        using var graphics = Graphics.FromImage(bitmap);
        using var primaryFont = new Font(Control.DefaultFont, FontStyle.Regular);
        using var secondaryFont = new Font(Control.DefaultFont.FontFamily, 8f, FontStyle.Regular);
        var bounds = new Rectangle(0, 0, Scale(420, dpi), Scale(48, dpi));

        var layout = calculate.Invoke(null, new object[] { graphics, bounds, dpi, primaryFont, secondaryFont })!;
        var nameBounds = ReadRectangle(layout, "NameBounds");
        var detailsBounds = ReadRectangle(layout, "DetailsBounds");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bounds.Contains(nameBounds), Is.True);
            Assert.That(bounds.Contains(detailsBounds), Is.True);
            Assert.That(nameBounds.Bottom, Is.LessThanOrEqualTo(detailsBounds.Top));
            Assert.That(nameBounds.Height, Is.GreaterThan(0));
            Assert.That(detailsBounds.Height, Is.GreaterThan(0));
        }));
    }

    [Test]
    public void DemoExposesRequiredLocalAndAsyncScenarios()
    {
        using var form = new BootstrapSelectDemoForm();
        form.CreateControl();
        form.PerformLayout();

        var selects = FindControls<BootstrapSelect>(form).ToArray();
        var labels = FindControls<Label>(form).Select(label => label.Text).ToArray();

        var localSingle = selects.Single(select =>
            select.DataProvider is null &&
            select.SelectionMode == BootstrapSelectMode.Single &&
            select.AccessibleName == "Local customer select");
        var localMultiple = selects.Single(select =>
            select.DataProvider is null &&
            select.SelectionMode == BootstrapSelectMode.Multiple);
        var asyncSingle = selects.Single(select =>
            select.DataProvider is not null &&
            select.SelectionMode == BootstrapSelectMode.Single);
        var asyncMultiple = selects.Single(select =>
            select.DataProvider is not null &&
            select.SelectionMode == BootstrapSelectMode.Multiple);
        var placementSelect = selects.Single(select =>
            select.AccessibleName == "Lower-right placement select");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(localSingle.Items, Has.Count.GreaterThanOrEqualTo(5));
            Assert.That(localSingle.AllowClear, Is.True);
            Assert.That(localSingle.Items.Any(item => item.Disabled), Is.True);
            Assert.That(localSingle.Items.Any(item => item.Text.Length > 60), Is.True);
            Assert.That(localMultiple.AllowCustomValues, Is.True);
            Assert.That(localMultiple.Items.Any(item => !string.IsNullOrEmpty(item.Group)), Is.True);
            Assert.That(asyncSingle.PageSize, Is.EqualTo(20));
            Assert.That(asyncMultiple.PageSize, Is.EqualTo(20));
            Assert.That(asyncMultiple.SelectedValues, Does.Contain(1));
            Assert.That(placementSelect.Anchor & AnchorStyles.Right, Is.EqualTo(AnchorStyles.Right));
            Assert.That(placementSelect.Anchor & AnchorStyles.Bottom, Is.EqualTo(AnchorStyles.Bottom));
            Assert.That(labels.Any(text => text.IndexOf("rapid", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
            Assert.That(labels.Any(text => text.IndexOf("retry", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
            Assert.That(labels.Any(text => text.IndexOf("flip", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
        }));
    }

    [Test]
    public void DemoProviderOffersLargePagedDataAndFirstAndLaterPageFailures()
    {
        using var form = new BootstrapSelectDemoForm();
        form.CreateControl();

        var providers = FindControls<BootstrapSelect>(form)
            .Where(select => select.DataProvider is not null)
            .Select(select => select.DataProvider!)
            .ToArray();

        Assert.That(providers, Has.Length.GreaterThanOrEqualTo(2));

        var largePage = providers[0]
            .SearchAsync(new BootstrapSelectQuery(string.Empty, 1, 500), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        Assert.That(largePage.Items, Has.Count.GreaterThanOrEqualTo(200));

        Assert.Throws<InvalidOperationException>((Action)(() => providers[1]
            .SearchAsync(new BootstrapSelectQuery("fail-first", 1, 20), CancellationToken.None)
            .GetAwaiter()
            .GetResult()));

        Assert.Throws<InvalidOperationException>((Action)(() => providers[1]
            .SearchAsync(new BootstrapSelectQuery("retry", 2, 20), CancellationToken.None)
            .GetAwaiter()
            .GetResult()));
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

    private static Rectangle ReadRectangle(object instance, string propertyName)
    {
        return (Rectangle)instance.GetType().GetProperty(propertyName)!.GetValue(instance)!;
    }

    private static int Scale(int logicalPixels, int dpi)
    {
        return (int)Math.Round(logicalPixels * dpi / 96d, MidpointRounding.AwayFromZero);
    }
}
