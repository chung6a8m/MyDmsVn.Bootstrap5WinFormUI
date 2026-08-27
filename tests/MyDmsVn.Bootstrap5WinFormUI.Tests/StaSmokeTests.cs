using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class StaSmokeTests
{
    [Test]
    public void NUnitRunsWinFormsFixtureOnStaThread()
    {
        Assert.That(Thread.CurrentThread.GetApartmentState(), Is.EqualTo(ApartmentState.STA));
    }

    [Test]
    public void StaFixtureCanInstantiateWinFormsControl()
    {
        using var control = new Control();

        Assert.That(control.IsDisposed, Is.False);
    }
}
