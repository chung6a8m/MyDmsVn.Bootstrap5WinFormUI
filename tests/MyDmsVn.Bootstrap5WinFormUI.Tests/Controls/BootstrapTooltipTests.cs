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
public sealed class BootstrapTooltipTests
{
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [Test]
    public void DefaultsMatchStage3ContractAndNativeTimingDefaults()
    {
        using var native = new ToolTip();
        using var tooltip = new BootstrapTooltip();
        var defaultMetrics = BootstrapThemeMetrics.Default;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tooltip.Variant, Is.EqualTo(BootstrapVariant.Dark));
            Assert.That(tooltip.CustomColor, Is.EqualTo(Color.Empty));
            Assert.That(tooltip.BorderRadius, Is.EqualTo(-1));
            Assert.That(
                tooltip.ContentPadding,
                Is.EqualTo(new Padding(defaultMetrics.SpacingSM, defaultMetrics.SpacingXS, defaultMetrics.SpacingSM, defaultMetrics.SpacingXS)));
            Assert.That(tooltip.InitialDelay, Is.EqualTo(native.InitialDelay));
            Assert.That(tooltip.ReshowDelay, Is.EqualTo(native.ReshowDelay));
            Assert.That(tooltip.AutoPopDelay, Is.EqualTo(native.AutoPopDelay));
            Assert.That(tooltip.Active, Is.EqualTo(native.Active));
            Assert.That(tooltip.ShowAlways, Is.EqualTo(native.ShowAlways));
        }));
    }

    [Test]
    public void ManagedPositioningDefaultsAreBackwardCompatible()
    {
        using var tooltip = new BootstrapTooltip();
        var native = GetInnerToolTip(tooltip);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tooltip.Positioning, Is.EqualTo(BootstrapTooltipPositioning.Native));
            Assert.That(tooltip.Placement, Is.EqualTo(BootstrapOverlayPlacement.Top));
            Assert.That(tooltip.CollisionBehavior, Is.EqualTo(BootstrapOverlayCollisionBehavior.FlipAndShift));
            Assert.That(tooltip.Offset, Is.EqualTo(6));
            Assert.That(tooltip.BoundaryPadding, Is.EqualTo(8));
            Assert.That(native.OwnerDraw, Is.True);
            Assert.That(native.IsBalloon, Is.False);
        }));
    }

    [Test]
    public void ManagedPositioningPropertiesRejectInvalidValuesBeforeMutation()
    {
        using var tooltip = new BootstrapTooltip
        {
            Positioning = BootstrapTooltipPositioning.Managed,
            Placement = BootstrapOverlayPlacement.BottomEnd,
            CollisionBehavior = BootstrapOverlayCollisionBehavior.Shift,
            Offset = 9,
            BoundaryPadding = 11
        };

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => tooltip.Positioning = (BootstrapTooltipPositioning)99));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => tooltip.Placement = (BootstrapOverlayPlacement)99));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => tooltip.CollisionBehavior = (BootstrapOverlayCollisionBehavior)99));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => tooltip.Offset = -1));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => tooltip.BoundaryPadding = -1));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tooltip.Positioning, Is.EqualTo(BootstrapTooltipPositioning.Managed));
            Assert.That(tooltip.Placement, Is.EqualTo(BootstrapOverlayPlacement.BottomEnd));
            Assert.That(tooltip.CollisionBehavior, Is.EqualTo(BootstrapOverlayCollisionBehavior.Shift));
            Assert.That(tooltip.Offset, Is.EqualTo(9));
            Assert.That(tooltip.BoundaryPadding, Is.EqualTo(11));
        }));
    }

    [Test]
    public void ComponentProvidesToolTipExtenderOnlyToControls()
    {
        using var tooltip = new BootstrapTooltip();
        using var button = new Button();
        var attributes = typeof(BootstrapTooltip)
            .GetCustomAttributes(typeof(ProvidePropertyAttribute), inherit: false)
            .Cast<ProvidePropertyAttribute>()
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tooltip, Is.InstanceOf<IExtenderProvider>());
            Assert.That(tooltip.CanExtend(button), Is.True);
            Assert.That(tooltip.CanExtend(new object()), Is.False);
            Assert.That(attributes.Any(attribute => attribute.PropertyName == "ToolTip"), Is.True);
        }));
    }

    [Test]
    public void ContainerConstructorAddsWrapperToContainerAndWrapperAloneOwnsNativeToolTip()
    {
        using var container = new Container();
        var tooltip = new BootstrapTooltip(container);
        var native = GetInnerToolTip(tooltip);
        var nativeDisposed = 0;
        native.Disposed += (_, _) => nativeDisposed++;

        Assert.That(container.Components.Cast<IComponent>().Contains(tooltip), Is.True);
        Assert.That(container.Components.Cast<IComponent>().Contains(native), Is.False);

        container.Dispose();

        Assert.That(nativeDisposed, Is.EqualTo(1));
    }

    [Test]
    public void ContainerConstructorRejectsNullContainer()
    {
        Assert.Throws<ArgumentNullException>((Action)(() => new BootstrapTooltip(null!)));
    }

    [Test]
    public void AssociationUsesNativeToolTipAsSingleSourceOfTruthAndSupportsMultipleControls()
    {
        using var tooltip = new BootstrapTooltip();
        using var first = new Button();
        using var second = new Label();

        tooltip.SetToolTip(first, "First caption");
        tooltip.SetToolTip(second, "Second\r\ncaption");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tooltip.GetToolTip(first), Is.EqualTo("First caption"));
            Assert.That(tooltip.GetToolTip(second), Is.EqualTo("Second\r\ncaption"));
        }));

        tooltip.SetToolTip(first, string.Empty);
        Assert.That(tooltip.GetToolTip(first), Is.EqualTo(string.Empty));
    }

    [Test]
    public void AssociationMethodsRejectNullArgumentsBeforeNativeMutation()
    {
        using var tooltip = new BootstrapTooltip();
        using var button = new Button();
        tooltip.SetToolTip(button, "Existing");

        Assert.Throws<ArgumentNullException>((Action)(() => tooltip.SetToolTip(null!, "x")));
        Assert.Throws<ArgumentNullException>((Action)(() => tooltip.SetToolTip(button, null!)));
        Assert.Throws<ArgumentNullException>((Action)(() => tooltip.GetToolTip(null!)));
        Assert.That(tooltip.GetToolTip(button), Is.EqualTo("Existing"));
    }

    [Test]
    public void AppearanceValidationOccursBeforeStateMutation()
    {
        using var tooltip = new BootstrapTooltip
        {
            Variant = BootstrapVariant.Success,
            BorderRadius = 4,
            ContentPadding = new Padding(7, 3, 9, 5)
        };

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => tooltip.Variant = (BootstrapVariant)99));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => tooltip.BorderRadius = -2));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => tooltip.ContentPadding = new Padding(1, -1, 1, 1)));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tooltip.Variant, Is.EqualTo(BootstrapVariant.Success));
            Assert.That(tooltip.BorderRadius, Is.EqualTo(4));
            Assert.That(tooltip.ContentPadding, Is.EqualTo(new Padding(7, 3, 9, 5)));
        }));
    }

    [Test]
    public void TimingAndStatePropertiesForwardDirectlyToNativeToolTip()
    {
        using var tooltip = new BootstrapTooltip();
        var native = GetInnerToolTip(tooltip);

        tooltip.InitialDelay = 321;
        tooltip.ReshowDelay = 77;
        tooltip.AutoPopDelay = 4321;
        tooltip.Active = false;
        tooltip.ShowAlways = true;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.InitialDelay, Is.EqualTo(321));
            Assert.That(native.ReshowDelay, Is.EqualTo(77));
            Assert.That(native.AutoPopDelay, Is.EqualTo(4321));
            Assert.That(native.Active, Is.False);
            Assert.That(native.ShowAlways, Is.True);
        }));

        native.InitialDelay = 654;
        native.ReshowDelay = 88;
        native.AutoPopDelay = 5432;
        native.Active = true;
        native.ShowAlways = false;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(tooltip.InitialDelay, Is.EqualTo(654));
            Assert.That(tooltip.ReshowDelay, Is.EqualTo(88));
            Assert.That(tooltip.AutoPopDelay, Is.EqualTo(5432));
            Assert.That(tooltip.Active, Is.True);
            Assert.That(tooltip.ShowAlways, Is.False);
        }));
    }

    [Test]
    public void NativeToolTipIsOwnerDrawnNonBalloonAndWrapperAddsNoThemeSubscription()
    {
        var baselineThemeSubscriptions = GetThemeSubscriptionCount();
        using var tooltip = new BootstrapTooltip();
        var native = GetInnerToolTip(tooltip);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.OwnerDraw, Is.True);
            Assert.That(native.IsBalloon, Is.False);
            Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineThemeSubscriptions));
        }));

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        Assert.DoesNotThrow((Action)(() => tooltip.SetToolTip(new Button(), "Theme resolved at popup/draw time")));
        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineThemeSubscriptions));
    }

    [Test]
    public void DisposeDisposesOwnedNativeToolTipExactlyOnce()
    {
        var tooltip = new BootstrapTooltip();
        var native = GetInnerToolTip(tooltip);
        var disposedCount = 0;
        native.Disposed += (_, _) => disposedCount++;

        tooltip.Dispose();
        tooltip.Dispose();

        Assert.That(disposedCount, Is.EqualTo(1));
    }

    [Test]
    public void PublicSurfaceDoesNotLeakNativeTooltipOrUnplannedPopupApis()
    {
        var type = typeof(BootstrapTooltip);
        var forbiddenProperties = new[]
        {
            "OwnerDraw", "IsBalloon", "AutomaticDelay", "UseAnimation", "UseFading", "ToolTipTitle", "ToolTipIcon", "NativeToolTip", "Content"
        };
        var forbiddenMethods = new[] { "Show", "Hide" };

        Assert.Multiple((Action)(() =>
        {
            foreach (var property in forbiddenProperties)
            {
                Assert.That(type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public), Is.Null, property);
            }

            foreach (var method in forbiddenMethods)
            {
                Assert.That(type.GetMethod(method, BindingFlags.Instance | BindingFlags.Public), Is.Null, method);
            }
        }));
    }

    [Test]
    public void ManagedPopupFollowedByMouseLeaveDoesNotPostASecondNativeShow()
    {
        AssertManagedPopupInvalidation((_, target) => target.RaiseMouseLeave());
    }

    [Test]
    public void ManagedPopupFollowedByMouseDownDoesNotPostASecondNativeShow()
    {
        AssertManagedPopupInvalidation((_, target) => target.RaiseMouseDown());
    }

    [Test]
    public void ManagedPopupFollowedByVisibilityRoundTripDoesNotPostASecondNativeShow()
    {
        AssertManagedPopupInvalidation((_, target) =>
        {
            target.Visible = false;
            target.Visible = true;
        });
    }

    [Test]
    public void ManagedPopupFollowedByPositioningRoundTripDoesNotPostASecondNativeShow()
    {
        AssertManagedPopupInvalidation((tooltip, _) =>
        {
            tooltip.Positioning = BootstrapTooltipPositioning.Native;
            tooltip.Positioning = BootstrapTooltipPositioning.Managed;
        });
    }

    private static void AssertManagedPopupInvalidation(Action<BootstrapTooltip, ManagedTargetProbeControl> invalidate)
    {
        using var form = new Form { ShowInTaskbar = false };
        using var target = new ManagedTargetProbeControl { Size = new Size(100, 30) };
        using var tooltip = new BootstrapTooltip { Positioning = BootstrapTooltipPositioning.Managed };
        form.Controls.Add(target);
        tooltip.SetToolTip(target, "Managed");
        form.Show();
        form.Activate();
        Application.DoEvents();
        var native = GetInnerToolTip(tooltip);
        var nativePopupCount = 0;
        native.Popup += (_, _) => nativePopupCount++;
        var popup = new PopupEventArgs(form, target, false, new Size(80, 24));

        InvokePrivate(tooltip, "OnToolTipPopup", native, popup);
        invalidate(tooltip, target);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(popup.Cancel, Is.False);
            Assert.That(nativePopupCount, Is.Zero);
        }));
    }

    [Test]
    public void ManagedPositioningAppliesEngineBoundsToActualNativeTooltipWindow()
    {
        var workingArea = Screen.PrimaryScreen!.WorkingArea;
        using var form = new Form
        {
            Bounds = new Rectangle(workingArea.Right - 80, workingArea.Top + 160, 80, 60),
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            TopMost = true
        };
        using var target = new Button { Dock = DockStyle.Fill };
        using var tooltip = new BootstrapTooltip
        {
            Positioning = BootstrapTooltipPositioning.Managed,
            Placement = BootstrapOverlayPlacement.Right,
            CollisionBehavior = BootstrapOverlayCollisionBehavior.None,
            Offset = 0,
            BoundaryPadding = 0,
            InitialDelay = 1,
            ShowAlways = true
        };
        form.Controls.Add(target);
        tooltip.SetToolTip(target, "Managed edge tooltip");
        var native = GetInnerToolTip(tooltip);
        var actualBounds = Rectangle.Empty;
        var expectedBounds = Rectangle.Empty;
        var captureQueued = false;
        native.Popup += (_, e) =>
        {
            var anchor = target.RectangleToScreen(target.ClientRectangle);
            expectedBounds = new Rectangle(
                anchor.Right,
                anchor.Top + ((anchor.Height - e.ToolTipSize.Height) / 2),
                e.ToolTipSize.Width,
                e.ToolTipSize.Height);
        };
        native.Draw += (_, e) =>
        {
            var windowHandle = GetWindowHandle(e.Graphics);
            if (captureQueued)
            {
                return;
            }

            captureQueued = true;
            target.BeginInvoke((Action)(() =>
            {
                actualBounds = GetWindowBounds(windowHandle);
                native.Hide(target);
                form.Close();
            }));
        };
        form.Shown += (_, _) =>
        {
            form.Activate();
            Cursor.Position = target.PointToScreen(new Point(target.Width / 2, target.Height / 2));
            SendMessage(target.Handle, 0x0200, IntPtr.Zero, CreateMouseLParam(target.Width / 2, target.Height / 2));
        };

        var originalCursorPosition = Cursor.Position;
        try
        {
            form.ShowDialog();
        }
        finally
        {
            Cursor.Position = originalCursorPosition;
        }

        Assert.That(actualBounds, Is.EqualTo(expectedBounds));
    }

    private static ToolTip GetInnerToolTip(BootstrapTooltip tooltip)
    {
        var field = typeof(BootstrapTooltip).GetField("_toolTip", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (ToolTip)field!.GetValue(tooltip)!;
    }

    private static int GetThemeSubscriptionCount()
    {
        var eventField = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(eventField, Is.Not.Null);
        var handler = eventField!.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private static void InvokePrivate(object instance, string methodName, params object?[] arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(instance, arguments);
    }

    private static IntPtr GetWindowHandle(Graphics graphics)
    {
        var hdc = graphics.GetHdc();
        try
        {
            var handle = WindowFromDC(hdc);
            Assert.That(handle, Is.Not.EqualTo(IntPtr.Zero));
            return handle;
        }
        finally
        {
            graphics.ReleaseHdc(hdc);
        }
    }

    private static Rectangle GetWindowBounds(IntPtr handle)
    {
        Assert.That(GetWindowRect(handle, out var bounds), Is.True);
        return Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
    }

    private sealed class ManagedTargetProbeControl : Control
    {
        public void RaiseMouseLeave()
        {
            OnMouseLeave(EventArgs.Empty);
        }

        public void RaiseMouseDown()
        {
            OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 1, 1, 0));
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromDC(IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out NativeRectangle bounds);

    private static IntPtr CreateMouseLParam(int x, int y)
    {
        return (IntPtr)((y << 16) | (x & 0xffff));
    }
}
