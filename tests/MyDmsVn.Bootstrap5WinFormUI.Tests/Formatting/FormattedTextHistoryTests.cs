using System;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Formatting;

[TestFixture]
public sealed class FormattedTextHistoryTests
{
    [Test]
    public void UndoRedoAndBranchEditsPreserveRawSnapshots()
    {
        var history = new FormattedTextHistory();
        var initial = new FormattedTextSnapshot("", 0, 0);
        var first = new FormattedTextSnapshot("1234", 2, 2);
        var second = new FormattedTextSnapshot("12345", 5, 0);
        history.Record(initial);

        Assert.That(history.TryUndo(first, out var undone), Is.True);
        Assert.That(undone.RawValue, Is.Empty);
        Assert.That(history.TryRedo(undone, out var redone), Is.True);
        Assert.That(redone.RawValue, Is.EqualTo("1234"));

        Assert.That(history.TryUndo(second, out _), Is.True);
        history.Record(first);
        Assert.That(history.TryRedo(first, out _), Is.False, "A new edit after undo must clear redo.");
        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.RawSelectionStart, Is.EqualTo(2));
            Assert.That(first.RawSelectionLength, Is.EqualTo(2));
        }));
    }

    [Test]
    public void DuplicateRecordsAreIgnoredAndUndoIsBoundedToOneHundredEntries()
    {
        var history = new FormattedTextHistory();
        for (var index = 0; index < 105; index++)
        {
            var snapshot = new FormattedTextSnapshot(index.ToString(), 0, 0);
            history.Record(snapshot);
            history.Record(snapshot);
        }

        var current = new FormattedTextSnapshot("current", 0, 0);
        var undoCount = 0;
        while (history.TryUndo(current, out var prior))
        {
            undoCount++;
            current = prior;
        }

        Assert.That(undoCount, Is.EqualTo(100));
        Assert.That(current.RawValue, Is.EqualTo("5"));
    }

    [Test]
    public void SnapshotClampsSelectionToRawCoordinates()
    {
        var snapshot = new FormattedTextSnapshot("1234", 3, 9);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(snapshot.RawSelectionStart, Is.EqualTo(3));
            Assert.That(snapshot.RawSelectionLength, Is.EqualTo(1));
        }));
    }
}
