using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
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
}
