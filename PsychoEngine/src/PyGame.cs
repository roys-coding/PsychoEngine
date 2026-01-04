using System.Diagnostics.CodeAnalysis;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using PsychoEngine.Input;

namespace PsychoEngine;

public class PyGame : Game
{
    private readonly GraphicsDeviceManager _deviceManager;

    [AllowNull]
    public static PyGame Instance { get; private set; }

    public ImGuiManager ImGuiManager { get; }

    public PyGame(string windowTitle, int windowWidth, int windowHeight)
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
        ImPlot.ShowDemoWindow();

        bool gameWindow = ImGui.Begin("Game");

        if (!gameWindow)
        {
            ImGui.End();
            return;
        }

        bool exitPressed = ImGui.Button("Exit");
        ImGui.SameLine();
        ImGui.TextDisabled("(ESC)");
        if (exitPressed || ImGui.IsKeyDown(ImGuiKey.Escape)) Exit();

        bool timesHeader = ImGui.CollapsingHeader("Times");

        if (timesHeader)
        {
            float targetElapsedMs = (float)TargetElapsedTime.TotalMilliseconds;
            bool  elapsedChanged  = ImGui.SliderFloat("TargetMs", ref targetElapsedMs, 1f, 500.0f);

            if (elapsedChanged) TargetElapsedTime = TimeSpan.FromMilliseconds(targetElapsedMs);

            float fps        = 1000f / targetElapsedMs;
            bool  fpsChanged = ImGui.SliderFloat("TargetFPS", ref fps, 2f, 1000f);

            if (fpsChanged) TargetElapsedTime = TimeSpan.FromMilliseconds(1000f / fps);

            float sleepMs        = (float)InactiveSleepTime.TotalMilliseconds;
            bool  sleepMsChanged = ImGui.SliderFloat("InactiveMs", ref sleepMs, 0f, 1000f);

            if (sleepMsChanged) InactiveSleepTime = TimeSpan.FromMilliseconds(sleepMs);

            ImGui.TextDisabled("Press Shift+F to reset to 60 FPS!");

            if (ImGui.IsKeyDown(ImGuiKey.F) && ImGui.IsKeyDown(ImGuiKey.ModShift))
            {
                // 60 fps.
                TargetElapsedTime = TimeSpan.FromTicks(166667);
            }

            bool isFixed                        = IsFixedTimeStep;
            bool isFixedChanged                 = ImGui.Checkbox("FixedTime", ref isFixed);
            if (isFixedChanged) IsFixedTimeStep = isFixed;

            bool vsync        = _deviceManager.SynchronizeWithVerticalRetrace;
            bool vsyncChanged = ImGui.Checkbox("VSync", ref vsync);

            if (vsyncChanged)
            {
                _deviceManager.SynchronizeWithVerticalRetrace = vsync;
                _deviceManager.ApplyChanges();
            }
        }

        ImGui.End();
    }

    protected override void Initialize()
    {
        ImGuiManager.Initialize(_deviceManager.GraphicsDevice);
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        PyGameTimes.Update = gameTime;

        PyMouse.Update(this);
        PyKeyboard.Update(this);
        PyGamePads.Update(this);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        PyGameTimes.Draw = gameTime;

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