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
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapCalendarPickerTests
{
    [Test]
    public void PickerMetadataAndCompleteDefaultMatrixAreExact()
    {
        using var picker = new BootstrapCalendarPicker();
        var type = typeof(BootstrapCalendarPicker);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(GetDefaultValue(type, nameof(BootstrapCalendarPicker.SelectionMode)), Is.EqualTo(BootstrapCalendarSelectionMode.Single));
            Assert.That(GetDefaultValue(type, nameof(BootstrapCalendarPicker.SelectedDate)), Is.Null);
            Assert.That(GetDefaultValue(type, nameof(BootstrapCalendarPicker.DateFormat)), Is.EqualTo("d"));
            Assert.That(GetDefaultValue(type, nameof(BootstrapCalendarPicker.PlaceholderText)), Is.EqualTo(string.Empty));
            Assert.That(GetDefaultValue(type, nameof(BootstrapCalendarPicker.ValidationState)), Is.EqualTo(BootstrapValidationState.None));
            Assert.That(GetDefaultValue(type, nameof(BootstrapCalendarPicker.BorderRadius)), Is.EqualTo(-1));
            Assert.That(type.GetProperty(nameof(BootstrapCalendarPicker.MinDate))!.GetCustomAttribute<DefaultValueAttribute>(), Is.Null);
            Assert.That(type.GetProperty(nameof(BootstrapCalendarPicker.MaxDate))!.GetCustomAttribute<DefaultValueAttribute>(), Is.Null);
            Assert.That(type.GetProperty(nameof(BootstrapCalendarPicker.RangeStart))!.GetCustomAttribute<DefaultValueAttribute>(), Is.Null);
            Assert.That(type.GetProperty(nameof(BootstrapCalendarPicker.RangeEnd))!.GetCustomAttribute<DefaultValueAttribute>(), Is.Null);
            Assert.That(type.GetProperty(nameof(BootstrapCalendarPicker.SelectedDates))!.GetCustomAttribute<DefaultValueAttribute>(), Is.Null);
            Assert.That(type.GetProperty(nameof(BootstrapCalendarPicker.SelectedDates))!.GetCustomAttribute<BrowsableAttribute>()!.Browsable, Is.False);
            Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(property => property.Name != nameof(BootstrapCalendarPicker.SelectedDates))
                .Select(property => property.GetCustomAttribute<BrowsableAttribute>()), Is.All.Null);

            Assert.That(picker.SelectionMode, Is.EqualTo(BootstrapCalendarSelectionMode.Single));
            Assert.That(picker.MinDate, Is.EqualTo(BootstrapCalendarSelectionModel.MinimumSupportedDate));
            Assert.That(picker.MaxDate, Is.EqualTo(BootstrapCalendarSelectionModel.MaximumSupportedDate));
            Assert.That(picker.SelectedDate, Is.Null);
            Assert.That(picker.RangeStart, Is.Null);
            Assert.That(picker.RangeEnd, Is.Null);
            Assert.That(picker.SelectedDates, Is.Empty);
            Assert.That(picker.DateFormat, Is.EqualTo("d"));
            Assert.That(picker.PlaceholderText, Is.Empty);
            Assert.That(picker.ValidationState, Is.EqualTo(BootstrapValidationState.None));
            Assert.That(picker.BorderRadius, Is.EqualTo(-1));
            Assert.That(picker.TabStop, Is.True);
            Assert.That(picker.AccessibleRole, Is.EqualTo(AccessibleRole.DropList));
            Assert.That(picker.AccessibilityObject.State & AccessibleStates.Collapsed, Is.Not.Zero);
        }));
    }

    [Test]
    public void PickerInvalidBoundsAndSelectionsAreAtomicWithExactEventCounts()
    {
        using var picker = new BootstrapCalendarPicker
        {
            MinDate = new DateTime(2026, 1, 1),
            MaxDate = new DateTime(2026, 12, 31),
            SelectedDate = new DateTime(2026, 6, 15)
        };
        var events = 0;
        picker.SelectionChanged += (_, _) => events++;

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => picker.MinDate = new DateTime(2027, 1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => picker.MaxDate = new DateTime(2025, 12, 31)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => picker.MinDate = BootstrapCalendarSelectionModel.MinimumSupportedDate.AddDays(-1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => picker.MaxDate = BootstrapCalendarSelectionModel.MaximumSupportedDate.AddDays(1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => picker.SelectedDate = new DateTime(2025, 12, 31)));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(picker.MinDate, Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(picker.MaxDate, Is.EqualTo(new DateTime(2026, 12, 31)));
            Assert.That(picker.SelectedDate, Is.EqualTo(new DateTime(2026, 6, 15)));
            Assert.That(events, Is.Zero);
        }));

        picker.SelectionMode = BootstrapCalendarSelectionMode.Range;
        picker.SetRange(new DateTime(2026, 4, 1), new DateTime(2026, 4, 5));
        events = 0;
        Assert.Throws<ArgumentException>((Action)(() => picker.SetRange(null, new DateTime(2026, 4, 5))));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => picker.SetRange(new DateTime(2025, 12, 31), new DateTime(2026, 4, 5))));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => picker.SetRange(new DateTime(2026, 4, 1), new DateTime(2027, 1, 1))));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(picker.RangeStart, Is.EqualTo(new DateTime(2026, 4, 1)));
            Assert.That(picker.RangeEnd, Is.EqualTo(new DateTime(2026, 4, 5)));
            Assert.That(events, Is.Zero);
        }));

        picker.SelectionMode = BootstrapCalendarSelectionMode.Multiple;
        picker.SetSelectedDates(new[] { new DateTime(2026, 3, 1), new DateTime(2026, 3, 2) });
        events = 0;
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => picker.SetSelectedDates(new[]
        {
            new DateTime(2026, 5, 1),
            new DateTime(2027, 1, 1),
            new DateTime(2026, 5, 2)
        })));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(picker.SelectedDates, Is.EqualTo(new[] { new DateTime(2026, 3, 1), new DateTime(2026, 3, 2) }));
            Assert.That(events, Is.Zero);
        }));
    }

    [Test]
    public void PickerAccessibilityDefaultActionStringsAndToggleBehaviorAreExact()
    {
        using var form = CreatePickerHost(out var picker);
        var accessible = picker.AccessibilityObject;
        Assert.That(accessible.DefaultAction, Is.EqualTo("Open calendar"));

        accessible.DoDefaultAction();
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(accessible.DefaultAction, Is.EqualTo("Close calendar"));
            Assert.That(GetActiveCalendar(picker), Is.Not.Null);
        }));

        accessible.DoDefaultAction();
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(accessible.DefaultAction, Is.EqualTo("Open calendar"));
            Assert.That(GetActiveCalendar(picker), Is.Null);
        }));
    }

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
    public void PickerSelectedDateSynchronizesWhileOpenWithOnePickerEventAndNoCloseOrRecursion()
    {
        using var form = CreatePickerHost(out var picker);
        var selection = 0;
        var closed = 0;
        picker.SelectionChanged += (_, _) => selection++;
        picker.Closed += (_, _) => closed++;
        picker.ShowDropDown();
        Application.DoEvents();
        var active = GetActiveCalendar(picker)!;

        picker.SelectedDate = new DateTime(2026, 6, 15);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(active.SelectedDate, Is.EqualTo(new DateTime(2026, 6, 15)));
            Assert.That(selection, Is.EqualTo(1));
            Assert.That(closed, Is.Zero);
            Assert.That(GetActiveCalendar(picker), Is.SameAs(active));
        }));
    }

    [Test]
    public void PickerSetSelectedDatesSynchronizesWhileOpenWithOnePickerEventAndNoCloseOrRecursion()
    {
        using var form = CreatePickerHost(out var picker);
        picker.SelectionMode = BootstrapCalendarSelectionMode.Multiple;
        var selection = 0;
        var closed = 0;
        picker.SelectionChanged += (_, _) => selection++;
        picker.Closed += (_, _) => closed++;
        picker.ShowDropDown();
        Application.DoEvents();
        var active = GetActiveCalendar(picker)!;

        picker.SetSelectedDates(new[] { new DateTime(2026, 6, 16), new DateTime(2026, 6, 14) });
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(active.SelectedDates, Is.EqualTo(new[] { new DateTime(2026, 6, 14), new DateTime(2026, 6, 16) }));
            Assert.That(selection, Is.EqualTo(1));
            Assert.That(closed, Is.Zero);
            Assert.That(GetActiveCalendar(picker), Is.SameAs(active));
        }));
    }

    [Test]
    public void PickerClearSelectionSynchronizesWhileOpenWithOnePickerEventAndNoCloseOrRecursion()
    {
        using var form = CreatePickerHost(out var picker);
        picker.SelectedDate = new DateTime(2026, 6, 15);
        var selection = 0;
        var closed = 0;
        picker.SelectionChanged += (_, _) => selection++;
        picker.Closed += (_, _) => closed++;
        picker.ShowDropDown();
        Application.DoEvents();
        var active = GetActiveCalendar(picker)!;

        picker.ClearSelection();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(active.SelectedDate, Is.Null);
            Assert.That(selection, Is.EqualTo(1));
            Assert.That(closed, Is.Zero);
            Assert.That(GetActiveCalendar(picker), Is.SameAs(active));
        }));
    }

    [Test]
    public void PickerThemeSwitchPreservesPopupStateAndCallerFontOwnership()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var callerFont = new Font("Arial", 13f);
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using var form = CreatePickerHost(out var picker);
            picker.SelectionMode = BootstrapCalendarSelectionMode.Range;
            picker.SetRange(new DateTime(2026, 6, 10), null);
            picker.ShowDropDown();
            Application.DoEvents();
            var active = GetActiveCalendar(picker)!;
            var opened = 0;
            var closed = 0;
            picker.Opened += (_, _) => opened++;
            picker.Closed += (_, _) => closed++;

            var darkBase = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            BootstrapThemeManager.CurrentTheme = new BootstrapTheme(
                BootstrapThemeMode.Dark,
                darkBase.Colors,
                darkBase.Metrics,
                new BootstrapThemeTypography(
                    new BootstrapFontToken("Segoe UI", 11f, FontStyle.Bold),
                    darkBase.Typography.BodySmall,
                    darkBase.Typography.Label,
                    darkBase.Typography.HeadingSmall,
                    darkBase.Typography.HeadingMedium));
            Application.DoEvents();
            var native = GetNativeDropDown(GetPickerDropDown(picker));
            Assert.Multiple((Action)(() =>
            {
                Assert.DoesNotThrow((Action)(() => picker.Font.GetHeight()));
                Assert.That(picker.Font.SizeInPoints, Is.EqualTo(11f).Within(0.05f));
                Assert.That(picker.Font.Style, Is.EqualTo(FontStyle.Bold));
                Assert.DoesNotThrow((Action)(() => active.Font.GetHeight()));
                Assert.That(active.Font.SizeInPoints, Is.EqualTo(11f).Within(0.05f));
                Assert.DoesNotThrow((Action)(() => native.Font.GetHeight()));
                Assert.That(native.Font.SizeInPoints, Is.EqualTo(11f).Within(0.05f));
                Assert.That(GetActiveCalendar(picker), Is.SameAs(active));
                Assert.That(picker.RangeStart, Is.EqualTo(new DateTime(2026, 6, 10)));
                Assert.That(opened, Is.Zero);
                Assert.That(closed, Is.Zero);
            }));

            picker.Font = callerFont;
            BootstrapThemeManager.CurrentTheme = originalTheme;
            Application.DoEvents();
            Assert.Multiple((Action)(() =>
            {
                Assert.That(picker.Font, Is.SameAs(callerFont));
                Assert.That(native.Font, Is.SameAs(callerFont));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
            Assert.DoesNotThrow((Action)(() => callerFont.GetHeight()));
            callerFont.Dispose();
        }
    }

    [Test]
    public void PickerEffectiveDpiRefreshesHostedLayoutWithoutChangingLogicalOrPopupState()
    {
        var effectiveDpi = 96;
        using var picker = new BootstrapCalendarPicker(() => effectiveDpi, hostedCalendarSetupCompleted: null)
        {
            SelectionMode = BootstrapCalendarSelectionMode.Multiple
        };
        picker.SetSelectedDates(new[] { new DateTime(2026, 6, 14), new DateTime(2026, 6, 16) });
        using var form = CreatePickerHost(picker);
        var opened = 0;
        var closed = 0;
        var selection = 0;
        picker.Opened += (_, _) => opened++;
        picker.Closed += (_, _) => closed++;
        picker.SelectionChanged += (_, _) => selection++;
        picker.ShowDropDown();
        Application.DoEvents();
        var active = GetActiveCalendar(picker)!;
        var expected96 = BootstrapCalendarRenderLogic.CalculatePreferredSize(
            BootstrapCalendarRenderLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, 96, active.BorderRadius));
        Assert.That(active.Size, Is.EqualTo(expected96));

        effectiveDpi = 144;
        RaiseDpiChangedAfterParent(picker);
        Application.DoEvents();
        var expected144 = BootstrapCalendarRenderLogic.CalculatePreferredSize(
            BootstrapCalendarRenderLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, 144, active.BorderRadius));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(active.Size, Is.EqualTo(expected144));
            Assert.That(GetActiveCalendar(picker), Is.SameAs(active));
            Assert.That(picker.SelectedDates, Is.EqualTo(new[] { new DateTime(2026, 6, 14), new DateTime(2026, 6, 16) }));
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.Zero);
            Assert.That(selection, Is.Zero);
        }));
    }

    [Test]
    public void PickerRepeatedOpenSelectSyncCloseAndDisposeDoesNotMultiplyEventsOrRetainActiveState()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();
        var picker = new BootstrapCalendarPicker();
        using var form = CreatePickerHost(picker);
        var selection = 0;
        var opened = 0;
        var closed = 0;
        picker.SelectionChanged += (_, _) => selection++;
        picker.Opened += (_, _) => opened++;
        picker.Closed += (_, _) => closed++;

        for (var index = 0; index < 12; index++)
        {
            picker.ShowDropDown();
            Application.DoEvents();
            picker.SelectedDate = new DateTime(2026, 6, 1).AddDays(index);
            picker.ClearSelection();
            ActivateDate(GetActiveCalendar(picker)!, new DateTime(2026, 7, 1).AddDays(index));
            Application.DoEvents();
            Assert.That(GetActiveCalendar(picker), Is.Null);
        }

        picker.ShowDropDown();
        Application.DoEvents();
        var dropdown = GetPickerDropDown(picker);
        picker.Dispose();
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(selection, Is.EqualTo(36));
            Assert.That(opened, Is.EqualTo(13));
            Assert.That(closed, Is.EqualTo(12));
            Assert.That(GetActiveCalendar(picker), Is.Null);
            Assert.That(GetNativeDropDown(dropdown).Visible, Is.False);
            Assert.That(GetPrivateField(dropdown, "_activePresentationSource"), Is.Null);
            Assert.That(GetPrivateField(dropdown, "_activeIconRenderer"), Is.Null);
            Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
        }));
    }

    [Test]
    public void PickerRepeatedCreateOpenProgrammaticSyncSelectCloseAndDisposeReleasesEveryHostedReference()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();

        for (var index = 0; index < 12; index++)
        {
            using var picker = new BootstrapCalendarPicker
            {
                SelectionMode = BootstrapCalendarSelectionMode.Multiple
            };
            using var form = CreatePickerHost(picker);
            var selection = 0;
            var opened = 0;
            var closed = 0;
            picker.SelectionChanged += (_, _) => selection++;
            picker.Opened += (_, _) => opened++;
            picker.Closed += (_, _) => closed++;

            picker.ShowDropDown();
            Application.DoEvents();
            var active = GetActiveCalendar(picker);
            var dropdown = GetPickerDropDown(picker);
            var nativeDropDown = GetNativeDropDown(dropdown);
            Assert.That(active, Is.Not.Null, "An effective open must create the picker-owned active calendar.");
            if (active is null) throw new AssertionException("An effective open did not create the picker-owned active calendar.");

            Assert.That(nativeDropDown.Visible, Is.True, "An effective open must make the native dropdown visible.");
            var host = GetHostedCalendarHost(nativeDropDown);
            Assert.Multiple((Action)(() =>
            {
                Assert.That(active.IsDisposed, Is.False);
                Assert.That(host.IsDisposed, Is.False);
                Assert.That(host.Control, Is.SameAs(active));
            }));

            var programmaticDate = new DateTime(2026, 8, 1).AddDays(index);
            picker.SetSelectedDates(new[] { programmaticDate });
            Application.DoEvents();
            ActivateDate(active, programmaticDate.AddDays(12));
            Application.DoEvents();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(selection, Is.EqualTo(2), "Each programmatic and user selection must raise exactly one event.");
                Assert.That(opened, Is.EqualTo(1));
                Assert.That(closed, Is.Zero, "Multiple selection must remain open until explicitly closed.");
                Assert.That(GetActiveCalendar(picker), Is.SameAs(active));
            }));

            picker.CloseDropDown();
            Application.DoEvents();
            Assert.Multiple((Action)(() =>
            {
                Assert.That(closed, Is.EqualTo(1));
                Assert.That(GetActiveCalendar(picker), Is.Null, "Closed must clear the active calendar reference.");
                Assert.That(nativeDropDown.Visible, Is.False);
                Assert.That(nativeDropDown.Items.Count, Is.EqualTo(1), "Closing retains the dropdown-owned snapshot until rebuild or disposal.");
                Assert.That(host.IsDisposed, Is.False, "Closing alone must not dispose the dropdown-owned host.");
                Assert.That(active.IsDisposed, Is.False, "Closing alone must not dispose the dropdown-owned hosted calendar.");
                Assert.That(host.Control, Is.SameAs(active));
                Assert.That(GetPrivateField(dropdown, "_activePresentationSource"), Is.Null);
                Assert.That(GetPrivateField(dropdown, "_activeIconRenderer"), Is.Null);
            }));

            Assert.DoesNotThrow((Action)(() => picker.Dispose()), "Closing a hosted calendar must not leave disposal callbacks that throw.");
            Assert.DoesNotThrow((Action)Application.DoEvents, "Disposal must leave no delayed hosted-calendar callbacks that throw.");
            Assert.Multiple((Action)(() =>
            {
                Assert.That(GetActiveCalendar(picker), Is.Null);
                Assert.That(nativeDropDown.IsDisposed, Is.True);
                Assert.That(nativeDropDown.Items, Is.Empty);
                Assert.That(host.Owner, Is.Null, "Disposal must detach the native host from the disposed dropdown.");
                Assert.That(active.IsDisposed, Is.True);
                Assert.That(selection, Is.EqualTo(2), "Disposal must not deliver delayed selection callbacks.");
                Assert.That(opened, Is.EqualTo(1), "Disposal must not deliver delayed opening callbacks.");
                Assert.That(closed, Is.EqualTo(1), "Disposal after an explicit close must not deliver a duplicate close callback.");
            }));
        }

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
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
    public void PickerFirstMonthSeedsFromRangeStart()
    {
        using var form = CreatePickerHost(out var picker);
        picker.SelectionMode = BootstrapCalendarSelectionMode.Range;
        picker.SetRange(new DateTime(2026, 4, 18), null);
        picker.ShowDropDown();
        Application.DoEvents();
        Assert.That(GetActiveCalendar(picker)!.DisplayMonth, Is.EqualTo(new DateTime(2026, 4, 1)));
    }

    [Test]
    public void PickerFirstMonthSeedsFromSortedFirstMultipleDate()
    {
        using var form = CreatePickerHost(out var picker);
        picker.SelectionMode = BootstrapCalendarSelectionMode.Multiple;
        picker.SetSelectedDates(new[] { new DateTime(2026, 7, 20), new DateTime(2026, 5, 2) });
        picker.ShowDropDown();
        Application.DoEvents();
        Assert.That(GetActiveCalendar(picker)!.DisplayMonth, Is.EqualTo(new DateTime(2026, 5, 1)));
    }

    [Test]
    public void PickerFirstMonthSeedsFromTodayWhenSelectionIsEmpty()
    {
        using var form = CreatePickerHost(out var picker);
        picker.ShowDropDown();
        Application.DoEvents();
        Assert.That(GetActiveCalendar(picker)!.DisplayMonth, Is.EqualTo(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)));
    }

    [Test]
    public void PickerFirstMonthClampsTodaySeedToInitialBounds()
    {
        using var form = CreatePickerHost(out var picker);
        var lowerBound = DateTime.Today.AddYears(2).Date;
        picker.MinDate = lowerBound;
        picker.MaxDate = lowerBound.AddMonths(3);
        picker.ShowDropDown();
        Application.DoEvents();
        Assert.That(GetActiveCalendar(picker)!.DisplayMonth, Is.EqualTo(new DateTime(lowerBound.Year, lowerBound.Month, 1)));
    }

    [Test]
    public void PickerRetainedMonthClampsAfterBoundsChangeBeforeNextOpen()
    {
        using var form = CreatePickerHost(out var picker);
        picker.MinDate = new DateTime(2026, 1, 1);
        picker.MaxDate = new DateTime(2026, 12, 31);
        picker.ShowDropDown();
        Application.DoEvents();
        GetActiveCalendar(picker)!.DisplayMonth = new DateTime(2026, 11, 1);
        picker.CloseDropDown();
        Application.DoEvents();

        picker.MaxDate = new DateTime(2026, 8, 20);
        picker.ShowDropDown();
        Application.DoEvents();

        Assert.That(GetActiveCalendar(picker)!.DisplayMonth, Is.EqualTo(new DateTime(2026, 8, 1)));
    }

    [Test]
    public void PickerLocalCalendarFailureAfterSubscriptionDisposesUnsubscribesAndNeverOpens()
    {
        BootstrapCalendar? local = null;
        BootstrapCalendarPicker? picker = null;
        picker = new BootstrapCalendarPicker(
            effectiveDpiProvider: null,
            hostedCalendarSetupCompleted: calendar =>
            {
                local = calendar;
                Assert.That(HasHandlerTarget(calendar, "SelectionActivated", picker), Is.True);
                Assert.That(HasHandlerTarget(calendar, "DisplayMonthChanged", picker), Is.True);
                throw new InvalidOperationException("after-subscription");
            });
        using (picker)
        {

            Assert.Throws<InvalidOperationException>((Action)(() => picker.ShowDropDown()));
            Assert.Multiple((Action)(() =>
            {
                Assert.That(local, Is.Not.Null);
                Assert.That(local!.IsDisposed, Is.True);
                Assert.That(HasHandlerTarget(local, "SelectionActivated", picker), Is.False);
                Assert.That(HasHandlerTarget(local, "DisplayMonthChanged", picker), Is.False);
                Assert.That(GetActiveCalendar(picker), Is.Null);
                Assert.That(GetNativeDropDown(GetPickerDropDown(picker)).Visible, Is.False);
                Assert.That(picker.AccessibilityObject.State & AccessibleStates.Collapsed, Is.Not.Zero);
            }));
        }
    }

    [Test]
    public void PickerNativeShowFailureAfterCalendarPublicationRollsBackPickerAndDropdownOwnership()
    {
        BootstrapCalendar? publishedCalendar = null;
        ToolStripControlHost? publishedNativeHost = null;
        BootstrapCalendarPicker? picker = null;
        BootstrapDropdown? dropdown = null;
        var nativeShowCalls = 0;
        var nativeHostDisposed = 0;
        picker = new BootstrapCalendarPicker(
            effectiveDpiProvider: null,
            hostedCalendarSetupCompleted: null,
            showNativeDropDown: (native, _, _) =>
            {
                nativeShowCalls++;
                publishedCalendar = GetActiveCalendar(picker!);
                Assert.That(publishedCalendar, Is.Not.Null);
                Assert.That(HasHandlerTarget(publishedCalendar!, "SelectionActivated", picker), Is.True);
                Assert.That(HasHandlerTarget(publishedCalendar!, "DisplayMonthChanged", picker), Is.True);
                Assert.That(native.Items, Has.Count.EqualTo(1));
                publishedNativeHost = native.Items[0] as ToolStripControlHost;
                Assert.That(publishedNativeHost, Is.Not.Null);
                Assert.That(publishedNativeHost!.Control, Is.SameAs(publishedCalendar));
                publishedNativeHost.Disposed += (_, _) => nativeHostDisposed++;
                Assert.That(GetPrivateField(dropdown!, "_activePresentationSource"), Is.SameAs(picker));
                Assert.That(GetPrivateField(dropdown!, "_activeIconRenderer"), Is.Not.Null);
                throw new InvalidOperationException("native-show");
            });
        using (picker)
        using (var target = new BootstrapButton())
        {
            dropdown = GetPickerDropDown(picker);
            dropdown.Target = target;
            var opened = 0;
            var closed = 0;
            picker.Opened += (_, _) => opened++;
            picker.Closed += (_, _) => closed++;

            var exception = Assert.Throws<InvalidOperationException>((Action)(() => picker.ShowDropDown()));

            Assert.Multiple((Action)(() =>
            {
                Assert.That(exception!.Message, Is.EqualTo("native-show"));
                Assert.That(nativeShowCalls, Is.EqualTo(1));
                Assert.That(publishedCalendar, Is.Not.Null);
                Assert.That(publishedCalendar!.IsDisposed, Is.True);
                Assert.That(HasHandlerTarget(publishedCalendar, "SelectionActivated", picker), Is.False);
                Assert.That(HasHandlerTarget(publishedCalendar, "DisplayMonthChanged", picker), Is.False);
                Assert.That(GetActiveCalendar(picker), Is.Null);
                Assert.That(publishedNativeHost, Is.Not.Null);
                Assert.That(nativeHostDisposed, Is.EqualTo(1));
                Assert.That(GetNativeDropDown(dropdown).Items, Is.Empty);
                Assert.That(GetNativeDropDown(dropdown).Visible, Is.False);
                Assert.That(GetPrivateField(dropdown, "_activePresentationSource"), Is.Null);
                Assert.That(GetPrivateField(dropdown, "_activeIconRenderer"), Is.Null);
                Assert.That(dropdown.Target, Is.SameAs(target));
                Assert.That(target.IsDisposed, Is.False);
                Assert.That(picker.AccessibilityObject.State & AccessibleStates.Collapsed, Is.Not.Zero);
                Assert.That(picker.AccessibilityObject.State & AccessibleStates.Expanded, Is.EqualTo(AccessibleStates.None));
                Assert.That(opened, Is.Zero);
                Assert.That(closed, Is.Zero);
            }));
        }
    }

    [Test]
    public void PickerChangedSingleActivationRaisesExactSelectionOpenCloseCounts()
    {
        using var form = CreatePickerHost(out var picker);
        var selection = 0;
        var opened = 0;
        var closed = 0;
        picker.SelectionChanged += (_, _) => selection++;
        picker.Opened += (_, _) => opened++;
        picker.Closed += (_, _) => closed++;

        picker.ShowDropDown();
        Application.DoEvents();
        ActivateDate(GetActiveCalendar(picker)!, new DateTime(2026, 8, 30));
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(picker.SelectedDate, Is.EqualTo(new DateTime(2026, 8, 30)));
            Assert.That(selection, Is.EqualTo(1));
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(GetActiveCalendar(picker), Is.Null);
        }));
    }

    [Test]
    public void PickerSingleCompletionClosesUsingOriginatingModeWhenSelectionHandlerChangesMode()
    {
        using var form = CreatePickerHost(out var picker);
        var closed = 0;
        picker.SelectionChanged += (_, _) => picker.SelectionMode = BootstrapCalendarSelectionMode.Multiple;
        picker.Closed += (_, _) => closed++;

        picker.ShowDropDown();
        Application.DoEvents();
        ActivateDate(GetActiveCalendar(picker)!, new DateTime(2026, 8, 30));
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(picker.SelectionMode, Is.EqualTo(BootstrapCalendarSelectionMode.Multiple));
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(GetActiveCalendar(picker), Is.Null);
        }));
    }

    [Test]
    public void PickerRangeActivationRaisesTwoChangesOneOpenOneCompletionClose()
    {
        using var form = CreatePickerHost(out var picker);
        picker.SelectionMode = BootstrapCalendarSelectionMode.Range;
        var selection = 0;
        var opened = 0;
        var closed = 0;
        picker.SelectionChanged += (_, _) => selection++;
        picker.Opened += (_, _) => opened++;
        picker.Closed += (_, _) => closed++;

        picker.ShowDropDown();
        Application.DoEvents();
        ActivateDate(GetActiveCalendar(picker)!, new DateTime(2026, 8, 30));
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(selection, Is.EqualTo(1));
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.Zero);
            Assert.That(GetActiveCalendar(picker), Is.Not.Null);
        }));

        ActivateDate(GetActiveCalendar(picker)!, new DateTime(2026, 8, 31));
        Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(selection, Is.EqualTo(2));
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(GetActiveCalendar(picker), Is.Null);
        }));
    }

    [Test]
    public void PickerCompletedRangeClosesUsingOriginatingModeWhenSelectionHandlerChangesMode()
    {
        using var form = CreatePickerHost(out var picker);
        picker.SelectionMode = BootstrapCalendarSelectionMode.Range;
        picker.SetRange(new DateTime(2026, 8, 30), null);
        var closed = 0;
        picker.SelectionChanged += (_, _) => picker.SelectionMode = BootstrapCalendarSelectionMode.Multiple;
        picker.Closed += (_, _) => closed++;

        picker.ShowDropDown();
        Application.DoEvents();
        ActivateDate(GetActiveCalendar(picker)!, new DateTime(2026, 8, 31));
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(picker.SelectionMode, Is.EqualTo(BootstrapCalendarSelectionMode.Multiple));
            Assert.That(closed, Is.EqualTo(1));
            Assert.That(GetActiveCalendar(picker), Is.Null);
        }));
    }

    [Test]
    public void PickerCompletedAdjacentMonthRangeRetainsActivatedMonthForNextOpen()
    {
        using var form = CreatePickerHost(out var picker);
        picker.SelectionMode = BootstrapCalendarSelectionMode.Range;
        picker.SetRange(new DateTime(2026, 8, 30), null);

        picker.ShowDropDown();
        Application.DoEvents();
        MouseDownDate(GetActiveCalendar(picker)!, new DateTime(2026, 9, 1));
        Application.DoEvents();
        Assert.That(GetActiveCalendar(picker), Is.Null);

        picker.ShowDropDown();
        Application.DoEvents();

        Assert.That(GetActiveCalendar(picker)!.DisplayMonth, Is.EqualTo(new DateTime(2026, 9, 1)));
    }

    [Test]
    public void PickerMultipleActivationRaisesExactChangesWithoutCompletionClose()
    {
        using var form = CreatePickerHost(out var picker);
        picker.SelectionMode = BootstrapCalendarSelectionMode.Multiple;
        var selection = 0;
        var opened = 0;
        var closed = 0;
        picker.SelectionChanged += (_, _) => selection++;
        picker.Opened += (_, _) => opened++;
        picker.Closed += (_, _) => closed++;

        picker.ShowDropDown();
        Application.DoEvents();
        ActivateDate(GetActiveCalendar(picker)!, new DateTime(2026, 8, 30));
        ActivateDate(GetActiveCalendar(picker)!, new DateTime(2026, 8, 31));
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(picker.SelectedDates, Is.EqualTo(new[] { new DateTime(2026, 8, 30), new DateTime(2026, 8, 31) }));
            Assert.That(selection, Is.EqualTo(2));
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.Zero);
            Assert.That(GetActiveCalendar(picker), Is.Not.Null);
        }));
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

    [TestCase(MouseButtons.Right)]
    [TestCase(MouseButtons.Middle)]
    public void PickerNonLeftMouseClickDoesNotToggle(MouseButtons button)
    {
        using var form = CreatePickerHost(out var picker);

        RaiseMouseClick(picker, button);
        Application.DoEvents();

        Assert.That(GetActiveCalendar(picker), Is.Null);
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

    private static Form CreatePickerHost(BootstrapCalendarPicker picker)
    {
        var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000), Size = new Size(480, 300) };
        picker.Location = new Point(24, 24);
        picker.Size = new Size(240, 36);
        form.Controls.Add(picker);
        form.Show();
        Application.DoEvents();
        return form;
    }

    private static object? GetDefaultValue(Type type, string propertyName) =>
        type.GetProperty(propertyName)!.GetCustomAttribute<DefaultValueAttribute>()!.Value;

    private static bool HasHandlerTarget(object instance, string eventName, object? target)
    {
        var field = instance.GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        var handlers = ((Delegate?)field!.GetValue(instance))?.GetInvocationList() ?? Array.Empty<Delegate>();
        return handlers.Any(handler => ReferenceEquals(handler.Target, target));
    }

    private static void RaiseDpiChangedAfterParent(BootstrapCalendarPicker picker)
    {
        var method = typeof(BootstrapCalendarPicker).GetMethod("OnDpiChangedAfterParent", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(picker, new object[] { EventArgs.Empty });
    }

    private static int GetThemeSubscriptionCount()
    {
        var eventField = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(eventField, Is.Not.Null);
        return ((Delegate?)eventField!.GetValue(null))?.GetInvocationList().Length ?? 0;
    }

    private static object? GetPrivateField(object instance, string name)
    {
        var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return field!.GetValue(instance);
    }

    private static void ActivateDate(BootstrapCalendar calendar, DateTime date)
    {
        var method = typeof(BootstrapCalendar).GetMethod("ActivateDate", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null); method!.Invoke(calendar, new object[] { date });
    }

    private static void MouseDownDate(BootstrapCalendar calendar, DateTime date)
    {
        var cell = calendar.ResolveLayout(calendar.DeviceDpi).DayCells.Single(day => day.Date == date.Date);
        var method = typeof(BootstrapCalendar).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(calendar, new object[]
        {
            new MouseEventArgs(MouseButtons.Left, 1, cell.Bounds.Left + cell.Bounds.Width / 2, cell.Bounds.Top + cell.Bounds.Height / 2, 0)
        });
    }

    private static void RaiseMouseClick(BootstrapCalendarPicker picker, MouseButtons button)
    {
        var method = typeof(BootstrapCalendarPicker).GetMethod("OnMouseClick", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(picker, new object[] { new MouseEventArgs(button, 1, picker.Width / 2, picker.Height / 2, 0) });
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

    private static ToolStripControlHost GetHostedCalendarHost(ToolStripDropDownMenu nativeDropDown)
    {
        Assert.That(nativeDropDown.Items.Count, Is.EqualTo(1), "The picker popup must contain exactly one hosted calendar row.");
        var host = nativeDropDown.Items[0] as ToolStripControlHost;
        Assert.That(host, Is.Not.Null, "The picker popup must use a native ToolStripControlHost.");
        if (host is null) throw new AssertionException("The picker popup did not create a ToolStripControlHost.");
        return host;
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
