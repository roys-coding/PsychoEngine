using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using PsychoEngine.Utilities;

namespace PsychoEngine.Input;

public static class GameMouse
{
    // TODO: Implement FNA Click ext.

    // Constants.
    private const float WheelDeltaUnit = 120f;

    // Events.
    public static event EventHandler<MouseMovedEventArgs>?    OnMoved;
    public static event EventHandler<MouseScrolledEventArgs>? OnScrolled;

    public static event EventHandler<MouseButtonEventArgs>? OnButtonDown;
    public static event EventHandler<MouseButtonEventArgs>? OnButtonPressed;
    public static event EventHandler<MouseButtonEventArgs>? OnButtonReleased;

    public static event EventHandler<MouseDraggedEventArgs>? OnDragStarted;
    public static event EventHandler<MouseDraggedEventArgs>? OnDragging;
    public static event EventHandler<MouseDraggedEventArgs>? OnDragReleased;

    public static event EventHandler<MouseMultiClickEventArgs>? OnMultiClick;

    // Constants.
    private static readonly MouseButtons[] AllButtons;

    // TODO: Make input configurable.

    // Config.
    private const  int                     DragThreshold                    = 5;
    private const  double                  ConsecutiveClickThresholdSeconds = 0.5;
    private static FocusLostInputBehaviour _focusLostInputBehaviour         = FocusLostInputBehaviour.ClearState;

    // Mouse states.
    private static MouseState _previousState;
    private static MouseState _currentState;

    // Button states.
    private static MouseButtonState _leftButton;
    private static MouseButtonState _middleButton;
    private static MouseButtonState _rightButton;
    private static MouseButtonState _x1Button;
    private static MouseButtonState _x2Button;

    // Input time stamps.
    public static TimeSpan LastMoveTime  { get; private set; }
    public static TimeSpan LastInputTime { get; private set; }

    // Position.
    public static Point PreviousPosition { get; private set; }
    public static Point Position         { get; private set; }
    public static Point PositionDelta    { get; private set; }
    public static bool  Moved            { get; private set; }

    // Scroll wheel.
    public static float PreviousScrollValue { get; private set; }
    public static float ScrollValue         { get; private set; }
    public static float ScrollDelta         { get; private set; }
    public static bool  Scrolled            { get; private set; }

    static GameMouse()
    {
        AllButtons = Enum.GetValues<MouseButtons>();

        #region ImGui

        CoreEngine.Instance.ImGuiManager.OnLayout += ImGuiOnLayout;

        OnMoved += (_, args) =>
                   {
                       if (!_logMovedEvent) return;
                       ImGuiLog("OnMoved");
                       ImGuiLog($"     -PrevPos: {args.PreviousPosition}");
                       ImGuiLog($"     -Position: {args.Position}");
                       ImGuiLog($"     -PosDelta: {args.PositionDelta}");
                   };

        OnScrolled += (_, args) =>
                      {
                          if (!_logScrollEvent) return;
                          ImGuiLog("OnScrolled");
                          ImGuiLog($"     -ScrollValue: {args.ScrollValue}");
                          ImGuiLog($"     -ScrollDelta: {args.ScrollDelta}");
                          ImGuiLog($"     -Position: {args.Position}");
                      };

        OnButtonDown += (_, args) =>
                        {
                            if (!_logDownEvent) return;
                            ImGuiLog("OnButtonDown");
                            ImGuiLog($"     -Button: {args.Button}");
                            ImGuiLog($"     -Position: {args.Position}");
                        };

        OnButtonPressed += (_, args) =>
                           {
                               if (!_logPressEvent) return;
                               ImGuiLog("OnButtonPressed");
                               ImGuiLog($"     -Button: {args.Button}");
                               ImGuiLog($"     -Position: {args.Position}");
                           };

        OnButtonReleased += (_, args) =>
                            {
                                if (!_logReleaseEvent) return;
                                ImGuiLog("OnButtonReleased");
                                ImGuiLog($"     -Button: {args.Button}");
                                ImGuiLog($"     -Position: {args.Position}");
                            };

        OnDragStarted += (_, args) =>
                         {
                             if (!_logDragStartEvent) return;
                             ImGuiLog("OnDragStarted");
                             ImGuiLog($"     -Button: {args.Button}");
                             ImGuiLog($"     -StartPos: {args.DragStartPosition}");
                             ImGuiLog($"     -Position: {args.Position}");
                         };

        OnDragging += (_, args) =>
                      {
                          if (!_logDragEvent) return;
                          ImGuiLog("OnDragging");
                          ImGuiLog($"     -Button: {args.Button}");
                          ImGuiLog($"     -StartPos: {args.DragStartPosition}");
                          ImGuiLog($"     -Position: {args.Position}");
                      };

        OnDragReleased += (_, args) =>
                          {
                              if (!_logDragEndEvent) return;
                              ImGuiLog("OnDragReleased");
                              ImGuiLog($"     -Button: {args.Button}");
                              ImGuiLog($"     -StartPos: {args.DragStartPosition}");
                              ImGuiLog($"     -Position: {args.Position}");
                          };

        OnMultiClick += (_, args) =>
                        {
                            if (!_logMulticlickEvent) return;
                            ImGuiLog("OnMultiClick");
                            ImGuiLog($"     -Button: {args.Button}");
                            ImGuiLog($"     -Clicks: {args.ClickCount}");
                            ImGuiLog($"     -Position: {args.Position}");
                        };

        #endregion
    }

