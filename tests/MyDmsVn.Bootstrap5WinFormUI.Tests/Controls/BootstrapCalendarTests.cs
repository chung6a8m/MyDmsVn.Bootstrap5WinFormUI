using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class BootstrapCalendarTests
{
    [Test]
    public void DefaultsExposeOnlyTheApprovedDesignerSafeCalendarSurface()
    {
        using var calendar = new BootstrapCalendar();
        var expectedMonth = ClampMonth(DateTime.Today, calendar.MinDate, calendar.MaxDate);
        var declared = typeof(BootstrapCalendar).GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(member => member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Event ||
                (member.MemberType == MemberTypes.Method && !((MethodInfo)member).IsSpecialName))
            .Select(member => member.Name).OrderBy(name => name).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(typeof(BootstrapCalendar).GetCustomAttribute<DefaultEventAttribute>()?.Name, Is.EqualTo(nameof(BootstrapCalendar.SelectionChanged)));
            Assert.That(calendar.Controls, Is.Empty);
            Assert.That(calendar.TabStop, Is.True);
            Assert.That(calendar.AccessibleRole, Is.EqualTo(AccessibleRole.Table));
            Assert.That(calendar.SelectionMode, Is.EqualTo(BootstrapCalendarSelectionMode.Single));
            Assert.That(calendar.MinDate, Is.EqualTo(DateTimePicker.MinimumDateTime.Date));
            Assert.That(calendar.MaxDate, Is.EqualTo(DateTimePicker.MaximumDateTime.Date));
            Assert.That(calendar.DisplayMonth, Is.EqualTo(expectedMonth));
            Assert.That(calendar.SelectedDate, Is.Null);
            Assert.That(calendar.RangeStart, Is.Null);
            Assert.That(calendar.RangeEnd, Is.Null);
            Assert.That(calendar.SelectedDates, Is.Empty);
            Assert.That(calendar.BorderRadius, Is.EqualTo(-1));
            Assert.That(calendar.Size, Is.EqualTo(calendar.GetPreferredSize(Size.Empty)));
            Assert.That(calendar.Size.Width, Is.GreaterThan(0));
            Assert.That(calendar.Size.Height, Is.GreaterThan(0));
            Assert.That(calendar.IsHandleCreated, Is.False);
            Assert.That(declared, Is.EqualTo(new[] { "BorderRadius", "ClearSelection", "DisplayMonth", "DisplayMonthChanged", "GetPreferredSize", "MaxDate", "MinDate", "RangeEnd", "RangeStart", "SelectedDate", "SelectedDates", "SelectionChanged", "SelectionMode", "SetRange", "SetSelectedDates", "ShowNextMonth", "ShowPreviousMonth" }));
        }));
        Assert.That(TypeDescriptor.GetProperties(calendar)[nameof(BootstrapCalendar.SelectedDates)]!.IsBrowsable, Is.False);
        Assert.That(TypeDescriptor.GetProperties(calendar)[nameof(BootstrapCalendar.RangeStart)]!.IsBrowsable, Is.True);
        Assert.That(TypeDescriptor.GetProperties(calendar)[nameof(BootstrapCalendar.RangeEnd)]!.IsBrowsable, Is.True);
        Assert.That(GetDefaultValue(nameof(BootstrapCalendar.SelectionMode)), Is.EqualTo(BootstrapCalendarSelectionMode.Single));
        Assert.That(GetDefaultValue(nameof(BootstrapCalendar.SelectedDate)), Is.Null);
        Assert.That(GetDefaultValue(nameof(BootstrapCalendar.BorderRadius)), Is.EqualTo(-1));
        Assert.That(typeof(BootstrapCalendar).GetProperty(nameof(BootstrapCalendar.RangeStart))!.GetCustomAttributes(typeof(BrowsableAttribute), false), Is.Empty);
        Assert.That(typeof(BootstrapCalendar).GetProperty(nameof(BootstrapCalendar.RangeEnd))!.GetCustomAttributes(typeof(BrowsableAttribute), false), Is.Empty);
        AssertExactPublicContract();
    }

    [Test]
    public void PreferredSizeMatchesRenderLogicAtDefaultDpi()
    {
        using var calendar = new BootstrapCalendar();
        var expected = BootstrapCalendarRenderLogic.CalculatePreferredSize(
            BootstrapCalendarRenderLogic.ResolveMetrics(BootstrapThemeManager.CurrentTheme.Metrics, DpiScaler.DefaultDpi, -1));
        Assert.That(calendar.GetPreferredSize(Size.Empty), Is.EqualTo(expected));
    }

    [Test]
    public void ModeSpecificStateAndEventsForwardAtomically()
    {
        using var calendar = new BootstrapCalendar { MinDate = new DateTime(2026, 1, 1), MaxDate = new DateTime(2026, 12, 31) };
        var changes = 0;
        calendar.SelectionChanged += (_, _) => changes++;
        calendar.SelectedDate = new DateTime(2026, 4, 10, 18, 0, 0);
        calendar.SelectedDate = new DateTime(2026, 4, 10);
        Assert.That(changes, Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>((Action)(() => calendar.SetRange(new DateTime(2026, 1, 1), null)));
        Assert.That(calendar.SelectedDate, Is.EqualTo(new DateTime(2026, 4, 10)));

        calendar.SelectionMode = BootstrapCalendarSelectionMode.Range;
        calendar.SetRange(new DateTime(2026, 6, 20), new DateTime(2026, 6, 10));
        Assert.That(calendar.RangeStart, Is.EqualTo(new DateTime(2026, 6, 10)));
        Assert.That(calendar.RangeEnd, Is.EqualTo(new DateTime(2026, 6, 20)));
        Assert.Throws<ArgumentException>((Action)(() => calendar.SetRange(null, new DateTime(2026, 6, 20))));
        Assert.That(calendar.RangeStart, Is.EqualTo(new DateTime(2026, 6, 10)));

        calendar.SelectionMode = BootstrapCalendarSelectionMode.Multiple;
        calendar.SetSelectedDates(new[] { new DateTime(2026, 7, 2), new DateTime(2026, 7, 1), new DateTime(2026, 7, 2) });
        Assert.That(calendar.SelectedDates, Is.EqualTo(new[] { new DateTime(2026, 7, 1), new DateTime(2026, 7, 2) }));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => calendar.SetSelectedDates(new[] { new DateTime(2026, 7, 3), new DateTime(2027, 1, 1) })));
        Assert.That(calendar.SelectedDates, Has.Count.EqualTo(2));
        calendar.ClearSelection();
        calendar.ClearSelection();
        Assert.That(calendar.SelectedDates, Is.Empty);
        Assert.That(changes, Is.EqualTo(6));
    }

    [Test]
    public void BoundsAndDisplayMonthNormalizeClampAndRaiseOnlyEffectiveEvents()
    {
        using var calendar = new BootstrapCalendar();
        var displayChanges = 0;
        calendar.DisplayMonthChanged += (_, _) => displayChanges++;
        calendar.MinDate = new DateTime(2026, 3, 15, 20, 0, 0);
        calendar.MaxDate = new DateTime(2026, 5, 10, 20, 0, 0);
        calendar.DisplayMonth = new DateTime(2026, 4, 27);
        calendar.DisplayMonth = new DateTime(2026, 4, 1);
        Assert.That(calendar.DisplayMonth, Is.EqualTo(new DateTime(2026, 4, 1)));
        Assert.That(displayChanges, Is.EqualTo(2));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => calendar.MinDate = new DateTime(2026, 6, 1)));
        Assert.That(calendar.MinDate, Is.EqualTo(new DateTime(2026, 3, 15)));
        calendar.ShowPreviousMonth();
        Assert.That(calendar.DisplayMonth, Is.EqualTo(new DateTime(2026, 3, 1)));
        calendar.ShowPreviousMonth();
        Assert.That(calendar.DisplayMonth, Is.EqualTo(new DateTime(2026, 3, 1)));
    }

    [Test]
    public void FocusAnchoringFollowsProgrammaticSelectionAndMonthRules()
    {
        using var calendar = new BootstrapCalendar { MinDate = new DateTime(2026, 1, 10), MaxDate = new DateTime(2026, 12, 20) };
        Assert.That(calendar.FocusedDate, Is.InRange(calendar.MinDate, calendar.MaxDate));
        calendar.SelectedDate = new DateTime(2026, 1, 31);
        Assert.That(calendar.FocusedDate, Is.EqualTo(new DateTime(2026, 1, 31)));
        calendar.DisplayMonth = new DateTime(2026, 2, 1);
        Assert.That(calendar.FocusedDate, Is.EqualTo(new DateTime(2026, 2, 28)));
        calendar.ClearSelection();
        calendar.SelectionMode = BootstrapCalendarSelectionMode.Range;
        Assert.That(calendar.FocusedDate, Is.EqualTo(new DateTime(2026, 2, 28)));
        calendar.SetRange(new DateTime(2026, 4, 2), new DateTime(2026, 4, 9));
        Assert.That(calendar.FocusedDate, Is.EqualTo(new DateTime(2026, 4, 9)));
        calendar.SelectionMode = BootstrapCalendarSelectionMode.Multiple;
        calendar.SetSelectedDates(new[] { new DateTime(2026, 6, 8), new DateTime(2026, 6, 3) });
        Assert.That(calendar.FocusedDate, Is.EqualTo(new DateTime(2026, 6, 3)));
        calendar.MaxDate = new DateTime(2026, 6, 1);
        Assert.That(calendar.FocusedDate, Is.EqualTo(new DateTime(2026, 6, 1)));
    }

    [Test]
    public void UnchangedNonEmptyProgrammaticSelectionsReanchorFocusWithoutRaisingSelectionChanged()
    {
        using var calendar = new BootstrapCalendar { MinDate = new DateTime(2026, 1, 1), MaxDate = new DateTime(2026, 12, 31) };
        var changes = 0;
        calendar.SelectionChanged += (_, _) => changes++;

        calendar.SelectedDate = new DateTime(2026, 1, 10);
        calendar.DisplayMonth = new DateTime(2026, 5, 1);
        calendar.SelectedDate = new DateTime(2026, 1, 10, 18, 0, 0);
        Assert.That(calendar.FocusedDate, Is.EqualTo(new DateTime(2026, 1, 10)));

        calendar.SelectionMode = BootstrapCalendarSelectionMode.Range;
        calendar.SetRange(new DateTime(2026, 2, 2), new DateTime(2026, 2, 8));
        calendar.DisplayMonth = new DateTime(2026, 6, 1);
        calendar.SetRange(new DateTime(2026, 2, 2), new DateTime(2026, 2, 8));
        Assert.That(calendar.FocusedDate, Is.EqualTo(new DateTime(2026, 2, 8)));

        calendar.SelectionMode = BootstrapCalendarSelectionMode.Multiple;
        calendar.SetSelectedDates(new[] { new DateTime(2026, 3, 7), new DateTime(2026, 3, 4) });
        calendar.DisplayMonth = new DateTime(2026, 7, 1);
        calendar.SetSelectedDates(new[] { new DateTime(2026, 3, 4), new DateTime(2026, 3, 7) });
        Assert.That(calendar.FocusedDate, Is.EqualTo(new DateTime(2026, 3, 4)));
        Assert.That(changes, Is.EqualTo(5));
    }

    [Test]
    public void LayoutCacheReusesGeometryUntilAKeyChanges()
    {
        using var calendar = new BootstrapCalendar { DisplayMonth = new DateTime(2026, 8, 1) };
        using var bitmap = new Bitmap(calendar.Width, calendar.Height);
        calendar.DrawToBitmap(bitmap, calendar.ClientRectangle);
        var first = calendar.LayoutBuildCount;
        calendar.DrawToBitmap(bitmap, calendar.ClientRectangle);
        Assert.That(calendar.LayoutBuildCount, Is.EqualTo(first));
        calendar.MinDate = calendar.MinDate.AddHours(12);
        calendar.MaxDate = calendar.MaxDate.AddHours(12);
        calendar.DrawToBitmap(bitmap, calendar.ClientRectangle);
        Assert.That(calendar.LayoutBuildCount, Is.EqualTo(first));
        calendar.BorderRadius = 12;
        calendar.DrawToBitmap(bitmap, calendar.ClientRectangle);
        Assert.That(calendar.LayoutBuildCount, Is.EqualTo(first + 1));
    }

    [Test]
    public void LayoutCacheCoversEveryRequiredGeometryKeyIncludingDpiAndThemeMetrics()
    {
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            using var calendar = new BootstrapCalendar { DisplayMonth = new DateTime(2026, 8, 1) };
            calendar.ResolveLayout(96);
            var builds = calendar.LayoutBuildCount;
            calendar.ResolveLayout(96);
            Assert.That(calendar.LayoutBuildCount, Is.EqualTo(builds));

            calendar.ResolveLayout(192); Assert.That(calendar.LayoutBuildCount, Is.EqualTo(++builds), "DPI");
            calendar.Size = new Size(calendar.Width + 1, calendar.Height); calendar.ResolveLayout(192); Assert.That(calendar.LayoutBuildCount, Is.EqualTo(++builds), "size");
            calendar.DisplayMonth = new DateTime(2026, 9, 1); calendar.ResolveLayout(192); Assert.That(calendar.LayoutBuildCount, Is.EqualTo(++builds), "month");

            var culture = (System.Globalization.CultureInfo)originalCulture.Clone();
            culture.DateTimeFormat.FirstDayOfWeek = originalCulture.DateTimeFormat.FirstDayOfWeek == DayOfWeek.Sunday ? DayOfWeek.Monday : DayOfWeek.Sunday;
            System.Globalization.CultureInfo.CurrentCulture = culture;
            calendar.ResolveLayout(192); Assert.That(calendar.LayoutBuildCount, Is.EqualTo(++builds), "culture first day");

            calendar.MinDate = new DateTime(2026, 1, 2); calendar.ResolveLayout(192); Assert.That(calendar.LayoutBuildCount, Is.EqualTo(++builds), "bounds");
            calendar.BorderRadius = 10; calendar.ResolveLayout(192); Assert.That(calendar.LayoutBuildCount, Is.EqualTo(++builds), "radius");
            using var callerFont = new Font(calendar.Font.FontFamily, calendar.Font.Size + 1f);
            calendar.Font = callerFont; calendar.ResolveLayout(192); Assert.That(calendar.LayoutBuildCount, Is.EqualTo(++builds), "font metrics");

            var metrics = new BootstrapThemeMetrics(28, 34, 38, 4, 6, 8, 2, 3, 4, 8, 12, 16, 24);
            BootstrapThemeManager.CurrentTheme = new BootstrapTheme(originalTheme.Mode, originalTheme.Colors, metrics, originalTheme.Typography);
            calendar.ResolveLayout(192); Assert.That(calendar.LayoutBuildCount, Is.EqualTo(++builds), "theme metrics");
            calendar.ResolveLayout(192); Assert.That(calendar.LayoutBuildCount, Is.EqualTo(builds), "unchanged key");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void DayOutlinesUseDpiScaledTaskThreeBorderMetrics()
    {
        var outlines = BootstrapCalendar.ResolveDayOutlineMetrics(BootstrapThemeMetrics.Default, 192);
        Assert.That(outlines.SelectionWidth, Is.EqualTo(2f));
        Assert.That(outlines.FocusWidth, Is.EqualTo(4f));

        var custom = new BootstrapThemeMetrics(28, 32, 38, 4, 6, 8, 3, 5, 4, 8, 12, 16, 24);
        outlines = BootstrapCalendar.ResolveDayOutlineMetrics(custom, 144);
        Assert.That(outlines.SelectionWidth, Is.EqualTo(4.5f));
        Assert.That(outlines.FocusWidth, Is.EqualTo(7.5f));
    }

    [Test]
    public void RenderClassificationDistinguishesCalendarStates()
    {
        var selected = BootstrapCalendar.ClassifyDay(new DateTime(2026, 8, 10), true, true, false, BootstrapCalendarSelectionMode.Single, new DateTime(2026, 8, 10), null, null, Array.Empty<DateTime>(), null);
        var middle = BootstrapCalendar.ClassifyDay(new DateTime(2026, 8, 11), true, true, false, BootstrapCalendarSelectionMode.Range, null, new DateTime(2026, 8, 10), new DateTime(2026, 8, 12), Array.Empty<DateTime>(), null);
        var preview = BootstrapCalendar.ClassifyDay(new DateTime(2026, 8, 14), true, true, false, BootstrapCalendarSelectionMode.Range, null, new DateTime(2026, 8, 10), null, Array.Empty<DateTime>(), new DateTime(2026, 8, 14));
        Assert.That((selected & BootstrapCalendarDayRenderState.Selected) != 0, Is.True);
        Assert.That((middle & BootstrapCalendarDayRenderState.RangeInterior) != 0, Is.True);
        Assert.That((preview & BootstrapCalendarDayRenderState.Preview) != 0, Is.True);
        Assert.That(BootstrapCalendar.ClassifyDay(DateTime.Today, false, false, true, BootstrapCalendarSelectionMode.Single, null, null, null, Array.Empty<DateTime>(), null), Is.EqualTo(BootstrapCalendarDayRenderState.AdjacentMonth | BootstrapCalendarDayRenderState.Disabled | BootstrapCalendarDayRenderState.Today));
    }

    [Test]
    public void OwnerDrawingSmokeCoversThemesModesAndTinyBounds()
    {
        var original = BootstrapThemeManager.CurrentTheme;
        try
        {
            foreach (var themeMode in new[] { BootstrapThemeMode.Light, BootstrapThemeMode.Dark })
            foreach (var selectionMode in new[] { BootstrapCalendarSelectionMode.Single, BootstrapCalendarSelectionMode.Range, BootstrapCalendarSelectionMode.Multiple })
            {
                BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(themeMode);
                using var calendar = new BootstrapCalendar { DisplayMonth = new DateTime(2026, 8, 1), SelectionMode = selectionMode, BorderRadius = 9 };
                if (selectionMode == BootstrapCalendarSelectionMode.Single) calendar.SelectedDate = new DateTime(2026, 8, 10);
                if (selectionMode == BootstrapCalendarSelectionMode.Range) calendar.SetRange(new DateTime(2026, 8, 10), selectionMode == BootstrapCalendarSelectionMode.Range && themeMode == BootstrapThemeMode.Light ? null : new DateTime(2026, 8, 15));
                if (selectionMode == BootstrapCalendarSelectionMode.Multiple) calendar.SetSelectedDates(new[] { new DateTime(2026, 8, 10), DateTime.Today.Date });
                calendar.Enabled = themeMode == BootstrapThemeMode.Light;
                using var bitmap = new Bitmap(calendar.Width, calendar.Height);
                Assert.DoesNotThrow((Action)(() => calendar.DrawToBitmap(bitmap, calendar.ClientRectangle)));
                calendar.Size = new Size(3, 2);
                using var tiny = new Bitmap(3, 2);
                Assert.DoesNotThrow((Action)(() => calendar.DrawToBitmap(tiny, calendar.ClientRectangle)));
            }
        }
        finally { BootstrapThemeManager.CurrentTheme = original; }
    }

    private static DateTime ClampMonth(DateTime date, DateTime minDate, DateTime maxDate)
    {
        var month = new DateTime(date.Year, date.Month, 1);
        var minMonth = new DateTime(minDate.Year, minDate.Month, 1);
        var maxMonth = new DateTime(maxDate.Year, maxDate.Month, 1);
        return month < minMonth ? minMonth : month > maxMonth ? maxMonth : month;
    }

    private static object? GetDefaultValue(string propertyName)
    {
        return ((DefaultValueAttribute)typeof(BootstrapCalendar).GetProperty(propertyName)!
            .GetCustomAttributes(typeof(DefaultValueAttribute), false).Single()).Value;
    }

    private static void AssertExactPublicContract()
    {
        var type = typeof(BootstrapCalendar);
        var constructor = type.GetConstructor(Type.EmptyTypes);
        Assert.That(constructor, Is.Not.Null);
        Assert.That(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public), Has.Length.EqualTo(1));

        AssertProperty(type, nameof(BootstrapCalendar.SelectionMode), typeof(BootstrapCalendarSelectionMode), canWrite: true, typeof(DefaultValueAttribute));
        AssertProperty(type, nameof(BootstrapCalendar.DisplayMonth), typeof(DateTime), canWrite: true);
        AssertProperty(type, nameof(BootstrapCalendar.MinDate), typeof(DateTime), canWrite: true);
        AssertProperty(type, nameof(BootstrapCalendar.MaxDate), typeof(DateTime), canWrite: true);
        AssertProperty(type, nameof(BootstrapCalendar.SelectedDate), typeof(DateTime?), canWrite: true, typeof(DefaultValueAttribute));
        AssertProperty(type, nameof(BootstrapCalendar.RangeStart), typeof(DateTime?), canWrite: false);
        AssertProperty(type, nameof(BootstrapCalendar.RangeEnd), typeof(DateTime?), canWrite: false);
        AssertProperty(type, nameof(BootstrapCalendar.SelectedDates), typeof(System.Collections.Generic.IReadOnlyList<DateTime>), canWrite: false, typeof(BrowsableAttribute));
        AssertProperty(type, nameof(BootstrapCalendar.BorderRadius), typeof(int), canWrite: true, typeof(DefaultValueAttribute));

        AssertEvent(type, nameof(BootstrapCalendar.SelectionChanged));
        AssertEvent(type, nameof(BootstrapCalendar.DisplayMonthChanged));
        AssertMethod(type, nameof(BootstrapCalendar.SetRange), typeof(void), typeof(DateTime?), typeof(DateTime?));
        AssertMethod(type, nameof(BootstrapCalendar.SetSelectedDates), typeof(void), typeof(System.Collections.Generic.IEnumerable<DateTime>));
        AssertMethod(type, nameof(BootstrapCalendar.ClearSelection), typeof(void));
        AssertMethod(type, nameof(BootstrapCalendar.ShowPreviousMonth), typeof(void));
        AssertMethod(type, nameof(BootstrapCalendar.ShowNextMonth), typeof(void));
        AssertMethod(type, nameof(BootstrapCalendar.GetPreferredSize), typeof(Size), typeof(Size));
    }

    private static void AssertProperty(Type type, string name, Type propertyType, bool canWrite, params Type[] attributeTypes)
    {
        var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.That(property, Is.Not.Null, name);
        Assert.That(property!.PropertyType, Is.EqualTo(propertyType), name);
        Assert.That(property.CanRead, Is.True, name);
        Assert.That(property.CanWrite, Is.EqualTo(canWrite), name);
        Assert.That(property.GetMethod!.IsPublic, Is.True, name);
        if (canWrite) Assert.That(property.SetMethod!.IsPublic, Is.True, name);
        foreach (var attributeType in attributeTypes) Assert.That(property.GetCustomAttributes(attributeType, false), Has.Length.EqualTo(1), name);
    }

    private static void AssertEvent(Type type, string name)
    {
        var eventInfo = type.GetEvent(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.That(eventInfo, Is.Not.Null, name);
        Assert.That(eventInfo!.EventHandlerType, Is.EqualTo(typeof(EventHandler)), name);
        Assert.That(eventInfo.AddMethod!.IsPublic, Is.True, name);
        Assert.That(eventInfo.RemoveMethod!.IsPublic, Is.True, name);
    }

    private static void AssertMethod(Type type, string name, Type returnType, params Type[] parameterTypes)
    {
        var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, null, parameterTypes, null);
        Assert.That(method, Is.Not.Null, name);
        Assert.That(method!.ReturnType, Is.EqualTo(returnType), name);
    }
}
