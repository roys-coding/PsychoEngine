using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Graphics;
using PsychoEngine.Input;
using PsychoEngine.Utilities;

namespace PsychoEngine.Graphics;

public static class PyGraphics
{
    // TODO: Timings.
    // TODO: Rendering.
    // TODO: Rendering resolution.
    // TODO: Multisampling.

    #region Fields

    private static bool _isInitialized;

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

    public static GraphicsAdapter CurrentAdapter => IsDeviceCreated ? Device.Adapter : GraphicsAdapter.DefaultAdapter;

    public static bool IsDeviceCreated { get; private set; }

    // Graphics settings.
    public static bool VerticalSync  => DeviceManager.SynchronizeWithVerticalRetrace;
    public static bool FixedTimeStep => PyGame.Instance.IsFixedTimeStep;

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
            throw new InvalidOperationException($"{nameof(PyGraphics)} has already been initialized.");
        }

        DeviceManager               =  new GraphicsDeviceManager(PyGame.Instance);
        DeviceManager.DeviceCreated += OnDeviceCreated;

        // TODO: Apply default settings.

        _isInitialized = true;

        #region ImGui

        PyGame.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;

        #endregion
    }

    #region ImGui

    private static void ImGuiOnLayout(object? sender, EventArgs e)
    {
        bool windowOpen = ImGui.Begin($"{PyFonts.Lucide.Gpu} Graphics");

        if (!windowOpen)
        {
            ImGui.End();
            return;
        }

        if (ImGui.CollapsingHeader("Settings##graphics"))
        {
            bool vsync        = VerticalSync;
            bool vsyncChanged = ImGui.Checkbox("Vertical Sync", ref vsync);
            if (vsyncChanged) SetVerticalSync(vsync);

            bool fixedStep        = FixedTimeStep;
            bool fixedStepChanged = ImGui.Checkbox("FixedTimeStep", ref fixedStep);
            if (fixedStepChanged) SetFixedTimeStep(fixedStep);
        }

        ImGui.End();
    }

    #endregion

    private static Texture2D   _testTexture;
    private static SpriteBatch _testBatch;
    private static Vector2     _texturePos;

    internal static void Draw()
    {
        if (PyMouse.IsDragging(MouseButton.Left))
        {
            _texturePos   =  PyMouse.Position.ToVector();
            _texturePos.Y -= 70;
        }
        
        Device.Clear(Color.CornflowerBlue);
        
        _testBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.DepthRead, RasterizerState.CullNone);
        
        _testBatch.Draw(_testTexture, _texturePos, new Rectangle(0, 0, 1, 1), Color.White, MathF.PI * 0.25f, Vector2.Zero, 100f, SpriteEffects.None, 0f);
        
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

    private static void OnDeviceCreated(object? sender, EventArgs e)
    {
        Device          = DeviceManager.GraphicsDevice;
        IsDeviceCreated = true;
        
        _testTexture = new Texture2D(Device, 1, 1, false, SurfaceFormat.Color);
        _testTexture.SetData(new byte[] { 255, 255, 255, 255 });
        
        _testBatch = new SpriteBatch(Device);
    }

    #endregion
}