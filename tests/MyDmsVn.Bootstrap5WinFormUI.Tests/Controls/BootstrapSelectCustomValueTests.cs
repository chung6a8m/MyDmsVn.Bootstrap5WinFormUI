using System;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSelectCustomValueTests
{
    [Test]
    public void CreateRowIsOptInAndSuppressesWhitespaceAndExactText()
    {
        using var select = new BootstrapSelect();
        select.Items.Add(new BootstrapSelectItem(1, "Alpha"));

        Assert.That(select.BuildCurrentLocalResultSet("New").Rows.Any(x => x.Kind == BootstrapSelectResultRowKind.CreateValue), Is.False);
        select.AllowCustomValues = true;
        Assert.That(select.BuildCurrentLocalResultSet("   ").Rows.Any(x => x.Kind == BootstrapSelectResultRowKind.CreateValue), Is.False);
        Assert.That(select.BuildCurrentLocalResultSet("alpha").Rows.Any(x => x.Kind == BootstrapSelectResultRowKind.CreateValue), Is.False);
        Assert.That(select.BuildCurrentLocalResultSet("New").Rows.Single(x => x.Kind == BootstrapSelectResultRowKind.CreateValue).Text, Is.EqualTo("Create 'New'"));
    }

    [Test]
    public void PartialMatchStillOffersCreateRow()
    {
        using var select = new BootstrapSelect { AllowCustomValues = true };
        select.Items.Add(new BootstrapSelectItem(1, "Alpha Beta"));

        var rows = select.BuildCurrentLocalResultSet("Alpha").Rows;

        Assert.That(rows.Any(x => x.Kind == BootstrapSelectResultRowKind.Item), Is.True);
        Assert.That(rows.Any(x => x.Kind == BootstrapSelectResultRowKind.CreateValue), Is.True);
    }

    [Test]
    public void NullFactoryResultRejectsCreationWithoutSelection()
    {
        using var select = new BootstrapSelect { AllowCustomValues = true, CustomValueFactory = _ => null };
        var row = select.BuildCurrentLocalResultSet("Gamma").Rows.Single(x => x.Kind == BootstrapSelectResultRowKind.CreateValue);

        Assert.That(select.ActivateResultRow(row, BootstrapSelectChangeReason.Keyboard), Is.False);
        Assert.That(select.SelectedItems, Is.Empty);
    }

    [Test]
    public void SuccessfulFactoryUsesCustomValueReasonAndNormalSelectionPipeline()
    {
        using var select = new BootstrapSelect
        {
            AllowCustomValues = true,
            CustomValueFactory = text => new BootstrapSelectItem(text.ToUpperInvariant(), text)
        };
        BootstrapSelectChangeReason? reason = null;
        select.Selecting += (_, e) => reason = e.Reason;
        var row = select.BuildCurrentLocalResultSet("Gamma").Rows.Single(x => x.Kind == BootstrapSelectResultRowKind.CreateValue);

        Assert.That(select.ActivateResultRow(row, BootstrapSelectChangeReason.Keyboard), Is.True);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.SelectedValue, Is.EqualTo("GAMMA"));
            Assert.That(select.SelectedItem!.Text, Is.EqualTo("Gamma"));
            Assert.That(reason, Is.EqualTo(BootstrapSelectChangeReason.CustomValue));
        }));
    }
}
