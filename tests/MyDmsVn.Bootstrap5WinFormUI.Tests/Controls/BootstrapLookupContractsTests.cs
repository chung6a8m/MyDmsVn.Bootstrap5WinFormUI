using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapLookupContractsTests
{
    [TestCase(typeof(BootstrapLookupEmptyQueryBehavior), new[] { "ShowAll", "ShowNone" })]
    [TestCase(typeof(BootstrapLookupTypingPopupBehavior), new[] { "AutoOpen", "KeepCurrentState" })]
    [TestCase(typeof(BootstrapLookupUnmatchedTextBehavior), new[] { "RestorePreviousSelection", "KeepFocusWithValidationError", "CommitAndAdd" })]
    [TestCase(typeof(BootstrapLookupEnterKeyBehavior), new[] { "CommitSelection", "CommitSelectionAndMoveNext" })]
    [TestCase(typeof(BootstrapLookupClosedEnterKeyBehavior), new[] { "ResolvePendingText", "DataGridViewDefault" })]
    [TestCase(typeof(BootstrapLookupCommitReason), new[] { "Keyboard", "Mouse", "Programmatic", "ExactMatch", "CommitAndAdd", "Clear" })]
    public void EnumContractsUseReviewedNamesAndSequentialValues(Type enumType, string[] names)
    {
        Assert.That(Enum.GetNames(enumType), Is.EqualTo(names));
        Assert.That(Enum.GetValues(enumType).Cast<object>().Select(Convert.ToInt32), Is.EqualTo(Enumerable.Range(0, names.Length)));
    }

    [Test]
    public void SearchMembersRejectInvalidAndDuplicateNamesWithoutChangingOrder()
    {
        var members = new BootstrapLookupSearchMemberCollection { "Code", "Name" };

        Assert.Multiple((Action)(() =>
        {
            Assert.That(members, Is.EqualTo(new[] { "Code", "Name" }));
            Assert.That((Action)(() => members.Add(" ")), Throws.ArgumentException);
            Assert.That((Action)(() => members.Add("Code")), Throws.ArgumentException);
            Assert.That((Action)(() => members.Add(null!)), Throws.ArgumentNullException);
            Assert.That(members, Is.EqualTo(new[] { "Code", "Name" }));
        }));
    }

    [Test]
    public void ColumnDefinitionsRejectNullAndExposeDesignerContentSerialization()
    {
        var columns = new BootstrapLookupColumnDefinitionCollection
        {
            new BootstrapLookupColumnDefinition { DataPropertyName = "Code" }
        };

        Assert.Multiple((Action)(() =>
        {
            Assert.That((Action)(() => columns.Add(null!)), Throws.ArgumentNullException);
            Assert.That(columns.Single().DataPropertyName, Is.EqualTo("Code"));
            Assert.That(typeof(BootstrapLookupColumnDefinitionCollection).GetConstructor(Type.EmptyTypes), Is.Not.Null);
        }));
    }

    [Test]
    public void ColumnDefinitionDefaultsAndValidationMatchTextColumnContract()
    {
        var definition = new BootstrapLookupColumnDefinition();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(definition.DataPropertyName, Is.Empty);
            Assert.That(definition.HeaderText, Is.Empty);
            Assert.That(definition.Width, Is.EqualTo(100));
            Assert.That(definition.MinimumWidth, Is.EqualTo(5));
            Assert.That(definition.Visible, Is.True);
            Assert.That(definition.AutoSizeMode, Is.EqualTo(DataGridViewAutoSizeColumnMode.None));
            Assert.That(definition.Alignment, Is.EqualTo(DataGridViewContentAlignment.MiddleLeft));
            Assert.That(definition.Format, Is.Empty);
            Assert.That(definition.ValueType, Is.Null);
            Assert.That((Action)(() => { definition.Width = 0; }), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That((Action)(() => { definition.MinimumWidth = 1; }), Throws.TypeOf<ArgumentOutOfRangeException>());
        }));
    }

    [Test]
    public void EventArgsExposeReviewedInputsAndWritableOutcomes()
    {
        var item = new object();
        var selection = new BootstrapLookupSelectionCommittedEventArgs(item, 42, "Coffee", BootstrapLookupCommitReason.Mouse);
        var highlighted = new BootstrapLookupHighlightedItemChangedEventArgs(null, item);
        var refresh = new BootstrapLookupRefreshRequestedEventArgs("cof");
        var add = new BootstrapLookupAddNewRequestedEventArgs("cof") { NewItem = item, Cancel = true };
        var create = new BootstrapLookupCreateItemFromTextEventArgs(" Coffee ", "Coffee") { Item = item, Cancel = true };

        Assert.Multiple((Action)(() =>
        {
            Assert.That(selection.Item, Is.SameAs(item));
            Assert.That(selection.Value, Is.EqualTo(42));
            Assert.That(selection.DisplayText, Is.EqualTo("Coffee"));
            Assert.That(selection.Reason, Is.EqualTo(BootstrapLookupCommitReason.Mouse));
            Assert.That(highlighted.OldItem, Is.Null);
            Assert.That(highlighted.NewItem, Is.SameAs(item));
            Assert.That(refresh.QueryText, Is.EqualTo("cof"));
            Assert.That(add.NewItem, Is.SameAs(item));
            Assert.That(add.Cancel, Is.True);
            Assert.That(create.OriginalText, Is.EqualTo(" Coffee "));
            Assert.That(create.NormalizedText, Is.EqualTo("Coffee"));
            Assert.That(create.Item, Is.SameAs(item));
            Assert.That(create.Cancel, Is.True);
        }));
    }

    [Test]
    public void GridContextEventArgsExposeNativeCellCoordinates()
    {
        using var grid = new DataGridView();
        var context = new BootstrapLookupCellEventArgs(grid, 2, 3);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(context.DataGridView, Is.SameAs(grid));
            Assert.That(context.RowIndex, Is.EqualTo(2));
            Assert.That(context.ColumnIndex, Is.EqualTo(3));
        }));
    }
}
