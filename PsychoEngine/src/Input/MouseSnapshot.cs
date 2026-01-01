using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public readonly struct MouseSnapshot
{
    private const float WheelDeltaUnit = 120f;

    private readonly MouseState _previousState;
    private readonly MouseState _currentState;

    public Point PreviousPosition { get; }
    public Point Position         { get; }
    public Point PositionDelta    { get; }
    public bool  HasMoved         { get; }

    public float PreviousScrollValue { get; }
    public float ScrollValue         { get; }
    public float ScrollDelta         { get; }
    public bool  HasScrolled         { get; }

    public MouseSnapshot(MouseState previousState, MouseState currentState)
    {
        _previousState = previousState;
        _currentState  = currentState;

        // Position.
        PreviousPosition = new Point(_previousState.X, _previousState.Y);
        Position         = new Point(_currentState.X,  _currentState.Y);
        PositionDelta    = Position - PreviousPosition;
        HasMoved         = PositionDelta != Point.Zero;

        // Scroll.
        PreviousScrollValue = _previousState.ScrollWheelValue / WheelDeltaUnit;
        ScrollValue         = _currentState.ScrollWheelValue  / WheelDeltaUnit;
        ScrollDelta         = ScrollValue - PreviousScrollValue;
        HasScrolled         = ScrollDelta != 0f;
    }

    public ButtonState GetButton(MouseButtons button)
    {
        return GetButtonInternal(_currentState, button);
    }

    public bool IsButtonUp(MouseButtons button)
    {
        return GetButtonInternal(_currentState, button) == ButtonState.Released;
    }

    public bool IsButtonDown(MouseButtons button)
    {
        return GetButtonInternal(_currentState, button) == ButtonState.Pressed;
    }

    public bool WasButtonPressed(MouseButtons button)
    {
        ButtonState previousState = GetButtonInternal(_previousState, button);
        ButtonState currentState  = GetButtonInternal(_currentState,  button);

        return previousState == ButtonState.Released && currentState == ButtonState.Pressed;
    }

    public bool WasButtonReleased(MouseButtons button)
    {
        ButtonState previousState = GetButtonInternal(_previousState, button);
        ButtonState currentState  = GetButtonInternal(_currentState,  button);

        return previousState == ButtonState.Pressed && currentState == ButtonState.Released;
    }

    private static ButtonState GetButtonInternal(MouseState state, MouseButtons button)
    {
        return button switch
               {
                   MouseButtons.None   => ButtonState.Released,
                   MouseButtons.Left   => state.LeftButton,
                   MouseButtons.Middle => state.MiddleButton,
                   MouseButtons.Right  => state.RightButton,
                   MouseButtons.X1     => state.XButton1,
                   MouseButtons.X2     => state.XButton2,
                   _                   => throw new InvalidOperationException($"MouseButton '{button}' not supported."),
               };
    }
}