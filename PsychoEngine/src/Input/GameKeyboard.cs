using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace PsychoEngine.Input;

public static class GameKeyboard
{
    // Events.
    public delegate void KeyboardEventHandler(object? sender, KeyboardEventArgs args);

    public static event KeyboardEventHandler? OnKeyDown;
    public static event KeyboardEventHandler? OnKeyPressed;
    public static event KeyboardEventHandler? OnKeyReleased;

    // Constants.
    public static readonly Keys[] AllKeys;

    // Config.
    private static FocusLostInputBehaviour _focusLostInputBehaviour;

    // Input states.
    private static KeyboardState _currentState;
    private static KeyboardState _previousState;
    private static Keys[]?       _allKeysDown;

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

        CoreEngine.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;
        
        OnKeyDown += (sender, args) =>
                     {
                         if (_ignoreDownEvent) return;
                         ImGuiLog($"Down {args.Key}");
                     };
        OnKeyPressed += (sender, args) => ImGuiLog($"Pressed {args.Key}");
        OnKeyReleased += (sender, args) => ImGuiLog($"Released {args.Key}");
    }

    private const  int          LogCapacity = 1000;
    private static bool         _activeOnly = true;
    private static bool         _ignoreDownEvent = false;
    private static List<string> _eventLog   = new(LogCapacity);

    private static void ImGuiLog(string message)
    {
        _eventLog.Add(message);

        if (_eventLog.Count >= LogCapacity)
        {
            _eventLog.RemoveAt(0);
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

        int  focusLost                                 = (int)_focusLostInputBehaviour;
        bool focusLostChanged                          = ImGui.DragInt("FocusLost", ref focusLost, 0, 2);
        if (focusLostChanged) _focusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;

        bool keysHeader = ImGui.CollapsingHeader("Keys");

        if (keysHeader)
        {
            ImGui.Checkbox("Active keys only", ref _activeOnly);
            
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;
            ImGui.BeginTable("Keys", 4, flags);
            ImGui.TableSetupColumn("Key");
            ImGui.TableSetupColumn("State");
            ImGui.TableSetupColumn("Pressed");
            ImGui.TableSetupColumn("Released");
            ImGui.TableHeadersRow();
            
            foreach (Keys button in AllKeys)
            {
                KeyState state    = GetKey(button);
                bool     pressed  = WasKeyPressed(button);
                bool     released = WasKeyReleased(button);
            
                if (_activeOnly && (state != KeyState.Down && !released)) continue;
                
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                if (state == KeyState.Down)
                {
                    ImGui.Text(button.ToString());
                }
                else
                {
                    ImGui.TextDisabled(button.ToString());
                }
                
                ImGui.TableNextColumn();
                
                if (state == KeyState.Down)
                {
                    ImGui.Text(state.ToString());
                }
                else
                {
                    ImGui.TextDisabled(state.ToString());
                }
                
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}pressed", ref pressed);
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}released", ref released);
            }
            ImGui.EndTable();
        }

        bool eventLogHeader = ImGui.CollapsingHeader("Events log");

        if (eventLogHeader)
        {
            ImGui.Checkbox("Ignore down event", ref _ignoreDownEvent);
            
            bool clearPressed = ImGui.Button("Clear");
            if (clearPressed) _eventLog.Clear();
            
            ImGui.BeginChild("Event log", ImGuiChildFlags.FrameStyle);
            
            foreach (string message in _eventLog)
            {
                ImGui.Text(message);
            }
            ImGui.SetScrollHereY();
            
            ImGui.EndChild();
        }

        ImGui.End();
    }

    public static void Update(Game game, GameTime gameTime)
    {
        if (game.IsActive && !ImGui.GetIO().WantCaptureKeyboard)
        {
            // Update input state normally.
            _previousState = _currentState;
            _currentState  = Keyboard.GetState();
        }
        else
        {
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

        /* TODO: Snapshots */

        if (_previousState != _currentState)
        {
            LastInputTime = gameTime.TotalGameTime;
        }

        // Clear previous frame's cached pressed keys.
        _allKeysDown = null;

        // Handle input handlers.
        foreach (Keys key in AllKeys)
        {
            if (IsKeyDown(key)) OnKeyDown?.Invoke(game, new KeyboardEventArgs(key));
            if (WasKeyPressed(key)) OnKeyPressed?.Invoke(game, new KeyboardEventArgs(key));
            if (WasKeyReleased(key)) OnKeyReleased?.Invoke(game, new KeyboardEventArgs(key));
        }
    }

    public static KeyState GetKey(Keys key)
    {
        return _currentState[key];
    }

    public static bool CheckKey(Keys key, KeyState inputState)
    {
        return _currentState[key] == inputState;
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
}