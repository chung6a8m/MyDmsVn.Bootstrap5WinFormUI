using System;
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
            e.ThrowException = true;
            return;
        }

        throw new InvalidOperationException(
            $"Unexpected DataGridView.DataError. Row={e.RowIndex}, Column={e.ColumnIndex}, Context={e.Context}.");
    }
}
