using System;
using System.Collections.Generic;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Formatting;

[TestFixture]
public sealed class InputCaretMapperTests
{
    [Test]
    public void MapsGeneralAndNumeralMiddlePositionsThroughRawCoordinates()
    {
        var general = new BootstrapGeneralInputFormatter(new BootstrapGeneralFormatOptions { Blocks = new[] { 4, 4 } });
        var numeral = new BootstrapNumeralInputFormatter(new BootstrapNumeralFormatOptions());

        Assert.Multiple((Action)(() =>
        {
            Assert.That(InputCaretMapper.ToRawPosition(general, "1234 5678", 2), Is.EqualTo(2));
            Assert.That(InputCaretMapper.ToRawPosition(general, "1234 5678", 5), Is.EqualTo(4));
            Assert.That(InputCaretMapper.ToRawPosition(numeral, "1,234,567", 5), Is.EqualTo(4));
            Assert.That(InputCaretMapper.ToFormattedPosition(numeral, "1234567", 4), Is.EqualTo(5));
        }));
    }

    [Test]
    public void PrefixAndEndPositionsMapToStableBounds()
    {
        var formatter = new BootstrapNumeralInputFormatter(new BootstrapNumeralFormatOptions { Prefix = "$" });
        var general = new BootstrapGeneralInputFormatter(new BootstrapGeneralFormatOptions
        {
            Prefix = "VN",
            Blocks = new[] { 4, 4 }
        });
        const string display = "$1,234";
        const string generalDisplay = "VN1234 5678";

        Assert.Multiple((Action)(() =>
        {
            Assert.That(InputCaretMapper.ToRawPosition(formatter, display, 0), Is.Zero);
            Assert.That(InputCaretMapper.ToRawPosition(formatter, display, 1), Is.Zero);
            Assert.That(InputCaretMapper.ToRawPosition(formatter, display, display.Length), Is.EqualTo(4));
            Assert.That(InputCaretMapper.ToFormattedPosition(formatter, "1234", 4), Is.EqualTo(display.Length));
            Assert.That(InputCaretMapper.ToRawPosition(general, generalDisplay, 0), Is.Zero);
            Assert.That(InputCaretMapper.ToRawPosition(general, generalDisplay, 1), Is.Zero);
            Assert.That(InputCaretMapper.ToRawPosition(general, generalDisplay, 2), Is.Zero);
        }));
    }

    [Test]
    public void RepresentativeFormattersAlwaysReturnPositionsWithinFinalDisplay()
    {
        var cases = new List<(IInputFormatter Formatter, string Raw)>
        {
            (new BootstrapGeneralInputFormatter(new BootstrapGeneralFormatOptions { Blocks = new[] { 4, 4 } }), "12345678"),
            (new BootstrapNumeralInputFormatter(new BootstrapNumeralFormatOptions()), "1234567.89"),
            (new BootstrapDateInputFormatter(new BootstrapDateFormatOptions()), "31082026"),
            (new BootstrapCreditCardInputFormatter(new BootstrapCreditCardFormatOptions()), "4111111111111111")
        };

        foreach (var item in cases)
        {
            var display = item.Formatter.Format(item.Raw);
            for (var position = 0; position <= display.Length; position++)
            {
                var rawPosition = InputCaretMapper.ToRawPosition(item.Formatter, display, position);
                var finalPosition = InputCaretMapper.ToFormattedPosition(item.Formatter, item.Raw, rawPosition);
                Assert.That(rawPosition, Is.InRange(0, item.Formatter.Unformat(display).Length));
                Assert.That(finalPosition, Is.InRange(0, display.Length));
            }
        }
    }

    [Test]
    public void InvalidCharactersAndSelectionsCannotProduceOutOfRangePositions()
    {
        var formatter = new BootstrapCreditCardInputFormatter(new BootstrapCreditCardFormatOptions());
        const string candidate = "4111-xx1111";
        var start = InputCaretMapper.ToRawPosition(formatter, candidate, 3);
        var end = InputCaretMapper.ToRawPosition(formatter, candidate, 9);
        var display = formatter.Format(formatter.Unformat(candidate));

        Assert.Multiple((Action)(() =>
        {
            Assert.That(start, Is.LessThanOrEqualTo(end));
            Assert.That(InputCaretMapper.ToFormattedPosition(formatter, formatter.Unformat(candidate), start), Is.InRange(0, display.Length));
            Assert.That(InputCaretMapper.ToFormattedPosition(formatter, formatter.Unformat(candidate), end), Is.InRange(0, display.Length));
        }));
    }
}
