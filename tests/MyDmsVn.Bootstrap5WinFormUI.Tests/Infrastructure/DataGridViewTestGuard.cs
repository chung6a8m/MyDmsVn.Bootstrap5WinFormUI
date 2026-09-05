using System;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;

internal static class DataGridViewTestGuard
{
    internal static void FailOnDataError(DataGridView grid)
    {
        if (grid is null)
        {
            throw new ArgumentNullException(nameof(grid));
        }

        grid.DataError += OnDataError;
    }

    private static void OnDataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        if (e.Exception is not null)
        {
            ExceptionDispatchInfo.Capture(e.Exception).Throw();
            return;
        }

        throw new InvalidOperationException(
            $"Unexpected DataGridView.DataError. Row={e.RowIndex}, Column={e.ColumnIndex}, Context={e.Context}.");
    }
}
