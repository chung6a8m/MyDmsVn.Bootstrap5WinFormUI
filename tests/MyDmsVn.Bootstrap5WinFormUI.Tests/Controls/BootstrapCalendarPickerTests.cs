using System;
using System.Collections.Generic;
using System.Drawing;
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
        probe!.RaiseClick();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.Zero);
            Assert.That(focusedAfterOpened, Is.True);
            Assert.That(probe.Focused, Is.True);
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
            Assert.That(probe.RaiseIsInputKey(key), Is.True, $"{key} must remain input for a hosted calendar control.");
            probe.RaiseKeyDown(key);
            Application.DoEvents();
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(focusSucceeded, Is.True);
            Assert.That(probe.Focused, Is.True);
            Assert.That(probe.InputKeys, Is.EqualTo(keys));
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

        public void RaiseClick()
        {
            OnClick(EventArgs.Empty);
        }

        public bool RaiseIsInputKey(Keys keyData)
        {
            return IsInputKey(keyData);
        }

        public void RaiseKeyDown(Keys keyData)
        {
            OnKeyDown(new KeyEventArgs(keyData));
        }

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
