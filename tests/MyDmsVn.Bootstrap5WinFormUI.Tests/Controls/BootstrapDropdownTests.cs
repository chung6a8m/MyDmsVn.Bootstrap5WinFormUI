using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
public sealed class BootstrapDropdownTests
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
    public void NativeDropDownCharacterizationPreservesAutoCloseAndLifecycle()
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
        menu.Close();
        Application.DoEvents();
        menu.Close();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(menu.Visible, Is.False);
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NativeMenuItemCharacterizationPreservesCheckedDisabledSeparatorAndNonTogglePolicy()
    {
        using var menu = new ToolStripDropDownMenu();
        var checkedItem = new ToolStripMenuItem("Checked")
        {
            Checked = true,
            CheckOnClick = false
        };
        var disabledItem = new ToolStripMenuItem("Disabled") { Enabled = false };
        var separator = new ToolStripSeparator();
        var clicks = 0;
        checkedItem.Click += (_, _) => clicks++;
        menu.Items.Add(checkedItem);
        menu.Items.Add(disabledItem);
        menu.Items.Add(separator);

        checkedItem.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(checkedItem.Checked, Is.True);
            Assert.That(checkedItem.CheckOnClick, Is.False);
            Assert.That(disabledItem.Enabled, Is.False);
            Assert.That(menu.Items[2], Is.TypeOf<ToolStripSeparator>());
            Assert.That(clicks, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NativeSubmenuCharacterizationPreservesHierarchyAndNestedLeafActivation()
    {
        using var menu = new ToolStripDropDownMenu();
        var parent = new ToolStripMenuItem("Parent");
        var leaf = new ToolStripMenuItem("Leaf");
        var clicks = 0;
        leaf.Click += (_, _) => clicks++;

        parent.DropDownItems.Add(leaf);
        menu.Items.Add(parent);
        leaf.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(menu.Items[0], Is.SameAs(parent));
            Assert.That(parent.DropDownItems.Count, Is.EqualTo(1));
            Assert.That(parent.DropDownItems[0], Is.SameAs(leaf));
            Assert.That(clicks, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NativeToolStripControlHostCharacterizationDisposesHostedControl()
    {
        var hostedControl = new DisposalTrackingControl();
        var host = new ToolStripControlHost(hostedControl);

        host.Dispose();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(hostedControl.IsDisposed, Is.True);
            Assert.That(hostedControl.DisposeCount, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NativeCompositeControlCharacterizationLetsBaseDisposeOwnChildDisposal()
    {
        var composite = new BaseOwnedCompositeControl();
        var child = composite.TrackedChild;

        composite.Dispose();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(child.IsDisposed, Is.True);
            Assert.That(child.DisposeCount, Is.EqualTo(1));
        }));
    }

    [Test]
    public void ItemModelDefaultsNormalizeTextAndRejectUndefinedKind()
    {
        var item = new BootstrapDropdownItem();
        item.Text = null!;
        var separator = new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.Kind, Is.EqualTo(BootstrapDropdownItemKind.Item));
            Assert.That(item.Text, Is.Empty);
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
        collection[0] = separator;
        Assert.That(collection.ToArray(), Is.EqualTo(new[] { separator, last }));
        Assert.Throws<ArgumentNullException>((Action)(() => collection.Add(null!)));
        Assert.Throws<ArgumentNullException>((Action)(() => collection[0] = null!));

        collection.Clear();
        Assert.That(collection, Is.Empty);
    }

    [Test]
    public void ItemModelExtendsKindsWithoutRenumberingExistingValues()
    {
        Assert.Multiple((Action)(() =>
        {
            Assert.That((int)BootstrapDropdownItemKind.Item, Is.Zero);
            Assert.That((int)BootstrapDropdownItemKind.Separator, Is.EqualTo(1));
            Assert.That(Enum.GetName(typeof(BootstrapDropdownItemKind), 2), Is.EqualTo("HostedControl"));
        }));

        var hosted = new BootstrapDropdownItem((BootstrapDropdownItemKind)2);
        Assert.That((int)hosted.Kind, Is.EqualTo(2));
    }

    [Test]
    public void ItemModelOwnsStableNestedCollectionThatRejectsNull()
    {
        var item = new BootstrapDropdownItem();
        var property = typeof(BootstrapDropdownItem).GetProperty("DropDownItems");

        Assert.That(property, Is.Not.Null);
        var first = property!.GetValue(item) as BootstrapDropdownItemCollection;
        var second = property.GetValue(item) as BootstrapDropdownItemCollection;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.SameAs(first));
            Assert.That(first, Is.Empty);
        }));
        Assert.Throws<ArgumentNullException>((Action)(() => first!.Add(null!)));
    }

    [Test]
    public void ItemModelHostedControlFactoryDefaultsToNullAndUsesHiddenDesignerMetadata()
    {
        var item = new BootstrapDropdownItem();
        var property = typeof(BootstrapDropdownItem).GetProperty("HostedControlFactory");

        Assert.That(property, Is.Not.Null);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(property!.PropertyType, Is.EqualTo(typeof(Func<Control>)));
            Assert.That(property.GetValue(item), Is.Null);
            Assert.That(property.GetCustomAttributes(typeof(BrowsableAttribute), inherit: true)
                .Cast<BrowsableAttribute>().Single().Browsable, Is.False);
            Assert.That(property.GetCustomAttributes(typeof(DesignerSerializationVisibilityAttribute), inherit: true)
                .Cast<DesignerSerializationVisibilityAttribute>().Single().Visibility,
                Is.EqualTo(DesignerSerializationVisibility.Hidden));
        }));

        Func<Control> factory = () => new TextBox();
        property!.SetValue(item, factory);
        Assert.That(property.GetValue(item), Is.SameAs(factory));
    }

    [Test]
    public void ItemRaiseClickUsesItemAsSender()
    {
        var item = new BootstrapDropdownItem();
        var clicks = 0;
        object? sender = null;
        item.Click += (actualSender, _) =>
        {
            clicks++;
            sender = actualSender;
        };

        item.RaiseClick();
        item.RaiseClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(clicks, Is.EqualTo(2));
            Assert.That(sender, Is.SameAs(item));
        }));
    }

    [Test]
    public void RendererPaletteUsesThemeTokensForEveryVariantAndThemeMode()
    {
        foreach (var mode in new[] { BootstrapThemeMode.Light, BootstrapThemeMode.Dark })
        {
            var colors = BootstrapThemeColors.CreateDefault(mode);
            foreach (var variant in Enum.GetValues(typeof(BootstrapVariant)).Cast<BootstrapVariant>())
            {
                var accent = BootstrapVariantColorResolver.Resolve(colors, variant);
                var normal = BootstrapDropdownRenderer.ResolvePalette(colors, variant, enabled: true, selected: false);
                var selected = BootstrapDropdownRenderer.ResolvePalette(colors, variant, enabled: true, selected: true);
                var disabled = BootstrapDropdownRenderer.ResolvePalette(colors, variant, enabled: false, selected: false);

                Assert.Multiple((Action)(() =>
                {
                    Assert.That(normal.Background, Is.EqualTo(colors.Surface));
                    Assert.That(normal.Foreground, Is.EqualTo(colors.Text));
                    Assert.That(normal.Border, Is.EqualTo(colors.Border));
                    Assert.That(normal.Accent, Is.EqualTo(accent));
                    Assert.That(selected.Background, Is.EqualTo(ColorUtil.Blend(accent, colors.Surface, 0.12f)));
                    Assert.That(disabled.Foreground, Is.EqualTo(colors.MutedText));
                    Assert.That(disabled.Accent, Is.EqualTo(colors.Disabled));
                }));
            }
        }
    }

    [Test]
    public void RendererMetricsScaleAcrossSupportedDpiMatrixAndRejectInvalidInputs()
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
            BootstrapDropdownRenderer.ResolvePalette(
                BootstrapThemeColors.CreateDefault(BootstrapThemeMode.Light),
                (BootstrapVariant)999,
                true,
                false)));
        Assert.Throws<ArgumentNullException>((Action)(() =>
            BootstrapDropdownRenderer.ResolveMetrics(null!, 96)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            BootstrapDropdownRenderer.ResolveMetrics(BootstrapThemeMetrics.Default, 0)));
    }

    [Test]
    public void DropdownDefaultsValidationAndMissingTargetContractAreStable()
    {
        using var dropdown = new BootstrapDropdown();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(dropdown.Target, Is.Null);
            Assert.That(dropdown.Items, Is.SameAs(dropdown.Items));
            Assert.That(dropdown.Items, Is.Empty);
            Assert.That(dropdown.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(dropdown.MinimumWidth, Is.Zero);
        }));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => dropdown.Variant = (BootstrapVariant)999));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => dropdown.MinimumWidth = -1));
        Assert.Throws<InvalidOperationException>((Action)(() => dropdown.Show()));
        Assert.DoesNotThrow((Action)(() => dropdown.Close()));
    }

    [Test]
    public void DropdownTreeValidationRejectsSeparatorChildrenAndFactory()
    {
        var separatorWithChild = new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator);
        separatorWithChild.DropDownItems.Add(new BootstrapDropdownItem());
        var separatorWithFactory = new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator)
        {
            HostedControlFactory = () => new TextBox()
        };

        Assert.Throws<InvalidOperationException>((Action)(() =>
            ValidateItemTreeViaInternalSeam(CollectionOf(separatorWithChild))));
        Assert.Throws<InvalidOperationException>((Action)(() =>
            ValidateItemTreeViaInternalSeam(CollectionOf(separatorWithFactory))));
    }

    [Test]
    public void DropdownTreeValidationRejectsMalformedHostedControlItems()
    {
        var missingFactory = new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl);
        var withChildren = new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = () => new TextBox()
        };
        withChildren.DropDownItems.Add(new BootstrapDropdownItem());

        Assert.Throws<InvalidOperationException>((Action)(() =>
            ValidateItemTreeViaInternalSeam(CollectionOf(missingFactory))));
        Assert.Throws<InvalidOperationException>((Action)(() =>
            ValidateItemTreeViaInternalSeam(CollectionOf(withChildren))));
    }

    [Test]
    public void DropdownTreeValidationRejectsFactoryOnNormalItem()
    {
        var item = new BootstrapDropdownItem
        {
            HostedControlFactory = () => new TextBox()
        };

        Assert.Throws<InvalidOperationException>((Action)(() =>
            ValidateItemTreeViaInternalSeam(CollectionOf(item))));
    }

    [Test]
    public void DropdownTreeValidationRejectsDuplicateInstancesAndCycles()
    {
        var duplicate = new BootstrapDropdownItem();
        var duplicates = new BootstrapDropdownItemCollection { duplicate, duplicate };

        var directCycle = new BootstrapDropdownItem();
        directCycle.DropDownItems.Add(directCycle);

        var indirectRoot = new BootstrapDropdownItem();
        var indirectChild = new BootstrapDropdownItem();
        indirectRoot.DropDownItems.Add(indirectChild);
        indirectChild.DropDownItems.Add(indirectRoot);

        Assert.Throws<InvalidOperationException>((Action)(() =>
            ValidateItemTreeViaInternalSeam(duplicates)));
        Assert.Throws<InvalidOperationException>((Action)(() =>
            ValidateItemTreeViaInternalSeam(CollectionOf(directCycle))));
        Assert.Throws<InvalidOperationException>((Action)(() =>
            ValidateItemTreeViaInternalSeam(CollectionOf(indirectRoot))));
    }

    [Test]
    public void DropdownTreeValidationAcceptsValidMixedDepthTree()
    {
        var root = new BootstrapDropdownItem { Text = "Root" };
        var child = new BootstrapDropdownItem { Text = "Child" };
        child.DropDownItems.Add(new BootstrapDropdownItem { Text = "Leaf" });
        child.DropDownItems.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator));
        child.DropDownItems.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = () => new TextBox()
        });
        root.DropDownItems.Add(child);

        Assert.DoesNotThrow((Action)(() =>
            ValidateItemTreeViaInternalSeam(CollectionOf(root))));
    }

    [Test]
    public void DropdownShowValidatesBeforeReplacingClosedNativeSnapshot()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        dropdown.Items.Add(new BootstrapDropdownItem { Text = "Valid" });
        dropdown.Show();
        Application.DoEvents();
        dropdown.Close();
        Application.DoEvents();
        var nativeDropDown = GetNativeDropDown(dropdown);
        var originalNativeItem = nativeDropDown.Items[0];

        dropdown.Items[0].HostedControlFactory = () => new TextBox();

        Assert.Throws<InvalidOperationException>((Action)(() => dropdown.Show()));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(nativeDropDown.Visible, Is.False);
            Assert.That(nativeDropDown.Items.Count, Is.EqualTo(1));
            Assert.That(nativeDropDown.Items[0], Is.SameAs(originalNativeItem));
            Assert.That(originalNativeItem.IsDisposed, Is.False);
        }));
    }

    [Test]
    public void DropdownActivationDispatchesOnlyEnabledCommandsAndNeverTogglesChecked()
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
    public void DropdownRecursiveSnapshotTreatsParentsAsNavigationAndActivatesNestedLeavesOnce()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        var parent = new BootstrapDropdownItem { Text = "Parent" };
        var leaf = new BootstrapDropdownItem { Text = "Leaf", Checked = true };
        var disabled = new BootstrapDropdownItem { Text = "Disabled", Enabled = false };
        var parentClicks = 0;
        var leafClicks = 0;
        var disabledClicks = 0;
        parent.Click += (_, _) => parentClicks++;
        leaf.Click += (_, _) => leafClicks++;
        disabled.Click += (_, _) => disabledClicks++;
        parent.DropDownItems.Add(leaf);
        parent.DropDownItems.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.Separator));
        parent.DropDownItems.Add(disabled);
        dropdown.Items.Add(parent);

        dropdown.Show();
        Application.DoEvents();
        var nativeRoot = GetNativeDropDown(dropdown);
        var nativeParent = (ToolStripMenuItem)nativeRoot.Items[0];
        var nativeLeaf = (ToolStripMenuItem)nativeParent.DropDownItems[0];
        var nativeDisabled = (ToolStripMenuItem)nativeParent.DropDownItems[2];

        nativeParent.PerformClick();
        nativeDisabled.PerformClick();
        nativeLeaf.PerformClick();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(BootstrapDropdown.CanActivate(parent), Is.False);
            Assert.That(BootstrapDropdown.CanActivate(leaf), Is.True);
            Assert.That(nativeParent.Tag, Is.SameAs(parent));
            Assert.That(nativeLeaf.Tag, Is.SameAs(leaf));
            Assert.That(nativeParent.DropDownItems[1], Is.TypeOf<ToolStripSeparator>());
            Assert.That(parentClicks, Is.Zero);
            Assert.That(leafClicks, Is.EqualTo(1));
            Assert.That(disabledClicks, Is.Zero);
            Assert.That(leaf.Checked, Is.True);
        }));
    }

    [Test]
    public void DropdownHostedControlFactoryBuildsFreshOwnedControlPerEffectiveOpening()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        var dropdown = new BootstrapDropdown { Target = button };
        var created = new System.Collections.Generic.List<DisposalTrackingControl>();
        dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = () =>
            {
                var control = new DisposalTrackingControl();
                created.Add(control);
                return control;
            }
        });

        dropdown.Show();
        Application.DoEvents();
        var firstHost = (ToolStripControlHost)GetNativeDropDown(dropdown).Items[0];
        dropdown.Close();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(created.Count, Is.EqualTo(1));
            Assert.That(firstHost.Control, Is.SameAs(created[0]));
            Assert.That(created[0].IsDisposed, Is.False);
        }));

        dropdown.Show();
        Application.DoEvents();
        dropdown.Close();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(created.Count, Is.EqualTo(2));
            Assert.That(created[0].IsDisposed, Is.True);
            Assert.That(created[0].DisposeCount, Is.EqualTo(1));
            Assert.That(created[1].IsDisposed, Is.False);
        }));

        dropdown.Dispose();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(created[1].IsDisposed, Is.True);
            Assert.That(created[1].DisposeCount, Is.EqualTo(1));
        }));
    }

    [Test]
    public void DropdownDisabledHostedItemDisablesBothHostAndCreatedControl()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            Enabled = false,
            HostedControlFactory = () => new TextBox()
        });

        dropdown.Show();
        Application.DoEvents();
        var host = (ToolStripControlHost)GetNativeDropDown(dropdown).Items[0];

        Assert.Multiple((Action)(() =>
        {
            Assert.That(host.Enabled, Is.False);
            Assert.That(host.Control.Enabled, Is.False);
        }));
    }

    [Test]
    public void DropdownFactoryFailureDisposesPartialSnapshotAndDoesNotOpen()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        var first = new DisposalTrackingControl();
        dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = () => first
        });
        dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = () => null!
        });
        var opened = 0;
        dropdown.Opened += (_, _) => opened++;

        Assert.Throws<InvalidOperationException>((Action)(() => dropdown.Show()));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(opened, Is.Zero);
            Assert.That(first.IsDisposed, Is.True);
            Assert.That(first.DisposeCount, Is.EqualTo(1));
            Assert.That(GetNativeDropDown(dropdown).Items, Is.Empty);
        }));
    }

    [Test]
    public void DropdownFactoryRejectsAlreadyDisposedControlBeforeOpening()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        var disposed = new DisposalTrackingControl();
        disposed.Dispose();
        dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = () => disposed
        });
        var opened = 0;
        dropdown.Opened += (_, _) => opened++;

        Assert.Throws<InvalidOperationException>((Action)(() => dropdown.Show()));
        Assert.That(opened, Is.Zero);
    }

    [Test]
    public void DropdownRecursivePresentationComputesMarginsIndependentlyPerLevel()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        var parent = new BootstrapDropdownItem { Text = "Parent" };
        parent.DropDownItems.Add(new BootstrapDropdownItem
        {
            Text = "Child icon",
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus)
        });
        dropdown.Items.Add(parent);

        dropdown.Show();
        Application.DoEvents();
        var root = GetNativeDropDown(dropdown);
        var parentNative = (ToolStripMenuItem)root.Items[0];
        var childLevel = (ToolStripDropDownMenu)parentNative.DropDown;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(root.ShowImageMargin, Is.False);
            Assert.That(root.ShowCheckMargin, Is.False);
            Assert.That(childLevel.ShowImageMargin, Is.True);
            Assert.That(childLevel.ShowCheckMargin, Is.False);
            Assert.That(childLevel.Renderer, Is.SameAs(root.Renderer));
        }));

        dropdown.Close();
        Application.DoEvents();
        dropdown.Items.Clear();
        var rootIconParent = new BootstrapDropdownItem
        {
            Text = "Root icon",
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus)
        };
        rootIconParent.DropDownItems.Add(new BootstrapDropdownItem { Text = "Plain child" });
        dropdown.Items.Add(rootIconParent);

        dropdown.Show();
        Application.DoEvents();
        root = GetNativeDropDown(dropdown);
        childLevel = (ToolStripDropDownMenu)((ToolStripMenuItem)root.Items[0]).DropDown;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(root.ShowImageMargin, Is.True);
            Assert.That(childLevel.ShowImageMargin, Is.False);
            Assert.That(childLevel.ShowCheckMargin, Is.False);
        }));
    }

    [Test]
    public void DropdownThemeRefreshesRecursiveIconsWithoutRecreatingHostedControls()
    {
        var button = new BootstrapButton { Text = "Menu" };
        var renderer = new RecordingIconRenderer();
        button.IconRenderer = renderer;
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        var hostedFactoryCalls = 0;
        var rootModel = new BootstrapDropdownItem
        {
            Text = "Root",
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus)
        };
        var childModel = new BootstrapDropdownItem
        {
            Text = "Child",
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus)
        };
        var grandchildModel = new BootstrapDropdownItem
        {
            Text = "Grandchild",
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus)
        };
        childModel.DropDownItems.Add(grandchildModel);
        childModel.DropDownItems.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = () =>
            {
                hostedFactoryCalls++;
                return new TextBox { Text = "Caller state" };
            }
        });
        rootModel.DropDownItems.Add(childModel);
        dropdown.Items.Add(rootModel);

        dropdown.Show();
        Application.DoEvents();
        var nativeRoot = GetNativeDropDown(dropdown);
        var nativeRootItem = (ToolStripMenuItem)nativeRoot.Items[0];
        var nativeChildItem = (ToolStripMenuItem)nativeRootItem.DropDownItems[0];
        var nativeGrandchildItem = (ToolStripMenuItem)nativeChildItem.DropDownItems[0];
        var nativeHost = (ToolStripControlHost)nativeChildItem.DropDownItems[1];
        var hostedControl = nativeHost.Control;
        var initialImages = new[]
        {
            nativeRootItem.Image,
            nativeChildItem.Image,
            nativeGrandchildItem.Image
        };

        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(renderer.RenderCount, Is.EqualTo(6));
            Assert.That(hostedFactoryCalls, Is.EqualTo(1));
            Assert.That(nativeHost.Control, Is.SameAs(hostedControl));
            Assert.That(((TextBox)hostedControl).Text, Is.EqualTo("Caller state"));
            Assert.That(nativeRootItem.Image, Is.Not.Null.And.Not.SameAs(initialImages[0]));
            Assert.That(nativeChildItem.Image, Is.Not.Null.And.Not.SameAs(initialImages[1]));
            Assert.That(nativeGrandchildItem.Image, Is.Not.Null.And.Not.SameAs(initialImages[2]));
            Assert.That(((ToolStripDropDownMenu)nativeRootItem.DropDown).Renderer, Is.SameAs(nativeRoot.Renderer));
            Assert.That(((ToolStripDropDownMenu)nativeChildItem.DropDown).Renderer, Is.SameAs(nativeRoot.Renderer));
        }));
    }

    [Test]
    public void DropdownShowNoOpsForEmptyDisabledAndLoadingTargets()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        var opened = 0;
        dropdown.Opened += (_, _) => opened++;

        dropdown.Show();
        Application.DoEvents();
        dropdown.Items.Add(new BootstrapDropdownItem { Text = "Action" });

        button.Enabled = false;
        dropdown.Show();
        Application.DoEvents();
        button.Enabled = true;
        button.Loading = true;
        dropdown.Show();
        Application.DoEvents();

        Assert.That(opened, Is.Zero);
    }

    [Test]
    public void DropdownForwardsNativeLifecycleThroughShowCloseAndTargetClickToggle()
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
    public void AppClickedCloseSuppressesSameTurnTargetClickAndExpiresOnNextMessageTurn()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        dropdown.Items.Add(new BootstrapDropdownItem { Text = "Action" });
        var native = GetNativeDropDown(dropdown);
        var opened = 0;
        var closed = 0;
        dropdown.Opened += (_, _) => opened++;
        dropdown.Closed += (_, _) => closed++;

        dropdown.Show();
        Application.DoEvents();
        native.Close(ToolStripDropDownCloseReason.AppClicked);
        button.PerformClick();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Visible, Is.False);
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.EqualTo(1));
        }));

        Application.DoEvents();
        button.PerformClick();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Visible, Is.True);
            Assert.That(opened, Is.EqualTo(2));
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void DropdownInternalAnchorOpensWithoutChangingPublicTargetAndForwardsLifecycle()
    {
        var presentationSource = new BootstrapButton { Text = "Presentation" };
        var anchor = new Panel { Size = new Size(240, 48) };
        using var form = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(200, 200),
            Size = new Size(500, 300)
        };
        presentationSource.Location = new Point(24, 24);
        anchor.Location = new Point(24, 80);
        form.Controls.Add(presentationSource);
        form.Controls.Add(anchor);
        form.Show();
        Application.DoEvents();
        using var dropdown = new BootstrapDropdown();
        dropdown.Items.Add(new BootstrapDropdownItem { Text = "Action" });
        var opened = 0;
        var closed = 0;
        dropdown.Opened += (_, _) => opened++;
        dropdown.Closed += (_, _) => closed++;

        InvokeShowFrom(dropdown, presentationSource, anchor, new Point(0, anchor.Height));
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(dropdown.Target, Is.Null);
            Assert.That(opened, Is.EqualTo(1));
        }));

        dropdown.Close();
        Application.DoEvents();
        Assert.That(closed, Is.EqualTo(1));
    }

    [Test]
    public void DropdownGenericInternalAnchorUsesPlainControlPresentationAndLeavesTargetUnchanged()
    {
        var presentationSource = new TextBox { Text = "Presentation", Size = new Size(180, 28) };
        var anchor = new Panel { Size = new Size(240, 48) };
        using var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(200, 200), Size = new Size(500, 300) };
        form.Controls.Add(presentationSource);
        form.Controls.Add(anchor);
        form.Show();
        Application.DoEvents();
        using var dropdown = new BootstrapDropdown();
        dropdown.Items.Add(new BootstrapDropdownItem(BootstrapDropdownItemKind.HostedControl)
        {
            HostedControlFactory = () => new TextBox { Text = "Hosted" }
        });
        var opened = 0;
        var closed = 0;
        dropdown.Opened += (_, _) => opened++;
        dropdown.Closed += (_, _) => closed++;

        InvokeShowFrom(dropdown, presentationSource, BootstrapIconRenderer.CreateDefault(), anchor, new Point(0, anchor.Height));
        Application.DoEvents();
        dropdown.Close();
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(dropdown.Target, Is.Null);
            Assert.That(opened, Is.EqualTo(1));
            Assert.That(closed, Is.EqualTo(1));
        }));
    }

    [Test]
    public void DropdownGenericSourceValidatesArgumentsAndClearsActivePresentationAfterClose()
    {
        using var source = new TextBox();
        using var anchor = new Panel();
        using var dropdown = new BootstrapDropdown();
        var renderer = new RecordingIconRenderer();
        dropdown.Items.Add(new BootstrapDropdownItem { Text = "Action", Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus) });
        Assert.Throws<ArgumentNullException>((Action)(() => InvokeShowFrom(dropdown, source, null!, anchor, Point.Empty)));
        Assert.Throws<ObjectDisposedException>((Action)(() => { source.Dispose(); InvokeShowFrom(dropdown, source, renderer, anchor, Point.Empty); }));

        using var liveSource = new TextBox { Size = new Size(120, 24) };
        using var form = CreateHost(liveSource);
        InvokeShowFrom(dropdown, liveSource, renderer, liveSource, new Point(0, liveSource.Height));
        Application.DoEvents();
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        Application.DoEvents();
        dropdown.Close(); Application.DoEvents();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(renderer.RenderCount, Is.GreaterThan(1));
            Assert.That(GetPrivateField(dropdown, "_activePresentationSource"), Is.Null);
            Assert.That(GetPrivateField(dropdown, "_activeIconRenderer"), Is.Null);
            Assert.That(dropdown.Target, Is.Null);
        }));
    }

    [Test]
    public void DropdownGenericSourceRejectsNullAndDisposedAnchorsAndNoOpsWhenDisabledOrEmpty()
    {
        using var source = new TextBox(); using var anchor = new Panel(); using var dropdown = new BootstrapDropdown();
        var renderer = BootstrapIconRenderer.CreateDefault();
        Assert.Throws<ArgumentNullException>((Action)(() => InvokeShowFrom(dropdown, null!, renderer, anchor, Point.Empty)));
        Assert.Throws<ArgumentNullException>((Action)(() => InvokeShowFrom(dropdown, source, renderer, null!, Point.Empty)));
        anchor.Dispose(); Assert.Throws<ObjectDisposedException>((Action)(() => InvokeShowFrom(dropdown, source, renderer, anchor, Point.Empty)));
        using var usableAnchor = new Panel(); source.Enabled = false; InvokeShowFrom(dropdown, source, renderer, usableAnchor, Point.Empty);
        Assert.Multiple((Action)(() => { Assert.That(GetNativeDropDown(dropdown).Visible, Is.False); Assert.That(dropdown.Target, Is.Null); }));
    }

    [Test]
    public void DropdownGenericPresentationPropagatesSourceFontAndMinimumWidth()
    {
        using var source = new TextBox { Size = new Size(180, 28), Font = new Font("Arial", 14f) };
        using var form = CreateHost(source);
        using var dropdown = new BootstrapDropdown { MinimumWidth = 180 };
        dropdown.Items.Add(new BootstrapDropdownItem { Text = "Action" });
        InvokeShowFrom(dropdown, source, BootstrapIconRenderer.CreateDefault(), source, new Point(0, source.Height)); Application.DoEvents();
        var native = GetNativeDropDown(dropdown);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.Font.FontFamily.Name, Is.EqualTo(source.Font.FontFamily.Name));
            Assert.That(native.Font.SizeInPoints, Is.EqualTo(source.Font.SizeInPoints));
            Assert.That(native.MinimumSize.Width, Is.EqualTo(BootstrapDropdown.ResolveMinimumWidth(180, source.DeviceDpi)));
        }));
    }

    [Test]
    public void DropdownActivePresentationSourceDrivesLiveMinimumWidthAndThemeRefresh()
    {
        var presentationSource = new BootstrapButton { Text = "Presentation" };
        var renderer = new RecordingIconRenderer();
        presentationSource.IconRenderer = renderer;
        var anchor = new Panel { Size = new Size(240, 48) };
        using var form = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(200, 200),
            Size = new Size(500, 300)
        };
        form.Controls.Add(presentationSource);
        form.Controls.Add(anchor);
        form.Show();
        Application.DoEvents();
        using var dropdown = new BootstrapDropdown();
        dropdown.Items.Add(new BootstrapDropdownItem
        {
            Text = "Action",
            Icon = IconDescriptor.Framework(FrameworkIconGlyph.Plus)
        });

        InvokeShowFrom(dropdown, presentationSource, anchor, new Point(0, anchor.Height));
        Application.DoEvents();
        dropdown.MinimumWidth = 220;
        var expectedWidth = BootstrapDropdown.ResolveMinimumWidth(220, presentationSource.DeviceDpi);
        var rendersBeforeTheme = renderer.RenderCount;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(dropdown.Target, Is.Null);
            Assert.That(GetNativeDropDown(dropdown).MinimumSize.Width, Is.EqualTo(expectedWidth));
            Assert.That(renderer.RenderCount, Is.GreaterThan(rendersBeforeTheme));
        }));
    }

    [Test]
    public void DropdownClassicTargetStillUpdatesMinimumWidthWhileOpen()
    {
        var button = new BootstrapButton { Text = "Menu" };
        using var form = CreateHost(button);
        using var dropdown = new BootstrapDropdown { Target = button };
        dropdown.Items.Add(new BootstrapDropdownItem { Text = "Action" });
        dropdown.Show();
        Application.DoEvents();

        dropdown.MinimumWidth = 210;

        Assert.That(
            GetNativeDropDown(dropdown).MinimumSize.Width,
            Is.EqualTo(BootstrapDropdown.ResolveMinimumWidth(210, button.DeviceDpi)));
    }

    [Test]
    public void DropdownRebuildsCurrentModelSnapshotOnEveryEffectiveOpening()
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
        dropdown.Opened += (_, _) => opened++;

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
            Assert.That(second.Text, Is.EqualTo("B changed"));
            Assert.That(second.Checked, Is.True);
            Assert.That(second.Enabled, Is.False);
        }));
    }

    [Test]
    public void DropdownTargetReplacementAndTargetDisposalDetachWithoutOwnershipTransfer()
    {
        var first = new BootstrapButton { Text = "First" };
        var second = new BootstrapButton { Text = "Second" };
        using var form = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(100, 100),
            Size = new Size(500, 300)
        };
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
        Assert.That(dropdown.Target, Is.Null);
        dropdown.Target = first;
        dropdown.Show();
        Application.DoEvents();
        dropdown.Close();
        Application.DoEvents();
        Assert.That(first.IsDisposed, Is.False);
    }

    [Test]
    public void DropdownUsesTargetIconRendererAndLogicalMinimumWidthScaling()
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
    public void OpenDropdownRefreshesOwnedIconsOnThemeChangeAndUnsubscribesOnDispose()
    {
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

        dropdown.Show();
        Application.DoEvents();
        var before = renderer.RenderCount;
        BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
        Application.DoEvents();
        Assert.That(renderer.RenderCount, Is.GreaterThan(before));

        dropdown.Close();
        Application.DoEvents();
        dropdown.Dispose();
        Assert.DoesNotThrow((Action)(() =>
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light)));
    }

    [Test]
    public void DropdownRepeatedOpenCloseAndSnapshotRebuildIsStable()
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

        for (var index = 0; index < 50; index++)
        {
            dropdown.Items.Clear();
            dropdown.Items.Add(new BootstrapDropdownItem
            {
                Text = "Action " + index,
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

    private static BootstrapDropdownItemCollection CollectionOf(BootstrapDropdownItem item)
    {
        return new BootstrapDropdownItemCollection { item };
    }

    private static void ValidateItemTreeViaInternalSeam(BootstrapDropdownItemCollection items)
    {
        var method = typeof(BootstrapDropdown).GetMethod(
            "ValidateItemTree",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "BootstrapDropdown must expose the planned internal validation seam.");

        try
        {
            method!.Invoke(null, new object[] { items });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    private static ToolStripDropDownMenu GetNativeDropDown(BootstrapDropdown dropdown)
    {
        var field = typeof(BootstrapDropdown).GetField("_dropDown", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (ToolStripDropDownMenu)field!.GetValue(dropdown)!;
    }

    private static void InvokeShowFrom(
        BootstrapDropdown dropdown,
        BootstrapButton presentationSource,
        Control anchor,
        Point location)
    {
        var method = typeof(BootstrapDropdown).GetMethod(
            "ShowFrom",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(BootstrapButton), typeof(Control), typeof(Point) },
            null);
        Assert.That(method, Is.Not.Null, "BootstrapDropdown must expose the planned internal anchored-show path.");

        try
        {
            method!.Invoke(dropdown, new object[] { presentationSource, anchor, location });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    private static object? GetPrivateField(BootstrapDropdown dropdown, string name)
    {
        var field = typeof(BootstrapDropdown).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return field!.GetValue(dropdown);
    }

    private static void InvokeShowFrom(
        BootstrapDropdown dropdown,
        Control presentationSource,
        IIconRenderer iconRenderer,
        Control anchor,
        Point location)
    {
        var method = typeof(BootstrapDropdown).GetMethod(
            "ShowFrom",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(Control), typeof(IIconRenderer), typeof(Control), typeof(Point) },
            null);
        Assert.That(method, Is.Not.Null, "BootstrapDropdown must expose the generic internal anchored-show path.");

        try
        {
            method!.Invoke(dropdown, new object[] { presentationSource, iconRenderer, anchor, location });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    private sealed class RecordingIconRenderer : IIconRenderer
    {
        public int RenderCount { get; private set; }
        public Rectangle LastBounds { get; private set; }

        public bool TryRender(Graphics graphics, IconDescriptor descriptor, Rectangle bounds, Color color)
        {
            RenderCount++;
            LastBounds = bounds;
            using var brush = new SolidBrush(color);
            graphics.FillRectangle(brush, bounds);
            return true;
        }
    }

    private sealed class BaseOwnedCompositeControl : Control
    {
        public BaseOwnedCompositeControl()
        {
            TrackedChild = new DisposalTrackingControl();
            Controls.Add(TrackedChild);
        }

        public DisposalTrackingControl TrackedChild { get; }
    }

    private sealed class DisposalTrackingControl : Control
    {
        public int DisposeCount { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                DisposeCount++;
            }

            base.Dispose(disposing);
        }
    }
}
