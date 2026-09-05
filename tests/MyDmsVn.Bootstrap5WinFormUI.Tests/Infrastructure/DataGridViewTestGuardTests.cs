using System;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class DataGridViewTestGuardTests
{
    [Test]
    public void FailOnDataErrorWithExceptionCannotBeSuppressedByLaterHandler()
    {
        using var grid = new ProbeDataGridView();
        DataGridViewTestGuard.FailOnDataError(grid);
        var laterHandlerRan = false;
        grid.DataError += (_, e) =>
        {
            laterHandlerRan = true;
            e.ThrowException = false;
        };
        var expected = new InvalidOperationException("boom");

        var exception = Assert.Throws<InvalidOperationException>((Action)(() =>
            grid.RaiseDataError(
                expected,
                columnIndex: 3,
                rowIndex: 7,
                DataGridViewDataErrorContexts.Commit | DataGridViewDataErrorContexts.CurrentCellChange)));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(exception, Is.SameAs(expected));
            Assert.That(laterHandlerRan, Is.False);
        }));
    }

    [Test]
    public void FailOnDataErrorWithoutExceptionThrowsDiagnosticFailure()
    {
        using var grid = new ProbeDataGridView();
        DataGridViewTestGuard.FailOnDataError(grid);

        var exception = Assert.Throws<InvalidOperationException>((Action)(() =>
            grid.RaiseDataError(
                exception: null,
                columnIndex: 3,
                rowIndex: 7,
                DataGridViewDataErrorContexts.Parsing)));

        Assert.That(exception!.Message, Does.Contain("Row=7"));
        Assert.That(exception.Message, Does.Contain("Column=3"));
        Assert.That(exception.Message, Does.Contain("Parsing"));
    }

    private sealed class ProbeDataGridView : DataGridView
    {
        internal DataGridViewDataErrorEventArgs RaiseDataError(
            Exception? exception,
            int columnIndex,
            int rowIndex,
            DataGridViewDataErrorContexts context)
        {
            var args = new DataGridViewDataErrorEventArgs(exception!, columnIndex, rowIndex, context);
            OnDataError(displayErrorDialogIfNoHandler: false, args);
            return args;
        }
    }
}
