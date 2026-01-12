using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PsychoEngine;
using PsychoEngine.Graphics;
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