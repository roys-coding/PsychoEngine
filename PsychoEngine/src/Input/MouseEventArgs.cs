namespace PsychoEngine.Input;

public class MouseEventArgs : EventArgs
{
    public GameMouseState State { get; }

    public MouseEventArgs(GameMouseState state)
    {
        State = state;
    }
}