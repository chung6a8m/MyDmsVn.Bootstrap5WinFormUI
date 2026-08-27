using System;
using System.Drawing;

namespace MyDmsVn.Bootstrap5WinFormUI.Rendering;

/// <summary>
/// Describes independent circular radii for the four corners of a rectangle.
/// </summary>
public readonly struct CornerRadius : IEquatable<CornerRadius>
{
    /// <summary>
    /// Gets a radius value with all corners square.
    /// </summary>
    public static CornerRadius Empty { get; } = new CornerRadius(0f);

    /// <summary>
    /// Initializes all corners with the same radius.
    /// </summary>
    public CornerRadius(float radius)
        : this(radius, radius, radius, radius)
    {
    }

    /// <summary>
    /// Initializes independent corner radii in clockwise order.
    /// </summary>
    public CornerRadius(float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        TopLeft = ValidateRadius(topLeft, nameof(topLeft));
        TopRight = ValidateRadius(topRight, nameof(topRight));
        BottomRight = ValidateRadius(bottomRight, nameof(bottomRight));
        BottomLeft = ValidateRadius(bottomLeft, nameof(bottomLeft));
    }

    /// <summary>
    /// Gets the top-left radius.
    /// </summary>
    public float TopLeft { get; }

    /// <summary>
    /// Gets the top-right radius.
    /// </summary>
    public float TopRight { get; }

    /// <summary>
    /// Gets the bottom-right radius.
    /// </summary>
    public float BottomRight { get; }

    /// <summary>
    /// Gets the bottom-left radius.
    /// </summary>
    public float BottomLeft { get; }

    /// <summary>
    /// Normalizes the radii so adjacent corners never overlap within the supplied bounds.
    /// </summary>
    public CornerRadius NormalizeTo(RectangleF bounds)
    {
        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return Empty;
        }

        var scale = 1f;
        scale = LimitScale(scale, bounds.Width, TopLeft + TopRight);
        scale = LimitScale(scale, bounds.Width, BottomLeft + BottomRight);
        scale = LimitScale(scale, bounds.Height, TopLeft + BottomLeft);
        scale = LimitScale(scale, bounds.Height, TopRight + BottomRight);

        if (scale >= 1f)
        {
            return this;
        }

        return new CornerRadius(
            TopLeft * scale,
            TopRight * scale,
            BottomRight * scale,
            BottomLeft * scale);
    }

    /// <inheritdoc />
    public bool Equals(CornerRadius other)
    {
        return TopLeft.Equals(other.TopLeft)
            && TopRight.Equals(other.TopRight)
            && BottomRight.Equals(other.BottomRight)
            && BottomLeft.Equals(other.BottomLeft);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is CornerRadius other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = TopLeft.GetHashCode();
            hash = (hash * 397) ^ TopRight.GetHashCode();
            hash = (hash * 397) ^ BottomRight.GetHashCode();
            hash = (hash * 397) ^ BottomLeft.GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Compares two corner-radius values for equality.
    /// </summary>
    public static bool operator ==(CornerRadius left, CornerRadius right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two corner-radius values for inequality.
    /// </summary>
    public static bool operator !=(CornerRadius left, CornerRadius right)
    {
        return !left.Equals(right);
    }

    private static float LimitScale(float currentScale, float available, float requested)
    {
        if (requested <= 0f || requested <= available)
        {
            return currentScale;
        }

        return Math.Min(currentScale, available / requested);
    }

    private static float ValidateRadius(float value, string parameterName)
    {
        if (value < 0f || float.IsNaN(value) || float.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Corner radius must be a finite, non-negative value.");
        }

        return value;
    }
}
