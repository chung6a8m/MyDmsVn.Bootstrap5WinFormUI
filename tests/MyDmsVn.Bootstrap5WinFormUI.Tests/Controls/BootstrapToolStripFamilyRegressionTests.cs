using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapToolStripFamilyRegressionTests
{
    private BootstrapTheme _originalTheme = null!;

    [SetUp]
    public void SetUp() => _originalTheme = BootstrapThemeManager.CurrentTheme;

    [TearDown]
    public void TearDown() => BootstrapThemeManager.CurrentTheme = _originalTheme;

    [Test]
    public void ThemeRoundTripKeepsNativeItemsAndSharedRendererInstances()
    {
        using var tool = new BootstrapToolStrip();
        using var menu = new BootstrapMenuStrip();
        using var context = new BootstrapContextMenuStrip();
        using var status = new BootstrapStatusStrip();
        var strips = new ToolStrip[] { tool, menu, context, status };
        foreach (var strip in strips) strip.Items.Add("Item");
        var items = strips.Select(strip => strip.Items[0]).ToArray();
        var renderers = strips.Select(strip => strip.Renderer).ToArray();

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);

        for (var index = 0; index < strips.Length; index++)
        {
            Assert.That(strips[index].Items[0], Is.SameAs(items[index]));
            Assert.That(strips[index].Renderer, Is.SameAs(renderers[index]));
        }
    }

    [Test]
    public void EveryControlHonorsCallerRendererAndFontOptOut()
    {
        using var tool = new BootstrapToolStrip();
        using var menu = new BootstrapMenuStrip();
        using var context = new BootstrapContextMenuStrip();
        using var status = new BootstrapStatusStrip();
        var strips = new ToolStrip[] { tool, menu, context, status };
        using var callerFont = new Font("Tahoma", 12f);
        var sentinels = strips.Select(_ => new SentinelRenderer()).ToArray();
        for (var index = 0; index < strips.Length; index++)
        {
            strips[index].Font = callerFont;
            strips[index].Renderer = sentinels[index];
        }

        tool.Variant = BootstrapVariant.Danger;
        menu.Variant = BootstrapVariant.Warning;
        context.Variant = BootstrapVariant.Info;
        status.Variant = BootstrapVariant.Success;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

        for (var index = 0; index < strips.Length; index++)
        {
            Assert.That(strips[index].Font, Is.SameAs(callerFont));
            Assert.That(strips[index].Renderer, Is.SameAs(sentinels[index]));
        }
        Assert.DoesNotThrow((Action)(() => _ = callerFont.Height));
    }

    [Test]
    public void DynamicItemsNestedMenusAndHandleRecreationRemainStable()
    {
        using var host = new WinFormsMessageLoopTestHost();
        host.Run(() =>
        {
            using var form = new Form { Size = new Size(420, 220) };
            using var menu = new BootstrapMenuStrip();
            var root = new ToolStripMenuItem("Root");
            menu.Items.Add(root);
            form.Controls.Add(menu);
            form.Show();
            root.DropDownItems.Add("First");
            root.ShowDropDown();
            root.DropDownItems.Add("Second");
            root.HideDropDown();
            menu.Items.Remove(root);
            menu.Items.Add(root);
            root.ShowDropDown();
            root.HideDropDown();
            form.Controls.Remove(menu);
            menu.CreateControl();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(root.DropDownItems.Count, Is.EqualTo(2));
                Assert.That(root.Owner, Is.SameAs(menu));
                Assert.That(menu.Renderer, Is.TypeOf<BootstrapToolStripRenderer>());
            }));
            form.Close();
        });
    }

    [Test]
    public void ThemeChangesPreserveStatusLabelBorderContract()
    {
        using var status = new BootstrapStatusStrip();
        var label = new ToolStripStatusLabel("Border")
        {
            BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Bottom,
            BorderStyle = Border3DStyle.RaisedOuter
        };
        status.Items.Add(label);

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        status.Variant = BootstrapVariant.Warning;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(label.BorderSides, Is.EqualTo(ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Bottom));
            Assert.That(label.BorderStyle, Is.EqualTo(Border3DStyle.RaisedOuter));
        }));
    }

    private sealed class SentinelRenderer : ToolStripRenderer
    {
    }
}
