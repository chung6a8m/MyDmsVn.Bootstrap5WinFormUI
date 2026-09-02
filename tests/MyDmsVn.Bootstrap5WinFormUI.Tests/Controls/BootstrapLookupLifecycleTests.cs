using System;
using System.Collections.Generic;
using System.ComponentModel;
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
[NonParallelizable]
public sealed class BootstrapLookupLifecycleTests
{
    [Test]
    public void DisposeDetachesThemeAndSourceSubscriptions()
    {
        var source = new BindingList<Item> { new Item(1, "Alpha") };
        var baseline = ThemeSubscriberCount();
        var lookup = new BootstrapLookupBox { DataSource = source, DisplayMember = "Name", ValueMember = "Id" };
        Assert.That(ThemeSubscriberCount(), Is.GreaterThan(baseline));
        lookup.Dispose();
        Assert.DoesNotThrow((Action)(() => source.Add(new Item(2, "Beta"))));
        Assert.That(ThemeSubscriberCount(), Is.EqualTo(baseline));
    }

    [Test]
    public void InvalidConfigurationIsRejectedBeforeMutation()
    {
        using var lookup = new BootstrapLookupBox();
        using var column = new BootstrapLookupColumn();
        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<InvalidEnumArgumentException>((Action)(() => lookup.EmptyQueryBehavior = (BootstrapLookupEmptyQueryBehavior)99));
            Assert.Throws<InvalidEnumArgumentException>((Action)(() => column.EnterKeyBehavior = (BootstrapLookupEnterKeyBehavior)99));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => column.SearchDebounceMilliseconds = -1));
            Assert.Throws<ArgumentNullException>((Action)(() => column.TextComparer = null!));
        }));
    }

    [Test]
    public void LookupOwnedInteractionSurfacesStayOutOfTabOrder()
    {
        using var lookup = new BootstrapLookupBox();
        Assert.That(lookup.AccessibleRole, Is.EqualTo(AccessibleRole.ComboBox));
        Assert.That(lookup.ResultsGrid.TabStop, Is.False);
        Assert.That(Descendants(lookup).Where(control => !ReferenceEquals(control, lookup)).All(control => !control.TabStop), Is.True);
    }

    private static int ThemeSubscriberCount()
    {
        var field = typeof(BootstrapThemeManager).GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic)!;
        return ((MulticastDelegate?)field.GetValue(null))?.GetInvocationList().Length ?? 0;
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    private sealed class Item
    {
        internal Item(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }
}
