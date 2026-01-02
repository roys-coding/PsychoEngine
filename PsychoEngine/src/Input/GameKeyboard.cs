using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public static class GameKeyboard
{
    // Events.
    public static event EventHandler<KeyboardEventArgs>? OnKeyDown;
    public static event EventHandler<KeyboardEventArgs>? OnKeyPressed;
    public static event EventHandler<KeyboardEventArgs>? OnKeyReleased;

    // Constants.
    public static readonly Keys[] AllKeys;

    // Config.
    private static FocusLostInputBehaviour _focusLostInputBehaviour;

    // Input states.
    private static KeyboardState _currentState;
    private static KeyboardState _previousState;
    private static Keys[]?       _allKeysDown;

    // Input time stamps.
    public static TimeSpan LastInputTime { get; private set; }

    public static Keys[] AllKeysDown
    {
        get
        {
            // Cache current frame's pressed keys.
            _allKeysDown ??= _currentState.GetPressedKeys();

            return _allKeysDown;
        }
    }

    // Key checks.
    public static bool ModShift   => IsKeyDown(Keys.LeftShift)   || IsKeyDown(Keys.RightShift);
    public static bool ModControl => IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl);
    public static bool ModAlt     => IsKeyDown(Keys.LeftAlt)     || IsKeyDown(Keys.RightAlt);
    public static bool ModSuper   => IsKeyDown(Keys.LeftWindows) || IsKeyDown(Keys.RightWindows);

    static GameKeyboard()
    {
        AllKeys = Enum.GetValues<Keys>();

        #region ImGui

        CoreEngine.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;

        
        OnKeyDown += (_, args) => 
                        {
                            if (!_logDownEvent) return;
                            ImGuiLog("OnKeyDown");
                            ImGuiLog($"     -Key: {args.Key}");
                        };
        OnKeyPressed += (_, args) => 
                           {
                               if (!_logPressEvent) return;
                               ImGuiLog("OnKeyPressed");
                               ImGuiLog($"     -Key: {args.Key}");
                           };
        OnKeyReleased += (_, args) => 
                            {
                                if (!_logReleaseEvent) return;
                                ImGuiLog("OnKeyReleased");
                                ImGuiLog($"     -Key: {args.Key}");
                            }; 

        #endregion
    }

    #region ImGui

    private const           int        LogCapacity     = 100;
    private static          bool       _activeKeysOnly = true;
    private static          bool       _logDownEvent;
    private static          bool       _logPressEvent   = true;
    private static          bool       _logReleaseEvent = true;

    private static readonly string[] FocusLostNames = Enum.GetNames<FocusLostInputBehaviour>();

    private static readonly List<string> EventLog = new(LogCapacity);
    private static          bool         _logHeader;

    private static void ImGuiLog(string message)
    {
        if (!_logHeader) return;

        EventLog.Add(message);

        if (EventLog.Count >= LogCapacity)
        {
            EventLog.RemoveAt(0);
        }
    }

    private static void ImGuiOnLayout(object? sender, EventArgs args)
    {
        bool windowOpen = ImGui.Begin($"{Fonts.Lucide.Keyboard} Keyboard ");

        if (!windowOpen)
        {
            ImGui.End();

            return;
        }

        bool configHeader = ImGui.CollapsingHeader("Config");
        
        if (configHeader)
        {
            int  focusLost                                 = (int)_focusLostInputBehaviour;
            bool focusLostChanged                          = ImGui.Combo("FocusLost Behaviour", ref focusLost, FocusLostNames, FocusLostNames.Length);
            if (focusLostChanged) _focusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;
        }
        
        bool timesHeader = ImGui.CollapsingHeader("Time stamps");

        if (timesHeader)
        {
            ImGui.Text($"Last Input: {LastInputTime}");
        }

        bool keysHeader = ImGui.CollapsingHeader("Keys");

        if (keysHeader)
        {
            ImGui.Checkbox("Only active keys", ref _activeKeysOnly);

            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;
            ImGui.BeginTable("Keys", 4, flags);
            ImGui.TableSetupColumn("Key");
            ImGui.TableSetupColumn("State");
            ImGui.TableSetupColumn("Pressed");
            ImGui.TableSetupColumn("Released");
            ImGui.TableHeadersRow();

            foreach (Keys key in AllKeys)
            {
                InputStates state    = GetKey(key);
                bool        pressed  = WasKeyPressed(key);
                bool        released = WasKeyReleased(key);

                if (_activeKeysOnly && state == InputStates.Up && !released) continue;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                if (!IsKeyDown(key)) ImGui.BeginDisabled();

                ImGui.Text(key.ToString());
                ImGui.TableNextColumn();
                ImGui.Text($"{(IsKeyDown(key) ? "Down" : "Up")}");
                
                if (!IsKeyDown(key)) ImGui.EndDisabled();
                
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{key}pressed", ref pressed);
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{key}released", ref released);
            }

            ImGui.EndTable();
        }

        _logHeader = ImGui.CollapsingHeader("Events log");

        if (_logHeader)
        {
            ImGui.Checkbox("Log DownEvent",    ref _logDownEvent);
            ImGui.Checkbox("Log PressEvent",   ref _logPressEvent);
            ImGui.Checkbox("Log ReleaseEvent", ref _logReleaseEvent);

            bool clearLogs = ImGui.Button("Clear");
            if (clearLogs) EventLog.Clear();

            const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar;
            
            ImGui.BeginChild("Event log", ImGuiChildFlags.FrameStyle, windowFlags);

            foreach (string message in EventLog)
            {
                ImGui.Text(message);
            }

            ImGui.SetScrollHereY();

            ImGui.EndChild();
        }

        ImGui.End();
    }

    #endregion

    public static InputStates GetKey(Keys key)
    {
        InputStates inputState = InputStates.None;

        if (IsKeyUp(key)) inputState        |= InputStates.Up;
        if (IsKeyDown(key)) inputState      |= InputStates.Down;
        if (WasKeyPressed(key)) inputState  |= InputStates.Pressed;
        if (WasKeyReleased(key)) inputState |= InputStates.Released;

        return inputState;
    }

    public static bool IsKeyUp(Keys key)
    {
        return _currentState[key] == KeyState.Up;
    }

    public static bool IsKeyDown(Keys key)
    {
        return _currentState[key] == KeyState.Down;
    }

    public static bool WasKeyPressed(Keys key)
    {
        return _previousState[key] == KeyState.Up && _currentState[key] == KeyState.Down;
    }

    public static bool WasKeyReleased(Keys key)
    {
        return _previousState[key] == KeyState.Down && _currentState[key] == KeyState.Up;
    }

    #region Internal methods

    internal static void Update(Game game)
    {
        UpdateKeyboardStates(game);

        // Clear previous frame's cached pressed keys.
        _allKeysDown = null;

        bool receivedAnyInput = false;

        // Handle input handlers.
        foreach (Keys key in AllKeys)
        {
            if (WasKeyPressed(key))
            {
                OnKeyPressed?.Invoke(game, new KeyboardEventArgs(key));
                receivedAnyInput = true;
            }
            
            if (IsKeyDown(key))
            {
                OnKeyDown?.Invoke(game, new KeyboardEventArgs(key));
                receivedAnyInput = true;
            }

            if (WasKeyReleased(key))
            {
                OnKeyReleased?.Invoke(game, new KeyboardEventArgs(key));
                receivedAnyInput = true;
            }
        }
        
        if (receivedAnyInput) LastInputTime = GameTimes.Update.TotalGameTime;
    }

    private static void UpdateKeyboardStates(Game game)
    {
        if (game.IsActive && !ImGui.GetIO().WantCaptureKeyboard)
        {
            // Update input state normally.
            _previousState = _currentState;
            _currentState  = Keyboard.GetState();

            return;
        }

        switch (_focusLostInputBehaviour)
        {
            case FocusLostInputBehaviour.ClearState:
                // Pass an empty state, releasing all keys.
                _previousState = _currentState;
                _currentState  = default(KeyboardState);
                break;

            case FocusLostInputBehaviour.MaintainState:
                // Maintain previous state, not releasing nor pressing any more keys.
                _previousState = _currentState;
                break;

            case FocusLostInputBehaviour.KeepUpdating:
                // Update input state normally.
                _previousState = _currentState;
                _currentState  = Keyboard.GetState();
                break;

            default:
                throw new
                    InvalidOperationException($"FocusLostInputBehaviour '{_focusLostInputBehaviour}' not supported.");
        }
    }

    #endregion
}