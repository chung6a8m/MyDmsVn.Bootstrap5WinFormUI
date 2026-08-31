using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapSelectInteractionTests
{
    [Test]
    public void ProgrammaticSelectionKeepsAllSelectionViewsCoherent()
    {
        using var select = new BootstrapSelect();
        var alpha = new BootstrapSelectItem(1, "Alpha");
        var beta = new BootstrapSelectItem(2, "Beta");
        select.Items.Add(alpha);
        select.Items.Add(beta);

        Assert.That(select.Select(alpha), Is.True);
        Assert.That(select.Select(new BootstrapSelectItem(1, "Duplicate instance")), Is.False);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.SelectedItem, Is.SameAs(alpha));
            Assert.That(select.SelectedValue, Is.EqualTo(1));
            Assert.That(select.SelectedItems, Has.Count.EqualTo(1));
            Assert.That(select.SelectedValues, Is.EqualTo(new object[] { 1 }));
        }));

        Assert.That(select.SelectValue(2), Is.True);
        Assert.That(select.SelectedItem, Is.SameAs(beta));
        Assert.That(select.DeselectValue(2), Is.True);
        Assert.That(select.SelectedItems, Is.Empty);
    }

    [Test]
    public void SuccessfulSelectUsesDeterministicEventOrderAndCancellationPreventsCommit()
    {
        using var select = new BootstrapSelect();
        var alpha = new BootstrapSelectItem(1, "Alpha");
        var events = new List<string>();
        select.Selecting += (_, e) => events.Add("Selecting:" + e.Reason);
        select.Selected += (_, e) => events.Add("Selected:" + e.Reason);
        select.SelectionChanged += (_, _) => events.Add("SelectionChanged");

        Assert.That(select.Select(alpha), Is.True);
        Assert.That(events, Is.EqualTo(new[] { "Selecting:Programmatic", "Selected:Programmatic", "SelectionChanged" }));

        select.ClearSelection();
        events.Clear();
        select.Selecting += (_, e) => { if (e.Item.Value.Equals(2)) e.Cancel = true; };
        Assert.That(select.Select(new BootstrapSelectItem(2, "Beta")), Is.False);
        Assert.That(select.SelectedItems, Is.Empty);
        Assert.That(events, Is.EqualTo(new[] { "Selecting:Programmatic" }));
    }

    [Test]
    public void ClearInMultipleModeHonorsPerItemCancellationAndRaisesOneBatchChangedEvent()
    {
        using var select = new BootstrapSelect { SelectionMode = BootstrapSelectMode.Multiple };
        var alpha = new BootstrapSelectItem(1, "Alpha");
        var beta = new BootstrapSelectItem(2, "Beta");
        select.Select(alpha);
        select.Select(beta);

        var deselected = new List<object>();
        var changed = 0;
        select.Deselecting += (_, e) => { if (e.Item.Value.Equals(2)) e.Cancel = true; };
        select.Deselected += (_, e) => deselected.Add(e.Item.Value);
        select.SelectionChanged += (_, _) => changed++;

        select.ClearSelection();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.SelectedValues, Is.EqualTo(new object[] { 2 }));
            Assert.That(deselected, Is.EqualTo(new object[] { 1 }));
            Assert.That(changed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void MultipleToSingleTransitionIsAtomicWhenModeChangeDeselectIsCancelled()
    {
        using var select = new BootstrapSelect { SelectionMode = BootstrapSelectMode.Multiple };
        var alpha = new BootstrapSelectItem(1, "Alpha");
        var beta = new BootstrapSelectItem(2, "Beta");
        select.Select(alpha);
        select.Select(beta);
        select.Deselecting += (_, e) =>
        {
            if (e.Reason == BootstrapSelectChangeReason.ModeChange && e.Item.Value.Equals(2)) e.Cancel = true;
        };

        select.SelectionMode = BootstrapSelectMode.Single;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.SelectionMode, Is.EqualTo(BootstrapSelectMode.Multiple));
            Assert.That(select.SelectedValues, Is.EqualTo(new object[] { 1, 2 }));
        }));
    }

    [Test]
    public void SelectedItemAndSelectedValueSettersUseNormalSelectionPipeline()
    {
        using var select = new BootstrapSelect();
        var alpha = new BootstrapSelectItem("a", "Alpha");
        select.Items.Add(alpha);
        var selecting = 0;
        select.Selecting += (_, _) => selecting++;

        select.SelectedItem = alpha;
        Assert.That(select.SelectedValue, Is.EqualTo("a"));
        select.SelectedValue = null;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.SelectedItems, Is.Empty);
            Assert.That(selecting, Is.EqualTo(1));
        }));
    }

    [Test]
    public void TabAfterPopupSelectionMovesFocusAndInvalidatesBothSelects()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var first = new TestBootstrapSelect { TabIndex = 0 };
        using var second = new TestBootstrapSelect { TabIndex = 1 };
        first.Items.Add(new BootstrapSelectItem(1, "Northwind"));
        form.Controls.Add(first);
        form.Controls.Add(second);
        form.Show();
        Application.DoEvents();

        first.Focus();
        first.OpenDropDownInternal();
        Application.DoEvents();
        Assert.That(first.ActivateHighlightedResultForTest(), Is.True);
        Application.DoEvents();
        Assert.That(first.Focused, Is.True, "Single selection should restore focus to the Select before Tab traversal.");

        var firstInvalidated = 0;
        var secondInvalidated = 0;
        first.Invalidated += (_, _) => firstInvalidated++;
        second.Invalidated += (_, _) => secondInvalidated++;

        var handled = first.ProcessDialogKeyForTest(Keys.Tab);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(second.Focused, Is.True, "Tab should move actual focus to the next Select.");
            Assert.That(firstInvalidated, Is.GreaterThan(0), "The Select losing focus must repaint its active border.");
            Assert.That(secondInvalidated, Is.GreaterThan(0), "The Select receiving focus must repaint its active border.");
        }));
    }

    [Test]
    public void MovingFocusBetweenSelectsInvalidatesBothFocusVisuals()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var first = new BootstrapSelect { TabIndex = 0 };
        using var second = new BootstrapSelect { TabIndex = 1 };
        form.Controls.Add(first);
        form.Controls.Add(second);
        form.Show();
        Application.DoEvents();

        first.Focus();
        Application.DoEvents();

        var firstInvalidated = 0;
        var secondInvalidated = 0;
        first.Invalidated += (_, _) => firstInvalidated++;
        second.Invalidated += (_, _) => secondInvalidated++;

        second.Focus();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(second.Focused, Is.True);
            Assert.That(firstInvalidated, Is.GreaterThan(0), "The previously active Select must repaint when focus leaves it.");
            Assert.That(secondInvalidated, Is.GreaterThan(0), "The newly active Select must repaint when focus enters it.");
        }));
    }

    [Test]
    public void PrintableInputStillFiltersLocalResultsThroughPopupSearch()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var select = new TestBootstrapSelect
        {
            Bounds = new Rectangle(20, 20, 220, 32)
        };
        select.Items.Add(new BootstrapSelectItem(1, "Alpha"));
        select.Items.Add(new BootstrapSelectItem(2, "Northwind"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        Assert.That(select.Focus(), Is.True);
        select.RaisePrintableKeyForTest('N');
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.True);
            Assert.That(select.VisibleResultItemTextsForTest, Does.Contain("Northwind"));
            Assert.That(select.VisibleResultItemTextsForTest, Does.Not.Contain("Alpha"));
        }));
    }

    [Test]
    public void TabFromFocusedNativeSearchEditorClosesPopupAndContinuesOwnerTraversal()
    {
        using var form = new Form
        {
            ShowInTaskbar = false,
            ClientSize = new Size(420, 160)
        };
        using var previous = new Button
        {
            Bounds = new Rectangle(20, 20, 100, 30),
            TabIndex = 0,
            Text = "Previous"
        };
        using var select = new TestBootstrapSelect
        {
            Bounds = new Rectangle(20, 60, 220, 32),
            TabIndex = 1
        };
        using var next = new Button
        {
            Bounds = new Rectangle(20, 105, 100, 30),
            TabIndex = 2,
            Text = "Next"
        };
        select.Items.Add(new BootstrapSelectItem(1, "Northwind"));
        form.Controls.Add(previous);
        form.Controls.Add(select);
        form.Controls.Add(next);
        form.Show();
        Application.DoEvents();

        Assert.That(select.Focus(), Is.True);
        select.OpenDropDownInternal();
        Application.DoEvents();

        var native = Descendants(select.DropDownContentForTest!)
            .OfType<TextBox>()
            .Single();
        Assert.That(native.Focus(), Is.True);
        Application.DoEvents();
        Assert.That(native.Focused, Is.True);

        var message = Message.Create(
            native.Handle,
            0x0100,
            (IntPtr)(int)Keys.Tab,
            IntPtr.Zero);

        Assert.That(native.PreProcessMessage(ref message), Is.True);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.False);
            Assert.That(
                next.Focused,
                Is.True,
                "Tab from the popup search editor must continue owner-relative WinForms traversal.");
        }));
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed class TestBootstrapSelect : BootstrapSelect
    {
        internal void RaisePrintableKeyForTest(char character)
        {
            OnKeyPress(new KeyPressEventArgs(character));
        }

        internal bool ProcessDialogKeyForTest(Keys keyData)
        {
            return ProcessDialogKey(keyData);
        }
    }
}
