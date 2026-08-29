using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;

internal sealed class BootstrapSelectResultSet
{
    private readonly ReadOnlyCollection<BootstrapSelectResultRow> _rows;

    internal BootstrapSelectResultSet(IEnumerable<BootstrapSelectResultRow> rows)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        var snapshot = new List<BootstrapSelectResultRow>();
        foreach (var row in rows)
        {
            if (row is null)
            {
                throw new ArgumentException("Result rows cannot contain null entries.", nameof(rows));
            }

            snapshot.Add(row);
        }

        _rows = snapshot.AsReadOnly();
    }

    internal IReadOnlyList<BootstrapSelectResultRow> Rows => _rows;

    internal static BootstrapSelectResultSet SingleMessage(BootstrapSelectResultRowKind kind, string text)
    {
        return new BootstrapSelectResultSet(new[] { BootstrapSelectResultRow.Message(kind, text) });
    }
}
