namespace PsychoEngine;

public static class PyGameTimes
{
    public static GameTime Update { get; internal set; }
    public static GameTime Draw   { get; internal set; }

    public static int FramesRunning { get; internal set; }
}