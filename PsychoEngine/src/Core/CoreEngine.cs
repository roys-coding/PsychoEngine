using Hexa.NET.ImGui;
using ImGuiXNA;

namespace PsychoEngine.Core;

public class CoreEngine : Game
{
    private ImGuiRenderer _imGuiRenderer;
    
    public CoreEngine(string windowTitle, int windowWidth, int windowHeight)
    {
        GraphicsDeviceManager deviceManager = new(this);
        deviceManager.PreferredBackBufferWidth  = windowWidth;
        deviceManager.PreferredBackBufferHeight = windowHeight;
        IsMouseVisible                          = true;

        Window.Title = windowTitle;
    }

    protected override void Initialize()
    {
        _imGuiRenderer = new ImGuiRenderer(this);
        _imGuiRenderer.Initialize();
        
        base.Initialize();
    }

    override protected void LoadContent()
    {
        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        
        _imGuiRenderer.NewFrame(gameTime);
        ImGui.ShowDemoWindow();
        _imGuiRenderer.Render();
        
        base.Draw(gameTime);
    }
    
    protected override void UnloadContent()
    {
        _imGuiRenderer.Dispose();

        base.UnloadContent();
    }
}