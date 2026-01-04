using Microsoft.Xna.Framework;
using PsychoEngine;
using PsychoEngine.Input;

namespace SampleGame;

public static class Program
{
    public static void Main()
    {
        using PyGame engine = new("Sample Game", 1280, 720);
        engine.Run();
    }
}