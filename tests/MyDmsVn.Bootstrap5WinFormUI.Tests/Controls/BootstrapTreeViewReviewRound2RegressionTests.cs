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
[NonParallelizable]
public sealed class BootstrapTreeViewReviewRound2RegressionTests
{
    [Test]
    public void Constructor_PreservesNativeHideSelectionDefault()
    {
        using var native = new TreeView();
        using var treeView = new BootstrapTreeView();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(native.HideSelection, Is.True, "Plain WinForms TreeView should provide the native baseline.");
            Assert.That(treeView.HideSelection, Is.EqualTo(native.HideSelection),
                "BootstrapTreeView must not silently change the inherited HideSelection default.");
        }));
    }

    [Test]
    public void HotTracking_ControlsFrameworkHotPresentation()
    {
        using var treeView = new ProbeBootstrapTreeView
        {
            Size = new Size(320, 120),
            ItemHeight = 24,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false,
            HotTracking = false,
        };
        var node = new TreeNode("Hot tracking node");
        treeView.Nodes.Add(node);
        _ = treeView.Handle;
        treeView.RaiseMouseMove(GetLabelPoint(node));

        using var hotTrackingOff = RenderNode(treeView, node);
+        treeView.HotTracking = true;
+        using var hotTrackingOn = RenderNode(treeView, node);
+        treeView.HotTracking = false;
+        using var hotTrackingDisabledAgain = RenderNode(treeView, node);
+
+        var hover = BootstrapThemeManager.CurrentTheme.Colors.Hover;
+        var row = GetRowBounds(treeView, node);
+        Assert.Multiple((Action)(() =>
+        {
+            Assert.That(ContainsColor(hotTrackingOff, row, hover), Is.False,
+                "HotTracking=false must not receive framework hot presentation.");
+            Assert.That(ContainsColor(hotTrackingOn, row, hover), Is.True,
+                "HotTracking=true must preserve native hot-tracking presentation under OwnerDrawAll.");
+            Assert.That(ContainsColor(hotTrackingDisabledAgain, row, hover), Is.False,
+                "Turning HotTracking off must immediately suppress framework hot presentation.");
+        }));
+    }
+
+    private static Point GetLabelPoint(TreeNode node)
+    {
+        return new Point(
+            node.Bounds.Left + Math.Max(1, node.Bounds.Width / 2),
+            node.Bounds.Top + Math.Max(1, node.Bounds.Height / 2));
+    }
+
+    private static Rectangle GetRowBounds(TreeView treeView, TreeNode node)
+    {
+        return Rectangle.Intersect(
+            treeView.ClientRectangle,
+            new Rectangle(treeView.ClientRectangle.Left, node.Bounds.Top, treeView.ClientRectangle.Width, treeView.ItemHeight));
+    }
+
+    private static Bitmap RenderNode(BootstrapTreeView treeView, TreeNode node)
+    {
+        var row = GetRowBounds(treeView, node);
+        var bitmap = new Bitmap(Math.Max(1, treeView.ClientSize.Width), Math.Max(1, treeView.ClientSize.Height));
+        using var graphics = Graphics.FromImage(bitmap);
+        graphics.Clear(Color.Magenta);
+        treeView.RenderNodeForTesting(graphics, node, row, node.Bounds, (TreeNodeStates)0);
+        return bitmap;
+    }
+
+    private static bool ContainsColor(Bitmap bitmap, Rectangle bounds, Color expected)
+    {
+        for (var y = Math.Max(0, bounds.Top); y < Math.Min(bitmap.Height, bounds.Bottom); y++)
+        {
+            for (var x = Math.Max(0, bounds.Left); x < Math.Min(bitmap.Width, bounds.Right); x++)
+            {
+                var pixel = bitmap.GetPixel(x, y);
+                if (Math.Abs(pixel.R - expected.R) <= 4 &&
+                    Math.Abs(pixel.G - expected.G) <= 4 &&
+                    Math.Abs(pixel.B - expected.B) <= 4)
+                {
+                    return true;
+                }
+            }
+        }
+
+        return false;
+    }
+
+    private sealed class ProbeBootstrapTreeView : BootstrapTreeView
+    {
+        internal void RaiseMouseMove(Point point)
+        {
+            base.OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0));
+        }
+    }
+}
