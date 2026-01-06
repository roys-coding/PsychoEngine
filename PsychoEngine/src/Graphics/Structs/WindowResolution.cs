using System.Diagnostics.CodeAnalysis;

namespace PsychoEngine.Graphics;

public struct WindowResolution : IEquatable<WindowResolution>, IComparable<WindowResolution>
{
    public int Width  { get; set; }
    public int Height { get; set; }

    public float AspectRatio => (float)Width / Height;

    public WindowResolution(int width, int height)
    {
        Width  = width;
        Height = height;
    }

    public override string ToString()
    {
        return $"({Width} x {Height})";
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is WindowResolution other && Equals(other);
    }

    public bool Equals(WindowResolution other)
    {
        return Width == other.Width && Height == other.Height;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    public int CompareTo(WindowResolution other)
    {
        if (Width == other.Width && Height == other.Height)
        {
            return 0;
        }

        float diagonalA = MathF.Sqrt(Width       * Width       + Height       * Height);
        float diagonalB = MathF.Sqrt(other.Width * other.Width + other.Height * other.Height);

        // ReSharper disable CompareOfFloatsByEqualityOperator
        if (diagonalA == diagonalB)
        {
            // Select the tallest resolution if both resolutions have the same diagonal.
            return Width.CompareTo(other.Width);
        }
        // ReSharper restore CompareOfFloatsByEqualityOperator

        // Select resolution with the biggest diagonal.
        return diagonalA.CompareTo(diagonalB);
    }

    public static bool operator >(WindowResolution left, WindowResolution right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <(WindowResolution left, WindowResolution right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(WindowResolution left, WindowResolution right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(WindowResolution left, WindowResolution right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator ==(WindowResolution left, WindowResolution right)
    {
        return left.CompareTo(right) == 0;
    }

    public static bool operator !=(WindowResolution left, WindowResolution right)
    {
        return left.CompareTo(right) != 0;
    }
}