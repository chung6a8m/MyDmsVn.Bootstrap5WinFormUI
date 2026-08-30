using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapCalendarPickerTests
{
    [Test]
    public void PickerDefaultsExposeThePlannedShellContract()
    {
        using var picker = new BootstrapCalendarPicker();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(picker.SelectionMode, Is.EqualTo(BootstrapCalendarSelectionMode.Single));
            Assert.That(picker.SelectedDate, Is.Null);
            Assert.That(picker.DateFormat, Is.EqualTo("d"));
            Assert.That(picker.PlaceholderText, Is.Empty);
            Assert.That(picker.ValidationState, Is.EqualTo(BootstrapValidationState.None));
            Assert.That(picker.BorderRadius, Is.EqualTo(-1));
            Assert.That(picker.TabStop, Is.True);
            Assert.That(picker.AccessibleRole, Is.EqualTo(AccessibleRole.DropList));
        }));
    }

    [Test]
    public void PickerFormatsSingleRangeAndMultipleSummaries()
    {
        using var picker = new BootstrapCalendarPicker { DateFormat = "yyyy-MM-dd", PlaceholderText = "Choose" };
        var accessible = picker.AccessibilityObject;
        Assert.That(accessible.Value, Is.EqualTo("Choose"));

        picker.SelectedDate = new DateTime(2026, 8, 30);
        Assert.That(accessible.Value, Is.EqualTo("2026-08-30"));

        picker.SelectionMode = BootstrapCalendarSelectionMode.Range;
        picker.SetRange(new DateTime(2026, 8, 29), null);
        Assert.That(accessible.Value, Is.EqualTo("2026-08-29 – …"));
        picker.SetRange(new DateTime(2026, 8, 29), new DateTime(2026, 8, 31));
        Assert.That(accessible.Value, Is.EqualTo("2026-08-29 – 2026-08-31"));

        picker.SelectionMode = BootstrapCalendarSelectionMode.Multiple;
        picker.SetSelectedDates(new[] { new DateTime(2026, 8, 31), new DateTime(2026, 8, 29), new DateTime(2026, 8, 30) });
        Assert.That(accessible.Value, Is.EqualTo("2026-08-29 (+2)"));
    }

    [Test]
    public void PickerOpenCloseOwnsOnlyActiveReferenceAndForwardsLifecycle()
    {
        using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000), Size = new Size(400, 200) };
        using var picker = new BootstrapCalendarPicker { Location = new Point(20, 20) };
        form.Controls.Add(picker);
        form.Show();
        Application.DoEvents();
        var opened = 0;
        var closed = 0;
        picker.Opened += (_, _) => opened++;
        picker.Closed += (_, _) => closed++;

        picker.ShowDropDown();
        Application.DoEvents();
        Assert.That(GetActiveCalendar(picker), Is.Not.Null);
        Assert.That(picker.AccessibilityObject.State & AccessibleStates.Expanded, Is.Not.EqualTo(0));

        picker.CloseDropDown();
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(GetActiveCalendar(picker), Is.Null);
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(picker.AccessibilityObject.State & AccessibleStates.Collapsed, Is.Not.EqualTo(0));
        }));
    }

    [Test]
    public void PickerOwnerClickFollowingNativeAppClickedDismissalDoesNotImmediatelyReopen()
    {
        using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000), Size = new Size(400, 200) };
        using var picker = new BootstrapCalendarPicker { Location = new Point(20, 20), Size = new Size(240, 36) };
        form.Controls.Add(picker);
        form.Show();
        Application.DoEvents();

        picker.ShowDropDown();
        Application.DoEvents();
        GetNativeDropDown(GetPickerDropDown(picker)).Close(ToolStripDropDownCloseReason.AppClicked);
        SendHostedClick(picker);

        Assert.That(GetActiveCalendar(picker), Is.Null);
    }

    [Test]
    public void PickerDeclaredSurfaceStateAndBoundsFollowCalendarModelAtomically()
    {
        using var picker = new BootstrapCalendarPicker();
        var properties = typeof(BootstrapCalendarPicker).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Select(property => property.Name);
        Assert.That(properties, Is.EquivalentTo(new[] { "SelectionMode", "MinDate", "MaxDate", "SelectedDate", "RangeStart", "RangeEnd", "SelectedDates", "DateFormat", "PlaceholderText", "ValidationState", "BorderRadius" }));
        Assert.That(picker.Controls.Count, Is.Zero);
        Assert.Throws<InvalidOperationException>((Action)(() => picker.SetRange(DateTime.Today, DateTime.Today)));
        var previousMin = picker.MinDate;
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => picker.MinDate = BootstrapCalendarSelectionModel.MaximumSupportedDate.AddDays(1)));
        Assert.That(picker.MinDate, Is.EqualTo(previousMin));
        picker.SelectionMode = BootstrapCalendarSelectionMode.Range;
        picker.SetRange(new DateTime(2026, 8, 31), new DateTime(2026, 8, 29));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(picker.RangeStart, Is.EqualTo(new DateTime(2026, 8, 29)));
            Assert.That(picker.RangeEnd, Is.EqualTo(new DateTime(2026, 8, 31)));
        }));
    }

    [Test]
    public void PickerDeclaredEventsMethodsMetadataAndEmptyDefaultsAreExact()
    {
        using var picker = new BootstrapCalendarPicker();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapCalendarPicker).GetCustomAttribute<DefaultEventAttribute>()!.Name, Is.EqualTo("SelectionChanged"));
            Assert.That(typeof(BootstrapCalendarPicker).GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Select(e => e.Name), Is.EquivalentTo(new[] { "SelectionChanged", "Opened", "Closed" }));
            Assert.That(typeof(BootstrapCalendarPicker).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName).Select(m => m.Name), Is.EquivalentTo(new[] { "SetRange", "SetSelectedDates", "ClearSelection", "ShowDropDown", "CloseDropDown" }));
            Assert.That(picker.MinDate, Is.EqualTo(BootstrapCalendarSelectionModel.MinimumSupportedDate));
            Assert.That(picker.MaxDate, Is.EqualTo(BootstrapCalendarSelectionModel.MaximumSupportedDate));
            Assert.That(picker.RangeStart, Is.Null); Assert.That(picker.RangeEnd, Is.Null); Assert.That(picker.SelectedDates, Is.Empty); Assert.That(picker.Controls, Is.Empty);
        }));
    }

    [Test]
    public void PickerWrongModeAndInvalidDomainLeaveModelAndEventsUnchanged()
    {
        using var picker = new BootstrapCalendarPicker();
        picker.SelectedDate = new DateTime(2026, 8, 30); var events = 0; picker.SelectionChanged += (_, _) => events++;
        Assert.Throws<InvalidOperationException>((Action)(() => picker.SetSelectedDates(new[] { DateTime.Today })));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => picker.SelectedDate = picker.MaxDate.AddDays(1)));
        Assert.Multiple((Action)(() => { Assert.That(picker.SelectedDate, Is.EqualTo(new DateTime(2026, 8, 30))); Assert.That(events, Is.Zero); }));
    }

    [Test]
    public void PickerSynchronizesProgrammaticSelectionAndBoundsWhileOpenWithoutClosing()
    {
        using var form = CreatePickerHost(out var picker);
        picker.ShowDropDown(); Application.DoEvents();
        picker.SelectionMode = BootstrapCalendarSelectionMode.Range;
        picker.SetRange(new DateTime(2026, 8, 29), new DateTime(2026, 8, 31));
        picker.MinDate = new DateTime(2026, 8, 1);
        picker.MaxDate = new DateTime(2026, 9, 30);
        var active = GetActiveCalendar(picker)!;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(active.SelectionMode, Is.EqualTo(BootstrapCalendarSelectionMode.Range));
            Assert.That(active.RangeStart, Is.EqualTo(picker.RangeStart));
            Assert.That(active.RangeEnd, Is.EqualTo(picker.RangeEnd));
            Assert.That(active.MinDate, Is.EqualTo(picker.MinDate));
            Assert.That(active.MaxDate, Is.EqualTo(picker.MaxDate));
            Assert.That(picker.AccessibilityObject.State & AccessibleStates.Expanded, Is.Not.EqualTo(0));
        }));
    }

    [Test]
    public void PickerCompletionPolicyUsesCalendarActivationNotSelectionChanged()
    {
        using var form = CreatePickerHost(out var picker);
        var changes = 0; picker.SelectionChanged += (_, _) => changes++;
        picker.SelectedDate = new DateTime(2026, 8, 30); changes = 0;
        picker.ShowDropDown(); Application.DoEvents();
        ActivateDate(GetActiveCalendar(picker)!, new DateTime(2026, 8, 30)); Application.DoEvents();
        Assert.That(changes, Is.Zero); Assert.That(GetActiveCalendar(picker), Is.Null);
        picker.SelectionMode = BootstrapCalendarSelectionMode.Range;
        picker.ShowDropDown(); Application.DoEvents();
        ActivateDate(GetActiveCalendar(picker)!, new DateTime(2026, 8, 29)); Application.DoEvents();
        Assert.That(GetActiveCalendar(picker), Is.Not.Null);
        ActivateDate(GetActiveCalendar(picker)!, new DateTime(2026, 8, 31)); Application.DoEvents();
        Assert.That(GetActiveCalendar(picker), Is.Null);
        picker.SelectionMode = BootstrapCalendarSelectionMode.Multiple;
        picker.ShowDropDown(); Application.DoEvents();
        ActivateDate(GetActiveCalendar(picker)!, new DateTime(2026, 8, 30)); Application.DoEvents();
        Assert.That(GetActiveCalendar(picker), Is.Not.Null);
    }

    [Test]
    public void PickerDateFormatAndAccessibilityDefaultActionPreserveStateAndRespectDisabled()
    {
        using var form = CreatePickerHost(out var picker);
        picker.DateFormat = "yyyy-MM-dd";
        Assert.Throws<FormatException>((Action)(() => picker.DateFormat = "Q"));
        Assert.That(picker.DateFormat, Is.EqualTo("yyyy-MM-dd"));
        picker.AccessibleName = "Due date";
        Assert.That(picker.AccessibilityObject.Name, Is.EqualTo("Due date"));
        picker.AccessibilityObject.DoDefaultAction(); Application.DoEvents();
        Assert.That(GetActiveCalendar(picker), Is.Not.Null);
        picker.CloseDropDown(); Application.DoEvents();
        picker.Enabled = false;
        picker.AccessibilityObject.DoDefaultAction(); Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(GetActiveCalendar(picker), Is.Null);
            Assert.That(picker.AccessibilityObject.State & AccessibleStates.Unavailable, Is.Not.EqualTo(0));
        }));
    }

    [Test]
    public void PickerNullPlaceholderAndFixedCultureSummariesRemainDeterministic()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            using var picker = new BootstrapCalendarPicker { DateFormat = "yyyy-MM-dd", PlaceholderText = null! };
            Assert.That(picker.AccessibilityObject.Value, Is.Empty);
            picker.SelectedDate = new DateTime(2026, 8, 30); Assert.That(picker.AccessibilityObject.Value, Is.EqualTo("2026-08-30"));
            picker.SelectionMode = BootstrapCalendarSelectionMode.Multiple; picker.SetSelectedDates(new[] { new DateTime(2026, 8, 30) }); Assert.That(picker.AccessibilityObject.Value, Is.EqualTo("2026-08-30"));
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Test]
    public void PickerHostedCalendarIsFreshAndUsesSelectedSeedThenRetainedMonth()
    {
        using var form = CreatePickerHost(out var picker);
        picker.SelectedDate = new DateTime(2026, 8, 30); picker.ShowDropDown(); Application.DoEvents(); var first = GetActiveCalendar(picker)!;
        Assert.That(first.DisplayMonth, Is.EqualTo(new DateTime(2026, 8, 1)));
        first.DisplayMonth = new DateTime(2026, 9, 1); picker.CloseDropDown(); Application.DoEvents(); picker.ShowDropDown(); Application.DoEvents();
        Assert.Multiple((Action)(() => { Assert.That(GetActiveCalendar(picker), Is.Not.SameAs(first)); Assert.That(GetActiveCalendar(picker)!.DisplayMonth, Is.EqualTo(new DateTime(2026, 9, 1))); }));
    }

    [Test]
    public void PickerFactoryFailureClearsActiveReferenceAndLeavesPopupClosed()
    {
        using var form = CreatePickerHost(out var picker);
        var dropdown = GetPickerDropDown(picker);
        dropdown.Items[0].HostedControlFactory = () => throw new InvalidOperationException("factory");
        Assert.Throws<InvalidOperationException>((Action)(() => picker.ShowDropDown()));
        Assert.That(GetActiveCalendar(picker), Is.Null);
    }

    [TestCase(Keys.Enter)]
    [TestCase(Keys.Space)]
    [TestCase(Keys.F4)]
    [TestCase(Keys.Down | Keys.Alt)]
    public void PickerKeyboardTriggerTogglesOpenThenClosed(Keys key)
    {
        using var form = CreatePickerHost(out var picker);
        SendHostedKey(picker, key); Application.DoEvents(); Assert.That(GetActiveCalendar(picker), Is.Not.Null);
        SendHostedKey(picker, key); Application.DoEvents(); Assert.That(GetActiveCalendar(picker), Is.Null);
    }

    [Test]
    public void PickerMouseTriggerTogglesAndDisabledMouseIsNoOp()
    {
        using var form = CreatePickerHost(out var picker);
        SendHostedClick(picker); Assert.That(GetActiveCalendar(picker), Is.Not.Null);
        SendHostedClick(picker); Assert.That(GetActiveCalendar(picker), Is.Null);
        picker.Enabled = false; SendHostedClick(picker); Assert.That(GetActiveCalendar(picker), Is.Null);
    }

    [Test]
    public void PickerOpenedFocusesHostedCalendarAndClosedDetachesHandlers()
    {
        using var form = CreatePickerHost(out var picker);
        picker.ShowDropDown(); Application.DoEvents(); var active = GetActiveCalendar(picker)!;
        Assert.That(active.Focused, Is.True);
        picker.CloseDropDown(); Application.DoEvents(); Assert.That(GetActiveCalendar(picker), Is.Null);
    }

    [Test]
    public void HostedControlClickKeepsPopupOpenAndCanFocusImmediatelyAfterOpened()
    {
        using var form = CreateHost(out var presentationSource);
        using var dropdown = new BootstrapDropdown { Target = presentationSource };
        FocusableHostedProbe? probe = null;
        var opened = 0;
        var closed = 0;
        var focusedAfterOpened = false;
        dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = () => probe = new FocusableHostedProbe()
        });
        dropdown.Opened += (_, _) =>
        {
            opened++;
            focusedAfterOpened = probe!.Focus();
        };
        dropdown.Closed += (_, _) => closed++;

        dropdown.Show();
        Application.DoEvents();
        SendHostedClick(probe!);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.Zero);
            Assert.That(focusedAfterOpened, Is.True);
            Assert.That(probe!.Focused, Is.True);
            Assert.That(probe.ClickCount, Is.EqualTo(1));
        }));
    }

    [Test]
    public void HostedControlNavigationAndActivationKeysReachFocusedProbeWithoutClosingPopup()
    {
        using var form = CreateHost(out var presentationSource);
        using var dropdown = new BootstrapDropdown { Target = presentationSource };
        var probe = new FocusableHostedProbe();
        var opened = 0;
        var closed = 0;
        var keys = new[]
        {
            Keys.Left,
            Keys.Right,
            Keys.Up,
            Keys.Down,
            Keys.PageUp,
            Keys.PageDown,
            Keys.Home,
            Keys.End,
            Keys.Enter,
            Keys.Space
        };
        dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = () => probe
        });
        dropdown.Opened += (_, _) => opened++;
        dropdown.Closed += (_, _) => closed++;

        dropdown.Show();
        Application.DoEvents();
        var focusSucceeded = probe.Focus();
        Application.DoEvents();

        foreach (var key in keys)
        {
            SendHostedKey(probe, key);
            Application.DoEvents();
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(focusSucceeded, Is.True);
            Assert.That(probe.Focused, Is.True);
            Assert.That(probe.InputKeys.Distinct(), Is.EqualTo(keys));
            Assert.That(probe.KeyDownKeys, Is.EqualTo(keys));
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.Zero);
        }));
    }

    [Test]
    public void HostedControlExplicitCloseRaisesClosedExactlyOnce()
    {
        using var form = CreateHost(out var presentationSource);
        using var dropdown = new BootstrapDropdown { Target = presentationSource };
        dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = () => new FocusableHostedProbe()
        });
        var opened = 0;
        var closed = 0;
        dropdown.Opened += (_, _) => opened++;
        dropdown.Closed += (_, _) => closed++;

        dropdown.Show();
        Application.DoEvents();
        dropdown.Close();
        Application.DoEvents();
        dropdown.Close();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    private static Form CreateHost(out BootstrapButton presentationSource)
    {
        var form = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-10000, -10000),
            Size = new Size(480, 300)
        };
        presentationSource = new BootstrapButton
        {
            Text = "Open calendar",
            Location = new Point(24, 24),
            Size = new Size(180, 40)
        };
        form.Controls.Add(presentationSource);
        form.Show();
        form.Activate();
        Application.DoEvents();
        return form;
    }

    private static Form CreatePickerHost(out BootstrapCalendarPicker picker)
    {
        var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000), Size = new Size(480, 300) };
        picker = new BootstrapCalendarPicker { Location = new Point(24, 24), Size = new Size(240, 36) };
        form.Controls.Add(picker); form.Show(); Application.DoEvents(); return form;
    }

    private static void ActivateDate(BootstrapCalendar calendar, DateTime date)
    {
        var method = typeof(BootstrapCalendar).GetMethod("ActivateDate", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null); method!.Invoke(calendar, new object[] { date });
    }

    private static BootstrapCalendar? GetActiveCalendar(BootstrapCalendarPicker picker)
    {
        var field = typeof(BootstrapCalendarPicker).GetField("_activeCalendar", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (BootstrapCalendar?)field!.GetValue(picker);
    }

    private static BootstrapDropdown GetPickerDropDown(BootstrapCalendarPicker picker)
    {
        var field = typeof(BootstrapCalendarPicker).GetField("_dropdown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (BootstrapDropdown)field!.GetValue(picker)!;
    }

    private static ToolStripDropDownMenu GetNativeDropDown(BootstrapDropdown dropdown)
    {
        var field = typeof(BootstrapDropdown).GetField("_dropDown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (ToolStripDropDownMenu)field!.GetValue(dropdown)!;
    }

    private static void SendHostedClick(Control control)
    {
        var center = new Point(control.Width / 2, control.Height / 2);
        var lParam = CreateMouseLParam(center.X, center.Y);
        SendMessage(control.Handle, 0x0201, (IntPtr)1, lParam);
        SendMessage(control.Handle, 0x0202, IntPtr.Zero, lParam);
    }

    private static void SendHostedKey(Control control, Keys key)
    {
        Assert.That(PostMessage(control.Handle, 0x0100, (IntPtr)(int)key, IntPtr.Zero), Is.True);
        Assert.That(PostMessage(control.Handle, 0x0101, (IntPtr)(int)key, IntPtr.Zero), Is.True);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    private static IntPtr CreateMouseLParam(int x, int y)
    {
        return (IntPtr)((y << 16) | (x & 0xffff));
    }

    private sealed class FocusableHostedProbe : Control
    {
        private static readonly HashSet<Keys> CalendarKeys = new()
        {
            Keys.Left,
            Keys.Right,
            Keys.Up,
            Keys.Down,
            Keys.PageUp,
            Keys.PageDown,
            Keys.Home,
            Keys.End,
            Keys.Enter,
            Keys.Space
        };

        public FocusableHostedProbe()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Size = new Size(240, 36);
        }

        public int ClickCount { get; private set; }

        public List<Keys> InputKeys { get; } = new();

        public List<Keys> KeyDownKeys { get; } = new();

        protected override bool IsInputKey(Keys keyData)
        {
            var keyCode = keyData & Keys.KeyCode;
            if (CalendarKeys.Contains(keyCode))
            {
                InputKeys.Add(keyCode);
                return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnClick(EventArgs e)
        {
            ClickCount++;
            base.OnClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            KeyDownKeys.Add(e.KeyCode);
            base.OnKeyDown(e);
        }
    }
}
