using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Graphics;

namespace PsychoEngine.Graphics;

public static class PyWindow
{
    // TODO: Persistent resolutions.

    #region Fields

    private static readonly SortedSet<WindowResolution> CustomResolutions = new();

    private static bool _isInitialized;

    #endregion

    #region Properties

    public static GameWindow GameWindow => PyGame.Instance.Window;

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

    public static WindowSizingPolicy SizingPolicy { get; private set; }

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

    public static WindowResolution ActiveResolution =>
        new(PyGraphics.Device.PresentationParameters.BackBufferWidth,
            PyGraphics.Device.PresentationParameters.BackBufferHeight);

    #endregion

    #region Public interface

    public static void SetSizingPolicy(WindowSizingPolicy sizingPolicy)
    {
        switch (sizingPolicy)
        {
            case WindowSizingPolicy.AllowUserResizing:
                GameWindow.AllowUserResizing = true;
                break;

            case WindowSizingPolicy.EnforceMonitorSupportedResolutions:
            case WindowSizingPolicy.EnforceCustomResolutions:
                GameWindow.AllowUserResizing = false;
                break;

            default: throw new NotSupportedException($"Window sizing policy '{sizingPolicy}' not supported.");
        }

        SizingPolicy = sizingPolicy;

        if (PyGraphics.IsDeviceCreated)
        {
            SetResolution(ActiveResolution.Width, ActiveResolution.Height);
        }
    }

    public static WindowResolution[] GetScreenSupportedResolutionsDescending()
    {
        return PyGraphics.CurrentAdapter.SupportedDisplayModes.Select(displayMode =>
                                                                          new WindowResolution(displayMode.Width,
                                                                              displayMode.Height))
                         .OrderDescending()
                         .ToArray();
    }

    public static bool AddCustomSupportedResolution(int width, int height)
    {
        return CustomResolutions.Add(new WindowResolution(width, height));
    }

