using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public static class GameKeyboard
{
    public class KeyboardEventArgs(Keys key) : EventArgs
    {
        public readonly Keys Key = key;
    }

    public delegate void KeyboardEventHandler(object? sender, KeyboardEventArgs e);

    public static event KeyboardEventHandler? OnKeyPressed;
    public static event KeyboardEventHandler? OnKeyDown;
    public static event KeyboardEventHandler? OnKeyReleased;

    public static readonly Keys[] AllKeys;

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
    
    public static bool ModShift => IsKeyDown(Keys.LeftShift) || IsKeyDown(Keys.RightShift);
    public static bool ModControl => IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl);
    public static bool ModAlt => IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt);
    public static bool ModSuper => IsKeyDown(Keys.LeftWindows) || IsKeyDown(Keys.RightWindows);

    static GameKeyboard()
    {
        AllKeys = Enum.GetValues<Keys>();

        CoreEngine.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;
    }

    private static void ImGuiOnLayout(object? sender, EventArgs args)
    {
        bool windowOpen = ImGui.Begin($"{Fonts.Lucide.Keyboard} Input");

        if (!windowOpen)
        {
            ImGui.End();

            return;
        }

        foreach (Keys key in AllKeys)
        {
            KeyState state     = GetKey(key);
            string     keyString = $"{key}: {state}";

            if (WasKeyPressed(key)) keyString += " Pressed";
            if (WasKeyReleased(key)) keyString += " Released";

            switch (state)
            {
                case KeyState.Down:     ImGui.Text(keyString); break;
                case KeyState.Up:       ImGui.TextDisabled(keyString); break;
            }
        }

        ImGui.End();
    }

    public static void Update(Game game)
    {
        _previousState = _currentState;
        _currentState  = Keyboard.GetState();
        
        // Clear previous frame's cached pressed keys.
        _allKeysDown   = null;

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