namespace PsychoEngine.Input;

public class MouseEventArgs : EventArgs
{
    public MouseSnapshot Snapshot { get; }

    public MouseEventArgs(MouseSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}