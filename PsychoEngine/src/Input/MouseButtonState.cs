using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

internal struct MouseButtonState
{
    // State.
    public InputStates InputState   { get; set; }

    // Dragging.
    public Point DragStartPosition  { get; set; }
    public bool  PreviousIsDragging { get; set; }
    public bool  IsDragging         { get; set; }

    // Multi clicking.
    public TimeSpan LastPressTime     { get; set; }
    public int      ConsecutiveClicks { get; set; }
}