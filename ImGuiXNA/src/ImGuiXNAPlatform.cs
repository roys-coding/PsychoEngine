#define FNA

using Hexa.NET.Utilities;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Vector2 = System.Numerics.Vector2;

namespace ImGuiXNA;

public sealed class ImGuiXnaPlatform
{
    // TODO: Add mouse cursor support.
    // TODO: Add mouse set position.
    // TODO: Implement a correct solution for input capturing.
    // TODO: Add Gamepad support.
    // TODO: Implement further platform features, such as clipboard, etc.

    private readonly Game _game;

    private GraphicsDevice _graphicsDevice;

    // Last delta-time is used if current delta-time is zero, to avoid crashes.
    // Initialized to epsilon, so delta-time is not zero in the
    // rare occasions where last delta-time is the first value used.
    private float _previousDeltaTime = float.Epsilon;

    // Input.
    private readonly Keys[]        _allKeys          = Enum.GetValues<Keys>();
    private readonly PlayerIndex[] _playerIndexes    = Enum.GetValues<PlayerIndex>();
    private readonly int           _supportedPlayers = Enum.GetNames<PlayerIndex>().Length;

#if MONOGAME
    private float _previousWheelValueX;
#endif
    private float _previousWheelValueY;

    // Input states.
    private MouseState      _previousMouseState;
    private KeyboardState   _previousKeyboardState;
    private TouchCollection _previousTouchState;

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

        UpdateInput(io);

