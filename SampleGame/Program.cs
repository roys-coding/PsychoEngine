using PsychoEngine;

namespace SampleGame;

public static class Program
{
    public static void Main()
    {
        using CoreEngine engine = new("Sample Game", 1000, 600);
        engine.Run();
    }
}