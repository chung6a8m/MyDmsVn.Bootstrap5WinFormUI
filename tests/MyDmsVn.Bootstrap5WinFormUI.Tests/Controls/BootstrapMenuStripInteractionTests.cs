using System;
using System.Drawing;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Tests.Infrastructure;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[NonParallelizable]
public sealed class BootstrapMenuStripInteractionTests
{
    [Test]
    public void NativeShortcutCheckDisabledAndNestedBehaviorRemainAuthoritative()
    {
        using var host = new WinFormsMessageLoopTestHost();
        host.Run(() =>
        {
            using var form = new Form();
            using var menu = new ShortcutMenuStrip();
            var root = new ToolStripMenuItem("&File");
            var save = new ToolStripMenuItem("&Save") { ShortcutKeys = Keys.Control | Keys.S, CheckOnClick = true };
            var disabled = new ToolStripMenuItem("Disabled") { Enabled = false };
            var nested = new ToolStripMenuItem("Nested");
            var child = new ToolStripMenuItem("Child");
            nested.DropDownItems.Add(child);
            root.DropDownItems.AddRange(new ToolStripItem[] { save, disabled, new ToolStripSeparator(), nested });
            menu.Items.Add(root);
            form.MainMenuStrip = menu;
            form.Controls.Add(menu);
            var editor = new TextBox { Location = new Point(20, 60) };
            form.Controls.Add(editor);
            var clicks = 0;
            save.Click += (_, _) => clicks++;
            form.Show();
            form.Activate();
            editor.Focus();
            Application.DoEvents();

            Assert.That(menu.DispatchKey(Keys.Control | Keys.S), Is.True);
            Assert.Multiple((Action)(() =>
            {
                Assert.That(clicks, Is.EqualTo(1));
                Assert.That(save.Checked, Is.True);
                Assert.That(disabled.Enabled, Is.False);
                Assert.That(nested.DropDownItems[0], Is.SameAs(child));
            }));
            form.Close();
        });
    }

    [Test]
    public void OpenNestedMenusUseSharedRendererAndRefreshWithoutRecreation()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var host = new WinFormsMessageLoopTestHost();
            host.Run(() =>
            {
                using var form = new Form { Size = new Size(400, 200) };
                using var menu = new BootstrapMenuStrip();
                var root = new ToolStripMenuItem("Root");
                var nested = new ToolStripMenuItem("Nested");
                nested.DropDownItems.Add("Child");
                root.DropDownItems.Add(nested);
                menu.Items.Add(root);
                form.Controls.Add(menu);
                form.Show();
                root.ShowDropDown();
                nested.ShowDropDown();
                var rootDropDown = root.DropDown;
                var nestedDropDown = nested.DropDown;

                menu.Variant = BootstrapVariant.Warning;
                BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

                Assert.Multiple((Action)(() =>
                {
                    Assert.That(root.DropDown, Is.SameAs(rootDropDown));
                    Assert.That(nested.DropDown, Is.SameAs(nestedDropDown));
                    Assert.That(rootDropDown.Renderer, Is.SameAs(menu.Renderer));
                    Assert.That(nestedDropDown.Renderer, Is.SameAs(menu.Renderer));
                }));
                root.HideDropDown();
                form.Close();
            });
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void ExplicitRootAndDescendantRenderersAreNeverOverwritten()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var menu = new BootstrapMenuStrip();
            var root = new ToolStripMenuItem("Root");
            root.DropDownItems.Add("Child");
            menu.Items.Add(root);
            var rootRenderer = new SentinelRenderer();
            var childRenderer = new SentinelRenderer();
            menu.Renderer = rootRenderer;
            root.DropDown.Renderer = childRenderer;

            menu.Variant = BootstrapVariant.Info;
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(menu.Renderer, Is.SameAs(rootRenderer));
                Assert.That(root.DropDown.Renderer, Is.Not.TypeOf<BootstrapToolStripRenderer>());
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    private sealed class ShortcutMenuStrip : BootstrapMenuStrip
    {
        public bool DispatchKey(Keys keys)
        {
            var message = new Message();
            return ProcessCmdKey(ref message, keys);
        }
    }

    private sealed class SentinelRenderer : ToolStripRenderer
    {
    }
}