    public static void SetResolution(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), $"{width} must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height),
                                                  $"{height} must be greater than zero.");
        }

        PyGraphics.DeviceManager.PreferredBackBufferWidth  = width;
        PyGraphics.DeviceManager.PreferredBackBufferHeight = height;

        PyGraphics.ApplyChanges();
    }

    // Resolution & window mode.
    public static void SetMode(WindowMode mode)
    {
        WindowResolution? appliedResolution = null;
        
        switch (mode)
        {
            case WindowMode.Windowed:
                PyGraphics.DeviceManager.IsFullScreen = false;
                GameWindow.IsBorderlessEXT            = false;
                
                break;

            case WindowMode.Fullscreen:
                PyGraphics.DeviceManager.IsFullScreen = true;
                GameWindow.IsBorderlessEXT = false;
                
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

        // Default values.
        Title          = "PsychoEngine Game";
        IsMouseVisible = false;
        SetSizingPolicy(WindowSizingPolicy.AllowUserResizing);
        SetResolution(800, 600);
        SetMode(WindowMode.Windowed);

        PyGraphics.DeviceManager.PreparingDeviceSettings += DeviceManagerOnPreparingDeviceSettings;
        GameWindow.ClientSizeChanged                     += OnClientSizeChanged;

        _isInitialized = true;

        #region ImGui

        PyGame.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;

        #endregion
    }

    #region ImGui

    #region ImGui fields

    private static readonly int[] ScreenSize =
    [
        0, 0,
    ];

    private static readonly string[] WindowModeNames   = Enum.GetNames<WindowMode>();
    private static readonly string[] SizingPolicyNames = Enum.GetNames<WindowSizingPolicy>();

    private static bool _editingScreenSize;

    #endregion

    private static void ImGuiOnLayout(object? sender, EventArgs e)
    {
        bool windowOpen = ImGui.Begin($"{PyFonts.Lucide.AppWindowMac} Window");

        if (!windowOpen)
        {
            ImGui.End();
            return;
        }

        string windowTitle      = Title;
        bool   titleChanged     = ImGui.InputText("Title", ref windowTitle, 255);
        if (titleChanged) Title = windowTitle;

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Size & mode"))
        {
            int  windowMode        = (int)Mode;
            bool windowModeChanged = ImGui.Combo("Mode", ref windowMode, WindowModeNames, WindowModeNames.Length);
            if (windowModeChanged) SetMode((WindowMode)windowMode);

            int sizingPolicy = (int)SizingPolicy;

            bool sizingPolicyChanged =
                ImGui.Combo("Sizing Policy", ref sizingPolicy, SizingPolicyNames, SizingPolicyNames.Length);

            if (sizingPolicyChanged) SetSizingPolicy((WindowSizingPolicy)sizingPolicy);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (!_editingScreenSize)
            {
                ScreenSize[0] = ActiveResolution.Width;
                ScreenSize[1] = ActiveResolution.Height;
            }

            bool screenSizeChanged =
                ImGui.DragInt2("##window_size", ref ScreenSize[0], 1, ImGuiSliderFlags.AlwaysClamp);

            if (screenSizeChanged) _editingScreenSize = true;

            if (!_editingScreenSize)
            {
                ImGui.BeginDisabled();
            }

            bool applySizePressed = ImGui.Button("Apply");
            ImGui.SameLine();
            bool resetSizePressed = ImGui.Button("Cancel");

            if (!_editingScreenSize)
            {
                ImGui.EndDisabled();
            }

            if (applySizePressed)
            {
                SetResolution(ScreenSize[0], ScreenSize[1]);
                _editingScreenSize = false;
            }

            if (resetSizePressed)
            {
                _editingScreenSize = false;
            }
        }

        if (ImGui.CollapsingHeader("Settings##window"))
        {
            bool mouseVisible                       = IsMouseVisible;
            bool mouseVisibleChanged                = ImGui.Checkbox("Is Mouse Visible", ref mouseVisible);
            if (mouseVisibleChanged) IsMouseVisible = mouseVisible;
        }

        if (ImGui.CollapsingHeader("Internal"))
        {
            ImGui.Text($"Active resolution: {ActiveResolution}");
            ImGui.Text($"Preferred resolution: ({PyGraphics.DeviceManager.PreferredBackBufferWidth} x {PyGraphics.DeviceManager.PreferredBackBufferHeight})");
        }

        ImGui.End();
    }

    #endregion

    private static void DeviceManagerOnPreparingDeviceSettings(object? sender, PreparingDeviceSettingsEventArgs e)
    {
        PresentationParameters presentationParameters = e.GraphicsDeviceInformation.PresentationParameters;
        
        EnforceSizingPolicy(presentationParameters);
    }

    private static void OnClientSizeChanged(object? sender, EventArgs e)
    {
        // Update preferred back buffer size to match new window size.
        // Otherwise, when changing other settings (such as vsync, multisampling, etc.),
        // the window resets to its previous size.

        PyGraphics.DeviceManager.PreferredBackBufferWidth  = GameWindow.ClientBounds.Width;
        PyGraphics.DeviceManager.PreferredBackBufferHeight = GameWindow.ClientBounds.Height;
    }

    private static void EnforceSizingPolicy(PresentationParameters presentationParameters)
    {
        if (presentationParameters.IsFullScreen || GameWindow.IsBorderlessEXT) return;

        WindowResolution targetResolution =
            new(presentationParameters.BackBufferWidth, presentationParameters.BackBufferHeight);

        // TODO: Enforce sizing policy in borderless & fullscreen modes.

        switch (SizingPolicy)
        {
            case WindowSizingPolicy.AllowUserResizing: break;

            case WindowSizingPolicy.EnforceMonitorSupportedResolutions:
                WindowResolution[] supportedResolutions = GetScreenSupportedResolutionsDescending();

                if (supportedResolutions.Contains(targetResolution)) break;

                WindowResolution resolvedResolution = default;
                bool             resolutionResolved = false;

                foreach (WindowResolution resolution in supportedResolutions)
                {
                    if (resolution > targetResolution) continue;

                    resolvedResolution = resolution;
                    resolutionResolved = true;
                    break;
                }

                if (!resolutionResolved)
                {
                    resolvedResolution = supportedResolutions.Last();
                }

                presentationParameters.BackBufferWidth  = resolvedResolution.Width;
                presentationParameters.BackBufferHeight = resolvedResolution.Height;

                break;

            case WindowSizingPolicy.EnforceCustomResolutions:
                if (CustomResolutions.Contains(targetResolution)) break;

                WindowResolution resolvedCustomResolution = default;
                bool             customResolutionResolved = false;

                foreach (WindowResolution resolution in CustomResolutions)
                {
                    if (resolution > targetResolution) continue;

                    resolvedCustomResolution = resolution;
                    customResolutionResolved = true;
                    break;
                }

                if (!customResolutionResolved)
                {
                    resolvedCustomResolution = CustomResolutions.First();
                }

                presentationParameters.BackBufferWidth  = resolvedCustomResolution.Width;
                presentationParameters.BackBufferHeight = resolvedCustomResolution.Height;

                break;

            default: throw new NotSupportedException($"Sizing policy '{SizingPolicy}' not supported.");
        }
    }

    #endregion
}