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
    public void NavigationUsesLogicalValueWhenDifferentItemsCompareEqual()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = new TestLookup
        {
            DisplayMember = "Name",
            ValueMember = "Id",
            DataSource = new BindingList<EqualProduct>
            {
                new EqualProduct(1, "First"), new EqualProduct(2, "Second")
            },
            SearchDebounceMilliseconds = 0
        };
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        lookup.OpenDropDown();

        lookup.SendKey(Keys.Down);
        var highlightedBeforeCommit = (EqualProduct)lookup.HighlightedItem!;
        var selectedRowBeforeCommit = lookup.ResultsGrid.SelectedRows.Cast<DataGridViewRow>().Single().Index;
        var statusBeforeCommit = Descendants(lookup.ResultsGrid.Parent!).OfType<Label>()
            .Single(label => label.Text.IndexOf("/", StringComparison.Ordinal) >= 0).Text;
        lookup.SendKey(Keys.Enter);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(highlightedBeforeCommit.Id, Is.EqualTo(2));
            Assert.That(selectedRowBeforeCommit, Is.EqualTo(1));
            Assert.That(statusBeforeCommit, Is.EqualTo("2 / 2"));
            Assert.That(lookup.SelectedValue, Is.EqualTo(2));
        }));
    }

    [TestCase(Keys.Down)]
    [TestCase(Keys.PageDown)]
    public void NavigationUsesFirstVisibleResultCellWhenLeadingColumnIsHidden(Keys key)
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = "Id",
            HeaderText = "Id",
            Visible = false
        });
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = "Name",
            HeaderText = "Name"
        });
        lookup.ExecuteSearchNow();
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        lookup.OpenDropDown();

        lookup.SendKey(key);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.ResultsGrid.CurrentCell?.ColumnIndex, Is.EqualTo(1));
            Assert.That(lookup.ResultsGrid.CurrentCell?.RowIndex, Is.EqualTo(1));
            Assert.That(lookup.ResultsGrid.SelectedRows[0].Index, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NavigationWithNoVisibleResultColumnsStillMovesHighlight()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = "Id",
            HeaderText = "Id",
            Visible = false
        });
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = "Name",
            HeaderText = "Name",
            Visible = false
        });
        lookup.ExecuteSearchNow();
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        lookup.OpenDropDown();

        lookup.SendKey(Keys.Down);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.ResultsGrid.CurrentCell, Is.Null);
            Assert.That(lookup.ResultsGrid.SelectedRows[0].Index, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ProgrammaticSelectionAndClearSynchronizeAnOpenPopup()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        lookup.OpenDropDown();
        Application.DoEvents();

        lookup.SelectValue(2);
        var selectedRowAfterValueChange = lookup.ResultsGrid.SelectedRows.Cast<DataGridViewRow>().Single().Index;
        var currentRowAfterValueChange = lookup.ResultsGrid.CurrentCell?.RowIndex;

        lookup.ClearSelection();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(selectedRowAfterValueChange, Is.EqualTo(1));
            Assert.That(currentRowAfterValueChange, Is.EqualTo(1));
            Assert.That(lookup.ResultsGrid.SelectedRows, Is.Empty);
            Assert.That(lookup.ResultsGrid.CurrentCell, Is.Null);
        }));
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
    public void KeyboardOpenReusesSettledResultMaterialization()
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

        Assert.That(CountingProduct.NameReads, Is.Zero);
    }

    [Test]
    public void SettledResultNavigationDoesNotRematerializeRows()
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
        lookup.OpenDropDown();
        CountingProduct.NameReads = 0;

        lookup.SendKey(Keys.Down);
        lookup.SendKey(Keys.PageDown);
        lookup.SendKey(Keys.Home);

        Assert.That(CountingProduct.NameReads, Is.Zero);
    }

    [Test]
    public void FirstOpenAppliesConfigurationAddedAfterDataBinding()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        lookup.SearchMembers.Add("Name");
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = "Name",
            HeaderText = "Configured name"
        });
        lookup.ShowColumnHeaders = false;
        lookup.ShowRefreshButton = true;
        lookup.ShowAddNewButton = true;
        lookup.MinimumSearchLength = 1;
        form.Controls.Add(lookup);
        form.Show();

        lookup.OpenDropDown();

        var buttons = Descendants(lookup.ResultsGrid.Parent!).OfType<Button>().ToArray();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.ResultsGrid.Columns, Has.Count.EqualTo(1));
            Assert.That(lookup.ResultsGrid.Columns[0].HeaderText, Is.EqualTo("Configured name"));
            Assert.That(lookup.ResultsGrid.ColumnHeadersVisible, Is.False);
            Assert.That(buttons.Single(button => button.Text == "Refresh").Visible, Is.True);
            Assert.That(buttons.Single(button => button.Text == "Add New").Visible, Is.True);
            Assert.That(lookup.ResultsGrid.Rows, Is.Empty);
        }));
    }

    [Test]
    public void FirstOpenReappliesSearchMembersAddedAfterSettledQuery()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = new TestLookup
        {
            DisplayMember = "Name",
            ValueMember = "Id",
            DataSource = new BindingList<SearchProduct> { new SearchProduct(1, "Alpha", "SKU-1") },
            SearchDebounceMilliseconds = 0
        };
        lookup.Text = "sku";
        lookup.ExecuteSearchNow();
        Assert.That(lookup.ResultsGrid.Rows, Is.Empty);
        lookup.SearchMembers.Add("Code");
        form.Controls.Add(lookup);
        form.Show();

        lookup.OpenDropDown();

        Assert.That(lookup.ResultsGrid.Rows, Has.Count.EqualTo(1));
        Assert.That(lookup.ResultsGrid.Rows[0].Cells[0].Value, Is.EqualTo("Alpha"));
    }

    [Test]
    public void FirstOpenReappliesChangedSearchNormalizerAfterSettledQuery()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        lookup.Text = "alias";
        lookup.ExecuteSearchNow();
        Assert.That(lookup.ResultsGrid.Rows, Is.Empty);
        lookup.SearchTextNormalizer = value => string.Equals(value, "alias", StringComparison.OrdinalIgnoreCase)
            ? "alpha"
            : value.ToLowerInvariant();
        form.Controls.Add(lookup);
        form.Show();

        lookup.OpenDropDown();

        Assert.That(lookup.ResultsGrid.Rows, Has.Count.EqualTo(1));
        Assert.That(lookup.ResultsGrid.Rows[0].Cells[0].Value, Is.EqualTo("Alpha"));
    }

    [Test]
    public void ReopenAfterCommitRefreshesProjectionForCanonicalText()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        lookup.Text = "a";
        lookup.OpenDropDown();
        lookup.SendKey(Keys.End);
        lookup.SendKey(Keys.Enter);

        lookup.OpenDropDown();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.Text, Is.EqualTo("Beta"));
            Assert.That(lookup.ResultsGrid.Rows, Has.Count.EqualTo(1));
            Assert.That(lookup.ResultsGrid.Rows[0].Cells[0].Value, Is.EqualTo("Beta"));
        }));
    }

    [Test]
    public void EnterCannotCommitHighlightedItemWithNullLogicalValue()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = new TestLookup
        {
            DisplayMember = "Name",
            ValueMember = "Id",
            DataSource = new BindingList<NullableProduct>
            {
                new NullableProduct(1, "Valid"), new NullableProduct(null, "Missing")
            },
            SearchDebounceMilliseconds = 0
        };
        form.Controls.Add(lookup);
        form.Show();
        lookup.SelectValue(1);
        lookup.Text = string.Empty;
        lookup.OpenDropDown();
        lookup.SendKey(Keys.Down);

        lookup.SendKey(Keys.Enter);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
            Assert.That(lookup.ValidationMessage, Is.Not.Empty);
            Assert.That(lookup.IsDropDownOpen, Is.True);
        }));
    }

    [Test]
    public void MouseCannotCommitHighlightedItemWithNullLogicalValue()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = new TestLookup
        {
            DisplayMember = "Name",
            ValueMember = "Id",
            DataSource = new BindingList<NullableProduct>
            {
                new NullableProduct(1, "Valid"), new NullableProduct(null, "Missing")
            },
            SearchDebounceMilliseconds = 0
        };
        form.Controls.Add(lookup);
        form.Show();
        lookup.SelectValue(1);
        lookup.Text = string.Empty;
        lookup.OpenDropDown();

        ClickResultRow(lookup, 1);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
            Assert.That(lookup.ValidationMessage, Is.Not.Empty);
            Assert.That(lookup.IsDropDownOpen, Is.True);
        }));
    }

    [Test]
    public void FilteredResultMouseClickCommitsBeforeExternalLeaveResolution()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = new TestLookup
        {
            DisplayMember = "Name",
            ValueMember = "Id",
            DataSource = new BindingList<Product>
            {
                new Product(1, "Brazil"), new Product(2, "Brandy")
            },
            SearchDebounceMilliseconds = 0
        };
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        lookup.SelectValue(1);
        lookup.Text = "bran";
        Application.DoEvents();

        ClickResultRow(lookup, 0);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.SelectedValue, Is.EqualTo(2));
            Assert.That(lookup.Text, Is.EqualTo("Brandy"));
        }));
    }

    [TestCase("Refresh")]
    [TestCase("Add New")]
    public void FooterMouseActionRunsBeforePendingLeaveResolution(string action)
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        lookup.ShowRefreshButton = action == "Refresh";
        lookup.ShowAddNewButton = action == "Add New";
        var requests = 0;
        lookup.RefreshRequested += (_, _) => requests++;
        lookup.AddNewRequested += (_, e) =>
        {
            requests++;
            e.NewItem = new Product(3, "Gamma");
        };
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        lookup.SelectValue(1);
        lookup.Text = "unmatched";
        lookup.OpenDropDown();
        Application.DoEvents();
        var content = lookup.ResultsGrid.Parent!;
        var button = Descendants(content).OfType<Button>().Single(candidate => candidate.Text == action);

        ClickControl(button);

        Assert.That(requests, Is.EqualTo(1));
        if (action == "Refresh")
            Assert.That(lookup.Text, Is.EqualTo("unmatched"));
        else
            Assert.That(lookup.SelectedValue, Is.EqualTo(3));
    }

    [Test]
    public void RefreshClickReturnsKeyboardFocusToEditor()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        lookup.ShowRefreshButton = true;
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        lookup.OpenDropDown();
        var refresh = Descendants(lookup.ResultsGrid.Parent!).OfType<Button>()
            .Single(button => button.Text == "Refresh");

        ClickControl(refresh);
        SendCharacterToFocusedControl('b');
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.EditorFocused, Is.True);
            Assert.That(lookup.Text, Is.EqualTo("b"));
            Assert.That(lookup.IsDropDownOpen, Is.True);
        }));
    }

    [Test]
    public void HeaderClickReturnsKeyboardFocusToEditor()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        lookup.OpenDropDown();

        ClickResultArea(lookup, rowIndex: -1);
        SendCharacterToFocusedControl('b');
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.EditorFocused, Is.True);
            Assert.That(lookup.Text, Is.EqualTo("b"));
            Assert.That(lookup.IsDropDownOpen, Is.True);
        }));
    }

    [Test]
    public void ReadOnlyBlocksKeyboardCommitAndMouseOpen()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        form.Controls.Add(lookup);
        form.Show();
        lookup.SelectValue(1);
        lookup.ReadOnly = true;
        lookup.Text = "Beta";

        lookup.SendKey(Keys.Enter);
        lookup.SendKey(Keys.Tab);
        lookup.SendKey(Keys.F4);
        var affordance = Descendants(lookup).Single(control => control.GetType().Name == "BootstrapLookupDropDownAffordance");
        ClickControl(affordance);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
            Assert.That(lookup.Text, Is.EqualTo("Beta"));
            Assert.That(lookup.IsDropDownOpen, Is.False);
            Assert.That(affordance.Enabled, Is.False);
        }));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void LeavingNonInteractiveLookupDoesNotCommitPendingText(bool readOnly)
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        using var destination = new Button { Top = 50 };
        form.Controls.AddRange(new Control[] { lookup, destination });
        form.Show();
        lookup.Focus();
        lookup.SelectValue(1);
        lookup.Text = "Beta";
        if (readOnly) lookup.ReadOnly = true;
        else lookup.Enabled = false;

        destination.Focus();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
            Assert.That(lookup.Text, Is.EqualTo("Beta"));
            Assert.That(lookup.HasPendingText, Is.True);
        }));
    }

    [Test]
    public void MovingFocusToAnotherApplicationFormResolvesPendingText()
    {
        using var firstForm = new Form { ShowInTaskbar = false };
        using var secondForm = new Form { ShowInTaskbar = false, Left = 400 };
        using var lookup = Create();
        using var destination = new Button();
        firstForm.Controls.Add(lookup);
        secondForm.Controls.Add(destination);
        firstForm.Show();
        secondForm.Show();
        firstForm.Activate();
        lookup.Focus();
        lookup.SelectValue(1);
        lookup.Text = "unmatched";
        lookup.CloseDropDown();
        Application.DoEvents();

        secondForm.Activate();
        Assert.That(destination.Focus(), Is.True);
        lookup.SendLeave();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.Text, Is.EqualTo("Alpha"));
            Assert.That(lookup.HasPendingText, Is.False);
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
        }));
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
        lookup.ExecuteSearchNow();
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
    public void ReopenSearchesTextPreservedWhenPendingDebounceWasCanceled()
    {
        using var form = new Form { ShowInTaskbar = false };
        using var lookup = Create();
        lookup.SearchDebounceMilliseconds = 500;
        form.Controls.Add(lookup);
        form.Show();
        lookup.Focus();
        lookup.Text = "Bet";

        lookup.Visible = false;
        lookup.Visible = true;
        lookup.OpenDropDown();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.Text, Is.EqualTo("Bet"));
            Assert.That(lookup.ResultsGrid.Rows, Has.Count.EqualTo(1));
            Assert.That(lookup.ResultsGrid.Rows[0].Cells[0].Value, Is.EqualTo("Beta"));
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

    private static void ClickResultRow(BootstrapLookupBox lookup, int rowIndex)
    {
        ClickResultArea(lookup, rowIndex);
    }

    private static void ClickResultArea(BootstrapLookupBox lookup, int rowIndex)
    {
        var grid = lookup.ResultsGrid;
        Assert.That(grid.Focus(), Is.True);
        var onCellMouseClick = typeof(DataGridView).GetMethod(
            "OnCellMouseClick",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        onCellMouseClick.Invoke(grid, new object[]
        {
            new DataGridViewCellMouseEventArgs(0, rowIndex, 4, 4, new MouseEventArgs(MouseButtons.Left, 1, 4, 4, 0))
        });
        Application.DoEvents();
    }

    private static void ClickControl(Control control, int x = 4, int y = 4)
    {
        var coordinates = new IntPtr((x & 0xffff) | ((y & 0xffff) << 16));
        control.Focus();
        SendMessage(control.Handle, 0x0201, (IntPtr)1, coordinates);
        SendMessage(control.Handle, 0x0202, IntPtr.Zero, coordinates);
        Application.DoEvents();
    }

    private static void SendCharacterToFocusedControl(char value)
    {
        var focusedHandle = GetFocus();
        Assert.That(focusedHandle, Is.Not.EqualTo(IntPtr.Zero));
        SendMessage(focusedHandle, 0x0102, (IntPtr)value, IntPtr.Zero);
    }

    private static System.Collections.Generic.IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
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
        internal void SendLeave() => OnLeave(EventArgs.Empty);
    }

    private sealed class Product
    {
        internal Product(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }

    private sealed class NullableProduct
    {
        internal NullableProduct(int? id, string name) { Id = id; Name = name; }
        public int? Id { get; }
        public string Name { get; }
    }

    private sealed class EqualProduct
    {
        internal EqualProduct(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
        public override bool Equals(object? obj) => obj is EqualProduct;
        public override int GetHashCode() => 0;
    }

    private sealed class SearchProduct
    {
        internal SearchProduct(int id, string name, string code) { Id = id; Name = name; Code = code; }
        public int Id { get; }
        public string Name { get; }
        public string Code { get; }
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

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetFocus();
}
