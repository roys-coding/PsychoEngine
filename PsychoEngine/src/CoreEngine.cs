using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using PsychoEngine.Core;
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
        InactiveSleepTime                             = TimeSpan.Zero;

        Window.Title = windowTitle;

        ImGuiManager = new ImGuiManager(this);

        ImGuiManager.OnLayout += ImGuiOnLayout;
    }

    private Stopwatch _updateWatch = new();
    private Stopwatch _drawWatch   = new();
    private double    _measuredUpdate;
    private double    _measuredDraw;

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
            bool  fpsChanged = ImGui.SliderFloat("TargetFPS", ref fps, 2f, 1000);

            if (fpsChanged) TargetElapsedTime = TimeSpan.FromMilliseconds(1000f / fps);

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

            ImGui.SeparatorText("Measured");

            ImGui.Text($"Ms between updates: {_measuredUpdate / 10000} ms");
            ImGui.Text($"Ms between draws: {_measuredDraw     / 10000} ms");
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
        _updateWatch.Stop();
        _measuredUpdate  = _updateWatch.ElapsedTicks;
        GameTimes.Update = gameTime;

        GameKeyboard.Update(this);
        GameMouse.Update(this);

        base.Update(gameTime);
        _updateWatch.Restart();
    }

    protected override void Draw(GameTime gameTime)
    {
        _drawWatch.Stop();
        _measuredDraw  = _drawWatch.ElapsedTicks;
        GameTimes.Draw = gameTime;

        GraphicsDevice.Clear(Color.CornflowerBlue);

        ImGuiManager.Draw(gameTime);

        base.Draw(gameTime);
        _drawWatch.Restart();
    }

    protected override void UnloadContent()
    {
        ImGuiManager.Terminate();

        base.UnloadContent();
    }
}