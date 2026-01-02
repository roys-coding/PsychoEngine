using System.Diagnostics.CodeAnalysis;
using Hexa.NET.ImGui;
using PsychoEngine.Input;

namespace PsychoEngine;

public class CoreEngine : Game
{
    private readonly GraphicsDeviceManager _deviceManager;

    [AllowNull]
    public static CoreEngine Instance { get; private set; }

    public ImGuiManager ImGuiManager { get; }

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

        ImGuiManager.OnLayout += ImGuiOnLayout;
    }

    private void ImGuiOnLayout(object? sender, EventArgs eventArgs)
    {
        const ImGuiDockNodeFlags dockFlags =
            ImGuiDockNodeFlags.PassthruCentralNode | ImGuiDockNodeFlags.NoDockingOverCentralNode;

        ImGui.DockSpaceOverViewport(dockFlags);

        ImGui.ShowDemoWindow();

        float targetElapsed                   = (float)TargetElapsedTime.TotalMilliseconds;
        bool  elapsedChanged                  = ImGui.SliderFloat("TargetElapsed", ref targetElapsed, 1f, 1000.0f);
        if (elapsedChanged) TargetElapsedTime = TimeSpan.FromMilliseconds(targetElapsed);
    }

    protected override void Initialize()
    {
        ImGuiManager.Initialize(_deviceManager.GraphicsDevice);
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GameKeyboard.Update(this, gameTime);
        GameMouse.Update(this, gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // TargetElapsedTime = TimeSpan.FromSeconds(0.75f);
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