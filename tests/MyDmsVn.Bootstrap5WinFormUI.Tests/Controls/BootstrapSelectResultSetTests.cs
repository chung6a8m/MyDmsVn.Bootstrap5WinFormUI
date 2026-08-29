using System;
using System.Linq;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSelectResultSetTests
{
    [Test]
    public void BuilderFiltersItemsAndCreatesNonSelectableGroupHeaders()
    {
        var selected = new BootstrapSelectItem(2, "Beta") { Group = "Group B" };
        var results = BootstrapSelectResultBuilder.BuildLocal(
            new[]
            {
                new BootstrapSelectItem(1, "Alpha") { Group = "Group A" },
                selected,
                new BootstrapSelectItem(3, "Beta disabled") { Group = "Group B", Disabled = true }
            },
            "beta",
            new BootstrapSelectTextMatcher(),
            item => item.Value.Equals(selected.Value));

        Assert.That(results.Rows.Select(row => row.Kind), Is.EqualTo(new[]
        {
            BootstrapSelectResultRowKind.GroupHeader,
            BootstrapSelectResultRowKind.Item,
            BootstrapSelectResultRowKind.Item
        }));
        Assert.That(results.Rows[0].Text, Is.EqualTo("Group B"));
        Assert.That(results.Rows[0].Item, Is.Null);
        Assert.That(results.Rows[1].IsSelected, Is.True);
        Assert.That(results.Rows[2].Item!.Disabled, Is.True);
    }

    [Test]
    public void EmptyGroupsAreHiddenAndUngroupedItemsNeedNoHeader()
    {
        var results = BootstrapSelectResultBuilder.BuildLocal(
            new[]
            {
                new BootstrapSelectItem(1, "Alpha") { Group = "Hidden" },
                new BootstrapSelectItem(2, "Beta")
            },
            "Beta",
            new BootstrapSelectTextMatcher(),
            _ => false);

        Assert.That(results.Rows, Has.Count.EqualTo(1));
        Assert.That(results.Rows[0].Kind, Is.EqualTo(BootstrapSelectResultRowKind.Item));
        Assert.That(results.Rows[0].Item!.Text, Is.EqualTo("Beta"));
    }

    [Test]
    public void AppendPageSuppressesAdjacentDuplicateGroupHeader()
    {
        var first = BootstrapSelectResultBuilder.BuildLoaded(
            new[] { new BootstrapSelectItem(1, "One") { Group = "Shared" } },
            _ => false);

        var appended = BootstrapSelectResultBuilder.AppendLoaded(
            first,
            new[]
            {
                new BootstrapSelectItem(2, "Two") { Group = "Shared" },
                new BootstrapSelectItem(3, "Three") { Group = "Next" }
            },
            _ => false);

        Assert.That(appended.Rows.Count(row => row.Kind == BootstrapSelectResultRowKind.GroupHeader && row.Text == "Shared"), Is.EqualTo(1));
        Assert.That(appended.Rows.Select(row => row.Kind), Is.EqualTo(new[]
        {
            BootstrapSelectResultRowKind.GroupHeader,
            BootstrapSelectResultRowKind.Item,
            BootstrapSelectResultRowKind.Item,
            BootstrapSelectResultRowKind.GroupHeader,
            BootstrapSelectResultRowKind.Item
        }));
    }

    [Test]
    public void SpecialRowsCarryTextWithoutPretendingToBeItems()
    {
        foreach (var kind in new[]
        {
            BootstrapSelectResultRowKind.CreateValue,
            BootstrapSelectResultRowKind.Loading,
            BootstrapSelectResultRowKind.LoadMoreError,
            BootstrapSelectResultRowKind.Empty,
            BootstrapSelectResultRowKind.Instruction,
            BootstrapSelectResultRowKind.Error
        })
        {
            var result = BootstrapSelectResultSet.SingleMessage(kind, kind.ToString());
            Assert.That(result.Rows, Has.Count.EqualTo(1));
            Assert.That(result.Rows[0].Kind, Is.EqualTo(kind));
            Assert.That(result.Rows[0].Item, Is.Null);
            Assert.That(result.Rows[0].Text, Is.EqualTo(kind.ToString()));
        }
    }

    [Test]
    public void ExactTextMatchIsIndependentFromMatcher()
    {
        Assert.That(BootstrapSelectResultBuilder.HasExactTextMatch(
            new[] { new BootstrapSelectItem(1, "ABC Corporation") }, "abc"), Is.False);
        Assert.That(BootstrapSelectResultBuilder.HasExactTextMatch(
            new[] { new BootstrapSelectItem(2, "ABC") }, "abc"), Is.True);
    }

    [Test]
    public void RowKindValuesStayStableAndComplete()
    {
        Assert.That(Enum.GetValues(typeof(BootstrapSelectResultRowKind)), Has.Length.EqualTo(8));
        Assert.That((int)BootstrapSelectResultRowKind.GroupHeader, Is.EqualTo(0));
        Assert.That((int)BootstrapSelectResultRowKind.Item, Is.EqualTo(1));
        Assert.That((int)BootstrapSelectResultRowKind.CreateValue, Is.EqualTo(2));
        Assert.That((int)BootstrapSelectResultRowKind.Loading, Is.EqualTo(3));
        Assert.That((int)BootstrapSelectResultRowKind.LoadMoreError, Is.EqualTo(4));
        Assert.That((int)BootstrapSelectResultRowKind.Empty, Is.EqualTo(5));
        Assert.That((int)BootstrapSelectResultRowKind.Instruction, Is.EqualTo(6));
        Assert.That((int)BootstrapSelectResultRowKind.Error, Is.EqualTo(7));
    }
}
