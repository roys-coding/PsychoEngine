namespace PsychoEngine.Input;

public abstract class MouseEventArgs : EventArgs
{
    public Point   Position     { get; }
    public ModKeys ModifierKeys { get; }

    public MouseEventArgs(Point position, ModKeys modifierKeys)
    {
        Position     = position;
        ModifierKeys = modifierKeys;
    }
}

public class MouseButtonEventArgs : MouseEventArgs
{
    public MouseButtons Button { get; }

    public MouseButtonEventArgs(MouseButtons button, Point position, ModKeys modifierKeys)
        : base(position, modifierKeys)
    {
        Button = button;
    }
}

public class MouseMovedEventArgs : MouseEventArgs
{
    public Point PreviousPosition { get; }
    public Point PositionDelta    { get; }

    public MouseMovedEventArgs(Point previousPosition, Point position, Point positionDelta, ModKeys modifierKeys)
        : base(position, modifierKeys)
    {
        PreviousPosition = previousPosition;
        PositionDelta    = positionDelta;
    }
}

public class MouseScrolledEventArgs : MouseEventArgs
{
    public float ScrollValue { get; }
    public float ScrollDelta { get; }

    public MouseScrolledEventArgs(float scrollValue, float scrollDelta, Point position, ModKeys modifierKeys)
        : base(position, modifierKeys)
    {
        ScrollValue = scrollValue;
        ScrollDelta = scrollDelta;
    }
}

public class MouseDraggedEventArgs : MouseEventArgs
{
    public MouseButtons Button            { get; }
    public Point        DragStartPosition { get; }

    public MouseDraggedEventArgs(MouseButtons button, Point dragStartPosition, Point position, ModKeys modifierKeys)
        : base(position, modifierKeys)
    {
        Button            = button;
        DragStartPosition = dragStartPosition;
    }
}

public class MouseMultiClickEventArgs : MouseEventArgs
{
    public MouseButtons Button     { get; }
    public int          ClickCount { get; }

    public MouseMultiClickEventArgs(MouseButtons button, int clickCount, Point position, ModKeys modifierKeys)
        : base(position, modifierKeys)
    {
        Button     = button;
        ClickCount = clickCount;
    }
}