    #region ImGui

    private const  int  LogCapacity = 100;
    private static bool _logMovedEvent;
    private static bool _logScrollEvent = true;
    private static bool _logDownEvent;
    private static bool _logPressEvent     = true;
    private static bool _logReleaseEvent   = true;
    private static bool _logDragStartEvent = true;
    private static bool _logDragEvent;
    private static bool _logDragEndEvent    = true;
    private static bool _logMulticlickEvent = true;

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
        bool windowOpen = ImGui.Begin($"{Fonts.Lucide.Mouse} Mouse");

        if (!windowOpen)
        {
            ImGui.End();

            return;
        }

        if (ImGui.CollapsingHeader("Config"))
        {
            int focusLost = (int)_focusLostInputBehaviour;

            bool focusLostChanged =
                ImGui.Combo("FocusLost Behaviour", ref focusLost, FocusLostNames, FocusLostNames.Length);

            if (focusLostChanged) _focusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;
        }

        if (ImGui.CollapsingHeader("Time stamps"))
        {
            ImGui.Text($"Last Movement: {LastMoveTime}");
            ImGui.Text($"Last Input: {LastInputTime}");
        }

        if (ImGui.CollapsingHeader("Movement"))
        {
            bool moved = Moved;

            ImGui.Text($"Position: {Position}");
            ImGui.Text($"Previous Position: {PreviousPosition}");
            ImGui.Text($"Position Delta: {PositionDelta}");
            ImGui.Checkbox("Moved", ref moved);
        }

        if (ImGui.CollapsingHeader("Scroll"))
        {
            bool scrolled = Scrolled;

            ImGui.Text($"Scroll: {ScrollValue}");
            ImGui.Text($"Previous Scroll: {PreviousScrollValue}");
            ImGui.Text($"Scroll Delta: {ScrollDelta}");
            ImGui.Checkbox("Scrolled", ref scrolled);
        }

