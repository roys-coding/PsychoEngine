namespace PsychoEngine.Input;

public class MouseButtonEventArgs : EventArgs
{
    public MouseButtons  Button   { get; }
    public MouseSnapshot State { get; }

    public MouseButtonEventArgs(MouseButtons button, MouseSnapshot state)
    {
        Button   = button;
        State = state;
    }
}