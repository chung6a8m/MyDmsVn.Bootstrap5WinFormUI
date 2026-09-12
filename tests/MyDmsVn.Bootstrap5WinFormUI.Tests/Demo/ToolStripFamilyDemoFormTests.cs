using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
[NonParallelizable]
public sealed class ToolStripFamilyDemoFormTests
{
    [Test]
    public void DemoConstructsAllFourNativeBackedControlsAndDisposesRepeatedly()
    {
        for (var index = 0; index < 3; index++)
        {
            using var form = new ToolStripFamilyDemoForm();
            Assert.That(GetField<BootstrapMenuStrip>(form, "_menuStrip"), Is.Not.Null);
            Assert.That(GetField<BootstrapToolStrip>(form, "_toolStrip"), Is.Not.Null);
            Assert.That(GetField<BootstrapContextMenuStrip>(form, "_contextMenu"), Is.Not.Null);
            Assert.That(GetField<BootstrapStatusStrip>(form, "_statusStrip"), Is.Not.Null);
        }
    }

    [Test]
    public void DemoControlsTrackRuntimeThemeSwitch()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var form = new ToolStripFamilyDemoForm();
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

            Assert.That(GetField<BootstrapMenuStrip>(form, "_menuStrip").Renderer, Is.TypeOf<BootstrapToolStripRenderer>());
            Assert.That(GetField<BootstrapStatusStrip>(form, "_statusStrip").Renderer, Is.TypeOf<BootstrapToolStripRenderer>());
        }
        finally { BootstrapThemeManager.CurrentTheme = original; }
    }

    [Test]
    public void IntegratedNavigationRegistersMenusAndToolStripsPage()
    {
        using var form = new MainForm();
        var navigation = GetField<BootstrapSidebar>(form, "_navigation");
        var item = navigation.Items.Single(candidate => candidate.Text == "Menus / ToolStrips");
        var definition = item.Tag!;
        var description = (string)definition.GetType().GetProperty("Description")!.GetValue(definition)!;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(description, Does.Contain("MenuStrip"));
            Assert.That(description, Does.Contain("ContextMenuStrip"));
            Assert.That(description, Does.Contain("ToolStrip"));
            Assert.That(description, Does.Contain("StatusStrip"));
            Assert.That(description, Does.Contain("shortcuts"));
            Assert.That(description, Does.Contain("overflow"));
            Assert.That(description, Does.Contain("native behavior"));
        }));
    }

    private static T GetField<T>(object owner, string name) where T : class
    {
        var field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return (T)field!.GetValue(owner)!;
    }
}
