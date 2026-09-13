using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
public sealed class BootstrapRangeTests
{
    private const int WmKeyDown = 0x0100;
    private const int WmMouseMove = 0x0200;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int MkLButton = 0x0001;
    private const int VkRight = 0x27;
    private BootstrapTheme _originalTheme = null!;

    [SetUp]
    public void SetUp() => _originalTheme = BootstrapThemeManager.CurrentTheme;

    [TearDown]
    public void TearDown() => BootstrapThemeManager.CurrentTheme = _originalTheme;

    [Test]
    public void PublicContractIsThinNativeTrackBar()
    {
        using var range = new BootstrapRange();
        var declaredProperties = typeof(BootstrapRange)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapRange).BaseType, Is.EqualTo(typeof(TrackBar)));
            Assert.That(range.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(declaredProperties, Is.EqualTo(new[] { nameof(BootstrapRange.Variant) }));
            Assert.That(typeof(BootstrapRange).GetProperty(nameof(TrackBar.Value))!.DeclaringType, Is.EqualTo(typeof(TrackBar)));
            Assert.That(typeof(BootstrapRange).GetProperty(nameof(TrackBar.Minimum))!.DeclaringType, Is.EqualTo(typeof(TrackBar)));
            Assert.That(typeof(BootstrapRange).GetProperty(nameof(TrackBar.Maximum))!.DeclaringType, Is.EqualTo(typeof(TrackBar)));
            Assert.That(typeof(BootstrapRange).GetProperty(nameof(TrackBar.TickStyle))!.DeclaringType, Is.EqualTo(typeof(TrackBar)));
            Assert.That(typeof(BootstrapRange).GetProperty(nameof(TrackBar.Orientation))!.DeclaringType, Is.EqualTo(typeof(TrackBar)));
            Assert.That(typeof(BootstrapRange).GetProperty(nameof(TrackBar.RightToLeftLayout))!.DeclaringType, Is.EqualTo(typeof(TrackBar)));
        }));
    }

    [Test]
    public void VariantHasDesignerMetadataAndRejectsUndefinedValueBeforeMutation()
    {
        using var range = new BootstrapRange { Variant = BootstrapVariant.Success };
        var descriptor = TypeDescriptor.GetProperties(range)[nameof(BootstrapRange.Variant)]!;

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => range.Variant = (BootstrapVariant)999));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(range.Variant, Is.EqualTo(BootstrapVariant.Success));
            Assert.That(descriptor.Category, Is.EqualTo("Appearance"));
            Assert.That(descriptor.Attributes[typeof(DefaultValueAttribute)], Is.EqualTo(new DefaultValueAttribute(BootstrapVariant.Primary)));
            Assert.That(descriptor.ShouldSerializeValue(range), Is.True);
        }));
    }

    [Test]
    public void VariantAndThemeChangesPreserveEveryNativeRangeSetting()
    {
        using var range = new BootstrapRange
        {
            Minimum = -20,
            Maximum = 80,
            Value = 25,
            SmallChange = 2,
            LargeChange = 11,
            TickFrequency = 7,
            TickStyle = TickStyle.Both,
            Orientation = Orientation.Vertical
        };

        range.Variant = BootstrapVariant.Warning;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(range.Minimum, Is.EqualTo(-20));
            Assert.That(range.Maximum, Is.EqualTo(80));
            Assert.That(range.Value, Is.EqualTo(25));
            Assert.That(range.SmallChange, Is.EqualTo(2));
            Assert.That(range.LargeChange, Is.EqualTo(11));
            Assert.That(range.TickFrequency, Is.EqualTo(7));
            Assert.That(range.TickStyle, Is.EqualTo(TickStyle.Both));
            Assert.That(range.Orientation, Is.EqualTo(Orientation.Vertical));
        }));
    }

    [Test]
    public void InheritedValueChangedEventIsRaisedExactlyOnceByNativeValueAssignment()
    {
        using var range = new BootstrapRange();
        var count = 0;
        object? sender = null;
        range.ValueChanged += (actualSender, _) =>
        {
            count++;
            sender = actualSender;
        };

        range.Value = 7;
        range.Value = 7;

        Assert.That(count, Is.EqualTo(1));
        Assert.That(sender, Is.SameAs(range));
    }

    [Test]
    public void NativeKeyboardScrollAndValueChangedSequenceMatchesPlainTrackBar()
    {
        var native = CaptureNativeKeyboardSequence(new TrackBar());
        var bootstrap = CaptureNativeKeyboardSequence(new BootstrapRange());

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Value, Is.GreaterThan(4));
            Assert.That(bootstrap.Value, Is.EqualTo(native.Value));
            Assert.That(bootstrap.ScrollCount, Is.EqualTo(native.ScrollCount));
            Assert.That(bootstrap.ValueChangedCount, Is.EqualTo(native.ValueChangedCount));
        }));
    }

    [Test]
    public void HostedCustomDrawPaintsChannelAndThumbWithoutChangingNativeValue()
    {
        using var host = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000)
        };
        using var range = new ProbeBootstrapRange
        {
            Minimum = 0,
            Maximum = 100,
            Value = 40,
            TickStyle = TickStyle.None,
            Size = new Size(240, 50)
        };
        host.Controls.Add(range);
        host.Show();
        _ = range.Handle;

        for (var value = 40; value < 50; value++)
        {
            range.Value = value;
            range.Refresh();
            Application.DoEvents();
        }

        var channelCountBeforeRecreation = range.SuppressedChannelDrawCount;
        var thumbCountBeforeRecreation = range.SuppressedThumbDrawCount;
        range.RecreateHandleForTesting();
        range.Refresh();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(range.SuppressedChannelDrawCount, Is.GreaterThan(channelCountBeforeRecreation));
            Assert.That(range.SuppressedThumbDrawCount, Is.GreaterThan(thumbCountBeforeRecreation));
            Assert.That(range.IsHandleCreated, Is.True);
            Assert.That(range.Value, Is.EqualTo(49));
            Assert.That(range.Minimum, Is.Zero);
            Assert.That(range.Maximum, Is.EqualTo(100));
        }));
    }

    [Test]
    public void ThumbPresentationTracksNativeBoundsAndClearsTransientState()
    {
        using var host = CreateHostedRange(out var range);
        var thumb = BootstrapRangeNativeMethods.GetThumbRectangle(range.Handle);
        var center = new Point(thumb.Left + (thumb.Width / 2), thumb.Top + (thumb.Height / 2));

        SendMouseMessage(range.Handle, WmMouseMove, center, buttonDown: false);
        Assert.That(range.CurrentVisualState.Hot, Is.True);

        SendMouseMessage(range.Handle, WmLButtonDown, center, buttonDown: true);
        Assert.That(range.CurrentVisualState.Pressed, Is.True);

        SendMouseMessage(range.Handle, WmLButtonUp, center, buttonDown: false);
        Assert.That(range.CurrentVisualState.Pressed, Is.False);

        range.RaiseMouseLeaveForTesting();
        Assert.That(range.CurrentVisualState.Hot, Is.False);

        range.Enabled = false;
        Assert.Multiple((Action)(() =>
        {
            Assert.That(range.CurrentVisualState.Enabled, Is.False);
            Assert.That(range.CurrentVisualState.Hot, Is.False);
            Assert.That(range.CurrentVisualState.Pressed, Is.False);
        }));
    }

    [Test]
    public void ThumbHoverIsRecomputedWhenValueMovesUnderAStationaryPointer()
    {
        using var host = CreateHostedRange(out var range);
        range.Minimum = 0;
        range.Maximum = 100;
        range.Value = range.Maximum;
        range.Refresh();
        Application.DoEvents();
        var destinationThumb = BootstrapRangeNativeMethods.GetThumbRectangle(range.Handle);
        var stationaryPointer = new Point(
            destinationThumb.Left + (destinationThumb.Width / 2),
            destinationThumb.Top + (destinationThumb.Height / 2));

        range.Value = range.Minimum;
        range.Refresh();
        Application.DoEvents();
        range.RaiseMouseMoveForTesting(stationaryPointer);
        Assert.That(range.CurrentVisualState.Hot, Is.False);

        range.Value = range.Maximum;
        range.Refresh();
        Application.DoEvents();
        Assert.That(range.CurrentVisualState.Hot, Is.True);

        range.Value = range.Minimum;
        range.Refresh();
        Application.DoEvents();
        Assert.That(range.CurrentVisualState.Hot, Is.False);
    }

    [Test]
    public void ThumbHoverPointerSnapshotSurvivesNativeHandleRecreation()
    {
        using var host = CreateHostedRange(out var range);
        range.Minimum = 0;
        range.Maximum = 100;
        range.Value = range.Maximum;
        range.Refresh();
        Application.DoEvents();
        var thumb = BootstrapRangeNativeMethods.GetThumbRectangle(range.Handle);
        var stationaryPointer = new Point(
            thumb.Left + (thumb.Width / 2),
            thumb.Top + (thumb.Height / 2));
        range.RaiseMouseMoveForTesting(stationaryPointer);
        Assert.That(range.CurrentVisualState.Hot, Is.True);

        range.RecreateHandleForTesting();
        range.Refresh();
        Application.DoEvents();

        Assert.That(range.CurrentVisualState.Hot, Is.True);
    }

    [Test]
    public void NativeHomeEndPageAndArrowKeysMatchPlainTrackBar()
    {
        var keys = new[] { Keys.Right, Keys.PageUp, Keys.PageDown, Keys.End, Keys.Home };
        var native = CaptureNativeKeyMatrix(new TrackBar(), keys);
        var bootstrap = CaptureNativeKeyMatrix(new BootstrapRange(), keys);

        Assert.That(bootstrap.Values, Is.EqualTo(native.Values));
        Assert.That(bootstrap.EventSequence, Is.EqualTo(native.EventSequence));
    }

    [Test]
    public void NativeThumbDragAndTabFocusRemainAuthoritative()
    {
        var nativeValue = CaptureNativeDrag(new TrackBar());
        var bootstrapValue = CaptureNativeDrag(new BootstrapRange());

        using var host = new Form();
        using var first = new BootstrapRange { TabIndex = 0 };
        using var second = new TextBox { TabIndex = 1 };
        host.Controls.Add(first);
        host.Controls.Add(second);
        host.Show();
        first.Focus();
        var moved = host.SelectNextControl(first, forward: true, tabStopOnly: true, nested: true, wrap: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(bootstrapValue, Is.EqualTo(nativeValue));
            Assert.That(bootstrapValue, Is.GreaterThan(5));
            Assert.That(moved, Is.True);
            Assert.That(second.Focused, Is.True);
        }));
    }

    [TestCase(Orientation.Horizontal, TickStyle.None)]
    [TestCase(Orientation.Horizontal, TickStyle.Both)]
    [TestCase(Orientation.Vertical, TickStyle.TopLeft)]
    [TestCase(Orientation.Vertical, TickStyle.BottomRight)]
    public void RuntimeLayoutChangesPreserveValueAndKeepCustomDrawAlive(Orientation orientation, TickStyle tickStyle)
    {
        using var host = CreateHostedRange(out var range);
        range.Minimum = -10;
        range.Maximum = 50;
        range.Value = 17;
        range.Orientation = orientation;
        range.TickStyle = tickStyle;
        range.TickFrequency = 3;
        range.RightToLeft = RightToLeft.Yes;
        range.RightToLeftLayout = true;
        range.ResetSuppressedDrawCounts();
        range.Refresh();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(range.Value, Is.EqualTo(17));
            Assert.That(range.Orientation, Is.EqualTo(orientation));
            Assert.That(range.TickStyle, Is.EqualTo(tickStyle));
            Assert.That(range.SuppressedChannelDrawCount, Is.GreaterThan(0));
            Assert.That(range.SuppressedThumbDrawCount, Is.GreaterThan(0));
            Assert.That(
                range.SuppressedTickDrawCount,
                tickStyle == TickStyle.None ? Is.Zero : Is.GreaterThan(0));
        }));
    }

    [Test]
    public void PathologicalNativeTickCountFallsBackBeforePerTickEnumeration()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That(
                BootstrapRangeNativeMethods.IsCustomTickCountSupported(
                    BootstrapRangeNativeMethods.MaximumCustomDrawTickCount),
                Is.True);
            Assert.That(
                BootstrapRangeNativeMethods.IsCustomTickCountSupported(
                    (long)BootstrapRangeNativeMethods.MaximumCustomDrawTickCount + 1L),
                Is.False,
                "Large native tick sets must be rejected before the per-position message loop.");
        }));
    }

    [Test]
    public void RtlThumbGeometryMatchesNativeTrackBarWithoutFrameworkValueReversal()
    {
        var native = CaptureRtlThumbGeometry(new TrackBar());
        var bootstrap = CaptureRtlThumbGeometry(new BootstrapRange());

        Assert.That(bootstrap.Bounds, Is.EqualTo(native.Bounds));
        Assert.That(bootstrap.Value, Is.EqualTo(native.Value));
    }

    [Test]
    public void AccessibilityAndInheritedPropertiesRemainNativeTrackBarContracts()
    {
        using var native = new TrackBar { Value = 3, AccessibleName = "Native range", AccessibleDescription = "Native description" };
        using var range = new BootstrapRange
        {
            Value = 3,
            AccessibleName = "Native range",
            AccessibleDescription = "Native description",
            TabStop = true
        };
        _ = native.Handle;
        _ = range.Handle;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(range.AccessibilityObject.Role, Is.EqualTo(native.AccessibilityObject.Role));
            Assert.That(range.AccessibilityObject.Name, Is.EqualTo(native.AccessibilityObject.Name));
            Assert.That(range.AccessibilityObject.Description, Is.EqualTo(native.AccessibilityObject.Description));
            Assert.That(range.AccessibilityObject.Value, Is.EqualTo(native.AccessibilityObject.Value));
            Assert.That(range.TabStop, Is.True);
        }));

        native.Value = 8;
        range.Value = 8;
        Assert.That(range.AccessibilityObject.Value, Is.EqualTo(native.AccessibilityObject.Value));
    }

    [Test]
    public void ReparentHandleRecreationAndLayoutChangesPreserveNativeState()
    {
        using var firstHost = new Form();
        using var secondHost = new Form();
        using var range = new ProbeBootstrapRange
        {
            Minimum = -5,
            Maximum = 25,
            Value = 12,
            SmallChange = 2,
            LargeChange = 6,
            TickFrequency = 4
        };
        firstHost.Controls.Add(range);
        firstHost.Show();
        _ = range.Handle;
        range.RecreateHandleForTesting();
        firstHost.Controls.Remove(range);
        secondHost.Controls.Add(range);
        secondHost.Show();
        range.Orientation = Orientation.Vertical;
        range.RightToLeft = RightToLeft.Yes;
        range.RightToLeftLayout = true;
        range.Refresh();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(range.Parent, Is.SameAs(secondHost));
            Assert.That(range.IsHandleCreated, Is.True);
            Assert.That(range.Minimum, Is.EqualTo(-5));
            Assert.That(range.Maximum, Is.EqualTo(25));
            Assert.That(range.Value, Is.EqualTo(12));
            Assert.That(range.SmallChange, Is.EqualTo(2));
            Assert.That(range.LargeChange, Is.EqualTo(6));
            Assert.That(range.TickFrequency, Is.EqualTo(4));
        }));
    }

    [Test]
    public void DesignerDefaultsKeepVariantMinimalAndNativePropertiesBrowsable()
    {
        using var range = new BootstrapRange();
        var properties = TypeDescriptor.GetProperties(range);
        var variant = properties[nameof(BootstrapRange.Variant)]!;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(variant.ShouldSerializeValue(range), Is.False);
            Assert.That(properties[nameof(TrackBar.Minimum)]!.IsBrowsable, Is.True);
            Assert.That(properties[nameof(TrackBar.Maximum)]!.IsBrowsable, Is.True);
            Assert.That(properties[nameof(TrackBar.Value)]!.IsBrowsable, Is.True);
            Assert.That(properties[nameof(TrackBar.TickFrequency)]!.IsBrowsable, Is.True);
            Assert.That(properties[nameof(TrackBar.Orientation)]!.IsBrowsable, Is.True);
        }));
    }

    [Test]
    public void RepeatedThemeValueAndInvalidationStressCompletesDeterministically()
    {
        using var host = CreateHostedRange(out var range);
        range.Minimum = 0;
        range.Maximum = 100;
        for (var index = 0; index < 150; index++)
        {
            range.Value = index % 101;
            range.Variant = (BootstrapVariant)(index % 8);
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(
                index % 2 == 0 ? BootstrapThemeMode.Light : BootstrapThemeMode.Dark);
            range.Invalidate();
            range.Update();
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(range.Value, Is.EqualTo(48));
            Assert.That(range.IsDisposed, Is.False);
            Assert.That(range.SuppressedChannelDrawCount, Is.GreaterThan(0));
            Assert.That(range.SuppressedThumbDrawCount, Is.GreaterThan(0));
        }));
    }

    [Test]
    public void DisposalRemovesThemeSubscription()
    {
        var before = GetThemeSubscriberCount();
        var range = new BootstrapRange();
        Assert.That(GetThemeSubscriberCount(), Is.EqualTo(before + 1));

        range.Dispose();

        Assert.That(GetThemeSubscriberCount(), Is.EqualTo(before));
    }

    private static int GetThemeSubscriberCount()
    {
        var field = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        var handler = (MulticastDelegate?)field!.GetValue(null);
        return handler?.GetInvocationList().Length ?? 0;
    }

    private static NativeEventSnapshot CaptureNativeKeyboardSequence(TrackBar trackBar)
    {
        using var host = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000)
        };
        using (trackBar)
        {
            trackBar.Minimum = 0;
            trackBar.Maximum = 10;
            trackBar.Value = 4;
            host.Controls.Add(trackBar);
            host.Show();
            _ = trackBar.Handle;
            trackBar.Focus();
            var scrollCount = 0;
            var valueChangedCount = 0;
            trackBar.Scroll += (_, _) => scrollCount++;
            trackBar.ValueChanged += (_, _) => valueChangedCount++;

            SendMessage(trackBar.Handle, WmKeyDown, new IntPtr(VkRight), IntPtr.Zero);
            Application.DoEvents();
            return new NativeEventSnapshot(trackBar.Value, scrollCount, valueChangedCount);
        }
    }

    private static Form CreateHostedRange(out ProbeBootstrapRange range)
    {
        var host = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000)
        };
        range = new ProbeBootstrapRange { Size = new Size(240, 50), Value = 5 };
        host.Controls.Add(range);
        host.Show();
        _ = range.Handle;
        range.Refresh();
        Application.DoEvents();
        return host;
    }

    private static NativeKeyMatrix CaptureNativeKeyMatrix(TrackBar trackBar, Keys[] keys)
    {
        using var host = new Form();
        using (trackBar)
        {
            trackBar.Minimum = 0;
            trackBar.Maximum = 100;
            trackBar.Value = 40;
            trackBar.SmallChange = 2;
            trackBar.LargeChange = 10;
            host.Controls.Add(trackBar);
            host.Show();
            trackBar.Focus();
            var values = new int[keys.Length];
            var events = new System.Collections.Generic.List<string>();
            trackBar.Scroll += (_, _) => events.Add("Scroll");
            trackBar.ValueChanged += (_, _) => events.Add("ValueChanged");
            for (var index = 0; index < keys.Length; index++)
            {
                SendMessage(trackBar.Handle, WmKeyDown, new IntPtr((int)keys[index]), IntPtr.Zero);
                values[index] = trackBar.Value;
            }

            return new NativeKeyMatrix(values, events.ToArray());
        }
    }

    private static int CaptureNativeDrag(TrackBar trackBar)
    {
        using var host = new Form();
        using (trackBar)
        {
            trackBar.Minimum = 0;
            trackBar.Maximum = 10;
            trackBar.Value = 5;
            trackBar.Size = new Size(240, 50);
            host.Controls.Add(trackBar);
            host.Show();
            _ = trackBar.Handle;
            var thumb = BootstrapRangeNativeMethods.GetThumbRectangle(trackBar.Handle);
            var start = new Point(thumb.Left + (thumb.Width / 2), thumb.Top + (thumb.Height / 2));
            var end = new Point(start.X + 60, start.Y);
            SendMouseMessage(trackBar.Handle, WmLButtonDown, start, buttonDown: true);
            SendMouseMessage(trackBar.Handle, WmMouseMove, end, buttonDown: true);
            SendMouseMessage(trackBar.Handle, WmLButtonUp, end, buttonDown: false);
            Application.DoEvents();
            return trackBar.Value;
        }
    }

    private static RtlSnapshot CaptureRtlThumbGeometry(TrackBar trackBar)
    {
        using var host = new Form();
        using (trackBar)
        {
            trackBar.Minimum = 0;
            trackBar.Maximum = 10;
            trackBar.Value = 2;
            trackBar.Size = new Size(240, 50);
            trackBar.RightToLeft = RightToLeft.Yes;
            trackBar.RightToLeftLayout = true;
            host.Controls.Add(trackBar);
            host.Show();
            _ = trackBar.Handle;
            Application.DoEvents();
            return new RtlSnapshot(BootstrapRangeNativeMethods.GetThumbRectangle(trackBar.Handle), trackBar.Value);
        }
    }

    private static void SendMouseMessage(IntPtr handle, int message, Point point, bool buttonDown)
    {
        var lParam = new IntPtr((point.Y << 16) | (point.X & 0xFFFF));
        SendMessage(handle, message, buttonDown ? new IntPtr(MkLButton) : IntPtr.Zero, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private readonly struct NativeEventSnapshot
    {
        internal NativeEventSnapshot(int value, int scrollCount, int valueChangedCount)
        {
            Value = value;
            ScrollCount = scrollCount;
            ValueChangedCount = valueChangedCount;
        }

        internal int Value { get; }

        internal int ScrollCount { get; }

        internal int ValueChangedCount { get; }
    }

    private readonly struct NativeKeyMatrix
    {
        internal NativeKeyMatrix(int[] values, string[] eventSequence)
        {
            Values = values;
            EventSequence = eventSequence;
        }

        internal int[] Values { get; }

        internal string[] EventSequence { get; }
    }

    private readonly struct RtlSnapshot
    {
        internal RtlSnapshot(Rectangle bounds, int value)
        {
            Bounds = bounds;
            Value = value;
        }

        internal Rectangle Bounds { get; }

        internal int Value { get; }
    }

    private sealed class ProbeBootstrapRange : BootstrapRange
    {
        internal int SuppressedChannelDrawCount { get; private set; }

        internal int SuppressedThumbDrawCount { get; private set; }

        internal int SuppressedTickDrawCount { get; private set; }

        internal void RecreateHandleForTesting() => RecreateHandle();

        internal void RaiseMouseLeaveForTesting() => OnMouseLeave(EventArgs.Empty);

        internal void RaiseMouseMoveForTesting(Point location) =>
            OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, location.X, location.Y, 0));

        internal void ResetSuppressedDrawCounts()
        {
            SuppressedChannelDrawCount = 0;
            SuppressedThumbDrawCount = 0;
            SuppressedTickDrawCount = 0;
        }

        protected override void WndProc(ref Message m)
        {
            var part = BootstrapRangeNativePart.Unknown;
            if (m.Msg == BootstrapRangeNativeMethods.WmReflectNotify &&
                BootstrapRangeNativeMethods.TryReadCustomDraw(m.LParam, Handle, out var draw) &&
                draw.DrawStage == BootstrapRangeNativeMethods.CddsItemPrePaint)
            {
                part = BootstrapRangeNativeMethods.ClassifyPart(draw.ItemSpec);
            }

            base.WndProc(ref m);

            if (m.Result != new IntPtr(BootstrapRangeNativeMethods.CdrfSkipDefault))
            {
                return;
            }

            if (part == BootstrapRangeNativePart.Channel)
            {
                SuppressedChannelDrawCount++;
            }
            else if (part == BootstrapRangeNativePart.Thumb)
            {
                SuppressedThumbDrawCount++;
            }
            else if (part == BootstrapRangeNativePart.Ticks)
            {
                SuppressedTickDrawCount++;
            }
        }
    }
}
