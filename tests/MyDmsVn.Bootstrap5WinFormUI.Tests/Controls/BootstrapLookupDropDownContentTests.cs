using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class BootstrapLookupDropDownContentTests
{
    [Test]
    public void ResultsGridEnforcesReadOnlySingleRowNonTabInvariants()
    {
        using var lookup = new BootstrapLookupBox();
        var grid = lookup.ResultsGrid;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(grid.ReadOnly, Is.True);
            Assert.That(grid.MultiSelect, Is.False);
            Assert.That(grid.SelectionMode, Is.EqualTo(DataGridViewSelectionMode.FullRowSelect));
            Assert.That(grid.AllowUserToAddRows, Is.False);
            Assert.That(grid.AllowUserToDeleteRows, Is.False);
            Assert.That(grid.RowHeadersVisible, Is.False);
            Assert.That(grid.TabStop, Is.False);
            Assert.That(TypeDescriptor.GetProperties(lookup)[nameof(BootstrapLookupBox.ResultsGrid)]!.IsBrowsable, Is.False);
            Assert.That(TypeDescriptor.GetProperties(lookup)[nameof(BootstrapLookupBox.ResultsGrid)]!.SerializationVisibility, Is.EqualTo(DesignerSerializationVisibility.Hidden));
        }));
    }

    [Test]
    public void ReapplyingUnchangedColumnConfigurationKeepsNativeColumns()
    {
        using var content = new BootstrapLookupDropDownContent();
        var definitions = new BootstrapLookupColumnDefinitionCollection
        {
            new BootstrapLookupColumnDefinition { DataPropertyName = "Name", HeaderText = "Product" }
        };
        content.ApplyColumns(definitions, true);
        var first = content.ResultsGrid.Columns[0];
        content.ApplyColumns(definitions, true);
        Assert.That(content.ResultsGrid.Columns[0], Is.SameAs(first));
    }

    [Test]
    public void FooterAlwaysExistsAndButtonsAreIndependentNonTabStops()
    {
        using var content = new BootstrapLookupDropDownContent();
        content.ConfigureFooter(showRefresh: true, showAddNew: false);
        content.UpdateStatus(2, 5, false, 0);
        var footer = content.Controls.Cast<Control>().Single(control => control.GetType().Name == "BootstrapLookupFooter");
        var buttons = footer.Controls.OfType<Button>().ToArray();
        var label = footer.Controls.OfType<Label>().Single();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(footer.Visible, Is.True);
            Assert.That(label.Text, Is.EqualTo("2 / 5"));
            Assert.That(buttons.Single(button => button.Text == "Refresh").Visible, Is.True);
            Assert.That(buttons.Single(button => button.Text == "Add New").Visible, Is.False);
            Assert.That(buttons, Has.All.Property(nameof(Control.TabStop)).False);
        }));
    }
}
