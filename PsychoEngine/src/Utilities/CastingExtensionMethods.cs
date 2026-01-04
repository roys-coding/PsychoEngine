using NVector2 = System.Numerics.Vector2;
using NVector3 = System.Numerics.Vector3;
using NVector4 = System.Numerics.Vector4;

namespace PsychoEngine.Utilities;

internal static class CastingExtensionMethods
{
    public static NVector2 ToNumerics(this Point point)
    {
        return new NVector2(point.X, point.Y);
    }

    public static Vector2 ToVector(this Point point)
    {
        return new Vector2(point.X, point.Y);
    }
    
    public static NVector2 ToNumerics(this Vector2 vector)
    {
        return new NVector2(vector.X, vector.Y);
    }
    
    public static NVector3 ToNumerics(this Vector3 vector)
    {
        return new NVector3(vector.X, vector.Y, vector.Z);
    }
    
    public static NVector4 ToNumerics(this Vector4 vector)
    {
        return new NVector4(vector.X, vector.Y, vector.Z, vector.W);
    }

    public static Vector2 ToXna(this NVector2 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    public static Vector3 ToXna(this NVector3 vector)
    {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }

    public static Vector4 ToXna(this NVector4 vector)
    {
        return new Vector4(vector.X, vector.Y, vector.Z, vector.W);
    }
}