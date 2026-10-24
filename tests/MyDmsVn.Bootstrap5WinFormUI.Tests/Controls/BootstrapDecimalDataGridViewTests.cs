using System;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapDecimalDataGridViewTests
{
    [Test]
    public void BoundDecimalCommitWritesModelOnceAfterNativeHostAcceptance()
    {
        using var host = new GridHost();
        var editor = host.BeginEdit();
        editor.Text = "1234.5";

        Assert.That(editor.Value, Is.EqualTo(12m));
        Assert.That(host.Row.Amount, Is.EqualTo(12m));
        Assert.That(host.Grid.EndEdit(), Is.True);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.Row.Amount, Is.EqualTo(1234.5m));
            Assert.That(host.Row.WriteCount, Is.EqualTo(1));
            Assert.That(editor.Value, Is.EqualTo(1234.5m));
        }));
    }

    [Test]
    public void InvalidConversionKeepsRawCandidateAndBoundModel()
    {
        using var host = new GridHost(expectDataError: true);
        var errors = 0;
        host.Grid.DataError += (_, e) => { errors++; e.ThrowException = false; };
        var editor = host.BeginEdit();
        editor.Text = "1.00001";

        Assert.That(host.Grid.EndEdit(), Is.False);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(errors, Is.GreaterThan(0));
            Assert.That(host.Grid.IsCurrentCellInEditMode, Is.True);
            Assert.That(editor.PendingText, Is.EqualTo("1.00001"));
            Assert.That(editor.Value, Is.EqualTo(12m));
            Assert.That(host.Row.Amount, Is.EqualTo(12m));
            Assert.That(host.Row.WriteCount, Is.Zero);
        }));
    }

    [Test]
    public void NullableProfileCannotCommitNullToNonNullableBoundProperty()
    {
        using var host = new GridHost(expectDataError: true);
        host.Column.Profile = new DecimalInputProfile(CultureInfo.GetCultureInfo("en-US"), true, 4, 0m, 999999m);
        var errors = 0;
        host.Grid.DataError += (_, e) => { errors++; e.ThrowException = false; };
        var editor = host.BeginEdit();
        editor.Text = string.Empty;

        Assert.That(host.Grid.EndEdit(), Is.False);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(errors, Is.GreaterThan(0));
            Assert.That(editor.PendingText, Is.Empty);
            Assert.That(host.Row.Amount, Is.EqualTo(12m));
            Assert.That(host.Row.WriteCount, Is.Zero);
        }));
    }

    [Test]
    public void NullableDataTableColumnCanCommitDbNull()
    {
        var table = new DataTable();
        table.Columns.Add(new DataColumn("Amount", typeof(decimal)) { AllowDBNull = true });
        table.Rows.Add(5m);
        using var form = new Form { ShowInTaskbar = false };
        var grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, DataSource = table };
        DataGridViewTestGuard.FailOnDataError(grid);
        grid.Columns.Add(new BootstrapDecimalColumn
        {
            DataPropertyName = "Amount",
            Profile = new DecimalInputProfile(CultureInfo.GetCultureInfo("en-US"), true, 2, 0m, 100m)
        });
        form.Controls.Add(grid);
        form.Show();
        Application.DoEvents();
        grid.CurrentCell = grid.Rows[0].Cells[0];
        Assert.That(grid.BeginEdit(true), Is.True);
        ((BootstrapDecimalInput)grid.EditingControl!).Text = string.Empty;

        try
        {
            Assert.That(grid.EndEdit(), Is.True);
            Assert.That(table.Rows[0]["Amount"], Is.EqualTo(DBNull.Value));
        }
        finally { grid.CancelEdit(); }
    }

    [Test]
    public void CanceledValidationRetainsCandidateAndDoesNotWrite()
    {
        using var host = new GridHost();
        host.Grid.CellValidating += (_, e) => e.Cancel = true;
        var editor = host.BeginEdit();
        editor.Text = "42";

        Assert.Throws<InvalidOperationException>((Action)(() => host.Grid.CurrentCell = host.Grid.Rows[0].Cells[2]));
        Assert.That(host.Grid.CurrentCell!.ColumnIndex, Is.Zero);
        Assert.That(editor.PendingText, Is.EqualTo("42"));
        Assert.That(host.Row.WriteCount, Is.Zero);
    }

    [Test]
    public void RecycledEditorResetsCandidateHistoryAndProfileForNullableColumn()
    {
        using var host = new GridHost();
        var first = host.BeginEdit();
        first.Text = "invalid";
        first.ValidationState = BootstrapValidationState.Invalid;
        host.Grid.CancelEdit();
        host.NullableColumn.Profile = new DecimalInputProfile(CultureInfo.GetCultureInfo("vi-VN"), true, 2, -100m, 100m);

        var second = host.BeginEdit(columnIndex: 2);
        Assert.That(second, Is.SameAs(first));
        Assert.That(second.PendingText, Is.Empty);
        Assert.That(second.Value, Is.Null);
        Assert.That(second.ValidationState, Is.EqualTo(BootstrapValidationState.None));
        var native = second.Controls.OfType<TextBox>().Single();
        var undo = Message.Create(native.Handle, 0x0100, (IntPtr)(int)(Keys.Control | Keys.Z), IntPtr.Zero);
        native.PreProcessMessage(ref undo);
        Assert.That(second.PendingText, Is.Empty);
        second.Text = "12,5";
        Assert.That(host.Grid.EndEdit(), Is.True);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.Row.NullableAmount, Is.EqualTo(12.5m));
            Assert.That(host.Row.NullableWriteCount, Is.EqualTo(1));
        }));
    }

    [TestCase(".5", "0.5")]
    [TestCase("1.00000", "1")]
    [TestCase("0", "0")]
    public void BoundHostUsesExactSharedDecimalGrammar(string candidate, string expected)
    {
        using var host = new GridHost();
        var editor = host.BeginEdit();
        editor.Text = candidate;

        Assert.That(host.Grid.EndEdit(), Is.True);
        Assert.That(host.Row.Amount, Is.EqualTo(decimal.Parse(expected, CultureInfo.InvariantCulture)));
        Assert.That(host.Row.WriteCount, Is.EqualTo(1));
    }

    [Test]
    public void ProfileChangedAfterEditStartsDoesNotReinterpretPendingCandidate()
    {
        using var host = new GridHost();
        var editor = host.BeginEdit();
        editor.Text = "12.5";
        host.Column.Profile = new DecimalInputProfile(CultureInfo.GetCultureInfo("vi-VN"), false, 2, 0m, 100m);

        Assert.That(host.Grid.EndEdit(), Is.True);
        Assert.That(host.Row.Amount, Is.EqualTo(12.5m));
    }

    [Test]
    public void ColumnCloneCarriesProfileAndAllowsNullCommit()
    {
        using var host = new GridHost();
        var clone = (BootstrapDecimalColumn)host.NullableColumn.Clone();
        Assert.That(clone.Profile, Is.SameAs(host.NullableColumn.Profile));
        var editor = host.BeginEdit(columnIndex: 2);
        editor.Text = "";
        Assert.That(host.Grid.EndEdit(), Is.True);
        Assert.That(host.Row.NullableAmount, Is.Null);
        clone.Dispose();
    }

    [Test]
    public void BoundCellDisplayUsesItsFrozenColumnCulture()
    {
        using var host = new GridHost();
        host.NullableColumn.Profile = new DecimalInputProfile(CultureInfo.GetCultureInfo("vi-VN"), true, 4, 0m, 999999m);
        host.Row.NullableAmount = 1234.5m;
        host.Grid.Refresh();

        Assert.That(host.Grid.Rows[0].Cells[2].FormattedValue, Is.EqualTo("1.234,5"));
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void ThemeChangeKeepsInvalidPendingTextCaretAndAcceptedModel(BootstrapThemeMode mode)
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            using var host = new GridHost();
            var editor = host.BeginEdit();
            editor.Text = "12x";
            var native = editor.Controls.OfType<TextBox>().Single();
            native.Focus();
            native.Select(1, 1);
            var focused = native.Focused;

            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                mode == BootstrapThemeMode.Light ? BootstrapThemeMode.Dark : BootstrapThemeMode.Light);
            Application.DoEvents();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(editor.PendingText, Is.EqualTo("12x"));
                Assert.That(editor.Value, Is.EqualTo(12m));
                Assert.That(native.SelectionStart, Is.EqualTo(1));
                Assert.That(native.SelectionLength, Is.EqualTo(1));
                Assert.That(native.Focused, Is.EqualTo(focused));
                Assert.That(host.Row.WriteCount, Is.Zero);
            }));
        }
        finally { BootstrapThemeManager.CurrentTheme = originalTheme; }
    }

    [Test]
    public void CancelProjectsCurrentModelAndReadOnlyCellRefusesEdit()
    {
        using var host = new GridHost();
        var editor = host.BeginEdit();
        editor.Text = "88";
        host.Row.Amount = 25m;
        host.Row.WriteCount = 0;
        host.Grid.CancelEdit();
        Assert.That(host.Row.Amount, Is.EqualTo(25m));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(editor.Value, Is.EqualTo(25m));
            Assert.That(editor.PendingText, Is.EqualTo("25"));
            Assert.That(host.Row.WriteCount, Is.Zero);
        }));
        host.Grid.Rows[0].Cells[0].ReadOnly = true;
        Assert.That(host.Grid.BeginEdit(true), Is.False);
    }

    [Test]
    public void NativeTabCommitsAndMovesToNextEditableCell()
    {
        using var host = new GridHost();
        host.Grid.Columns[1].ReadOnly = true;
        var editor = host.BeginEdit();
        editor.Text = "31";
        var native = editor.Controls.OfType<TextBox>().Single();
        var message = Message.Create(native.Handle, 0x0100, (IntPtr)(int)Keys.Tab, IntPtr.Zero);

        native.PreProcessMessage(ref message);
        Application.DoEvents();
        Assert.That(host.Row.Amount, Is.EqualTo(31m));
        Assert.That(host.Grid.CurrentCell!.ColumnIndex, Is.EqualTo(2));
    }

    [Test]
    public void NativeShiftTabTraversesBackAndEnterCommitsTypedValue()
    {
        using var host = new GridHost();
        host.Grid.Columns[1].ReadOnly = true;
        var editor = host.BeginEdit(columnIndex: 2);
        editor.Text = "7.5";
        var native = editor.Controls.OfType<TextBox>().Single();
        var message = Message.Create(native.Handle, 0x0100, (IntPtr)(int)(Keys.Shift | Keys.Tab), IntPtr.Zero);
        native.PreProcessMessage(ref message);
        Application.DoEvents();

        Assert.That(host.Row.NullableAmount, Is.EqualTo(7.5m));
        Assert.That(host.Grid.CurrentCell!.ColumnIndex, Is.Zero);
        editor = host.BeginEdit();
        editor.Text = "9";
        message = Message.Create(editor.Controls.OfType<TextBox>().Single().Handle, 0x0100, (IntPtr)(int)Keys.Enter, IntPtr.Zero);
        editor.Controls.OfType<TextBox>().Single().PreProcessMessage(ref message);
        Application.DoEvents();
        Assert.That(host.Row.Amount, Is.EqualTo(9m));
    }

    [Test]
    public void NativeTabAndShiftTabIncludeBoundaryCellsAcrossRows()
    {
        using var host = new GridHost(twoRows: true);
        var editor = host.BeginEdit(columnIndex: 2);
        editor.Text = "7.5";
        var native = editor.Controls.OfType<TextBox>().Single();
        var message = Message.Create(native.Handle, 0x0100, (IntPtr)(int)Keys.Tab, IntPtr.Zero);
        native.PreProcessMessage(ref message);
        Application.DoEvents();
        Assert.That(host.Grid.CurrentCell!.RowIndex, Is.EqualTo(1));
        Assert.That(host.Grid.CurrentCell!.ColumnIndex, Is.Zero);

        Assert.That(host.Grid.BeginEdit(true), Is.True);
        editor = (BootstrapDecimalInput)host.Grid.EditingControl!;
        native = editor.Controls.OfType<TextBox>().Single();
        message = Message.Create(native.Handle, 0x0100, (IntPtr)(int)(Keys.Shift | Keys.Tab), IntPtr.Zero);
        native.PreProcessMessage(ref message);
        Application.DoEvents();
        Assert.That(host.Grid.CurrentCell!.RowIndex, Is.Zero);
        Assert.That(host.Grid.CurrentCell!.ColumnIndex, Is.EqualTo(2));
    }

    [Test]
    public void NativeNewRowCreatesOneTypedBoundRow()
    {
        using var host = new GridHost(empty: true);
        var editor = host.BeginEdit();
        editor.Text = "6.25";
        Assert.That(host.Grid.EndEdit(), Is.True);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.Rows.Count, Is.EqualTo(1));
            Assert.That(host.Rows[0].Amount, Is.EqualTo(6.25m));
            Assert.That(host.Grid.Rows.Cast<DataGridViewRow>().Count(row => row.IsNewRow), Is.EqualTo(1));
        }));
    }

    [Test]
    public void RemovedRowCannotReceiveLateEditorNotification()
    {
        using var host = new GridHost(twoRows: true);
        var editor = host.BeginEdit();
        editor.Text = "8";
        host.Grid.CancelEdit();
        host.Rows.RemoveAt(0);
        var remaining = host.Rows[0];
        editor.Text = "99";

        Assert.That(remaining.Amount, Is.EqualTo(20m));
        Assert.That(remaining.WriteCount, Is.Zero);
    }

    private sealed class GridHost : IDisposable
    {
        internal GridHost(bool empty = false, bool twoRows = false, bool expectDataError = false)
        {
            Row = new DecimalRow { Amount = 12m };
            Row.WriteCount = 0;
            Rows = empty ? new BindingList<DecimalRow>() : new BindingList<DecimalRow> { Row };
            if (twoRows) Rows.Add(new DecimalRow { Amount = 20m });
            foreach (var row in Rows) row.WriteCount = 0;
            Binding = new BindingSource { DataSource = Rows };
            Column = new BootstrapDecimalColumn
            {
                DataPropertyName = nameof(DecimalRow.Amount),
                Profile = new DecimalInputProfile(CultureInfo.GetCultureInfo("en-US"), false, 4, 0m, 999999m)
            };
            Grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = empty, DataSource = Binding };
            if (!expectDataError) DataGridViewTestGuard.FailOnDataError(Grid);
            Grid.Columns.Add(Column);
            Grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DecimalRow.Other) });
            NullableColumn = new BootstrapDecimalColumn
            {
                DataPropertyName = nameof(DecimalRow.NullableAmount),
                Profile = new DecimalInputProfile(CultureInfo.GetCultureInfo("en-US"), true, 4, -999999m, 999999m)
            };
            Grid.Columns.Add(NullableColumn);
            Form = new Form { ShowInTaskbar = false };
            Form.Controls.Add(Grid);
            Form.Show();
            Application.DoEvents();
        }

        internal Form Form { get; }
        internal DataGridView Grid { get; }
        internal BootstrapDecimalColumn Column { get; }
        internal BootstrapDecimalColumn NullableColumn { get; }
        internal DecimalRow Row { get; }
        internal BindingList<DecimalRow> Rows { get; }
        internal BindingSource Binding { get; }

        internal BootstrapDecimalInput BeginEdit(int columnIndex = 0)
        {
            Grid.CurrentCell = Grid.Rows[0].Cells[columnIndex];
            Assert.That(Grid.BeginEdit(true), Is.True);
            Application.DoEvents();
            return (BootstrapDecimalInput)Grid.EditingControl!;
        }

        public void Dispose()
        {
            Grid.CancelEdit();
            Form.Dispose();
            Binding.Dispose();
        }
    }

    private sealed class DecimalRow
    {
        private decimal _amount;
        public decimal Amount { get => _amount; set { _amount = value; WriteCount++; } }
        public int WriteCount { get; set; }
        public string Other { get; set; } = string.Empty;
        private decimal? _nullableAmount;
        public decimal? NullableAmount { get => _nullableAmount; set { _nullableAmount = value; NullableWriteCount++; } }
        public int NullableWriteCount { get; set; }
    }
}
