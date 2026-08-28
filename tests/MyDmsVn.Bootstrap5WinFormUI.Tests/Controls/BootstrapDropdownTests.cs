using System;
using System.Drawing;
using System.Linq;
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
public sealed class BootstrapDropdownTests
{
    [Test]
    public void NativeDropDownUsesAutoCloseAndForwardsOneOpenCloseTransition()
    {
        var button = new Button { Text = "Open" };
        using var form = CreateHost(button);
        using var menu = new ToolStripDropDownMenu();
        menu.Items.Add(new ToolStripMenuItem("Action"));

        var opened = 0;
        var closed = 0;
        menu.Opened += (_, _) => opened++;
        menu.Closed += (_, _) => closed++;

        Assert.That(menu.AutoClose, Is.True);

        menu.Show(button, new Point(0, button.Height));
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(menu.Visible, Is.True);
            Assert.That(opened, Is.EqualTo(1));
        }));

        menu.Close();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(menu.Visible, Is.False);
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NativeMenuItemsPreserveCheckedEnabledSeparatorAndNonTogglePolicy()
    {
        using var menu = new ToolStripDropDownMenu();
        var checkedItem = new ToolStripMenuItem("Checked")
        {
            Checked = true,
            Enabled = true,
            CheckOnClick = false
        };
        var disabledItem = new ToolStripMenuItem("Disabled") { Enabled = false };
        var separator = new ToolStripSeparator();
        var clickCount = 0;
        checkedItem.Click += (_, _) => clickCount++;

        menu.Items.Add(checkedItem);
        menu.Items.Add(disabledItem);
        menu.Items.Add(separator);
        checkedItem.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(menu.Items[0], Is.SameAs(checkedItem));
            Assert.That(menu.Items[1], Is.SameAs(disabledItem));
            Assert.That(menu.Items[2], Is.SameAs(separator));
            Assert.That(checkedItem.Checked, Is.True);
            Assert.That(checkedItem.CheckOnClick, Is.False);
            Assert.That(disabledItem.Enabled, Is.False);
            Assert.That(clickCount, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NativeCloseIsIdempotent()
    {
        var button = new Button { Text = "Open" };
        using var form = CreateHost(button);
        using var menu = new ToolStripDropDownMenu();
        menu.Items.Add(new ToolStripMenuItem("Action"));

        var closed = 0;
        menu.Closed += (_, _) => closed++;
        menu.Show(button, new Point(0, button.Height));
        Application.DoEvents();
        menu.Close();
        Application.DoEvents();
        menu.Close();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(menu.Visible, Is.False);
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ItemDefaultsNormalizeTextAndValidateKind()
    {
        var item = new BootstrapDropdownItem();
        var separator = new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator);
        item.Text = null!;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.Kind, Is.EqualTo(BootstrapDropdownItemKind.Item));
            Assert.That(item.Text, Is.EqualTo(string.Empty));
            Assert.That(item.Icon, Is.Null);
            Assert.That(item.Enabled, Is.True);
            Assert.That(item.Checked, Is.False);
            Assert.That(item.Tag, Is.Null);
            Assert.That(separator.Kind, Is.EqualTo(BootstrapDropdownItemKind.Separator));
        }));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            _ = new BootstrapDropdownItem((BootstrapDropdownItemKind)999)));
    }

    [Test]
    public void ItemCollectionPreservesOrderAndRejectsNull()
    {
        var collection = new BootstrapDropdownItemCollection();
        var first = new BootstrapDropdownItem { Text = "First" };
        var separator = new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator);
        var last = new BootstrapDropdownItem { Text = "Last" };

        collection.Add(first);
        collection.Add(separator);
        collection.Add(last);
        Assert.That(collection.ToArray(), Is.EqualTo(new[] { first, separator, last }));

        collection.RemoveAt(1);
        Assert.That(collection.ToArray(), Is.EqualTo(new[] { first, last }));

        collection[0] = separator;
        Assert.That(collection.ToArray(), Is.EqualTo(new[] { separator, last }));

        Assert.Throws<ArgumentNullException>((Action)(() => collection.Add(null!)));
        Assert.Throws<ArgumentNullException>((Action)(() => collection[0] = null!));

        collection.Clear();
        Assert.That(collection, Is.Empty);
    }

    [Test]
    public void ItemRaiseClickUsesItemAsSender()
    {
        var item = new BootstrapDropdownItem();
        var count = 0;
        object? sender = null;
        item.Click += (actualSender, _) =>
        {
            count++;
            sender = actualSender;
        };

        item.RaiseClick();
        item.RaiseClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(sender, Is.SameAs(item));
        }));
    }

    [Test]
    public void RendererPaletteUsesThemeTokensForEveryVariant()
    {
        foreach (var mode in new[] { BootstrapThemeMode.Light, BootstrapThemeMode.Dark })
        {
            var colors = BootstrapThemeColors.CreateDefault(mode);
            foreach (var variant in Enum.GetValues(typeof(BootstrapVariant)).Cast<BootstrapVariant>())
            {
                var variantColor = BootstrapVariantColorResolver.Resolve(colors, variant);
                var normal = BootstrapDropdownRenderer.ResolvePalette(colors, variant, enabled: true, selected: false);
                var selected = BootstrapDropdownRenderer.ResolvePalette(colors, variant, enabled: true, selected: true);
                var disabled = BootstrapDropdownRenderer.ResolvePalette(colors, variant, enabled: false, selected: false);

                Assert.Multiple((Action)(() =>
                {
                    Assert.That(normal.Background, Is.EqualTo(colors.Surface));
                    Assert.That(normal.Foreground, Is.EqualTo(colors.Text));
                    Assert.That(normal.Border, Is.EqualTo(colors.Border));
                    Assert.That(normal.Accent, Is.EqualTo(variantColor));
                    Assert.That(selected.Background, Is.EqualTo(ColorUtil.Blend(variantColor, colors.Surface, 0.12f)));
                    Assert.That(selected.Foreground, Is.EqualTo(colors.Text));
                    Assert.That(disabled.Foreground, Is.EqualTo(colors.MutedText));
                    Assert.That(disabled.Accent, Is.EqualTo(colors.Disabled));
                }));
            }
        }
    }

    [Test]
    public void RendererMetricsScaleAndValidateInputs()
    {
        foreach (var dpi in new[] { 96, 120, 144, 168, 192 })
        {
            var metrics = BootstrapDropdownRenderer.ResolveMetrics(BootstrapThemeMetrics.Default, dpi);
            Assert.Multiple((Action)(() =>
            {
                Assert.That(metrics.ItemHorizontalPadding, Is.EqualTo(DpiScaler.Scale(BootstrapThemeMetrics.Default.SpacingSM, dpi)));
                Assert.That(metrics.ItemVerticalPadding, Is.EqualTo(DpiScaler.Scale(BootstrapThemeMetrics.Default.SpacingXS, dpi)));
                Assert.That(metrics.ImageSize, Is.EqualTo(DpiScaler.Scale(BootstrapThemeMetrics.Default.SpacingLG, dpi)));
                Assert.That(metrics.SeparatorInset, Is.EqualTo(DpiScaler.Scale(BootstrapThemeMetrics.Default.SpacingSM, dpi)));
                Assert.That(metrics.BorderWidth, Is.EqualTo(DpiScaler.Scale((float)BootstrapThemeMetrics.Default.BorderWidth, dpi)));
            }));
        }

        Assert.Throws<ArgumentNullException>((Action)(() =>
            BootstrapDropdownRenderer.ResolvePalette(null!, BootstrapVariant.Primary, true, false)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            BootstrapDropdownRenderer.ResolvePalette(BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light), (BootstrapVariant)999, true, false)));
        Assert.Throws<ArgumentNullException>((Action)(() => BootstrapDropdownRenderer.ResolveMetrics(null!, 96)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapDropdownRenderer.ResolveMetrics(BootstrapThemeMetrics.Default, 0)));
    }

    [Test]
    public void DropdownDefaultsValidationAndMissingTargetContract()
    {
        using var dropdown = new BootstrapDropdown();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(dropdown.Target, Is.Null);
            Assert.That(dropdown.Items, Is.SameAs(dropdown.Items));
            Assert.That(dropdown.Items, Is.Empty);
            Assert.That(dropdown.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(dropdown.MinimumWidth, Is.EqualTo(0));
        }));

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => dropdown.Variant = (BootstrapVariant)999));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => dropdown.MinimumWidth = -1));
        Assert.Throws<InvalidOperationException>((Action)(() => dropdown.Show()));
        Assert.DoesNotThrow(() => dropdown.Close());
    }

    [Test]
    public void DropdownActivationRejectsDisabledAndSeparatorWithoutTogglingChecked()
    {
        using var dropdown = new BootstrapDropdown();
        var enabled = new BootstrapDropdownItem { Checked = true };
        var disabled = new BootstrapDropdownItem { Enabled = false };
        var separator = new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator);
        var enabledClicks = 0;
        var otherClicks = 0;
        enabled.Click += (_, _) => enabledClicks++;
        disabled.Click += (_, _) => otherClicks++;
        separator.Click += (_, _) => otherClicks++;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapDropdown.CanActivate(enabled), Is.True);
            Assert.That(BootstrapDropdown.CanActivate(disabled), Is.False);
            Assert.That(BootstrapDropdown.CanActivate(separator), Is.False);
        }));

        dropdown.ActivateItem(enabled);
        dropdown.ActivateItem(disabled);
        dropdown.ActivateItem(separator);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(enabledClicks, Is.EqualTo(1));
            Assert.That(otherClicks, Is.Zero);
            Assert.That(enabled.Checked, Is.True);
        }));
        Assert.Throws<ArgumentNullException>((Action)(() => dropdown.ActivateItem(null!)));
    }

    [Test]
    public void DropdownNoOpsForEmptyDisabledAndLoadingTargets()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        var opened = 0;
        dropdown.Opened += (_, _) => opened++;

        dropdown.Show();
        Application.DoEvents();
        Assert.That(opened, Is.Zero);

        dropdown.Items.Add(new BootstrapDropdownItem { Text = "Action" });
        button.Enabled = false;
        dropdown.Show();
        Application.DoEvents();
        Assert.That(opened, Is.Zero);

        button.Enabled = true;
        button.Loading = true;
        dropdown.Show();
        Application.DoEvents();
        Assert.That(opened, Is.Zero);
    }

    [Test]
    public void DropdownForwardsNativeLifecycleThroughShowAndTargetClick()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        dropdown.Items.Add(new BootstrapDropdownItem { Text = "Action" });

        var opened = 0;
        var closed = 0;
        object? openedSender = null;
        object? closedSender = null;
        dropdown.Opened += (sender, _) => { opened++; openedSender = sender; };
        dropdown.Closed += (sender, _) => { closed++; closedSender = sender; };

        dropdown.Show();
        Application.DoEvents();
        dropdown.Close();
        Application.DoEvents();
        button.PerformClick();
        Application.DoEvents();
        button.PerformClick();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(opened, Is.EqualTo(2));
            Assert.That(closed, Is.EqualTo(2));
            Assert.That(openedSender, Is.SameAs(dropdown));
            Assert.That(closedSender, Is.SameAs(dropdown));
        }));
    }

    [Test]
    public void DropdownRebuildsSnapshotOnEachOpening()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        var first = new BootstrapDropdownItem { Text = "A" };
        var second = new BootstrapDropdownItem { Text = "B" };
        dropdown.Items.Add(first);
        dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator));
        dropdown.Items.Add(second);

        var opened = 0;
        var closed = 0;
        dropdown.Opened += (_, _) => opened++;
        dropdown.Closed += (_, _) => closed++;

        dropdown.Show();
        Application.DoEvents();
        dropdown.Close();
        Application.DoEvents();

        dropdown.Items.Remove(first);
        second.Text = "B changed";
        second.Checked = true;
        second.Enabled = false;
        dropdown.Items.Add(new BootstrapDropdownItem { Text = "C" });

        dropdown.Show();
        Application.DoEvents();
        dropdown.Close();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(opened, Is.EqualTo(2));
            Assert.That(closed, Is.EqualTo(2));
            Assert.That(second.Checked, Is.True);
            Assert.That(second.Enabled, Is.False);
        }));
    }

    [Test]
    public void DropdownTargetReplacementAndDisposalDetachOldTargetWithoutOwningIt()
    {
        var first = new BootstrapButton { Text = "First" };
        var second = new BootstrapButton { Text = "Second" };
        using var form = new Form { Size = new Size(500, 300), StartPosition = FormStartPosition.Manual, Location = new Point(100, 100) };
        first.Location = new Point(24, 24);
        second.Location = new Point(160, 24);
        form.Controls.Add(first);
        form.Controls.Add(second);
        form.Show();
        Application.DoEvents();

        using var dropdown = new BootstrapDropdown { Target = first };
        dropdown.Items.Add(new BootstrapDropdownItem { Text = "Action" });
        var opened = 0;
        var closed = 0;
        dropdown.Opened += (_, _) => opened++;
        dropdown.Closed += (_, _) => closed++;

        dropdown.Show();
        Application.DoEvents();
        dropdown.Target = second;
        Application.DoEvents();
        first.PerformClick();
        Application.DoEvents();
        second.PerformClick();
        Application.DoEvents();
        dropdown.Close();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(opened, Is.EqualTo(2));
            Assert.That(closed, Is.EqualTo(2));
            Assert.That(first.IsDisposed, Is.False);
            Assert.That(second.IsDisposed, Is.False);
        }));

        second.Dispose();
        dropdown.Target = first;
        dropdown.Show();
        Application.DoEvents();
        dropdown.Close();
        Application.DoEvents();
        Assert.That(first.IsDisposed, Is.False);
    }

    [Test]
    public void DropdownUsesTargetIconRendererAndScalesMinimumWidth()
    {
        var button = new BootstrapButton { Text = "Menu" };
        var renderer = new RecordingIconRenderer();
        button.IconRenderer = renderer;
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button, MinimumWidth = 160 };
        dropdown.Items.Add(new BootstrapDropdownItem
        {
            Text = "Create",
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus)
        });

        dropdown.Show();
        Application.DoEvents();
        dropdown.Close();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(renderer.RenderCount, Is.GreaterThan(0));
            Assert.That(renderer.LastBounds.Width, Is.GreaterThan(0));
            Assert.That(renderer.LastBounds.Width, Is.EqualTo(renderer.LastBounds.Height));
            Assert.That(BootstrapDropdown.ResolveMinimumWidth(0, 96), Is.Zero);
            Assert.That(BootstrapDropdown.ResolveMinimumWidth(160, 96), Is.EqualTo(160));
            Assert.That(BootstrapDropdown.ResolveMinimumWidth(160, 144), Is.EqualTo(DpiScaler.Scale(160, 144)));
        }));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapDropdown.ResolveMinimumWidth(-1, 96)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => BootstrapDropdown.ResolveMinimumWidth(160, 0)));
    }

    [Test]
    public void OpenDropdownRefreshesIconsWhenThemeChangesAndUnsubscribesOnDispose()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var button = new BootstrapButton { Text = "Menu" };
        var renderer = new RecordingIconRenderer();
        button.IconRenderer = renderer;
        using var form = CreateHost(button);
        var dropdown = new BootstrapDropdown { Target = button };
        dropdown.Items.Add(new BootstrapDropdownItem
        {
            Text = "Create",
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus)
        });

        try
        {
            dropdown.Show();
            Application.DoEvents();
            var beforeThemeChange = renderer.RenderCount;

            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            Application.DoEvents();
            Assert.That(renderer.RenderCount, Is.GreaterThan(beforeThemeChange));

            dropdown.Close();
            Application.DoEvents();
            dropdown.Dispose();
            Assert.DoesNotThrow(() => BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light));
        }
        finally
        {
            dropdown.Dispose();
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void DropdownRepeatedOpenCloseRebuildIsStable()
    {
        var button = new BootstrapButton { Text = "Menu" };
        var renderer = new RecordingIconRenderer();
        button.IconRenderer = renderer;
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        var opened = 0;
        var closed = 0;
        dropdown.Opened += (_, _) => opened++;
        dropdown.Closed += (_, _) => closed++;

        for (var i = 0; i < 50; i++)
        {
            dropdown.Items.Clear();
            dropdown.Items.Add(new BootstrapDropdownItem
            {
                Text = "Action " + i,
                Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus)
            });
            dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator));
            dropdown.Items.Add(new BootstrapDropdownItem { Text = "Disabled", Enabled = false });
            dropdown.Show();
            Application.DoEvents();
            dropdown.Close();
            Application.DoEvents();
        }

        Assert.Multiple((Action)(() =>
        {
            Assert.That(opened, Is.EqualTo(50));
            Assert.That(closed, Is.EqualTo(50));
            Assert.That(renderer.RenderCount, Is.EqualTo(50));
        }));
    }

    private static Form CreateHost(Control target)
    {
        var form = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(100, 100),
            Size = new Size(500, 300)
        };
        target.Location = new Point(24, 24);
        form.Controls.Add(target);
        form.Show();
        Application.DoEvents();
        return form;
    }

    private sealed class RecordingIconRenderer : IIconRenderer
    {
        public int RenderCount { get; private set; }
        public Rectangle LastBounds { get; private set; }
        public Color LastColor { get; private set; }

        public bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color)
        {
            RenderCount++;
            LastBounds = bounds;
            LastColor = color;
            using var brush = new SolidBrush(color);
            graphics.FillRectangle(brush, bounds);
            return true;
        }
    }
}
