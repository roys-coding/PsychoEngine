using Microsoft.Xna.Framework;

namespace PsychoEngine.Core;

public class CoreEngine : Game
{
    public CoreEngine(string windowTitle, int windowWidth, int windowHeight)
    {
        GraphicsDeviceManager deviceManager = new(this);
        deviceManager.PreferredBackBufferWidth = windowWidth;
        deviceManager.PreferredBackBufferHeight = windowHeight;
        
        Window.Title = windowTitle;
    }
}