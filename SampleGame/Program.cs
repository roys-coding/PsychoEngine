using PsychoEngine.Core;

namespace SampleGame;

public static class Program
{
    public static void Main()
    {
        using CoreEngine engine = new("Sample Game", 1280, 720);
        engine.Run();
    }
}