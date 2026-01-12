using System.Diagnostics.CodeAnalysis;

namespace PsychoEngine.Graphics;

public struct GraphicsResolution : IEquatable<GraphicsResolution>, IComparable<GraphicsResolution>
{
    public int Width  { get; set; }
    public int Height { get; set; }

    public float AspectRatio => (float)Width / Height;
    public int Area => Width * Height;

    public GraphicsResolution(int width, int height)
    {
        Width  = width;
        Height = height;
    }

    public Vector2 ToVector2()
    {
        return new Vector2(Width, Height);
    }

    public Point ToPoint()
    {
        return new Point(Width, Height);
    }

    public void Deconstruct(out int width, out int height)
    {
        width  = Width;
        height = Height;
    }

    public override string ToString()
    {
        return $"({Width} x {Height})";
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is GraphicsResolution other && Equals(other);
    }

    public bool Equals(GraphicsResolution other)
    {
        return Width == other.Width && Height == other.Height;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    public int CompareTo(GraphicsResolution other)
    {
        // Wider resolutions take priority.
        return other.Width == Width && other.Height == Height ? 0 :
               Width > other.Width                            ? 1 :
               Height > other.Height                          ? 1 : -1;
    }

    public static bool operator >(GraphicsResolution left, GraphicsResolution right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <(GraphicsResolution left, GraphicsResolution right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(GraphicsResolution left, GraphicsResolution right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(GraphicsResolution left, GraphicsResolution right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator ==(GraphicsResolution left, GraphicsResolution right)
    {
        return left.CompareTo(right) == 0;
    }

    public static bool operator !=(GraphicsResolution left, GraphicsResolution right)
    {
        return left.CompareTo(right) != 0;
    }
}