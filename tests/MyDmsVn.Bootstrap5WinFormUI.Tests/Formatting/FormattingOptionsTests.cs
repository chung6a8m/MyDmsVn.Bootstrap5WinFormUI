using System;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Formatting;

[TestFixture]
public sealed class FormattingOptionsTests
{
    [Test]
    public void GeneralDefaultsAndArraysAreDefensive()
    {
        var options = new BootstrapGeneralFormatOptions();
        var blocks = new[] { 4, 4, 4, 4 };
        var delimiters = new[] { ".", "-" };

        options.Blocks = blocks;
        options.Delimiters = delimiters;
        blocks[0] = 99;
        delimiters[0] = "!";
        var returnedBlocks = options.Blocks;
        var returnedDelimiters = options.Delimiters;
        returnedBlocks[1] = 99;
        returnedDelimiters[1] = "!";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(options.Delimiter, Is.EqualTo(" "));
            Assert.That(options.Blocks, Is.EqualTo(new[] { 4, 4, 4, 4 }));
            Assert.That(options.Delimiters, Is.EqualTo(new[] { ".", "-" }));
            Assert.Throws<ArgumentException>((Action)(() => options.Blocks = new[] { 4, 0, 4 }));
        }));
    }

    [Test]
    public void GeneralCaseFlagsAreMutuallyExclusiveAndNotifyOnce()
    {
        var options = new BootstrapGeneralFormatOptions { Lowercase = true };
        var changes = 0;
        options.Changed += (_, _) => changes++;

        options.Uppercase = true;
        options.Uppercase = true;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(options.Uppercase, Is.True);
            Assert.That(options.Lowercase, Is.False);
            Assert.That(changes, Is.EqualTo(1));
        }));
    }

    [Test]
    public void NumeralDefaultsAndValidationAreAtomic()
    {
        var options = new BootstrapNumeralFormatOptions();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(options.Delimiter, Is.EqualTo(","));
            Assert.That(options.ThousandsGroupStyle, Is.EqualTo(BootstrapNumeralGroupStyle.Thousand));
            Assert.That(options.DecimalMark, Is.EqualTo("."));
            Assert.That(options.DecimalScale, Is.EqualTo(2));
            Assert.That(options.StripLeadingZeroes, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => options.DecimalScale = -1));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => options.IntegerScale = -1));
            Assert.Throws<ArgumentException>((Action)(() => options.Delimiter = ".."));
            Assert.Throws<ArgumentException>((Action)(() => options.DecimalMark = ",,"));
            Assert.Throws<ArgumentException>((Action)(() => options.Delimiter = "."));
            Assert.That(options.Delimiter, Is.EqualTo(","));
        }));
    }

    [Test]
    public void DateTimeAndCreditCardDefaultsMatchContract()
    {
        var date = new BootstrapDateFormatOptions();
        var time = new BootstrapTimeFormatOptions();
        var card = new BootstrapCreditCardFormatOptions();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(date.Pattern, Is.EqualTo("dmY"));
            Assert.That(date.Delimiter, Is.EqualTo("/"));
            Assert.That(time.Pattern, Is.EqualTo("hm"));
            Assert.That(time.Delimiter, Is.EqualTo(":"));
            Assert.That(time.TimeFormat, Is.EqualTo(BootstrapTimeFormat.TwentyFourHour));
            Assert.That(card.Delimiter, Is.EqualTo(" "));
            Assert.That(card.StrictMode, Is.False);
            Assert.Throws<ArgumentException>((Action)(() => date.Pattern = "dmyY"));
            Assert.Throws<ArgumentException>((Action)(() => date.Pattern = "dx"));
            Assert.Throws<ArgumentException>((Action)(() => time.Pattern = "hh"));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => time.TimeFormat = (BootstrapTimeFormat)99));
        }));
    }

    [Test]
    public void EffectiveAssignmentsRaiseOneChangedNotification()
    {
        var date = new BootstrapDateFormatOptions();
        var changes = 0;
        date.Changed += (_, _) => changes++;

        date.Pattern = "Ymd";
        date.Pattern = "Ymd";
        date.Delimiter = null!;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(date.Delimiter, Is.Empty);
            Assert.That(changes, Is.EqualTo(2));
        }));
    }
}
