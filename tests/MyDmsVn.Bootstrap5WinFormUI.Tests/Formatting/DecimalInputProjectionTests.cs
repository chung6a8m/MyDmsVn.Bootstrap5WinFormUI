using System;
using System.Globalization;
using MyDmsVn.Bootstrap5WinFormUI.Formatting;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Formatting;

[TestFixture]
public sealed class DecimalInputProjectionTests
{
    private static DecimalInputProfile Profile(CultureInfo culture) =>
        new DecimalInputProfile(culture, false, 4, -999999999m, 999999999m);

    [Test]
    public void Generated_grouping_round_trips_every_boundary_and_selection()
    {
        var projection = DecimalInputProjection.Project("1234.50", Profile(CultureInfo.GetCultureInfo("en-US")));
        Assert.That(projection.CandidateText, Is.EqualTo("1234.50"));
        Assert.That(projection.DisplayText, Is.EqualTo("1,234.50"));
        Assert.That(projection.IsIdentity, Is.False);
        Assert.That(projection.ToDisplayBoundary(1, false), Is.EqualTo(1));
        Assert.That(projection.ToDisplayBoundary(1), Is.EqualTo(2));
        Assert.That(projection.ToCandidateBoundary(1), Is.EqualTo(1));
        Assert.That(projection.ToCandidateBoundary(2), Is.EqualTo(1));
        Assert.That(projection.IsGeneratedCharacter(1), Is.True);
        Assert.That(projection.IsGeneratedCharacter(2), Is.False);
        for (var i = 0; i <= projection.CandidateText.Length; i++)
        {
            Assert.That(projection.ToCandidateBoundary(projection.ToDisplayBoundary(i, false)), Is.EqualTo(i));
            Assert.That(projection.ToCandidateBoundary(projection.ToDisplayBoundary(i)), Is.EqualTo(i));
        }
        var selectionStart = projection.ToCandidateBoundary(0);
        var selectionEnd = projection.ToCandidateBoundary(5);
        Assert.That(projection.CandidateText.Substring(selectionStart, selectionEnd - selectionStart), Is.EqualTo("1234"));
    }

    [Test]
    public void External_grouping_remains_source_text_even_when_display_matches_generated_grouping()
    {
        var profile = Profile(CultureInfo.GetCultureInfo("en-US"));
        var generated = DecimalInputProjection.Project("1234", profile);
        var external = DecimalInputProjection.Project("1,234", profile);
        Assert.That(generated.DisplayText, Is.EqualTo("1,234"));
        Assert.That(external.DisplayText, Is.EqualTo(generated.DisplayText));
        Assert.That(generated.IsGeneratedCharacter(1), Is.True);
        Assert.That(external.IsIdentity, Is.True);
        Assert.That(external.CandidateText, Is.EqualTo("1,234"));
        Assert.That(external.IsGeneratedCharacter(1), Is.False);
        Assert.That(DecimalInputParser.TryParsePending(external.CandidateText, profile).State, Is.EqualTo(InputParseState.Invalid));
    }

    [Test]
    public void Culture_and_literal_multi_character_tokens_are_preserved()
    {
        var vi = DecimalInputProjection.Project("1234567,50", Profile(CultureInfo.GetCultureInfo("vi-VN")));
        Assert.That(vi.DisplayText, Is.EqualTo("1.234.567,50"));
        Assert.That(vi.ToCandidateBoundary(2), Is.EqualTo(1));
        Assert.That(vi.ToCandidateBoundary(6), Is.EqualTo(4));
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
        culture.NumberFormat.PositiveSign = "plus";
        culture.NumberFormat.NegativeSign = "minus";
        culture.NumberFormat.NumberDecimalSeparator = "::";
        var profile = Profile(culture);
        var positive = DecimalInputProjection.Project("plus1234::50", profile);
        Assert.That(positive.DisplayText, Is.EqualTo("plus1,234::50"));
        Assert.That(positive.IsGeneratedCharacter(5), Is.True);
        Assert.That(positive.ToCandidateBoundary(6), Is.EqualTo(5));
        Assert.That(DecimalInputProjection.Project("minus1234::50", profile).DisplayText, Is.EqualTo("minus1,234::50"));
    }

    [Test]
    public void Invalid_intermediate_and_outer_whitespace_fall_back_to_identity()
    {
        var profile = Profile(CultureInfo.GetCultureInfo("en-US"));
        foreach (var raw in new[] { "", "-", ".", "+.", "12x34", "1,234", "1234.", " 1234 ", "1e4" })
        {
            var projection = DecimalInputProjection.Project(raw, profile);
            Assert.That(projection.IsIdentity, Is.True, raw);
            Assert.That(projection.DisplayText, Is.EqualTo(raw), raw);
            Assert.That(projection.CandidateText, Is.EqualTo(raw), raw);
            for (var i = 0; i <= raw.Length; i++)
            {
                Assert.That(projection.ToDisplayBoundary(i), Is.EqualTo(i), raw);
                Assert.That(projection.ToCandidateBoundary(i), Is.EqualTo(i), raw);
            }
        }
    }

    [Test]
    public void Group_size_zero_stops_grouping_and_repeated_projection_does_not_mutate_inputs()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
        culture.NumberFormat.NumberGroupSizes = new[] { 3, 2, 0 };
        var profile = Profile(culture);
        var original = "123456789";
        var first = DecimalInputProjection.Project(original, profile);
        var second = DecimalInputProjection.Project(original, profile);
        Assert.That(first.DisplayText, Is.EqualTo("1234,56,789"));
        Assert.That(second.DisplayText, Is.EqualTo(first.DisplayText));
        Assert.That(first.CandidateText, Is.EqualTo(original));
        Assert.That(second.ToDisplayBoundary(4), Is.EqualTo(first.ToDisplayBoundary(4)));
        Assert.That(profile.Culture.NumberFormat.NumberGroupSizes, Is.EqualTo(new[] { 3, 2, 0 }));
    }

    [Test]
    public void Multi_character_generated_separator_maps_each_display_boundary_to_one_candidate_boundary()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
        culture.NumberFormat.NumberGroupSeparator = "__";
        culture.NumberFormat.NumberDecimalSeparator = "::";
        var projection = DecimalInputProjection.Project("1234::5", Profile(culture));
        Assert.That(projection.DisplayText, Is.EqualTo("1__234::5"));
        Assert.That(projection.ToDisplayBoundary(1, false), Is.EqualTo(1));
        Assert.That(projection.ToDisplayBoundary(1), Is.EqualTo(3));
        Assert.That(projection.ToCandidateBoundary(1), Is.EqualTo(1));
        Assert.That(projection.ToCandidateBoundary(2), Is.EqualTo(1));
        Assert.That(projection.ToCandidateBoundary(3), Is.EqualTo(1));
        Assert.That(projection.IsGeneratedCharacter(1), Is.True);
        Assert.That(projection.IsGeneratedCharacter(2), Is.True);
        Assert.That(projection.IsGeneratedCharacter(3), Is.False);
    }

    [Test]
    public void Boundary_arguments_are_checked_without_changing_the_result()
    {
        var projection = DecimalInputProjection.Project("1234", Profile(CultureInfo.GetCultureInfo("en-US")));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => projection.ToDisplayBoundary(-1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => projection.ToDisplayBoundary(5)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => projection.ToCandidateBoundary(-1)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => projection.ToCandidateBoundary(6)));
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => projection.IsGeneratedCharacter(5)));
        Assert.That(projection.DisplayText, Is.EqualTo("1,234"));
    }
}
