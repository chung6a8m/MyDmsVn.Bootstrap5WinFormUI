using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapDateInputInteractionTests
{
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
    private const int WmChar = 0x0102;
    private const int WmKeyDown = 0x0100;
    private const int WmImeStartComposition = 0x010D;
    private const int WmImeEndComposition = 0x010E;

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void Native_typing_retains_raw_caret_focus_until_one_enter_commit(BootstrapThemeMode mode)
    {
        var prior = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            using var form = Host(out var input);
            var accepted = 0;
            input.ValueChanged += (_, _) => accepted++;
            input.NativeEditor.SelectAll();
            foreach (var ch in "5/11")
            {
                SendMessage(input.NativeEditor.Handle, WmChar, (IntPtr)ch, IntPtr.Zero);
                Assert.That(input.PendingText, Is.EqualTo(input.NativeEditor.Text));
                Assert.That(input.NativeEditor.Focused, Is.True);
                Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 23)));
            }
            Assert.That(input.PendingText, Is.EqualTo("5/11"));
            Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(4));
            SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Enter, IntPtr.Zero);
            Assert.That(input.PendingText, Is.EqualTo("05/11/2026"));
            Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 11, 5)));
            Assert.That(accepted, Is.EqualTo(1));
        }
        finally { BootstrapThemeManager.CurrentTheme = prior; }
    }

    [Test]
    public void Popup_escape_restores_raw_candidate_and_same_date_activation_is_explicit()
    {
        using var form = Host(out var input);
        input.Text = "5/11";
        input.ShowCalendar();
        Application.DoEvents();
        input.Calendar.CloseDropDown();
        Application.DoEvents();
        Assert.That(input.PendingText, Is.EqualTo("5/11"));
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 23)));

        input.ProjectAcceptedValue(new DateTime(2026, 10, 23));
        input.ShowCalendar();
        Application.DoEvents();
        var accepted = 0;
        input.Calendar.SelectionAccepted += (_, _) => accepted++;
        var field = typeof(BootstrapCalendarPicker).GetField("_activeCalendar", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var calendar = (BootstrapCalendar)field.GetValue(input.Calendar)!;
        var activate = typeof(BootstrapCalendar).GetMethod("ActivateDate", BindingFlags.Instance | BindingFlags.NonPublic)!;
        activate.Invoke(calendar, new object[] { new DateTime(2026, 10, 23) });
        Application.DoEvents();
        Assert.That(accepted, Is.EqualTo(1));
        Assert.That(input.PendingText, Is.EqualTo("23/10/2026"));
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 23)));
    }

    [Test]
    public void Child_calendar_open_route_captures_and_restores_raw_candidate()
    {
        using var form = Host(out var input);
        input.Text = "5/11";
        input.Calendar.ShowDropDown();
        Application.DoEvents();
        Assert.That(input.Calendar.SelectedDate, Is.EqualTo(new DateTime(2026, 11, 5)));
        input.Calendar.SelectedDate = new DateTime(2026, 11, 6);
        Assert.That(input.PendingText, Is.EqualTo("06/11/2026"));
        input.Calendar.CloseDropDown();
        Application.DoEvents();
        Assert.That(input.PendingText, Is.EqualTo("5/11"));
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 23)));
    }

    [Test]
    public void Reused_calendar_accepts_disjoint_profile_bounds_in_both_directions()
    {
        using var form = Host(out var input);
        input.Profile = new DateInputProfile(false, new DateTime(2027, 1, 1), new DateTime(2027, 1, 31));
        input.BeginEditSession(new DateTime(2027, 1, 5));
        input.ShowCalendar();
        Application.DoEvents();
        Assert.That(input.Calendar.MinDate, Is.EqualTo(new DateTime(2027, 1, 1)));
        input.Calendar.CloseDropDown();
        input.Profile = new DateInputProfile(false, new DateTime(1900, 1, 1), new DateTime(1900, 1, 31));
        input.BeginEditSession(new DateTime(1900, 1, 5));
        input.ShowCalendar();
        Application.DoEvents();
        Assert.That(input.Calendar.MaxDate, Is.EqualTo(new DateTime(1900, 1, 31)));
        input.Calendar.CloseDropDown();
    }

    [Test]
    public void Segment_arrows_preserve_invalid_candidate_and_refuse_impossible_month()
    {
        using var form = Host(out var input);
        input.Text = "31/1/2026";
        input.NativeEditor.SelectionStart = 4;
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("31/1/2026"));
        input.Text = "31/12/2026";
        input.NativeEditor.SelectionStart = 1;
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("01/01/2027"));
    }

    [Test]
    public void Ime_composition_defers_enter_acceptance_until_composition_ends()
    {
        using var form = Host(out var input);
        input.Text = "5/11";
        SendMessage(input.NativeEditor.Handle, WmImeStartComposition, IntPtr.Zero, IntPtr.Zero);
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Enter, IntPtr.Zero);
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 23)));
        Assert.That(input.PendingText, Is.EqualTo("5/11"));
        SendMessage(input.NativeEditor.Handle, WmImeEndComposition, IntPtr.Zero, IntPtr.Zero);
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Enter, IntPtr.Zero);
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 11, 5)));
    }

    private static Form Host(out BootstrapDateInput input)
    {
        var form = new Form { Width = 420, Height = 150, ShowInTaskbar = false };
        input = new BootstrapDateInput
        {
            Profile = new DateInputProfile(false, new DateTime(1900, 1, 1), new DateTime(2100, 12, 31)),
            ReferenceDateProvider = () => new DateTime(2026, 10, 23),
            Dock = DockStyle.Top
        };
        form.Controls.Add(input);
        form.Show();
        form.Activate();
        input.BeginEditSession(new DateTime(2026, 10, 23));
        input.NativeEditor.Focus();
        Application.DoEvents();
        return form;
    }
}
