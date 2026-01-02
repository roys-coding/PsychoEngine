namespace PsychoEngine.Input;

public abstract class MouseEventArgs : EventArgs
{
    public MouseSnapshot MouseState { get; }

    public MouseEventArgs(MouseSnapshot mouseState)
    {
        MouseState = mouseState;
    }
}

public class MouseButtonEventArgs : MouseEventArgs
{
    public MouseButtons Button { get; }

    public MouseButtonEventArgs(MouseButtons button, MouseSnapshot mouseState)
        : base(mouseState)
    {
        Button = button;
    }
}

public class MouseMovedEventArgs : MouseEventArgs
{
    public Point PreviousPosition { get; }
    public Point Position         { get; }
    public Point PositionDelta    { get; }

    public MouseMovedEventArgs(Point previousPosition, Point position, Point positionDelta, MouseSnapshot mouseState)
        : base(mouseState)
    {
        PreviousPosition = previousPosition;
        Position         = position;
        PositionDelta    = positionDelta;
    }
}

public class MouseScrolledEventArgs : MouseEventArgs
{
    public float ScrollValue { get; }
    public float ScrollDelta { get; }

    public MouseScrolledEventArgs(float scrollValue, float scrollDelta, MouseSnapshot mouseState)
        : base(mouseState)
    {
        ScrollValue = scrollValue;
        ScrollDelta = scrollDelta;
    }
}

public class MouseDraggedEventArgs : MouseEventArgs
{
    public MouseButtons Button            { get; }
    public Point        DragStartPosition { get; }

    public MouseDraggedEventArgs(MouseButtons button, Point dragStartPosition, MouseSnapshot mouseState)
        : base(mouseState)
    {
        Button            = button;
        DragStartPosition = dragStartPosition;
    }
}

public class MouseMultiClickEventArgs : MouseEventArgs
{
    public MouseButtons Button            { get; }
    public int          ClickCount { get; }

    public MouseMultiClickEventArgs(MouseButtons button, int clickCount, MouseSnapshot mouseState)
        : base(mouseState)
    {
        Button            = button;
        ClickCount = clickCount;
    }
}