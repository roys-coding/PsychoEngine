namespace PsychoEngine.Graphics;

public class WindowEventArgs : EventArgs
{
    public int WindowWidth  { get; }
    public int WindowHeight { get; }

    public WindowEventArgs(int windowWidth, int windowHeight)
    {
        WindowWidth  = windowWidth;
        WindowHeight = windowHeight;
    }
}