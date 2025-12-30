using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using PsychoEngine.Input;

namespace PsychoEngine;

public class CoreEngine : Game
{
    public static CoreEngine Instance { get; private set; }

    public readonly ImGuiManager          ImGuiManager;
    private readonly GraphicsDeviceManager _deviceManager;

    public CoreEngine(string windowTitle, int windowWidth, int windowHeight)
    {
        if (Instance is not null)
        {
            throw new InvalidOperationException("CoreEngine has already been initialized.");
        }

        Instance = this;

        _deviceManager                                = new GraphicsDeviceManager(this);
        _deviceManager.PreferredBackBufferWidth       = windowWidth;
        _deviceManager.PreferredBackBufferHeight      = windowHeight;
        _deviceManager.SynchronizeWithVerticalRetrace = false;
        IsMouseVisible                                = false;
        Window.AllowUserResizing                      = true;

        Window.Title = windowTitle;

        ImGuiManager = new ImGuiManager(this);

        ImGuiManager.OnLayout     += ImGuiOnLayout;
    }

    private void ImGuiOnLayout(object? sender, EventArgs eventArgs)
    {
        const ImGuiDockNodeFlags dockFlags =
            ImGuiDockNodeFlags.PassthruCentralNode | ImGuiDockNodeFlags.NoDockingOverCentralNode;

        ImGui.DockSpaceOverViewport(dockFlags);
    }

    protected override void Initialize()
    {
        ImGuiManager.Initialize(_deviceManager.GraphicsDevice);
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GameKeyboard.Update(this);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        ImGuiManager.Draw(gameTime);

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        ImGuiManager.Terminate();

        base.UnloadContent();
    }
}