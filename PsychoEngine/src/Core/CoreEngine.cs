using Hexa.NET.ImGui;
using ImGuiXNA;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Core;

public class CoreEngine : Game
{
    private          ImGuiRenderer?        _imGuiRenderer;
    private readonly GraphicsDeviceManager _deviceManager;
    
    public CoreEngine(string windowTitle, int windowWidth, int windowHeight)
    {
        _deviceManager                           = new GraphicsDeviceManager(this);
        _deviceManager.PreferredBackBufferWidth  = windowWidth;
        _deviceManager.PreferredBackBufferHeight = windowHeight;
        IsMouseVisible                           = true;
        Window.AllowUserResizing                 = true;

        Window.Title   = windowTitle;
    }

    protected override void Initialize()
    {
        _imGuiRenderer = new ImGuiRenderer(this);
        _imGuiRenderer.Initialize();
        
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        KeyboardState keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.T))
        {
            _deviceManager.SynchronizeWithVerticalRetrace = !_deviceManager.SynchronizeWithVerticalRetrace;
            _deviceManager.ApplyChanges();
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        
        _imGuiRenderer?.NewFrame(gameTime);
        ImGui.ShowDemoWindow();
        ImGui.Text($"Game Time: {gameTime.ElapsedGameTime}");
        ImGui.Text($"VSync: {_deviceManager.SynchronizeWithVerticalRetrace}");
        _imGuiRenderer?.Render();
        
        base.Draw(gameTime);
    }
    
    protected override void UnloadContent()
    {
        _imGuiRenderer?.Dispose();

        base.UnloadContent();
    }
}