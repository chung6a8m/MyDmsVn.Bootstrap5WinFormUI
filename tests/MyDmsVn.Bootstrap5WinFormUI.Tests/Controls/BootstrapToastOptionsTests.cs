using System;
using System.Linq;
using System.Reflection;
using MyDmsVn.Bootstrap5WinFormUI.Controls;
using NUnit.Framework;

namespace MyDmsVn.Bootstrap5WinFormUI.Tests.Controls;

[TestFixture]
public sealed class BootstrapToastOptionsTests
{
    [Test]
    public void Options_DefaultsMatchToastContract()
    {
        var options = new BootstrapToastOptions();

        Assert.Multiple((Action)(() =>
        {
            Assert.That(options.Title, Is.Empty);
            Assert.That(options.Text, Is.Empty);
            Assert.That(options.Variant, Is.EqualTo(BootstrapVariant.Primary));
            Assert.That(options.Icon, Is.Null);
            Assert.That(options.Dismissible, Is.True);
            Assert.That(options.AutoHide, Is.True);
            Assert.That(options.AutoHideDelay, Is.EqualTo(5000));
            Assert.That(options.AnimationDuration, Is.EqualTo(200));
            Assert.That(options.IncludeInHistory, Is.True);
        }));
    }

    [Test]
    public void Options_NullStringsNormalizeToEmpty()
    {
        var options = new BootstrapToastOptions { Title = null!, Text = null! };

        Assert.Multiple((Action)(() =>
        {
            Assert.That(options.Title, Is.Empty);
            Assert.That(options.Text, Is.Empty);
        }));
    }

    [Test]
    public void Options_InvalidValuesAreRejectedWithoutMutation()
    {
        var options = new BootstrapToastOptions
        {
            Variant = BootstrapVariant.Success,
            AutoHideDelay = 2500,
            AnimationDuration = 350
        };

        Assert.Multiple((Action)(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => options.Variant = (BootstrapVariant)999));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => options.AutoHideDelay = 0));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => options.AnimationDuration = -1));
            Assert.That(options.Variant, Is.EqualTo(BootstrapVariant.Success));
            Assert.That(options.AutoHideDelay, Is.EqualTo(2500));
            Assert.That(options.AnimationDuration, Is.EqualTo(350));
        }));
    }

    [Test]
    public void HistoryItem_HasInternalConstructorAndImmutableSnapshotProperties()
    {
        var publicConstructors = typeof(BootstrapToastHistoryItem).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var writableProperties = typeof(BootstrapToastHistoryItem)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is not null)
            .ToArray();
        var id = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 8, 29, 12, 30, 0, TimeSpan.Zero);
        var item = new BootstrapToastHistoryItem(
            id,
            createdAt,
            "Saved",
            "Order saved",
            BootstrapVariant.Success,
            isRead: false);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(publicConstructors, Is.Empty);
            Assert.That(writableProperties, Is.Empty);
            Assert.That(item.Id, Is.EqualTo(id));
            Assert.That(item.CreatedAtUtc, Is.EqualTo(createdAt));
            Assert.That(item.CreatedAtUtc.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(item.Title, Is.EqualTo("Saved"));
            Assert.That(item.Text, Is.EqualTo("Order saved"));
            Assert.That(item.Variant, Is.EqualTo(BootstrapVariant.Success));
            Assert.That(item.IsRead, Is.False);
        }));
    }

    [Test]
    public void HistoryItem_NormalizesStringsAndRejectsNonUtcTimestampOrInvalidVariant()
    {
        var id = Guid.NewGuid();
        var utc = DateTimeOffset.UtcNow;

        var normalized = new BootstrapToastHistoryItem(
            id,
            utc,
            null!,
            null!,
            BootstrapVariant.Primary,
            isRead: true);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(normalized.Title, Is.Empty);
            Assert.That(normalized.Text, Is.Empty);
            Assert.Throws<ArgumentException>((Action)(() => new BootstrapToastHistoryItem(
                id,
                utc.ToOffset(TimeSpan.FromHours(7)),
                "",
                "",
                BootstrapVariant.Primary,
                false)));
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => new BootstrapToastHistoryItem(
                id,
                utc,
                "",
                "",
                (BootstrapVariant)999,
                false)));
        }));
    }
}
