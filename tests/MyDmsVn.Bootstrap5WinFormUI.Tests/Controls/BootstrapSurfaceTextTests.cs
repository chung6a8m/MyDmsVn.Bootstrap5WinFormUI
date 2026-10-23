using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapSurfaceTextTests
{
    [Test]
    public void SemanticRolesFollowThemeWithoutChangingNativeLayout()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using var panel = new BootstrapPanel { SurfaceRole = BootstrapSurfaceRole.Alternate, AutoScroll = true };
            using var label = new BootstrapLabel { TextRole = BootstrapTextRole.Warning, AutoSize = true, UseMnemonic = true, Text = "&Warning text that wraps" };
            panel.Controls.Add(label);

            var dark = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            BootstrapThemeManager.CurrentTheme = dark;

            Assert.Multiple((Action)(() =>
            {
                Assert.That(panel, Is.InstanceOf<Panel>());
                Assert.That(panel.AutoScroll, Is.True);
                Assert.That(panel.BackColor, Is.EqualTo(dark.Colors.SurfaceSecondary));
                Assert.That(label, Is.InstanceOf<Label>());
                Assert.That(label.AutoSize, Is.True);
                Assert.That(label.UseMnemonic, Is.True);
                Assert.That(label.Text, Is.EqualTo("&Warning text that wraps"));
                Assert.That(label.ForeColor, Is.EqualTo(dark.Colors.Warning));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ExplicitColorsAndCallerFontSurviveThemeAndRoleChanges()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        using var callerFont = new Font("Segoe UI", 12f, FontStyle.Italic);
        try
        {
            using (var panel = new BootstrapPanel { BackColor = Color.MediumPurple, ForeColor = Color.DarkSlateBlue, Font = callerFont })
            using (var label = new BootstrapLabel { BackColor = Color.LightYellow, ForeColor = Color.Crimson, Font = callerFont })
            {
                panel.SurfaceRole = BootstrapSurfaceRole.Surface;
                label.TextRole = BootstrapTextRole.Danger;
                BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
                Assert.Multiple((Action)(() =>
                {
                    Assert.That(panel.BackColor, Is.EqualTo(Color.MediumPurple));
                    Assert.That(panel.ForeColor, Is.EqualTo(Color.DarkSlateBlue));
                    Assert.That(label.BackColor, Is.EqualTo(Color.LightYellow));
                    Assert.That(label.ForeColor, Is.EqualTo(Color.Crimson));
                    Assert.That(panel.Font, Is.SameAs(callerFont));
                    Assert.That(label.Font, Is.SameAs(callerFont));
                }));
            }

            Assert.That(callerFont.SizeInPoints, Is.EqualTo(12f));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ExplicitAssignmentOfCurrentThemeColorStillRetainsCallerOwnership()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            var light = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            BootstrapThemeManager.CurrentTheme = light;
            using var panel = new BootstrapPanel { BackColor = light.Colors.Body, ForeColor = light.Colors.Text };
            using var label = new BootstrapLabel { ForeColor = light.Colors.Text };
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

            Assert.That(panel.BackColor, Is.EqualTo(light.Colors.Body));
            Assert.That(panel.ForeColor, Is.EqualTo(light.Colors.Text));
            Assert.That(label.ForeColor, Is.EqualTo(light.Colors.Text));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void AllRolesMapToCurrentThemeTokens()
    {
        using var panel = new BootstrapPanel();
        using var label = new BootstrapLabel();
        var colors = BootstrapThemeManager.CurrentTheme.Colors;
        Assert.That(panel.BackColor, Is.EqualTo(colors.Body));
        Assert.That(label.ForeColor, Is.EqualTo(colors.Text));
        panel.SurfaceRole = BootstrapSurfaceRole.Surface;
        Assert.That(panel.BackColor, Is.EqualTo(colors.Surface));
        panel.SurfaceRole = BootstrapSurfaceRole.Alternate;
        Assert.That(panel.BackColor, Is.EqualTo(colors.SurfaceSecondary));
        label.TextRole = BootstrapTextRole.Muted;
        Assert.That(label.ForeColor, Is.EqualTo(colors.MutedText));
        label.TextRole = BootstrapTextRole.Success;
        Assert.That(label.ForeColor, Is.EqualTo(colors.Success));
        label.TextRole = BootstrapTextRole.Warning;
        Assert.That(label.ForeColor, Is.EqualTo(colors.Warning));
        label.TextRole = BootstrapTextRole.Danger;
        Assert.That(label.ForeColor, Is.EqualTo(colors.Danger));
    }

    [Test]
    public void ThemeTypographyChangesOnlyUnownedFonts()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        using var callerFont = new Font("Segoe UI", 13f);
        try
        {
            using var panel = new BootstrapPanel();
            using var label = new BootstrapLabel { Font = callerFont };
            var basis = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            var typography = new BootstrapThemeTypography(
                new BootstrapFontToken("Segoe UI", 11f),
                basis.Typography.BodySmall,
                new BootstrapFontToken("Segoe UI", 10f, FontStyle.Bold),
                basis.Typography.HeadingSmall,
                basis.Typography.HeadingMedium);
            BootstrapThemeManager.CurrentTheme = new BootstrapTheme(basis.Mode, basis.Colors, basis.Metrics, typography);

            Assert.That(panel.Font.SizeInPoints, Is.EqualTo(11f));
            Assert.That(label.Font, Is.SameAs(callerFont));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void ParentFontChangeDoesNotBecomeAnExplicitOverride()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        using var parentFont = new Font("Segoe UI", 17f);
        try
        {
            using var parent = new Panel();
            using var panel = new BootstrapPanel();
            using var label = new BootstrapLabel();
            parent.Controls.Add(panel);
            parent.Controls.Add(label);
            parent.Font = parentFont;
            var basis = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            var typography = new BootstrapThemeTypography(
                new BootstrapFontToken("Segoe UI", 12f), basis.Typography.BodySmall,
                new BootstrapFontToken("Segoe UI", 10f), basis.Typography.HeadingSmall,
                basis.Typography.HeadingMedium);
            BootstrapThemeManager.CurrentTheme = new BootstrapTheme(basis.Mode, basis.Colors, basis.Metrics, typography);

            Assert.That(panel.Font.SizeInPoints, Is.EqualTo(12f));
            Assert.That(label.Font.SizeInPoints, Is.EqualTo(10f));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void RepeatedRoleAndThemeChangesRetainSemanticOwnership()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var panel = new BootstrapPanel();
            using var label = new BootstrapLabel();
            for (var i = 0; i < 20; i++)
            {
                BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(i % 2 == 0 ? BootstrapThemeMode.Dark : BootstrapThemeMode.Light);
                panel.SurfaceRole = i % 2 == 0 ? BootstrapSurfaceRole.Surface : BootstrapSurfaceRole.Alternate;
                label.TextRole = i % 2 == 0 ? BootstrapTextRole.Success : BootstrapTextRole.Warning;
                var colors = BootstrapThemeManager.CurrentTheme.Colors;
                Assert.That(panel.BackColor, Is.EqualTo(i % 2 == 0 ? colors.Surface : colors.SurfaceSecondary));
                Assert.That(label.ForeColor, Is.EqualTo(i % 2 == 0 ? colors.Success : colors.Warning));
            }
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }

    [Test]
    public void LabelPreservesWrappingMnemonicAndAccessibilityProperties()
    {
        using var label = new BootstrapLabel
        {
            AutoSize = false,
            Size = new Size(90, 48),
            Text = "&Long warning message that wraps",
            AccessibleName = "Warning message",
            AccessibleRole = AccessibleRole.Alert,
            UseMnemonic = true
        };

        Assert.Multiple((Action)(() =>
        {
            Assert.That(label.Size, Is.EqualTo(new Size(90, 48)));
            Assert.That(label.Text, Is.EqualTo("&Long warning message that wraps"));
            Assert.That(label.AccessibleName, Is.EqualTo("Warning message"));
            Assert.That(label.AccessibleRole, Is.EqualTo(AccessibleRole.Alert));
            Assert.That(label.UseMnemonic, Is.True);
        }));
    }

    [Test]
    public void DisposalStopsThemeUpdatesAndKeepsCallerFontAlive()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        using var callerFont = new Font("Segoe UI", 10f);
        var panel = new BootstrapPanel { Font = callerFont };
        var label = new BootstrapLabel { Font = callerFont };
        panel.Dispose();
        label.Dispose();
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            Assert.That(callerFont.SizeInPoints, Is.EqualTo(10f));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = original;
        }
    }
}
