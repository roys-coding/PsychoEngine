using ImGuiXNA;
using Hexa.NET.ImGui;

namespace PsychoEngine.Core;

public class ImGuiManager
{
    private readonly ImGuiRenderer _renderer;
    
    public ImGuiManager(Game game)
    {
        _renderer = new ImGuiRenderer(game);
    }

    public void Initialize()
    {
        _renderer.Initialize();
    }

    public void Draw(GameTime gameTime)
    {
        _renderer.NewFrame(gameTime);
        ImGui.ShowDemoWindow();
        _renderer.Render();
    }

    public void Terminate()
    {
        _renderer.Dispose();
    }
}