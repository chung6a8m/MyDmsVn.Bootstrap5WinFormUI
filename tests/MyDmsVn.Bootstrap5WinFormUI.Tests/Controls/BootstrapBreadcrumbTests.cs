using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class BootstrapBreadcrumbTests
{
    [Test]
    public void ZeroOneTwoAndThreeItemsCreateTheExpectedNativeComposition()
    {
        using var breadcrumb = new BootstrapBreadcrumb();
        Assert.That(breadcrumb.Controls, Is.Empty);

        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Home"));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(breadcrumb.Controls.OfType<LinkLabel>(), Is.Empty);
            Assert.That(breadcrumb.Controls.OfType<Label>().Single().Text, Is.EqualTo("Home"));
        }));

        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Library"));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(breadcrumb.Controls.OfType<LinkLabel>().Select(link => link.Text), Is.EqualTo(new[] { "Home" }));
            Assert.That(breadcrumb.Controls.OfType<Label>().Count(label => label.Text == "/"), Is.EqualTo(1));
            Assert.That(breadcrumb.Controls.OfType<Label>().Any(label => label.Text == "Library"), Is.True);
        }));

        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Data"));
        var semanticOrder = breadcrumb.Controls.Cast<Control>().Select(control => control.Text).ToArray();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(breadcrumb.Controls.OfType<LinkLabel>().Select(link => link.Text), Is.EqualTo(new[] { "Home", "Library" }));
            Assert.That(breadcrumb.Controls.OfType<LinkLabel>().All(link => link.TabStop), Is.True);
            Assert.That(breadcrumb.Controls.OfType<Label>().Any(label => label.Text == "Data" && !label.TabStop), Is.True);
            Assert.That(breadcrumb.Controls.OfType<Label>().Count(label => label.Text == "/"), Is.EqualTo(2));
            Assert.That(semanticOrder, Is.EqualTo(new[] { "Home", "/", "Library", "/", "Data" }));
        }));
    }

    [Test]
    public void GeneratedChildrenUseTheExactNativePropertyContract()
    {
        using var breadcrumb = CreateThreeItemBreadcrumb();
        var theme = BootstrapThemeManager.CurrentTheme;
        var links = breadcrumb.Controls.OfType<LinkLabel>().ToArray();
        var current = breadcrumb.Controls.OfType<Label>().Single(label => label.Text == "Data");
        var dividers = breadcrumb.Controls.OfType<Label>().Where(label => label.Text == "/").ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(links.All(link => link.AutoSize && link.TabStop && !link.UseMnemonic), Is.True);
            Assert.That(links.All(link => link.LinkBehavior == LinkBehavior.AlwaysUnderline), Is.True);
            Assert.That(links.All(link => !link.LinkVisited), Is.True);
            Assert.That(links.All(link => link.LinkColor == theme.Colors.Primary), Is.True);
            Assert.That(links.All(link => link.VisitedLinkColor == theme.Colors.Primary), Is.True);
            Assert.That(links.All(link => link.ActiveLinkColor == theme.Colors.Primary), Is.True);
            Assert.That(links.All(link => link.DisabledLinkColor == theme.Colors.Disabled), Is.True);
            Assert.That(links.All(link => link.BackColor == Color.Transparent), Is.True);
            Assert.That(links.All(link => link.Margin == Padding.Empty && link.Padding == Padding.Empty), Is.True);
            Assert.That(links.Select(link => link.AccessibleName), Is.EqualTo(new[] { "Home", "Library" }));
            Assert.That(links.All(link => link.Links.Count == 1 && link.Links[0].Start == 0 && link.Links[0].Length == link.Text.Length), Is.True);

            Assert.That(current.AutoSize, Is.True);
            Assert.That(current.TabStop, Is.False);
            Assert.That(current.UseMnemonic, Is.False);
            Assert.That(current.BackColor, Is.EqualTo(Color.Transparent));
            Assert.That(current.ForeColor, Is.EqualTo(theme.Colors.MutedText));
            Assert.That(current.Margin, Is.EqualTo(Padding.Empty));
            Assert.That(current.Padding, Is.EqualTo(Padding.Empty));
            Assert.That(current.AccessibleName, Is.EqualTo("Data"));
            Assert.That(current.AccessibleDescription, Is.EqualTo("Current page."));

            Assert.That(dividers.All(divider => divider.AutoSize && !divider.TabStop && !divider.UseMnemonic), Is.True);
            Assert.That(dividers.All(divider => divider.AccessibleRole == AccessibleRole.None), Is.True);
            Assert.That(dividers.All(divider => divider.AccessibleName == string.Empty), Is.True);
            Assert.That(dividers.All(divider => divider.AccessibleDescription == string.Empty), Is.True);
            Assert.That(dividers.All(divider => divider.ForeColor == theme.Colors.MutedText), Is.True);
        }));
    }

    [Test]
    public void EveryStructuralMutationRebuildsAndDisposesThePreviousGeneration()
    {
        using var breadcrumb = CreateThreeItemBreadcrumb();
        AssertRebuildDisposesOldControls(breadcrumb, () => breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Details")));
        AssertRebuildDisposesOldControls(breadcrumb, () => breadcrumb.Items.Insert(1, new BootstrapBreadcrumbItem("Inserted")));
        AssertRebuildDisposesOldControls(breadcrumb, () => breadcrumb.Items[1] = new BootstrapBreadcrumbItem("Replacement"));
        AssertRebuildDisposesOldControls(breadcrumb, () => breadcrumb.Items.RemoveAt(1));
        AssertRebuildDisposesOldControls(breadcrumb, () => breadcrumb.Items.Clear());
        Assert.That(breadcrumb.Controls, Is.Empty);
    }

    [Test]
    public void TextMutationUpdatesExistingControlsAndPreservesFocusedAncestor()
    {
        using var form = new Form();
        using var breadcrumb = CreateThreeItemBreadcrumb();
        form.Controls.Add(breadcrumb);
        form.Show();
        Application.DoEvents();

        var link = breadcrumb.Controls.OfType<LinkLabel>().First();
        var current = breadcrumb.Controls.OfType<Label>().Single(label => label.Text == "Data");
        Assert.That(link.Focus(), Is.True);
        var before = breadcrumb.GetPreferredSize(Size.Empty);

        breadcrumb.Items[0].Text = "A much longer home location";
        breadcrumb.Items[2].Text = "Current data location";
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(breadcrumb.Controls.OfType<LinkLabel>().First(), Is.SameAs(link));
            Assert.That(breadcrumb.Controls.OfType<Label>().Single(label => label.AccessibleDescription == "Current page."), Is.SameAs(current));
            Assert.That(link.Text, Is.EqualTo("A much longer home location"));
            Assert.That(link.AccessibleName, Is.EqualTo(link.Text));
            Assert.That(link.Focused, Is.True);
            Assert.That(current.Text, Is.EqualTo("Current data location"));
            Assert.That(current.AccessibleName, Is.EqualTo(current.Text));
            Assert.That(breadcrumb.GetPreferredSize(Size.Empty).Width, Is.GreaterThan(before.Width));
        }));
    }

    [Test]
    public void TagMutationPreservesGenerationAndDoesNotRequestLayout()
    {
        using var breadcrumb = new LayoutCountingBreadcrumb();
        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Home"));
        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Data"));
        var controls = breadcrumb.Controls.Cast<Control>().ToArray();
        var preferred = breadcrumb.GetPreferredSize(Size.Empty);
        breadcrumb.ResetLayoutCount();
        var tag = new object();

        breadcrumb.Items[0].Tag = tag;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(breadcrumb.Items[0].Tag, Is.SameAs(tag));
            Assert.That(breadcrumb.Controls.Cast<Control>().ToArray(), Is.EqualTo(controls));
            Assert.That(breadcrumb.GetPreferredSize(Size.Empty), Is.EqualTo(preferred));
            Assert.That(breadcrumb.LayoutCount, Is.Zero);
        }));
    }

    [Test]
    public void PreferredSizeUsesProposedWidthThenMaximumWidthOtherwiseOneRow()
    {
        using var breadcrumb = new BootstrapBreadcrumb();
        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("A long root location"));
        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("A long middle location"));
        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("A long current location"));

        var unbounded = breadcrumb.GetPreferredSize(Size.Empty);
        var proposed = breadcrumb.GetPreferredSize(new Size(150, 0));
        breadcrumb.MaximumSize = new Size(160, 0);
        var maximum = breadcrumb.GetPreferredSize(Size.Empty);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(proposed.Height, Is.GreaterThan(unbounded.Height));
            Assert.That(maximum.Width, Is.LessThanOrEqualTo(160));
            Assert.That(maximum.Height, Is.GreaterThan(unbounded.Height));
        }));
    }

    private static BootstrapBreadcrumb CreateThreeItemBreadcrumb()
    {
        var breadcrumb = new BootstrapBreadcrumb();
        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Home"));
        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Library"));
        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Data"));
        return breadcrumb;
    }

    private static void AssertRebuildDisposesOldControls(BootstrapBreadcrumb breadcrumb, Action mutation)
    {
        var previous = breadcrumb.Controls.Cast<Control>().ToArray();
        mutation();

        Assert.That(previous.All(control => control.IsDisposed), Is.True);
        Assert.That(breadcrumb.Controls.Cast<Control>().Intersect(previous), Is.Empty);
    }

    private sealed class LayoutCountingBreadcrumb : BootstrapBreadcrumb
    {
        internal int LayoutCount { get; private set; }

        internal void ResetLayoutCount()
        {
            LayoutCount = 0;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            LayoutCount++;
            base.OnLayout(levent);
        }
    }
}
