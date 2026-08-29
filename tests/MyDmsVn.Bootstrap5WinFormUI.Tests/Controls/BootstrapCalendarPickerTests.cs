using System;
using System.Collections.Generic;
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
public sealed class BootstrapCalendarPickerTests
{
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
            SendKeys.SendWait(ToSendKeysSequence(key));
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

    private static void SendHostedClick(Control control)
    {
        var center = new Point(control.Width / 2, control.Height / 2);
        var lParam = CreateMouseLParam(center.X, center.Y);
        SendMessage(control.Handle, 0x0201, (IntPtr)1, lParam);
        SendMessage(control.Handle, 0x0202, IntPtr.Zero, lParam);
    }

    private static string ToSendKeysSequence(Keys key)
    {
        return key switch
        {
            Keys.Left => "{LEFT}",
            Keys.Right => "{RIGHT}",
            Keys.Up => "{UP}",
            Keys.Down => "{DOWN}",
            Keys.PageUp => "{PGUP}",
            Keys.PageDown => "{PGDN}",
            Keys.Home => "{HOME}",
            Keys.End => "{END}",
            Keys.Enter => "{ENTER}",
            Keys.Space => " ",
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unsupported hosted-control key.")
        };
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

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
