using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapListViewTests
{
    private sealed class TestBootstrapListView : BootstrapListView
    {
        public bool DoubleBufferedForTest => DoubleBuffered;

        public void RecreateHandleForTest() => RecreateHandle();
    }

    [Test]
    public void DefaultsMatchNativeBackedContract()
    {
        using var list = new TestBootstrapListView();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list, Is.InstanceOf<ListView>());
            Assert.That(list.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(list.Striped, Is.False);
            Assert.That(list.HoverHighlight, Is.True);
            Assert.That(list.OwnerDraw, Is.True);
            Assert.That(list.DoubleBufferedForTest, Is.True);
        }));
    }

    [Test]
    public void V1DeclaresOnlyBootstrapAppearanceProperties()
    {
        var declared = typeof(BootstrapListView)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .ToArray();

        Assert.That(declared, Is.EquivalentTo(new[]
        {
            nameof(BootstrapListView.Variant),
            nameof(BootstrapListView.Striped),
            nameof(BootstrapListView.HoverHighlight)
        }));
    }

    [Test]
    public void AppearanceChangesPreserveCallerOwnedNativeState()
    {
        using var images = new ImageList();
        using var image = new Bitmap(16, 16);
        images.Images.Add("item", image);

        using var list = new BootstrapListView
        {
            CheckBoxes = true,
            SmallImageList = images,
            View = View.Details
        };
        var column = new ColumnHeader { Text = "Name", Width = 160 };
        var group = new ListViewGroup("Group");
        var item = new ListViewItem("Item", group)
        {
            Checked = true,
            ImageKey = "item",
            Selected = true
        };
        list.Columns.Add(column);
        list.Groups.Add(group);
        list.Items.Add(item);

        list.Variant = BootstrapVariant.Success;
        list.Striped = true;
        list.HoverHighlight = false;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(list.Items[0], Is.SameAs(item));
            Assert.That(list.Columns[0], Is.SameAs(column));
            Assert.That(list.Groups[0], Is.SameAs(group));
            Assert.That(list.SmallImageList, Is.SameAs(images));
            Assert.That(item.Group, Is.SameAs(group));
            Assert.That(item.Checked, Is.True);
            Assert.That(item.Selected, Is.True);
            Assert.That(item.ImageKey, Is.EqualTo("item"));
        }));
    }
}
