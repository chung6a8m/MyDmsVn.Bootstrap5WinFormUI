using System;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Theme;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapThemeManagerTests
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

    [Test]
    public void SettingCurrentThemeRaisesThemeChangedWithOldAndNewTheme()
    {
        var oldTheme = BootstrapThemeManager.CurrentTheme;
        var newTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark, reducedMotion: true);
        BootstrapThemeChangedEventArgs? observed = null;
        EventHandler<BootstrapThemeChangedEventArgs> handler = (_, args) => observed = args;

        BootstrapThemeManager.ThemeChanged += handler;
        try
        {
            BootstrapThemeManager.CurrentTheme = newTheme;
        }
        finally
        {
            BootstrapThemeManager.ThemeChanged -= handler;
        }

        Assert.Multiple((TestDelegate)(() =>
        {
            Assert.That(observed, Is.Not.Null);
            Assert.That(observed!.OldTheme, Is.SameAs(oldTheme));
            Assert.That(observed.NewTheme, Is.SameAs(newTheme));
            Assert.That(BootstrapThemeManager.CurrentTheme, Is.SameAs(newTheme));
        }));
    }

    [Test]
    public void SettingSameThemeInstanceDoesNotRaiseThemeChanged()
    {
        var theme = BootstrapThemeManager.CurrentTheme;
        var eventCount = 0;
        EventHandler<BootstrapThemeChangedEventArgs> handler = (_, _) => eventCount++;

        BootstrapThemeManager.ThemeChanged += handler;
        try
        {
            BootstrapThemeManager.CurrentTheme = theme;
        }
        finally
        {
            BootstrapThemeManager.ThemeChanged -= handler;
        }

        Assert.That(eventCount, Is.Zero);
    }

    [Test]
    public void SettingNullThemeIsRejected()
    {
        TestDelegate action = () => BootstrapThemeManager.CurrentTheme = null!;

        Assert.That(action, Throws.TypeOf<ArgumentNullException>());
    }
}
