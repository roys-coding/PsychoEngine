using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public class PyGamePad
{
    // Todo: implement FNA Ext (gyro, rumble, etc)
    // TODO: implement events.
    private readonly PlayerIndex _playerIndex;

    // States.
    private GamePadState _previousState;
    private GamePadState _currentState;

    public bool IsConnected => GamePad.GetState(_playerIndex).IsConnected;

    // Time stamps.
    public TimeSpan LastInputTime { get; private set; }

    internal PyGamePad(PlayerIndex playerIndex)
    {
        _playerIndex = playerIndex;
    }

    public InputStates GetButton(GamePadButton button)
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

    public float GetTrigger(GamePadTrigger trigger)
    {
        return trigger switch
               {
                   GamePadTrigger.None  => 0f,
                   GamePadTrigger.Left  => _currentState.Triggers.Left,
                   GamePadTrigger.Right => _currentState.Triggers.Right,
                   _                     => throw new InvalidOperationException($"Trigger '{trigger}' not supported."),
               };
    }

    public Vector2 GetThumbstick(GamePadThumbstick thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbstick.None => Vector2.Zero,
                   GamePadThumbstick.Left => _currentState.ThumbSticks.Left,
                   GamePadThumbstick.Right => _currentState.ThumbSticks.Right,
                   _ => throw new InvalidOperationException($"Thumbstick '{thumbstick}' not supported."),
               };
    }

    public InputStates GetThumbstickButton(GamePadThumbstick thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbstick.None => InputStates.Up,
                   GamePadThumbstick.Left => GetButton(GamePadButton.LeftThumb),
                   GamePadThumbstick.Right => GetButton(GamePadButton.RightThumb),
                   _ => throw new InvalidOperationException($"Thumbstick '{thumbstick}' not supported."),
               };
    }

    public bool IsThumbstickUp(GamePadThumbstick thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbstick.None => false,
                   GamePadThumbstick.Left => IsButtonUp(GamePadButton.LeftThumb),
                   GamePadThumbstick.Right => IsButtonUp(GamePadButton.RightThumb),
                   _ => throw new InvalidOperationException($"Thumbsticks '{thumbstick}' not supported."),
               };
    }

    public bool IsThumbstickDown(GamePadThumbstick thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbstick.None => false,
                   GamePadThumbstick.Left => IsButtonDown(GamePadButton.LeftThumb),
                   GamePadThumbstick.Right => IsButtonDown(GamePadButton.RightThumb),
                   _ => throw new InvalidOperationException($"Thumbsticks '{thumbstick}' not supported."),
               };
    }

    public bool WasThumbstickPressed(GamePadThumbstick thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbstick.None => false,
                   GamePadThumbstick.Left => WasButtonPressed(GamePadButton.LeftThumb),
                   GamePadThumbstick.Right => WasButtonPressed(GamePadButton.RightThumb),
                   _ => throw new InvalidOperationException($"Thumbsticks '{thumbstick}' not supported."),
               };
    }

    public bool WasThumbstickReleased(GamePadThumbstick thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbstick.None => false,
                   GamePadThumbstick.Left => WasButtonReleased(GamePadButton.LeftThumb),
                   GamePadThumbstick.Right => WasButtonReleased(GamePadButton.RightThumb),
                   _ => throw new InvalidOperationException($"Thumbsticks '{thumbstick}' not supported."),
               };
    }

    public bool SetVibration(float leftMotor, float rightMotor)
    {
        return GamePad.SetVibration(_playerIndex, leftMotor, rightMotor);
    }

    #region Internal methods

    internal void Update(Game game)
    {
        if (game.IsActive && !ImGui.GetIO().WantCaptureKeyboard)
        {
            // Update input state normally.
            _previousState = _currentState;
            _currentState  = GamePad.GetState(_playerIndex);

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
                _currentState  = GamePad.GetState(_playerIndex);
                break;

            default:
                throw new
                    InvalidOperationException($"FocusLostInputBehaviour '{PyGamePads.FocusLostInputBehaviour}' not supported.");
        }

        if (_previousState != _currentState)
        {
            LastInputTime = PyGameTimes.Update.TotalGameTime;
            // BUG: Last input time not detected correctly.
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
                   _ => throw new InvalidOperationException($"Button '{button}' not supported."),
               };
    }

    #endregion
}