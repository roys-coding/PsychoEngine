using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public readonly struct GameMouseState
{
    // Buttons.
    public ButtonState LeftButton   { get; }
    public ButtonState MiddleButton { get; }
    public ButtonState RightButton  { get; }
    public ButtonState X1Button     { get; }
    public ButtonState X2Button     { get; }

    public bool WasLeftButtonPressed   { get; }
    public bool WasMiddleButtonPressed { get; }
    public bool WasRightButtonPressed  { get; }
    public bool WasX1ButtonPressed     { get; }
    public bool WasX2ButtonPressed     { get; }

    public bool WasLeftButtonReleased   { get; }
    public bool WasMiddleButtonReleased { get; }
    public bool WasRightButtonReleased  { get; }
    public bool WasX1ButtonReleased     { get; }
    public bool WasX2ButtonReleased     { get; }

    // Position.
    public Point Position      { get; }
    public Point PositionDelta { get; }
    public bool  HasMoved      { get; }

    // Scroll wheel.
    public float ScrollValue { get; }
    public float ScrollDelta { get; }
    public bool  HasScrolled { get; }

    public GameMouseState(
        ButtonState leftButton,
        ButtonState middleButton,
        ButtonState rightButton,
        ButtonState x1Button,
        ButtonState x2Button,
        bool        wasLeftButtonPressed,
        bool        wasMiddleButtonPressed,
        bool        wasRightButtonPressed,
        bool        wasX1ButtonPressed,
        bool        wasX2ButtonPressed,
        bool        wasLeftButtonReleased,
        bool        wasMiddleButtonReleased,
        bool        wasRightButtonReleased,
        bool        wasX1ButtonReleased,
        bool        wasX2ButtonReleased,
        Point       position,
        Point       positionDelta,
        bool        hasMoved,
        float       scrollValue,
        float       scrollDelta,
        bool        hasScrolled
    )
    {
        LeftButton              = leftButton;
        MiddleButton            = middleButton;
        RightButton             = rightButton;
        X1Button                = x1Button;
        X2Button                = x2Button;
        WasLeftButtonPressed    = wasLeftButtonPressed;
        WasMiddleButtonPressed  = wasMiddleButtonPressed;
        WasRightButtonPressed   = wasRightButtonPressed;
        WasX1ButtonPressed      = wasX1ButtonPressed;
        WasX2ButtonPressed      = wasX2ButtonPressed;
        WasLeftButtonReleased   = wasLeftButtonReleased;
        WasMiddleButtonReleased = wasMiddleButtonReleased;
        WasRightButtonReleased  = wasRightButtonReleased;
        WasX1ButtonReleased     = wasX1ButtonReleased;
        WasX2ButtonReleased     = wasX2ButtonReleased;
        Position                = position;
        PositionDelta           = positionDelta;
        HasMoved                = hasMoved;
        ScrollValue             = scrollValue;
        ScrollDelta             = scrollDelta;
        HasScrolled             = hasScrolled;
    }

    public ButtonState GetButton(MouseButtons button)
    {
        return button switch
               {
                   MouseButtons.None   => ButtonState.Released,
                   MouseButtons.Left   => LeftButton,
                   MouseButtons.Middle => MiddleButton,
                   MouseButtons.Right  => RightButton,
                   MouseButtons.X1     => X1Button,
                   MouseButtons.X2     => X2Button,
                   _                   => throw new InvalidOperationException($"Button '{button}' not supported."),
               };
    }

    public bool IsButtonDown(MouseButtons button)
    {
        return GetButton(button) == ButtonState.Pressed;
    }

    public bool IsButtonUp(MouseButtons button)
    {
        return GetButton(button) == ButtonState.Pressed;
    }

    public bool WasButtonPressed(MouseButtons button)
    {
        return button switch
               {
                   MouseButtons.None   => false,
                   MouseButtons.Left   => WasLeftButtonPressed,
                   MouseButtons.Middle => WasMiddleButtonPressed,
                   MouseButtons.Right  => WasRightButtonPressed,
                   MouseButtons.X1     => WasX1ButtonPressed,
                   MouseButtons.X2     => WasX2ButtonPressed,
                   _                   => throw new InvalidOperationException($"Button '{button}' not supported."),
               };
    }

    public bool WasButtonReleased(MouseButtons button)
    {
        return button switch
               {
                   MouseButtons.None   => false,
                   MouseButtons.Left   => WasLeftButtonReleased,
                   MouseButtons.Middle => WasMiddleButtonReleased,
                   MouseButtons.Right  => WasRightButtonReleased,
                   MouseButtons.X1     => WasX1ButtonReleased,
                   MouseButtons.X2     => WasX2ButtonReleased,
                   _                   => throw new InvalidOperationException($"Button '{button}' not supported."),
               };
    }
}