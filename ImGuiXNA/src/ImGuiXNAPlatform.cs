#define FNA

using Hexa.NET.Utilities;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Vector2 = System.Numerics.Vector2;

namespace ImGuiXNA;

public sealed class ImGuiXnaPlatform
{
    private readonly Game           _game;
    private          GraphicsDevice _graphicsDevice;
    // Last delta-time is used if current delta-time is zero, to avoid crashes.
    // Initialized to epsilon, so delta-time is not zero in the
    // rare occasions where last delta-time is the first value used.
    private float          _previousDeltaTime = float.Epsilon;

    // Input.
    private readonly Keys[]        _allKeys          = Enum.GetValues<Keys>();
    private readonly PlayerIndex[] _playerIndexes    = Enum.GetValues<PlayerIndex>();
    private readonly int           _supportedPlayers = Enum.GetNames<PlayerIndex>().Length;

#if MONOGAME
    private float _previousWheelValueX;
#endif
    private          float           _previousWheelValueY;
    
    // Input states.
    private          MouseState      _previousMouseState;
    private          KeyboardState   _previousKeyboardState;
    private          TouchCollection _previousTouchState;

    #region Life cycle
    
    public ImGuiXnaPlatform(Game game)
    {
        _game = game;
    }

    public unsafe void Initialize(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;

        ImGuiIOPtr         io         = ImGui.GetIO();
        ImGuiPlatformIOPtr platformIo = ImGui.GetPlatformIO();

        // Set up backend flags.
#if FNA
        io.BackendPlatformName = "imgui_impl_fna".ToUTF8Ptr();
#endif
#if MONOGAME
        io.BackendPlatformName =  "imgui_impl_monogame".ToUTF8Ptr();
        io.BackendFlags        |= ImGuiBackendFlags.HasMouseCursors;
#endif
        io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;
        io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        // Set up platform texture capabilities.
        if (_graphicsDevice.GraphicsProfile == GraphicsProfile.Reach)
        {
            platformIo.RendererTextureMaxWidth  = 2048;
            platformIo.RendererTextureMaxHeight = 2048;
        }
        else
        {
            platformIo.RendererTextureMaxWidth  = 4096;
            platformIo.RendererTextureMaxHeight = 4096;
        }

        /* TODO: Implement further platform features, such as clipboard, etc.*/

        // Set up text input events.
#if MONOGAME
        _game.Window.TextInput += (_, _) => io.AddInputCharacter(a.Character);
#endif
#if FNA
        TextInputEXT.TextInput += c => io.AddInputCharacter(c);
#endif
    }

    #endregion

    public void NewFrame(GameTime gameTime)
    {
        ImGuiIOPtr io = ImGui.GetIO();

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Delta time must be positive.
        if (deltaTime <= 0f) deltaTime = _previousDeltaTime;

        _previousDeltaTime = deltaTime;
        io.DeltaTime       = deltaTime;

        // Update display size and scale.
        int backBufferWidth  = _graphicsDevice.PresentationParameters.BackBufferWidth;
        int backBufferHeight = _graphicsDevice.PresentationParameters.BackBufferHeight;
        io.DisplaySize             = new Vector2(backBufferWidth, backBufferHeight);
        io.DisplayFramebufferScale = Vector2.One;

        UpdateInput();

        // Temporary solution to keyboard capturing.
        if (io.WantCaptureKeyboard)
        {
            TextInputEXT.StartTextInput();
        }
        else
        {
            TextInputEXT.StopTextInput();
        }
        
        // TODO: Implement a correct solution for input capturing.

        ImGui.NewFrame();
    }

    private void UpdateInput()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        
        io.AddFocusEvent(_game.IsActive);

        if (io.AppFocusLost) return;

        MouseState      mouse    = Mouse.GetState();
        TouchCollection touches  = TouchPanel.GetState();
        KeyboardState   keyboard = Keyboard.GetState();

        HandleGamepads(io);
        HandleMouse(io, mouse);
        HandleTouchScreen(io, touches);
        HandleKeyboard(io, keyboard);

