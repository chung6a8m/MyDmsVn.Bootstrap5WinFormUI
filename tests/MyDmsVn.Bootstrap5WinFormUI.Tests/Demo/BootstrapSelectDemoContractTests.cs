using System;
using System.Collections.Generic;
using System.Linq;
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

        Assert.Multiple((Action)(() =>
        {
            Assert.That(localSingle.Items, Has.Count.GreaterThanOrEqualTo(4));
            Assert.That(localSingle.Items.Any(item => item.Disabled), Is.True);
            Assert.That(localMultiple.AllowCustomValues, Is.True);
            Assert.That(localMultiple.Items.Any(item => !string.IsNullOrEmpty(item.Group)), Is.True);
            Assert.That(asyncSingle.PageSize, Is.GreaterThan(0));
            Assert.That(asyncMultiple.PageSize, Is.GreaterThan(0));
            Assert.That(labels.Any(text => text.IndexOf("rapid", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
            Assert.That(labels.Any(text => text.IndexOf("retry", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
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

        Assert.Throws<InvalidOperationException>(() => providers[1]
            .SearchAsync(new BootstrapSelectQuery("fail-first", 1, 20), CancellationToken.None)
            .GetAwaiter()
            .GetResult());

        Assert.Throws<InvalidOperationException>(() => providers[1]
            .SearchAsync(new BootstrapSelectQuery("retry", 2, 20), CancellationToken.None)
            .GetAwaiter()
            .GetResult());
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
