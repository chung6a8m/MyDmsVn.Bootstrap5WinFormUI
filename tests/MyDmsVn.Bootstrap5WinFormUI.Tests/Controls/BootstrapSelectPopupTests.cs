using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapSelectPopupTests
{
    private const int WmActivateApp = 0x001C;

    [Test]
    public void PopupIsLazyReusedAndRaisesLifecycleEvents()
    {
        using var form = new Form { Size = new Size(500, 400), StartPosition = FormStartPosition.Manual, Location = new Point(100, 100) };
        using var select = new BootstrapSelect { Location = new Point(30, 30), Width = 240 };
        select.Items.Add(new BootstrapSelectItem(1, "Alpha"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        var opened = 0;
        var closed = 0;
        select.DropDownOpened += (_, _) => opened++;
        select.DropDownClosed += (_, _) => closed++;
        Assert.That(select.IsDropDownCreatedForTest, Is.False);

        select.OpenDropDownInternal();
        Application.DoEvents();
        var creationCount = select.DropDownCreationCountForTest;
        var firstBounds = select.DropDownBoundsForTest;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.True);
            Assert.That(creationCount, Is.EqualTo(1));
            Assert.That(firstBounds.Width, Is.GreaterThanOrEqualTo(select.Width));
            Assert.That(opened, Is.EqualTo(1));
        }));

        select.CloseDropDownInternal(false);
        select.OpenDropDownInternal();
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.DropDownCreationCountForTest, Is.EqualTo(creationCount));
            Assert.That(opened, Is.EqualTo(2));
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void OwningFormDeactivateClosesOpenPopup()
    {
        using var form = new TestForm();
        using var select = new BootstrapSelect();
        select.Items.Add(new BootstrapSelectItem(1, "Alpha"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();
        var closed = 0;
        select.DropDownClosed += (_, _) => closed++;

        select.OpenDropDownInternal();
        Application.DoEvents();
        Assert.That(select.IsDropDownOpenForTest, Is.True);

        form.RaiseDeactivate();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.False);
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ApplicationDeactivateMessageAfterPopupFocusClosesOpenPopup()
    {
        using var form = new Form();
        using var select = new BootstrapSelect { SearchEnabled = true };
        select.Items.Add(new BootstrapSelectItem(1, "Alpha"));
        form.Controls.Add(select);
        form.Show();
        form.Activate();
        select.Focus();
        Application.DoEvents();
        var closed = 0;
        select.DropDownClosed += (_, _) => closed++;

        select.OpenDropDownInternal();
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.True);
            Assert.That(select.DropDownHandleForTest, Is.Not.EqualTo(IntPtr.Zero));
        }));

        SendMessage(select.DropDownHandleForTest, WmActivateApp, IntPtr.Zero, IntPtr.Zero);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.False);
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void LocalSinglePopupKeepsStableHeightAcrossRepeatedOpenCycles()
    {
        using var form = new Form
        {
            Size = new Size(600, 500),
            StartPosition = FormStartPosition.Manual,
            Location = new Point(100, 100)
        };
        using var select = new BootstrapSelect { Location = new Point(30, 30), Width = 340 };
        select.Items.Add(new BootstrapSelectItem(1, "Contoso"));
        select.Items.Add(new BootstrapSelectItem(2, "Fabrikam"));
        select.Items.Add(new BootstrapSelectItem(3, "Northwind"));
        select.Items.Add(new BootstrapSelectItem(4, "Adventure Works") { Disabled = true });
        select.Items.Add(new BootstrapSelectItem(
            5,
            "Tailspin Toys — a deliberately long customer caption used to verify ellipsis and popup width behavior"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        var heights = new int[3];
        for (var cycle = 0; cycle < heights.Length; cycle++)
        {
            select.OpenDropDownInternal();
            Application.DoEvents();

            heights[cycle] = select.DropDownBoundsForTest.Height;
            Assert.Multiple((Action)(() =>
            {
                Assert.That(select.IsDropDownOpenForTest, Is.True);
                Assert.That(select.DropDownCreationCountForTest, Is.EqualTo(1));
                Assert.That(select.VisibleResultItemTextsForTest, Has.Count.EqualTo(5));
            }));

            select.CloseDropDownInternal(false);
            Application.DoEvents();
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(heights[1], Is.EqualTo(heights[0]));
            Assert.That(heights[2], Is.EqualTo(heights[0]));
        }));
    }

    [Test]
    public void LocalSearchFiltersRowsAndActivatedRowUsesSelectionPipeline()
    {
        using var form = new Form();
        using var select = new BootstrapSelect();
        select.Items.Add(new BootstrapSelectItem(1, "Alpha"));
        select.Items.Add(new BootstrapSelectItem(2, "Beta"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        select.OpenDropDownInternal();
        select.SetSearchTextForTest("be");
        Application.DoEvents();

        Assert.That(select.VisibleResultItemTextsForTest, Is.EqualTo(new[] { "Beta" }));
        Assert.That(select.ActivateHighlightedResultForTest(), Is.True);
        Assert.That(select.SelectedValue, Is.EqualTo(2));
        Assert.That(select.IsDropDownOpenForTest, Is.False);
    }

    [Test]
    public void DisabledRowsAreSkippedByKeyboardNavigation()
    {
        using var form = new Form();
        using var select = new BootstrapSelect();
        select.Items.Add(new BootstrapSelectItem(1, "Disabled") { Disabled = true });
        select.Items.Add(new BootstrapSelectItem(2, "Enabled"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        select.OpenDropDownInternal();
        Assert.That(select.HighlightedResultTextForTest, Is.EqualTo("Enabled"));
    }

    [Test]
    public void SelectionRefreshWithCloseOnSelectFalsePreservesNavigation()
    {
        using var form = new Form { Size = new Size(600, 500) };
        using var select = new BootstrapSelect
        {
            SelectionMode = BootstrapSelectMode.Multiple,
            CloseOnSelect = false,
            Width = 320
        };
        for (var value = 1; value <= 12; value++)
        {
            select.Items.Add(new BootstrapSelectItem(value, "Item " + value));
        }
        Assert.That(select.SelectValue(1), Is.True);
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        select.OpenDropDownInternal();
        Application.DoEvents();
        Assert.That(select.MoveHighlightedResultForTest(8), Is.True);
        var highlighted = select.HighlightedResultTextForTest;
        var scrollOffset = select.ResultScrollOffsetForTest;

        Assert.That(select.ActivateHighlightedResultForTest(), Is.True);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.True);
            Assert.That(select.HighlightedResultTextForTest, Is.EqualTo(highlighted));
            Assert.That(select.ResultScrollOffsetForTest, Is.EqualTo(scrollOffset));
        }));
    }

    [Test]
    public void OwnerDpiRefreshReappliesOpenPopupWithoutRecreation()
    {
        var workingArea = Screen.PrimaryScreen!.WorkingArea;
        var formSize = new Size(640, 520);
        using var form = new Form
        {
            Size = formSize,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(
                workingArea.Left + Math.Max(0, (workingArea.Width - formSize.Width) / 2),
                workingArea.Top + Math.Max(0, (workingArea.Height - formSize.Height) / 2))
        };
        using var select = new BootstrapSelect
        {
            Location = new Point(150, 220),
            Width = 320,
            ResultRowHeight = 48,
            MaxDropDownHeight = 120
        };
        for (var value = 1; value <= 12; value++)
        {
            select.Items.Add(new BootstrapSelectItem(value, "Item " + value));
        }
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        select.OpenDropDownInternal();
        Application.DoEvents();
        var creationCount = select.DropDownCreationCountForTest;

        foreach (var dpi in new[] { 96, 144, 192 })
        {
            select.ApplyDropDownDpiForTest(dpi);
            Application.DoEvents();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(select.IsDropDownOpenForTest, Is.True);
                Assert.That(select.DropDownCreationCountForTest, Is.EqualTo(creationCount));
                Assert.That(select.EffectiveResultRowHeightForTest, Is.EqualTo(DpiScaler.Scale(48, dpi)));
                Assert.That(select.DropDownBoundsForTest.Height, Is.LessThanOrEqualTo(DpiScaler.Scale(120, dpi)));
                Assert.That(Screen.FromControl(select).WorkingArea.Contains(select.DropDownBoundsForTest), Is.True);
            }));
        }
    }

    [Test]
    public void OwnerDpiAppliedBeforeFirstOpenControlsInitialPopupPresentation()
    {
        using var form = new Form { Size = new Size(700, 600), StartPosition = FormStartPosition.Manual, Location = new Point(100, 100) };
        using var select = new BootstrapSelect { Width = 300, ResultRowHeight = 48 };
        select.Items.Add(new BootstrapSelectItem(1, "Alpha"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        select.ApplyDropDownDpiForTest(144);
        Assert.That(select.IsDropDownCreatedForTest, Is.False);

        select.OpenDropDownInternal();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.True);
            Assert.That(select.DropDownCreationCountForTest, Is.EqualTo(1));
            Assert.That(select.EffectiveResultRowHeightForTest, Is.EqualTo(72));
        }));
    }

    [Test]
    public void OwnerDpiRefreshUpdatesCreatedClosedPopupForNextOpen()
    {
        using var form = new Form { Size = new Size(700, 600), StartPosition = FormStartPosition.Manual, Location = new Point(100, 100) };
        using var select = new BootstrapSelect { Width = 300, ResultRowHeight = 48 };
        select.Items.Add(new BootstrapSelectItem(1, "Alpha"));
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        select.OpenDropDownInternal();
        select.CloseDropDownInternal(false);
        Application.DoEvents();
        var creationCount = select.DropDownCreationCountForTest;

        select.ApplyDropDownDpiForTest(144);
        select.OpenDropDownInternal();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.True);
            Assert.That(select.DropDownCreationCountForTest, Is.EqualTo(creationCount));
            Assert.That(select.EffectiveResultRowHeightForTest, Is.EqualTo(72));
        }));
    }

    [Test]
    public void ChangingResultRowHeightReflowsOpenPopupWithoutRecreation()
    {
        using var form = new Form { Size = new Size(800, 700), StartPosition = FormStartPosition.Manual, Location = new Point(100, 100) };
        using var select = new BootstrapSelect
        {
            Location = new Point(40, 40),
            Width = 320,
            MaxDropDownHeight = 320
        };
        for (var value = 1; value <= 12; value++)
        {
            select.Items.Add(new BootstrapSelectItem(value, "Item " + value));
        }
        form.Controls.Add(select);
        form.Show();
        Application.DoEvents();

        select.OpenDropDownInternal();
        Application.DoEvents();
        var creationCount = select.DropDownCreationCountForTest;
        var initialBounds = select.DropDownBoundsForTest;

        select.ResultRowHeight = 48;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(select.IsDropDownOpenForTest, Is.True);
            Assert.That(select.DropDownCreationCountForTest, Is.EqualTo(creationCount));
            Assert.That(select.EffectiveResultRowHeightForTest, Is.EqualTo(48));
            Assert.That(select.DropDownBoundsForTest.Height, Is.GreaterThan(initialBounds.Height));
            Assert.That(select.DropDownBoundsForTest.Height, Is.LessThanOrEqualTo(320));
            Assert.That(Screen.FromControl(select).WorkingArea.Contains(select.DropDownBoundsForTest), Is.True);
        }));
    }

    private sealed class TestForm : Form
    {
        internal void RaiseDeactivate()
        {
            OnDeactivate(EventArgs.Empty);
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
}
