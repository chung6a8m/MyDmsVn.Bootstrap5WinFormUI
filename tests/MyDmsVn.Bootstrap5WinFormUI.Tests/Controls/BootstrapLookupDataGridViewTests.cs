using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapLookupDataGridViewTests
{
    [Test]
    public void ColumnUsesInternalCellAndEditingControlContracts()
    {
        using var column = new BootstrapLookupColumn();
        Assert.That(column, Is.InstanceOf<DataGridViewColumn>());
        Assert.That(column.CellTemplate!.GetType().IsPublic, Is.False);
        Assert.That(column.CellTemplate.EditType.IsPublic, Is.False);
        Assert.That(typeof(IDataGridViewEditingControl).IsAssignableFrom(column.CellTemplate.EditType), Is.True);
    }

    [Test]
    public void CloneOwnsIndependentConfigurationCollections()
    {
        using var column = CreateColumn(new BindingList<Product> { new Product(1, "Alpha") });
        column.LookupColumns.Add(new BootstrapLookupColumnDefinition { DataPropertyName = "Name" });
        column.SearchMembers.Add("Name");
        using var clone = (BootstrapLookupColumn)column.Clone();
        clone.LookupColumns[0].HeaderText = "Changed";
        clone.SearchMembers.Add("Code");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(clone.LookupColumns, Is.Not.SameAs(column.LookupColumns));
            Assert.That(clone.LookupColumns[0], Is.Not.SameAs(column.LookupColumns[0]));
            Assert.That(column.LookupColumns[0].HeaderText, Is.Empty);
            Assert.That(column.SearchMembers, Is.EqualTo(new[] { "Name" }));
        }));
    }

    [Test]
    public void CellFormatsDisplayButCommitsRawValueAndNotifiesDirtyOnce()
    {
        var products = new BindingList<Product> { new Product(1, "Alpha"), new Product(2, "Beta") };
        var host = CreateGridHost(CreateColumn(products), new Row { ProductId = 1 });
        using var form = host.Form;
        using var ownedGrid = host.Grid;
        var grid = host.Grid;
        var dirty = 0;
        grid.CurrentCellDirtyStateChanged += (_, _) => dirty++;
        BeginEdit(grid, 0);
        var editor = (BootstrapLookupBox)grid.EditingControl!;
        var columnEvents = 0;
        BootstrapLookupCellEventArgs? context = null;
        ((BootstrapLookupColumn)grid.Columns[0]).SelectionCommitted += (_, e) => { columnEvents++; context = e; };

        editor.SelectValue(2);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(editor.SelectedValue, Is.EqualTo(2));
            Assert.That(((IDataGridViewEditingControl)editor).EditingControlFormattedValue, Is.EqualTo(2));
            Assert.That(((IDataGridViewEditingControl)editor).EditingControlValueChanged, Is.True);
            Assert.That(dirty, Is.EqualTo(1));
            Assert.That(columnEvents, Is.EqualTo(1));
            Assert.That(context!.RowIndex, Is.EqualTo(0));
            Assert.That(context.Value, Is.EqualTo(2));
        }));
        grid.EndEdit();
    }

    [Test]
    public void ReusedEditorDetachesOldSourceAndForwardsOnlyCurrentColumn()
    {
        var sourceA = new BindingList<Product> { new Product(1, "Alpha") };
        var sourceB = new BindingList<Product> { new Product(2, "Beta") };
        var columnA = CreateColumn(sourceA); columnA.DataPropertyName = "ProductA";
        var columnB = CreateColumn(sourceB); columnB.DataPropertyName = "ProductB"; columnB.SearchMembers.Add("Code");
        using var form = new Form { ShowInTaskbar = false };
        using var grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, DataSource = new BindingList<Row> { new Row { ProductA = 1, ProductB = 2 } } };
        grid.Columns.Add(columnA); grid.Columns.Add(columnB); form.Controls.Add(grid); form.Show(); Application.DoEvents();
        BeginEdit(grid, 0);
        var reused = grid.EditingControl;
        grid.EndEdit(); grid.CurrentCell = grid.Rows[0].Cells[1]; grid.BeginEdit(true); Application.DoEvents();
        var editor = (BootstrapLookupBox)grid.EditingControl!;
        var aEvents = 0; var bEvents = 0;
        columnA.SelectionCommitted += (_, _) => aEvents++;
        columnB.SelectionCommitted += (_, _) => bEvents++;
        sourceA.Add(new Product(3, "Old source mutation"));
        editor.SelectValue(2);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(grid.EditingControl, Is.SameAs(reused));
            Assert.That(editor.DataSource, Is.SameAs(sourceB));
            Assert.That(editor.SearchMembers, Is.EqualTo(new[] { "Code" }));
            Assert.That(aEvents, Is.Zero);
            Assert.That(bEvents, Is.EqualTo(1));
        }));
        grid.EndEdit();
    }

    private static BootstrapLookupColumn CreateColumn(object source) => new BootstrapLookupColumn
    {
        DataSource = source, DisplayMember = "Name", ValueMember = "Id", DataPropertyName = "ProductId"
    };

    private static (Form Form, DataGridView Grid) CreateGridHost(BootstrapLookupColumn column, Row row)
    {
        var form = new Form { ShowInTaskbar = false };
        var grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, DataSource = new BindingList<Row> { row } };
        grid.Columns.Add(column); form.Controls.Add(grid); form.Show(); Application.DoEvents();
        return (form, grid);
    }

    private static void BeginEdit(DataGridView grid, int columnIndex)
    {
        grid.CurrentCell = grid.Rows[0].Cells[columnIndex];
        Assert.That(grid.BeginEdit(true), Is.True);
        Application.DoEvents();
        Assert.That(grid.EditingControl, Is.InstanceOf<BootstrapLookupBox>());
    }

    private sealed class Product
    {
        internal Product(int id, string name) { Id = id; Name = name; Code = name.Substring(0, 1); }
        public int Id { get; }
        public string Name { get; }
        public string Code { get; }
    }

    private sealed class Row
    {
        public int ProductId { get; set; }
        public int ProductA { get; set; }
        public int ProductB { get; set; }
    }
}
