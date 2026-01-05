using System.Diagnostics.CodeAnalysis;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Graphics;
using PsychoEngine.Utilities;
using Vector2 = System.Numerics.Vector2;

namespace PsychoEngine.Graphics;

public static class PyGraphics
{
    // TODO: Timings.
    // TODO: Rendering.
    // TODO: Persistent resolutions.
    // TODO: Rendering resolution.
    // TODO: ImGui debug window.
    
    public static class Window
    {
        #region Properties

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
            if (windowWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(windowWidth), $"{windowWidth} must be greater than zero.");
            }
            if (windowHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(windowHeight),
                                                      $"{windowHeight} must be greater than zero.");
            }
            
            DeviceManager.PreferredBackBufferWidth = windowWidth;
            DeviceManager.PreferredBackBufferHeight = windowHeight;
            
            ApplyChanges();
        }

        #endregion

        #region Private methods

        private static void SetModeWindowed()
        {
            DeviceManager.IsFullScreen = false;
            GameWindow.IsBorderlessEXT  = false;
            
            ApplyChanges();
        }

        private static void SetModeFullscreen()
        {
            DeviceManager.IsFullScreen = true;
            GameWindow.IsBorderlessEXT  = false;
            
            ApplyChanges();
        }

        private static void SetModeBorderless()
        {
            DeviceManager.IsFullScreen = true;
            GameWindow.IsBorderlessEXT  = false;
            
            ApplyChanges();
        }

        #endregion
    }

    #region Fields

    private static bool                   _isInitialized;
    private static bool                   _isDeviceInitialized;

    #endregion

    #region Properties

    // Device.
    public static GraphicsDeviceManager DeviceManager
    {
        get => field ?? throw new NullReferenceException("Attempted to access PyGraphics before initialization.");
        private set;
    }

    public static GraphicsDevice Device
    {
        get => field ?? throw new NullReferenceException("Attempted to access GraphicsDevice before initialization.");
        private set;
    }

    // Graphics settings.
    public static bool                  VerticalSync  => DeviceManager.SynchronizeWithVerticalRetrace;
    public static bool                  FixedTimeStep => PyGame.Instance.IsFixedTimeStep;
    
    private static GameWindow GameWindow => PyGame.Instance.Window;

    #endregion

    #region Public interface

    public static void SetVerticalSync(bool vSync)
    {
        DeviceManager.SynchronizeWithVerticalRetrace = vSync;
        ApplyChanges();
    }

    public static void SetFixedTimeStep(bool fixedTimeStep)
    {
        PyGame.Instance.IsFixedTimeStep = fixedTimeStep;
    }

    #endregion

    #region Non public methods

    internal static void Initialize()
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("PyGraphics has already been initialized.");
        }
        
        DeviceManager = new GraphicsDeviceManager(PyGame.Instance);

        DeviceManager.DeviceCreated += OnDeviceCreated;
        GameWindow.ClientSizeChanged += OnClientSizeChanged;

        _isInitialized = true;

        #region ImGui

        PyGame.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;

        #endregion
    }

    #region ImGui

    private static unsafe void ImGuiOnLayout(object? sender, EventArgs e)
    {
        bool windowOpen = ImGui.Begin($"{PyFonts.Lucide.Monitor} Graphics");

        if (!windowOpen)
        {
            ImGui.End();
            return;
        }
        
        ImGui.SeparatorText("Window");

        string windowTitle             = Window.Title;
        bool   titleChanged            = ImGui.InputText("Title", ref windowTitle, 255);
        if (titleChanged) Window.Title = windowTitle;

        bool mouseVisible                              = Window.IsMouseVisible;
        bool mouseVisibleChanged                       = ImGui.Checkbox("Is Mouse Visible", ref mouseVisible);
        if (mouseVisibleChanged) Window.IsMouseVisible = mouseVisible;

        bool allowResizing                                 = Window.AllowUserResizing;
        bool allowResizingChanged                          = ImGui.Checkbox("Allow Resizing", ref allowResizing);
        if (allowResizingChanged) Window.AllowUserResizing = allowResizing;

        int[] windowSize =
        [
            Window.Width, Window.Height,
        ];

        fixed (int* windowSizePtr = windowSize)
        {
            bool    sizeChanged = ImGui.DragInt2("Size", windowSizePtr, 1, ImGuiSliderFlags.AlwaysClamp);
            if (sizeChanged) Window.SetSize(windowSize[0], windowSize[1]);
        }

        ImGui.SeparatorText("Graphics");
        
        bool vsync        = VerticalSync;
        bool vsyncChanged = ImGui.Checkbox("Vertical Sync", ref vsync);
        if (vsyncChanged) SetVerticalSync(vsync);
        
        bool fixedStep        = FixedTimeStep;
        bool fixedStepChanged = ImGui.Checkbox("FixedTimeStep", ref fixedStep);
        if (fixedStepChanged) SetFixedTimeStep(fixedStep);
        
        ImGui.End();
    }

    #endregion

    internal static void Draw()
    {
        Device.Clear(Color.CornflowerBlue);
    }

    private static void ApplyChanges()
    {
        if (!_isDeviceInitialized)
        {
            return;
        }
        
        DeviceManager.ApplyChanges();
    }

    private static void OnDeviceCreated(object? sender, EventArgs e)
    {
        Device               = DeviceManager.GraphicsDevice;
        _isDeviceInitialized = true;
    }

    private static void OnClientSizeChanged(object? sender, EventArgs e)
    {
        ApplyChanges();
    }

    #endregion
}