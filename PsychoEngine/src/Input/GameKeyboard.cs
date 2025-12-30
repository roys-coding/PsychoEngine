using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using Vector4 = System.Numerics.Vector4;

namespace PsychoEngine.Input;

public static class GameKeyboard
{
    public class KeyboardEventArgs(Keys key) : EventArgs
    {
        public readonly Keys Key = key;
    }

    public delegate void KeyboardEventHandler(object? sender, KeyboardEventArgs e);

    public static event KeyboardEventHandler OnKeyPressed;
    public static event KeyboardEventHandler OnKeyDown;
    public static event KeyboardEventHandler OnKeyReleased;

    public static readonly Keys[] AllKeys;

    private static KeyboardState _currentState;
    private static KeyboardState _previousState;

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
            InputState state     = GetKey(key);
            string     keyString = $"{key}: {state}";

            switch (state)
            {
                case InputState.Down:     ImGui.Text(keyString); break;
                case InputState.Up:       ImGui.TextDisabled(keyString); break;
                case InputState.Pressed:  ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), keyString); break;
                case InputState.Released: ImGui.TextColored(new Vector4(0f, 1f, 0f, 1f), keyString); break;
            }
        }

        ImGui.End();
    }

    public static void Update(Game game)
    {
        _previousState = _currentState;
        _currentState  = Keyboard.GetState();

        foreach (Keys key in AllKeys)
        {
            InputState state = GetKey(key);

            switch (state)
            {
                case InputState.Pressed:  OnKeyPressed?.Invoke(null, new KeyboardEventArgs(key)); break;
                case InputState.Down:     OnKeyDown?.Invoke(null, new KeyboardEventArgs(key)); break;
                case InputState.Released: OnKeyReleased?.Invoke(null, new KeyboardEventArgs(key)); break;

                case InputState.Up: 
                default: break;
            }
        }
    }

    public static InputState GetKey(Keys key)
    {
        InputState state;

        if (_currentState.IsKeyDown(key))
        {
            state = _previousState.IsKeyDown(key) ? InputState.Down : InputState.Pressed;
        }
        else
        {
            state = _previousState.IsKeyDown(key) ? InputState.Released : InputState.Up;
        }

        return state;
    }

    public static bool CheckKey(Keys key, InputState inputState)
    {
        return GetKey(key) == inputState;
    }

    public static bool IsKeyUp(Keys key)
    {
        return GetKey(key) == InputState.Up;
    }

    public static bool IsKeyPressed(Keys key)
    {
        return GetKey(key) == InputState.Pressed;
    }

    public static bool IsKeyDown(Keys key)
    {
        return GetKey(key) == InputState.Down;
    }

    public static bool IsKeyReleased(Keys key)
    {
        return GetKey(key) == InputState.Released;
    }
}