namespace PsychoEngine.Input;

public abstract class MouseEventArgs : EventArgs
{
    public Point Position { get; }
    
    public MouseEventArgs(Point position)
    {
        Position = position;
    }
}

public class MouseButtonEventArgs : MouseEventArgs
{
    public MouseButtons Button { get; }

    public MouseButtonEventArgs(MouseButtons button, Point position)
        : base(position)
    {
        Button = button;
    }
}

public class MouseMovedEventArgs : MouseEventArgs
{
    public Point PreviousPosition { get; }
    public Point PositionDelta    { get; }

    public MouseMovedEventArgs(Point previousPosition, Point position, Point positionDelta)
        : base(position)
    {
        PreviousPosition = previousPosition;
        PositionDelta    = positionDelta;
    }
}

public class MouseScrolledEventArgs : MouseEventArgs
{
    public float ScrollValue { get; }
    public float ScrollDelta { get; }

    public MouseScrolledEventArgs(float scrollValue, float scrollDelta, Point position)
        : base(position)
    {
        ScrollValue = scrollValue;
        ScrollDelta = scrollDelta;
    }
}

public class MouseDraggedEventArgs : MouseEventArgs
{
    public MouseButtons Button            { get; }
    public Point        DragStartPosition { get; }

    public MouseDraggedEventArgs(MouseButtons button, Point dragStartPosition, Point position)
        : base(position)
    {
        Button            = button;
        DragStartPosition = dragStartPosition;
    }
}

public class MouseMultiClickEventArgs : MouseEventArgs
{
    public MouseButtons Button            { get; }
    public int          ClickCount { get; }

    public MouseMultiClickEventArgs(MouseButtons button, int clickCount, Point position)
        : base(position)
    {
        Button            = button;
        ClickCount = clickCount;
    }
}