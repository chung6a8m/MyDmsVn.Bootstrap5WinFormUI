using System;
using System.Reflection;
using MyDmsVn.Bootstrap5WinFormUI.Controls.Internal;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class BootstrapUiDebouncerTests
{
    [Test]
    public void ZeroDelayExecutesImmediatelyAndNegativeDelayIsRejected()
    {
        using var debouncer = new BootstrapUiDebouncer();
        var calls = 0;
        debouncer.Schedule(TimeSpan.Zero, () => calls++);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(calls, Is.EqualTo(1));
            Assert.That((Action)(() => debouncer.Schedule(TimeSpan.FromMilliseconds(-1), () => { })), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That((Action)(() => debouncer.Schedule(TimeSpan.Zero, null!)), Throws.ArgumentNullException);
        }));
    }

    [Test]
    public void ReplacementAndCancelControlTheSinglePendingAction()
    {
        using var debouncer = new BootstrapUiDebouncer();
        var calls = string.Empty;
        debouncer.Schedule(TimeSpan.FromSeconds(1), () => calls += "old");
        debouncer.Schedule(TimeSpan.FromSeconds(1), () => calls += "new");
        FireTimer(debouncer);
        Assert.That(calls, Is.EqualTo("new"));

        debouncer.Schedule(TimeSpan.FromSeconds(1), () => calls += "cancelled");
        debouncer.Cancel();
        FireTimer(debouncer);
        Assert.That(calls, Is.EqualTo("new"));
    }

    [Test]
    public void DisposeIsIdempotentAndGuardsScheduling()
    {
        var debouncer = new BootstrapUiDebouncer();
        debouncer.Dispose();
        debouncer.Dispose();
        debouncer.Cancel();
        Assert.That((Action)(() => debouncer.Schedule(TimeSpan.Zero, () => { })), Throws.TypeOf<ObjectDisposedException>());
    }

    private static void FireTimer(BootstrapUiDebouncer debouncer)
    {
        typeof(BootstrapUiDebouncer).GetMethod("OnTick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(debouncer, new object?[] { null, EventArgs.Empty });
    }
}
