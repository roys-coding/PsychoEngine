using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using PsychoEngine.Utils;

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

        OnButtonPressed  += (_, args) => ImGuiLog($"{_frame}: Pressed {args.Button}");
        OnButtonReleased += (_, args) => ImGuiLog($"{_frame}: Released {args.Button}");
        OnScrolled       += (_, args) => ImGuiLog($"{_frame}: Scrolled {args.ScrollDelta}");
        OnDragStarted    += (_, args) => ImGuiLog($"{_frame}: Drag started {args.Button}, {args.DragStartPosition}");
        OnDragReleased   += (_, args) => ImGuiLog($"{_frame}: Drag released {args.Button}, {args.DragStartPosition}");
        
        OnMultiClick     += (_, args) =>
                            {
                                if (_ignoreMulticlickEvent) return;
                                ImGuiLog($"{_frame}: Multi Click {args.Button}x{args.ConsecutiveClicks}");
                            };

        OnMoved += (_, args) =>
                   {
                       if (_ignoreMovedEvent) return;
                       ImGuiLog($"{_frame}: Moved {args.MouseState.Position} Delta {args.MouseState.PositionDelta}");
                   };

        OnDragging += (_, args) =>
                      {
                          if (_ignoreDraggingEvent) return;
                          ImGuiLog($"{_frame}: Dragging {args.Button}, {args.DragStartPosition}");
                      };

        OnButtonDown += (_, args) =>
                        {
                            if (_ignoreDownEvent) return;
                            ImGuiLog($"{_frame}: Down {args.Button}");
                        };

        #endregion
    }

    #region ImGui

    private const           int          LogCapacity = 1000;
    private static          int          _frame;
    private static          bool         _ignoreDownEvent;
    private static          bool         _ignoreMovedEvent;
    private static          bool         _ignoreDraggingEvent;
    private static          bool         _ignoreMulticlickEvent;
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
        _frame++;
        bool windowOpen = ImGui.Begin($"{Fonts.Lucide.Mouse} Mouse");

        if (!windowOpen)
        {
            ImGui.End();

            return;
        }

        int  focusLost                                 = (int)_focusLostInputBehaviour;
        bool focusLostChanged                          = ImGui.DragInt("FocusLost", ref focusLost, 0, 2);
        if (focusLostChanged) _focusLostInputBehaviour = (FocusLostInputBehaviour)focusLost;

        bool movementHeader = ImGui.CollapsingHeader("Movement");

        if (movementHeader)
        {
            bool hasMoved = Moved;

            ImGui.Text($"Position: {Position}");
            ImGui.Text($"PrevPos: {PreviousPosition}");
            ImGui.Text($"PosDelta: {PositionDelta}");
            ImGui.Checkbox("HasMoved", ref hasMoved);
        }

        bool scrollHeader = ImGui.CollapsingHeader("Scroll");

        if (scrollHeader)
        {
            bool hasScrolled = Scrolled;

            ImGui.Text($"Scroll: {ScrollValue}");
            ImGui.Text($"PrevScroll: {PreviousScrollValue}");
            ImGui.Text($"ScrollDelta: {ScrollDelta}");
            ImGui.Checkbox("HasScrolled", ref hasScrolled);
        }

        bool buttonsHeader = ImGui.CollapsingHeader("Buttons");

        if (buttonsHeader)
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

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                if (IsButtonDown(button))
                {
                    ImGui.Text(button.ToString());
                }
                else
                {
                    ImGui.TextDisabled(button.ToString());
                }

                ImGui.TableNextColumn();

                if (IsButtonDown(button))
                {
                    ImGui.Text("Down");
                }
                else
                {
                    ImGui.TextDisabled("Up");
                }

                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}pressed", ref pressed);
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}released", ref released);
            }

            ImGui.EndTable();
        }

        bool dragHeader = ImGui.CollapsingHeader("Drag");

        if (dragHeader)
        {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;
            ImGui.BeginTable("Drag", 5, flags);
            ImGui.TableSetupColumn("Button");
            ImGui.TableSetupColumn("LastPress");
            ImGui.TableSetupColumn("Dragging");
            ImGui.TableSetupColumn("Started");
            ImGui.TableSetupColumn("Stopped");
            ImGui.TableHeadersRow();

            foreach (MouseButtons button in AllButtons)
            {
                Point lastPress   = GetDragStartPosition(button);
                bool  dragging    = IsDragging(button);
                bool  startedDrag = WasDragStarted(button);
                bool  stoppedDrag = WasDragReleased(button);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                if (dragging)
                {
                    ImGui.Text(button.ToString());
                }
                else
                {
                    ImGui.TextDisabled(button.ToString());
                }

                ImGui.TableNextColumn();

                if (dragging)
                {
                    ImGui.Text(lastPress.ToString());
                }
                else
                {
                    ImGui.TextDisabled(lastPress.ToString());
                }

                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}dragging", ref dragging);
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}starteddrag", ref startedDrag);
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}stoppeddrag", ref stoppedDrag);
            }

            ImGui.EndTable();
        }

        bool clicksHeader = ImGui.CollapsingHeader("Multi Clicks");

        if (clicksHeader)
        {
            const ImGuiTableFlags flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV;
            ImGui.BeginTable("Drag", 4, flags);
            ImGui.TableSetupColumn("Button");
            ImGui.TableSetupColumn("LastPress");
            ImGui.TableSetupColumn("Clicks");
            ImGui.TableSetupColumn("Multiclicked");
            ImGui.TableHeadersRow();

            foreach (MouseButtons button in AllButtons)
            {
                MouseButtonState state           = GetButtonState(button);
                int              clicks          = GetConsecutiveClicks(button);
                TimeSpan         lastReleaseTime = state.LastPressTime;
                bool             multiClicked    = WasButtonMultiClicked(button);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(button.ToString());
                ImGui.TableNextColumn();
                ImGui.Text(lastReleaseTime.ToString());
                ImGui.TableNextColumn();
                ImGui.Text(clicks.ToString());
                ImGui.TableNextColumn();
                ImGui.Checkbox($"##{button}multiclick", ref multiClicked);
            }

            ImGui.EndTable();
        }

        _logHeader = ImGui.CollapsingHeader("Events log");

        if (_logHeader)
        {
            ImGui.Checkbox("Ignore down event",     ref _ignoreDownEvent);
            ImGui.Checkbox("Ignore move event",     ref _ignoreMovedEvent);
            ImGui.Checkbox("Ignore dragging event", ref _ignoreDraggingEvent);
            ImGui.Checkbox("Ignore multiclick event", ref _ignoreMulticlickEvent);

            bool clearPressed = ImGui.Button("Clear");
            if (clearPressed) EventLog.Clear();

            ImGui.BeginChild("Event log", ImGuiChildFlags.FrameStyle);

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

    public static bool IsDragging(MouseButtons button)
    {
        return GetButtonState(button).IsDragging;
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

    public static int GetConsecutiveClicks(MouseButtons button)
    {
        return GetButtonState(button).ConsecutiveClicks;
    }

    #region Internal methods

    internal static void Update(Game game, GameTime gameTime)
    {
        UpdateMouseStates(game);

        // TODO: Last input time detection
        // TODO: Multi click detection.

        // Position.
        PreviousPosition = new Point(_previousState.X, _previousState.Y);
        Position         = new Point(_currentState.X,  _currentState.Y);
        PositionDelta    = Position - PreviousPosition;
        Moved            = PositionDelta != Point.Zero;

        if (Moved)
        {
            OnMoved?.Invoke(null,
                            new MouseMovedEventArgs(PreviousPosition, Position, PositionDelta, GetSnapshot()));
        }

        // Scroll.
        PreviousScrollValue = _previousState.ScrollWheelValue / WheelDeltaUnit;
        ScrollValue         = _currentState.ScrollWheelValue  / WheelDeltaUnit;
        ScrollDelta         = ScrollValue - PreviousScrollValue;
        Scrolled            = ScrollDelta != 0f;

        if (Scrolled)
        {
            OnScrolled?.Invoke(null, new MouseScrolledEventArgs(ScrollValue, ScrollDelta, GetSnapshot()));
        }

        // Input state and dragging.
        foreach (MouseButtons button in AllButtons)
        {
            UpdateButtonInputState(button);
            UpdateButtonDragging(button);
            UpdateButtonConsecutiveClicking(button, gameTime);
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

    private static void UpdateButtonInputState(MouseButtons button)
    {
        MouseButtonState state         = GetButtonState(button);
        ButtonState      previousState = _previousState.GetButton(button);
        ButtonState      currentState  = _currentState.GetButton(button);

        InputStates inputState = InputStates.None;

        if (currentState == ButtonState.Pressed)
        {
            inputState         |= InputStates.Down;
            OnButtonDown?.Invoke(null, new MouseButtonEventArgs(button, GetSnapshot()));

            if (previousState == ButtonState.Released)
            {
                inputState         |= InputStates.Pressed;
                OnButtonPressed?.Invoke(null, new MouseButtonEventArgs(button, GetSnapshot()));
            }
        }
        else if (currentState == ButtonState.Released)
        {
            inputState         |= InputStates.Up;
            
            if (previousState == ButtonState.Pressed)
            {
                inputState |= InputStates.Released;
                OnButtonReleased?.Invoke(null, new MouseButtonEventArgs(button, GetSnapshot()));
            }
        }

        state.InputState = inputState;
        SetButtonState(button, state);
    }

    private static void UpdateButtonDragging(MouseButtons button)
    {
        MouseButtonState state = GetButtonState(button);

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
                OnDragReleased?.Invoke(null, new MouseDraggedEventArgs(button, state.DragStartPosition, GetSnapshot()));
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
                OnDragStarted?.Invoke(null, new MouseDraggedEventArgs(button, state.DragStartPosition, GetSnapshot()));
            }
        }

        if (state.IsDragging)
        {
            OnDragging?.Invoke(null, new MouseDraggedEventArgs(button, state.DragStartPosition, GetSnapshot()));
        }

        SetButtonState(button, state);
    }

    private static void UpdateButtonConsecutiveClicking(MouseButtons button, GameTime gameTime)
    {
        MouseButtonState state = GetButtonState(button);

        if (WasButtonPressed(button))
        {
            TimeSpan timeSinceLastClick = gameTime.TotalGameTime - state.LastPressTime;

            if (timeSinceLastClick.TotalSeconds <= ConsecutiveClickThresholdSeconds)
            {
                state.ConsecutiveClicks++;

                OnMultiClick?.Invoke(null,
                                     new MouseMultiClickEventArgs(button, state.ConsecutiveClicks, GetSnapshot()));
            }
            else
            {
                state.ConsecutiveClicks = 0;
            }

            state.LastPressTime = gameTime.TotalGameTime;
        }

        SetButtonState(button, state);
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

    private static MouseSnapshot GetSnapshot()
    {
        return default(MouseSnapshot);
    }

    #endregion
}