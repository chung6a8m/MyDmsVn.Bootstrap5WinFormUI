using System.Collections.Generic;

namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

internal sealed class FormattedTextHistory
{
    private const int MaximumEntries = 100;
    private readonly List<FormattedTextSnapshot> _undo = new List<FormattedTextSnapshot>();
    private readonly List<FormattedTextSnapshot> _redo = new List<FormattedTextSnapshot>();

    internal void Record(FormattedTextSnapshot snapshot)
    {
        if (_undo.Count > 0 && _undo[_undo.Count - 1].Equals(snapshot)) return;
        AddBounded(_undo, snapshot);
        _redo.Clear();
    }

    internal bool TryUndo(FormattedTextSnapshot current, out FormattedTextSnapshot snapshot)
    {
        if (_undo.Count == 0)
        {
            snapshot = default;
            return false;
        }

        AddBounded(_redo, current);
        snapshot = Pop(_undo);
        return true;
    }

    internal bool TryRedo(FormattedTextSnapshot current, out FormattedTextSnapshot snapshot)
    {
        if (_redo.Count == 0)
        {
            snapshot = default;
            return false;
        }

        AddBounded(_undo, current);
        snapshot = Pop(_redo);
        return true;
    }

    internal void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }

    private static void AddBounded(List<FormattedTextSnapshot> stack, FormattedTextSnapshot snapshot)
    {
        if (stack.Count == MaximumEntries) stack.RemoveAt(0);
        stack.Add(snapshot);
    }

    private static FormattedTextSnapshot Pop(List<FormattedTextSnapshot> stack)
    {
        var index = stack.Count - 1;
        var snapshot = stack[index];
        stack.RemoveAt(index);
        return snapshot;
    }
}
