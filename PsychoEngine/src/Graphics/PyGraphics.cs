using Microsoft.Xna.Framework.Graphics;

namespace PsychoEngine.Graphics;

public static class PyGraphics
{
    public static class Window
    {
        #region Properties

        // General options.
        public static string Title
        {
            get => GameWindow.Title;
            set => GameWindow.Title = value;
        }

        public static bool IsMouseVisible
        {
            get => PyGame.Instance.IsMouseVisible;
            set => PyGame.Instance.IsMouseVisible = value;
        }

        public static bool AllowUserResizing
        {
            get => GameWindow.AllowUserResizing;
            set => GameWindow.AllowUserResizing = value;
        }

        public static WindowMode Mode
        {
            get
            {
                if (DeviceManager.IsFullScreen)
                {
                    return WindowMode.Fullscreen;
                }

                return GameWindow.IsBorderlessEXT ? WindowMode.Borderless : WindowMode.Windowed;
            }
        }

        // Size.
        public static Point Size   => new(Width, Height);
        public static int   Width  => DeviceManager.PreferredBackBufferWidth;
        public static int   Height => DeviceManager.PreferredBackBufferHeight;

        #endregion

        #region Public interface

        public static void SetMode(WindowMode mode)
        {
            switch (mode)
            {
                case WindowMode.Windowed:
                    SetModeWindowed();
                    break;

                case WindowMode.Borderless:
                    SetModeBorderless();
                    break;

                case WindowMode.Fullscreen:
                    SetModeFullscreen();
                    break;

                default: throw new NotSupportedException($"Window mode '{mode}' not supported.");
            }
        }

        public static void SetSize(int windowWidth, int windowHeight)
        {
            DeviceManager.PreferredBackBufferWidth = windowWidth;
            DeviceManager.PreferredBackBufferHeight = windowHeight;
            
            DeviceManager.ApplyChanges();
        }

        #endregion

        #region Private methods

        private static void SetModeWindowed()
        {
            DeviceManager.IsFullScreen = false;
            GameWindow.IsBorderlessEXT  = false;
            
            DeviceManager.ApplyChanges();
        }

        private static void SetModeFullscreen()
        {
            DeviceManager.IsFullScreen = true;
            GameWindow.IsBorderlessEXT  = false;
            
            DeviceManager.ApplyChanges();
        }

        private static void SetModeBorderless()
        {
            DeviceManager.IsFullScreen = true;
            GameWindow.IsBorderlessEXT  = false;
            
            DeviceManager.ApplyChanges();
        }

        #endregion
    }

    #region Fields

    public static GraphicsDeviceManager DeviceManager { get; private set; }
    public static GraphicsDevice        Device { get; private set; }

    #endregion

    #region Properties

    public static bool VerticalSync => DeviceManager.SynchronizeWithVerticalRetrace;
    public static bool FixedTimeStep => PyGame.Instance.IsFixedTimeStep;
    
    private static GameWindow GameWindow => PyGame.Instance.Window;

    #endregion

    #region Public interface

    public static void SetVerticalSync(bool vSync)
    {
        DeviceManager.SynchronizeWithVerticalRetrace = vSync;
        DeviceManager.ApplyChanges();
    }

    public static void SetFixedTimeStep(bool fixedTimeStep)
    {
        PyGame.Instance.IsFixedTimeStep = fixedTimeStep;
    }

    #endregion

    #region Non public methods

    internal static void Initialize()
    {
        DeviceManager = new GraphicsDeviceManager(PyGame.Instance);

        DeviceManager.DeviceCreated += OnDeviceCreated;
        GameWindow.ClientSizeChanged += OnClientSizeChanged;
    }

    internal static void Draw()
    {
        Device.Clear(Color.CornflowerBlue);
    }

    private static void OnDeviceCreated(object? sender, EventArgs e)
    {
        Device = DeviceManager.GraphicsDevice;
    }

    private static void OnClientSizeChanged(object? sender, EventArgs e)
    {
        DeviceManager.ApplyChanges();
    }

    #endregion
}