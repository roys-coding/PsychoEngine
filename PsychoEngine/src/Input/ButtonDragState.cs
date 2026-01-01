namespace PsychoEngine.Input;

public readonly struct ButtonDragState
{
    public Point LastPressPosition { get; }
    public bool  Dragging          { get; }

    public ButtonDragState(Point lastPressPosition, bool dragging)
    {
        LastPressPosition = lastPressPosition;
        Dragging          = dragging;
    }
}