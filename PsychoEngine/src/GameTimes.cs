namespace PsychoEngine;

public static class GameTimes
{
    public static GameTime Update { get; internal set; }
    public static GameTime Draw   { get; internal set; }

    public static int FramesRunning { get; internal set; }
}