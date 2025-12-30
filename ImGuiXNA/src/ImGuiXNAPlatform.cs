#define FNA

using Hexa.NET.Utilities;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Vector2 = System.Numerics.Vector2;

namespace ImGuiXNA;

public sealed class ImGuiXnaPlatform
{
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

        // TODO: Implement a correct solution for input capturing.

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
                case TouchLocationState.Pressed:  io.AddMouseButtonEvent((int)ImGuiMouseButton.Left, true); break;
                case TouchLocationState.Released: io.AddMouseButtonEvent((int)ImGuiMouseButton.Left, false); break;
                case TouchLocationState.Moved:    io.AddMousePosEvent(touch.Position.X, touch.Position.Y); break;

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
        switch (key)
        {
            // Basic.
            case Keys.Tab:              return ImGuiKey.Tab;
            case Keys.Enter:            return ImGuiKey.Enter;
            case Keys.Escape:           return ImGuiKey.Escape;
            case Keys.Space:            return ImGuiKey.Space;
            case Keys.Back:             return ImGuiKey.Backspace;
            case Keys.Insert:           return ImGuiKey.Insert;
            case Keys.Delete:           return ImGuiKey.Delete;
            case Keys.Home:             return ImGuiKey.Home;
            case Keys.End:              return ImGuiKey.End;
            case Keys.PageUp:           return ImGuiKey.PageUp;
            case Keys.PageDown:         return ImGuiKey.PageDown;
            case Keys.Pause:            return ImGuiKey.Pause;
            case Keys.PrintScreen:      return ImGuiKey.PrintScreen;
            case Keys.CapsLock:         return ImGuiKey.CapsLock;
            case Keys.NumLock:          return ImGuiKey.NumLock;
            case Keys.Scroll:           return ImGuiKey.ScrollLock;
            // Arrows.
            case Keys.Left:             return ImGuiKey.LeftArrow;
            case Keys.Right:            return ImGuiKey.RightArrow;
            case Keys.Up:               return ImGuiKey.UpArrow;
            case Keys.Down:             return ImGuiKey.DownArrow;
            // Modifiers.
            case Keys.LeftShift:        return ImGuiKey.LeftShift;
            case Keys.RightShift:       return ImGuiKey.RightShift;
            case Keys.LeftControl:      return ImGuiKey.LeftCtrl;
            case Keys.RightControl:     return ImGuiKey.RightCtrl;
            case Keys.LeftAlt:          return ImGuiKey.LeftAlt;
            case Keys.RightAlt:         return ImGuiKey.RightAlt;
            case Keys.LeftWindows:      return ImGuiKey.LeftSuper;
            case Keys.RightWindows:     return ImGuiKey.RightSuper;
            case Keys.Apps:             return ImGuiKey.Menu;
            // Digits.
            case Keys.D0:               return ImGuiKey.Key0;
            case Keys.D1:               return ImGuiKey.Key1;
            case Keys.D2:               return ImGuiKey.Key2;
            case Keys.D3:               return ImGuiKey.Key3;
            case Keys.D4:               return ImGuiKey.Key4;
            case Keys.D5:               return ImGuiKey.Key5;
            case Keys.D6:               return ImGuiKey.Key6;
            case Keys.D7:               return ImGuiKey.Key7;
            case Keys.D8:               return ImGuiKey.Key8;
            case Keys.D9:               return ImGuiKey.Key9;
            // Letters.
            case Keys.A:                return ImGuiKey.A;
            case Keys.B:                return ImGuiKey.B;
            case Keys.C:                return ImGuiKey.C;
            case Keys.D:                return ImGuiKey.D;
            case Keys.E:                return ImGuiKey.E;
            case Keys.F:                return ImGuiKey.F;
            case Keys.G:                return ImGuiKey.G;
            case Keys.H:                return ImGuiKey.H;
            case Keys.I:                return ImGuiKey.I;
            case Keys.J:                return ImGuiKey.J;
            case Keys.K:                return ImGuiKey.K;
            case Keys.L:                return ImGuiKey.L;
            case Keys.M:                return ImGuiKey.M;
            case Keys.N:                return ImGuiKey.N;
            case Keys.O:                return ImGuiKey.O;
            case Keys.P:                return ImGuiKey.P;
            case Keys.Q:                return ImGuiKey.Q;
            case Keys.R:                return ImGuiKey.R;
            case Keys.S:                return ImGuiKey.S;
            case Keys.T:                return ImGuiKey.T;
            case Keys.U:                return ImGuiKey.U;
            case Keys.V:                return ImGuiKey.V;
            case Keys.W:                return ImGuiKey.W;
            case Keys.X:                return ImGuiKey.X;
            case Keys.Y:                return ImGuiKey.Y;
            case Keys.Z:                return ImGuiKey.Z;
            // Function keys.
            case Keys.F1:               return ImGuiKey.F1;
            case Keys.F2:               return ImGuiKey.F2;
            case Keys.F3:               return ImGuiKey.F3;
            case Keys.F4:               return ImGuiKey.F4;
            case Keys.F5:               return ImGuiKey.F5;
            case Keys.F6:               return ImGuiKey.F6;
            case Keys.F7:               return ImGuiKey.F7;
            case Keys.F8:               return ImGuiKey.F8;
            case Keys.F9:               return ImGuiKey.F9;
            case Keys.F10:              return ImGuiKey.F10;
            case Keys.F11:              return ImGuiKey.F11;
            case Keys.F12:              return ImGuiKey.F12;
            case Keys.F13:              return ImGuiKey.F13;
            case Keys.F14:              return ImGuiKey.F14;
            case Keys.F15:              return ImGuiKey.F15;
            case Keys.F16:              return ImGuiKey.F16;
            case Keys.F17:              return ImGuiKey.F17;
            case Keys.F18:              return ImGuiKey.F18;
            case Keys.F19:              return ImGuiKey.F19;
            case Keys.F20:              return ImGuiKey.F20;
            case Keys.F21:              return ImGuiKey.F21;
            case Keys.F22:              return ImGuiKey.F22;
            case Keys.F23:              return ImGuiKey.F23;
            case Keys.F24:              return ImGuiKey.F24;
            // Numpad.
            case Keys.NumPad0:          return ImGuiKey.Keypad0;
            case Keys.NumPad1:          return ImGuiKey.Keypad1;
            case Keys.NumPad2:          return ImGuiKey.Keypad2;
            case Keys.NumPad3:          return ImGuiKey.Keypad3;
            case Keys.NumPad4:          return ImGuiKey.Keypad4;
            case Keys.NumPad5:          return ImGuiKey.Keypad5;
            case Keys.NumPad6:          return ImGuiKey.Keypad6;
            case Keys.NumPad7:          return ImGuiKey.Keypad7;
            case Keys.NumPad8:          return ImGuiKey.Keypad8;
            case Keys.NumPad9:          return ImGuiKey.Keypad9;
            case Keys.Add:              return ImGuiKey.KeypadAdd;
            case Keys.Subtract:         return ImGuiKey.KeypadSubtract;
            case Keys.Multiply:         return ImGuiKey.KeypadMultiply;
            case Keys.Divide:           return ImGuiKey.KeypadDivide;
            case Keys.Decimal:          return ImGuiKey.KeypadDecimal;
            // OEM / punctuation.
            case Keys.OemSemicolon:     return ImGuiKey.Semicolon;
            case Keys.OemPlus:          return ImGuiKey.Equal;
            case Keys.OemComma:         return ImGuiKey.Comma;
            case Keys.OemMinus:         return ImGuiKey.Minus;
            case Keys.OemPeriod:        return ImGuiKey.Period;
            case Keys.OemQuestion:      return ImGuiKey.Slash;
            case Keys.OemTilde:         return ImGuiKey.GraveAccent;
            case Keys.OemOpenBrackets:  return ImGuiKey.LeftBracket;
            case Keys.OemCloseBrackets: return ImGuiKey.RightBracket;
            case Keys.OemPipe:          return ImGuiKey.Backslash;
            case Keys.OemQuotes:        return ImGuiKey.Apostrophe;
            // Browser.
            case Keys.BrowserBack:        return ImGuiKey.AppBack;
            case Keys.BrowserForward:     return ImGuiKey.AppForward;
            // Unmapped.
            case Keys.None:
            case Keys.Select:
            case Keys.Print:
            case Keys.Execute:
            case Keys.Help:
            case Keys.Sleep:
            case Keys.Separator:
            case Keys.BrowserRefresh:
            case Keys.BrowserStop:
            case Keys.BrowserSearch:
            case Keys.BrowserFavorites:
            case Keys.BrowserHome:
            case Keys.VolumeMute:
            case Keys.VolumeDown:
            case Keys.VolumeUp:
            case Keys.MediaNextTrack:
            case Keys.MediaPreviousTrack:
            case Keys.MediaStop:
            case Keys.MediaPlayPause:
            case Keys.LaunchMail:
            case Keys.SelectMedia:
            case Keys.LaunchApplication1:
            case Keys.LaunchApplication2:
            case Keys.Oem8:
            case Keys.OemBackslash:
            case Keys.ProcessKey:
            case Keys.Attn:
            case Keys.Crsel:
            case Keys.Exsel:
            case Keys.EraseEof:
            case Keys.Play:
            case Keys.Zoom:
            case Keys.Pa1:
            case Keys.OemClear:
            case Keys.ChatPadGreen:
            case Keys.ChatPadOrange:
            case Keys.ImeConvert:
            case Keys.ImeNoConvert:
            case Keys.Kana:
            case Keys.Kanji:
            case Keys.OemAuto:
            case Keys.OemCopy:
            case Keys.OemEnlW:
            default:                      return ImGuiKey.None;
        }
    }

    #endregion
}