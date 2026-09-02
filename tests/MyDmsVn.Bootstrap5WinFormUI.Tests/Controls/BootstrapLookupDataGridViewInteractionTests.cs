using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapLookupDataGridViewInteractionTests
{
    [Test]
    public void ValidTabCommitsRawValueAndUsesNativeNextEditableCell()
    {
        using var host = new GridHost();
        var editor = host.BeginLookupEdit();
        editor.Text = "Beta";

        Assert.That(SendDialogKey(editor, Keys.Tab), Is.True);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.Grid.CurrentCell!.ColumnIndex, Is.EqualTo(2), "Hidden/read-only cells must be skipped by native traversal.");
            Assert.That(host.Rows[0].ProductId, Is.EqualTo(2));
        }));
    }

    [Test]
    public void InvalidTabKeepsCurrentCellAndDoesNotMutateModel()
    {
        using var host = new GridHost();
        host.LookupColumn.UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.KeepFocusWithValidationError;
        var editor = host.BeginLookupEdit();
        editor.Text = "Unknown";

        Assert.That(SendDialogKey(editor, Keys.Tab), Is.True);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.Grid.CurrentCell!.ColumnIndex, Is.Zero);
            Assert.That(host.Rows[0].ProductId, Is.EqualTo(1));
            Assert.That(editor.ValidationMessage, Is.Not.Empty);
        }));
        host.Grid.CancelEdit();
    }

    [Test]
    public void EscapeRestoresPendingTextWithoutRollingBackCommittedValue()
    {
        using var host = new GridHost();
        var editor = host.BeginLookupEdit();
        editor.SelectValue(2);
        editor.Text = "Unknown";

        SendKey(editor, Keys.Escape);

        Assert.That(editor.SelectedValue, Is.EqualTo(2));
        Assert.That(editor.Text, Is.EqualTo("Beta"));
        host.Grid.EndEdit();
        Assert.That(host.Rows[0].ProductId, Is.EqualTo(2));
    }

    [Test]
    public void SearchAndHighlightDoNotMoveBindingSourceCurrency()
    {
        using var host = new GridHost(twoRows: true);
        host.Binding.Position = 1;
        var editor = host.BeginLookupEdit(rowIndex: 1);
        editor.Text = "a";
        editor.ExecuteSearchNow();
        editor.NavigateResults(Keys.End);

        Assert.That(host.Binding.Position, Is.EqualTo(1));
        host.Grid.CancelEdit();
    }

    [Test]
    public void NativeNewRowLookupCommitCreatesOneTypedRowAndKeepsPlaceholder()
    {
        using var host = new GridHost(empty: true);
        var editor = host.BeginLookupEdit();
        editor.SelectValue(2);
        Assert.That(SendDialogKey(editor, Keys.Tab), Is.True);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.Rows, Has.Count.EqualTo(1));
            Assert.That(host.Rows[0].ProductId, Is.EqualTo(2));
            Assert.That(host.Grid.Rows.Cast<DataGridViewRow>().Count(row => row.IsNewRow), Is.EqualTo(1));
        }));
    }

    [Test]
    public void ClosedEnterCanDelegateToDataGridViewDefault()
    {
        using var host = new GridHost();
        host.LookupColumn.ClosedEnterKeyBehavior = BootstrapLookupClosedEnterKeyBehavior.DataGridViewDefault;
        var editor = (IDataGridViewEditingControl)host.BeginLookupEdit();
        Assert.That(editor.EditingControlWantsInputKey(Keys.Enter, true), Is.False);
        host.Grid.CancelEdit();
    }

    private static bool SendDialogKey(BootstrapLookupBox lookup, Keys key)
    {
        var native = Descendants(lookup).OfType<TextBox>().Single();
        var message = Message.Create(native.Handle, 0x0100, (IntPtr)(int)key, IntPtr.Zero);
        return native.PreProcessMessage(ref message);
    }

    private static void SendKey(BootstrapLookupBox lookup, Keys key)
    {
        var native = Descendants(lookup).OfType<TextBox>().Single();
        var message = Message.Create(native.Handle, 0x0100, (IntPtr)(int)key, IntPtr.Zero);
        native.PreProcessMessage(ref message);
        Application.DoEvents();
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private sealed class GridHost : IDisposable
    {
        internal GridHost(bool twoRows = false, bool empty = false)
        {
            Products = new BindingList<Product> { new Product(1, "Alpha"), new Product(2, "Beta") };
            Rows = empty ? new BindingList<OrderLine>() : new BindingList<OrderLine> { new OrderLine { ProductId = 1 } };
            if (twoRows) Rows.Add(new OrderLine { ProductId = 2 });
            Binding = new BindingSource { DataSource = Rows };
            LookupColumn = new BootstrapLookupColumn
            {
                DataPropertyName = "ProductId", DataSource = Products, DisplayMember = "Name", ValueMember = "Id",
                SearchDebounceMilliseconds = 0
            };
            Grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, DataSource = Binding };
            Grid.Columns.Add(LookupColumn);
            Grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReadOnlyText", ReadOnly = true, Visible = false });
            Grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity" });
            Form = new Form { ShowInTaskbar = false };
            Form.Controls.Add(Grid); Form.Show(); Application.DoEvents();
        }

        internal Form Form { get; }
        internal DataGridView Grid { get; }
        internal BootstrapLookupColumn LookupColumn { get; }
        internal BindingList<Product> Products { get; }
        internal BindingList<OrderLine> Rows { get; }
        internal BindingSource Binding { get; }

        internal BootstrapLookupBox BeginLookupEdit(int rowIndex = 0)
        {
            Grid.CurrentCell = Grid.Rows[rowIndex].Cells[0];
            Assert.That(Grid.BeginEdit(true), Is.True);
            Application.DoEvents();
            return (BootstrapLookupBox)Grid.EditingControl!;
        }

        public void Dispose()
        {
            Grid.CancelEdit();
            Form.Dispose();
            Grid.Dispose();
            Binding.Dispose();
        }
    }

    private sealed class Product
    {
        internal Product(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }

    private sealed class OrderLine
    {
        public int ProductId { get; set; }
        public string ReadOnlyText { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
