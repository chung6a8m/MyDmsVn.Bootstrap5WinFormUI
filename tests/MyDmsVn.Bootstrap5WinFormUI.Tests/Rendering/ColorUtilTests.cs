using System.Drawing;
using MyDmsVn.Bootstrap5WinFormUI.Rendering;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Rendering;

[TestFixture]
public sealed class ColorUtilTests
{
    [Test]
    public void RelativeLuminanceUsesSrgbDefinition()
    {
        Assert.That(ColorUtil.GetRelativeLuminance(Color.Black), Is.EqualTo(0d).Within(0.0001d));
        Assert.That(ColorUtil.GetRelativeLuminance(Color.White), Is.EqualTo(1d).Within(0.0001d));
    }

    [Test]
    public void ContrastRatioForBlackAndWhiteIsTwentyOneToOne()
    {
        Assert.That(ColorUtil.GetContrastRatio(Color.Black, Color.White), Is.EqualTo(21d).Within(0.001d));
    }

    [Test]
    public void GetContrastingTextColorChoosesHigherContrastCandidate()
    {
        Assert.That(
            ColorUtil.GetContrastingTextColor(Color.FromArgb(0x21, 0x25, 0x29), Color.White, Color.Black),
            Is.EqualTo(Color.White));

        Assert.That(
            ColorUtil.GetContrastingTextColor(Color.FromArgb(0xF8, 0xF9, 0xFA), Color.White, Color.Black),
            Is.EqualTo(Color.Black));
    }

    [Test]
    public void BlendInterpolatesRgbChannels()
    {
        var result = ColorUtil.Blend(Color.Red, Color.Blue, 0.5f);

        Assert.That(result, Is.EqualTo(Color.FromArgb(255, 128, 0, 128)));
    }
}
