using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using MyDmsVn.Bootstrap5WinFormUI.Demo;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Demo;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class PlaceholderDemoFormTests
{
    [Test]
    public void ConstructionAndDisposalNeedNoExternalServiceOrInteractiveUi()
    {
        Assert.DoesNotThrow((Action)(() =>
        {
            using var form = new PlaceholderDemoForm();
            form.CreateControl();
            form.PerformLayout();
            Assert.That(form.Visible, Is.False);
        }));

        var infrastructureFields = typeof(PlaceholderDemoForm)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(field => field.FieldType)
            .Where(type => typeof(System.Windows.Forms.Timer).IsAssignableFrom(type) || typeof(BackgroundWorker).IsAssignableFrom(type))
            .ToArray();
        Assert.That(infrastructureFields, Is.Empty);
    }

    [Test]
    public void DemoCoversSizesAnimationsColorsRadiusAndSizingOwnership()
    {
        using var form = new PlaceholderDemoForm();
        var placeholders = FindControls<BootstrapPlaceholder>(form).ToArray();

        Assert.Multiple((Action)delegate
        {
            Assert.That(
                placeholders.Where(item => item.AutoSize).Select(item => item.PlaceholderSize).Distinct(),
                Is.EquivalentTo(Enum.GetValues(typeof(BootstrapPlaceholderSize)).Cast<BootstrapPlaceholderSize>()));
            Assert.That(
                placeholders.Select(item => item.Animation).Distinct(),
                Is.EquivalentTo(Enum.GetValues(typeof(BootstrapPlaceholderAnimation)).Cast<BootstrapPlaceholderAnimation>()));
            Assert.That(
                placeholders.Select(item => item.Variant).Distinct(),
                Is.EquivalentTo(Enum.GetValues(typeof(BootstrapVariant)).Cast<BootstrapVariant>()));
            Assert.That(placeholders.All(item => string.IsNullOrEmpty(item.AccessibleName)), Is.True);
            Assert.That(placeholders.Any(item => !item.CustomColor.IsEmpty), Is.True);
            Assert.That(placeholders.Any(item => item.BorderRadius == 0), Is.True);
            Assert.That(placeholders.Any(item => item.BorderRadius == -1), Is.True);
            Assert.That(placeholders.Any(item => item.BorderRadius >= 999), Is.True);
            Assert.That(placeholders.Any(item => item.AutoSize), Is.True);
            Assert.That(placeholders.Any(item => !item.AutoSize), Is.True);
        });
    }

    [Test]
    public void SkeletonUsesIndependentPlaceholdersAndCallerOwnedExplicitBarGeometry()
    {
        using var form = new PlaceholderDemoForm();
        var skeleton = FindControls<Panel>(form).Single(panel => panel.AccessibleName == "Composed skeleton card");
        var placeholders = FindControls<BootstrapPlaceholder>(skeleton).ToArray();
        var explicitBars = placeholders
            .Where(item => item.Tag is string tag && tag.StartsWith("Skeleton explicit bar", StringComparison.Ordinal))
            .ToArray();
        var naturalSizeExamples = FindControls<BootstrapPlaceholder>(form)
            .Where(item => item.Tag is string tag && tag.StartsWith("Natural size", StringComparison.Ordinal))
            .ToArray();

        Assert.Multiple((Action)delegate
        {
            Assert.That(placeholders.Length, Is.GreaterThanOrEqualTo(6));
            Assert.That(placeholders.Distinct().Count(), Is.EqualTo(placeholders.Length));
            Assert.That(explicitBars.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(explicitBars.All(item => !item.AutoSize), Is.True);
            Assert.That(explicitBars.All(item => item.PlaceholderSize == BootstrapPlaceholderSize.Default), Is.True);
            Assert.That(explicitBars.Select(item => item.Width).Distinct().Count(), Is.GreaterThan(1));
            Assert.That(naturalSizeExamples.Any(item => item.PlaceholderSize == BootstrapPlaceholderSize.Small), Is.True);
        });
    }

    [Test]
    public void ApplicationOwnedSwapChangesPanelsAndNativeStatusText()
    {
        using var form = new PlaceholderDemoForm();
        form.Show();
        Application.DoEvents();
        var skeleton = FindControls<Panel>(form).Single(panel => panel.AccessibleName == "Swap skeleton panel");
        var content = FindControls<Panel>(form).Single(panel => panel.AccessibleName == "Loaded content panel");
        var status = FindControls<Label>(form).Single(label => label.AccessibleName == "Loading status");
        var toggle = FindControls<Button>(form).Single(button => button.AccessibleName == "Toggle loaded content");

        Assert.Multiple((Action)delegate
        {
            Assert.That(skeleton.Visible, Is.True);
            Assert.That(content.Visible, Is.False);
            Assert.That(status.Text, Is.EqualTo("Loading content…"));
        });

        toggle.PerformClick();

        Assert.Multiple((Action)delegate
        {
            Assert.That(skeleton.Visible, Is.False);
            Assert.That(content.Visible, Is.True);
            Assert.That(status.Text, Is.EqualTo("Content loaded."));
        });
    }

    [Test]
    public void MainFormRegistersAndConstructsPlaceholderSkeletonPage()
    {
        using var main = new MainForm();
        var sidebar = FindControls<BootstrapSidebar>(main).Single();
        var item = sidebar.Items.Single(candidate => candidate.Text == "Placeholder / Skeleton");
        Assert.That(item.Tag, Is.Not.Null);

        var definitionType = item.Tag!.GetType();
        var description = (string)definitionType.GetProperty("Description")!.GetValue(item.Tag)!;
        var factory = (Func<Form>)definitionType.GetProperty("CreateForm")!.GetValue(item.Tag)!;
        using var page = factory();

        Assert.Multiple((Action)delegate
        {
            Assert.That(page, Is.TypeOf<PlaceholderDemoForm>());
            Assert.That(description, Does.Contain("Glow"));
            Assert.That(description, Does.Contain("Wave"));
            Assert.That(description, Does.Contain("skeleton"));
            Assert.That(description, Does.Contain("application-owned"));
        });
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
