using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PsychoEngine;
using PsychoEngine.Input;

namespace SampleGame;

public static class Program
{
    public static void Main()
    {
        FNALoggerEXT.LogInfo  += msg => PyConsole.LogInfo(msg, "fna");
        FNALoggerEXT.LogWarn  += msg => PyConsole.LogWarning(msg, "fna");
        FNALoggerEXT.LogError += msg => PyConsole.LogError(msg, "fna");
        
        using PyGame engine = new("Sample Game", 1280, 720);
        engine.Run();
    }
}