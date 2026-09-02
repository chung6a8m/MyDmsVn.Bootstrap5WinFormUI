using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapLookupInteractionTests
{
    [Test]
    public void F4AndNavigationKeepEditorFocusAndEnterCommitsHighlight()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        Application.DoEvents();

        lookup.SendKey(Keys.F4);
        lookup.SendKey(Keys.End);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.IsDropDownOpen, Is.True);
            Assert.That(lookup.EditorFocused, Is.True);
            Assert.That(lookup.ResultsGrid.Focused, Is.False);
            Assert.That(lookup.SelectedValue, Is.Null);
        }));

        lookup.SendKey(Keys.Enter);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.SelectedValue, Is.EqualTo(2));
            Assert.That(lookup.IsDropDownOpen, Is.False);
        }));
    }

    [Test]
    public void EscapeRestoresCommittedTextWithoutAnotherCommit()
    {
        using var lookup = Create();
        lookup.SelectValue(1);
        var commits = 0;
        lookup.SelectionCommitted += (_, _) => commits++;
        lookup.Text = "unknown";
        lookup.SendKey(Keys.Escape);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.Text, Is.EqualTo("Alpha"));
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
            Assert.That(lookup.HasPendingText, Is.False);
            Assert.That(commits, Is.Zero);
        }));
    }

    [Test]
    public void InvalidTabIsConsumedAndKeepsLookupState()
    {
        using var lookup = Create();
        lookup.UnmatchedTextBehavior = BootstrapLookupUnmatchedTextBehavior.KeepFocusWithValidationError;
        lookup.Text = "unknown";

        Assert.That(lookup.SendDialogKey(Keys.Tab), Is.True);
        Assert.That(lookup.ValidationMessage, Is.Not.Empty);
        Assert.That(lookup.HasPendingText, Is.True);
    }

    [Test]
    public void CommitAndMoveNextUsesNormalFormTraversal()
    {
        using var form = new Form { ShowInTaskbar = false, ClientSize = new Size(300, 120) };
        using var lookup = Create();
        using var next = new Button { TabIndex = 1, Top = 50 };
        lookup.TabIndex = 0;
        lookup.EnterKeyBehavior = BootstrapLookupEnterKeyBehavior.CommitSelectionAndMoveNext;
        form.Controls.AddRange(new Control[] { lookup, next });
        form.Show();
        lookup.Focus();
        lookup.Text = "Alpha";
        lookup.SendKey(Keys.Enter);
        Application.DoEvents();

        Assert.That(next.Focused, Is.True);
        Assert.That(lookup.SelectedValue, Is.EqualTo(1));
    }

    private static TestLookup Create() => new TestLookup
    {
        DisplayMember = "Name",
        ValueMember = "Id",
        DataSource = new BindingList<Product> { new Product(1, "Alpha"), new Product(2, "Beta") },
        SearchDebounceMilliseconds = 0
    };

    private sealed class TestLookup : BootstrapLookupBox
    {
        internal bool EditorFocused => Editor.Focused;
        internal void SendKey(Keys key)
        {
            var args = new KeyEventArgs(key);
            OnEditorKeyDown(args);
        }
        internal bool SendDialogKey(Keys key) => ProcessDialogKey(key);
    }

    private sealed class Product
    {
        internal Product(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }
}
