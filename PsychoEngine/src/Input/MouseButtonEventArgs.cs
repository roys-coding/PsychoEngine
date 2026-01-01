namespace PsychoEngine.Input;

public class MouseButtonEventArgs : EventArgs
{
    public MouseButtons  Button   { get; }
    public MouseSnapshot Snapshot { get; }

    public MouseButtonEventArgs(MouseButtons button, MouseSnapshot snapshot)
    {
        Button   = button;
        Snapshot = snapshot;
    }
}