using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public class PyGamePad
{
    // Todo: implement FNA Ext (gyro, rumble, etc)

    #region Fields

    // States.
    private GamePadState _previousState;
    private GamePadState _currentState;

    // Connection.
    private bool _previousIsConnected;

    #endregion

    #region Properties

    public PlayerIndex PlayerIndex { get; }

    // Device state.
    public bool IsConnected { get; private set; }

    // Time stamps.
    public TimeSpan LastInputTime { get; private set; }

    #endregion

    internal PyGamePad(PlayerIndex playerIndex)
    {
        PlayerIndex = playerIndex;
    }

    #region Public interface

    // Connection.
    public bool WasConnected()
    {
        return !_previousIsConnected && IsConnected;
    }

    public bool WasDisconnected()
    {
        return _previousIsConnected && !IsConnected;
    }

    // Buttons.
    public InputStates GetButtonState(GamePadButton button)
    {
        InputStates inputState = InputStates.None;

        if (IsButtonUp(button)) inputState        |= InputStates.Up;
        if (IsButtonDown(button)) inputState      |= InputStates.Down;
        if (WasButtonPressed(button)) inputState  |= InputStates.Pressed;
        if (WasButtonReleased(button)) inputState |= InputStates.Released;

        return inputState;
    }

    public bool IsButtonUp(GamePadButton button)
    {
        return GetButtonState(button, _currentState) != ButtonState.Pressed;
    }

    public bool IsButtonDown(GamePadButton button)
    {
        return GetButtonState(button, _currentState) == ButtonState.Pressed;
    }

    public bool WasButtonPressed(GamePadButton button)
    {
        ButtonState previousState = GetButtonState(button, _previousState);
        ButtonState currentState  = GetButtonState(button, _currentState);

        return previousState == ButtonState.Released && currentState == ButtonState.Pressed;
    }

    public bool WasButtonReleased(GamePadButton button)
    {
        ButtonState previousState = GetButtonState(button, _previousState);
        ButtonState currentState  = GetButtonState(button, _currentState);

        return previousState == ButtonState.Pressed && currentState == ButtonState.Released;
    }

    // Triggers.
    public float GetTrigger(GamePadTrigger trigger)
    {
        return GetTriggerInternal(trigger, _currentState);
    }

    public float GetTriggerDelta(GamePadTrigger trigger)
    {
        float previousValue = GetTriggerInternal(trigger, _previousState);
        float currentValue  = GetTriggerInternal(trigger, _currentState);

        return currentValue - previousValue;
    }

    public bool DidTriggerMove(GamePadTrigger trigger)
    {
        float previousValue = GetTriggerInternal(trigger, _previousState);
        float currentValue  = GetTriggerInternal(trigger, _currentState);

        // BUG: Check if tolerance should be added.
        return currentValue != previousValue;
    }

    // Thumbsticks.
    public Vector2 GetThumbstick(GamePadThumbstick thumbstick)
    {
        return GetThumbstickInternal(thumbstick, _currentState);
    }

    public Vector2 GetThumbstickDelta(GamePadThumbstick thumbstick)
    {
        Vector2 previousValue = GetThumbstickInternal(thumbstick, _previousState);
        Vector2 currentValue  = GetThumbstickInternal(thumbstick, _currentState);

        return currentValue - previousValue;
    }

    public InputStates GetThumbstickButtonState(GamePadThumbstick thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbstick.None => InputStates.Up,
                   GamePadThumbstick.Left => GetButtonState(GamePadButton.LeftThumb),
                   GamePadThumbstick.Right => GetButtonState(GamePadButton.RightThumb),
                   _ => throw new NotSupportedException($"Thumbstick '{thumbstick}' not supported."),
               };
    }

    public bool IsThumbstickButtonUp(GamePadThumbstick thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbstick.None => false,
                   GamePadThumbstick.Left => IsButtonUp(GamePadButton.LeftThumb),
                   GamePadThumbstick.Right => IsButtonUp(GamePadButton.RightThumb),
                   _ => throw new InvalidOperationException($"Thumbsticks '{thumbstick}' not supported."),
               };
    }

    public bool IsThumbstickButtonDown(GamePadThumbstick thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbstick.None => false,
                   GamePadThumbstick.Left => IsButtonDown(GamePadButton.LeftThumb),
                   GamePadThumbstick.Right => IsButtonDown(GamePadButton.RightThumb),
                   _ => throw new InvalidOperationException($"Thumbsticks '{thumbstick}' not supported."),
               };
    }

    public bool WasThumbstickButtonPressed(GamePadThumbstick thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbstick.None => false,
                   GamePadThumbstick.Left => WasButtonPressed(GamePadButton.LeftThumb),
                   GamePadThumbstick.Right => WasButtonPressed(GamePadButton.RightThumb),
                   _ => throw new NotSupportedException($"Thumbsticks '{thumbstick}' not supported."),
               };
    }

    public bool WasThumbstickButtonReleased(GamePadThumbstick thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbstick.None => false,
                   GamePadThumbstick.Left => WasButtonReleased(GamePadButton.LeftThumb),
                   GamePadThumbstick.Right => WasButtonReleased(GamePadButton.RightThumb),
                   _ => throw new InvalidOperationException($"Thumbsticks '{thumbstick}' not supported."),
               };
    }

    public bool DidThumbstickMove(GamePadThumbstick thumbstick)
    {
        Vector2 previousValue = GetThumbstickInternal(thumbstick, _previousState);
        Vector2 currentValue  = GetThumbstickInternal(thumbstick, _currentState);

        return previousValue != currentValue;
    }

    // Vibration.
    public bool SetVibration(float leftMotor, float rightMotor)
    {
        return GamePad.SetVibration(PlayerIndex, leftMotor, rightMotor);
    }

    #endregion

    #region Non public methods

    internal void Update(Game game)
    {
        UpdateGamePadStates(game);

        _previousIsConnected = IsConnected;
        IsConnected          = GamePad.GetState(PlayerIndex).IsConnected;

        bool receivedAnyInput = false;

        // Update thumbsticks.
        foreach (GamePadThumbstick thumbstick in PyGamePads.ThumbsticksEnum)
        {
            if (GetThumbstick(thumbstick) != Vector2.Zero)
            {
                receivedAnyInput = true;
            }
        }

        // Update triggers.
        foreach (GamePadTrigger trigger in PyGamePads.TriggersEnum)
        {
            if (GetTrigger(trigger) != 0f)
            {
                receivedAnyInput = true;
            }
        }

        // Update buttons.
        foreach (GamePadButton button in PyGamePads.ButtonsEnum)
        {
            if (WasButtonPressed(button))
            {
                receivedAnyInput = true;
            }

            if (IsButtonDown(button))
            {
                receivedAnyInput = true;
            }

            if (WasButtonReleased(button))
            {
                receivedAnyInput = true;
            }
        }

        if (receivedAnyInput)
        {
            LastInputTime = PyGameTimes.Update.TotalGameTime;
        }
    }

    private void UpdateGamePadStates(Game game)
    {
        if (game.IsActive && !ImGui.GetIO().WantCaptureKeyboard)
        {
            // Update input state normally.
            _previousState = _currentState;
            _currentState  = GamePad.GetState(PlayerIndex);

            return;
        }

        switch (PyGamePads.FocusLostInputBehaviour)
        {
            case FocusLostInputBehaviour.ClearStates:
                // Pass an empty state, releasing all keys.
                _previousState = _currentState;
                _currentState  = default(GamePadState);
                break;

            case FocusLostInputBehaviour.FreezeStates:
                // Maintain previous state, not releasing nor pressing any more keys.
                _previousState = _currentState;
                break;

            case FocusLostInputBehaviour.KeepUpdating:
                // Update input state normally.
                _previousState = _currentState;
                _currentState  = GamePad.GetState(PlayerIndex);
                break;

            default:
                throw new
                    InvalidOperationException($"FocusLostInputBehaviour '{PyGamePads.FocusLostInputBehaviour}' not supported.");
        }
    }

    private static ButtonState GetButtonState(GamePadButton button, GamePadState state)
    {
        return button switch
               {
                   GamePadButton.None => ButtonState.Released,
                   GamePadButton.A => state.Buttons.A,
                   GamePadButton.B => state.Buttons.B,
                   GamePadButton.X => state.Buttons.X,
                   GamePadButton.Y => state.Buttons.Y,
                   GamePadButton.DPadUp => state.DPad.Up,
                   GamePadButton.DPadDown => state.DPad.Down,
                   GamePadButton.DPadLeft => state.DPad.Left,
                   GamePadButton.DPadRight => state.DPad.Right,
                   GamePadButton.LeftShoulder => state.Buttons.LeftShoulder,
                   GamePadButton.RightShoulder => state.Buttons.RightShoulder,
                   GamePadButton.LeftThumb => state.Buttons.LeftStick,
                   GamePadButton.RightThumb => state.Buttons.RightStick,
                   GamePadButton.Back => state.Buttons.Back,
                   GamePadButton.Start => state.Buttons.Start,
                   GamePadButton.Guide => state.Buttons.BigButton,
                   _ => throw new NotSupportedException($"Button '{button}' not supported."),
               };
    }

    private float GetTriggerInternal(GamePadTrigger trigger, GamePadState state)
    {
        return trigger switch
               {
                   GamePadTrigger.None  => 0f,
                   GamePadTrigger.Left  => state.Triggers.Left,
                   GamePadTrigger.Right => state.Triggers.Right,
                   _                    => throw new InvalidOperationException($"Trigger '{trigger}' not supported."),
               };
    }

    private Vector2 GetThumbstickInternal(GamePadThumbstick thumbstick, GamePadState state)
    {
        return thumbstick switch
               {
                   GamePadThumbstick.None => Vector2.Zero,
                   GamePadThumbstick.Left => state.ThumbSticks.Left,
                   GamePadThumbstick.Right => state.ThumbSticks.Right,
                   _ => throw new InvalidOperationException($"Thumbstick '{thumbstick}' not supported."),
               };
    }

    #endregion
}