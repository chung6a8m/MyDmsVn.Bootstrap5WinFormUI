using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class IntegratedDemoApplicationTests
{
    private static readonly string[] RequiredPages =
    {
        "Theme",
        "Buttons / Groups / Toolbar",
        "Inputs",
        "Checks / Radios / Switches",
        "Advanced Inputs",
        "Select",
        "Input Groups",
        "Cards",
        "Feedback",
        "Collapse / Accordion",
        "Loading / Spinner",
        "Progress",
        "Sidebar",
        "DataGrid",
        "Pagination",
        "Navigation / Tabs"
    };

    [Test]
    public void Phase14MainDemoUsesFrameworkSidebarWithAllRequiredPages()
    {
        using var form = new MainForm();
        form.CreateControl();
        form.PerformLayout();

        var sidebar = FindControls<BootstrapSidebar>(form).Single();
        var pageNames = sidebar.Items.Select(item => item.Text).ToArray();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(pageNames, Is.EqualTo(RequiredPages));
            Assert.That(sidebar.SelectedItem, Is.Not.Null);
            Assert.That(sidebar.SelectedItem!.Text, Is.EqualTo("Theme"));
        }));
    }

    [Test]
    public void SelectingDataGridNavigationEmbedsDataGridDemoInMainWindow()
    {
        using var form = new MainForm();
        form.CreateControl();
        form.PerformLayout();

        var sidebar = FindControls<BootstrapSidebar>(form).Single();
        sidebar.SelectedItem = sidebar.Items.Single(item => item.Text == "DataGrid");
        form.PerformLayout();

        var embeddedForms = FindControls<Form>(form).ToArray();
        Assert.That(
            embeddedForms.Any(child => child.GetType().Name == "DataGridDemoForm" && !child.TopLevel),
            Is.True,
            "Phase 14 navigation should keep component demos inside the integrated application window.");
    }

    [Test]
    public void SelectingFeedbackNavigationEmbedsFeedbackDemoInMainWindow()
    {
        using var form = new MainForm();
        form.CreateControl();
        form.PerformLayout();

        var sidebar = FindControls<BootstrapSidebar>(form).Single();
        sidebar.SelectedItem = sidebar.Items.Single(item => item.Text == "Feedback");
        form.PerformLayout();

        var embeddedForms = FindControls<Form>(form).ToArray();
        Assert.That(
            embeddedForms.Any(child => child.GetType().Name == "FeedbackDemoForm" && !child.TopLevel),
            Is.True,
            "The first feedback-stage control should add one reusable Feedback page to the integrated demo.");
    }

    [Test]
    public void SelectingAdvancedInputsNavigationEmbedsAdvancedInputsDemoInMainWindow()
    {
        using var form = new MainForm();
        form.CreateControl();
        form.PerformLayout();

        var sidebar = FindControls<BootstrapSidebar>(form).Single();
        sidebar.SelectedItem = sidebar.Items.Single(item => item.Text == "Advanced Inputs");
        form.PerformLayout();

        var embeddedForms = FindControls<Form>(form).ToArray();
        Assert.That(
            embeddedForms.Any(child => child.GetType().Name == "AdvancedInputsDemoForm" && !child.TopLevel),
            Is.True,
            "Advanced input stages should share one reusable integrated demo page.");
    }

    [Test]
    public void SelectingChecksNavigationEmbedsChecksDemoInMainWindow()
    {
        using var form = new MainForm();
        form.CreateControl();
        var sidebar = FindControls<BootstrapSidebar>(form).Single();
        sidebar.SelectedItem = sidebar.Items.Single(item => item.Text == "Checks / Radios / Switches");
        form.PerformLayout();

        Assert.That(FindControls<Form>(form).Any(child => child.GetType().Name == "ChecksDemoForm" && !child.TopLevel), Is.True);
    }

    [Test]
    public void SelectingSelectNavigationEmbedsBootstrapSelectDemoInMainWindow()
    {
        using var form = new MainForm();
        form.CreateControl();
        form.PerformLayout();

        var sidebar = FindControls<BootstrapSidebar>(form).Single();
        sidebar.SelectedItem = sidebar.Items.Single(item => item.Text == "Select");
        form.PerformLayout();

        var embeddedForms = FindControls<Form>(form).ToArray();
        Assert.That(
            embeddedForms.Any(child => child.GetType().Name == "BootstrapSelectDemoForm" && !child.TopLevel),
            Is.True,
            "BootstrapSelect scenarios should be reachable from the integrated demo navigation.");
    }

    [Test]
    public void SelectingInputGroupsNavigationEmbedsInputGroupDemoInMainWindow()
    {
        using var form = new MainForm();
        form.CreateControl();
        var sidebar = FindControls<BootstrapSidebar>(form).Single();
        sidebar.SelectedItem = sidebar.Items.Single(item => item.Text == "Input Groups");
        form.PerformLayout();

        Assert.That(FindControls<Form>(form).Any(child => child.GetType().Name == "InputGroupDemoForm" && !child.TopLevel), Is.True);
    }

    [Test]
    public void ThemeAndReducedMotionControlsRemainAvailableOnEveryPage()
    {
        using var form = new MainForm();
        form.Show();
        form.PerformLayout();

        var sidebar = FindControls<BootstrapSidebar>(form).Single();
        var themeSelector = FindControls<ComboBox>(form).Single(combo => combo.Items.Contains("Light") && combo.Items.Contains("Dark"));
        var reducedMotion = FindControls<CheckBox>(form).Single(checkBox => checkBox.Text == "Reduced motion");

        foreach (var page in sidebar.Items)
        {
            sidebar.SelectedItem = page;
            form.PerformLayout();

            Assert.Multiple((Action)(() =>
            {
                Assert.That(themeSelector.Visible, Is.True, $"Theme selector must remain visible on {page.Text}.");
                Assert.That(reducedMotion.Visible, Is.True, $"Reduced-motion control must remain visible on {page.Text}.");
            }));
        }
    }

    private static IEnumerable<T> FindControls<T>(Control root)
        where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindControls<T>(child))
            {
                yield return nested;
            }
        }
    }
}
