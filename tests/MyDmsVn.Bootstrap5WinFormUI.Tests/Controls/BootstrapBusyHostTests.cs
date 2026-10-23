using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapBusyHostTests
{
    [Test]
    public void Fast_operation_never_shows_overlay_and_leaves_content_enabled_and_focused()
    {
        using var form = new Form();
        using var content = new TextBox { Dock = DockStyle.Fill };
        using var delay = new ManualBusyDelay();
        using var host = new BootstrapBusyHost(content, delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host);
        form.Show();
        content.Focus();
        var focused = form.ActiveControl;

        var operation = host.Begin(new BusyPresentation("Loading"));
        operation.Dispose();
        delay.Fire();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Overlay(host).Visible, Is.False);
            Assert.That(content.Enabled, Is.True);
            Assert.That(form.ActiveControl, Is.SameAs(focused));
        }));
    }

    [Test]
    public void Newest_live_token_wins_and_old_release_cannot_hide_it()
    {
        using var form = new Form();
        using var content = new TextBox();
        using var delay = new ManualBusyDelay();
        using var host = new BootstrapBusyHost(content, delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host);
        form.Show();

        var old = host.Begin(new BusyPresentation("Old"));
        delay.Fire();
        var newest = host.Begin(new BusyPresentation("Newest"));
        old.Dispose();
        old.Dispose();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Overlay(host).Visible, Is.True);
            Assert.That(Message(host).Text, Is.EqualTo("Newest"));
            Assert.That(content.Enabled, Is.True);
        }));

        newest.Dispose();
        Assert.That(Overlay(host).Visible, Is.False);
    }

    [Test]
    public void Cancel_requests_only_current_operation_without_releasing_it()
    {
        using var form = new Form();
        using var delay = new ManualBusyDelay();
        using var host = new BootstrapBusyHost(new TextBox(), delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host);
        form.Show();
        var oldCalls = 0;
        var newCalls = 0;
        using var old = host.Begin(new BusyPresentation("Old", () => oldCalls++));
        delay.Fire();
        using var newest = host.Begin(new BusyPresentation("New", () => newCalls++));

        Cancel(host).PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(newCalls, Is.EqualTo(1));
            Assert.That(oldCalls, Is.Zero);
            Assert.That(Overlay(host).Visible, Is.True);
        }));

        newest.Dispose();
        Cancel(host).PerformClick();
        Assert.That(oldCalls, Is.EqualTo(1));
    }

    [Test]
    public void Completing_newest_operation_reveals_the_still_live_older_presentation()
    {
        using var form = new Form();
        using var delay = new ManualBusyDelay();
        using var host = new BootstrapBusyHost(new TextBox(), delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host);
        form.Show();
        using var old = host.Begin(new BusyPresentation("Older"));
        delay.Fire();
        var newest = host.Begin(new BusyPresentation("Newest"));

        newest.Dispose();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(Overlay(host).Visible, Is.True);
            Assert.That(Message(host).Text, Is.EqualTo("Older"));
        }));
    }

    [Test]
    public void Visible_overlay_tracks_theme_without_releasing_the_operation()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var form = new Form();
            using var delay = new ManualBusyDelay();
            using var host = new BootstrapBusyHost(new TextBox(), delay) { Dock = DockStyle.Fill };
            form.Controls.Add(host);
            form.Show();
            using var token = host.Begin(new BusyPresentation("Working"));
            delay.Fire();

            var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            BootstrapThemeManager.CurrentTheme = dark;

            Assert.Multiple((Action)(() =>
            {
                Assert.That(Overlay(host).BackColor, Is.EqualTo(dark.Colors.SurfaceSecondary));
                Assert.That(Message(host).ForeColor, Is.EqualTo(dark.Colors.Text));
                Assert.That(Overlay(host).Visible, Is.True);
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void Hide_cancels_pending_show_and_disposal_invalidates_tokens()
    {
        using var form = new Form();
        using var delay = new ManualBusyDelay();
        var host = new BootstrapBusyHost(new TextBox(), delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host);
        form.Show();
        var token = host.Begin(new BusyPresentation("Hidden"));

        host.Hide();
        delay.Fire();
        Assert.That(Overlay(host).Visible, Is.False);
        host.Show();
        Assert.That(delay.LastDelay, Is.EqualTo(TimeSpan.FromMilliseconds(150)));
        delay.Fire();
        Assert.That(Overlay(host).Visible, Is.True);

        host.Dispose();
        token.Dispose();
        Assert.Throws<ObjectDisposedException>((Action)(() => host.Begin(new BusyPresentation("Late"))));
    }

    [Test]
    public void Handle_recreation_hides_overlay_then_reschedules_current_operation()
    {
        using var form = new Form();
        using var delay = new ManualBusyDelay();
        using var host = new BootstrapBusyHost(new TextBox(), delay) { Dock = DockStyle.Fill };
        form.Controls.Add(host);
        form.Show();
        using var token = host.Begin(new BusyPresentation("Still working"));
        delay.Fire();
        Assert.That(Overlay(host).Visible, Is.True);

        typeof(Control).GetMethod("RecreateHandle", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(host, null);

        Assert.That(Overlay(host).Visible, Is.False);
        delay.Fire();
        Assert.That(Overlay(host).Visible, Is.True);
        Assert.That(Message(host).Text, Is.EqualTo("Still working"));
    }

    private static Panel Overlay(BootstrapBusyHost host) => host.Controls.OfType<Panel>().Single();
    private static Label Message(BootstrapBusyHost host) => Overlay(host).Controls.OfType<Label>().Single();
    private static Button Cancel(BootstrapBusyHost host) => Overlay(host).Controls.OfType<Button>().Single();

    private sealed class ManualBusyDelay : IBootstrapBusyDelay
    {
        private Action? _pending;
        public TimeSpan LastDelay { get; private set; }
        public void Schedule(TimeSpan delay, Action action) { LastDelay = delay; _pending = action; }
        public void Cancel() => _pending = null;
        public void Fire() { var pending = _pending; _pending = null; pending?.Invoke(); }
        public void Dispose() => _pending = null;
    }
}
