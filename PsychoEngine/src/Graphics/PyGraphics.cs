using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using PsychoEngine.Input;
using PsychoEngine.Utilities;

namespace PsychoEngine.Graphics;

public static partial class PyGraphics
{
    public delegate Rectangle CustomScalingMethod(
        int resolutionWidth,
        int resolutionHeight,
        int windowWidth,
        int windowHeight
    );

    // TODO: Timings.
    // TODO: Rendering.
    // TODO: Rendering resolution.
    // TODO: Multisampling.

    #region Fields

    private static bool _isInitialized;

    // Canvas & scaling.
    private static readonly SortedSet<GraphicsResolution> SupportedResolutions;
    private static          GraphicsResolution            _currentResolution;

    private static RenderTarget2D?      _canvas;
    private static CustomScalingMethod? _customScalingMethod;

    #endregion

    #region Properties

    // Device.
    public static GraphicsDeviceManager DeviceManager
    {
        get =>
            field ??
            throw new NullReferenceException($"{nameof(DeviceManager)} is null. " +
                                             $"Make sure you called {nameof(PyGraphics)}.{nameof(Initialize)}() first.)");
        private set;
    }

    public static GraphicsDevice Device
    {
        get =>
            field ??
            throw new NullReferenceException($"{nameof(Device)} is null." +
                                             $" Wait until the GraphicsDevice is initialized before referencing it.");
        private set;
    }

    public static bool IsDeviceCreated { get; private set; }

    public static GraphicsAdapter CurrentAdapter => IsDeviceCreated ? Device.Adapter : GraphicsAdapter.DefaultAdapter;

    // Graphics settings.

    public static GraphicsResolution CanvasResolution
    {
        get
        {
            if (_canvas is null)
            {
                throw new NullReferenceException("Canvas is null." +
                                                 "Wait until the GraphicsDevice is initialized before referencing it.");
            }
            
            return new GraphicsResolution(_canvas.Width, _canvas.Height);
        }
    }
    public static Rectangle CanvasBounds { get; private set; }

    public static CanvasResizingPolicy CanvasResizingPolicy { get; private set; }
    public static CanvasScalingPolicy  CanvasScalingPolicy  { get; private set; }

    public static bool VerticalSync  => DeviceManager.SynchronizeWithVerticalRetrace;
    public static bool FixedTimeStep => PyGame.Instance.IsFixedTimeStep;

    #endregion

    static PyGraphics()
    {
        SupportedResolutions =
        [
            new GraphicsResolution(800, 600),
            // new GraphicsResolution(1024, 768),
            // new GraphicsResolution(1152, 864),
            // new GraphicsResolution(1176, 664),
            // new GraphicsResolution(1280, 720),
            // new GraphicsResolution(1280, 800),
            // new GraphicsResolution(1280, 960),
            // new GraphicsResolution(1280, 1024),
            // new GraphicsResolution(1360, 768),
            // new GraphicsResolution(1366, 768),
            // new GraphicsResolution(1440, 900),
            // new GraphicsResolution(1440, 1080),
            // new GraphicsResolution(1600, 900),
            // new GraphicsResolution(1600, 1024),
            // new GraphicsResolution(1680, 1050),
            // new GraphicsResolution(1768, 992),
            // new GraphicsResolution(1920, 1080),
        ];
    }

    #region Public interface

    public static void SetCanvasCustomScalingMethod(CustomScalingMethod method)
    {
        _customScalingMethod = method;

        if (CanvasScalingPolicy == CanvasScalingPolicy.Custom)
        {
            CalculateCanvasBounds(PyWindow.Width, PyWindow.Height);
        }
    }

    public static void SetCanvasScalingPolicy(CanvasScalingPolicy policy)
    {
        if (policy == CanvasScalingPolicy)
        {
            return;
        }

        CanvasScalingPolicy = policy;
        CalculateCanvasBounds(PyWindow.Width, PyWindow.Height);
    }

    public static void SetCanvasExpandPolicy(CanvasResizingPolicy policy)
    {
        if (policy == CanvasResizingPolicy)
        {
            return;
        }

        CanvasResizingPolicy = policy;
        CreateCanvas(PyWindow.Width, PyWindow.Height);
        CalculateCanvasBounds(PyWindow.Width, PyWindow.Height);
    }

    public static void SetVerticalSync(bool vSync)
    {
        if (vSync == VerticalSync)
        {
            return;
        }

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
            throw new InvalidOperationException($"{nameof(PyGraphics)} has already been initialized.");
        }

        _isInitialized = true;
        DeviceManager  = new GraphicsDeviceManager(PyGame.Instance);

        // Default settings.
        CanvasScalingPolicy  = CanvasScalingPolicy.ScaleToFit;
        CanvasResizingPolicy = CanvasResizingPolicy.Fixed;

        // Event subscriptions.
        PyWindow.OnSizeChanged      += PyWindowOnOnSizeChanged;
        DeviceManager.DeviceCreated += OnDeviceCreated;

        #region ImGui

        PyGame.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;

