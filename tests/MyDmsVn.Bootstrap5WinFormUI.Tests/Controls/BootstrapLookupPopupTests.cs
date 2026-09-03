using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using MyDmsVn.Bootstrap5WinFormUI.Theme;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
[NonParallelizable]
public sealed class BootstrapLookupPopupTests
{
    [Test]
    public void OpenPopupReappliesSurfaceAndContentWhenThemeChanges()
    {
        var originalTheme = BootstrapThemeManager.CurrentTheme;
        try
        {
            BootstrapThemeManager.CurrentTheme = BootstrapTheme.CreateDefault(BootstrapThemeMode.Light);
            using var host = new Form();
            using var lookup = CreateLookup();
            host.Controls.Add(lookup);
            host.Show();
            lookup.OpenDropDown();
            Application.DoEvents();
            var content = (BootstrapLookupDropDownContent)lookup.ResultsGrid.Parent!;
            var surface = GetSurface(content);
            var darkBase = BootstrapTheme.CreateDefault(BootstrapThemeMode.Dark);
            var dark = new BootstrapTheme(
                BootstrapThemeMode.Dark,
                darkBase.Colors,
                darkBase.Metrics,
                new BootstrapThemeTypography(
                    new BootstrapFontToken("Segoe UI", 11f, FontStyle.Bold),
                    darkBase.Typography.BodySmall,
                    darkBase.Typography.Label,
                    darkBase.Typography.HeadingSmall,
                    darkBase.Typography.HeadingMedium));

            BootstrapThemeManager.CurrentTheme = dark;
            Application.DoEvents();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(lookup.IsDropDownOpen, Is.True);
                Assert.That(surface.BackColor, Is.EqualTo(dark.Colors.Surface));
                Assert.That(content.Font.SizeInPoints, Is.EqualTo(11f).Within(0.05f));
                Assert.That(content.Font.Style, Is.EqualTo(FontStyle.Bold));
            }));
        }
        finally
        {
            BootstrapThemeManager.CurrentTheme = originalTheme;
        }
    }

    [Test]
    public void OpenPopupReappliesMetricsAndBoundsWhenOwnerDpiChanges()
    {
        using var host = new Form { Size = new Size(700, 600) };
        using var lookup = CreateLookup();
        lookup.MaxDropDownHeight = 80;
        lookup.DataSource = new BindingList<Product>
        {
            new(1, "One"), new(2, "Two"), new(3, "Three"), new(4, "Four"),
            new(5, "Five"), new(6, "Six"), new(7, "Seven"), new(8, "Eight")
        };
        host.Controls.Add(lookup);
        host.Show();
        lookup.OpenDropDown();
        Application.DoEvents();
        var content = (BootstrapLookupDropDownContent)lookup.ResultsGrid.Parent!;
        var surface = GetSurface(content);
        var controller = GetController(lookup);

        controller.ApplyOwnerDpiChange(96);
        Application.DoEvents();
        var surfaceAt96Dpi = surface;
        var heightAt96Dpi = surface.Height;

        controller.ApplyOwnerDpiChange(192);
        Application.DoEvents();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.IsDropDownOpen, Is.True);
            Assert.That(GetSurface(content), Is.SameAs(surfaceAt96Dpi));
            Assert.That(heightAt96Dpi, Is.EqualTo(80));
            Assert.That(surface.Height, Is.EqualTo(160));
        }));
    }

    [Test]
    public void OpenPopupScalesLogicalFixedColumnDimensionsWhenDpiChanges()
    {
        using var host = new Form { Size = new Size(700, 600) };
        using var lookup = CreateLookup();
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = "Name",
            HeaderText = "Product",
            Width = 120,
            MinimumWidth = 40
        });
        host.Controls.Add(lookup);
        host.Show();
        lookup.OpenDropDown();
        var controller = GetController(lookup);

        controller.ApplyOwnerDpiChange(96);
        var widthAt96 = lookup.ResultsGrid.Columns[0].Width;
        var minimumAt96 = lookup.ResultsGrid.Columns[0].MinimumWidth;
        controller.ApplyOwnerDpiChange(192);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(widthAt96, Is.EqualTo(120));
            Assert.That(minimumAt96, Is.EqualTo(40));
            Assert.That(lookup.ResultsGrid.Columns[0].Width, Is.EqualTo(240));
            Assert.That(lookup.ResultsGrid.Columns[0].MinimumWidth, Is.EqualTo(80));
        }));
    }

    [Test]
    public void PublicOpenCloseAndCancelHaveDistinctStateSemantics()
    {
        using var host = new Form();
        using var lookup = CreateLookup();
        host.Controls.Add(lookup);
        host.Show();
        lookup.SelectValue(1);
        lookup.Text = "pending";
        lookup.OpenDropDown();
        Assert.That(lookup.IsDropDownOpen, Is.True);

        lookup.CloseDropDown();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.IsDropDownOpen, Is.False);
            Assert.That(lookup.Text, Is.EqualTo("pending"));
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
        }));

        lookup.OpenDropDown();
        lookup.CancelPendingEdit();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(lookup.IsDropDownOpen, Is.False);
            Assert.That(lookup.Text, Is.EqualTo("Coffee"));
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
        }));
        host.Close();
    }

    [Test]
    public void RefreshRaisesRequestWithoutCommittingOrChangingPendingText()
    {
        using var lookup = CreateLookup();
        lookup.SelectValue(1);
        lookup.Text = "cof";
        var refreshes = 0;
        lookup.RefreshRequested += (_, e) => { refreshes++; Assert.That(e.QueryText, Is.EqualTo("cof")); };
        lookup.RefreshResults();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(refreshes, Is.EqualTo(1));
            Assert.That(lookup.Text, Is.EqualTo("cof"));
            Assert.That(lookup.SelectedValue, Is.EqualTo(1));
        }));
    }

    [Test]
    public void SameValuePresentationChangeRepositionsOpenAutosizedPopup()
    {
        var source = new BindingList<Product> { new(1, "A") };
        using var host = new Form { Size = new Size(600, 400) };
        using var lookup = new BootstrapLookupBox
        {
            Width = 180,
            DropDownWidth = 180,
            MaxDropDownHeight = 300,
            DisplayMember = "Name",
            ValueMember = "Id",
            DataSource = source,
            SearchDebounceMilliseconds = 0
        };
        lookup.Columns.Add(new BootstrapLookupColumnDefinition
        {
            DataPropertyName = "Name",
            HeaderText = "Product",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        });
        host.Controls.Add(lookup);
        host.Show();
        lookup.OpenDropDown();
        Application.DoEvents();
        var content = (BootstrapLookupDropDownContent)lookup.ResultsGrid.Parent!;
        var initialHeight = GetSurface(content).Height;

        source[0] = new Product(1, new string('W', 80));
        Application.DoEvents();

        var horizontalScrollBar = lookup.ResultsGrid.Controls.OfType<HScrollBar>().Single();
        Assert.Multiple((Action)(() =>
        {
            Assert.That(horizontalScrollBar.Visible, Is.True);
            Assert.That(GetSurface(content).Height, Is.GreaterThan(initialHeight));
            Assert.That(lookup.ResultsGrid.GetRowDisplayRectangle(0, false).Bottom, Is.LessThanOrEqualTo(horizontalScrollBar.Top));
        }));
    }

    private static BootstrapLookupBox CreateLookup() => new BootstrapLookupBox
    {
        DisplayMember = "Name",
        ValueMember = "Id",
        DataSource = new BindingList<Product> { new(1, "Coffee"), new(2, "Tea") }
    };

    private static BootstrapOverlaySurface GetSurface(BootstrapLookupDropDownContent content) =>
        (BootstrapOverlaySurface)content.Parent!.Parent!;

    private static BootstrapLookupDropDownController GetController(BootstrapLookupBox lookup)
    {
        var field = typeof(BootstrapLookupBox).GetField("_dropDownController", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (BootstrapLookupDropDownController)field.GetValue(lookup)!;
    }

    private sealed class Product
    {
        internal Product(int id, string name) { Id = id; Name = name; }
        public int Id { get; }
        public string Name { get; }
    }
}
