using System;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapDateInputTests
{
    private static DateInputProfile Required => new DateInputProfile(false, new DateTime(1900, 1, 1), new DateTime(2100, 12, 31));

    [Test]
    public void Default_height_fits_themed_text_and_calendar_children()
    {
        using var input = new BootstrapDateInput();
        var expectedHeight = DpiScaler.Scale(BootstrapThemeManager.CurrentTheme.Metrics.ControlHeight, input.DeviceDpi);
        Assert.That(input.Height, Is.InRange(expectedHeight, expectedHeight + 2),
            "The date input must use its preferred control height before docking children expands them to the UserControl default height.");
    }

    [Test]
    public void Session_captures_reference_and_rejects_profile_change_with_pending_text()
    {
        using var input = new BootstrapDateInput { Profile = Required };
        var today = new DateTime(2026, 12, 31);
        input.ReferenceDateProvider = () => today;
        input.BeginEditSession(new DateTime(2026, 10, 23));
        input.Text = "5";
        today = new DateTime(2027, 1, 1);
        Assert.That(input.TryParsePending().Value, Is.EqualTo(new DateTime(2026, 12, 5)));
        Assert.Throws<InvalidOperationException>((Action)(() => input.Profile = new DateInputProfile(true, Required.Minimum, Required.Maximum)));
        input.ProjectAcceptedValue(new DateTime(2026, 11, 4));
        Assert.That(input.PendingText, Is.EqualTo("04/11/2026"));
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 11, 4)));
        input.BeginEditSession(new DateTime(2026, 11, 4));
        input.Text = "5";
        Assert.That(input.TryParsePending().Value, Is.EqualTo(new DateTime(2027, 1, 5)));
    }

    [Test]
    public void Invalid_required_input_stays_pending_until_explicit_commit_or_cancel()
    {
        using var input = new BootstrapDateInput { Profile = Required };
        input.BeginEditSession(new DateTime(2026, 10, 23));
        var changes = 0;
        input.ValueChanged += (_, _) => changes++;
        input.Text = "31/11";
        Assert.That(input.TryCommitPending(), Is.False);
        Assert.That(input.PendingText, Is.EqualTo("31/11"));
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 23)));
        input.ProjectAcceptedValue(input.Value);
        Assert.That(input.PendingText, Is.EqualTo("23/10/2026"));
        Assert.That(changes, Is.Zero);
    }

    [Test]
    public void Optional_blank_commits_null_and_required_blank_does_not()
    {
        using var input = new BootstrapDateInput { Profile = new DateInputProfile(true, Required.Minimum, Required.Maximum) };
        input.BeginEditSession(new DateTime(2026, 10, 23));
        input.Text = string.Empty;
        Assert.That(input.TryCommitPending(), Is.True);
        Assert.That(input.Value, Is.Null);
        input.Profile = Required;
        input.BeginEditSession(new DateTime(2026, 10, 23));
        input.Text = string.Empty;
        Assert.That(input.TryCommitPending(), Is.False);
        Assert.That(input.Value, Is.EqualTo(new DateTime(2026, 10, 23)));
    }

    [Test]
    public void Reused_profile_and_midnight_clock_keep_each_session_snapshot()
    {
        using var input = new BootstrapDateInput { Profile = Required };
        var today = new DateTime(2026, 12, 31, 23, 59, 59);
        input.ReferenceDateProvider = () => today;
        input.BeginEditSession(new DateTime(2026, 10, 23));
        input.Text = "5";
        today = new DateTime(2027, 1, 1);
        Assert.That(input.TryParsePending().Value, Is.EqualTo(new DateTime(2026, 12, 5)));
        input.ProjectAcceptedValue(new DateTime(2026, 10, 23));
        input.Profile = new DateInputProfile(true, new DateTime(2027, 1, 1), new DateTime(2027, 1, 31));
        input.BeginEditSession(null);
        input.Text = "5";
        Assert.That(input.TryParsePending().Value, Is.EqualTo(new DateTime(2027, 1, 5)));
        Assert.That(input.TryCommitPending(), Is.True);
        Assert.That(input.Value, Is.EqualTo(new DateTime(2027, 1, 5)));
    }

    [Test]
    public void New_candidate_in_same_session_recaptures_month_but_pending_candidate_keeps_its_reference()
    {
        using var input = new BootstrapDateInput { Profile = Required };
        var today = new DateTime(2026, 10, 31);
        input.ReferenceDateProvider = () => today;
        input.BeginEditSession(null);
        input.Text = "5";
        today = new DateTime(2026, 11, 1);
        Assert.That(input.TryParsePending().Value, Is.EqualTo(new DateTime(2026, 10, 5)));
        Assert.That(input.TryCommitPending(), Is.True);

        input.Text = "6";
        Assert.That(input.TryParsePending().Value, Is.EqualTo(new DateTime(2026, 11, 6)));
    }
}
