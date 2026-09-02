using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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
    public void OpeningWithNonFirstHighlightKeepsVisualSelectionAndEnterCommitAligned()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        lookup.SelectValue(2);
        lookup.Text = string.Empty;

        lookup.OpenDropDown();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.ResultsGrid.SelectedRows, Has.Count.EqualTo(1));
            Assert.That(lookup.ResultsGrid.SelectedRows[0].Index, Is.EqualTo(1));
            Assert.That(lookup.ResultsGrid.CurrentCell?.RowIndex, Is.EqualTo(1));
        }));

        lookup.SendKey(Keys.Enter);
        Assert.That(lookup.SelectedValue, Is.EqualTo(2));
    }

    [Test]
    public void VisiblePopupPreservesNativeEditorTypingAndNavigation()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = new TestLookup
        {
            DisplayMember = "Name",
            ValueMember = "Id",
            DataSource = new BindingList<Product>
            {
                new Product(1, "Brazil"),
                new Product(2, "Brandy")
            },
            SearchDebounceMilliseconds = 0
        };
        form.Controls.Add(lookup);
        form.Show();
        form.Activate();
        lookup.Focus();
        Application.DoEvents();

        SendKeys.SendWait("b");
        Application.DoEvents();
        Assert.That(lookup.IsDropDownOpen, Is.True);

        SendKeys.SendWait("ra");
        SendKeys.SendWait("{DOWN}");
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.Text, Is.EqualTo("bra"));
            Assert.That(lookup.EditorFocused, Is.True);
            Assert.That(((Product)lookup.HighlightedItem!).Id, Is.EqualTo(2));
        }));
    }

    [Test]
    public void OpenPopupResizesWhenResultProjectionShrinksAndGrows()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = new TestLookup
        {
            DisplayMember = "Name",
            ValueMember = "Id",
            DataSource = new BindingList<Product>
            {
                new Product(1, "Exact"), new Product(2, "Other 2"), new Product(3, "Other 3"),
                new Product(4, "Other 4"), new Product(5, "Other 5"), new Product(6, "Other 6")
            },
            SearchDebounceMilliseconds = 0
        };
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        lookup.OpenDropDown();
        Application.DoEvents();
        var expandedHeight = lookup.ResultsGrid.Height;

        lookup.Text = "Exact";
        Application.DoEvents();
        var reducedHeight = lookup.ResultsGrid.Height;

        lookup.Text = string.Empty;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(reducedHeight, Is.LessThan(expandedHeight));
            Assert.That(lookup.ResultsGrid.Height, Is.EqualTo(expandedHeight));
        }));
    }

    [Test]
    public void PointerDownOnNonFocusableFormSurfaceClosesOpenPopup()
    {
        using var form = new Form { ShowInTaskbar = false, ClientSize = new Size(300, 140) };
        using var lookup = Create();
        using var surface = new Label { Bounds = new Rectangle(0, 60, 250, 50), Text = "Surface" };
        form.Controls.AddRange(new Control[] { lookup, surface });
        form.Show();
        lookup.Focus();
        lookup.OpenDropDown();
        Application.DoEvents();

        Assert.That(PostMessage(surface.Handle, 0x0201, (IntPtr)1, new IntPtr(5 | (5 << 16))), Is.True);
        Assert.That(PostMessage(surface.Handle, 0x0202, IntPtr.Zero, new IntPtr(5 | (5 << 16))), Is.True);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.EditorFocused, Is.True);
            Assert.That(lookup.IsDropDownOpen, Is.False);
        }));
    }

    [Test]
    public void KeyboardOpenMaterializesEachResultOnce()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = new TestLookup
        {
            DisplayMember = "Name",
            ValueMember = "Id",
            DataSource = new BindingList<CountingProduct>
            {
                new CountingProduct(1, "Alpha"), new CountingProduct(2, "Beta")
            },
            SearchDebounceMilliseconds = 0
        };
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        CountingProduct.NameReads = 0;

        lookup.SendKey(Keys.F4);

        Assert.That(CountingProduct.NameReads, Is.EqualTo(2));
    }

    [Test]
    public void OpenPopupHeightIncludesOverlayChromeAndHorizontalScrollbar()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = new TestLookup
        {
            Width = 200,
            DropDownWidth = 200,
            DisplayMember = "Name",
            ValueMember = "Id",
            DataSource = new BindingList<Product> { new Product(1, "Alpha") },
            SearchDebounceMilliseconds = 0
        };
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = "Name",
            HeaderText = "Product",
            Width = 400
        });
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();

        lookup.OpenDropDown();
        Application.DoEvents();

        var horizontalScrollBar = lookup.ResultsGrid.Controls.OfType<HScrollBar>().Single();
        var displayedRow = lookup.ResultsGrid.GetRowDisplayRectangle(0, false);
        var footer = lookup.ResultsGrid.Parent!.Controls.Cast<Control>()
            .Single(control => control.GetType().Name == "BootstrapLookupFooter");
        Assert.Multiple((Action)(() =>
        {
            Assert.That(horizontalScrollBar.Visible, Is.True);
            Assert.That(lookup.ResultsGrid.DisplayedRowCount(false), Is.EqualTo(1));
            Assert.That(displayedRow.Height, Is.EqualTo(lookup.ResultsGrid.Rows[0].Height));
            Assert.That(displayedRow.Bottom, Is.LessThanOrEqualTo(horizontalScrollBar.Top));
            Assert.That(lookup.ResultsGrid.Bottom, Is.LessThanOrEqualTo(footer.Top));
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

    [Test]
    public void PendingDebounceCannotReopenAfterEditorLosesFocus()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        using var next = new Button { Top = 50, Text = "Next" };
        lookup.SearchDebounceMilliseconds = 60;
        form.Controls.AddRange(new Control[] { lookup, next });
        form.Show(); lookup.Focus(); Application.DoEvents();

        lookup.Text = "Alpha";
        next.Focus();
        Application.DoEvents();
        PumpMessagesFor(180);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(next.Focused, Is.True);
            Assert.That(lookup.IsDropDownOpen, Is.False);
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ProgrammaticTextOnUnfocusedLookupDoesNotAutoOpen()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        using var focused = new Button { Top = 50 };
        lookup.SearchDebounceMilliseconds = 40;
        form.Controls.AddRange(new Control[] { lookup, focused });
        form.Show(); focused.Focus(); Application.DoEvents();

        lookup.Text = "Beta";
        PumpMessagesFor(140);

        Assert.That(lookup.IsDropDownOpen, Is.False);
        Assert.That(focused.Focused, Is.True);
    }

    private static void PumpMessagesFor(int milliseconds)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds)
        {
            Application.DoEvents();
            Thread.Sleep(5);
        }
        Application.DoEvents();
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

    private sealed class CountingProduct
    {
        private readonly string _name;
        internal CountingProduct(int id, string name) { Id = id; _name = name; }
        internal static int NameReads { get; set; }
        public int Id { get; }
        public string Name { get { NameReads++; return _name; } }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);
}
