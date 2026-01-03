using PsychoEngine;
using PsychoEngine.Input;

namespace SampleGame;

public static class Program
{
    public static void Main()
    {
        using PyGame engine = new("Sample Game", 1000, 600);
        engine.Run();
    }
}