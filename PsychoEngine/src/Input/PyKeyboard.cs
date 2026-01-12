using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public static partial class PyKeyboard
{
    #region Events

    public static event EventHandler<KeyboardEventArgs>? OnKeyDown;
    public static event EventHandler<KeyboardEventArgs>? OnKeyPressed;
    public static event EventHandler<KeyboardEventArgs>? OnKeyReleased;

    #endregion

    #region Fields

    // Constants.
    internal static readonly Keys[] AllKeys;

    // Config.
    private static FocusLostInputBehaviour _focusLostInputBehaviour;

    // States.
    private static KeyboardState _currentState;
    private static KeyboardState _previousState;
    private static Keys[]?       _allKeysDown;

    #endregion

    #region Properties

    // Time stamps.
    public static TimeSpan LastInputTime { get; private set; }

    // Modifier keys.
    public static bool ModShift   => IsKeyDown(Keys.LeftShift) || IsKeyDown(Keys.RightShift);
    public static bool ModControl => IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl);
    public static bool ModAlt     => IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt);
    public static bool ModSuper   => IsKeyDown(Keys.LeftWindows) || IsKeyDown(Keys.RightWindows);

    public static ModifierKeys ModifierKeysDown
    {
        get
        {
            ModifierKeys modifierKeys = ModifierKeys.None;

            if (ModShift)
            {
                modifierKeys |= ModifierKeys.Shift;
            }

            if (ModControl)
            {
                modifierKeys |= ModifierKeys.Control;
            }

            if (ModAlt)
            {
                modifierKeys |= ModifierKeys.Alt;
            }

            if (ModSuper)
            {
                modifierKeys |= ModifierKeys.Super;
            }

            return modifierKeys;
        }
    }

    #endregion

    static PyKeyboard()
    {
        AllKeys = Enum.GetValues<Keys>();

        InitializeImGui();
    }

    #region Public interface

    public static Keys[] GetAllKeysDown()
    {
        // Cache current frame's pressed keys.
        _allKeysDown ??= _currentState.GetPressedKeys();

        return _allKeysDown;
    }

    public static InputStates GetKeyState(Keys key)
    {
        InputStates inputState = InputStates.None;

        if (IsKeyUp(key))
        {
            inputState |= InputStates.Up;
        }

        if (IsKeyDown(key))
        {
            inputState |= InputStates.Down;
        }

        if (WasKeyPressed(key))
        {
            inputState |= InputStates.Pressed;
        }

        if (WasKeyReleased(key))
        {
            inputState |= InputStates.Released;
        }

        return inputState;
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

    #endregion

    #region Non public methods

    internal static void Update(Game game)
    {
        UpdateKeyboardStates(game);

        // Clear previous frame's cached pressed keys.
        _allKeysDown = null;

        bool receivedAnyInput = false;

        // Handle input handlers.
        foreach (Keys key in AllKeys)
        {
            if (WasKeyPressed(key))
            {
                OnKeyPressed?.Invoke(game, new KeyboardEventArgs(key, ModifierKeysDown));
                receivedAnyInput = true;
            }

            if (IsKeyDown(key))
            {
                OnKeyDown?.Invoke(game, new KeyboardEventArgs(key, ModifierKeysDown));
                receivedAnyInput = true;
            }

            if (WasKeyReleased(key))
            {
                OnKeyReleased?.Invoke(game, new KeyboardEventArgs(key, ModifierKeysDown));
                receivedAnyInput = true;
            }
        }

        if (receivedAnyInput)
        {
            LastInputTime = PyGameTimes.Update.TotalGameTime;
        }
    }

    private static void UpdateKeyboardStates(Game game)
    {
        if (game.IsActive && !ImGui.GetIO().WantCaptureKeyboard)
        {
            // Update input state normally.
            _previousState = _currentState;
            _currentState  = Keyboard.GetState();

            return;
        }

        switch (_focusLostInputBehaviour)
        {
            case FocusLostInputBehaviour.ClearStates:
                // Pass an empty state, releasing all keys.
                _previousState = _currentState;
                _currentState  = default(KeyboardState);
                break;

            case FocusLostInputBehaviour.FreezeStates:
                // Maintain previous state, not releasing nor pressing any more keys.
                _previousState = _currentState;
                break;

            case FocusLostInputBehaviour.KeepUpdating:
                // Update input state normally.
                _previousState = _currentState;
                _currentState  = Keyboard.GetState();
                break;

            default:
                throw new NotSupportedException($"FocusLostInputBehaviour '{_focusLostInputBehaviour}' not supported.");
        }
    }

    #endregion
}