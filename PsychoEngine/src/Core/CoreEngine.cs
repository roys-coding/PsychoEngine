using Hexa.NET.ImGui;
using ImGuiXNA;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Core;

public class CoreEngine : Game
{
    private readonly ImGuiManager          _imGuiManager;
    private readonly GraphicsDeviceManager _deviceManager;
    
    public CoreEngine(string windowTitle, int windowWidth, int windowHeight)
    {
        _deviceManager                           = new GraphicsDeviceManager(this);
        _deviceManager.PreferredBackBufferWidth  = windowWidth;
        _deviceManager.PreferredBackBufferHeight = windowHeight;
        IsMouseVisible                           = true;
        Window.AllowUserResizing                 = true;

        Window.Title   = windowTitle;
        
        _imGuiManager = new ImGuiManager(this);

        _imGuiManager.OnLayout += (_, _) =>
                                  {
                                      ImGui.ShowDemoWindow();
                                      ImGui.Text($"{Fonts.Lucide.Gamepad} Gamepad");
                                      ImGui.Text($"{Fonts.Lucide.Star} Star");
                                  };
    }

    protected override void Initialize()
    {
        _imGuiManager.Initialize();
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _imGuiManager.Draw(gameTime);
        
        base.Draw(gameTime);
    }
    
    protected override void UnloadContent()
    {
        _imGuiManager.Terminate();

        base.UnloadContent();
    }
}