using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapTreeViewTests
{
    [Test]
    public void Constructor_PreservesNativeTreeViewContractAndBootstrapDefaults()
    {
        using var treeView = new BootstrapTreeView();

        Assert.That(treeView, Is.InstanceOf<TreeView>());
        Assert.That(treeView.BorderStyle, Is.EqualTo(BorderStyle.None));
        Assert.That(treeView.DrawMode, Is.EqualTo(TreeViewDrawMode.OwnerDrawAll));
        Assert.That(treeView.HideSelection, Is.False);
        Assert.That(treeView.Variant, Is.EqualTo(BootstrapVariant.Primary));
    }

    [Test]
    public void NativeMembers_RemainInheritedWithoutShadowWrappers()
    {
        foreach (var propertyName in new[]
                 {
                     nameof(TreeView.Nodes),
                     nameof(TreeView.SelectedNode),
                     nameof(TreeView.CheckBoxes),
                     nameof(TreeView.ImageList),
                     nameof(TreeView.LabelEdit),
                 })
        {
            var declaredMember = typeof(BootstrapTreeView).GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            Assert.That(declaredMember, Is.Null, $"{propertyName} must remain inherited from TreeView.");
        }

        using var treeView = new BootstrapTreeView();
        using var imageList = new ImageList();
        var root = new TreeNode("Root");

        treeView.Nodes.Add(root);
        treeView.SelectedNode = root;
        treeView.CheckBoxes = true;
        treeView.ImageList = imageList;
        treeView.LabelEdit = true;

        Assert.That(treeView.Nodes[0], Is.SameAs(root));
        Assert.That(treeView.SelectedNode, Is.SameAs(root));
        Assert.That(treeView.CheckBoxes, Is.True);
        Assert.That(treeView.ImageList, Is.SameAs(imageList));
        Assert.That(treeView.LabelEdit, Is.True);
    }

    [Test]
    public void NativePresentationProperties_RoundTrip()
    {
        using var treeView = new BootstrapTreeView
        {
            FullRowSelect = true,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            Indent = 28,
            ItemHeight = 24,
        };

        Assert.That(treeView.FullRowSelect, Is.True);
        Assert.That(treeView.ShowLines, Is.False);
        Assert.That(treeView.ShowPlusMinus, Is.False);
        Assert.That(treeView.ShowRootLines, Is.False);
        Assert.That(treeView.Indent, Is.EqualTo(28));
        Assert.That(treeView.ItemHeight, Is.EqualTo(24));
    }
}
