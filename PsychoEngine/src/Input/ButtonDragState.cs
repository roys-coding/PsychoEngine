namespace PsychoEngine.Input;

internal readonly struct ButtonDragState
{
    public Point StartPosition  { get; }
    public bool  PreviousIsDragging { get; }
    public bool  IsDragging         { get; }

    public ButtonDragState(Point startPosition, bool previousIsDragging, bool isDragging)
    {
        StartPosition  = startPosition;
        PreviousIsDragging = previousIsDragging;
        IsDragging         = isDragging;
    }
}