namespace PsychoEngine.Utils;

public static class MathExtensionMethods
{
    extension(Point pointA)
    {
        public float Distance(Point pointB)
        {
            float dX = pointB.X - pointA.X; 
            float dY = pointB.Y - pointA.Y;
            
            return MathF.Sqrt(dX * dX + dY * dY);
        }
    }
}