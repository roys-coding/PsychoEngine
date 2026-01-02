namespace PsychoEngine.Input;

internal struct ButtonExtraState
{
    // Dragging.
    public Point DragStartPosition  { get; set; }
    public bool  PreviousIsDragging { get; set; }
    public bool  IsDragging         { get; set; }

    // Multi clicking.
    public TimeSpan LastPressTime     { get; set; }
    public int      ConsecutiveClicks { get; set; }
}