        #endregion
    }

    [AllowNull]
    private static Texture2D   _testTexture;
    [AllowNull]
    private static SpriteBatch _testBatch;
    private static Vector2     _texturePos;

    internal static void Draw()
    {
        if (PyMouse.IsDragging(MouseButton.Left))
        {
            _texturePos   =  PyMouse.Position.ToVector() * (CanvasResolution.ToVector2() / PyWindow.Size.ToVector());
            _texturePos.Y -= 35;
        }

        Device.SetRenderTarget(_canvas);

        Device.Clear(Color.CornflowerBlue);

        int winMiddleX = (int)(PyWindow.Width * 0.5f);
        int winMiddleY = (int)(PyWindow.Height * 0.5f);

        _testBatch.Begin(SpriteSortMode.BackToFront,
                         BlendState.AlphaBlend,
                         SamplerState.LinearClamp,
                         DepthStencilState.DepthRead,
                         RasterizerState.CullNone);

        _testBatch.Draw(_testTexture,
                        _texturePos,
                        new Rectangle(0, 0, 1, 1),
                        Color.White,
                        MathF.PI * 0.25f,
                        Vector2.Zero,
                        50f,
                        SpriteEffects.None,
                        0f);

        DrawRect(0, 0, CanvasResolution.Width, CanvasResolution.Height, 3, Color.Red);

        _testBatch.End();

        Device.SetRenderTarget(null);

        _testBatch.Begin(SpriteSortMode.BackToFront,
                         BlendState.AlphaBlend,
                         SamplerState.LinearClamp,
                         DepthStencilState.DepthRead,
                         RasterizerState.CullNone);

        Device.Clear(Color.Black);

        _testBatch.Draw(_canvas, CanvasBounds, Color.White);

        int halfCurrResX = (int)(_currentResolution.Width * 0.5f);
        int halfCurrResY = (int)(_currentResolution.Height * 0.5f);

        DrawRect(winMiddleX - halfCurrResX,
                 winMiddleY - halfCurrResY,
                 _currentResolution.Width,
                 _currentResolution.Height,
                 2,
                 Color.White);

        if (_showSupportedResolutions)
        {
            foreach (GraphicsResolution resolution in SupportedResolutions)
            {
                int halfSizeX = (int)(resolution.Width * 0.5f);
                int halfSizeY = (int)(resolution.Height * 0.5f);

                DrawRect(winMiddleX - halfSizeX,
                         winMiddleY - halfSizeY,
                         resolution.Width,
                         resolution.Height,
                         3,
                         Color.Yellow * 0.5f);
            }
        }

        _testBatch.End();
    }

    internal static void ApplyChanges()
    {
        if (!IsDeviceCreated)
        {
            return;
        }

        DeviceManager.ApplyChanges();
    }

    private static void SelectResolution(int windowWidth, int windowHeight)
    {
        // This method selects the biggest possible supported resolution (in area) that fits inside the game window.
        
        GraphicsResolution selectedResolution = SupportedResolutions.First();
        int                lastResolutionArea = 0;
        GraphicsResolution windowResolution   = new(windowWidth, windowHeight);

        foreach (GraphicsResolution resolution in SupportedResolutions)
        {
            // Skip resolutions bigger than the current window size.
            if (resolution > windowResolution)
            {
                continue;
            }

            // Select resolution with the greatest area.
            if (resolution.Area <= lastResolutionArea)
            {
                continue;
            }

            selectedResolution = resolution;
            lastResolutionArea = resolution.Area;
        }

        _currentResolution = selectedResolution;
    }

    private static void CalculateCanvasBounds(int windowWidth, int windowHeight)
    {
        Rectangle bounds;

        switch (CanvasResizingPolicy)
        {
            case CanvasResizingPolicy.Fixed:
                switch (CanvasScalingPolicy)
                {
                    case CanvasScalingPolicy.NoScaling:
                        bounds = ScalingNoScaling(_currentResolution.Width, _currentResolution.Height);

                        break;

                    case CanvasScalingPolicy.ScaleToFit:
                        bounds = ScalingScaleToFit(_currentResolution.Width,
                                                   _currentResolution.Height,
                                                   windowWidth,
                                                   windowHeight);

                        break;

                    case CanvasScalingPolicy.Stretch:
                        bounds = ScalingStretch(windowWidth, windowHeight);

                        break;

                    case CanvasScalingPolicy.Integer:
                        bounds = ScalingInteger(_currentResolution.Width, _currentResolution.Height);

                        break;

                    case CanvasScalingPolicy.Custom:
                        if (_customScalingMethod is null)
                        {
                            throw new NullReferenceException($"No custom scaling method is defined. Use {nameof(SetCanvasCustomScalingMethod)} to define one.");
                        }

                        bounds = _customScalingMethod(_currentResolution.Width,
                                                      _currentResolution.Height,
                                                      windowWidth,
                                                      windowHeight);

                        break;

                    default: throw new NotSupportedException($"Scaling policy '{CanvasScalingPolicy}' not supported.");
                }

                break;

            case CanvasResizingPolicy.MatchSize:
                // Fill entire window.
                bounds.Width  = windowWidth;
                bounds.Height = windowHeight;
                bounds.X      = 0;
                bounds.Y      = 0;

                break;

            default: throw new NotSupportedException($"Expand policy '{CanvasResizingPolicy}' not supported.");
        }

        CanvasBounds = bounds;
    }

    private static void CreateCanvas(int windowWidth, int windowHeight)
    {
        int resolutionWidth;
        int resolutionHeight;

        switch (CanvasResizingPolicy)
        {
            case CanvasResizingPolicy.Fixed:
                resolutionWidth  = _currentResolution.Width;
                resolutionHeight = _currentResolution.Height;

                break;

            case CanvasResizingPolicy.MatchSize:
                resolutionWidth  = windowWidth;
                resolutionHeight = windowHeight;

                break;

            default: throw new NotSupportedException($"Resizing policy '{CanvasResizingPolicy}' not supported.");
        }

        _canvas?.Dispose();
        _canvas = new RenderTarget2D(Device, resolutionWidth, resolutionHeight);
    }

    #region Scaling methods

    private static Rectangle ScalingInteger(int resolutionWidth, int resolutionHeight)
    {
        int timesX = (int)((float)PyWindow.Width / resolutionWidth);
        int timesY = (int)((float)PyWindow.Height / resolutionHeight);
        int scale  = (int)MathF.Min(timesX, timesY);

        int boundsWidth  = resolutionWidth * scale;
        int boundsHeight = resolutionHeight * scale;

        // Center canvas.
        int boundsX = (int)(PyWindow.Width / 2f - boundsWidth / 2f);
        int boundsY = (int)(PyWindow.Height / 2f - boundsHeight / 2f);

        return new Rectangle(boundsX, boundsY, boundsWidth, boundsHeight);
    }

    private static Rectangle ScalingNoScaling(int resolutionWidth, int resolutionHeight)
    {
        // Center canvas.
        int boundsX = (int)(PyWindow.Width / 2f - resolutionWidth / 2f);
        int boundsY = (int)(PyWindow.Height / 2f - resolutionHeight / 2f);

        return new Rectangle(boundsX, boundsY, resolutionWidth, resolutionHeight);
    }

    private static Rectangle ScalingScaleToFit(
        int resolutionWidth,
        int resolutionHeight,
        int windowWidth,
        int windowHeight
    )
    {
        float canvasRatio = (float)resolutionWidth / resolutionHeight;
        float windowRatio = (float)windowWidth / windowHeight;

        int boundsWidth;
        int boundsHeight;

        if (canvasRatio > windowRatio)
        {
            boundsWidth  = PyWindow.Width;
            boundsHeight = (int)(boundsWidth / canvasRatio);
        }
        else if (canvasRatio < windowRatio)
        {
            boundsHeight = PyWindow.Height;
            boundsWidth  = (int)(boundsHeight * canvasRatio);
        }
        else
        {
            boundsWidth  = PyWindow.Width;
            boundsHeight = PyWindow.Height;
        }

        // Center canvas.
        int canvasX = (int)(PyWindow.Width / 2f - boundsWidth / 2f);
        int canvasY = (int)(PyWindow.Height / 2f - boundsHeight / 2f);

        return new Rectangle(canvasX, canvasY, boundsWidth, boundsHeight);
    }

    private static Rectangle ScalingStretch(int windowWidth, int windowHeight)
    {
        return new Rectangle(0, 0, windowWidth, windowHeight);
    }

    #endregion

    #region Event subscriptions

    private static void OnDeviceCreated(object? sender, EventArgs args)
    {
        Device          = DeviceManager.GraphicsDevice;
        IsDeviceCreated = true;

        int windowWidth  = Device.PresentationParameters.BackBufferWidth;
        int windowHeight = Device.PresentationParameters.BackBufferHeight;
        
        SelectResolution(windowWidth, windowHeight);
        CreateCanvas(windowWidth, windowHeight);
        CalculateCanvasBounds(windowWidth, windowHeight);

        _testTexture = new Texture2D(Device, 1, 1, false, SurfaceFormat.Color);

        _testTexture.SetData(new byte[]
        {
            255, 255, 255, 255,
        });

        _testBatch = new SpriteBatch(Device);
    }

    private static void PyWindowOnOnSizeChanged(object? sender, WindowEventArgs args)
    {
        if (!IsDeviceCreated)
        {
            return;
        }

        SelectResolution(args.WindowWidth, args.WindowHeight);
        CreateCanvas(args.WindowWidth, args.WindowHeight);
        CalculateCanvasBounds(args.WindowWidth, args.WindowHeight);
    }

    private static void DrawRect(int x, int y, int width, int height, int weight, Color color)
    {
        _testBatch.Draw(_testTexture, new Rectangle(x, y, width, weight), color);
        _testBatch.Draw(_testTexture, new Rectangle(x, y + height - weight, width, weight), color);
        _testBatch.Draw(_testTexture, new Rectangle(x, y, weight, height), color);
        _testBatch.Draw(_testTexture, new Rectangle(x + width - weight, y, weight, height), color);
    }

    #endregion

    #endregion
}