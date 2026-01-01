namespace PsychoEngine.Input;

public class MouseEventArgs : EventArgs
{
    public MouseSnapshot State { get; }

    public MouseEventArgs(MouseSnapshot state)
    {
        State = state;
    }
}