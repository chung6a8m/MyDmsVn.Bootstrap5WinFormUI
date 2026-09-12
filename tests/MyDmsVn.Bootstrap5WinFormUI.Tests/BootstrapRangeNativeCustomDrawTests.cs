using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapRangeNativeCustomDrawTests
{
    private const int WmKeyDown = 0x0100;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int VkRight = 0x27;

    [TestCase(Orientation.Horizontal, TickStyle.None, false)]
    [TestCase(Orientation.Horizontal, TickStyle.BottomRight, true)]
    [TestCase(Orientation.Vertical, TickStyle.None, false)]
    [TestCase(Orientation.Vertical, TickStyle.Both, true)]
    public void HostedTrackBarReceivesReflectedPrepaintAndNativeParts(
        Orientation orientation,
        TickStyle tickStyle,
        bool expectsTicks)
    {
        using var host = CreateHost(out var trackBar, orientation, tickStyle);

        ForcePaint(host, trackBar);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(trackBar.SawPrePaint, Is.True, "The parent HWND must reflect NM_CUSTOMDRAW to the child TrackBar.");
            Assert.That(trackBar.Parts, Does.Contain(BootstrapRangeNativePart.Channel));
            Assert.That(trackBar.Parts, Does.Contain(BootstrapRangeNativePart.Thumb));
            if (expectsTicks)
            {
                Assert.That(trackBar.Parts, Does.Contain(BootstrapRangeNativePart.Ticks));
            }
        }));
    }

    [Test]
    public void SuppressingOnlyPaintedPartsPreservesNativeKeyboardAndMouseMovement()
    {
        using var host = CreateHost(out var trackBar, Orientation.Horizontal, TickStyle.BottomRight);
        trackBar.SuppressChannelAndThumb = true;
        trackBar.Value = 4;
        ForcePaint(host, trackBar);

        trackBar.Focus();
        SendMessage(trackBar.Handle, WmKeyDown, new IntPtr(VkRight), IntPtr.Zero);
        var afterKeyboard = trackBar.Value;

        var channel = trackBar.LastChannelBounds;
        var click = new Point(Math.Max(channel.Left, channel.Right - 2), channel.Top + Math.Max(1, channel.Height / 2));
        SendMouseMessage(trackBar.Handle, WmLButtonDown, click, true);
        SendMouseMessage(trackBar.Handle, WmLButtonUp, click, false);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(trackBar.PaintedParts, Does.Contain(BootstrapRangeNativePart.Channel));
            Assert.That(trackBar.PaintedParts, Does.Contain(BootstrapRangeNativePart.Thumb));
            Assert.That(afterKeyboard, Is.GreaterThan(4), "Native arrow-key handling must remain authoritative.");
            Assert.That(trackBar.Value, Is.GreaterThanOrEqualTo(afterKeyboard), "Native channel clicks must continue to move the value.");
        }));
    }

    [Test]
    public void NativeThumbStateIsCharacterizedWithoutAssumingPresentationFlags()
    {
        using var host = CreateHost(out var trackBar, Orientation.Horizontal, TickStyle.None);
        ForcePaint(host, trackBar);
        var normalStates = trackBar.ThumbStates.ToArray();

        trackBar.ThumbStates.Clear();
        trackBar.Focus();
        ForcePaint(host, trackBar);
        var focusedStates = trackBar.ThumbStates.ToArray();

        trackBar.ThumbStates.Clear();
        trackBar.Enabled = false;
        ForcePaint(host, trackBar);
        var disabledStates = trackBar.ThumbStates.ToArray();

        Assert.That(normalStates, Is.Not.Empty);
        TestContext.Progress.WriteLine(
            $"TrackBar thumb uItemState: normal=[{FormatStates(normalStates)}], " +
            $"focused=[{FormatStates(focusedStates)}], disabled=[{FormatStates(disabledStates)}]");

        // The supported common-controls path supplies the thumb callback, but
        // no CDIS flag is stable for focus, hover, press, or disabled state on
        // both TFMs. Production therefore uses only Control state plus minimal
        // pointer/capture bookkeeping for presentation; native interaction is
        // still left untouched.
    }

    [Test]
    public void NativeTickMessagesExposePhysicalIntermediatePositionsAndChannelBounds()
    {
        using var host = CreateHost(out var trackBar, Orientation.Horizontal, TickStyle.BottomRight);
        trackBar.TickFrequency = 2;
        ForcePaint(host, trackBar);

        var channel = BootstrapRangeNativeMethods.GetChannelRectangle(trackBar.Handle);
        var positions = BootstrapRangeNativeMethods.GetIntermediateTickPositions(trackBar.Handle);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(channel.IsEmpty, Is.False);
            Assert.That(positions, Is.Not.Empty);
            Assert.That(positions.All(position => position >= channel.Left && position < channel.Right), Is.True);
        }));
    }

    private static Form CreateHost(
        out ProbeTrackBar trackBar,
        Orientation orientation,
        TickStyle tickStyle)
    {
        var host = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000),
            ClientSize = new Size(300, 180)
        };
        trackBar = new ProbeTrackBar
        {
            Orientation = orientation,
            TickStyle = tickStyle,
            TickFrequency = 1,
            Minimum = 0,
            Maximum = 10,
            Value = 5,
            Location = new Point(20, 20),
            Size = orientation == Orientation.Horizontal
                ? new Size(240, 60)
                : new Size(60, 130)
        };
        host.Controls.Add(trackBar);
        host.Show();
        _ = host.Handle;
        _ = trackBar.Handle;
        Application.DoEvents();
        return host;
    }

    private static void ForcePaint(Form host, ProbeTrackBar trackBar)
    {
        host.Refresh();
        trackBar.Refresh();
        Application.DoEvents();
    }

    private static void SendMouseMessage(IntPtr handle, int message, Point point, bool buttonDown)
    {
        var lParam = new IntPtr((point.Y << 16) | (point.X & 0xFFFF));
        SendMessage(handle, message, buttonDown ? new IntPtr(1) : IntPtr.Zero, lParam);
    }

    private static string FormatStates(IEnumerable<uint> states) =>
        string.Join(",", states.Select(state => $"0x{state:X}"));

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private sealed class ProbeTrackBar : TrackBar
    {
        internal bool SawPrePaint { get; private set; }

        internal bool SuppressChannelAndThumb { get; set; }

        internal HashSet<BootstrapRangeNativePart> Parts { get; } = new HashSet<BootstrapRangeNativePart>();

        internal HashSet<BootstrapRangeNativePart> PaintedParts { get; } = new HashSet<BootstrapRangeNativePart>();

        internal List<uint> ThumbStates { get; } = new List<uint>();

        internal Rectangle LastChannelBounds { get; private set; }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == BootstrapRangeNativeMethods.WmReflectNotify &&
                BootstrapRangeNativeMethods.TryReadCustomDraw(m.LParam, Handle, out var draw))
            {
                if (draw.DrawStage == BootstrapRangeNativeMethods.CddsPrePaint)
                {
                    SawPrePaint = true;
                    m.Result = new IntPtr(BootstrapRangeNativeMethods.CdrfNotifyItemDraw);
                    return;
                }

                if (draw.DrawStage == BootstrapRangeNativeMethods.CddsItemPrePaint)
                {
                    var part = BootstrapRangeNativeMethods.ClassifyPart(draw.ItemSpec);
                    Parts.Add(part);
                    if (part == BootstrapRangeNativePart.Channel)
                    {
                        LastChannelBounds = draw.Bounds;
                    }
                    else if (part == BootstrapRangeNativePart.Thumb)
                    {
                        ThumbStates.Add(draw.ItemState);
                    }

                    if (SuppressChannelAndThumb &&
                        (part == BootstrapRangeNativePart.Channel || part == BootstrapRangeNativePart.Thumb))
                    {
                        using var graphics = Graphics.FromHdc(draw.DeviceContext);
                        using var brush = new SolidBrush(part == BootstrapRangeNativePart.Channel ? Color.Magenta : Color.Lime);
                        graphics.FillRectangle(brush, draw.Bounds);
                        PaintedParts.Add(part);
                        m.Result = new IntPtr(BootstrapRangeNativeMethods.CdrfSkipDefault);
                        return;
                    }
                }
            }

            base.WndProc(ref m);
        }
    }
}
