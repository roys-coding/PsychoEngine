namespace PsychoEngine.Input;

public class MouseButtonEventArgs : EventArgs
{
    public MouseButtons Button { get; }
    public Point Position { get; }

    public MouseButtonEventArgs(MouseButtons button, Point position)
    {
        Button        = button;
        Position = position;
    }
}

public class MouseMovedEventArgs : EventArgs
{
    public Point PreviousPosition { get; }
    public Point Position         { get; }
    public Point PositionDelta    { get; }

    public MouseMovedEventArgs(Point previousPosition, Point position, Point positionDelta)
    {
        PreviousPosition = previousPosition;
        Position         = position;
        PositionDelta    = positionDelta;
    }
}

public class MouseScrolledEventArgs : EventArgs
{
    public float ScrollValue { get; }
    public float ScrollDelta { get; }
    public Point Position    { get; }

    public MouseScrolledEventArgs(float scrollValue, float scrollDelta, Point position)
    {
        ScrollValue   = scrollValue;
        ScrollDelta   = scrollDelta;
        Position = position;
    }
}

public class MouseDraggedEventArgs : EventArgs
{
    public MouseButtons Button            { get; }
    public Point        DragStartPosition { get; }
    public Point        Position          { get; }

    public MouseDraggedEventArgs(MouseButtons button, Point dragStartPosition, Point position)
    {
        Button            = button;
        DragStartPosition = dragStartPosition;
        Position     = position;
    }
}

public class MouseMultiClickEventArgs : EventArgs
{
    public MouseButtons Button     { get; }
    public int          ClickCount { get; }
    public Point        Position          { get; }

    public MouseMultiClickEventArgs(MouseButtons button, int clickCount, Point position)
    {
        Button        = button;
        ClickCount    = clickCount;
        Position = position;
    }
}