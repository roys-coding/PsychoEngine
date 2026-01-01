using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public readonly struct MouseSnapshot
{
    public Point PreviousPosition { get; }
    public Point Position         { get; }
    public Point PositionDelta    { get; }
    public bool  WasMoved         { get; }

    public float PreviousScrollValue { get; }
    public float ScrollValue         { get; }
    public float ScrollDelta         { get; }
    public bool  WasScrolled         { get; }

    public MouseSnapshot() { }

    public ButtonState GetButton(MouseButtons button)
    {
        throw new NotImplementedException();
    }

    public bool IsButtonUp(MouseButtons button)
    {
        throw new NotImplementedException();
    }

    public bool IsButtonDown(MouseButtons button)
    {
        throw new NotImplementedException();
    }

    public bool WasButtonPressed(MouseButtons button)
    {
        throw new NotImplementedException();
    }

    public bool WasButtonReleased(MouseButtons button)
    {
        throw new NotImplementedException();
    }
}