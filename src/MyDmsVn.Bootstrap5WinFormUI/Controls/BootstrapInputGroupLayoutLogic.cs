using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace MyDmsVn.Bootstrap5WinFormUI.Controls;

internal readonly struct BootstrapInputGroupLayoutItem
{
    internal BootstrapInputGroupLayoutItem(int preferredWidth, int minimumWidth, bool stretch)
    {
        if (preferredWidth < 0) throw new ArgumentOutOfRangeException(nameof(preferredWidth));
        if (minimumWidth < 0) throw new ArgumentOutOfRangeException(nameof(minimumWidth));
        PreferredWidth = Math.Max(preferredWidth, minimumWidth);
        MinimumWidth = minimumWidth;
        Stretch = stretch;
    }

    internal int PreferredWidth { get; }
    internal int MinimumWidth { get; }
    internal bool Stretch { get; }
}

internal sealed class BootstrapInputGroupLayoutResult
{
    internal BootstrapInputGroupLayoutResult(IReadOnlyList<Rectangle> bounds, int preferredWidth, int preferredHeight)
    {
        Bounds = bounds;
        PreferredWidth = preferredWidth;
        PreferredHeight = preferredHeight;
    }

    internal IReadOnlyList<Rectangle> Bounds { get; }
    internal int PreferredWidth { get; }
    internal int PreferredHeight { get; }
}

internal static class BootstrapInputGroupLayoutLogic
{
    internal static BootstrapInputGroupLayoutResult Calculate(
        IReadOnlyList<BootstrapInputGroupLayoutItem> items,
        int clientWidth,
        int rowHeight,
        int seamOverlap,
        bool rightToLeft)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (clientWidth < 0) throw new ArgumentOutOfRangeException(nameof(clientWidth));
        if (rowHeight < 0) throw new ArgumentOutOfRangeException(nameof(rowHeight));
        if (seamOverlap < 0) throw new ArgumentOutOfRangeException(nameof(seamOverlap));
        if (items.Count == 0)
        {
            return new BootstrapInputGroupLayoutResult(Array.Empty<Rectangle>(), 0, rowHeight);
        }

        var widths = AllocateWidths(items, SaturatingAdd(clientWidth, SaturatingMultiply(seamOverlap, items.Count - 1)));
        var rectangles = new Rectangle[items.Count];
        var position = 0;
        for (var i = 0; i < widths.Length; i++)
        {
            var width = Math.Min(widths[i], Math.Max(0, clientWidth - position));
            rectangles[i] = new Rectangle(position, 0, Math.Max(0, width), rowHeight);
            position = Math.Max(0, position + width - seamOverlap);
        }

        if (rightToLeft)
        {
            for (var i = 0; i < rectangles.Length; i++)
            {
                var value = rectangles[i];
                rectangles[i] = new Rectangle(clientWidth - value.Right, value.Y, value.Width, value.Height);
            }
        }

        long preferred = 0;
        for (var i = 0; i < items.Count; i++) preferred += items[i].PreferredWidth;
        preferred -= (long)seamOverlap * (items.Count - 1);
        return new BootstrapInputGroupLayoutResult(
            new ReadOnlyCollection<Rectangle>(rectangles),
            (int)Math.Max(0, Math.Min(int.MaxValue, preferred)),
            rowHeight);
    }

    private static int[] AllocateWidths(IReadOnlyList<BootstrapInputGroupLayoutItem> items, int budget)
    {
        var widths = new int[items.Count];
        long natural = 0;
        long allMinimum = 0;
        var stretchCount = 0;
        for (var i = 0; i < items.Count; i++)
        {
            widths[i] = items[i].Stretch ? items[i].MinimumWidth : items[i].PreferredWidth;
            natural += widths[i];
            allMinimum += items[i].MinimumWidth;
            if (items[i].Stretch) stretchCount++;
        }

        if (budget >= natural)
        {
            if (stretchCount > 0)
            {
                DistributeEqually(widths, items, budget - (int)Math.Min(int.MaxValue, natural), stretchOnly: true);
            }
            return widths;
        }

        if (budget >= allMinimum)
        {
            var shrink = (int)(natural - budget);
            var capacities = new int[items.Count];
            var totalCapacity = 0;
            for (var i = 0; i < items.Count; i++)
            {
                if (!items[i].Stretch)
                {
                    capacities[i] = items[i].PreferredWidth - items[i].MinimumWidth;
                    totalCapacity += capacities[i];
                }
            }
            ApplyProportionalReduction(widths, capacities, Math.Min(shrink, totalCapacity), totalCapacity);
            return widths;
        }

        return AllocateProportionally(items.Select(item => item.MinimumWidth).ToArray(), budget);
    }

    private static void DistributeEqually(int[] widths, IReadOnlyList<BootstrapInputGroupLayoutItem> items, int surplus, bool stretchOnly)
    {
        var eligible = Enumerable.Range(0, items.Count).Where(index => !stretchOnly || items[index].Stretch).ToArray();
        if (eligible.Length == 0) return;
        var each = surplus / eligible.Length;
        var remainder = surplus % eligible.Length;
        for (var i = 0; i < eligible.Length; i++) widths[eligible[i]] += each + (i < remainder ? 1 : 0);
    }

    private static void ApplyProportionalReduction(int[] widths, int[] capacities, int reduction, int totalCapacity)
    {
        if (reduction <= 0 || totalCapacity <= 0) return;
        var applied = 0;
        for (var i = 0; i < widths.Length; i++)
        {
            var share = (int)((long)reduction * capacities[i] / totalCapacity);
            widths[i] -= share;
            applied += share;
        }
        for (var i = 0; applied < reduction && i < widths.Length; i++)
        {
            if (capacities[i] > 0 && widths[i] > 0)
            {
                widths[i]--;
                applied++;
            }
        }
    }

    private static int[] AllocateProportionally(int[] weights, int budget)
    {
        var result = new int[weights.Length];
        var total = weights.Sum();
        if (budget <= 0 || total <= 0) return result;
        var fractions = new long[weights.Length];
        var allocated = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            var numerator = (long)budget * weights[i];
            result[i] = (int)(numerator / total);
            fractions[i] = numerator % total;
            allocated += result[i];
        }
        foreach (var index in Enumerable.Range(0, weights.Length).OrderByDescending(i => fractions[i]).ThenBy(i => i))
        {
            if (allocated >= budget) break;
            result[index]++;
            allocated++;
        }
        return result;
    }

    private static int SaturatingAdd(int left, int right) => left > int.MaxValue - right ? int.MaxValue : left + right;
    private static int SaturatingMultiply(int left, int right) => left == 0 || right == 0 ? 0 : (left > int.MaxValue / right ? int.MaxValue : left * right);
}
