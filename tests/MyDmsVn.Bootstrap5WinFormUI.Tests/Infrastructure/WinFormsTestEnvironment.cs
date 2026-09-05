using System.Windows.Forms;
using NUnit.Framework;

[SetUpFixture]
public sealed class WinFormsTestEnvironment
{
    [OneTimeSetUp]
    public void ConfigureUnhandledExceptionMode()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
    }
}
