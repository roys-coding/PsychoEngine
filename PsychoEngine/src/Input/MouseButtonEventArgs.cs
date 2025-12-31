namespace PsychoEngine.Input;

public class MouseButtonEventArgs : EventArgs
{
    public MouseButtons   Button { get; }
    public GameMouseState State  { get; }

    public MouseButtonEventArgs(MouseButtons button, GameMouseState state)
    {
        Button = button;
        State  = state;
    }
}