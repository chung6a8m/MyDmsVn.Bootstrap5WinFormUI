using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapComboBoxInteractionTests
{
    [Test]
    public void DropDownListSelectionUsesInheritedNativeEventPathExactlyOnce()
    {
        using var form = CreateHost(out var comboBox);
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Items.AddRange(new object[] { "Alpha", "Beta", "Gamma" });
        var selectedIndexChanged = 0;
        var selectedValueChanged = 0;
        var selectionChangeCommitted = 0;
        comboBox.SelectedIndexChanged += (_, _) => selectedIndexChanged++;
        comboBox.SelectedValueChanged += (_, _) => selectedValueChanged++;
        comboBox.SelectionChangeCommitted += (_, _) => selectionChangeCommitted++;

        comboBox.SelectedIndex = 1;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(comboBox.SelectedItem, Is.EqualTo("Beta"));
            Assert.That(comboBox.Text, Is.EqualTo("Beta"));
            Assert.That(selectedIndexChanged, Is.EqualTo(1));
            Assert.That(selectedValueChanged, Is.EqualTo(1));
            Assert.That(selectionChangeCommitted, Is.EqualTo(0), "Programmatic native selection must not synthesize a committed-user event.");
        }));
    }

    [Test]
    public void EditableModeRetainsNativeTextSelectionAndAutoCompleteConfiguration()
    {
        using var form = CreateHost(out var comboBox);
        comboBox.DropDownStyle = ComboBoxStyle.DropDown;
        comboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        comboBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
        comboBox.AutoCompleteCustomSource.AddRange(new[] { "Alpha", "Alpine", "Beta" });
        comboBox.Text = "Al";
        comboBox.SelectionStart = 1;
        comboBox.SelectionLength = 1;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDown));
            Assert.That(comboBox.AutoCompleteMode, Is.EqualTo(AutoCompleteMode.SuggestAppend));
            Assert.That(comboBox.AutoCompleteSource, Is.EqualTo(AutoCompleteSource.CustomSource));
            Assert.That(comboBox.AutoCompleteCustomSource, Is.EquivalentTo(new[] { "Alpha", "Alpine", "Beta" }));
            Assert.That(comboBox.Text, Is.EqualTo("Al"));
            Assert.That(comboBox.SelectionStart, Is.EqualTo(1));
            Assert.That(comboBox.SelectionLength, Is.EqualTo(1));
        }));
    }

    [Test]
    public void BoundEditableModeRetainsNativeDisplayValueAndTextMembers()
    {
        var source = new List<LookupItem>
        {
            new LookupItem(1, "Alpha"),
            new LookupItem(2, "Beta")
        };
        using var form = CreateHost(out var comboBox);
        comboBox.DropDownStyle = ComboBoxStyle.DropDown;
        comboBox.DisplayMember = nameof(LookupItem.Name);
        comboBox.ValueMember = nameof(LookupItem.Id);
        comboBox.DataSource = source;
        Application.DoEvents();

        comboBox.SelectedValue = 2;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(comboBox.SelectedItem, Is.SameAs(source[1]));
            Assert.That(comboBox.SelectedValue, Is.EqualTo(2));
            Assert.That(comboBox.Text, Is.EqualTo("Beta"));
        }));
    }

    [Test]
    public void NativeDropDownOpenCloseLifecycleIsNotReEmittedByFramework()
    {
        using var form = CreateHost(out var comboBox);
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Items.AddRange(new object[] { "Alpha", "Beta", "Gamma" });
        comboBox.SelectedIndex = 0;
        var dropDownCount = 0;
        var dropDownClosedCount = 0;
        comboBox.DropDown += (_, _) => dropDownCount++;
        comboBox.DropDownClosed += (_, _) => dropDownClosedCount++;

        comboBox.DroppedDown = true;
        Application.DoEvents();
        comboBox.DroppedDown = false;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.DroppedDown, Is.False);
            Assert.That(dropDownCount, Is.EqualTo(1));
            Assert.That(dropDownClosedCount, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ComboBoxRemainsSingleNativeTabStopBetweenSiblingControls()
    {
        using var form = new Form { ShowInTaskbar = false, Width = 360, Height = 180 };
        var before = new TextBox { Left = 20, Top = 20, Width = 120, TabIndex = 0 };
        var comboBox = new BootstrapComboBox { Left = 20, Top = 55, Width = 180, TabIndex = 1 };
        var after = new TextBox { Left = 20, Top = 95, Width = 120, TabIndex = 2 };
        form.Controls.AddRange(new Control[] { before, comboBox, after });
        form.Show();
        Application.DoEvents();

        before.Focus();
        Application.DoEvents();
        Assert.That(form.SelectNextControl(before, true, true, true, true), Is.True);
        Application.DoEvents();
        Assert.That(comboBox.Focused || comboBox.ContainsFocus, Is.True);

        Assert.That(form.SelectNextControl(comboBox, true, true, true, true), Is.True);
        Application.DoEvents();
        Assert.That(after.Focused, Is.True);

        Assert.That(form.SelectNextControl(after, false, true, true, true), Is.True);
        Application.DoEvents();
        Assert.That(comboBox.Focused || comboBox.ContainsFocus, Is.True);
    }

    [Test]
    public void NativeKeyboardAndPreviewEventsRemainInheritedWithoutFrameworkForwarding()
    {
        using var comboBox = new TestBootstrapComboBox();
        var keyDownCount = 0;
        var keyPressCount = 0;
        var keyUpCount = 0;
        var previewKeyDownCount = 0;
        comboBox.KeyDown += (_, _) => keyDownCount++;
        comboBox.KeyPress += (_, _) => keyPressCount++;
        comboBox.KeyUp += (_, _) => keyUpCount++;
        comboBox.PreviewKeyDown += (_, _) => previewKeyDownCount++;

        comboBox.RaiseNativeKeyDown(new KeyEventArgs(Keys.Down));
        comboBox.RaiseNativeKeyPress(new KeyPressEventArgs('a'));
        comboBox.RaiseNativeKeyUp(new KeyEventArgs(Keys.Down));
        comboBox.RaiseNativePreviewKeyDown(new PreviewKeyDownEventArgs(Keys.Tab));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(keyDownCount, Is.EqualTo(1));
            Assert.That(keyPressCount, Is.EqualTo(1));
            Assert.That(keyUpCount, Is.EqualTo(1));
            Assert.That(previewKeyDownCount, Is.EqualTo(1));
        }));
    }

    private static Form CreateHost(out BootstrapComboBox comboBox)
    {
        var form = new Form { ShowInTaskbar = false, Width = 360, Height = 180 };
        comboBox = new BootstrapComboBox { Left = 20, Top = 20, Width = 220 };
        form.Controls.Add(comboBox);
        form.Show();
        Application.DoEvents();
        return form;
    }

    private sealed class TestBootstrapComboBox : BootstrapComboBox
    {
        public void RaiseNativeKeyDown(KeyEventArgs e) => OnKeyDown(e);

        public void RaiseNativeKeyPress(KeyPressEventArgs e) => OnKeyPress(e);

        public void RaiseNativeKeyUp(KeyEventArgs e) => OnKeyUp(e);

        public void RaiseNativePreviewKeyDown(PreviewKeyDownEventArgs e) => OnPreviewKeyDown(e);
    }

    private sealed class LookupItem
    {
        public LookupItem(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }
}
