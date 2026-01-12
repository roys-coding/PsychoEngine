using System.Diagnostics.CodeAnalysis;
using PsychoEngine.Input;

namespace PsychoEngine;

public partial class PyGame : Game
{
    private readonly GraphicsDeviceManager _deviceManager;

    [AllowNull] public static PyGame Instance { get; private set; }

    internal ImGuiManager ImGuiManager { get; }

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

        InitializeImGui();
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