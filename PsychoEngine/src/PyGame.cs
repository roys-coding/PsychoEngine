using System.Diagnostics.CodeAnalysis;
using PsychoEngine.Graphics;
using PsychoEngine.Input;

namespace PsychoEngine;

public partial class PyGame : Game
{
    [AllowNull] public static PyGame Instance { get; private set; }

    internal ImGuiManager ImGuiManager { get; }

    public PyGame(string windowTitle, int windowWidth, int windowHeight)
    {
        if (Instance is not null)
        {
            throw new InvalidOperationException("CoreEngine has already been initialized.");
        }

        Instance = this;

        ImGuiManager = new ImGuiManager(this);

        PyGraphics.Initialize();
        PyWindow.Initialize();
        
        PyWindow.Title = windowTitle;
        PyWindow.SetSize(windowWidth, windowHeight);
        PyWindow.IsMouseVisible = false;
        PyWindow.IsResizable    = true;
        
        PyGraphics.SetVerticalSync(false);

        InitializeImGui();
    }

    protected override void Initialize()
    {
        ImGuiManager.Initialize(PyGraphics.Device);
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
        
        PyGraphics.Draw();

        ImGuiManager.Draw(gameTime);

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        ImGuiManager.Terminate();

        base.UnloadContent();
    }
}