        if (ImGui.CollapsingHeader("Buttons"))
        {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;
            ImGui.BeginTable("Buttons", 4, flags);
            ImGui.TableSetupColumn("Button");
            ImGui.TableSetupColumn("State");
            ImGui.TableSetupColumn("Pressed");
            ImGui.TableSetupColumn("Released");
            ImGui.TableHeadersRow();

            foreach (MouseButtons button in AllButtons)
            {
                bool pressed  = WasButtonPressed(button);
                bool released = WasButtonReleased(button);

                if (!IsButtonDown(button)) ImGui.BeginDisabled();

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(button.ToString());
                ImGui.TableNextColumn();
                ImGui.Text($"{(IsButtonDown(button) ? "Down" : "Up")}");

                if (!IsButtonDown(button)) ImGui.EndDisabled();

                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}pressed", ref pressed);
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}released", ref released);
            }

            ImGui.EndTable();
        }

        if (ImGui.CollapsingHeader("Dragging"))
        {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;
            ImGui.BeginTable("Dragging", 5, flags);
            ImGui.TableSetupColumn("Button");
            ImGui.TableSetupColumn("Start Pos");
            ImGui.TableSetupColumn("Dragging");
            ImGui.TableSetupColumn("Started");
            ImGui.TableSetupColumn("Released");
            ImGui.TableHeadersRow();

            foreach (MouseButtons button in AllButtons)
            {
                Point startPos    = GetDragStartPosition(button);
                bool  dragging    = IsDragging(button);
                bool  startedDrag = WasDragStarted(button);
                bool  stoppedDrag = WasDragReleased(button);

                if (!dragging) ImGui.BeginDisabled();

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(button.ToString());
                ImGui.TableNextColumn();
                ImGui.Text(startPos.ToString());

                if (!dragging) ImGui.EndDisabled();

                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}dragging", ref dragging);
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}starteddrag", ref startedDrag);
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}releaseddrag", ref stoppedDrag);
            }

            ImGui.EndTable();
        }

        if (ImGui.CollapsingHeader("Multi clicking"))
        {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;
            ImGui.BeginTable("Multi clicking", 4, flags);
            ImGui.TableSetupColumn("Button");
            ImGui.TableSetupColumn("Last Press");
            ImGui.TableSetupColumn("Clicks");
            ImGui.TableSetupColumn("Multi clicked");
            ImGui.TableHeadersRow();

            foreach (MouseButtons button in AllButtons)
            {
                MouseButtonState state           = GetButtonState(button);
                int              clicks          = GetConsecutiveClicks(button);
                TimeSpan         lastReleaseTime = state.LastPressTime;
                bool             multiClicked    = WasButtonMultiClicked(button);

                if (!multiClicked) ImGui.BeginDisabled();

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(button.ToString());
                ImGui.TableNextColumn();
                ImGui.Text(lastReleaseTime.ToString());
                ImGui.TableNextColumn();
                ImGui.Text(clicks.ToString());

                if (!multiClicked) ImGui.EndDisabled();

                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}multi click", ref multiClicked);
            }

            ImGui.EndTable();
        }

        _logHeader = ImGui.CollapsingHeader("Events log");

        if (_logHeader)
        {
            ImGui.Checkbox("Log MovedEvent",      ref _logMovedEvent);
            ImGui.Checkbox("Log ScrollEvent",     ref _logScrollEvent);
            ImGui.Checkbox("Log DownEvent",       ref _logDownEvent);
            ImGui.Checkbox("Log PressEvent",      ref _logPressEvent);
            ImGui.Checkbox("Log ReleaseEvent",    ref _logReleaseEvent);
            ImGui.Checkbox("Log DragStartEvent",  ref _logDragStartEvent);
            ImGui.Checkbox("Log DragEvent",       ref _logDragEvent);
            ImGui.Checkbox("Log DragEndEvent",    ref _logDragEndEvent);
            ImGui.Checkbox("Log MultiClickEvent", ref _logMulticlickEvent);

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

    public static InputStates GetButton(MouseButtons button)
    {
        return GetButtonState(button).InputState;
    }

    public static bool IsButtonUp(MouseButtons button)
    {
        return GetButton(button).HasFlag(InputStates.Up);
    }

    public static bool IsButtonDown(MouseButtons button)
    {
        return GetButton(button).HasFlag(InputStates.Down);
    }

    public static bool WasButtonPressed(MouseButtons button)
    {
        return GetButton(button).HasFlag(InputStates.Pressed);
    }

    public static bool WasButtonReleased(MouseButtons button)
    {
        return GetButton(button).HasFlag(InputStates.Released);
    }

    public static bool WasDragStarted(MouseButtons button)
    {
        MouseButtonState state = GetButtonState(button);

        return state is
               {
                   IsDragging        : true,
                   PreviousIsDragging: false,
               };
    }

    public static bool IsDragging(MouseButtons button)
    {
        return GetButtonState(button).IsDragging;
    }

    public static bool WasDragReleased(MouseButtons button)
    {
        MouseButtonState state = GetButtonState(button);

        return state is
               {
                   IsDragging        : false,
                   PreviousIsDragging: true,
               };
    }

    public static Point GetDragStartPosition(MouseButtons button)
    {
        return GetButtonState(button).DragStartPosition;
    }

    public static bool WasButtonMultiClicked(MouseButtons button)
    {
        return WasButtonPressed(button) && GetButtonState(button).ConsecutiveClicks > 0;
    }

    public static bool WasButtonMultiClicked(MouseButtons button, int multiClicks)
    {
        MouseButtonState buttonState = GetButtonState(button);

        return WasButtonPressed(button)                        &&
               buttonState.ConsecutiveClicks               > 0 &&
               buttonState.ConsecutiveClicks % multiClicks == 0;
    }

    public static int GetConsecutiveClicks(MouseButtons button)
    {
        return GetButtonState(button).ConsecutiveClicks;
    }

    #region Internal methods

    internal static void Update(Game game)
    {
        UpdateMouseStates(game);

        // Position.
        PreviousPosition = new Point(_previousState.X, _previousState.Y);
        Position         = new Point(_currentState.X,  _currentState.Y);
        PositionDelta    = Position - PreviousPosition;
        Moved            = PositionDelta != Point.Zero;

        if (Moved)
        {
            OnMoved?.Invoke(null,
                            new MouseMovedEventArgs(PreviousPosition,
                                                    Position,
                                                    PositionDelta,
                                                    GameKeyboard.ModifierKeys));

            LastMoveTime = GameTimes.Update.TotalGameTime;
        }

        // Scroll.
        PreviousScrollValue = _previousState.ScrollWheelValue / WheelDeltaUnit;
        ScrollValue         = _currentState.ScrollWheelValue  / WheelDeltaUnit;
        ScrollDelta         = ScrollValue - PreviousScrollValue;
        Scrolled            = ScrollDelta != 0f;

        if (Scrolled)
        {
            OnScrolled?.Invoke(null,
                               new MouseScrolledEventArgs(ScrollValue,
                                                          ScrollDelta,
                                                          Position,
                                                          GameKeyboard.ModifierKeys));

            LastInputTime = GameTimes.Update.TotalGameTime;
        }

        // Input state and dragging.
        foreach (MouseButtons button in AllButtons)
        {
            MouseButtonState state = GetButtonState(button);

            UpdateButtonInputState(button, ref state);
            UpdateButtonDragging(button, ref state);
            UpdateButtonConsecutiveClicking(button, ref state);

            SetButtonState(button, state);
        }
    }

    private static void UpdateMouseStates(Game game)
    {
        if (game.IsActive && !ImGui.GetIO().WantCaptureMouse)
        {
            // Update input state normally.
            _previousState = _currentState;
            _currentState  = Mouse.GetState();

            return;
        }

        switch (_focusLostInputBehaviour)
        {
            case FocusLostInputBehaviour.ClearState:
                // Pass an empty state, releasing all buttons.
                // We only retain the last position and scroll value.
                _previousState = _currentState;

                _currentState = new MouseState(_previousState.X,
                                               _previousState.Y,
                                               _previousState.ScrollWheelValue,
                                               ButtonState.Released,
                                               ButtonState.Released,
                                               ButtonState.Released,
                                               ButtonState.Released,
                                               ButtonState.Released);

                break;

            case FocusLostInputBehaviour.MaintainState:
                // Maintain previous state, not releasing nor pressing any more buttons.
                _previousState = _currentState;
                break;

            case FocusLostInputBehaviour.KeepUpdating:
                // Update input state normally.
                _previousState = _currentState;
                _currentState  = Mouse.GetState();
                break;

            default:
                throw new
                    InvalidOperationException($"FocusLostInputBehaviour '{_focusLostInputBehaviour}' not supported.");
        }
    }

    private static void UpdateButtonInputState(MouseButtons button, ref MouseButtonState state)
    {
        ButtonState previousState = _previousState.GetButton(button);
        ButtonState currentState  = _currentState.GetButton(button);

        InputStates inputState       = InputStates.None;
        bool        receivedAnyInput = false;

        switch (currentState)
        {
            case ButtonState.Pressed:
            {
                if (previousState == ButtonState.Released)
                {
                    inputState |= InputStates.Pressed;

                    OnButtonPressed?.Invoke(null,
                                            new MouseButtonEventArgs(button, Position, GameKeyboard.ModifierKeys));
                }

                inputState |= InputStates.Down;
                OnButtonDown?.Invoke(null, new MouseButtonEventArgs(button, Position, GameKeyboard.ModifierKeys));

                receivedAnyInput = true;
                break;
            }

            case ButtonState.Released:
            {
                inputState |= InputStates.Up;

                if (previousState == ButtonState.Pressed)
                {
                    inputState |= InputStates.Released;

                    OnButtonReleased?.Invoke(null,
                                             new MouseButtonEventArgs(button, Position, GameKeyboard.ModifierKeys));

                    receivedAnyInput = true;
                }

                break;
            }

            default: throw new InvalidOperationException($"Mouse state '{currentState}' not supported.");
        }

        state.InputState = inputState;

        if (receivedAnyInput) LastInputTime = GameTimes.Update.TotalGameTime;
    }

    private static void UpdateButtonDragging(MouseButtons button, ref MouseButtonState state)
    {
        state.PreviousIsDragging = state.IsDragging;

        if (WasButtonPressed(button))
        {
            state.DragStartPosition = Position;
        }

        if (WasButtonReleased(button))
        {
            if (state.IsDragging)
            {
                // Button stopped dragging.
                state.IsDragging = false;

                OnDragReleased?.Invoke(null,
                                       new MouseDraggedEventArgs(button,
                                                                 state.DragStartPosition,
                                                                 Position,
                                                                 GameKeyboard.ModifierKeys));
            }

            state.DragStartPosition = Point.Zero;
        }

        if (IsButtonDown(button) && !state.IsDragging)
        {
            float distance = state.DragStartPosition.Distance(Position);

            if (distance > DragThreshold)
            {
                // Button began dragging.
                state.IsDragging = true;

                OnDragStarted?.Invoke(null,
                                      new MouseDraggedEventArgs(button,
                                                                state.DragStartPosition,
                                                                Position,
                                                                GameKeyboard.ModifierKeys));
            }
        }

        if (state.IsDragging)
        {
            OnDragging?.Invoke(null,
                               new MouseDraggedEventArgs(button,
                                                         state.DragStartPosition,
                                                         Position,
                                                         GameKeyboard.ModifierKeys));
        }
    }

    private static void UpdateButtonConsecutiveClicking(MouseButtons button, ref MouseButtonState state)
    {
        if (WasButtonPressed(button))
        {
            TimeSpan timeSinceLastClick = GameTimes.Update.TotalGameTime - state.LastPressTime;

            if (timeSinceLastClick.TotalSeconds <= ConsecutiveClickThresholdSeconds)
            {
                state.ConsecutiveClicks++;

                OnMultiClick?.Invoke(null,
                                     new MouseMultiClickEventArgs(button,
                                                                  state.ConsecutiveClicks,
                                                                  Position,
                                                                  GameKeyboard.ModifierKeys));
            }
            else
            {
                state.ConsecutiveClicks = 0;
            }

            state.LastPressTime = GameTimes.Update.TotalGameTime;
        }
    }

    private static MouseButtonState GetButtonState(MouseButtons button)
    {
        return button switch
               {
                   MouseButtons.None   => default(MouseButtonState),
                   MouseButtons.Left   => _leftButton,
                   MouseButtons.Middle => _middleButton,
                   MouseButtons.Right  => _rightButton,
                   MouseButtons.X1     => _x1Button,
                   MouseButtons.X2     => _x2Button,
                   _                   => throw new InvalidOperationException($"MouseButton '{button}' not supported."),
               };
    }

    private static void SetButtonState(MouseButtons button, MouseButtonState state)
    {
        switch (button)
        {
            case MouseButtons.None: break;

            case MouseButtons.Left:
                _leftButton = state;
                break;

            case MouseButtons.Middle:
                _middleButton = state;
                break;

            case MouseButtons.Right:
                _rightButton = state;
                break;

            case MouseButtons.X1:
                _x1Button = state;
                break;

            case MouseButtons.X2:
                _x2Button = state;
                break;

            default: throw new InvalidOperationException($"MouseButton '{button}' not supported.");
        }
    }

    #endregion
}