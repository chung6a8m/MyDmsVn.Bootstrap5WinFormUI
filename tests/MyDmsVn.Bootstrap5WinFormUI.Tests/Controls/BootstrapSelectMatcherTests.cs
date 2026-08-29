using System;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapSelectMatcherTests
{
    [Test]
    public void DefaultMatcherUsesCaseInsensitiveTextContains()
    {
        var matcher = new BootstrapSelectTextMatcher();
        var item = new BootstrapSelectItem(1, "Alpha Corporation");

        Assert.Multiple((Action)(() =>
        {
            Assert.That(matcher.IsMatch(item, "alpha"), Is.True);
            Assert.That(matcher.IsMatch(item, "CORP"), Is.True);
            Assert.That(matcher.IsMatch(item, "pha Cor"), Is.True);
            Assert.That(matcher.IsMatch(item, "omega"), Is.False);
        }));
    }

    [Test]
    public void EmptyQueryMatchesEveryItemAndNullArgumentsAreRejected()
    {
        var matcher = new BootstrapSelectTextMatcher();
        var item = new BootstrapSelectItem(1, "Alpha");

        Assert.That(matcher.IsMatch(item, string.Empty), Is.True);
        Assert.That((Action)(() => matcher.IsMatch(null!, "a")), Throws.TypeOf<ArgumentNullException>());
        Assert.That((Action)(() => matcher.IsMatch(item, null!)), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void DefaultMatcherDoesNotPerformImplicitAccentOrFuzzyTransformation()
    {
        var matcher = new BootstrapSelectTextMatcher();

        Assert.That(matcher.IsMatch(new BootstrapSelectItem(1, "café"), "cafe"), Is.False);
        Assert.That(matcher.IsMatch(new BootstrapSelectItem(2, "alphabet"), "albt"), Is.False);
    }
}
