using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Hardening;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class Phase15HardeningTests
{
    private BootstrapTheme? _originalTheme;

    [SetUp]
    public void SetUp()
    {
        _originalTheme = BootstrapThemeManager.CurrentTheme;
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalTheme is not null)
        {
            BootstrapThemeManager.CurrentTheme = _originalTheme;
        }
    }

    [TestCase(96, 32, 4)]
    [TestCase(120, 40, 5)]
    [TestCase(144, 48, 6)]
    [TestCase(168, 56, 7)]
    [TestCase(192, 64, 8)]
    public void FullDpiMatrixScalesSharedGeometryConsistently(int dpi, int expectedWidth, int expectedInset)
    {
        var size = DpiScaler.Scale(new Size(32, 16), dpi);
        var padding = DpiScaler.Scale(new Padding(4, 8, 12, 16), dpi);
        var rectangle = DpiScaler.Scale(new Rectangle(4, 8, 32, 16), dpi);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(size.Width, Is.EqualTo(expectedWidth));
            Assert.That(size.Height, Is.EqualTo(expectedWidth / 2));
            Assert.That(padding.Left, Is.EqualTo(expectedInset));
            Assert.That(rectangle.X, Is.EqualTo(expectedInset));
            Assert.That(rectangle.Width, Is.EqualTo(expectedWidth));
        }));
    }

    [Test]
    public void ThemeSwitchStressKeepsRepresentativeControlsUsableAndDisposalDetachesSubscriptions()
    {
        var baselineSubscriptions = GetThemeSubscriptionCount();
        Control[] controls =
        {
            new BootstrapButton(),
            new BootstrapSpinner(),
            new BootstrapTextBox(),
            new BootstrapCard(),
            new BootstrapCollapse(),
            new BootstrapAccordion(),
            new BootstrapProgressBar(),
            new BootstrapSidebar(),
            new BootstrapDataGridView()
        };

        try
        {
            for (var index = 0; index < 50; index++)
            {
                var mode = index % 2 == 0 ? BootstrapThemeMode.Dark : BootstrapThemeMode.Light;
                BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(mode, reducedMotion: index % 3 == 0);
            }

            Assert.That(controls.All(control => !control.IsDisposed), Is.True);
        }
        finally
        {
            foreach (var control in controls)
            {
                control.Dispose();
            }
        }

        Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
        Assert.DoesNotThrow((Action)(() => BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark)));
    }

    [Test]
    public void RapidCollapseAndSidebarTogglesConvergeUnderReducedMotion()
    {
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light, reducedMotion: true);

        using var collapse = new BootstrapCollapse
        {
            ExpandedHeightMode = BootstrapCollapseHeightMode.Fixed,
            ExpandedHeight = 120
        };
        using var sidebar = new BootstrapSidebar();

        for (var index = 0; index < 101; index++)
        {
            collapse.Toggle();
            sidebar.Toggle();
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(collapse.Expanded, Is.False);
            Assert.That(collapse.AnimationProgress, Is.Zero);
            Assert.That(collapse.IsAnimating, Is.False);
            Assert.That(sidebar.Expanded, Is.False);
        }));
    }

    [Test]
    public void CollapseControlLifecycleHooksIgnoreNullEventPayloads()
    {
        using var collapse = new CollapseControlEventProbe();

        Assert.Multiple((Action)(() =>
        {
            Assert.DoesNotThrow((Action)(() => collapse.RaiseControlAdded(null)));
            Assert.DoesNotThrow((Action)(() => collapse.RaiseControlRemoved(null)));
        }));
    }

    [Test]
    public void CoreAssemblyDoesNotReferenceOptionalIconPackages()
    {
        var referenceNames = typeof(BootstrapTheme).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(referenceNames, Does.Not.Contain("FontAwesome.Sharp"));
            Assert.That(referenceNames, Does.Not.Contain("Svg"));
            Assert.That(referenceNames, Does.Not.Contain("SkiaSharp"));
        }));
    }

    [Test]
    public void PublicControlApiDoesNotExposeKnownPrototypeAliases()
    {
        var assembly = typeof(BootstrapButton).Assembly;
        var publicControlTypes = assembly.GetExportedTypes()
            .Where(type => string.Equals(type.Namespace, "MyDmsVn.Bootstrap5WinFormUI.Controls", StringComparison.Ordinal))
            .ToArray();
        var bannedMemberNames = new[] { "AnimationTime", "TransitionDuration", "IsLoading", "IsExpanded", "Busy", "BusyText" };
        var offenders = publicControlTypes
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(member => bannedMemberNames.Contains(member.Name, StringComparer.Ordinal))
                .Select(member => type.Name + "." + member.Name))
            .ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Theme.AppTheme"), Is.Null);
            Assert.That(offenders, Is.Empty, string.Join(", ", offenders));
        }));
    }

    private static int GetThemeSubscriptionCount()
    {
        var eventField = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(eventField, Is.Not.Null, "ThemeChanged must remain an ordinary static event so lifetime can be audited.");
        var handler = eventField!.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private sealed class CollapseControlEventProbe : BootstrapCollapse
    {
        public void RaiseControlAdded(Control? control)
        {
            base.OnControlAdded(new ControlEventArgs(control!));
        }

        public void RaiseControlRemoved(Control? control)
        {
            base.OnControlRemoved(new ControlEventArgs(control!));
        }
    }
}
