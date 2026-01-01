namespace PsychoEngine.Input;

public readonly struct ButtonDragState
{
    public Point LastPressPosition { get; }
    public bool  IsDragging        { get; }

    public ButtonDragState(Point lastPressPosition, bool isDragging)
    {
        LastPressPosition = lastPressPosition;
        IsDragging        = isDragging;
    }
}