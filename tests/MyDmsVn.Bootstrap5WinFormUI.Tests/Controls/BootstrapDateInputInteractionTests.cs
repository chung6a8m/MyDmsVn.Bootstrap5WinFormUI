using System;
using System.Collections.Generic;
using System.Drawing;
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
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PostMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
    private const int WmChar = 0x0102;
    private const int WmKeyDown = 0x0100;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmImeStartComposition = 0x010D;
    private const int WmImeEndComposition = 0x010E;

    [TestCase(1, 0, false)]
    [TestCase(1, 0, true)]
    [TestCase(0, 3, false)]
    [TestCase(0, 3, true)]
    public void DateInputCorrection_Canceled_popup_restores_native_range_before_notification(int start, int length, bool pointer)
    {
        using var form = Host(out var input);
        input.Text = "5/11";
        input.NativeEditor.Select(start, length);
        var restoredInNotification = false;
        input.PendingTextChanged += (_, _) =>
        {
            if (input.PendingText == "5/11")
                restoredInNotification = input.NativeEditor.SelectionStart == start && input.NativeEditor.SelectionLength == length;
        };

        if (pointer)
        {
            SendMessage(input.Calendar.Handle, WmLButtonDown, (IntPtr)1, IntPtr.Zero);
            SendMessage(input.Calendar.Handle, WmLButtonUp, IntPtr.Zero, IntPtr.Zero);
        }
        else SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.F4, IntPtr.Zero);
        Application.DoEvents();
        input.Calendar.SelectedDate = new DateTime(2026, 11, 6);
        Assert.That(input.PendingText, Is.EqualTo("06/11/2026"));
        input.Calendar.CloseDropDown();
        Application.DoEvents();

        Assert.That(input.PendingText, Is.EqualTo("5/11"));
        Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(start));
        Assert.That(input.NativeEditor.SelectionLength, Is.EqualTo(length));
        Assert.That(restoredInNotification, Is.True);
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 23)));
    }

    [Test]
    public void DateInputCorrection_Pointer_calendar_cancel_returns_focus_to_native_editor()
    {
        using var form = Host(out var input);
        input.Text = "5/11";
        SendMessage(input.Calendar.Handle, WmLButtonDown, (IntPtr)1, IntPtr.Zero);
        SendMessage(input.Calendar.Handle, WmLButtonUp, IntPtr.Zero, IntPtr.Zero);
        Application.DoEvents();
        Assert.That(input.Calendar.SelectedDate, Is.EqualTo(new DateTime(2026, 11, 5)));
        input.Calendar.CloseDropDown();
        Application.DoEvents();
        Assert.That(input.NativeEditor.Focused, Is.True);
        Assert.That(input.PendingText, Is.EqualTo("5/11"));
    }

    [Test]
    public void DateInputCorrection_External_dismissal_does_not_steal_new_focus()
    {
        using var form = Host(out var input);
        using var next = new Button { Text = "Next", Dock = DockStyle.Bottom };
        form.Controls.Add(next);
        input.Text = "5/11";
        SendMessage(input.Calendar.Handle, WmLButtonDown, (IntPtr)1, IntPtr.Zero);
        SendMessage(input.Calendar.Handle, WmLButtonUp, IntPtr.Zero, IntPtr.Zero);
        Application.DoEvents();
        Assert.That(input.Calendar.SelectedDate, Is.EqualTo(new DateTime(2026, 11, 5)));

        next.Focus();
        Application.DoEvents();
        input.Calendar.CloseDropDown();
        Application.DoEvents();
        Assert.That(next.Focused, Is.True);
        Assert.That(input.PendingText, Is.EqualTo("5/11"));
    }

    [Test]
    public void DateInputCorrection_Embedded_trigger_never_renders_date_summary()
    {
        using var form = Host(out var input);
        input.Calendar.SelectedDate = new DateTime(2026, 10, 12);
        using var first = Capture(input.Calendar);
        Assert.That(BitmapHasInk(first, input.Calendar.BackColor), Is.True, "The trigger must paint a visible calendar icon.");
        input.Calendar.SelectedDate = new DateTime(2026, 10, 22);
        using var second = Capture(input.Calendar);

        Assert.That(second.Size, Is.EqualTo(first.Size));
        for (var y = 0; y < first.Height; y++)
            for (var x = 0; x < first.Width; x++)
                Assert.That(second.GetPixel(x, y), Is.EqualTo(first.GetPixel(x, y)),
                    $"The embedded trigger painted calendar summary pixels at ({x}, {y}).");

        using var standalone = new BootstrapCalendarPicker { Size = new Size(240, input.Height), SelectedDate = new DateTime(2026, 10, 12) };
        using var standaloneFirst = Capture(standalone);
        standalone.SelectedDate = new DateTime(2026, 10, 22);
        using var standaloneSecond = Capture(standalone);
        Assert.That(BitmapsEqual(standaloneFirst, standaloneSecond), Is.False,
            "The standalone picker must keep painting its date summary.");
    }

    [Test]
    public void DateInputCorrection_Unified_surface_has_one_border_and_compact_hit_area()
    {
        using var form = Host(out var input);
        Assert.That(input.Controls.Count, Is.EqualTo(1), "The outer DateInput must contain one painted editor surface.");
        Assert.That(input.Calendar.Parent, Is.SameAs(input.Controls[0]), "The calendar action belongs inside the editor surface.");
        var scale = input.DeviceDpi / 96d;
        Assert.That(input.Calendar.Width / scale, Is.InRange(28d, 32d));
        Assert.That(input.Calendar.Height / scale, Is.InRange(28d, 32d));

        using var neutral = Capture(input.Controls[0]);
        input.ValidationState = BootstrapValidationState.Invalid;
        using var invalid = Capture(input.Controls[0]);
        Assert.That(invalid.GetPixel(0, invalid.Height / 2),
            Is.Not.EqualTo(neutral.GetPixel(0, neutral.Height / 2)),
            "Validation must tint the one exterior editor border.");

        var before = input.Height;
        var preferredBefore = input.GetPreferredSize(Size.Empty).Height;
        input.Dock = DockStyle.Fill;
        form.PerformLayout();
        input.Dock = DockStyle.Top;
        form.PerformLayout();
        Assert.That(input.Height, Is.EqualTo(before), "Docking must not inflate the date field height.");
        Assert.That(input.GetPreferredSize(Size.Empty).Height, Is.EqualTo(preferredBefore),
            "Preferred height must be stable before and after docking.");
    }

    [TestCase(BootstrapThemeMode.Light)]
    [TestCase(BootstrapThemeMode.Dark)]
    public void DateInputCorrection_Larger_font_keeps_native_date_and_trigger_readable(BootstrapThemeMode mode)
    {
        var previousTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            using var largeFont = new Font(SystemFonts.DefaultFont.FontFamily, SystemFonts.DefaultFont.SizeInPoints + 12f);
            using var form = Host(out var input);
            input.Font = largeFont;
            form.PerformLayout();

            Assert.That(input.NativeEditor.Font.SizeInPoints, Is.GreaterThanOrEqualTo(largeFont.SizeInPoints - 0.1f));
            Assert.That(input.Height, Is.GreaterThanOrEqualTo(input.NativeEditor.PreferredHeight + 2));
            Assert.That(input.NativeEditor.Bounds.Height, Is.GreaterThanOrEqualTo(input.NativeEditor.PreferredHeight));
            Assert.That(input.GetPreferredSize(Size.Empty).Height,
                Is.GreaterThanOrEqualTo(input.NativeEditor.PreferredHeight + 2));
            Assert.That(input.Calendar.Bottom, Is.LessThan(input.Height));
            Assert.That(input.Calendar.Top, Is.GreaterThan(0));
            Assert.That(input.PendingText, Is.EqualTo("23/10/2026"));

            input.Text = "5/11";
            input.NativeEditor.SelectionStart = 2;
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode == BootstrapThemeMode.Light ? BootstrapThemeMode.Dark : BootstrapThemeMode.Light);
            Application.DoEvents();
            Assert.That(input.PendingText, Is.EqualTo("5/11"));
            Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(2));
            Assert.That(input.Height, Is.GreaterThanOrEqualTo(input.NativeEditor.PreferredHeight + 2));

            input.Font = SystemFonts.DefaultFont;
            form.PerformLayout();
            Assert.That(input.Height, Is.EqualTo(32 * input.DeviceDpi / 96),
                "Returning to the normal font must release the height expanded for large text.");
        }
        finally { BootstrapThemeManager.CurrentTheme = previousTheme; }
    }

    [Test]
    public void DateInputCorrection_Default_theme_font_remains_theme_owned()
    {
        var previousTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using var form = Host(out var input);
            var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            BootstrapThemeManager.CurrentTheme = new BootstrapTheme(
                BootstrapThemeMode.Dark, dark.Colors, dark.Metrics,
                new BootstrapThemeTypography(
                    new BootstrapFontToken("Segoe UI", 12f),
                    dark.Typography.BodySmall,
                    dark.Typography.Label,
                    dark.Typography.HeadingSmall,
                    dark.Typography.HeadingMedium));
            Application.DoEvents();
            Assert.That(input.NativeEditor.Font.SizeInPoints, Is.EqualTo(12f).Within(0.05f));
            Assert.That(input.NativeEditor.Bounds.Height, Is.GreaterThanOrEqualTo(input.NativeEditor.PreferredHeight));
            Assert.That(input.PendingText, Is.EqualTo("23/10/2026"));
        }
        finally { BootstrapThemeManager.CurrentTheme = previousTheme; }
    }

    private static Bitmap Capture(Control control)
    {
        var bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
        return bitmap;
    }

    private static bool BitmapsEqual(Bitmap left, Bitmap right)
    {
        if (left.Size != right.Size) return false;
        for (var y = 0; y < left.Height; y++)
            for (var x = 0; x < left.Width; x++)
                if (left.GetPixel(x, y) != right.GetPixel(x, y)) return false;
        return true;
    }

    private static bool BitmapHasInk(Bitmap bitmap, Color background)
    {
        for (var y = 0; y < bitmap.Height; y++)
            for (var x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y) != background) return true;
        return false;
    }

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
        input.NativeEditor.Focus();
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Escape, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("23/10/2026"));

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
    public void Popup_after_local_midnight_uses_the_edit_sessions_original_reference_date()
    {
        using var form = Host(out var input);
        var now = new DateTime(2026, 10, 23, 23, 59, 59);
        input.ReferenceDateProvider = () => now;
        input.BeginEditSession(new DateTime(2026, 10, 23));
        input.Text = "5";
        now = new DateTime(2026, 11, 1);
        input.ShowCalendar();
        Application.DoEvents();
        Assert.That(input.Calendar.SelectedDate, Is.EqualTo(new DateTime(2026, 10, 5)));
        input.Calendar.CloseDropDown();
        Assert.That(input.TryParsePending().Value, Is.EqualTo(new DateTime(2026, 10, 5)));
        Assert.That(input.PendingText, Is.EqualTo("5"));
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
    public void New_candidate_started_by_arrow_captures_current_year_and_keeps_it_while_pending()
    {
        using var form = Host(out var input);
        var now = new DateTime(2026, 12, 31);
        input.ReferenceDateProvider = () => now;
        input.BeginEditSession(new DateTime(2026, 12, 31));
        now = new DateTime(2027, 1, 1);
        input.NativeEditor.SelectionStart = 1;
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("01/01/2027"));

        now = new DateTime(2027, 2, 1);
        input.Text = "5";
        Assert.That(input.TryParsePending().Value, Is.EqualTo(new DateTime(2027, 1, 5)));
        Assert.That(input.TryCommitPending(), Is.True);
        Assert.That(input.Value, Is.EqualTo(new DateTime(2027, 1, 5)));
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

    [Test]
    public void Native_tab_keeps_invalid_candidate_and_commits_valid_candidate_before_focus_moves()
    {
        using var form = Host(out var input);
        var next = new Button { Text = "Next", Dock = DockStyle.Bottom, TabIndex = 1 };
        form.Controls.Add(next);
        input.TabIndex = 0;
        input.NativeEditor.Focus();
        input.Text = "31/11";
        Assert.That(PostMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Tab, IntPtr.Zero), Is.True);
        Application.DoEvents();
        Assert.That(input.NativeEditor.Focused, Is.True);
        Assert.That(input.PendingText, Is.EqualTo("31/11"));
        input.Text = "5/11";
        Assert.That(PostMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Tab, IntPtr.Zero), Is.True);
        Application.DoEvents();
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 11, 5)));
        Assert.That(next.Focused, Is.True);
    }

    [Test]
    public void Native_f4_opens_calendar_without_committing_pending_date()
    {
        using var form = Host(out var input);
        input.Text = "5/11";
        var opened = 0;
        input.Calendar.Opened += (_, _) => opened++;
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.F4, IntPtr.Zero);
        Application.DoEvents();
        Assert.That(opened, Is.EqualTo(1));
        Assert.That(input.PendingText, Is.EqualTo("5/11"));
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 23)));
        input.Calendar.CloseDropDown();
        SendMessage(input.NativeEditor.Handle, 0x020A, (IntPtr)(120 << 16), IntPtr.Zero); // WM_MOUSEWHEEL
        Assert.That(input.PendingText, Is.EqualTo("5/11"));
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 23)));
    }

    [Test]
    public void Native_alt_down_opens_calendar_without_accepting_candidate()
    {
        using var form = Host(out var input);
        input.Text = "5/11";
        input.NativeEditor.Focus();
        var opened = 0;
        input.Calendar.Opened += (_, _) => opened++;
        SendKeys.SendWait("%{DOWN}");
        Application.DoEvents();
        Assert.That(opened, Is.EqualTo(1));
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 23)));
        input.Calendar.CloseDropDown();
    }

    [Test]
    public void Theme_switch_and_popup_disposal_preserve_raw_date_until_owner_closes()
    {
        var previous = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var form = Host(out var input);
            input.Text = "5/11";
            input.NativeEditor.SelectionStart = 2;
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            Application.DoEvents();
            Assert.That(input.PendingText, Is.EqualTo("5/11"));
            Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(2));
            input.ShowCalendar();
            Application.DoEvents();
            form.Dispose();
            Assert.That(input.IsDisposed, Is.True);
        }
        finally { BootstrapThemeManager.CurrentTheme = previous; }
    }

    [Test]
    public void DateInputCorrection_Repeated_month_and_year_arrows_keep_native_caret_before_notification()
    {
        using var form = Host(out var input);
        input.ProjectAcceptedValue(new DateTime(2026, 10, 12));
        input.NativeEditor.SelectionStart = 4;
        var notifications = 0;
        var notifiedSelectionStarts = new List<int>();
        var notifiedSelectionLengths = new List<int>();
        EventHandler onPending = (_, _) =>
        {
            notifications++;
            notifiedSelectionStarts.Add(input.NativeEditor.SelectionStart);
            notifiedSelectionLengths.Add(input.NativeEditor.SelectionLength);
        };
        input.PendingTextChanged += onPending;
        foreach (var (key, expected) in new[]
        {
            (Keys.Up, "12/11/2026"), (Keys.Up, "12/12/2026"), (Keys.Down, "12/11/2026")
        })
        {
            SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)key, IntPtr.Zero);
            Assert.That(input.PendingText, Is.EqualTo(expected));
            Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(4));
            Assert.That(input.NativeEditor.SelectionLength, Is.Zero);
            Assert.That(notifiedSelectionStarts[notifications - 1], Is.EqualTo(4));
            Assert.That(notifiedSelectionLengths[notifications - 1], Is.Zero);
            Assert.That(input.NativeEditor.Focused, Is.True);
            Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 12)));
        }
        Assert.That(notifications, Is.EqualTo(3));

        input.ProjectAcceptedValue(new DateTime(2026, 10, 12));
        input.NativeEditor.SelectionStart = 8;
        input.PendingTextChanged -= onPending;
        foreach (var (key, expected) in new[]
        {
            (Keys.Up, "12/10/2027"), (Keys.Up, "12/10/2028"), (Keys.Down, "12/10/2027")
        })
        {
            SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)key, IntPtr.Zero);
            Assert.That(input.PendingText, Is.EqualTo(expected));
            Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(8));
            Assert.That(input.NativeEditor.SelectionLength, Is.Zero);
            Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 12)));
        }
    }

    [Test]
    public void DateInputCorrection_Short_date_maps_month_caret_and_cross_separator_selection()
    {
        using var form = Host(out var input);
        input.Text = "5/1";
        input.NativeEditor.SelectionStart = 2;
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("05/02/2026"));
        Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(3));
        Assert.That(input.NativeEditor.SelectionLength, Is.Zero);
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("05/03/2026"));
        Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(3));

        input.Text = "5/1";
        input.NativeEditor.Select(2, 1);
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("05/02/2026"));
        Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(3));
        Assert.That(input.NativeEditor.SelectionLength, Is.EqualTo(2));

        input.Text = "5/1";
        input.NativeEditor.Select(1, 2);
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("06/01/2026"));
        Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(2));
        Assert.That(input.NativeEditor.SelectionLength, Is.EqualTo(3));
    }

    [TestCase(Keys.Up, 1, "13/10/2026")]
    [TestCase(Keys.Down, 1, "11/10/2026")]
    [TestCase(Keys.Up, 4, "12/11/2026")]
    [TestCase(Keys.Down, 4, "12/09/2026")]
    [TestCase(Keys.Up, 8, "12/10/2027")]
    [TestCase(Keys.Down, 8, "12/10/2025")]
    public void DateInputCorrection_Each_segment_and_direction_preserves_collapsed_caret(Keys key, int caret, string expected)
    {
        using var form = Host(out var input);
        input.ProjectAcceptedValue(new DateTime(2026, 10, 12));
        input.NativeEditor.SelectionStart = caret;
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)key, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo(expected));
        Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(caret));
        Assert.That(input.NativeEditor.SelectionLength, Is.Zero);
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 12)));
    }

    [Test]
    public void DateInputCorrection_Rejected_and_readonly_arrows_preserve_raw_selection_and_value()
    {
        using var form = Host(out var input);
        input.Text = "31/11/2026";
        input.NativeEditor.Select(3, 2);
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("31/11/2026"));
        Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(3));
        Assert.That(input.NativeEditor.SelectionLength, Is.EqualTo(2));

        input.ProjectAcceptedValue(new DateTime(2026, 10, 23));
        input.NativeEditor.Select(3, 2);
        input.ReadOnly = true;
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("23/10/2026"));
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 23)));
        Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(3));
        Assert.That(input.NativeEditor.SelectionLength, Is.EqualTo(2));

        input.ReadOnly = false;
        input.NativeEditor.Select(3, 2);
        SendMessage(input.NativeEditor.Handle, WmImeStartComposition, IntPtr.Zero, IntPtr.Zero);
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("23/10/2026"));
        Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(3));
        Assert.That(input.NativeEditor.SelectionLength, Is.EqualTo(2));
        SendMessage(input.NativeEditor.Handle, WmImeEndComposition, IntPtr.Zero, IntPtr.Zero);

        input.Enabled = false;
        input.NativeEditor.Select(3, 2);
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("23/10/2026"));
        Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(3));
        Assert.That(input.NativeEditor.SelectionLength, Is.EqualTo(2));
    }

    [Test]
    public void DateInputCorrection_Out_of_bounds_and_impossible_year_step_keep_selection()
    {
        using var form = Host(out var input);
        input.Profile = new DateInputProfile(false, new DateTime(2024, 1, 1), new DateTime(2026, 12, 31));
        input.BeginEditSession(new DateTime(2024, 2, 29));
        input.NativeEditor.Select(6, 4);
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("29/02/2024"));
        Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(6));
        Assert.That(input.NativeEditor.SelectionLength, Is.EqualTo(4));

        input.ProjectAcceptedValue(new DateTime(2026, 12, 31));
        input.NativeEditor.Select(6, 4);
        SendMessage(input.NativeEditor.Handle, WmKeyDown, (IntPtr)Keys.Up, IntPtr.Zero);
        Assert.That(input.PendingText, Is.EqualTo("31/12/2026"));
        Assert.That(input.NativeEditor.SelectionStart, Is.EqualTo(6));
        Assert.That(input.NativeEditor.SelectionLength, Is.EqualTo(4));
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
