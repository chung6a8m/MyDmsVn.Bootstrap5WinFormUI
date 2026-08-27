using System;
using System.Reflection;
using System.Windows.Forms;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapDataGridViewContractTests
{
    [Test]
    public void Phase13PublicContractExists()
    {
        var assembly = typeof(BootstrapButton).Assembly;
        var gridType = assembly.GetType("MyDmsVn.Bootstrap5WinFormUI.Controls.BootstrapDataGridView");

        Assert.That(gridType, Is.Not.Null, "Phase 13 requires BootstrapDataGridView.");
        Assert.That(typeof(DataGridView).IsAssignableFrom(gridType!), Is.True, "BootstrapDataGridView must retain normal DataGridView behavior through inheritance.");
        Assert.That(gridType!.GetConstructor(Type.EmptyTypes), Is.Not.Null, "Designer usage requires a public parameterless constructor.");

        foreach (var propertyName in new[] { "EmptyStateText", "Loading", "LoadingText" })
        {
            Assert.That(gridType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public), Is.Not.Null, propertyName);
        }
    }
}
