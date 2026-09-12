using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapRangeTests
{
    private const int WmKeyDown = 0x0100;
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
}
