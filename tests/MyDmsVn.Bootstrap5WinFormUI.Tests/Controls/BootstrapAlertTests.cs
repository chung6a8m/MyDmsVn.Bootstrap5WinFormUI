using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Icons;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapAlertTests
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
    public void DefaultsAndMetadataMatchStage2ContractAndConstructionIsDesignerSafe()
    {
        Assert.DoesNotThrow((Action)(() =>
        {
            using var alert = new BootstrapAlert();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(alert.Text, Is.EqualTo(string.Empty));
                Assert.That(alert.Variant, Is.EqualTo(BootstrapVariant.Primary));
                Assert.That(alert.Icon, Is.Null);
                Assert.That(alert.IconRenderer, Is.Not.Null);
                Assert.That(alert.Dismissible, Is.False);
                Assert.That(alert.BorderRadius, Is.EqualTo(-1));
                Assert.That(alert.TabStop, Is.False);
                Assert.That(alert.AccessibleRole, Is.EqualTo(AccessibleRole.Alert));
                Assert.That(alert.AccessibleDescription, Is.Not.Empty);
            }));

            Assert.That(TypeDescriptor.GetDefaultProperty(typeof(BootstrapAlert))?.Name, Is.EqualTo(nameof(Control.Text)));
            Assert.That(TypeDescriptor.GetDefaultEvent(typeof(BootstrapAlert))?.Name, Is.EqualTo(nameof(BootstrapAlert.Dismissed)));

            AssertDefaultValue(nameof(BootstrapAlert.Variant), BootstrapVariant.Primary);
            AssertDefaultValue(nameof(BootstrapAlert.Icon), null);
            AssertDefaultValue(nameof(BootstrapAlert.Dismissible), false);
            AssertDefaultValue(nameof(BootstrapAlert.BorderRadius), -1);

            var rendererProperty = TypeDescriptor.GetProperties(typeof(BootstrapAlert))[nameof(BootstrapAlert.IconRenderer)];
            Assert.That(rendererProperty, Is.Not.Null);
            Assert.That(rendererProperty!.IsBrowsable, Is.False);
            var serialization = (DesignerSerializationVisibilityAttribute?)rendererProperty.Attributes[typeof(DesignerSerializationVisibilityAttribute)];
            Assert.That(serialization?.Visibility, Is.EqualTo(DesignerSerializationVisibility.Hidden));
        }));
    }

    [Test]
    public void TextUsesNativeNormalizationAndRaisesTextChangedOnlyForEffectiveChanges()
    {
        using var alert = new BootstrapAlert();
        var changed = 0;
        alert.TextChanged += (_, _) => changed++;

        alert.Text = "Saved";
        alert.Text = "Saved";
        alert.Text = "Line one\r\nLine two";
        alert.Text = string.Empty;
        alert.Text = null!;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(alert.Text, Is.EqualTo(string.Empty));
            Assert.That(changed, Is.EqualTo(3));
        }));
    }

    [Test]
    public void PropertiesValidateBeforeMutationAndIconDoesNotChangeUnrelatedState()
    {
        using var alert = new BootstrapAlert
        {
            Text = "State must survive",
            Dismissible = true,
            Visible = true
        };

        foreach (BootstrapVariant variant in Enum.GetValues(typeof(BootstrapVariant)))
        {
            Assert.DoesNotThrow((Action)(() => alert.Variant = variant));
        }

        alert.Variant = BootstrapVariant.Success;
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => alert.Variant = (BootstrapVariant)999));
        Assert.That(alert.Variant, Is.EqualTo(BootstrapVariant.Success));

        alert.BorderRadius = -1;
        alert.BorderRadius = 0;
        alert.BorderRadius = 7;
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => alert.BorderRadius = -2));
        Assert.That(alert.BorderRadius, Is.EqualTo(7));

        var renderer = new RecordingIconRenderer();
        alert.IconRenderer = renderer;
        Assert.Throws<ArgumentNullException>((Action)(() => alert.IconRenderer = null!));
        Assert.That(alert.IconRenderer, Is.SameAs(renderer));

        var originalText = alert.Text;
        var originalVisibility = alert.Visible;
        var originalDismissible = alert.Dismissible;
        alert.Icon = IconDescriptor.Framework(FrameworkIconGlyph.Check);
        alert.Icon = null;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(alert.Text, Is.EqualTo(originalText));
            Assert.That(alert.Visible, Is.EqualTo(originalVisibility));
            Assert.That(alert.Dismissible, Is.EqualTo(originalDismissible));
        }));
    }

    [Test]
    public void AlertOwnsExactlyOnePrivateNativeDismissButton()
    {
        using var alert = new BootstrapAlert();

        Assert.That(alert.Controls.Count, Is.EqualTo(1));
        var button = alert.Controls.OfType<Button>().Single();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(button.GetType(), Is.EqualTo(typeof(Button)));
            Assert.That(button.Visible, Is.False);
            Assert.That(button.TabStop, Is.False);
            Assert.That(button.AccessibleRole, Is.EqualTo(AccessibleRole.PushButton));
            Assert.That(button.AccessibleName, Is.EqualTo("Dismiss alert"));
            Assert.That(button.AccessibleDescription, Is.EqualTo("Dismisses this alert."));
            Assert.That(alert.Controls.OfType<Panel>(), Is.Empty);
            Assert.That(alert.Controls.OfType<Label>(), Is.Empty);
            Assert.That(alert.Controls.OfType<BootstrapButton>(), Is.Empty);
        }));
    }

    [Test]
    public void AlertUsesDoubleBufferedOwnerPaintingStyles()
    {
        using var alert = new StyleProbeAlert();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(alert.HasStyle(ControlStyles.UserPaint), Is.True);
            Assert.That(alert.HasStyle(ControlStyles.AllPaintingInWmPaint), Is.True);
            Assert.That(alert.HasStyle(ControlStyles.OptimizedDoubleBuffer), Is.True);
            Assert.That(alert.HasStyle(ControlStyles.ResizeRedraw), Is.True);
            Assert.That(alert.HasStyle(ControlStyles.SupportsTransparentBackColor), Is.True);
            Assert.That(alert.BackColor, Is.EqualTo(Color.Transparent));
        }));
    }

    [Test]
    public void LayoutPlacesDismissButtonUsingPureAlertLayout()
    {
        using var alert = new BootstrapAlert
        {
            Size = new Size(360, 52),
            Dismissible = true
        };
        alert.PerformLayout();

        var dpi = alert.DeviceDpi > 0 ? alert.DeviceDpi : DpiScaler.DefaultDpi;
        var metrics = BootstrapAlertRenderLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, dpi, alert.BorderRadius);
        var expected = BootstrapAlertRenderLogic.CalculateLayout(alert.ClientRectangle, metrics, hasIcon: false, dismissible: true);
        var button = alert.Controls.OfType<Button>().Single();

        Assert.That(button.Bounds, Is.EqualTo(expected.CloseBounds));
    }

    [Test]
    public void OptionalIconUsesConfiguredRendererWithResolvedBoundsAndForeground()
    {
        using var host = new Form();
        using var alert = new BootstrapAlert
        {
            Bounds = new Rectangle(10, 10, 360, 52),
            Text = "Changes saved.",
            Variant = BootstrapVariant.Success,
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.Check)
        };
        var renderer = new RecordingIconRenderer();
        alert.IconRenderer = renderer;
        host.Controls.Add(alert);
        host.CreateControl();
        alert.CreateControl();
        alert.PerformLayout();

        using (var bitmap = new Bitmap(alert.Width, alert.Height))
        {
            alert.DrawToBitmap(bitmap, alert.ClientRectangle);
        }

        var dpi = alert.DeviceDpi > 0 ? alert.DeviceDpi : DpiScaler.DefaultDpi;
        var metrics = BootstrapAlertRenderLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, dpi, alert.BorderRadius);
        var layout = BootstrapAlertRenderLogic.CalculateLayout(alert.ClientRectangle, metrics, hasIcon: true, dismissible: false);
        var palette = BootstrapAlertRenderLogic.ResolvePalette(BootstrapThemeManager.CurrentTheme.Colors, alert.Variant, enabled: true);

        Assert.That(renderer.Calls.Count, Is.EqualTo(1));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(renderer.Calls[0].Descriptor, Is.SameAs(alert.Icon));
            Assert.That(renderer.Calls[0].Bounds, Is.EqualTo(layout.IconBounds));
            Assert.That(renderer.Calls[0].Color, Is.EqualTo(palette.Foreground));
        }));

        renderer.Calls.Clear();
        alert.Icon = null;
        using (var bitmap = new Bitmap(alert.Width, alert.Height))
        {
            alert.DrawToBitmap(bitmap, alert.ClientRectangle);
        }

        Assert.That(renderer.Calls, Is.Empty);
    }

    [Test]
    public void DismissButtonPaintUsesFrameworkCloseGlyphThroughConfiguredRenderer()
    {
        using var host = new Form();
        using var alert = new BootstrapAlert
        {
            Bounds = new Rectangle(10, 10, 360, 52),
            Dismissible = true
        };
        var renderer = new RecordingIconRenderer();
        alert.IconRenderer = renderer;
        host.Controls.Add(alert);
        host.CreateControl();
        alert.CreateControl();
        alert.PerformLayout();
        var button = alert.Controls.OfType<Button>().Single();
        button.CreateControl();

        using var bitmap = new Bitmap(Math.Max(1, button.Width), Math.Max(1, button.Height));
        button.DrawToBitmap(bitmap, button.ClientRectangle);

        Assert.That(renderer.Calls.Count, Is.EqualTo(1));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(renderer.Calls[0].Descriptor.SourceKind, Is.EqualTo(IconSourceKind.FrameworkVector));
            Assert.That(renderer.Calls[0].Descriptor.Value, Is.EqualTo(FrameworkIconGlyph.Close.ToString()));
            Assert.That(renderer.Calls[0].Bounds.Width, Is.GreaterThan(0));
            Assert.That(renderer.Calls[0].Bounds.Height, Is.GreaterThan(0));
        }));
    }

    [Test]
    public void PaintSmokeCoversVariantsThemesStatesIconsMultilineAndRadii()
    {
        var variants = Enum.GetValues(typeof(BootstrapVariant)).Cast<BootstrapVariant>().ToArray();
        var radii = new[] { -1, 0, 10 };

        foreach (var mode in new[] { BootstrapThemeMode.Light, BootstrapThemeMode.Dark })
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode);
            using var host = new Form { ClientSize = new Size(500, 500) };

            for (var index = 0; index < variants.Length; index++)
            {
                using var alert = new BootstrapAlert
                {
                    Bounds = new Rectangle(10, 10, 420, index % 3 == 0 ? 72 : 52),
                    Text = index % 3 == 0
                        ? "Upload failed.\r\nCheck the connection and try again."
                        : "Alert presentation smoke test.",
                    Variant = variants[index],
                    Enabled = index % 4 != 0,
                    Dismissible = index % 2 == 0,
                    BorderRadius = radii[index % radii.Length],
                    Icon = index % 2 == 1 ? IconDescriptor.Framework(FrameworkIconGlyph.Check) : null
                };
                host.Controls.Add(alert);
                host.CreateControl();
                alert.CreateControl();
                alert.PerformLayout();

                using var bitmap = new Bitmap(alert.Width, alert.Height);
                Assert.DoesNotThrow((Action)(() => alert.DrawToBitmap(bitmap, alert.ClientRectangle)), $"{mode}/{variants[index]}");
                host.Controls.Remove(alert);
            }
        }
    }

    [Test]
    public void ProgrammaticDismissRaisesExactlyOncePerEffectiveVisibleTransition()
    {
        using var alert = new BootstrapAlert { Visible = true };
        var dismissed = 0;
        alert.Dismissed += (_, _) => dismissed++;

        alert.Dismiss();
        alert.Dismiss();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(alert.Visible, Is.False);
            Assert.That(dismissed, Is.EqualTo(1));
            Assert.That(alert.IsDisposed, Is.False);
        }));

        alert.Visible = true;
        alert.Dismiss();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(alert.Visible, Is.False);
            Assert.That(dismissed, Is.EqualTo(2));
        }));
    }

    [Test]
    public void DirectVisibleChangesDoNotSynthesizeDismissedEvent()
    {
        using var alert = new BootstrapAlert { Visible = true };
        var dismissed = 0;
        alert.Dismissed += (_, _) => dismissed++;

        alert.Visible = false;
        alert.Visible = true;
        alert.Visible = false;

        Assert.That(dismissed, Is.EqualTo(0));
    }

    [Test]
    public void DisabledAlertCanStillBeDismissedProgrammatically()
    {
        using var alert = new BootstrapAlert
        {
            Enabled = false,
            Visible = true
        };
        var dismissed = 0;
        alert.Dismissed += (_, _) => dismissed++;

        alert.Dismiss();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(alert.Visible, Is.False);
            Assert.That(dismissed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NativeDismissButtonUsesTheSameDismissPath()
    {
        using var host = new Form { ClientSize = new Size(420, 120) };
        using var alert = new BootstrapAlert
        {
            Bounds = new Rectangle(10, 10, 360, 52),
            Dismissible = true
        };
        host.Controls.Add(alert);
        var dismissed = 0;
        alert.Dismissed += (_, _) => dismissed++;

        host.Show();
        Application.DoEvents();
        var button = alert.Controls.OfType<Button>().Single();
        button.PerformClick();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(alert.Visible, Is.False);
            Assert.That(dismissed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void DismissibilityControlsNativeFocusabilityWithoutDuplicatingChildrenOrHandlers()
    {
        using var alert = new BootstrapAlert();
        var button = alert.Controls.OfType<Button>().Single();
        var dismissed = 0;
        alert.Dismissed += (_, _) => dismissed++;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(alert.TabStop, Is.False);
            Assert.That(button.Visible, Is.False);
            Assert.That(button.TabStop, Is.False);
        }));

        for (var index = 0; index < 5; index++)
        {
            alert.Dismissible = true;
            alert.PerformLayout();
            Assert.That(button.Visible, Is.True);
            Assert.That(button.TabStop, Is.True);
            alert.Dismissible = false;
            alert.PerformLayout();
            Assert.That(button.Visible, Is.False);
            Assert.That(button.TabStop, Is.False);
        }

        alert.Dismissible = true;
        alert.Enabled = false;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(button.Enabled, Is.False);
            Assert.That(alert.Controls.Count, Is.EqualTo(1));
        }));

        alert.Enabled = true;
        alert.Visible = true;
        button.PerformClick();
        Assert.That(dismissed, Is.EqualTo(1));
    }

    [Test]
    public void RuntimeThemeSwitchUpdatesPaletteAndThemeOwnedFontInPlace()
    {
        using var alert = new BootstrapAlert
        {
            Variant = BootstrapVariant.Info,
            Dismissible = true
        };
        var button = alert.Controls.OfType<Button>().Single();
        var initialReference = alert;

        var darkTypography = new BootstrapThemeTypography(
            new BootstrapFontToken("Segoe UI", 11f, FontStyle.Bold),
            BootstrapThemeTypography.Default.BodySmall,
            BootstrapThemeTypography.Default.Label,
            BootstrapThemeTypography.Default.HeadingSmall,
            BootstrapThemeTypography.Default.HeadingMedium);
        var darkBase = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        BootstrapThemeManager.CurrentTheme = new BootstrapTheme(
            BootstrapThemeMode.Dark,
            darkBase.Colors,
            darkBase.Metrics,
            darkTypography);

        var expected = BootstrapAlertRenderLogic.ResolvePalette(
            BootstrapThemeManager.CurrentTheme.Colors,
            alert.Variant,
            enabled: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(alert, Is.SameAs(initialReference));
            Assert.That(button.BackColor, Is.EqualTo(expected.Surface));
            Assert.That(button.ForeColor, Is.EqualTo(expected.Foreground));
            Assert.That(alert.Font.SizeInPoints, Is.EqualTo(11f).Within(0.05f));
            Assert.That(alert.Font.Style, Is.EqualTo(FontStyle.Bold));
        }));
    }

    [Test]
    public void CallerOwnedFontRemainsCallerOwnedAcrossThemeSwitchAndDisposal()
    {
        using var callerFont = new Font("Segoe UI", 10f, FontStyle.Italic);
        var alert = new BootstrapAlert
        {
            Font = callerFont
        };

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        Assert.That(alert.Font, Is.SameAs(callerFont));

        alert.Dispose();

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.DoesNotThrow((Action)(() => graphics.MeasureString("x", callerFont)));
    }

    [Test]
    public void DisposalReleasesThemeSubscriptionAndThemeOwnedFont()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();
        var alert = new BootstrapAlert();
        var ownedFont = alert.Font;

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions + 1));

        alert.Dispose();

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
        Assert.DoesNotThrow((Action)(() =>
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark)));

        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.Catch((Action)(() => graphics.MeasureString("x", ownedFont)));
    }

    [Test]
    public void RepeatedLifecycleStressDoesNotDuplicateChildrenOrThrowOnThemeChanges()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();

        for (var index = 0; index < 100; index++)
        {
            using var alert = new BootstrapAlert
            {
                Dismissible = index % 2 == 0,
                Icon = index % 3 == 0 ? IconDescriptor.Framework(FrameworkIconGlyph.Check) : null
            };
            alert.PerformLayout();
            Assert.That(alert.Controls.Count, Is.EqualTo(1));

            if (index % 20 == 0)
            {
                var mode = BootstrapThemeManager.CurrentTheme.Mode == BootstrapThemeMode.Light
                    ? BootstrapThemeMode.Dark
                    : BootstrapThemeMode.Light;
                Assert.DoesNotThrow((Action)(() =>
                    BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode)));
            }
        }

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
    }

    private static void AssertDefaultValue(string propertyName, object? expected)
    {
        var property = TypeDescriptor.GetProperties(typeof(BootstrapAlert))[propertyName];
        Assert.That(property, Is.Not.Null);
        var attribute = (DefaultValueAttribute?)property!.Attributes[typeof(DefaultValueAttribute)];
        Assert.That(attribute, Is.Not.Null, $"{propertyName} should declare DefaultValueAttribute.");
        Assert.That(attribute!.Value, Is.EqualTo(expected));
    }

    private static int GetThemeSubscriptionCount()
    {
        var eventField = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(eventField, Is.Not.Null);
        var handler = eventField!.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private sealed class StyleProbeAlert : BootstrapAlert
    {
        public bool HasStyle(ControlStyles style)
        {
            return GetStyle(style);
        }
    }

    private sealed class RecordingIconRenderer : IIconRenderer
    {
        public List<RenderCall> Calls { get; } = new List<RenderCall>();

        public bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color)
        {
            Calls.Add(new RenderCall(descriptor, bounds, color));
            return true;
        }
    }

    private readonly struct RenderCall
    {
        public RenderCall(IconDescriptor descriptor, Rectangle bounds, Color color)
        {
            Descriptor = descriptor;
            Bounds = bounds;
            Color = color;
        }

        public IconDescriptor Descriptor { get; }

        public Rectangle Bounds { get; }

        public Color Color { get; }
    }
}
