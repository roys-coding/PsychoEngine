namespace PsychoEngine.Input;

public class GamePadEventArgs : EventArgs
{
    public PlayerIndex PlayerIndex { get; }

    public GamePadEventArgs(PlayerIndex playerIndex)
    {
        PlayerIndex = playerIndex;
    }
}

public class GamePadButtonEventArgs : GamePadEventArgs
{
    public GamePadButton Button { get; }

    public GamePadButtonEventArgs(GamePadButton button, PlayerIndex playerIndex)
        : base(playerIndex)
    {
        Button = button;
    }
}

public class GamePadTriggerEventArgs : GamePadEventArgs
{
    public GamePadTrigger Trigger      { get; }
    public float          TriggerValue { get; }
    public float          TriggerDelta { get; }

    public GamePadTriggerEventArgs(
        GamePadTrigger trigger,
        float          triggerValue,
        float          triggerDelta,
        PlayerIndex    playerIndex
    )
        : base(playerIndex)
    {
        Trigger      = trigger;
        TriggerValue = triggerValue;
        TriggerDelta = triggerDelta;
    }
}

public class GamePadThumbstickEventArgs : GamePadEventArgs
{
    public GamePadThumbstick Thumbstick      { get; }
    public Vector2           ThumbstickValue { get; }
    public Vector2           ThumbstickDelta { get; }

    public GamePadThumbstickEventArgs(
        GamePadThumbstick thumbstick,
        Vector2           thumbstickValue,
        Vector2           thumbstickDelta,
        PlayerIndex       playerIndex
    )
        : base(playerIndex)
    {
        Thumbstick      = thumbstick;
        ThumbstickValue = thumbstickValue;
        ThumbstickDelta = thumbstickDelta;
    }
}