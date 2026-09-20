using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;

namespace MyDmsVn.Bootstrap5WinFormUI.Demo;

/// <summary>Demonstrates Bootstrap-inspired hierarchy trails composed from native links and labels.</summary>
public sealed class BreadcrumbDemoForm : DemoFormBase
{
    private readonly FlowLayoutPanel _content = new FlowLayoutPanel();
    private readonly Label _activationOutput = new Label();

    /// <summary>Initializes the Breadcrumb scenarios.</summary>
    public BreadcrumbDemoForm()
    {
        Text = "BootstrapBreadcrumb Demo";
        ClientSize = new Size(1000, 820);
        MinimumSize = new Size(760, 520);

        _content.Dock = DockStyle.Fill;
        _content.AutoScroll = true;
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.Padding = new Padding(16);
        Controls.Add(_content);

        _activationOutput.AutoSize = true;
        _activationOutput.AccessibleName = "Breadcrumb activation output";
        _activationOutput.Text = "Activate an ancestor link to see index, text, and tag.";
        _activationOutput.Margin = new Padding(0, 0, 0, 14);
        _content.Controls.Add(_activationOutput);

        AddScenario("Current item only", CreateTrail("Dashboard"), "The final item is always current and is not focusable.");
        AddScenario("Basic hierarchy", CreateTrail("Home", "Library", "Data"), "Tab reaches native ancestor links; the final item stays current.");
        AddScenario("Deep hierarchy", CreateTrail("Organization", "Division", "Department", "Projects", "Quarterly reporting"), "Longer trails remain ordered and wrap only at segment boundaries.");

        var customDivider = CreateTrail("Home", "Catalog", "Product");
        customDivider.Divider = ">";
        AddScenario("Custom divider", customDivider, "Divider text is caller-configurable.");

        var emptyDivider = CreateTrail("Home", "Catalog", "Product");
        emptyDivider.Divider = string.Empty;
        AddScenario("Empty divider", emptyDivider, "Empty text removes the glyph while retaining inter-item spacing.");

        var wrapped = CreateTrail("Home", "A very long department name", "A long project location", "Current report");
        wrapped.MaximumSize = new Size(320, 0);
        AddScenario("Width-constrained wrapping", wrapped, "MaximumSize supplies the supported standalone wrap constraint.");

        var rtl = CreateTrail("Home", "Library", "Data");
        rtl.RightToLeft = RightToLeft.Yes;
        rtl.RightToLeftDivider = "<";
        AddScenario("Right-to-left", rtl, "Visual flow mirrors while collection order and click indices stay logical.");

        var disabled = CreateTrail("Home", "Library", "Data");
        disabled.Enabled = false;
        AddScenario("Disabled", disabled, "Parent disabled state suppresses native link interaction and uses disabled colors.");

        var liveText = CreateTrail("Home", "Live item", "Current");
        var updateText = new Button { AutoSize = true, Text = "Update live item text" };
        updateText.Click += (_, _) => liveText.Items[1].Text = "Live item updated " + DateTime.Now.ToString("HH:mm:ss");
        AddScenario("Live non-empty text mutation", liveText, "Text changes update the existing native control in place.", updateText);

        var interactive = CreateTrail("Home", "Library", "Data");
        interactive.AccessibleName = "Interactive navigation breadcrumb";
        WireActivationOutput(interactive, simulateNavigation: true);
        AddScenario("Caller-owned navigation", interactive, "Activating an ancestor trims this demo trail in Form code; Breadcrumb itself never navigates.");
    }

    private BootstrapBreadcrumb CreateTrail(params string[] labels)
    {
        var breadcrumb = new BootstrapBreadcrumb();
        foreach (var label in labels)
        {
            breadcrumb.Items.Add(new BootstrapBreadcrumbItem(label) { Tag = "tag:" + label.ToLowerInvariant().Replace(' ', '-') });
        }

        WireActivationOutput(breadcrumb, simulateNavigation: false);
        return breadcrumb;
    }

    private void WireActivationOutput(BootstrapBreadcrumb breadcrumb, bool simulateNavigation)
    {
        breadcrumb.ItemClicked += (_, e) =>
        {
            _activationOutput.Text = string.Format(
                "Clicked index {0}: {1} ({2})",
                e.Index,
                e.Item.Text,
                e.Item.Tag ?? "no tag");

            if (!simulateNavigation)
            {
                return;
            }

            // Navigation state belongs to the application. The control only reports activation.
            var retainedTrail = breadcrumb.Items.Take(e.Index + 1).ToArray();
            breadcrumb.Items.Clear();
            foreach (var item in retainedTrail)
            {
                breadcrumb.Items.Add(item);
            }
        };
    }

    private void AddScenario(string title, BootstrapBreadcrumb breadcrumb, string note, Control? action = null)
    {
        var preferred = breadcrumb.GetPreferredSize(Size.Empty);
        var panel = new Panel
        {
            Width = 900,
            Height = Math.Max(116, preferred.Height + (action is null ? 74 : 112)),
            Margin = new Padding(0, 0, 0, 12)
        };
        var heading = new Label { AutoSize = true, Text = title, Location = new Point(0, 0) };
        var description = new Label { AutoSize = true, Text = note, Location = new Point(0, 24) };
        breadcrumb.Location = new Point(0, 52);

        panel.Controls.Add(breadcrumb);
        panel.Controls.Add(description);
        panel.Controls.Add(heading);
        if (action is not null)
        {
            action.Location = new Point(0, breadcrumb.Bottom + 8);
            panel.Controls.Add(action);
        }

        _content.Controls.Add(panel);
    }
}
