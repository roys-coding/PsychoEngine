using Hexa.NET.ImGui;

namespace PsychoEngine.Core;

public class CoreEngine : Game
{
    public static CoreEngine Instance { get; private set; }

    private readonly ImGuiManager          _imGuiManager;
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

        _imGuiManager = new ImGuiManager(this);

        _imGuiManager.OnLayout += ImGuiOnLayout;
    }

    private void ImGuiOnLayout(object? sender, EventArgs eventArgs)
    {
        const ImGuiDockNodeFlags dockFlags =
            ImGuiDockNodeFlags.PassthruCentralNode | ImGuiDockNodeFlags.NoDockingOverCentralNode;

        ImGui.DockSpaceOverViewport(dockFlags);
    }

    protected override void Initialize()
    {
        _imGuiManager.Initialize(_deviceManager.GraphicsDevice);
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