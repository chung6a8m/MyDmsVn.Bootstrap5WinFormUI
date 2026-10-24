using System;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Formatting;

[TestFixture]
public sealed class QuickDateParserTests
{
    private static readonly DateTime Reference = new DateTime(2026, 10, 23, 23, 59, 59, DateTimeKind.Local);
    private static DateInputProfile Required => new DateInputProfile(false, new DateTime(1900, 1, 1), new DateTime(2100, 12, 31));

    [TestCase("5", 2026, 10, 5)]
    [TestCase("5/11", 2026, 11, 5)]
    [TestCase("5/11/2027", 2027, 11, 5)]
    [TestCase("29/2/2028", 2028, 2, 29)]
    [TestCase("1/1/2000", 2000, 1, 1)]
    public void Explicit_DMY_forms_complete_only_at_parse(string raw, int year, int month, int day)
    {
        var result = QuickDateParser.TryParsePending(raw, Required, Reference);
        Assert.That(result.State, Is.EqualTo(InputParseState.Valid));
        Assert.That(result.Value, Is.EqualTo(new DateTime(year, month, day)));
        Assert.That(result.Value!.Value.Kind, Is.EqualTo(DateTimeKind.Unspecified));
        Assert.That(result.Value.Value.TimeOfDay, Is.EqualTo(TimeSpan.Zero));
    }

    [TestCase("0511")]
    [TestCase("05112027")]
    [TestCase("5/11/27")]
    [TestCase("31/11")]
    [TestCase("29/2/2027")]
    [TestCase("5//2027")]
    [TestCase("5/11/")]
    [TestCase("5/11/2027 12:00")]
    public void Unsupported_or_impossible_forms_do_not_produce_a_value(string raw)
    {
        var result = QuickDateParser.TryParsePending(raw, Required, Reference);
        Assert.That(result.State, Is.Not.EqualTo(InputParseState.Valid));
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Failure, Is.Not.Null);
    }

    [Test]
    public void Blank_and_bounds_follow_profile()
    {
        Assert.That(QuickDateParser.TryParsePending("", Required, Reference).State, Is.EqualTo(InputParseState.Empty));
        Assert.That(QuickDateParser.TryParsePending("", new DateInputProfile(true, Required.Minimum, Required.Maximum), Reference).Value, Is.Null);
        Assert.That(QuickDateParser.TryParsePending("31/12/1899", Required, Reference).Failure?.Code, Is.EqualTo(InputParseFailureCode.OutOfRange));
        Assert.That(QuickDateParser.TryParsePending("1/1/2101", Required, Reference).Failure?.Code, Is.EqualTo(InputParseFailureCode.OutOfRange));
    }

    [Test]
    public void Captured_reference_controls_midnight_and_year_rollover()
    {
        var end = new DateTime(2026, 12, 31, 23, 59, 59);
        var start = new DateTime(2027, 1, 1, 0, 0, 0);
        Assert.That(QuickDateParser.TryParsePending("5", Required, end).Value, Is.EqualTo(new DateTime(2026, 12, 5)));
        Assert.That(QuickDateParser.TryParsePending("5", Required, start).Value, Is.EqualTo(new DateTime(2027, 1, 5)));
    }

    [Test]
    public void Segment_steps_cross_day_boundaries_but_refuse_impossible_month_or_year()
    {
        Assert.That(QuickDateParser.Step(new DateTime(2026, 12, 31), DateSegment.Day, 1, Required).Value, Is.EqualTo(new DateTime(2027, 1, 1)));
        Assert.That(QuickDateParser.Step(new DateTime(2026, 3, 31), DateSegment.Day, -1, Required).Value, Is.EqualTo(new DateTime(2026, 3, 30)));
        Assert.That(QuickDateParser.Step(new DateTime(2026, 1, 31), DateSegment.Month, 1, Required).State, Is.EqualTo(InputParseState.Invalid));
        Assert.That(QuickDateParser.Step(new DateTime(2028, 2, 29), DateSegment.Year, 1, Required).State, Is.EqualTo(InputParseState.Invalid));
        Assert.That(QuickDateParser.Step(new DateTime(2027, 2, 28), DateSegment.Year, 1, Required).Value, Is.EqualTo(new DateTime(2028, 2, 28)));
    }
}
