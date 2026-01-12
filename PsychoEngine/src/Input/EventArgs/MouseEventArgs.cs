namespace PsychoEngine.Input;

public abstract class MouseEventArgs : EventArgs
{
    public Point        Position     { get; }
    public ModifierKeys ModifierKeys { get; }

    public MouseEventArgs(Point position, ModifierKeys modifierKeys)
    {
        Position     = position;
        ModifierKeys = modifierKeys;
    }
}

public class MouseButtonEventArgs : MouseEventArgs
{
    public MouseButton Button { get; }

    public MouseButtonEventArgs(MouseButton button, Point position, ModifierKeys modifierKeys) :
        base(position, modifierKeys)
    {
        Button = button;
    }
}

public class MouseMovedEventArgs : MouseEventArgs
{
    public Point PreviousPosition { get; }
    public Point PositionDelta    { get; }

    public MouseMovedEventArgs(Point previousPosition, Point position, Point positionDelta, ModifierKeys modifierKeys)
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

    public MouseScrolledEventArgs(float scrollValue, float scrollDelta, Point position, ModifierKeys modifierKeys)
        : base(position, modifierKeys)
    {
        ScrollValue = scrollValue;
        ScrollDelta = scrollDelta;
    }
}

public class MouseDraggedEventArgs : MouseEventArgs
{
    public MouseButton Button            { get; }
    public Point       DragStartPosition { get; }

    public MouseDraggedEventArgs(MouseButton button, Point dragStartPosition, Point position, ModifierKeys modifierKeys)
        : base(position, modifierKeys)
    {
        Button            = button;
        DragStartPosition = dragStartPosition;
    }
}

public class MouseMultiClickEventArgs : MouseEventArgs
{
    public MouseButton Button     { get; }
    public int         ClickCount { get; }

    public MouseMultiClickEventArgs(MouseButton button, int clickCount, Point position, ModifierKeys modifierKeys) :
        base(position, modifierKeys)
    {
        Button     = button;
        ClickCount = clickCount;
    }
}