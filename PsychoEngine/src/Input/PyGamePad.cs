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

    public InputStates GetButton(GamePadButtons button)
    {
        InputStates inputState = InputStates.None;

        if (IsButtonUp(button)) inputState        |= InputStates.Up;
        if (IsButtonDown(button)) inputState      |= InputStates.Down;
        if (WasButtonPressed(button)) inputState  |= InputStates.Pressed;
        if (WasButtonReleased(button)) inputState |= InputStates.Released;

        return inputState;
    }

    public bool IsButtonUp(GamePadButtons button)
    {
        return GetButtonState(button, _currentState) != ButtonState.Pressed;
    }

    public bool IsButtonDown(GamePadButtons button)
    {
        return GetButtonState(button, _currentState) == ButtonState.Pressed;
    }

    public bool WasButtonPressed(GamePadButtons button)
    {
        ButtonState previousState = GetButtonState(button, _previousState);
        ButtonState currentState  = GetButtonState(button, _currentState);

        return previousState == ButtonState.Released && currentState == ButtonState.Pressed;
    }

    public bool WasButtonReleased(GamePadButtons button)
    {
        ButtonState previousState = GetButtonState(button, _previousState);
        ButtonState currentState  = GetButtonState(button, _currentState);

        return previousState == ButtonState.Pressed && currentState == ButtonState.Released;
    }

    public float GetTrigger(GamePadTriggers trigger)
    {
        return trigger switch
               {
                   GamePadTriggers.None  => 0f,
                   GamePadTriggers.Left  => _currentState.Triggers.Left,
                   GamePadTriggers.Right => _currentState.Triggers.Right,
                   _                     => throw new InvalidOperationException($"Trigger '{trigger}' not supported."),
               };
    }

    public Vector2 GetThumbstick(GamePadThumbsticks thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbsticks.None => Vector2.Zero,
                   GamePadThumbsticks.Left => _currentState.ThumbSticks.Left,
                   GamePadThumbsticks.Right => _currentState.ThumbSticks.Right,
                   _ => throw new InvalidOperationException($"Thumbstick '{thumbstick}' not supported."),
               };
    }

    public InputStates GetButtonThumbstick(GamePadThumbsticks thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbsticks.None => InputStates.Up,
                   GamePadThumbsticks.Left => GetButton(GamePadButtons.LeftThumb),
                   GamePadThumbsticks.Right => GetButton(GamePadButtons.RightThumb),
                   _ => throw new InvalidOperationException($"Thumbstick '{thumbstick}' not supported."),
               };
    }

    public bool IsThumbstickUp(GamePadThumbsticks thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbsticks.None => false,
                   GamePadThumbsticks.Left => IsButtonUp(GamePadButtons.LeftThumb),
                   GamePadThumbsticks.Right => IsButtonUp(GamePadButtons.RightThumb),
                   _ => throw new InvalidOperationException($"Thumbsticks '{thumbstick}' not supported."),
               };
    }

    public bool IsThumbstickDown(GamePadThumbsticks thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbsticks.None => false,
                   GamePadThumbsticks.Left => IsButtonDown(GamePadButtons.LeftThumb),
                   GamePadThumbsticks.Right => IsButtonDown(GamePadButtons.RightThumb),
                   _ => throw new InvalidOperationException($"Thumbsticks '{thumbstick}' not supported."),
               };
    }

    public bool WasThumbstickPressed(GamePadThumbsticks thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbsticks.None => false,
                   GamePadThumbsticks.Left => WasButtonPressed(GamePadButtons.LeftThumb),
                   GamePadThumbsticks.Right => WasButtonPressed(GamePadButtons.RightThumb),
                   _ => throw new InvalidOperationException($"Thumbsticks '{thumbstick}' not supported."),
               };
    }

    public bool WasThumbstickReleased(GamePadThumbsticks thumbstick)
    {
        return thumbstick switch
               {
                   GamePadThumbsticks.None => false,
                   GamePadThumbsticks.Left => WasButtonReleased(GamePadButtons.LeftThumb),
                   GamePadThumbsticks.Right => WasButtonReleased(GamePadButtons.RightThumb),
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
            case FocusLostInputBehaviour.ClearState:
                // Pass an empty state, releasing all keys.
                _previousState = _currentState;
                _currentState  = default(GamePadState);
                break;

            case FocusLostInputBehaviour.MaintainState:
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

    private static ButtonState GetButtonState(GamePadButtons button, GamePadState state)
    {
        return button switch
               {
                   GamePadButtons.None => ButtonState.Released,
                   GamePadButtons.A => state.Buttons.A,
                   GamePadButtons.B => state.Buttons.B,
                   GamePadButtons.X => state.Buttons.X,
                   GamePadButtons.Y => state.Buttons.Y,
                   GamePadButtons.DPadUp => state.DPad.Up,
                   GamePadButtons.DPadDown => state.DPad.Down,
                   GamePadButtons.DPadLeft => state.DPad.Left,
                   GamePadButtons.DPadRight => state.DPad.Right,
                   GamePadButtons.LeftShoulder => state.Buttons.LeftShoulder,
                   GamePadButtons.RightShoulder => state.Buttons.RightShoulder,
                   GamePadButtons.LeftThumb => state.Buttons.LeftStick,
                   GamePadButtons.RightThumb => state.Buttons.RightStick,
                   GamePadButtons.Back => state.Buttons.Back,
                   GamePadButtons.Start => state.Buttons.Start,
                   GamePadButtons.Guide => state.Buttons.BigButton,
                   _ => throw new InvalidOperationException($"Button '{button}' not supported."),
               };
    }

    #endregion
}