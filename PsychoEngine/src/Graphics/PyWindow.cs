namespace PsychoEngine.Graphics;

public static partial class PyWindow
{
    // TODO: Persistent resolutions.

    #region Events

    public static event EventHandler<WindowEventArgs>? OnSizeChanged;

    #endregion

    #region Fields

    private static bool _isInitialized;

    #endregion

    #region Properties

    internal static GameWindow GameWindow => PyGame.Instance.Window;

    // General options.
    public static string Title
    {
        get => GameWindow.Title;
        set
        {
            if (value is null)
            {
                throw new ArgumentException("Title cannot be null.", nameof(value));
            }

            GameWindow.Title = value;
        }
    }

    public static bool IsMouseVisible
    {
        get => PyGame.Instance.IsMouseVisible;
        set => PyGame.Instance.IsMouseVisible = value;
    }

    public static bool IsResizable
    {
        get => GameWindow.AllowUserResizing;
        set => GameWindow.AllowUserResizing = value;
    }

    // Resolution & window mode.
    public static WindowMode Mode
    {
        get
        {
            if (PyGraphics.Device.PresentationParameters.IsFullScreen)
            {
                return WindowMode.Fullscreen;
            }

            return GameWindow.IsBorderlessEXT ? WindowMode.Borderless : WindowMode.Windowed;
        }
    }

    public static int   Width       => PyGraphics.Device.PresentationParameters.BackBufferWidth;
    public static int   Height      => PyGraphics.Device.PresentationParameters.BackBufferHeight;
    public static float AspectRatio => (float)Size.X / Size.Y;

    public static Point Size =>
        new(PyGraphics.Device.PresentationParameters.BackBufferWidth,
            PyGraphics.Device.PresentationParameters.BackBufferHeight);

    #endregion

    #region Public interface

    public static GraphicsResolution[] GetScreenSupportedResolutionsDescending()
    {
        return PyGraphics.CurrentAdapter.SupportedDisplayModes.Select(displayMode =>
                                                                          new GraphicsResolution(displayMode.Width,
                                                                              displayMode.Height))
                         .OrderDescending()
                         .ToArray();
    }

    public static void SetSize(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Window width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Window height must be greater than zero.");
        }

        PyGraphics.DeviceManager.PreferredBackBufferWidth  = width;
        PyGraphics.DeviceManager.PreferredBackBufferHeight = height;

        PyGraphics.ApplyChanges();

        OnSizeChanged?.Invoke(null, new WindowEventArgs(width, height));
    }

    // Resolution & window mode.
    public static void SetMode(WindowMode mode)
    {
        GraphicsResolution? appliedResolution = null;

        switch (mode)
        {
            case WindowMode.Windowed:
                PyGraphics.DeviceManager.IsFullScreen = false;
                GameWindow.IsBorderlessEXT            = false;

                break;

            case WindowMode.Borderless:
                PyGraphics.DeviceManager.IsFullScreen = false;
                GameWindow.IsBorderlessEXT            = true;
                break;

            case WindowMode.Fullscreen:
                PyGraphics.DeviceManager.IsFullScreen = true;
                GameWindow.IsBorderlessEXT            = false;

                break;

            default: throw new NotSupportedException($"Window mode '{mode}' not supported.");
        }

        if (appliedResolution.HasValue)
        {
            PyGraphics.DeviceManager.PreferredBackBufferWidth  = appliedResolution.Value.Width;
            PyGraphics.DeviceManager.PreferredBackBufferHeight = appliedResolution.Value.Height;
        }

        PyGraphics.ApplyChanges();
    }

    #endregion

    #region Non public methods

    internal static void Initialize()
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException($"{nameof(PyWindow)} has already been initialized.");
        }

        _isInitialized = true;

        // Default settings.
        Title          = "PsychoEngine Game";
        IsMouseVisible = true;
        IsResizable    = false;
        SetSize(800, 600);
        SetMode(WindowMode.Windowed);

        // Event subscriptions.
        PyGraphics.DeviceManager.PreparingDeviceSettings += DeviceManagerOnPreparingDeviceSettings;
        GameWindow.ClientSizeChanged                     += OnClientSizeChanged;

        #region ImGui

        PyGame.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;
        OnSizeChanged                         += OnOnSizeChanged;

        #endregion
    }

    private static void DeviceManagerOnPreparingDeviceSettings(object? sender, PreparingDeviceSettingsEventArgs e)
    {
        // PresentationParameters presentationParameters = e.GraphicsDeviceInformation.PresentationParameters;
    }

    private static void OnClientSizeChanged(object? sender, EventArgs e)
    {
        // Update preferred back buffer size to match new window size.
        // Otherwise, when changing other settings (such as vsync, multisampling, etc.),
        // the window resets to its previous size.
        PyGraphics.DeviceManager.PreferredBackBufferWidth  = GameWindow.ClientBounds.Width;
        PyGraphics.DeviceManager.PreferredBackBufferHeight = GameWindow.ClientBounds.Height;

        OnSizeChanged?.Invoke(null, new WindowEventArgs(Width, Height));
    }

    #endregion
}