namespace PsychoEngine.Input;

public static partial class PyGamePads
{
    #region Events

    // Connection.
    public static event EventHandler<GamePadEventArgs>? OnPlayerConnected;
    public static event EventHandler<GamePadEventArgs>? OnPlayerDisconnected;

    // Buttons.
    public static event EventHandler<GamePadButtonEventArgs>? OnButtonDown;
    public static event EventHandler<GamePadButtonEventArgs>? OnButtonPressed;
    public static event EventHandler<GamePadButtonEventArgs>? OnButtonReleased;

    // Analog input.
    public static event EventHandler<GamePadTriggerEventArgs>?    OnTriggerMoved;
    public static event EventHandler<GamePadThumbstickEventArgs>? OnThumbstickMoved;

    #endregion

    #region Fields

    // Constants.
    internal static readonly int                 SupportedPlayersCount;
    internal static readonly PlayerIndex[]       PlayersEnum;
    internal static readonly GamePadButton[]     ButtonsEnum;
    internal static readonly GamePadTrigger[]    TriggersEnum;
    internal static readonly GamePadThumbstick[] ThumbsticksEnum;

    // GamePad states.
    private static readonly IDictionary<PlayerIndex, PyGamePad> GamePads;

    // Config.
    internal static FocusLostInputBehaviour FocusLostInputBehaviour = FocusLostInputBehaviour.ClearStates;

    #endregion

    #region Properties

    // Device state.
    public static bool IsAnyConnected { get; private set; }

    // Time stamps. 
    public static TimeSpan    LastInputTime              { get; private set; }
    public static PlayerIndex LastInputResponsiblePlayer { get; private set; }

    #endregion

    static PyGamePads()
    {
        PlayersEnum           = Enum.GetValues<PlayerIndex>();
        ButtonsEnum           = Enum.GetValues<GamePadButton>();
        TriggersEnum          = Enum.GetValues<GamePadTrigger>();
        ThumbsticksEnum       = Enum.GetValues<GamePadThumbstick>();
        SupportedPlayersCount = PlayersEnum.Length;
        GamePads              = new Dictionary<PlayerIndex, PyGamePad>(SupportedPlayersCount);

        // Populate states dictionary.
        foreach (PlayerIndex player in PlayersEnum)
        {
            GamePads.Add(player, new PyGamePad(player));
        }

        InitializeImGui();
    }

    #region Public interface

    public static PyGamePad GetPlayer(PlayerIndex playerIndex)
    {
        PyGamePad? player;
        bool       playerFound = GamePads.TryGetValue(playerIndex, out player);

        if (!playerFound || player == null)
        {
            throw new NotSupportedException($"Player '{playerIndex}' not supported.");
        }

        return player;
    }

    public static bool IsPlayerConnected(PlayerIndex playerIndex)
    {
        return GetPlayer(playerIndex).IsConnected;
    }

    #endregion

    #region Non public methods

    internal static void Update(Game game)
    {
        bool anyGamePadConnected = false;
        
        foreach (PyGamePad gamePad in GamePads.Values)
        {
            if (gamePad.IsConnected)
            {
                anyGamePadConnected = true;
            }
            
            gamePad.Update(game);

            // Handle connection events.
            if (gamePad.WasConnected())
            {
                OnPlayerConnected?.Invoke(null, new GamePadEventArgs(gamePad.PlayerIndex));
            }

            if (gamePad.WasDisconnected())
            {
                OnPlayerDisconnected?.Invoke(null, new GamePadEventArgs(gamePad.PlayerIndex));
            }

            // Handle thumbstick events.
            foreach (GamePadThumbstick thumbstick in ThumbsticksEnum)
            {
                if (gamePad.DidThumbstickMove(thumbstick))
                {
                    OnThumbstickMoved?.Invoke(null,
                                              new GamePadThumbstickEventArgs(thumbstick,
                                                                             gamePad.GetThumbstick(thumbstick),
                                                                             gamePad.GetThumbstickDelta(thumbstick),
                                                                             gamePad.PlayerIndex));
                }
            }

            // Handle trigger events.
            foreach (GamePadTrigger trigger in TriggersEnum)
            {
                if (gamePad.DidTriggerMove(trigger))
                {
                    OnTriggerMoved?.Invoke(null,
                                           new GamePadTriggerEventArgs(trigger,
                                                                       gamePad.GetTrigger(trigger),
                                                                       gamePad.GetTriggerDelta(trigger),
                                                                       gamePad.PlayerIndex));
                }
            }

            // Handle button events.
            foreach (GamePadButton button in ButtonsEnum)
            {
                if (gamePad.WasButtonPressed(button))
                {
                    OnButtonPressed?.Invoke(null, new GamePadButtonEventArgs(button, gamePad.PlayerIndex));
                }

                if (gamePad.IsButtonDown(button))
                {
                    OnButtonDown?.Invoke(null, new GamePadButtonEventArgs(button, gamePad.PlayerIndex));
                }

                if (gamePad.WasButtonReleased(button))
                {
                    OnButtonReleased?.Invoke(null, new GamePadButtonEventArgs(button, gamePad.PlayerIndex));
                }
            }

            if (gamePad.LastInputTime > LastInputTime)
            {
                LastInputTime              = gamePad.LastInputTime;
                LastInputResponsiblePlayer = gamePad.PlayerIndex;
            }
        }
        
        IsAnyConnected = anyGamePadConnected;
    }

    #endregion
}