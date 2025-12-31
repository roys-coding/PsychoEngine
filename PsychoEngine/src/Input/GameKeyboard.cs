using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public static class GameKeyboard
{
    // Subclasses.
    public class KeyboardEventArgs(Keys key) : EventArgs
    {
        public Keys Key { get; } = key;
    }

    // Events.
    public delegate void KeyboardEventHandler(object? sender, KeyboardEventArgs args);

    public static event KeyboardEventHandler? OnKeyDown;
    public static event KeyboardEventHandler? OnKeyPressed;
    public static event KeyboardEventHandler? OnKeyReleased;

    public static readonly Keys[] AllKeys;

    private static FocusLostInputBehaviour _focusLostInputBehaviour = FocusLostInputBehaviour.ClearState;

    // Input state.
    private static KeyboardState _currentState;
    private static KeyboardState _previousState;
    private static Keys[]?       _allKeysDown;

    public static Keys[] AllKeysDown
    {
        get
        {
            // Cache current frame's pressed keys.
            _allKeysDown ??= _currentState.GetPressedKeys();

            return _allKeysDown;
        }
    }

    public static TimeSpan LastInputTime
    {
        get;
        private set;
    }

    public static bool ModShift
    {
        get => IsKeyDown(Keys.LeftShift) || IsKeyDown(Keys.RightShift);
    }

    public static bool ModControl
    {
        get => IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl);
    }

    public static bool ModAlt
    {
        get => IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt);
    }

    public static bool ModSuper
    {
        get => IsKeyDown(Keys.LeftWindows) || IsKeyDown(Keys.RightWindows);
    }

    static GameKeyboard()
    {
        AllKeys = Enum.GetValues<Keys>();

        CoreEngine.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;
    }

    private static void ImGuiOnLayout(object? sender, EventArgs args)
    {
        bool windowOpen = ImGui.Begin($"{Fonts.Lucide.Keyboard} Keyboard ");

        if (!windowOpen)
        {
            ImGui.End();

            return;
        }

        int  focusLost                                 = (int)_focusLostInputBehaviour;
        bool focusLostChanged                          = ImGui.DragInt("FocusLost", ref focusLost, 0, 1);
        if (focusLostChanged) _focusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;
        
        ImGui.TextDisabled($"Last input: {LastInputTime}");

        foreach (Keys key in AllKeys)
        {
            KeyState state     = GetKey(key);
            string   keyString = $"{key}: {state}";

            if (WasKeyPressed(key)) keyString  += " Pressed";
            if (WasKeyReleased(key)) keyString += " Released";

            switch (state)
            {
                case KeyState.Down: ImGui.Text(keyString); break;
                case KeyState.Up:   ImGui.TextDisabled(keyString); break;
            }
        }

        ImGui.End();
    }

    public static void Update(Game game, GameTime gameTime)
    {
        if (!game.IsActive || ImGui.GetIO().WantCaptureKeyboard)
        {
            switch (_focusLostInputBehaviour)
            {
                case FocusLostInputBehaviour.ClearState:
                    _previousState = _currentState;
                    _currentState  = default(KeyboardState);

                    break;

                case FocusLostInputBehaviour.MaintainState: _previousState = _currentState; break;

                default:
                    throw new
                        InvalidOperationException($"FocusLostInputBehaviour '{_focusLostInputBehaviour}' not supported.");
            }
        }
        else
        {
            _previousState = _currentState;
            _currentState  = Keyboard.GetState();
        }
        
        if (_previousState != _currentState)
        { 
            LastInputTime = gameTime.TotalGameTime;
        }

        // Clear previous frame's cached pressed keys.
        _allKeysDown = null;

        // Handle input handlers.
        foreach (Keys key in AllKeys)
        {
            if (IsKeyDown(key)) OnKeyDown?.Invoke(game, new KeyboardEventArgs(key));
            if (WasKeyPressed(key)) OnKeyPressed?.Invoke(game, new KeyboardEventArgs(key));
            if (WasKeyReleased(key)) OnKeyReleased?.Invoke(game, new KeyboardEventArgs(key));
        }
    }

    public static KeyState GetKey(Keys key)
    {
        return _currentState[key];
    }

    public static bool CheckKey(Keys key, KeyState inputState)
    {
        return _currentState[key] == inputState;
    }

    public static bool IsKeyUp(Keys key)
    {
        return _currentState[key] == KeyState.Up;
    }

    public static bool IsKeyDown(Keys key)
    {
        return _currentState[key] == KeyState.Down;
    }

    public static bool WasKeyPressed(Keys key)
    {
        return _previousState[key] == KeyState.Up && _currentState[key] == KeyState.Down;
    }

    public static bool WasKeyReleased(Keys key)
    {
        return _previousState[key] == KeyState.Down && _currentState[key] == KeyState.Up;
    }
}