        _previousMouseState    = mouse;
        _previousKeyboardState = keyboard;
        _previousTouchState    = touches;
    }

    #region Input handling internal

    private void HandleGamepads(ImGuiIOPtr io)
    {
        bool anyGamepadConnected = false;

        // Check if any gamepad is connected.
        for (int i = 0; i < _supportedPlayers; i++)
        {
            if (GamePad.GetState(_playerIndexes[i]).IsConnected)
            {
                anyGamepadConnected = true;
            }
        }

        // Update HasGamepad backend flag correspondingly.
        if (anyGamepadConnected)
        {
            io.BackendFlags |= ImGuiBackendFlags.HasGamepad;
        }
        else
        {
            io.BackendFlags &= ~ImGuiBackendFlags.HasGamepad;
        }

        /* TODO Handle gamepad buttons joysticks and triggers. */
    }

    private void HandleMouse(ImGuiIOPtr io, MouseState state)
    {
        const float wheelDelta = 120f;

        // Handle position.
        io.AddMousePosEvent(state.X, state.Y);

        // Handle mouse buttons.
        io.AddMouseButtonEvent((int)ImGuiMouseButton.Left,   state.LeftButton   == ButtonState.Pressed);
        io.AddMouseButtonEvent((int)ImGuiMouseButton.Right,  state.RightButton  == ButtonState.Pressed);
        io.AddMouseButtonEvent((int)ImGuiMouseButton.Middle, state.MiddleButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(3,                            state.XButton1     == ButtonState.Pressed);
        io.AddMouseButtonEvent(4,                            state.XButton2     == ButtonState.Pressed);

        // Handle the mouse wheel.
#if MONOGAME
        float mouseWheelX = (state.HorizontalScrollWheelValue - _previousWheelValueX) / WheelDelta;
        float mouseWheelY = (state.ScrollWheelValue           - _previousWheelValueY) / WheelDelta;
        io.AddMouseWheelEvent(mouseWheelX, mouseWheelY);

        _previousWheelValueX = state.HorizontalScrollWheelValue;
        _previousWheelValueY = state.ScrollWheelValue;
#endif
#if FNA
        float mouseWheelY = (state.ScrollWheelValue - _previousWheelValueY) / wheelDelta;
        io.AddMouseWheelEvent(0f, mouseWheelY);

        _previousWheelValueY = state.ScrollWheelValue;
#endif
    }

    private void HandleTouchScreen(ImGuiIOPtr io, TouchCollection state)
    {
        // Handle touch events.
        foreach (TouchLocation touch in state)
        {
            switch (touch.State)
            {
                case TouchLocationState.Pressed:  io.AddMouseButtonEvent((int)ImGuiMouseButton.Left, true); break;
                case TouchLocationState.Released: io.AddMouseButtonEvent((int)ImGuiMouseButton.Left, false); break;
                case TouchLocationState.Moved:    io.AddMousePosEvent(touch.Position.X, touch.Position.Y); break;

                default:
                case TouchLocationState.Invalid: break;
            }
        }
    }

    private void HandleKeyboard(ImGuiIOPtr io, KeyboardState state)
    {
        foreach (Keys key in _allKeys)
        {
            ImGuiKey imguiKey;

            // Skip unmapped keys.
            if (!TryMapKeys(key, out imguiKey)) continue;

            io.AddKeyEvent(imguiKey, state.IsKeyDown(key));
        }
    }

    private static bool TryMapKeys(Keys key, out ImGuiKey imguiKey)
    {
        // Special case not handled in the switch.
        // If the actual key we put in is "None", return none and true;
        // otherwise, return none and false.
        if (key == Keys.None)
        {
            imguiKey = ImGuiKey.None;

            return true;
        }

        imguiKey = key switch
                   {
                       Keys.Back                           => ImGuiKey.Backspace,
                       Keys.Tab                            => ImGuiKey.Tab,
                       Keys.Enter                          => ImGuiKey.Enter,
                       Keys.CapsLock                       => ImGuiKey.CapsLock,
                       Keys.Escape                         => ImGuiKey.Escape,
                       Keys.Space                          => ImGuiKey.Space,
                       Keys.PageUp                         => ImGuiKey.PageUp,
                       Keys.PageDown                       => ImGuiKey.PageDown,
                       Keys.End                            => ImGuiKey.End,
                       Keys.Home                           => ImGuiKey.Home,
                       Keys.Left                           => ImGuiKey.LeftArrow,
                       Keys.Right                          => ImGuiKey.RightArrow,
                       Keys.Up                             => ImGuiKey.UpArrow,
                       Keys.Down                           => ImGuiKey.DownArrow,
                       Keys.PrintScreen                    => ImGuiKey.PrintScreen,
                       Keys.Insert                         => ImGuiKey.Insert,
                       Keys.Delete                         => ImGuiKey.Delete,
                       >= Keys.D0 and <= Keys.D9           => ImGuiKey.Key0    + (key - Keys.D0),
                       >= Keys.A and <= Keys.Z             => ImGuiKey.A       + (key - Keys.A),
                       >= Keys.NumPad0 and <= Keys.NumPad9 => ImGuiKey.Keypad0 + (key - Keys.NumPad0),
                       Keys.Multiply                       => ImGuiKey.KeypadMultiply,
                       Keys.Add                            => ImGuiKey.KeypadAdd,
                       Keys.Subtract                       => ImGuiKey.KeypadSubtract,
                       Keys.Decimal                        => ImGuiKey.KeypadDecimal,
                       Keys.Divide                         => ImGuiKey.KeypadDivide,
                       >= Keys.F1 and <= Keys.F24          => ImGuiKey.F1 + (key - Keys.F1),
                       Keys.NumLock                        => ImGuiKey.NumLock,
                       Keys.Scroll                         => ImGuiKey.ScrollLock,
                       Keys.LeftShift                      => ImGuiKey.ModShift,
                       Keys.LeftControl                    => ImGuiKey.ModCtrl,
                       Keys.LeftAlt                        => ImGuiKey.ModAlt,
                       Keys.OemSemicolon                   => ImGuiKey.Semicolon,
                       Keys.OemPlus                        => ImGuiKey.Equal,
                       Keys.OemComma                       => ImGuiKey.Comma,
                       Keys.OemMinus                       => ImGuiKey.Minus,
                       Keys.OemPeriod                      => ImGuiKey.Period,
                       Keys.OemQuestion                    => ImGuiKey.Slash,
                       Keys.OemTilde                       => ImGuiKey.GraveAccent,
                       Keys.OemOpenBrackets                => ImGuiKey.LeftBracket,
                       Keys.OemCloseBrackets               => ImGuiKey.RightBracket,
                       Keys.OemPipe                        => ImGuiKey.Backslash,
                       Keys.OemQuotes                      => ImGuiKey.Apostrophe,
                       Keys.BrowserBack                    => ImGuiKey.AppBack,
                       Keys.BrowserForward                 => ImGuiKey.AppForward,
                       _                                   => ImGuiKey.None,
                   };

        return imguiKey != ImGuiKey.None;
    }

    #endregion
}