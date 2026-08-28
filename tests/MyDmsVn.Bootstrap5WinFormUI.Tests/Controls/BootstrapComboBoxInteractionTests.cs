using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
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
    public void UnboundSelectionTransitionsIncludingNoOpMatchPlainComboBox()
    {
        using var form = CreateComparisonHost(out var native, out var bootstrap);
        native.Items.AddRange(new object[] { "Alpha", "Beta", "Gamma" });
        bootstrap.Items.AddRange(new object[] { "Alpha", "Beta", "Gamma" });

        var nativeIndexChanged = 0;
        var nativeValueChanged = 0;
        var nativeCommitted = 0;
        var bootstrapIndexChanged = 0;
        var bootstrapValueChanged = 0;
        var bootstrapCommitted = 0;
        native.SelectedIndexChanged += (_, _) => nativeIndexChanged++;
        native.SelectedValueChanged += (_, _) => nativeValueChanged++;
        native.SelectionChangeCommitted += (_, _) => nativeCommitted++;
        bootstrap.SelectedIndexChanged += (_, _) => bootstrapIndexChanged++;
        bootstrap.SelectedValueChanged += (_, _) => bootstrapValueChanged++;
        bootstrap.SelectionChangeCommitted += (_, _) => bootstrapCommitted++;

        foreach (var index in new[] { 0, 2, 2, 1 })
        {
            native.SelectedIndex = index;
            bootstrap.SelectedIndex = index;
            Application.DoEvents();
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrap.SelectedIndex, Is.EqualTo(native.SelectedIndex));
            Assert.That(bootstrap.SelectedItem, Is.EqualTo(native.SelectedItem));
            Assert.That(bootstrap.Text, Is.EqualTo(native.Text));
            Assert.That(bootstrapIndexChanged, Is.EqualTo(nativeIndexChanged));
            Assert.That(bootstrapValueChanged, Is.EqualTo(nativeValueChanged));
            Assert.That(bootstrapCommitted, Is.EqualTo(nativeCommitted));
            Assert.That(bootstrapCommitted, Is.EqualTo(0));
        }));
    }

    [Test]
    public void BoundRebindAndClearDataSourceMatchPlainComboBox()
    {
        var nativeFirst = CreateLookupSource("A");
        var bootstrapFirst = CreateLookupSource("A");
        var nativeSecond = CreateLookupSource("B");
        var bootstrapSecond = CreateLookupSource("B");
        using var form = CreateComparisonHost(out var native, out var bootstrap);

        ConfigureBinding(native, nativeFirst);
        ConfigureBinding(bootstrap, bootstrapFirst);
        Application.DoEvents();

        native.SelectedValue = 2;
        bootstrap.SelectedValue = 2;
        Application.DoEvents();
        AssertEquivalentBoundState(native, bootstrap);

        native.SelectedIndex = 0;
        bootstrap.SelectedIndex = 0;
        Application.DoEvents();
        AssertEquivalentBoundState(native, bootstrap);

        native.DataSource = nativeSecond;
        bootstrap.DataSource = bootstrapSecond;
        Application.DoEvents();
        AssertEquivalentBoundState(native, bootstrap);

        native.DataSource = null;
        bootstrap.DataSource = null;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrap.DataSource, Is.Null);
            Assert.That(bootstrap.Items.Count, Is.EqualTo(native.Items.Count));
            Assert.That(bootstrap.SelectedIndex, Is.EqualTo(native.SelectedIndex));
            Assert.That(bootstrap.Text, Is.EqualTo(native.Text));
        }));
    }

    [Test]
    public void FormattingAndGetItemTextMatchPlainComboBox()
    {
        using var form = CreateComparisonHost(out var native, out var bootstrap);
        native.FormattingEnabled = true;
        bootstrap.FormattingEnabled = true;
        var nativeItem = new LookupItem(1, "Alpha");
        var bootstrapItem = new LookupItem(1, "Alpha");
        native.Items.Add(nativeItem);
        bootstrap.Items.Add(bootstrapItem);
        native.Format += (_, e) => e.Value = "Formatted: " + ((LookupItem)e.ListItem!).Name;
        bootstrap.Format += (_, e) => e.Value = "Formatted: " + ((LookupItem)e.ListItem!).Name;

        native.SelectedIndex = 0;
        bootstrap.SelectedIndex = 0;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrap.GetItemText(bootstrapItem), Is.EqualTo(native.GetItemText(nativeItem)));
            Assert.That(bootstrap.Text, Is.EqualTo(native.Text));
            Assert.That(bootstrap.SelectedIndex, Is.EqualTo(native.SelectedIndex));
        }));
    }

    [Test]
    public void PresentationOnlyChangesDoNotRaiseAnyNativeSelectionEvents()
    {
        using var form = CreateHost(out var comboBox);
        comboBox.Items.AddRange(new object[] { "Alpha", "Beta" });
        comboBox.SelectedIndex = 1;
        var selectedIndexChanged = 0;
        var selectedValueChanged = 0;
        var selectionChangeCommitted = 0;
        comboBox.SelectedIndexChanged += (_, _) => selectedIndexChanged++;
        comboBox.SelectedValueChanged += (_, _) => selectedValueChanged++;
        comboBox.SelectionChangeCommitted += (_, _) => selectionChangeCommitted++;

        comboBox.ValidationState = BootstrapValidationState.Valid;
        comboBox.BorderRadius = 8;
        comboBox.LeadingIcon = IconDescriptor.Framework(FrameworkIconGlyph.Check);
        comboBox.IconRenderer = new RecordingIconRenderer();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(selectedIndexChanged, Is.EqualTo(0));
            Assert.That(selectedValueChanged, Is.EqualTo(0));
            Assert.That(selectionChangeCommitted, Is.EqualTo(0));
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
    public void ListItemsAutoCompleteRoundTripMatchesPlainComboBox()
    {
        using var form = CreateComparisonHost(out var native, out var bootstrap);
        native.Items.AddRange(new object[] { "Alpha", "Alpine", "Beta" });
        bootstrap.Items.AddRange(new object[] { "Alpha", "Alpine", "Beta" });

        native.DropDownStyle = ComboBoxStyle.DropDown;
        bootstrap.DropDownStyle = ComboBoxStyle.DropDown;
        native.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        bootstrap.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        native.AutoCompleteSource = AutoCompleteSource.ListItems;
        bootstrap.AutoCompleteSource = AutoCompleteSource.ListItems;
        native.Text = "Al";
        bootstrap.Text = "Al";
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrap.DropDownStyle, Is.EqualTo(native.DropDownStyle));
            Assert.That(bootstrap.AutoCompleteMode, Is.EqualTo(native.AutoCompleteMode));
            Assert.That(bootstrap.AutoCompleteSource, Is.EqualTo(native.AutoCompleteSource));
            Assert.That(bootstrap.Text, Is.EqualTo(native.Text));
        }));
    }

    [Test]
    public void DropDownListAutoCompleteRestrictionsMatchPlainComboBox()
    {
        using var native = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        using var bootstrap = new BootstrapComboBox { DropDownStyle = ComboBoxStyle.DropDownList };

        var nativeModeError = CaptureException(() => native.AutoCompleteMode = AutoCompleteMode.SuggestAppend);
        var bootstrapModeError = CaptureException(() => bootstrap.AutoCompleteMode = AutoCompleteMode.SuggestAppend);
        var nativeSourceError = CaptureException(() => native.AutoCompleteSource = AutoCompleteSource.ListItems);
        var bootstrapSourceError = CaptureException(() => bootstrap.AutoCompleteSource = AutoCompleteSource.ListItems);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrapModeError, Is.EqualTo(nativeModeError));
            Assert.That(bootstrapSourceError, Is.EqualTo(nativeSourceError));
            Assert.That(bootstrap.AutoCompleteMode, Is.EqualTo(native.AutoCompleteMode));
            Assert.That(bootstrap.AutoCompleteSource, Is.EqualTo(native.AutoCompleteSource));
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
    public void NativeKeyboardMessagesMatchPlainDropDownListBehavior()
    {
        using var form = new Form { ShowInTaskbar = false, Width = 520, Height = 180 };
        var native = new TestNativeComboBox
        {
            Left = 20,
            Top = 20,
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        var bootstrap = new TestBootstrapComboBox
        {
            Left = 260,
            Top = 20,
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        native.Items.AddRange(new object[] { "Alpha", "Beta", "Gamma" });
        bootstrap.Items.AddRange(new object[] { "Alpha", "Beta", "Gamma" });
        native.SelectedIndex = 0;
        bootstrap.SelectedIndex = 0;
        form.Controls.AddRange(new Control[] { native, bootstrap });
        form.Show();
        Application.DoEvents();

        foreach (var key in new[] { Keys.Down, Keys.Up, Keys.Enter, Keys.Escape })
        {
            native.Focus();
            Application.DoEvents();
            native.DispatchKey(key);
            Application.DoEvents();

            bootstrap.Focus();
            Application.DoEvents();
            bootstrap.DispatchKey(key);
            Application.DoEvents();

            Assert.That(bootstrap.SelectedIndex, Is.EqualTo(native.SelectedIndex), $"Selection diverged for {key}.");
        }
    }

    [Test]
    public void DisabledControlCannotTakeFocusAndReEnablePreservesNativeState()
    {
        using var form = CreateHost(out var comboBox);
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Items.AddRange(new object[] { "Alpha", "Beta", "Gamma" });
        comboBox.SelectedIndex = 1;
        comboBox.Enabled = false;
        Application.DoEvents();

        var focusedWhileDisabled = comboBox.Focus();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(focusedWhileDisabled, Is.False);
            Assert.That(comboBox.Focused || comboBox.ContainsFocus, Is.False);
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(comboBox.Items.Count, Is.EqualTo(3));
        }));

        comboBox.Enabled = true;
        Application.DoEvents();
        var focusedAfterEnable = comboBox.Focus();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(focusedAfterEnable, Is.True);
            Assert.That(comboBox.SelectedIndex, Is.EqualTo(1));
            Assert.That(comboBox.Items.Count, Is.EqualTo(3));
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

    private static Form CreateComparisonHost(out ComboBox native, out BootstrapComboBox bootstrap)
    {
        var form = new Form { ShowInTaskbar = false, Width = 520, Height = 180 };
        native = new ComboBox { Left = 20, Top = 20, Width = 200 };
        bootstrap = new BootstrapComboBox { Left = 260, Top = 20, Width = 200 };
        form.Controls.AddRange(new Control[] { native, bootstrap });
        form.Show();
        Application.DoEvents();
        return form;
    }

    private static void ConfigureBinding(ComboBox comboBox, List<LookupItem> source)
    {
        comboBox.DisplayMember = nameof(LookupItem.Name);
        comboBox.ValueMember = nameof(LookupItem.Id);
        comboBox.DataSource = source;
    }

    private static List<LookupItem> CreateLookupSource(string prefix)
    {
        return new List<LookupItem>
        {
            new LookupItem(1, prefix + " Alpha"),
            new LookupItem(2, prefix + " Beta")
        };
    }

    private static void AssertEquivalentBoundState(ComboBox native, BootstrapComboBox bootstrap)
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrap.SelectedIndex, Is.EqualTo(native.SelectedIndex));
            Assert.That(bootstrap.SelectedValue, Is.EqualTo(native.SelectedValue));
            Assert.That(bootstrap.Text, Is.EqualTo(native.Text));
            Assert.That(bootstrap.DisplayMember, Is.EqualTo(native.DisplayMember));
            Assert.That(bootstrap.ValueMember, Is.EqualTo(native.ValueMember));
        }));
    }

    private static Type? CaptureException(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex.GetType();
        }
    }

    private sealed class TestNativeComboBox : ComboBox
    {
        public void DispatchKey(Keys key)
        {
            var down = Message.Create(Handle, 0x0100, (IntPtr)key, IntPtr.Zero);
            WndProc(ref down);
            var up = Message.Create(Handle, 0x0101, (IntPtr)key, IntPtr.Zero);
            WndProc(ref up);
        }
    }

    private sealed class TestBootstrapComboBox : BootstrapComboBox
    {
        public void RaiseNativeKeyDown(KeyEventArgs e) => OnKeyDown(e);

        public void RaiseNativeKeyPress(KeyPressEventArgs e) => OnKeyPress(e);

        public void RaiseNativeKeyUp(KeyEventArgs e) => OnKeyUp(e);

        public void RaiseNativePreviewKeyDown(PreviewKeyDownEventArgs e) => OnPreviewKeyDown(e);

        public void DispatchKey(Keys key)
        {
            var down = Message.Create(Handle, 0x0100, (IntPtr)key, IntPtr.Zero);
            WndProc(ref down);
            var up = Message.Create(Handle, 0x0101, (IntPtr)key, IntPtr.Zero);
            WndProc(ref up);
        }
    }

    private sealed class RecordingIconRenderer : IIconRenderer
    {
        public bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color) => true;
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
