using System;
using System.ComponentModel;
using System.Drawing;
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

    [TestCase(1, 86)]
    [TestCase(3, 146)]
    public void PreferredHeightIncludesFooterHeaderAndEveryUncappedRow(int rowCount, int expectedHeight)
    {
        using var content = CreateSizedContent(rowCount);
        var preferred = content.GetPreferredSize(new Size(300, 320));
        content.Size = preferred;
        content.CreateControl();
        content.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(preferred.Height, Is.EqualTo(expectedHeight));
            Assert.That(content.ResultsGrid.DisplayedRowCount(false), Is.EqualTo(rowCount));
        }));
    }

    [Test]
    public void PreferredHeightStopsAtAvailableHeightWhenRowsRequireScrolling()
    {
        using var content = CreateSizedContent(12);
        var preferred = content.GetPreferredSize(new Size(300, 200));
        content.Size = preferred;
        content.CreateControl();
        content.PerformLayout();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(preferred.Height, Is.EqualTo(200));
            Assert.That(content.ResultsGrid.DisplayedRowCount(false), Is.GreaterThan(0).And.LessThan(12));
        }));
    }

    private static BootstrapLookupDropDownContent CreateSizedContent(int rowCount)
    {
        var content = new BootstrapLookupDropDownContent();
        content.ResultsGrid.BorderStyle = BorderStyle.None;
        content.ResultsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        content.ResultsGrid.ColumnHeadersHeight = 24;
        content.ResultsGrid.RowTemplate.Height = 30;
        content.ApplyColumns(new BootstrapLookupColumnDefinitionCollection
        {
            new BootstrapLookupColumnDefinition { DataPropertyName = "Name", HeaderText = "Product" }
        }, true);
        content.ApplyResults(Enumerable.Range(1, rowCount)
            .Select(index => new BootstrapLookupSourceItem(new SizedItem(index), index, $"Item {index}", index - 1))
            .ToArray());
        return content;
    }

    private sealed class SizedItem
    {
        internal SizedItem(int id) { Id = id; Name = $"Item {id}"; }
        public int Id { get; }
        public string Name { get; }
    }
}
