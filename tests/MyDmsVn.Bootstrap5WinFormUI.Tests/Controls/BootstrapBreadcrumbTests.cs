using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Reflection;
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

    [Test]
    public void AncestorActivationRaisesExactlyOneStableEventWithoutMutatingItems()
    {
        using var breadcrumb = CreateThreeItemBreadcrumb();
        var originalItems = breadcrumb.Items.ToArray();
        var received = Array.Empty<BootstrapBreadcrumbItemClickedEventArgs>();
        breadcrumb.ItemClicked += (_, e) => received = received.Concat(new[] { e }).ToArray();
        var links = breadcrumb.Controls.OfType<LinkLabel>().ToArray();

        Activate(links[0]);
        Activate(links[1]);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(received.Length, Is.EqualTo(2));
            Assert.That(received.Select(e => e.Item), Is.EqualTo(originalItems.Take(2)));
            Assert.That(received.Select(e => e.Index), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(breadcrumb.Items, Is.EqualTo(originalItems));
            Assert.That(breadcrumb.Items[breadcrumb.Items.Count - 1].Text, Is.EqualTo("Data"));
            Assert.That(
                breadcrumb.Controls.Cast<Control>().Where(control => control is not LinkLabel).All(control => control is Label),
                Is.True);
        }));
    }

    [Test]
    public void ReentrantActivationAllowsCallerToReplaceTrailWithoutDuplicateEvent()
    {
        using var breadcrumb = CreateThreeItemBreadcrumb();
        BootstrapBreadcrumbItemClickedEventArgs? received = null;
        var events = 0;
        breadcrumb.ItemClicked += (_, e) =>
        {
            events++;
            received = e;
            breadcrumb.Items.Clear();
            breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Replacement current"));
        };

        Activate(breadcrumb.Controls.OfType<LinkLabel>().First());

        Assert.Multiple((Action)(() =>
        {
            Assert.That(events, Is.EqualTo(1));
            Assert.That(received?.Item.Text, Is.EqualTo("Home"));
            Assert.That(received?.Index, Is.Zero);
            Assert.That(breadcrumb.Items.Select(item => item.Text), Is.EqualTo(new[] { "Replacement current" }));
            Assert.That(breadcrumb.Controls.OfType<LinkLabel>(), Is.Empty);
            Assert.That(breadcrumb.Controls.OfType<Label>().Single().Text, Is.EqualTo("Replacement current"));
        }));
    }

    [Test]
    public void StaleLinkCannotActivateAfterRebuildEvenWhenItsItemRemainsAnAncestor()
    {
        using var breadcrumb = CreateThreeItemBreadcrumb();
        var home = breadcrumb.Items[0];
        var stale = breadcrumb.Controls.OfType<LinkLabel>().First();
        var staleNativeLink = stale.Links.Cast<LinkLabel.Link>().Single();
        var events = 0;
        breadcrumb.ItemClicked += (_, _) => events++;

        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Details"));
        var current = breadcrumb.Controls.OfType<LinkLabel>().First(link => link.Text == "Home");

        Assert.That(stale.IsDisposed, Is.True);
        Activate(stale, staleNativeLink);
        Assert.That(events, Is.Zero);
        Assert.That(breadcrumb.Items[0], Is.SameAs(home));

        Activate(current);
        Assert.That(events, Is.EqualTo(1));
    }

    [Test]
    public void OnlyAncestorsAreTabbableAndDisablingParentPreservesItemData()
    {
        using var form = new Form();
        using var breadcrumb = CreateThreeItemBreadcrumb();
        form.Controls.Add(breadcrumb);
        form.Show();
        Application.DoEvents();
        var originalItems = breadcrumb.Items.ToArray();
        var links = breadcrumb.Controls.OfType<LinkLabel>().ToArray();
        var current = breadcrumb.Controls.OfType<Label>().Single(label => label.AccessibleDescription == "Current page.");

        Assert.That(links[0].Focus(), Is.True);
        breadcrumb.Items[0].Text = "Home updated";
        breadcrumb.Enabled = false;
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(links.All(link => link.TabStop), Is.True);
            Assert.That(current.TabStop, Is.False);
            Assert.That(breadcrumb.Controls.OfType<Label>().Where(label => label is not LinkLabel).All(label => !label.TabStop), Is.True);
            Assert.That(links.All(link => !link.Enabled), Is.True);
            Assert.That(breadcrumb.Items, Is.EqualTo(originalItems));
            Assert.That(breadcrumb.Items[0].Text, Is.EqualTo("Home updated"));
        }));
    }

    [Test]
    public void AccessibilityMetadataDistinguishesLinksCurrentItemAndDividers()
    {
        using var breadcrumb = CreateThreeItemBreadcrumb();
        breadcrumb.Divider = string.Empty;
        var links = breadcrumb.Controls.OfType<LinkLabel>().ToArray();
        var current = breadcrumb.Controls.OfType<Label>().Single(label => label.AccessibleDescription == "Current page.");
        var dividers = breadcrumb.Controls.OfType<Label>().Where(label => label.AccessibleRole == AccessibleRole.None).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(breadcrumb.AccessibleRole, Is.EqualTo(AccessibleRole.Grouping));
            Assert.That(breadcrumb.AccessibleName, Is.EqualTo("Breadcrumb"));
            Assert.That(breadcrumb.AccessibleDescription, Is.EqualTo("Breadcrumb navigation."));
            Assert.That(links.Select(link => link.AccessibleName), Is.EqualTo(new[] { "Home", "Library" }));
            Assert.That(current.AccessibleName, Is.EqualTo("Data"));
            Assert.That(current.AccessibleDescription, Is.EqualTo("Current page."));
            Assert.That(dividers.Length, Is.EqualTo(2));
            Assert.That(dividers.All(divider => divider.Text == string.Empty), Is.True);
            Assert.That(dividers.All(divider => divider.AccessibleName == string.Empty), Is.True);
            Assert.That(dividers.All(divider => divider.AccessibleDescription == string.Empty), Is.True);
        }));
    }

    [Test]
    public void DividerCustomizationAndRtlMirrorGeometryWithoutChangingLogicalIndex()
    {
        using var breadcrumb = CreateThreeItemBreadcrumb();
        breadcrumb.AutoSize = false;
        breadcrumb.Size = new Size(400, 80);
        breadcrumb.Divider = ">";
        breadcrumb.PerformLayout();
        var ltr = GetItemBounds(breadcrumb);

        breadcrumb.RightToLeftDivider = "<";
        breadcrumb.RightToLeft = RightToLeft.Yes;
        breadcrumb.PerformLayout();
        var rtl = GetItemBounds(breadcrumb);
        BootstrapBreadcrumbItemClickedEventArgs? received = null;
        breadcrumb.ItemClicked += (_, e) => received = e;
        Activate(breadcrumb.Controls.OfType<LinkLabel>().First(link => link.Text == "Library"));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(breadcrumb.Controls.OfType<Label>().Count(label => label.Text == "<"), Is.EqualTo(2));
            Assert.That(rtl.Length, Is.EqualTo(ltr.Length));
            Assert.That(rtl[0].Left, Is.EqualTo(breadcrumb.ClientSize.Width - ltr[0].Right));
            Assert.That(rtl[1].Left, Is.EqualTo(breadcrumb.ClientSize.Width - ltr[1].Right));
            Assert.That(received?.Index, Is.EqualTo(1));
            Assert.That(received?.Item.Text, Is.EqualTo("Library"));
        }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void WidthConstrainedRuntimeLayoutKeepsDividerWithFollowingItem(bool rightToLeft)
    {
        using var breadcrumb = new BootstrapBreadcrumb
        {
            AutoSize = false,
            RightToLeft = rightToLeft ? RightToLeft.Yes : RightToLeft.No,
            RightToLeftDivider = "<"
        };
        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("A long root location"));
        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("A long middle location"));
        breadcrumb.Items.Add(new BootstrapBreadcrumbItem("A long current location"));
        var preferred = breadcrumb.GetPreferredSize(new Size(190, 0));
        breadcrumb.ClientSize = new Size(190, preferred.Height);
        breadcrumb.PerformLayout();

        var semanticControls = breadcrumb.Controls.Cast<Control>().ToArray();
        for (var index = 1; index < breadcrumb.Items.Count; index++)
        {
            var itemControl = semanticControls[(index * 2)];
            var dividerControl = semanticControls[(index * 2) - 1];
            Assert.That(dividerControl.Bounds.Top, Is.EqualTo(itemControl.Bounds.Top));
        }

        Assert.That(semanticControls.Max(control => control.Bottom), Is.EqualTo(preferred.Height));
    }

    [Test]
    [NonParallelizable]
    public void ThemeChangesUpdateExistingGenerationExactlyOnceAndPreserveModels()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            var baselineSubscriptions = GetThemeSubscriptionCount();
            using var breadcrumb = new LayoutCountingBreadcrumb();
            breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Home") { Tag = new object() });
            breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Data") { Tag = new object() });
            var controls = breadcrumb.Controls.Cast<Control>().ToArray();
            var items = breadcrumb.Items.ToArray();
            var tags = items.Select(item => item.Tag).ToArray();
            breadcrumb.ResetLayoutCount();

            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);

            var colors = BootstrapThemeManager.CurrentTheme.Colors;
            Assert.Multiple((Action)(() =>
            {
                Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions + 1));
                Assert.That(breadcrumb.Controls.Cast<Control>().ToArray(), Is.EqualTo(controls));
                Assert.That(breadcrumb.Items, Is.EqualTo(items));
                Assert.That(breadcrumb.Items.Select(item => item.Tag), Is.EqualTo(tags));
                Assert.That(breadcrumb.Controls.OfType<LinkLabel>().All(link => link.LinkColor == colors.Primary), Is.True);
                Assert.That(breadcrumb.Controls.OfType<Label>().Where(label => label is not LinkLabel).All(label => label.ForeColor == colors.MutedText), Is.True);
                Assert.That(breadcrumb.LayoutCount, Is.GreaterThan(0));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    [NonParallelizable]
    public void ThemeFontIsOwnedButCallerFontRemainsCallerOwned()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            var themed = new BootstrapBreadcrumb();
            var originalOwnedFont = themed.Font;
            var current = BootstrapThemeManager.CurrentTheme;
            var typography = new BootstrapThemeTypography(
                new BootstrapFontToken("Segoe UI", current.Typography.Body.SizeInPoints + 2f),
                current.Typography.BodySmall,
                current.Typography.Label,
                current.Typography.HeadingSmall,
                current.Typography.HeadingMedium);
            BootstrapThemeManager.CurrentTheme = new BootstrapTheme(
                current.Mode,
                current.Colors,
                current.Metrics,
                typography,
                current.ReducedMotion);
            var replacementOwnedFont = themed.Font;

            Assert.That(replacementOwnedFont, Is.Not.SameAs(originalOwnedFont));
            AssertFontDisposed(originalOwnedFont);
            themed.Dispose();
            AssertFontDisposed(replacementOwnedFont);

            using var callerFont = new Font("Segoe UI", 10f, FontStyle.Italic);
            var callerOwned = new BootstrapBreadcrumb { Font = callerFont };
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            Assert.That(callerOwned.Font, Is.SameAs(callerFont));
            callerOwned.Dispose();
            AssertFontUsable(callerFont);
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [TestCase(96, 8, 4)]
    [TestCase(120, 10, 5)]
    [TestCase(144, 12, 6)]
    [TestCase(168, 14, 7)]
    [TestCase(192, 16, 8)]
    public void LayoutMetricsScaleThemeSpacingAtSupportedDpi(int dpi, int dividerGap, int rowGap)
    {
        var method = typeof(BootstrapBreadcrumb).GetMethod(
            "ResolveLayoutMetrics",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(int) },
            modifiers: null);
        Assert.That(method, Is.Not.Null);

        var metrics = (BootstrapBreadcrumbLayoutMetrics)method!.Invoke(null, new object[] { dpi })!;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(metrics.DividerGap, Is.EqualTo(dividerGap));
            Assert.That(metrics.RowGap, Is.EqualTo(rowGap));
        }));
    }

    [Test]
    public void HandleBackedActivationKeepsLogicalIndexWhileVirtualDpiGapsIncrease()
    {
        using var form = new Form();
        using var breadcrumb = CreateThreeItemBreadcrumb();
        form.Controls.Add(breadcrumb);
        form.Show();
        Application.DoEvents();
        Assert.That(breadcrumb.IsHandleCreated, Is.True);

        var method = typeof(BootstrapBreadcrumb).GetMethod(
            "ResolveLayoutMetrics",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(int) },
            modifiers: null)!;
        var at96 = (BootstrapBreadcrumbLayoutMetrics)method.Invoke(null, new object[] { 96 })!;
        var at192 = (BootstrapBreadcrumbLayoutMetrics)method.Invoke(null, new object[] { 192 })!;
        BootstrapBreadcrumbItemClickedEventArgs? received = null;
        breadcrumb.ItemClicked += (_, e) => received = e;

        Activate(breadcrumb.Controls.OfType<LinkLabel>().First(link => link.Text == "Library"));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(at192.DividerGap, Is.GreaterThan(at96.DividerGap));
            Assert.That(at192.RowGap, Is.GreaterThan(at96.RowGap));
            Assert.That(received?.Index, Is.EqualTo(1));
            Assert.That(received?.Item, Is.SameAs(breadcrumb.Items[1]));
        }));
    }

    [Test]
    [NonParallelizable]
    public void LifecycleStressDetachesMapsLinksItemsAndThemeSubscription()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        var baselineSubscriptions = GetThemeSubscriptionCount();
        try
        {
            var breadcrumb = new BootstrapBreadcrumb();
            var retainedItem = new BootstrapBreadcrumbItem("Home");
            breadcrumb.Items.Add(retainedItem);
            breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Data"));
            LinkLabel? staleLink = null;
            LinkLabel.Link? staleNativeLink = null;

            for (var iteration = 0; iteration < 100; iteration++)
            {
                staleLink = breadcrumb.Controls.OfType<LinkLabel>().First();
                staleNativeLink = staleLink.Links.Cast<LinkLabel.Link>().Single();
                breadcrumb.Items.Add(new BootstrapBreadcrumbItem("Step " + iteration));
                breadcrumb.Items[breadcrumb.Items.Count - 1].Text = "Current " + iteration;
                breadcrumb.Divider = iteration % 2 == 0 ? "/" : ">";
                breadcrumb.RightToLeft = iteration % 2 == 0 ? RightToLeft.No : RightToLeft.Yes;
                breadcrumb.WrapContents = iteration % 3 != 0;
                breadcrumb.Items.RemoveAt(breadcrumb.Items.Count - 1);
            }

            var currentControls = breadcrumb.Controls.Cast<Control>().ToArray();
            var events = 0;
            breadcrumb.ItemClicked += (_, _) => events++;
            breadcrumb.Dispose();
            retainedItem.Text = "Caller still owns item";
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            Activate(staleLink!, staleNativeLink!);

            Assert.Multiple((Action)(() =>
            {
                Assert.That(currentControls.All(control => control.IsDisposed), Is.True);
                Assert.That(GetPrivateDictionaryCount(breadcrumb, "_activeLinks"), Is.Zero);
                Assert.That(GetPrivateDictionaryCount(breadcrumb, "_itemControls"), Is.Zero);
                Assert.That(GetThemeSubscriptionCount(), Is.EqualTo(baselineSubscriptions));
                Assert.That(events, Is.Zero);
                Assert.That(retainedItem.Text, Is.EqualTo("Caller still owns item"));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
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

    private static void Activate(LinkLabel link, LinkLabel.Link? nativeLink = null)
    {
        var method = typeof(LinkLabel).GetMethod(
            "OnLinkClicked",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        method.Invoke(
            link,
            new object[] { new LinkLabelLinkClickedEventArgs(nativeLink ?? link.Links.Cast<LinkLabel.Link>().Single()) });
    }

    private static Rectangle[] GetItemBounds(BootstrapBreadcrumb breadcrumb)
    {
        return breadcrumb.Items
            .Select(item => breadcrumb.Controls.Cast<Control>().Single(control => control.AccessibleName == item.Text))
            .Select(control => control.Bounds)
            .ToArray();
    }

    private static int GetThemeSubscriptionCount()
    {
        var eventField = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(eventField, Is.Not.Null);
        var handler = eventField!.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private static int GetPrivateDictionaryCount(BootstrapBreadcrumb breadcrumb, string fieldName)
    {
        var field = typeof(BootstrapBreadcrumb).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return ((ICollection)field!.GetValue(breadcrumb)!).Count;
    }

    private static void AssertFontDisposed(Font font)
    {
        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.Catch((Action)(() => graphics.MeasureString("x", font)));
    }

    private static void AssertFontUsable(Font font)
    {
        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        Assert.DoesNotThrow((Action)(() => graphics.MeasureString("x", font)));
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