        ImGui.NewFrame();
    }

    private void UpdateInput(ImGuiIOPtr io)
    {
        io.AddFocusEvent(_game.IsActive);

        // Do not update input if game is unfocused.
        if (io.AppFocusLost) return;

        // Temporary solution to keyboard capturing.
        if (io.WantCaptureKeyboard)
        {
            TextInputEXT.StartTextInput();
        }
        else
        {
            TextInputEXT.StopTextInput();
        }

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
    }

    private void HandleMouse(ImGuiIOPtr io, MouseState state)
    {
        const float wheelDelta = 120f;

        if (_previousMouseState == state) return;

        io.AddMouseSourceEvent(ImGuiMouseSource.Mouse);

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
        if (state.Count == 0) return;

        io.AddMouseSourceEvent(ImGuiMouseSource.TouchScreen);

        // Handle touch events.
        foreach (TouchLocation touch in state)
        {
            switch (touch.State)
            {
                case TouchLocationState.Pressed:
                    io.AddMouseButtonEvent((int)ImGuiMouseButton.Left, true);
                    break;

                case TouchLocationState.Released:
                    io.AddMouseButtonEvent((int)ImGuiMouseButton.Left, false);
                    break;

                case TouchLocationState.Moved:
                    io.AddMousePosEvent(touch.Position.X, touch.Position.Y);
                    break;

                default:
                case TouchLocationState.Invalid: break;
            }
        }

        // TODO: Test touch input.
    }

    private void HandleKeyboard(ImGuiIOPtr io, KeyboardState state)
    {
        if (_previousKeyboardState == state) return;

        // Handle mod keys.
        io.AddKeyEvent(ImGuiKey.ModShift, state.IsKeyDown(Keys.LeftShift)   || state.IsKeyDown(Keys.RightShift));
        io.AddKeyEvent(ImGuiKey.ModCtrl,  state.IsKeyDown(Keys.LeftControl) || state.IsKeyDown(Keys.RightControl));
        io.AddKeyEvent(ImGuiKey.ModAlt,   state.IsKeyDown(Keys.LeftAlt)     || state.IsKeyDown(Keys.RightAlt));
        io.AddKeyEvent(ImGuiKey.ModSuper, state.IsKeyDown(Keys.LeftWindows) || state.IsKeyDown(Keys.RightWindows));

        foreach (Keys key in _allKeys)
        {
            ImGuiKey imguiKey = MapKey(key);

            // Skip unmapped keys.
            if (imguiKey == ImGuiKey.None) continue;

            io.AddKeyEvent(imguiKey, state.IsKeyDown(key));
        }
    }

    private static ImGuiKey MapKey(Keys key)
    {
        return key switch
               {
                   // Basic.
                   Keys.Tab         => ImGuiKey.Tab,
                   Keys.Enter       => ImGuiKey.Enter,
                   Keys.Escape      => ImGuiKey.Escape,
                   Keys.Space       => ImGuiKey.Space,
                   Keys.Back        => ImGuiKey.Backspace,
                   Keys.Insert      => ImGuiKey.Insert,
                   Keys.Delete      => ImGuiKey.Delete,
                   Keys.Home        => ImGuiKey.Home,
                   Keys.End         => ImGuiKey.End,
                   Keys.PageUp      => ImGuiKey.PageUp,
                   Keys.PageDown    => ImGuiKey.PageDown,
                   Keys.Pause       => ImGuiKey.Pause,
                   Keys.PrintScreen => ImGuiKey.PrintScreen,
                   Keys.CapsLock    => ImGuiKey.CapsLock,
                   Keys.NumLock     => ImGuiKey.NumLock,
                   Keys.Scroll      => ImGuiKey.ScrollLock,
                   // Arrows.
                   Keys.Left  => ImGuiKey.LeftArrow,
                   Keys.Right => ImGuiKey.RightArrow,
                   Keys.Up    => ImGuiKey.UpArrow,
                   Keys.Down  => ImGuiKey.DownArrow,
                   // Modifiers.
                   Keys.LeftShift    => ImGuiKey.LeftShift,
                   Keys.RightShift   => ImGuiKey.RightShift,
                   Keys.LeftControl  => ImGuiKey.LeftCtrl,
                   Keys.RightControl => ImGuiKey.RightCtrl,
                   Keys.LeftAlt      => ImGuiKey.LeftAlt,
                   Keys.RightAlt     => ImGuiKey.RightAlt,
                   Keys.LeftWindows  => ImGuiKey.LeftSuper,
                   Keys.RightWindows => ImGuiKey.RightSuper,
                   Keys.Apps         => ImGuiKey.Menu,
                   // Digits.
                   Keys.D0 => ImGuiKey.Key0,
                   Keys.D1 => ImGuiKey.Key1,
                   Keys.D2 => ImGuiKey.Key2,
                   Keys.D3 => ImGuiKey.Key3,
                   Keys.D4 => ImGuiKey.Key4,
                   Keys.D5 => ImGuiKey.Key5,
                   Keys.D6 => ImGuiKey.Key6,
                   Keys.D7 => ImGuiKey.Key7,
                   Keys.D8 => ImGuiKey.Key8,
                   Keys.D9 => ImGuiKey.Key9,
                   // Letters.
                   Keys.A => ImGuiKey.A,
                   Keys.B => ImGuiKey.B,
                   Keys.C => ImGuiKey.C,
                   Keys.D => ImGuiKey.D,
                   Keys.E => ImGuiKey.E,
                   Keys.F => ImGuiKey.F,
                   Keys.G => ImGuiKey.G,
                   Keys.H => ImGuiKey.H,
                   Keys.I => ImGuiKey.I,
                   Keys.J => ImGuiKey.J,
                   Keys.K => ImGuiKey.K,
                   Keys.L => ImGuiKey.L,
                   Keys.M => ImGuiKey.M,
                   Keys.N => ImGuiKey.N,
                   Keys.O => ImGuiKey.O,
                   Keys.P => ImGuiKey.P,
                   Keys.Q => ImGuiKey.Q,
                   Keys.R => ImGuiKey.R,
                   Keys.S => ImGuiKey.S,
                   Keys.T => ImGuiKey.T,
                   Keys.U => ImGuiKey.U,
                   Keys.V => ImGuiKey.V,
                   Keys.W => ImGuiKey.W,
                   Keys.X => ImGuiKey.X,
                   Keys.Y => ImGuiKey.Y,
                   Keys.Z => ImGuiKey.Z,
                   // Function keys.
                   Keys.F1  => ImGuiKey.F1,
                   Keys.F2  => ImGuiKey.F2,
                   Keys.F3  => ImGuiKey.F3,
                   Keys.F4  => ImGuiKey.F4,
                   Keys.F5  => ImGuiKey.F5,
                   Keys.F6  => ImGuiKey.F6,
                   Keys.F7  => ImGuiKey.F7,
                   Keys.F8  => ImGuiKey.F8,
                   Keys.F9  => ImGuiKey.F9,
                   Keys.F10 => ImGuiKey.F10,
                   Keys.F11 => ImGuiKey.F11,
                   Keys.F12 => ImGuiKey.F12,
                   Keys.F13 => ImGuiKey.F13,
                   Keys.F14 => ImGuiKey.F14,
                   Keys.F15 => ImGuiKey.F15,
                   Keys.F16 => ImGuiKey.F16,
                   Keys.F17 => ImGuiKey.F17,
                   Keys.F18 => ImGuiKey.F18,
                   Keys.F19 => ImGuiKey.F19,
                   Keys.F20 => ImGuiKey.F20,
                   Keys.F21 => ImGuiKey.F21,
                   Keys.F22 => ImGuiKey.F22,
                   Keys.F23 => ImGuiKey.F23,
                   Keys.F24 => ImGuiKey.F24,
                   // Numpad.
                   Keys.NumPad0  => ImGuiKey.Keypad0,
                   Keys.NumPad1  => ImGuiKey.Keypad1,
                   Keys.NumPad2  => ImGuiKey.Keypad2,
                   Keys.NumPad3  => ImGuiKey.Keypad3,
                   Keys.NumPad4  => ImGuiKey.Keypad4,
                   Keys.NumPad5  => ImGuiKey.Keypad5,
                   Keys.NumPad6  => ImGuiKey.Keypad6,
                   Keys.NumPad7  => ImGuiKey.Keypad7,
                   Keys.NumPad8  => ImGuiKey.Keypad8,
                   Keys.NumPad9  => ImGuiKey.Keypad9,
                   Keys.Add      => ImGuiKey.KeypadAdd,
                   Keys.Subtract => ImGuiKey.KeypadSubtract,
                   Keys.Multiply => ImGuiKey.KeypadMultiply,
                   Keys.Divide   => ImGuiKey.KeypadDivide,
                   Keys.Decimal  => ImGuiKey.KeypadDecimal,
                   // OEM / punctuation.
                   Keys.OemSemicolon     => ImGuiKey.Semicolon,
                   Keys.OemPlus          => ImGuiKey.Equal,
                   Keys.OemComma         => ImGuiKey.Comma,
                   Keys.OemMinus         => ImGuiKey.Minus,
                   Keys.OemPeriod        => ImGuiKey.Period,
                   Keys.OemQuestion      => ImGuiKey.Slash,
                   Keys.OemTilde         => ImGuiKey.GraveAccent,
                   Keys.OemOpenBrackets  => ImGuiKey.LeftBracket,
                   Keys.OemCloseBrackets => ImGuiKey.RightBracket,
                   Keys.OemPipe          => ImGuiKey.Backslash,
                   Keys.OemQuotes        => ImGuiKey.Apostrophe,
                   // Browser.
                   Keys.BrowserBack    => ImGuiKey.AppBack,
                   Keys.BrowserForward => ImGuiKey.AppForward,
                   // Unmapped.
                   Keys.None or Keys.Select or Keys.Print or Keys.Execute or Keys.Help or Keys.Sleep or Keys.Separator
                       or Keys.BrowserRefresh or Keys.BrowserStop or Keys.BrowserSearch or Keys.BrowserFavorites
                       or Keys.BrowserHome or Keys.VolumeMute or Keys.VolumeDown or Keys.VolumeUp or Keys.MediaNextTrack
                       or Keys.MediaPreviousTrack or Keys.MediaStop or Keys.MediaPlayPause or Keys.LaunchMail
                       or Keys.SelectMedia or Keys.LaunchApplication1 or Keys.LaunchApplication2 or Keys.Oem8
                       or Keys.OemBackslash or Keys.ProcessKey or Keys.Attn or Keys.Crsel or Keys.Exsel or Keys.EraseEof
                       or Keys.Play or Keys.Zoom or Keys.Pa1 or Keys.OemClear or Keys.ChatPadGreen or Keys.ChatPadOrange
                       or Keys.ImeConvert or Keys.ImeNoConvert or Keys.Kana or Keys.Kanji or Keys.OemAuto
                       or Keys.OemCopy or Keys.OemEnlW => ImGuiKey.None,
                   _ => ImGuiKey.None,
               };
    }

    #endregion
}